using System;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Logging;
using SEE.Core.Maintenance;
using Timberborn.Buildings;
using Timberborn.EntitySystem;
using Timberborn.GameDistricts;
using Timberborn.GoodConsumingBuildingSystem;
using Timberborn.InventorySystem;
using Timberborn.MechanicalSystem;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Partial part: component lifecycle and vanilla-component binding (REQ-SAFE-02/13, AD-10).
    /// The boiler stays disabled until the building is finished, mirrors the verified reference-mod
    /// lifecycle, and unregisters itself on delete so the registry never serves a dead entity.
    ///
    /// Binding is deliberately retried on the first ticks: the dedicated inventory decorators of
    /// the fuel consumer and the water tank may finish after the controller activates, and a
    /// missing reference must degrade to "not consuming yet", never to an exception.
    /// </summary>
    public sealed partial class SEEBoilerController
    {
        /// <inheritdoc />
        public void Awake()
        {
            try
            {
                _boilerSpec = HasComponent<SEEBoilerSpec>() ? GetComponent<SEEBoilerSpec>() : null;
                _maintenanceSpec = HasComponent<SEEMaintenanceSpec>() ? GetComponent<SEEMaintenanceSpec>() : null;
                _blockObject = HasComponent<Timberborn.BlockSystem.BlockObject>()
                    ? GetComponent<Timberborn.BlockSystem.BlockObject>()
                    : null;
                _entityComponent = HasComponent<EntityComponent>() ? GetComponent<EntityComponent>() : null;

                if (_boilerSpec == null)
                {
                    DisableComponent();
                    _awake = false;
                    LogError("boiler blueprint has no SEEBoilerSpec; component disabled", null);
                    return;
                }

                ResolveSpec();
                _ledger.Reset(
                    SEEConst.MaxCondition,
                    _autoMaintenanceDefault,
                    MaintenancePhase.None,
                    0f,
                    0f);
                ApplyRules();

                // Nothing may run before the building is finished: no ticks, no production.
                DisableComponent();
                _awake = true;
            }
            catch (Exception exception)
            {
                _awake = false;
                DisableComponent();
                LogError("boiler failed to initialise; component disabled", exception);
            }
        }

        /// <inheritdoc />
        public void OnEnterFinishedState()
        {
            Activate();
        }

        /// <inheritdoc />
        public void OnExitFinishedState()
        {
            Deactivate();
        }

        /// <inheritdoc />
        public void PostLoadEntity()
        {
            // Loaded finished buildings never receive OnEnterFinishedState again. The per-entity
            // save state was already applied by IPersistentEntity.Load; the deferred part
            // (mechanical-node write-back) runs on the first tick (REQ-SAVE-12).
            Activate();
        }

        /// <inheritdoc />
        public void DeleteEntity()
        {
            try
            {
                Deactivate();
            }
            catch (Exception exception)
            {
                LogError("boiler delete failed", exception);
            }
        }

        private void Activate()
        {
            if (_activated || !_awake || _blockObject == null || !_blockObject.IsFinished)
            {
                return;
            }

            try
            {
                ResolveEntityId();
                TryResolveComponents();
                ApplyRules();
                // P4B read-only snapshot: the panel shows a real material list from the first frame
                // instead of the unknown glyph while the 10-tick refresh has not run yet.
                RefreshMaintenanceMaterialSnapshot();
                _registry.Register(_entityId, this);
                EnableComponent();
                _activated = true;

                bool newBoiler = !_saveLoaded;
                if (newBoiler)
                {
                    _saveLoaded = true;
                }

                // AD-3: the vanilla consumer is paused permanently; the controller is the single
                // withdrawal path. Water comes from the dedicated public input inventory, which the
                // owner must enable before haulers can deliver into it.
                EnsureVanillaConsumptionPaused();
                EnableWaterInventory();
                _nodeMultiplier = float.NaN;
                RefreshStock();
                if (!_applyAfterLoad)
                {
                    // A freshly activated node may still carry its default multiplier; publish 0
                    // until the first settlement computed the real demand (no uncontrolled output).
                    // A loaded boiler skips the write: REQ-SAVE-12 defers it to the first tick so
                    // nothing is written while the vanilla mechanical graph is still rebuilding.
                    WriteMultiplier(0f);
                }

                UpdateViewState();
                if (newBoiler)
                {
                    _runtime.Log.Info(
                        SeeLogKeys.BoilerCreated,
                        "boiler active: tier=" + _tier + " size=" + _size + " entity=" + _entityId.ToString("N"));
                }
            }
            catch (Exception exception)
            {
                _activated = false;
                try
                {
                    _registry.Unregister(_entityId);
                }
                catch (Exception)
                {
                    // Defensive: a stale registry entry only makes the panel query return false.
                }

                DisableComponent();
                LogError("boiler activation failed; component disabled", exception);
            }
        }

        private void Deactivate()
        {
            if (!_activated)
            {
                return;
            }

            _activated = false;
            try
            {
                _registry.Unregister(_entityId);
            }
            catch (Exception)
            {
                // A stale registry entry would only make the panel query return false.
            }

            // Owner-side parity with vanilla inventory owners: an unfinished/reverted building must
            // not stay a hauling target.
            DisableWaterInventory();
            DisableComponent();
        }

        private void ResolveEntityId()
        {
            if (_entityComponent == null && HasComponent<EntityComponent>())
            {
                _entityComponent = GetComponent<EntityComponent>();
            }

            _entityId = _entityComponent != null ? _entityComponent.EntityId : Guid.Empty;
        }

        /// <summary>
        /// Binds the vanilla components once; every lookup is isolated because a component may be
        /// absent (unfinished building, damaged inventory) without being an error.
        /// </summary>
        private void TryResolveComponents()
        {
            if (_componentsResolved)
            {
                return;
            }

            TryResolve(ref _mechanicalNode);
            TryResolve(ref _goodConsuming);
            TryResolve(ref _buildingAccessible);
            TryResolve(ref _districtBuilding);

            if (_goodConsuming != null && _fuelInventory == null)
            {
                try
                {
                    _fuelInventory = _goodConsuming.Inventory;
                }
                catch (Exception)
                {
                    // The dedicated decorator has not initialised the inventory yet; retry next tick.
                }
            }

            // Bug fix 2026-11-08: the water tank is a dedicated public input inventory created by
            // SEEWaterInventoryInitializer (not a StockpileSpec and not a GoodConsuming good any
            // more). AllInventories is used because the vanilla initializer leaves the inventory
            // disabled until the owner enables it; an enabled-only scan would never find it.
            TryResolveWaterInventory();

            // The water tank is not part of the resolution gate: a missing tank must degrade to the
            // visible NoWater state, never silently disable the whole boiler. The controller warns
            // once (see TickBoilers) so a broken decorator wiring is diagnosable in Player.log.
            _componentsResolved = _mechanicalNode != null
                                  && _goodConsuming != null
                                  && _fuelInventory != null;
        }

        private void TryResolve<T>(ref T? field) where T : Timberborn.BaseComponentSystem.BaseComponent
        {
            if (field != null)
            {
                return;
            }

            try
            {
                field = HasComponent<T>() ? GetComponent<T>() : null;
            }
            catch (Exception)
            {
                field = null;
            }
        }

        /// <summary>
        /// AD-3: pause the vanilla good consumer permanently. The toggle is created lazily once (the
        /// game API adds a listener on every <c>GetGoodConsumingToggle</c> call) and re-asserted each
        /// settlement in case another component resumed it.
        /// </summary>
        private void EnsureVanillaConsumptionPaused()
        {
            if (_vanillaConsumptionPaused)
            {
                return;
            }

            GoodConsumingBuilding? consumer = _goodConsuming;
            if (consumer == null)
            {
                return;
            }

            try
            {
                GoodConsumingToggle? toggle = _fuelToggle;
                if (toggle == null)
                {
                    toggle = consumer.GetGoodConsumingToggle();
                    _fuelToggle = toggle;
                }

                if (toggle != null && !toggle.Paused)
                {
                    toggle.PauseConsumption();
                }

                _vanillaConsumptionPaused = toggle != null;
            }
            catch (Exception exception)
            {
                LogWarn("vanilla consumption could not be paused (" + exception.GetType().Name + ")");
                _vanillaConsumptionPaused = false;
            }
        }

    }
}
