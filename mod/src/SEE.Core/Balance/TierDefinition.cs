using SEE.Core.Abstractions;

namespace SEE.Core.Balance
{
    /// <summary>
    /// One row of the authoritative 12-entry boiler family (requirements §4.1, SHARED_BRIEF §3).
    ///
    /// The values are injected by the caller (generated data file or test fixture) and are never
    /// hard-coded in this assembly: that keeps balance tuning a data change (REQ-AFR-09 /
    /// CONTRACT §10 rule 6). <see cref="TierCatalog"/> owns the completeness and range checks.
    /// </summary>
    public sealed class TierDefinition
    {
        /// <summary>Footprint class of the blueprint (Small/Medium/Large).</summary>
        public BoilerSize Size { get; init; }

        /// <summary>Technology tier of the blueprint (T1..T4).</summary>
        public BoilerTier Tier { get; init; }

        /// <summary>Base horsepower before the faction output coefficient (requirements §4.1).</summary>
        public int BaseHorsepower { get; init; }

        /// <summary>Base fuel rate per game hour before the faction fuel coefficient.</summary>
        public float BaseFuelPerHour { get; init; }

        /// <summary>Water rate per game hour at full load (identical for both factions).</summary>
        public float WaterPerHour { get; init; }

        /// <summary>Declared fuel inventory capacity in goods (authoritative value, AD-3).</summary>
        public int FuelStockGoods { get; init; }

        /// <summary>Declared water tank capacity in goods.</summary>
        public int WaterTankGoods { get; init; }

        /// <summary>Maintenance interval in running days (6/8/10/12 by tier).</summary>
        public float MaintenanceIntervalDays { get; init; }

        /// <summary>
        /// Frozen TemplateName of this definition for one faction (CONTRACT_SEE §2.7):
        /// <c>SteamBoiler.&lt;Size&gt;.&lt;Tier&gt;.&lt;Faction&gt;</c>.
        /// </summary>
        /// <param name="faction">Faction variant of the blueprint.</param>
        /// <returns>The frozen template name, for example <c>SteamBoiler.Medium.T2.Folktails</c>.</returns>
        public string TemplateName(BoilerFaction faction)
        {
            return "SteamBoiler." + Size + "." + Tier + "." + faction;
        }
    }
}
