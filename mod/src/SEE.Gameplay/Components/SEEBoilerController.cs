using System;
using SEE.Core.Abstractions;
using SEE.Core.Config;
using SEE.Core.Constants;
using SEE.Core.Formulas;
using SEE.Core.Logging;
using SEE.Core.Maintenance;
using SEE.Core.State;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Buildings;
using Timberborn.EntitySystem;
using Timberborn.GameDistricts;
using Timberborn.GoodConsumingBuildingSystem;
using Timberborn.InventorySystem;
using Timberborn.MechanicalSystem;
using Timberborn.TimeSystem;
using Timberborn.WorldPersistence;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// The single runtime component of all 24 steam boiler blueprints (CONTRACT_SEE §2/§5,
    /// REQ-ARCH-07). One controller serves every size/tier/faction: the blueprint
    /// <see cref="SEEBoilerSpec"/> selects the numbers, the config resolves the session values, so
    /// there is no per-blueprint code path (REQ-NFR-04).
    ///
    /// Responsibilities (CONTRACT_SEE §8): one O(1) settlement step driven by
    /// <c>BoilerTickSystem</c>; fuel/water accounting through a single withdrawal path (AD-3);
    /// output modulation through <c>MechanicalNode.SetOutputMultiplier</c> (D-1 case A);
    /// wear/maintenance through <see cref="MaintenanceLedger"/>; per-entity persistence (D-6 hybrid)
    /// and the read view / commands of <see cref="IBoilerStatusView"/> / <see cref="IBoilerLogic"/>.
    ///
    /// Every entry point is isolated so one damaged boiler can never stop the game loop
    /// (REQ-SAFE-02). Lifecycle mirrors the verified reference-mod component: disabled until the
    /// building is finished, enabled from <see cref="OnEnterFinishedState"/> /
    /// <see cref="PostLoadEntity"/>, unregistered on delete.
    /// </summary>
    public sealed partial class SEEBoilerController : BaseComponent, IAwakableComponent,
        IFinishedStateListener, IDeletableEntity, IPostLoadableEntity, IPersistentEntity,
        IBoilerLogic, IBoilerStatusView
    {
        private readonly SEERuntime _runtime;
        private readonly SEEBoilerRegistry _registry;
        private readonly IDayNightCycle _dayNightCycle;
        private readonly MaintenanceLedger _ledger = new MaintenanceLedger();
        private readonly SEEMaintenanceController _maintenance;

        // Blueprint specs and vanilla components (all optional at Awake, re-resolved on activation).
        private SEEBoilerSpec? _boilerSpec;
        private SEEMaintenanceSpec? _maintenanceSpec;
        private BlockObject? _blockObject;
        private EntityComponent? _entityComponent;
        private MechanicalNode? _mechanicalNode;
        private GoodConsumingBuilding? _goodConsuming;
        private GoodConsumingToggle? _fuelToggle;
        private Inventory? _fuelInventory;
        private Inventory? _waterInventory;
        private BuildingAccessible? _buildingAccessible;
        private DistrictBuilding? _districtBuilding;

        // Immutable per-blueprint numbers resolved from the specs.
        private MaintenanceRules _rules = MaintenanceRules.Default;
        private BoilerTier _tier = BoilerTier.T1;
        private BoilerSize _size = BoilerSize.Small;
        private BoilerFaction _faction = BoilerFaction.Folktails;
        private int _ratedHorsepower;
        private int _fuelCapacity;
        private int _waterCapacity;
        private float _fuelPerHour;
        private float _waterPerHour;
        private float _logEnergy = 1f;
        private float _plankEnergy = 2f;
        private bool _allowsLog;
        private bool _allowsPlank;
        private bool _fuelVanilla;
        private float _standbyLoadRatio;
        private Guid _entityId;

        // Runtime flags.
        private bool _awake;
        private bool _activated;
        private bool _saveLoaded;
        private bool _applyAfterLoad;
        private bool _paused;
        private bool _maintenanceRequested;
        private bool _autoMaintenanceDefault = true;
        private bool _componentsResolved;
        private bool _vanillaConsumptionPaused;
        private bool _waterTankWarningLogged;
        private bool _hasFuel;
        private bool _hasWater;
        private WaterSupplyMode _waterSupplyMode = WaterSupplyMode.Auto;
        private bool _powerLineAttached;

        // Cached view values (no game read in the panel path, REQ-UI-01).
        private BoilerStatus _status = BoilerStatus.Idle;
        private float _conditionMultiplier = 1f;
        private float _loadRatio;
        private int _actualOutput;
        private int _networkSupplyHp;
        private int _networkDemandHp;
        private float _networkEfficiency;
        private int _fuelInStock;
        private int _waterInStock;
        private float _nodeMultiplier;

        // Fractional consumption remainders (< 1 good, never persisted, CONTRACT_SEE §6.7).
        private double _fuelCarry;
        private double _waterCarry;

        /// <summary>
        /// Creates the controller. Every dependency is a DI singleton with exactly one constructor
        /// (game API fact); the transient controller itself is created once per boiler entity by
        /// the template decorator.
        /// </summary>
        /// <param name="runtime">Session config + throttled log singleton.</param>
        /// <param name="registry">Boiler registry answering UI queries and commands.</param>
        /// <param name="dayNightCycle">Game clock; supplies the current game day.</param>
        public SEEBoilerController(
            SEERuntime runtime,
            SEEBoilerRegistry registry,
            IDayNightCycle dayNightCycle)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _dayNightCycle = dayNightCycle ?? throw new ArgumentNullException(nameof(dayNightCycle));
            _maintenance = new SEEMaintenanceController(_ledger);
        }

        // ------------------------------------------------------------------ IBoilerLogic

        /// <inheritdoc />
        public bool SetPaused(Guid entityId, bool paused)
        {
            if (!IsCommandTarget(entityId))
            {
                return false;
            }

            _paused = paused;
            if (paused)
            {
                WriteMultiplier(0f);
            }

            UpdateViewState();
            return true;
        }

        /// <inheritdoc />
        public bool RequestMaintenanceNow(Guid entityId)
        {
            if (!IsCommandTarget(entityId))
            {
                return false;
            }

            _maintenanceRequested = true;
            return true;
        }

        /// <inheritdoc />
        public bool SetAutoMaintenance(Guid entityId, bool enabled)
        {
            if (!IsCommandTarget(entityId))
            {
                return false;
            }

            _ledger.AutoMaintenance = enabled;
            UpdateViewState();
            return true;
        }

        /// <inheritdoc />
        public bool SetWaterSupplyMode(Guid entityId, WaterSupplyMode mode)
        {
            if (!IsCommandTarget(entityId))
            {
                return false;
            }

            _waterSupplyMode = Enum.IsDefined(typeof(WaterSupplyMode), mode) ? mode : WaterSupplyMode.Auto;
            // TODO(P5): re-resolve the effective mode from the compat detection result; a player
            // intent that is unavailable right now must stay stored but run as hauler.
            return true;
        }

        // ------------------------------------------------------------------ internal settlement surface

        /// <summary>True once construction finished and the controller is registered.</summary>
        internal bool IsActive => _activated;

        /// <summary>Rated horsepower of this blueprint (already includes the faction multiplier).</summary>
        internal int RatedOutput => _ratedHorsepower;

        /// <summary>Output booked by the previous settlement step, used by the network λ batch.</summary>
        internal int SettledOutput => _actualOutput;

        /// <summary>Mechanical graph this boiler belongs to; null before the node joins a graph.</summary>
        internal MechanicalGraph? CurrentGraph
        {
            get
            {
                MechanicalNode? node = _mechanicalNode;
                if (node == null)
                {
                    return null;
                }

                try
                {
                    return node.Graph;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        // ------------------------------------------------------------------ state helpers

        /// <summary>Resolves the immutable numbers of this blueprint from its specs.</summary>
        private void ResolveSpec()
        {
            SEEBoilerSpec spec = _boilerSpec!;
            _tier = spec.ResolveTier();
            _size = spec.ResolveSize();
            _faction = spec.ResolveFaction();
            _ratedHorsepower = spec.Horsepower > 0 ? spec.Horsepower : 0;
            _fuelPerHour = spec.FuelPerHour > 0f && !float.IsNaN(spec.FuelPerHour) ? spec.FuelPerHour : 0f;
            _waterPerHour = spec.WaterPerHour > 0f && !float.IsNaN(spec.WaterPerHour) ? spec.WaterPerHour : 0f;
            _fuelCapacity = spec.FuelCapacityGoods > 0 ? spec.FuelCapacityGoods : 0;
            _waterCapacity = spec.WaterCapacityGoods > 0 ? spec.WaterCapacityGoods : 0;
            _autoMaintenanceDefault = spec.AutoMaintenanceDefault;
            ResolveFuelKinds(spec);
            _status = BoilerStatus.Idle;
        }

        /// <summary>Precomputes the allowed fuel kinds and their energy values once (no tick strings).</summary>
        private void ResolveFuelKinds(SEEBoilerSpec spec)
        {
            _allowsLog = false;
            _allowsPlank = false;
            _logEnergy = SeeConsumptionLedger.DefaultLogEnergy;
            _plankEnergy = SeeConsumptionLedger.DefaultPlankEnergy;

            for (int i = 0; i < spec.FuelGoodIds.Length; i++)
            {
                string goodId = spec.FuelGoodIds[i];
                if (string.Equals(goodId, GoodIds.Log, StringComparison.OrdinalIgnoreCase))
                {
                    _allowsLog = true;
                    _logEnergy = spec.FuelValueFor(GoodIds.Log);
                }
                else if (string.Equals(goodId, GoodIds.Plank, StringComparison.OrdinalIgnoreCase))
                {
                    _allowsPlank = true;
                    _plankEnergy = spec.FuelValueFor(GoodIds.Plank);
                }
            }

            if (!_allowsLog && !_allowsPlank)
            {
                // Missing data means "no restriction declared"; vanilla fuel is Log (contract §3.2).
                _allowsLog = true;
            }

            // P4B: the panel-visible list of burnable goods (same source as the consumption path).
            _fuelGoods.Clear();
            if (_allowsLog)
            {
                _fuelGoods.Add(GoodIds.Log);
            }

            if (_allowsPlank)
            {
                _fuelGoods.Add(GoodIds.Plank);
            }
        }

        /// <summary>Rebuilds the maintenance rules from the blueprint maintenance spec + config.</summary>
        private void ApplyRules()
        {
            System.Collections.Generic.IReadOnlyList<MaintenanceMaterial> materials =
                _maintenanceSpec != null
                    ? _maintenanceSpec.ResolveMaterials()
                    : Array.Empty<MaintenanceMaterial>();
            if (!_runtime.Config.Maintenance.ManualMaterialsEnabled)
            {
                // Debug/newbie switch (requirements §20.2): no material requirement at all.
                materials = Array.Empty<MaintenanceMaterial>();
            }

            _rules = _runtime.RulesFor(_tier, true, materials);
            bool blueprintEnabled = _maintenanceSpec == null || _maintenanceSpec.Enabled;
            if (!blueprintEnabled || !_rules.Enabled)
            {
                // CONTRACT_SEE §3.3: the blueprint switch can disable maintenance for one boiler
                // (condition frozen at 100, no materials); the config switch disables it globally.
                _rules = new MaintenanceRules(
                    enabled: false,
                    auto: _rules.Auto,
                    autoThreshold: _rules.AutoThreshold,
                    durationHours: _rules.DurationHours,
                    intervalDays: _rules.IntervalDays,
                    materials: _rules.Materials,
                    manualWarnAbove: _rules.ManualWarnAbove);
            }

            _ledger.Rules = _rules;

            // Resolved here (and not in Awake) so a blueprint restored before the runtime singleton
            // finished Load still sees the real session config (config wins over the blueprint token).
            _fuelVanilla = string.Equals(_boilerSpec!.FuelScaling, "vanilla", StringComparison.OrdinalIgnoreCase)
                           || _runtime.Config.Balance.FuelScaling == FuelScalingMode.Vanilla;

            // 2026-11-08 hotfix-3: the network demand collapses to 0 at night (worker buildings
            // disable their power consumption outside working hours), which used to turn every
            // supplied boiler into Idle. A positive standby ratio keeps it running at a low load;
            // 0 restores the old idle-at-zero-demand behaviour.
            _standbyLoadRatio = _runtime.Config.Balance.StandbyLoadRatio;
        }

        /// <summary>Recomputes the cached, allocation-free values the read view serves.</summary>
        private void UpdateViewState()
        {
            _conditionMultiplier = ConditionCurve.Multiplier(_ledger.Condition);
            var input = new BoilerStateInput(
                isPaused: _paused,
                maintenancePhase: _ledger.Phase,
                condition: _ledger.Condition,
                hasFuel: _hasFuel,
                hasWater: _hasWater,
                networkDemandRatio: _loadRatio);
            _status = BoilerStateMachine.Evaluate(in input);
            if (!BoilerStateMachine.Produces(_status))
            {
                _actualOutput = 0;
            }

            // P4B display-only snapshot values, derived from already cached numbers (no game read).
            _waterBeaverEquivalent = ComputeWaterBeaverEquivalent(_waterPerHour);
            _nextMaintenanceDay = ComputeNextMaintenanceDay();
        }

        /// <summary>Rejects a command that targets a different or inactive boiler.</summary>
        private bool IsCommandTarget(Guid entityId)
        {
            return _activated && entityId != Guid.Empty && entityId == _entityId;
        }

        /// <summary>Current game day as a float, used for the saved last-maintenance day.</summary>
        private float CurrentDay()
        {
            try
            {
                return _dayNightCycle != null ? _dayNightCycle.DayNumber : 0f;
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        private void LogError(string message, Exception? exception)
        {
            try
            {
                string suffix = exception != null ? " (" + exception.GetType().Name + ")" : string.Empty;
                _runtime.Log.Error(SeeLogKeys.EntityError, message + suffix + "|" + _entityId.ToString("N"));
            }
            catch (Exception)
            {
                // Diagnostics must not escape (REQ-SAFE-02).
            }
        }

        private void LogWarn(string message)
        {
            try
            {
                _runtime.Log.Warn(SeeLogKeys.EntityError, message + "|" + _entityId.ToString("N"));
            }
            catch (Exception)
            {
                // Diagnostics must not escape.
            }
        }
    }
}
