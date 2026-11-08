namespace SEE.Core.Maintenance
{
    /// <summary>
    /// What the maintenance logic wants the gameplay layer to do this tick (requirements
    /// §9.5/§13.5). The ledger never consumes materials itself: the caller performs the atomic
    /// district withdrawal and then calls <see cref="MaintenanceLedger.BeginMaintenance"/>.
    /// </summary>
    public enum MaintenanceAction
    {
        /// <summary>No state change; keep running or keep waiting.</summary>
        None = 0,

        /// <summary>Materials and road are fine: consume the materials, then begin maintenance.</summary>
        Start = 1,

        /// <summary>Maintenance is wanted but the district inventory lacks materials; nothing was consumed.</summary>
        BlockedByMaterials = 2,

        /// <summary>Materials exist but no road reaches the boiler; nothing was consumed.</summary>
        BlockedByAccess = 3,

        /// <summary>The downtime timer reached zero; the caller should complete the maintenance.</summary>
        Completed = 4
    }
}
