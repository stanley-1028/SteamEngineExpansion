using System;
using System.Collections.Immutable;
using SEE.Core.Abstractions;
using Timberborn.BlueprintSystem;

namespace SEE.Gameplay
{
    /// <summary>
    /// The custom blueprint spec of the steam boiler (requirements §13.3, REQ-IMPL-07). One spec
    /// serves all 12 sizes/tiers and both factions: every per-blueprint difference is data, so
    /// balancing never changes code (REQ-NFR-04).
    ///
    /// The blueprint JSON key is <c>"SEE.SEEBoilerSpec"</c> and comes from the
    /// <see cref="SpecAliasAttribute"/>: the game indexes a spec type by each alias string and by
    /// the CLR type name only, never by namespace-qualified name (verified in game IL). Without
    /// the alias the blueprint silently has no boiler spec and the building fails to instantiate.
    /// </summary>
    [SpecAlias("SEE.SEEBoilerSpec")]
    public record SEEBoilerSpec : ComponentSpec
    {
        /// <summary>Tier token: T1..T4 (default T2, CONTRACT_SEE §3.2).</summary>
        [Serialize]
        public string Tier { get; set; } = "T2";

        /// <summary>Size token: Small/Medium/Large (default Medium, CONTRACT_SEE §3.2).</summary>
        [Serialize]
        public string Size { get; set; } = "Medium";

        /// <summary>Faction token: Folktails/IronTeeth (default Folktails); display/diagnostics only.</summary>
        [Serialize]
        public string Faction { get; set; } = "Folktails";

        /// <summary>Rated horsepower, already multiplied by the faction output coefficient (D-3).</summary>
        [Serialize]
        public int Horsepower { get; set; } = 460;

        /// <summary>Rated fuel items per game hour at full load (already faction-adjusted, 2 decimals).</summary>
        [Serialize]
        public float FuelPerHour { get; set; } = 0.11f;

        /// <summary>Rated water items per game hour at full load (no faction multiplier).</summary>
        [Serialize]
        public float WaterPerHour { get; set; } = 0.32f;

        /// <summary>Fuel inventory capacity in whole goods (table value, both factions identical).</summary>
        [Serialize]
        public int FuelCapacityGoods { get; set; } = 3;

        /// <summary>Water tank capacity in whole goods (table value, both factions identical).</summary>
        [Serialize]
        public int WaterCapacityGoods { get; set; } = 64;

        /// <summary>Fuel good ids this boiler accepts, for example ["Log"] or ["Log", "Plank"].</summary>
        [Serialize]
        public ImmutableArray<string> FuelGoodIds { get; set; } = ImmutableArray<string>.Empty;

        /// <summary>Energy value per fuel good, index-aligned with <see cref="FuelGoodIds"/> (Plank = 2 Log).</summary>
        [Serialize]
        public ImmutableArray<float> FuelValues { get; set; } = ImmutableArray<float>.Empty;

        /// <summary>Fuel model token: "proportional" (default) or "vanilla".</summary>
        [Serialize]
        public string FuelScaling { get; set; } = "proportional";

        /// <summary>Automatic-maintenance default of a freshly built boiler.</summary>
        [Serialize]
        public bool AutoMaintenanceDefault { get; set; } = true;

        /// <summary>Resolves <see cref="Tier"/> to the Core enum; unknown tokens fall back to T1.</summary>
        /// <returns>A valid tier id.</returns>
        public BoilerTier ResolveTier()
        {
            return Enum.TryParse(Tier, true, out BoilerTier tier) && IsDefined(tier) ? tier : BoilerTier.T1;
        }

        /// <summary>Resolves <see cref="Size"/> to the Core enum; unknown tokens fall back to Small.</summary>
        /// <returns>A valid size id.</returns>
        public BoilerSize ResolveSize()
        {
            return Enum.TryParse(Size, true, out BoilerSize value) && IsDefined(value) ? value : BoilerSize.Small;
        }

        /// <summary>Resolves <see cref="Faction"/> to the Core enum; unknown tokens fall back to Folktails.</summary>
        /// <returns>A valid faction id.</returns>
        public BoilerFaction ResolveFaction()
        {
            return Enum.TryParse(Faction, true, out BoilerFaction value) && IsDefined(value)
                ? value
                : BoilerFaction.Folktails;
        }

        /// <summary>Returns the energy value of one fuel good (1.0 when the array is missing a value).</summary>
        /// <param name="goodId">Vanilla good id.</param>
        /// <returns>Positive energy value used to convert goods into fuel energy.</returns>
        public float FuelValueFor(string goodId)
        {
            if (string.IsNullOrEmpty(goodId) || FuelGoodIds.IsDefaultOrEmpty)
            {
                return 1.0f;
            }

            for (int i = 0; i < FuelGoodIds.Length; i++)
            {
                if (!string.Equals(FuelGoodIds[i], goodId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (i < FuelValues.Length && FuelValues[i] > 0f && !float.IsNaN(FuelValues[i]))
                {
                    return FuelValues[i];
                }

                return 1.0f;
            }

            return 1.0f;
        }

        /// <summary>True when this blueprint accepts the given fuel good.</summary>
        /// <param name="goodId">Vanilla good id to test.</param>
        /// <returns>True when the good appears in <see cref="FuelGoodIds"/>.</returns>
        public bool IsFuelAllowed(string goodId)
        {
            if (string.IsNullOrEmpty(goodId))
            {
                return false;
            }

            if (FuelGoodIds.IsDefaultOrEmpty)
            {
                // Missing data means "no restriction declared"; the vanilla good consumer enforces
                // the real whitelist, so the controller must not turn a data gap into a dead boiler.
                return true;
            }

            for (int i = 0; i < FuelGoodIds.Length; i++)
            {
                if (string.Equals(FuelGoodIds[i], goodId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDefined(BoilerTier tier)
        {
            return tier == BoilerTier.T1 || tier == BoilerTier.T2 || tier == BoilerTier.T3 || tier == BoilerTier.T4;
        }

        private static bool IsDefined(BoilerSize size)
        {
            return size == BoilerSize.Small || size == BoilerSize.Medium || size == BoilerSize.Large;
        }

        private static bool IsDefined(BoilerFaction faction)
        {
            return faction == BoilerFaction.Folktails || faction == BoilerFaction.IronTeeth;
        }
    }
}
