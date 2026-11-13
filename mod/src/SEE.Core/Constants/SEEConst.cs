namespace SEE.Core.Constants
{
    /// <summary>
    /// Every runtime constant of the mod (requirements §11.4, REQ-ARCH-05). Balance values are
    /// deliberately absent: they are data (<c>engines.json</c>) or configuration
    /// (<c>SEE_Config.json</c>), never code. Only unit conversions and the handful of values that
    /// are part of the shipped contract live here.
    /// </summary>
    public static class SEEConst
    {
        /// <summary>Mod id; must equal <c>manifest.json</c> "Id" (REQ-NFR-01).</summary>
        public const string ModId = "SteamEngineExpansion";

        /// <summary>Name of the JSON configuration file in the mod root.</summary>
        public const string ConfigFileName = "SEE_Config.json";

        /// <summary>Save component key of one boiler (REQ-SAVE-01).</summary>
        public const string SaveComponentKey = "SEE";

        /// <summary>Production/maintenance tick interval in wall-clock seconds (REQ-PERF-01).</summary>
        public const double TickIntervalSeconds = 1.0;

        /// <summary>Total budget of one compatibility detection pass, in seconds (REQ-COMPAT-07).</summary>
        public const double DetectBudgetSeconds = 2.0;

        /// <summary>Per-key log throttle window in seconds (REQ-OPS-03).</summary>
        public const double LogThrottleSeconds = 30.0;

        /// <summary>Entity panel refresh cap in Hz (REQ-PERF-04).</summary>
        public const int UiRefreshHz = 4;

        /// <summary>Game hours per game day (Timberborn day length).</summary>
        public const double HoursPerDay = 24.0;

        /// <summary>
        /// Water one beaver drinks per game day (research 01 §5, requirements §3.3/§7.3). Used by
        /// the display-only "≈N beavers" conversion; the panel and the read view share this one
        /// constant so the two never disagree.
        /// </summary>
        public const float BeaverWaterPerDay = 2.12f;

        /// <summary>Upper bound of the persisted condition value, 0..100 (REQ-MAINT-01).</summary>
        public const float MaxCondition = 100.0f;

        /// <summary>Upper bound of the persisted maintenance timer, in game hours (D-7: closed [0,2]).</summary>
        public const float MaxMaintenanceRemainingHours = 2.0f;

        /// <summary>Shipped maintenance downtime, in game hours (REQ-MAINT-09, config default 2).</summary>
        public const float MaintenanceDurationHours = 2.0f;

        /// <summary>Shipped automatic-maintenance trigger threshold (REQ-MAINT-05, config default 50).</summary>
        public const float AutoThreshold = 50.0f;

        /// <summary>Condition above which a manual maintenance only shows a low-benefit hint (C-5).</summary>
        public const float ManualWarnAbove = 90.0f;

        /// <summary>Lower bound of the wear load factor, <c>0.4 + 0.6 * load</c> (REQ-MAINT-02).</summary>
        public const float LoadFloor = 0.4f;

        /// <summary>Range of the wear load factor above <see cref="LoadFloor"/> (0.6).</summary>
        public const float LoadFactorRange = 0.6f;

        /// <summary>Condition points a full interval consumes: 100 - 50 = 50 (REQ-MAINT-02/04).</summary>
        public const double WearPointsPerInterval = 50.0;

        /// <summary>Condition of the automatic-maintenance trigger, 100 - 50 (REQ-MAINT-04).</summary>
        public const double AutoThresholdCondition = 50.0;

        /// <summary>Lower edge of the full-output condition band (61..100) (REQ-MAINT-05).</summary>
        public const double FullBandMin = 61.0;

        /// <summary>Length of the 30-day balance window in game hours: 30 * 24 (requirements §5.5).</summary>
        public const double ThirtyDayHours = 30.0 * 24.0;

        /// <summary>Fallback maintenance trigger threshold used before a spec is bound (REQ-MAINT-05).</summary>
        public const float DefaultMaintenanceThreshold = AutoThreshold;

        /// <summary>Fallback maintenance interval in running days used before a spec is bound.</summary>
        public const float DefaultMaintenanceIntervalDays = 8.0f;

        /// <summary>Fallback maintenance downtime in game hours used before a spec is bound.</summary>
        public const float DefaultMaintenanceDurationHours = MaintenanceDurationHours;

        /// <summary>Mod-relative blueprint folder for the 24 boiler blueprints (D-9).</summary>
        public const string BlueprintFolder = "Buildings/Power/SteamBoiler";

        /// <summary>
        /// Dedicated bottom-bar tool group of the boilers (the collapsible "Steam Engines" building
        /// menu; data/BlockObjectToolGroups/BlockObjectToolGroup.SteamEngines.blueprint.json). Seat it
        /// between the vanilla Power group (order 70) and Science group (order 80).
        /// </summary>
        public const string ToolGroupId = "SteamEngines";

        /// <summary>First tool-group order of the boiler family; the 12 base entries use 110..121.</summary>
        public const int ToolOrderStart = 110;

        /// <summary>Root localization namespace (REQ-UI-05).</summary>
        public const string LocNamespace = "SEE";
    }
}
