using SEE.Core.Abstractions;
using SEE.Core.Constants;

namespace SEE.Core.Save
{
    /// <summary>
    /// Plain snapshot of one boiler's persisted data (requirements §14.1, save schema v1; D-8).
    /// The gameplay layer copies it from <c>IObjectSaver</c>/<c>IObjectLoader</c> keys, while this
    /// type carries no game dependency, so the whole encode/validate/migrate path is testable
    /// outside the game.
    ///
    /// The eleven fields are exactly the §14.1 building entry minus the fields the game can
    /// recompute: entity id is stored as the GUID string ("D" format) because the requirements'
    /// original <c>long</c> does not exist in the game API (D-8).
    /// </summary>
    public sealed class BoilerSaveState
    {
        /// <summary>Entity id as a GUID string; empty when the record has no identity yet.</summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>Technology tier of the blueprint.</summary>
        public BoilerTier Tier { get; set; } = BoilerTier.T1;

        /// <summary>Footprint class of the blueprint.</summary>
        public BoilerSize Size { get; set; } = BoilerSize.Small;

        /// <summary>Faction of the blueprint (display/diagnostics only).</summary>
        public BoilerFaction Faction { get; set; } = BoilerFaction.Folktails;

        /// <summary>Condition 0..100 (REQ-MAINT-01).</summary>
        public float Condition { get; set; } = SEEConst.MaxCondition;

        /// <summary>Per-boiler automatic maintenance flag (REQ-MAINT-05).</summary>
        public bool AutoMaintenance { get; set; } = true;

        /// <summary>Persisted maintenance sub-state (REQ-MAINT-11).</summary>
        public MaintenancePhase MaintenancePhase { get; set; } = MaintenancePhase.None;

        /// <summary>Remaining maintenance downtime in game hours; 0 unless maintaining.</summary>
        public float MaintenanceRemainingHours { get; set; }

        /// <summary>Game day of the last completed maintenance; 0 when never maintained.</summary>
        public float LastMaintenanceDay { get; set; }

        /// <summary>Player intent for the water supply mode (REQ-FUEL-11).</summary>
        public WaterSupplyMode WaterSupplyMode { get; set; } = WaterSupplyMode.Auto;

        /// <summary>True when the Power Lines adapter last injected a connection point.</summary>
        public bool PowerLineAttached { get; set; }

        /// <summary>
        /// Creates the safe state of a freshly finished boiler: condition 100, automatic
        /// maintenance on (spec default overrides it later), no maintenance pending.
        /// </summary>
        /// <returns>A valid state with documented defaults.</returns>
        public static BoilerSaveState NewGame()
        {
            return new BoilerSaveState
            {
                EntityId = string.Empty,
                Tier = BoilerTier.T1,
                Size = BoilerSize.Small,
                Faction = BoilerFaction.Folktails,
                Condition = SEEConst.MaxCondition,
                AutoMaintenance = true,
                MaintenancePhase = MaintenancePhase.None,
                MaintenanceRemainingHours = 0f,
                LastMaintenanceDay = 0f,
                WaterSupplyMode = WaterSupplyMode.Auto,
                PowerLineAttached = false
            };
        }
    }
}
