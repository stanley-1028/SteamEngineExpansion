using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Constants;

namespace SEE.Core.Maintenance
{
    /// <summary>
    /// Immutable maintenance parameters of one boiler, resolved from the gameplay maintenance spec
    /// and the config (requirements §9.4/§9.5, REQ-NFR-04). The ledger clamps every value again
    /// defensively, so a corrupt blueprint or config cannot create negative wear.
    /// </summary>
    public readonly struct MaintenanceRules
    {
        /// <summary>Creates the rules of one boiler.</summary>
        /// <param name="enabled">False disables the whole maintenance system for this boiler.</param>
        /// <param name="auto">False requires a manual request; the per-boiler flag still applies.</param>
        /// <param name="autoThreshold">Condition at or below which automatic maintenance becomes due.</param>
        /// <param name="durationHours">Downtime of one maintenance cycle, in game hours.</param>
        /// <param name="intervalDays">Days of running time a full condition lasts (6/8/10/12).</param>
        /// <param name="materials">Materials consumed per cycle; null is treated as empty.</param>
        /// <param name="manualWarnAbove">Condition above which a manual request only shows a hint (C-5).</param>
        public MaintenanceRules(
            bool enabled,
            bool auto,
            float autoThreshold,
            float durationHours,
            float intervalDays,
            IReadOnlyList<MaintenanceMaterial> materials,
            float manualWarnAbove = SEEConst.ManualWarnAbove)
        {
            Enabled = enabled;
            Auto = auto;
            AutoThreshold = autoThreshold;
            DurationHours = durationHours;
            IntervalDays = intervalDays;
            Materials = materials ?? Array.Empty<MaintenanceMaterial>();
            ManualWarnAbove = manualWarnAbove;
        }

        /// <summary>Shipped fallback rules used before the spec is bound (safe, no materials).</summary>
        public static MaintenanceRules Default { get; } = new MaintenanceRules(
            enabled: true,
            auto: true,
            autoThreshold: SEEConst.DefaultMaintenanceThreshold,
            durationHours: SEEConst.DefaultMaintenanceDurationHours,
            intervalDays: SEEConst.DefaultMaintenanceIntervalDays,
            materials: Array.Empty<MaintenanceMaterial>(),
            manualWarnAbove: SEEConst.ManualWarnAbove);

        /// <summary>False disables maintenance entirely for this boiler.</summary>
        public bool Enabled { get; }

        /// <summary>Whether automatic maintenance may trigger at all.</summary>
        public bool Auto { get; }

        /// <summary>Condition at or below which automatic maintenance becomes due, 0..100.</summary>
        public float AutoThreshold { get; }

        /// <summary>Condition above which a manual request only shows a low-benefit hint; never disables it.</summary>
        public float ManualWarnAbove { get; }

        /// <summary>Downtime of one maintenance cycle, in game hours.</summary>
        public float DurationHours { get; }

        /// <summary>Days of running time a full condition lasts.</summary>
        public float IntervalDays { get; }

        /// <summary>Materials consumed per maintenance cycle.</summary>
        public IReadOnlyList<MaintenanceMaterial> Materials { get; }
    }
}
