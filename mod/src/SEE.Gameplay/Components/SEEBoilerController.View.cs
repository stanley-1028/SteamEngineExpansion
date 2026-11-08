using System.Collections.Generic;
using SEE.Core.Abstractions;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Partial part: the read model exposed to SEE.UI through
    /// <see cref="SEE.Core.Abstractions.IBoilerStatusView"/> (REQ-ARCH-03, REQ-UI-01). Every member
    /// reads a cached field or the pure maintenance ledger, so a 4 Hz panel refresh allocates
    /// nothing and never touches a game component.
    /// </summary>
    public sealed partial class SEEBoilerController
    {
        /// <inheritdoc />
        public System.Guid EntityId => _entityId;

        /// <inheritdoc />
        public BoilerStatus Status => _status;

        /// <inheritdoc />
        public MaintenancePhase MaintenancePhase => _ledger.Phase;

        /// <inheritdoc />
        public BoilerTier Tier => _tier;

        /// <inheritdoc />
        public BoilerSize Size => _size;

        /// <inheritdoc />
        public BoilerFaction Faction => _faction;

        /// <inheritdoc />
        public bool IsFinished => _activated;

        /// <inheritdoc />
        public bool IsPaused => _paused;

        /// <inheritdoc />
        public int RatedHorsepower => _ratedHorsepower;

        /// <inheritdoc />
        public int ActualOutput => _actualOutput;

        /// <inheritdoc />
        public float Condition => _ledger.Condition;

        /// <inheritdoc />
        public float ConditionMultiplier => _conditionMultiplier;

        /// <inheritdoc />
        public float LoadRatio => _loadRatio;

        /// <inheritdoc />
        public int NetworkSupplyHp => _networkSupplyHp;

        /// <inheritdoc />
        public int NetworkDemandHp => _networkDemandHp;

        /// <inheritdoc />
        public float NetworkEfficiency => _networkEfficiency;

        /// <inheritdoc />
        public int FuelInStock => _fuelInStock;

        /// <inheritdoc />
        public int FuelCapacity => _fuelCapacity;

        /// <inheritdoc />
        public int WaterInStock => _waterInStock;

        /// <inheritdoc />
        public int WaterCapacity => _waterCapacity;

        /// <inheritdoc />
        public float FuelPerHour => _fuelPerHour;

        /// <inheritdoc />
        public float WaterPerHour => _waterPerHour;

        /// <inheritdoc />
        public bool AutoMaintenance => _ledger.AutoMaintenance;

        /// <inheritdoc />
        public float MaintenanceRemainingHours => _ledger.RemainingHours;

        /// <inheritdoc />
        public float LastMaintenanceDay => _ledger.LastMaintenanceDay;

        /// <inheritdoc />
        public float MaintenanceTriggerThreshold => _rules.AutoThreshold;

        /// <inheritdoc />
        public WaterSupplyMode WaterSupplyMode => _waterSupplyMode;

        /// <inheritdoc />
        public bool PowerLineAttached => _powerLineAttached;

        /// <inheritdoc />
        public float WaterBeaverEquivalent => _waterBeaverEquivalent;

        /// <inheritdoc />
        public IReadOnlyList<string> FuelGoods => _fuelGoods;

        /// <inheritdoc />
        public IReadOnlyList<MaintenanceMaterialStatus> MaintenanceMaterials => _materialStatuses;

        /// <inheritdoc />
        public float NextMaintenanceDay => _nextMaintenanceDay;

        /// <inheritdoc />
        public int MissingMaterialCount => _missingMaterialCount;

        /// <summary>
        /// P4B contract slot for the water-mode availability reason. Gameplay currently reports
        /// <see cref="WaterSupplyAvailability.Available"/>; P5 (detection wiring) is the card that
        /// publishes the Timber Pipes/Power Lines facts into these four slots, so the UI never
        /// invents a reason. G6/G7 stay open in the UI README until that wiring lands.
        /// </summary>
        public WaterSupplyAvailability WaterSupplyAvailability => _waterSupplyAvailability;

        /// <inheritdoc />
        public bool PipeSupplyActive => _pipeSupplyActive;

        /// <inheritdoc />
        public bool BeltSupplyActive => _beltSupplyActive;

        /// <inheritdoc />
        public bool BeltHasPower => _beltHasPower;
    }
}
