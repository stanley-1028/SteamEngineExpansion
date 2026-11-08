using System;
using System.Collections.Generic;

namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Read-only view of one boiler, the only contract the UI assembly consumes
    /// (REQ-ARCH-03/REQ-UI-01). It exposes plain values only: no game type, no delegate, no
    /// mutable collection, so SEE.UI never needs a reference to SEE.Gameplay.
    /// </summary>
    public interface IBoilerStatusView
    {
        /// <summary>Entity id of the boiler (<c>EntityComponent.EntityId</c>, a GUID).</summary>
        Guid EntityId { get; }

        /// <summary>Highest-priority state (§9.7).</summary>
        BoilerStatus Status { get; }

        /// <summary>Persisted maintenance sub-state (§9.7/§9.8).</summary>
        MaintenancePhase MaintenancePhase { get; }

        /// <summary>Technology tier of the blueprint.</summary>
        BoilerTier Tier { get; }

        /// <summary>Footprint class of the blueprint.</summary>
        BoilerSize Size { get; }

        /// <summary>Faction of the blueprint.</summary>
        BoilerFaction Faction { get; }

        /// <summary>True once construction finished.</summary>
        bool IsFinished { get; }

        /// <summary>True when the player paused this boiler only.</summary>
        bool IsPaused { get; }

        /// <summary>Rated horsepower of this blueprint (already includes the faction multiplier).</summary>
        int RatedHorsepower { get; }

        /// <summary>
        /// Actual output in whole hp after the condition curve and load. This is the contract's
        /// output-horsepower source (the frozen name stays <c>ActualOutput</c>; adding a second
        /// <c>OutputHorsepower</c> alias would let the two values drift).
        /// </summary>
        int ActualOutput { get; }

        /// <summary>Current condition in 0..100 (float keeps multi-day precision).</summary>
        float Condition { get; }

        /// <summary>Output multiplier of the current condition band (0, 0.40, 0.85, 0.93 or 1).</summary>
        float ConditionMultiplier { get; }

        /// <summary>Load ratio of this boiler, 0..1.</summary>
        float LoadRatio { get; }

        /// <summary>Total mechanical power supply of the network, in hp.</summary>
        int NetworkSupplyHp { get; }

        /// <summary>Total mechanical power demand of the network, in hp.</summary>
        int NetworkDemandHp { get; }

        /// <summary>Network power efficiency, 0..1.</summary>
        float NetworkEfficiency { get; }

        /// <summary>Fuel items currently in the fuel inventory.</summary>
        int FuelInStock { get; }

        /// <summary>Fuel inventory capacity in items.</summary>
        int FuelCapacity { get; }

        /// <summary>Water items currently in the water tank.</summary>
        int WaterInStock { get; }

        /// <summary>Water tank capacity in items.</summary>
        int WaterCapacity { get; }

        /// <summary>Rated fuel consumption at full load, in items per game hour.</summary>
        float FuelPerHour { get; }

        /// <summary>Rated water consumption at full load, in items per game hour.</summary>
        float WaterPerHour { get; }

        /// <summary>Per-boiler automatic maintenance flag.</summary>
        bool AutoMaintenance { get; }

        /// <summary>Remaining maintenance downtime in game hours (0 unless maintaining).</summary>
        float MaintenanceRemainingHours { get; }

        /// <summary>Game day of the last completed maintenance; 0 when never maintained.</summary>
        float LastMaintenanceDay { get; }

        /// <summary>Condition at or below which automatic maintenance triggers.</summary>
        float MaintenanceTriggerThreshold { get; }

        /// <summary>Player intent for the water supply mode.</summary>
        WaterSupplyMode WaterSupplyMode { get; }

        /// <summary>True when the Power Lines adapter successfully injected a connection point.</summary>
        bool PowerLineAttached { get; }

        // ---------------------------------------------------------------- P4B additions (read-only)

        /// <summary>
        /// Rated water consumption expressed as "≈N beavers' daily drinking water" (requirements
        /// §15.2/§7.3). Gameplay computes the value once per settlement step so the panel does not
        /// have to duplicate the research constant; 0 means "cannot be computed".
        /// </summary>
        float WaterBeaverEquivalent { get; }

        /// <summary>The two fuel goods this boiler may burn, in blueprint order (Log / Plank).</summary>
        IReadOnlyList<string> FuelGoods { get; }

        /// <summary>
        /// Maintenance materials of the next cycle with the district stock last observed by the
        /// maintenance step (requirements §9.4/§15.2); empty while the boiler declares none.
        /// </summary>
        IReadOnlyList<MaintenanceMaterialStatus> MaintenanceMaterials { get; }

        /// <summary>
        /// Game day the next maintenance is expected (display estimate; 0 means unknown or
        /// maintenance disabled). The authoritative countdown stays gameplay state.
        /// </summary>
        float NextMaintenanceDay { get; }

        /// <summary>Number of materials still short of the requirement (0 when all are covered).</summary>
        int MissingMaterialCount { get; }

        /// <summary>Why the selected water-supply mode cannot run, if it cannot (requirements §15.2).</summary>
        WaterSupplyAvailability WaterSupplyAvailability { get; }

        /// <summary>True when the Timber Pipes network currently feeds this boiler's water tank.</summary>
        bool PipeSupplyActive { get; }

        /// <summary>True when a conveyor currently delivers fuel to this boiler.</summary>
        bool BeltSupplyActive { get; }

        /// <summary>True when the conveyor network delivers power (false when it is unpowered).</summary>
        bool BeltHasPower { get; }
    }
}
