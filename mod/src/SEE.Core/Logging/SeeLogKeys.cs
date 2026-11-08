namespace SEE.Core.Logging
{
    /// <summary>
    /// Stable log keys (REQ-NFR-08). Messages are built from these constants, never from inline
    /// strings, so a validator can grep them and the 30 s throttle always keys on a known value.
    /// The category part (<c>SEE/Info</c>, <c>SEE/Warn</c>, <c>SEE/Error</c>, <c>SEE/Compat</c>)
    /// follows requirements §11.5.
    /// </summary>
    public static class SeeLogKeys
    {
        /// <summary>Mod startup completed.</summary>
        public const string Startup = "SEE/Info/Startup";

        /// <summary>Configuration loaded and validated.</summary>
        public const string ConfigLoaded = "SEE/Info/Config";

        /// <summary>Configuration value clamped, rejected or missing.</summary>
        public const string ConfigInvalid = "SEE/Warn/Config";

        /// <summary>Save data was migrated.</summary>
        public const string SaveMigrated = "SEE/Info/SaveMigrate";

        /// <summary>A single boiler save was corrupt and was safely reset.</summary>
        public const string SaveCorrupt = "SEE/Warn/SaveCorrupt";

        /// <summary>A save written by a newer schema was loaded read-only; no data was discarded.</summary>
        public const string SaveReadOnly = "SEE/Warn/SaveReadOnly";

        /// <summary>Save write/read failure.</summary>
        public const string SaveError = "SEE/Error/Save";

        /// <summary>A boiler became active.</summary>
        public const string BoilerCreated = "SEE/Info/BoilerCreated";

        /// <summary>A boiler entered maintenance.</summary>
        public const string MaintenanceStarted = "SEE/Info/Maintenance";

        /// <summary>Maintenance could not start (materials or road).</summary>
        public const string MaintenanceBlocked = "SEE/Warn/MaintenanceBlocked";

        /// <summary>Maintenance finished.</summary>
        public const string MaintenanceFinished = "SEE/Info/MaintenanceDone";

        /// <summary>A per-boiler tick threw and was contained.</summary>
        public const string TickError = "SEE/Error/Tick";

        /// <summary>A boiler lifecycle call threw and was contained.</summary>
        public const string EntityError = "SEE/Error/Entity";

        /// <summary>The registry query/command path failed.</summary>
        public const string QueryError = "SEE/Error/Query";

        /// <summary>Low-cost periodic diagnostics line.</summary>
        public const string Diagnostic = "SEE/Info/Diag";

        /// <summary>Compatibility scan result.</summary>
        public const string CompatScan = "SEE/Compat/Scan";

        /// <summary>An optional adapter or injection failed and degraded.</summary>
        public const string CompatAdapterError = "SEE/Compat/AdapterError";

        /// <summary>Optional runtime patch host state change.</summary>
        public const string CompatPatch = "SEE/Compat/Patch";
    }
}
