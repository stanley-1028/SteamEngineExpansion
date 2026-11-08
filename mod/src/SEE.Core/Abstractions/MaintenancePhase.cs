namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Maintenance sub-state persisted in the save file (requirements §9.7/§9.8). It is separate
    /// from <see cref="BoilerStatus"/> on purpose: only these four values are written to disk, so
    /// adding a display-only status can never change the save contract.
    /// </summary>
    public enum MaintenancePhase
    {
        /// <summary>Normal operation; no maintenance request is pending.</summary>
        None = 0,

        /// <summary>Downtime timer is running; output is 0 and materials were already consumed.</summary>
        Maintaining = 1,

        /// <summary>Automatic maintenance is due but the district lacks the materials.</summary>
        MissingMaterials = 2,

        /// <summary>Automatic maintenance is due but no road reaches the pickup point.</summary>
        NoAccess = 3
    }
}
