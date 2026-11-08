using System;
using System.Collections.Generic;

namespace SEE.Core.Config
{
    /// <summary>
    /// Resolved <c>maintenance.*</c> configuration (requirements §9.2/§20.2/§20.6). The per-boiler
    /// interval and materials additionally come from the blueprint maintenance spec; this group
    /// holds the session-level switches and defaults.
    /// </summary>
    public sealed class MaintenanceConfig
    {
        internal MaintenanceConfig(
            bool enabled,
            IReadOnlyList<float> intervalDays,
            float loadFactorBase,
            float autoThreshold,
            float manualWarnAbove,
            float durationHours,
            bool manualMaterialsEnabled)
        {
            Enabled = enabled;
            IntervalDays = intervalDays ?? throw new ArgumentNullException(nameof(intervalDays));
            LoadFactorBase = loadFactorBase;
            AutoThreshold = autoThreshold;
            ManualWarnAbove = manualWarnAbove;
            DurationHours = durationHours;
            ManualMaterialsEnabled = manualMaterialsEnabled;
        }

        /// <summary>Master switch; false freezes condition at 100 and hides maintenance UI (REQ-MAINT-13).</summary>
        public bool Enabled { get; }

        /// <summary>Maintenance interval in running days, indexed by tier (6/8/10/12).</summary>
        public IReadOnlyList<float> IntervalDays { get; }

        /// <summary>Load coefficient floor (0.4, range 0.2..0.8).</summary>
        public float LoadFactorBase { get; }

        /// <summary>Condition at or below which automatic maintenance triggers (50, range 20..80).</summary>
        public float AutoThreshold { get; }

        /// <summary>Condition above which manual maintenance only shows a "still healthy" hint (90).</summary>
        public float ManualWarnAbove { get; }

        /// <summary>Maintenance downtime in game hours (2, range 1..8).</summary>
        public float DurationHours { get; }

        /// <summary>Whether manual maintenance consumes materials (debug/newbie switch).</summary>
        public bool ManualMaterialsEnabled { get; }
    }
}
