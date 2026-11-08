using SEE.Core.Constants;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;

namespace SEE.Gameplay
{
    /// <summary>
    /// The stable key table of the boiler save (requirements §14.1/§9.8, D-6 hybrid save).
    ///
    /// Two surfaces share the name "SEE": the world-node index (singleton key) and the per-entity
    /// state component. Field names are an external contract: a key may be added, never renamed,
    /// and enums are stored by name so adding a member later cannot reinterpret older data.
    /// </summary>
    internal static class SeeSaveKeys
    {
        /// <summary>World-node key of the SEE index (D-6).</summary>
        internal static readonly SingletonKey Node = new SingletonKey(SEEConst.SaveComponentKey);

        /// <summary>Per-entity component key holding the seven persisted boiler fields (D-6).</summary>
        internal static readonly ComponentKey Component = new ComponentKey(SEEConst.SaveComponentKey);

        /// <summary>Index save schema version written by this build (1 in v0.1).</summary>
        internal static readonly PropertyKey<int> SchemaVersion = new PropertyKey<int>("schemaVersion");

        /// <summary>Mod version that wrote the node; diagnostics only (REQ-SAVE-02).</summary>
        internal static readonly PropertyKey<string> ModVersion = new PropertyKey<string>("modVersion");

        /// <summary>Checksum of the canonical node document (requirements §14.3).</summary>
        internal static readonly PropertyKey<string> Checksum = new PropertyKey<string>("checksum");

        /// <summary>
        /// Canonical JSON document of the world node (CONTRACT_SEE §6.1/§6.2, AD-11). The vanilla
        /// singleton persistence has no nested-object primitive, so the Core codec document — which
        /// carries schemaVersion/modVersion/checksum/buildings[]/compat in the frozen shape — is
        /// stored as one string; the primitive header keys above mirror it for inspection. The
        /// per-entity component remains the only source of truth for the seven mutable fields.
        /// </summary>
        internal static readonly PropertyKey<string> Document = new PropertyKey<string>("document");

        /// <summary>Wear 0..100 (REQ-MAINT-01).</summary>
        internal static readonly PropertyKey<float> Condition = new PropertyKey<float>("condition");

        /// <summary>Per-boiler automatic maintenance flag (REQ-MAINT-05).</summary>
        internal static readonly PropertyKey<bool> AutoMaintenance = new PropertyKey<bool>("autoMaintenance");

        /// <summary>Maintenance sub-state, stored as the <c>MaintenancePhase</c> name.</summary>
        internal static readonly PropertyKey<string> MaintenanceState = new PropertyKey<string>("maintenanceState");

        /// <summary>Remaining maintenance downtime in game hours, 0 unless maintaining.</summary>
        internal static readonly PropertyKey<float> MaintenanceRemainingHours =
            new PropertyKey<float>("maintenanceRemainingHours");

        /// <summary>Game day of the last completed maintenance.</summary>
        internal static readonly PropertyKey<float> LastMaintenanceDay = new PropertyKey<float>("lastMaintenanceDay");

        /// <summary>Player intent for the water supply mode (REQ-FUEL-11).</summary>
        internal static readonly PropertyKey<string> WaterSupplyMode = new PropertyKey<string>("waterSupplyMode");

        /// <summary>True when the Power Lines adapter last injected a connection point.</summary>
        internal static readonly PropertyKey<bool> PowerLineAttached = new PropertyKey<bool>("powerLineAttached");
    }
}
