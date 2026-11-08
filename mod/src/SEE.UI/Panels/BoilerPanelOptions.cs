using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Config;
using SEE.Core.Constants;

namespace SEE.UI.Panels
{
    /// <summary>
    /// The resolved panel configuration the pure model builder needs (requirements §7.7, §15.7).
    /// It is a plain value object so the offline tests can build one without a config file, while
    /// the game-side composition root fills it from <see cref="SeeConfig"/>.
    ///
    /// P4B froze the beaver-equivalent key (<see cref="SeeLocKeys.UiPanelWaterBeaverEquivalent"/>):
    /// the default now shows the line, and the parameter stays available for tests/tuning.
    /// </summary>
    public sealed class BoilerPanelOptions
    {
        /// <summary>Water a single beaver drinks per game day (requirements §3.3/§7.3; Core constant).</summary>
        public const float DefaultBeaverWaterPerDay = SEEConst.BeaverWaterPerDay;

        /// <summary>Shipped default of <c>perf.uiRefreshHz</c>.</summary>
        public const float DefaultUiRefreshHz = 4f;

        /// <summary>Shipped default of <c>ui.lowSupplyHours</c>.</summary>
        public const float DefaultLowSupplyHours = 8f;

        /// <summary>Shipped default of <c>maintenance.manualWarnAbove</c>.</summary>
        public const float DefaultManualWarnAbove = 90f;

        /// <summary>Shipped default of <c>maintenance.autoThreshold</c>.</summary>
        public const float DefaultAutoThreshold = 50f;

        private readonly float[] _intervalDays;

        /// <summary>Builds options; every value is clamped to the contract range.</summary>
        /// <param name="uiRefreshHz">Panel refresh cap, 1..10 Hz.</param>
        /// <param name="lowSupplyHours">Low-fuel / low-water warning threshold, 2..24 h.</param>
        /// <param name="showIdleBadge">Whether an idle boiler shows a world badge (default false).</param>
        /// <param name="maintenanceEnabled">Global maintenance switch (REQ-MAINT-17).</param>
        /// <param name="manualWarnAbove">Condition above which a manual maintenance warns (C-5).</param>
        /// <param name="autoThreshold">Automatic maintenance trigger condition.</param>
        /// <param name="badgeRefreshHz">World-badge refresh cap, 0.5..2 Hz.</param>
        /// <param name="maxBadgesVisible">Maximum visible world badges, 16..256.</param>
        /// <param name="beaverWaterPerDay">Display-only water conversion, one beaver per game day.</param>
        /// <param name="intervalDaysByTier">Maintenance interval of T1..T4 in running days.</param>
        /// <param name="waterBeaverEquivalentLocKey">Frozen key for the beaver-equivalent row (P4B).</param>
        public BoilerPanelOptions(
            float uiRefreshHz = DefaultUiRefreshHz,
            float lowSupplyHours = DefaultLowSupplyHours,
            bool showIdleBadge = false,
            bool maintenanceEnabled = true,
            float manualWarnAbove = DefaultManualWarnAbove,
            float autoThreshold = DefaultAutoThreshold,
            float badgeRefreshHz = Status.BoilerBadgePolicy.DefaultBadgeRefreshHz,
            int maxBadgesVisible = Status.BoilerBadgePolicy.DefaultMaxBadgesVisible,
            float beaverWaterPerDay = DefaultBeaverWaterPerDay,
            IReadOnlyList<float>? intervalDaysByTier = null,
            string waterBeaverEquivalentLocKey = SeeLocKeys.UiPanelWaterBeaverEquivalent)
        {
            UiRefreshHz = ClampFloat(uiRefreshHz, 1f, 10f, DefaultUiRefreshHz);
            LowSupplyHours = ClampFloat(lowSupplyHours, 2f, 24f, DefaultLowSupplyHours);
            ShowIdleBadge = showIdleBadge;
            MaintenanceEnabled = maintenanceEnabled;
            ManualWarnAbove = ClampFloat(manualWarnAbove, 50f, 100f, DefaultManualWarnAbove);
            AutoThreshold = ClampFloat(autoThreshold, 20f, 80f, DefaultAutoThreshold);
            BadgeRefreshHz = ClampFloat(
                badgeRefreshHz,
                Status.BoilerBadgePolicy.MinBadgeRefreshHz,
                Status.BoilerBadgePolicy.MaxBadgeRefreshHz,
                Status.BoilerBadgePolicy.DefaultBadgeRefreshHz);
            MaxBadgesVisible = maxBadgesVisible <= 0
                ? Status.BoilerBadgePolicy.DefaultMaxBadgesVisible
                : Status.BoilerBadgePolicy.VisibleLimit(maxBadgesVisible);
            BeaverWaterPerDay = ClampFloat(beaverWaterPerDay, 0.01f, 100f, DefaultBeaverWaterPerDay);
            WaterBeaverEquivalentLocKey = waterBeaverEquivalentLocKey ?? string.Empty;

            _intervalDays = new float[4];
            for (int i = 0; i < _intervalDays.Length; i++)
            {
                float fallback = DefaultIntervalDays(i);
                _intervalDays[i] = intervalDaysByTier != null && i < intervalDaysByTier.Count
                    ? ClampFloat(intervalDaysByTier[i], 4f, 20f, fallback)
                    : fallback;
            }
        }

