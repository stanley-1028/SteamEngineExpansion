namespace SEE.UI.Panels
{
    /// <summary>
    /// Pure refresh gate of the entity-panel fragment (requirements §15.7, REQ-OPS-01/04).
    ///
    /// The panel system calls <c>UpdateFragment</c> every frame. This policy turns that into "at
    /// most <c>perf.uiRefreshHz</c> snapshot reads per second" and gives the offline test runner a
    /// deterministic hook for both acceptance points:
    /// <list type="bullet">
    /// <item>4 Hz throttle: <see cref="IsDue"/> is false until the interval elapsed;</item>
    /// <item>closed panel: <see cref="ShouldQuery"/> is false immediately, so a cleared fragment
    /// performs zero queries and zero allocations (REQ-OPS-04).</item>
    /// </list>
    /// </summary>
    public static class BoilerPanelRefreshPolicy
    {
        /// <summary>Lower bound of <c>perf.uiRefreshHz</c> (§7.7).</summary>
        public const float MinUiRefreshHz = 1f;

        /// <summary>Upper bound of <c>perf.uiRefreshHz</c> (§7.7).</summary>
        public const float MaxUiRefreshHz = 10f;

        /// <summary>Shipped default of <c>perf.uiRefreshHz</c>.</summary>
        public const float DefaultUiRefreshHz = 4f;

        /// <summary>Seconds between two snapshot reads for the given cap (250 ms at 4 Hz).</summary>
        /// <param name="uiRefreshHz">Configured cap; non-positive/non-finite falls back to 4 Hz.</param>
        /// <returns>Interval in seconds, between 100 ms and 1 s.</returns>
        public static float IntervalSeconds(float uiRefreshHz)
        {
            float hz = uiRefreshHz;
            if (float.IsNaN(hz) || float.IsInfinity(hz) || hz <= 0f)
            {
                hz = DefaultUiRefreshHz;
            }

            if (hz < MinUiRefreshHz)
            {
                hz = MinUiRefreshHz;
            }
            else if (hz > MaxUiRefreshHz)
            {
                hz = MaxUiRefreshHz;
            }

            return 1f / hz;
        }

        /// <summary>
        /// True only while the panel is open on a selected entity. A cleared fragment (closed
        /// panel) must never query, format or allocate (REQ-OPS-04).
        /// </summary>
        /// <param name="panelOpen">Whether a boiler is currently shown.</param>
        /// <param name="hasEntity">Whether the fragment still holds an entity id.</param>
        /// <returns>True when the fragment may do work.</returns>
        public static bool ShouldQuery(bool panelOpen, bool hasEntity) => panelOpen && hasEntity;

        /// <summary>True when the next refresh window opened.</summary>
        /// <param name="now">Current unscaled time, in seconds.</param>
        /// <param name="nextAllowedTime">Time of the next allowed read.</param>
        /// <returns>True when the panel may re-read the view.</returns>
        public static bool IsDue(float now, float nextAllowedTime) => now >= nextAllowedTime;

        /// <summary>Time of the next allowed read after a read that started at <paramref name="now"/>.</summary>
        /// <param name="now">Current unscaled time, in seconds.</param>
        /// <param name="intervalSeconds">Interval from <see cref="IntervalSeconds"/>.</param>
        /// <returns>Next allowed read time.</returns>
        public static float NextAllowedTime(float now, float intervalSeconds) => now + intervalSeconds;
    }
}
