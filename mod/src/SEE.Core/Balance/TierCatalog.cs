using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;

namespace SEE.Core.Balance
{
    /// <summary>
    /// Typed view of the 12 boiler definitions (3 sizes × 4 tiers; requirements §4.1, REQ-CORE-04).
    ///
    /// The catalog is constructed from injected <see cref="TierDefinition"/> rows so no balance
    /// value lives in code (CONTRACT §10 rule 6). The constructor validates the family shape — all
    /// 12 (Size, Tier) combinations exactly once and only finite, positive values — so a broken
    /// data file or fixture fails loudly at build/test time instead of producing a half family.
    /// </summary>
    public sealed class TierCatalog
    {
        /// <summary>Number of definitions a complete family must contain: 3 sizes × 4 tiers.</summary>
        public const int ExpectedCount = 12;

        private static readonly BoilerSize[] Sizes = { BoilerSize.Small, BoilerSize.Medium, BoilerSize.Large };
        private static readonly BoilerTier[] Tiers = { BoilerTier.T1, BoilerTier.T2, BoilerTier.T3, BoilerTier.T4 };

        private readonly List<TierDefinition> _definitions;

        /// <summary>Builds a catalog from the injected rows, validating the complete 12-entry family.</summary>
        /// <param name="definitions">The 12 definitions; null or incomplete input is rejected.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="definitions"/> is null.</exception>
        /// <exception cref="ArgumentException">When the family is incomplete, duplicated or invalid.</exception>
        public TierCatalog(IEnumerable<TierDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _definitions = new List<TierDefinition>(ExpectedCount);
            foreach (TierDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("the catalog contains a null definition", nameof(definitions));
                }

                Validate(definition);
                _definitions.Add(definition);
            }

            ValidateFamily(_definitions);
        }

        /// <summary>The definitions in injection order (3 sizes × 4 tiers).</summary>
        public IReadOnlyList<TierDefinition> Definitions => _definitions;

        /// <summary>Number of injected definitions; always <see cref="ExpectedCount"/> after construction.</summary>
        public int Count => _definitions.Count;

        /// <summary>Looks up one definition by size and tier.</summary>
        /// <param name="size">Footprint class.</param>
        /// <param name="tier">Technology tier.</param>
        /// <param name="definition">The matching definition when found.</param>
        /// <returns>True when the (Size, Tier) combination exists.</returns>
        public bool TryGet(BoilerSize size, BoilerTier tier, out TierDefinition definition)
        {
            for (int i = 0; i < _definitions.Count; i++)
            {
                TierDefinition candidate = _definitions[i];
                if (candidate.Size == size && candidate.Tier == tier)
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null!;
            return false;
        }

        /// <summary>Returns one definition by size and tier.</summary>
        /// <param name="size">Footprint class.</param>
        /// <param name="tier">Technology tier.</param>
        /// <returns>The matching definition.</returns>
        /// <exception cref="KeyNotFoundException">When the combination is not present.</exception>
        public TierDefinition Get(BoilerSize size, BoilerTier tier)
        {
            if (TryGet(size, tier, out TierDefinition definition))
            {
                return definition;
            }

            throw new KeyNotFoundException("no boiler definition for " + size + "/" + tier);
        }

        private static void Validate(TierDefinition definition)
        {
            if (!IsDefined(definition.Size) || !IsDefined(definition.Tier))
            {
                throw new ArgumentException("the definition uses an undefined size or tier");
            }

            if (definition.BaseHorsepower <= 0)
            {
                throw new ArgumentException("BaseHorsepower must be positive");
            }

            if (!IsFinitePositive(definition.BaseFuelPerHour))
            {
                throw new ArgumentException("BaseFuelPerHour must be finite and positive");
            }

            if (!IsFinitePositive(definition.WaterPerHour))
            {
                throw new ArgumentException("WaterPerHour must be finite and positive");
            }

            if (definition.FuelStockGoods < 0)
            {
                throw new ArgumentException("FuelStockGoods must not be negative");
            }

            if (definition.WaterTankGoods < 0)
            {
                throw new ArgumentException("WaterTankGoods must not be negative");
            }

            if (!IsFinitePositive(definition.MaintenanceIntervalDays))
            {
                throw new ArgumentException("MaintenanceIntervalDays must be finite and positive");
            }
        }

        private static void ValidateFamily(List<TierDefinition> definitions)
        {
            if (definitions.Count != ExpectedCount)
            {
                throw new ArgumentException(
                    "the catalog must contain exactly " + ExpectedCount + " definitions, got " + definitions.Count);
            }

            foreach (BoilerSize size in Sizes)
            {
                foreach (BoilerTier tier in Tiers)
                {
                    int matches = 0;
                    for (int i = 0; i < definitions.Count; i++)
                    {
                        if (definitions[i].Size == size && definitions[i].Tier == tier)
                        {
                            matches++;
                        }
                    }

                    if (matches != 1)
                    {
                        throw new ArgumentException(
                            "the catalog needs exactly one entry for " + size + "/" + tier + ", found " + matches);
                    }
                }
            }
        }

        private static bool IsDefined(BoilerSize size)
        {
            return size == BoilerSize.Small || size == BoilerSize.Medium || size == BoilerSize.Large;
        }

        private static bool IsDefined(BoilerTier tier)
        {
            return tier == BoilerTier.T1 || tier == BoilerTier.T2 || tier == BoilerTier.T3 || tier == BoilerTier.T4;
        }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
