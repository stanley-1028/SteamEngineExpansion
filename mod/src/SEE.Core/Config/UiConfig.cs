namespace SEE.Core.Config
{
    /// <summary>
    /// Resolved <c>ui.*</c> configuration (requirements §9.4/§15.7): low-supply warning threshold
    /// and whether the idle badge is shown.
    /// </summary>
    public sealed class UiConfig
    {
        internal UiConfig(float lowSupplyHours, bool showIdleBadge)
        {
            LowSupplyHours = lowSupplyHours;
            ShowIdleBadge = showIdleBadge;
        }

        /// <summary>Low fuel/water warning threshold in game hours (8, range 2..24).</summary>
        public float LowSupplyHours { get; }

        /// <summary>Whether an idle boiler shows a world badge (false by default, reduces noise).</summary>
        public bool ShowIdleBadge { get; }
    }
}
