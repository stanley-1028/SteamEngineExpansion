namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Faction whose material set and multipliers apply to a boiler (requirements §4.4/§16.3).
    /// One blueprint belongs to exactly one faction because model materials are faction-scoped
    /// (trap R-A).
    /// </summary>
    public enum BoilerFaction
    {
        /// <summary>Folktails: wooden shell, +0% output, -10% fuel.</summary>
        Folktails = 0,

        /// <summary>Iron Teeth: cast-iron boiler, +5% output, nominal fuel.</summary>
        IronTeeth = 1
    }
}
