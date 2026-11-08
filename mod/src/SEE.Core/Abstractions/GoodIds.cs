namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Vanilla good ids the mod reasons about (requirements §9.4/§13.4). No new good is ever
    /// introduced: a typo in these strings silently empties every maintenance/fuel check, so they
    /// are constants in Core rather than literals in gameplay code.
    /// </summary>
    public static class GoodIds
    {
        /// <summary>Vanilla Log.</summary>
        public const string Log = "Log";

        /// <summary>Vanilla Plank.</summary>
        public const string Plank = "Plank";

        /// <summary>Vanilla Gear.</summary>
        public const string Gear = "Gear";

        /// <summary>Vanilla MetalBlock.</summary>
        public const string MetalBlock = "MetalBlock";

        /// <summary>Vanilla Water.</summary>
        public const string Water = "Water";

        /// <summary>Vanilla liquid whitelist token used by <c>StockpileSpec</c>.</summary>
        public const string LiquidGoodType = "Liquid";
    }
}