        /// <summary>Panel refresh cap in Hz (1..10).</summary>
        public float UiRefreshHz { get; }

        /// <summary>Low-supply warning threshold in game hours (2..24).</summary>
        public float LowSupplyHours { get; }

        /// <summary>Whether an idle boiler shows a badge (default false).</summary>
        public bool ShowIdleBadge { get; }

        /// <summary>Global maintenance switch; when false the whole maintenance block is hidden.</summary>
        public bool MaintenanceEnabled { get; }

        /// <summary>Condition above which a manual maintenance warns about low benefit.</summary>
        public float ManualWarnAbove { get; }

        /// <summary>Condition at or below which automatic maintenance triggers.</summary>
        public float AutoThreshold { get; }

        /// <summary>World-badge refresh cap in Hz (0.5..2).</summary>
        public float BadgeRefreshHz { get; }

        /// <summary>Maximum visible world badges (16..256).</summary>
        public int MaxBadgesVisible { get; }

        /// <summary>Display-only water conversion: one beaver drinks this many water per day.</summary>
        public float BeaverWaterPerDay { get; }

        /// <summary>Frozen loc key of the beaver-equivalent line (P4B); empty hides the row in tests.</summary>
        public string WaterBeaverEquivalentLocKey { get; }

        /// <summary>Shipped defaults (mirror <c>SEE_Config.json</c>).</summary>
        public static BoilerPanelOptions Default { get; } = new BoilerPanelOptions();

        /// <summary>Maintenance interval of one tier in running days.</summary>
        /// <param name="tier">Boiler tier.</param>
        /// <returns>Interval in running days (4..20).</returns>
        public float IntervalDaysFor(BoilerTier tier)
        {
            int index = (int)tier;
            return index < 0 || index > 3 ? _intervalDays[0] : _intervalDays[index];
        }

        /// <summary>Projects the validated config onto the panel's needs.</summary>
        /// <param name="config">Resolved config; null yields <see cref="Default"/>.</param>
        /// <returns>Panel options matching the config.</returns>
        public static BoilerPanelOptions FromConfig(SeeConfig? config)
        {
            if (config == null)
            {
                return Default;
            }

            var intervals = new float[4];
            for (int i = 0; i < intervals.Length; i++)
            {
                intervals[i] = config.IntervalDaysFor((BoilerTier)i);
            }

            return new BoilerPanelOptions(
                uiRefreshHz: config.Perf.UiRefreshHz,
                lowSupplyHours: config.Ui.LowSupplyHours,
                showIdleBadge: config.Ui.ShowIdleBadge,
                maintenanceEnabled: config.Maintenance.Enabled,
                manualWarnAbove: config.Maintenance.ManualWarnAbove,
                autoThreshold: config.Maintenance.AutoThreshold,
                badgeRefreshHz: config.Perf.BadgeRefreshHz,
                maxBadgesVisible: config.Perf.MaxBadgesVisible,
                intervalDaysByTier: intervals);
        }

        private static float DefaultIntervalDays(int tierIndex)
        {
            switch (tierIndex)
            {
                case 0:
                    return 6f;
                case 1:
                    return 8f;
                case 2:
                    return 10f;
                default:
                    return 12f;
            }
        }

        private static float ClampFloat(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return fallback;
            }

            return value < min ? min : (value > max ? max : value);
        }
    }
}
