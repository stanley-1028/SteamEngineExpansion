namespace SEE.UI.Localization
{
    /// <summary>
    /// Base-game localization keys the boiler panel reuses instead of shipping duplicate text.
    ///
    /// Keys that belong to SEE itself live in <see cref="SEE.Core.Constants.SeeLocKeys"/> and are
    /// checked against the shipped CSVs by the data validator. The keys below are not ours: they
    /// are baked into the installed game (grep-verified against the shipped localization tables of
    /// Timberborn 1.1.2.8), so using them cannot show a raw key to the player (REQ-UI-05).
    /// </summary>
    public static class SeeUiVanillaKeys
    {
        /// <summary>Prefix of a good's display-name key: <c>"Good." + goodId + ".DisplayName"</c>.</summary>
        public const string GoodDisplayNamePrefix = "Good.";

        /// <summary>Suffix of a good's display-name key.</summary>
        public const string GoodDisplayNameSuffix = ".DisplayName";

        /// <summary>
        /// Full sentence telling the player that a building needs mechanical power: reused as the
        /// "next step" hint of the NoFuel/Idle rows (REQ-UI-02).
        /// </summary>
        public const string HintNeedsPower = "Status.Mechanical.NoPower";

        /// <summary>
        /// "Power input / maximum: {0} / {1} ({2}%)". Used as the tooltip of the network row so the
        /// delivered hp stays visible next to the network totals.
        /// </summary>
        public const string PowerDetail = "Mechanical.PowerInputMaximum";

        /// <summary>
        /// "{0} hp" / "{0} 匹馬力": the game's horsepower unit, so panel numbers follow the active
        /// language exactly like the vanilla mechanical-node formatter.
        /// </summary>
        public const string PowerUnit = "Unit.HorsePower.NumberAndUnit";
    }
}