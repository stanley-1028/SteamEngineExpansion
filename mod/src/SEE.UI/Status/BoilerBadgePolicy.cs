using SEE.Core.Abstractions;

namespace SEE.UI.Status
{
    /// <summary>
    /// Badge policy shared by the panel badge and the (future) world-space badge
    /// (requirements §15.3, §15.7, REQ-OPS-03, REQ-UI-03). It contains no engine types: the
    /// refresh interval, visibility limit and idle suppression are ordinary values, so the
    /// offline test runner can prove the 1 Hz badge budget and the <c>showIdleBadge=false</c>
    /// default without launching the game.
    /// </summary>
    public static class BoilerBadgePolicy
    {
        /// <summary>Lower bound of the configured world-badge refresh rate, in Hz (§7.7).</summary>
        public const float MinBadgeRefreshHz = 0.5f;

        /// <summary>Upper bound of the configured world-badge refresh rate, in Hz (§7.7).</summary>
        public const float MaxBadgeRefreshHz = 2f;

        /// <summary>Lower bound of <c>perf.maxBadgesVisible</c> (§7.7).</summary>
        public const int MinBadgesVisible = 16;

        /// <summary>Upper bound of <c>perf.maxBadgesVisible</c> (§7.7).</summary>
        public const int MaxBadgesVisible = 256;

        /// <summary>Shipped default of <c>perf.badgeRefreshHz</c>.</summary>
        public const float DefaultBadgeRefreshHz = 1f;

        /// <summary>Shipped default of <c>perf.maxBadgesVisible</c>.</summary>
        public const int DefaultMaxBadgesVisible = 64;

        /// <summary>
        /// True when this status renders a badge at all. <see cref="BoilerStatus.Running"/> never
        /// shows one (requirements §15.3 row 9); <see cref="BoilerStatus.Idle"/> only when the
        /// player opted in with <c>ui.showIdleBadge</c> (default false, REQ-UI-08 noise budget).
        /// </summary>
        /// <param name="status">Highest-priority state.</param>
        /// <param name="showIdleBadge">Configured <c>ui.showIdleBadge</c>.</param>
        /// <returns>True when the status has a visible badge.</returns>
        public static bool IsVisible(BoilerStatus status, bool showIdleBadge)
        {
            if (status == BoilerStatus.Running)
            {
                return false;
            }

            if (status == BoilerStatus.Idle)
            {
                return showIdleBadge;
            }

            return true;
        }

        /// <summary>Localization key of the badge; empty when the status shows no badge.</summary>
        /// <param name="status">Highest-priority state.</param>
        /// <param name="showIdleBadge">Configured <c>ui.showIdleBadge</c>.</param>
        /// <returns>A frozen <c>Status.SEE.*</c> key, or an empty string.</returns>
        public static string LocKey(BoilerStatus status, bool showIdleBadge) =>
            IsVisible(status, showIdleBadge) ? StatusDescriptors.StatusLocKey(status) : string.Empty;

        /// <summary>Display priority of a badge; lower value wins (declaration order of the state enum).</summary>
        /// <param name="status">Highest-priority state.</param>
        /// <returns>0 (Paused) through 8 (Idle).</returns>
        public static int Priority(BoilerStatus status) => StatusDescriptors.BadgePriority(status);

        /// <summary>Refresh interval of the world-badge pass, clamped to the config range.</summary>
        /// <param name="badgeRefreshHz">Configured <c>perf.badgeRefreshHz</c>.</param>
        /// <returns>Seconds between two badge passes (1 s at the default 1 Hz).</returns>
        public static float RefreshIntervalSeconds(float badgeRefreshHz)
        {
            float hz = badgeRefreshHz;
            if (float.IsNaN(hz) || float.IsInfinity(hz) || hz <= 0f)
            {
                hz = DefaultBadgeRefreshHz;
            }

            if (hz < MinBadgeRefreshHz)
            {
                hz = MinBadgeRefreshHz;
            }
            else if (hz > MaxBadgeRefreshHz)
            {
                hz = MaxBadgeRefreshHz;
            }

            return 1f / hz;
        }

        /// <summary>Clamped <c>perf.maxBadgesVisible</c>: 64 by default, 16..256.</summary>
        /// <param name="configured">Raw configured value.</param>
        /// <returns>The number of badges that may be visible at once.</returns>
        public static int VisibleLimit(int configured)
        {
            if (configured < MinBadgesVisible)
            {
                return configured <= 0 ? DefaultMaxBadgesVisible : MinBadgesVisible;
            }

            return configured > MaxBadgesVisible ? MaxBadgesVisible : configured;
        }

        /// <summary>True when a badge pass is due at <paramref name="now"/>.</summary>
        /// <param name="now">Current unscaled time, in seconds.</param>
        /// <param name="nextPassTime">Time the next pass was scheduled for.</param>
        /// <returns>True when the pass may run.</returns>
        public static bool IsDue(float now, float nextPassTime) => now >= nextPassTime;

        /// <summary>Next scheduled badge pass time after a pass that started at <paramref name="now"/>.</summary>
        /// <param name="now">Current unscaled time, in seconds.</param>
        /// <param name="intervalSeconds">Interval from <see cref="RefreshIntervalSeconds"/>.</param>
        /// <returns>Time of the next allowed pass.</returns>
        public static float NextPassTime(float now, float intervalSeconds) => now + intervalSeconds;
    }
}
