using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SEE.Core.Abstractions;
using Timberborn.BlueprintSystem;
using Timberborn.Goods;

namespace SEE.Gameplay
{
    /// <summary>
    /// The custom maintenance spec of the steam boiler (requirements §13.3). The vanilla game has
    /// no maintenance system at all, so wear, downtime and material requirements are declared per
    /// blueprint here and enforced by SEE.
    ///
    /// The blueprint JSON key is <c>"SEE.SEEMaintenanceSpec"</c> (from <see cref="SpecAliasAttribute"/>;
    /// the alias is required, see <see cref="SEEBoilerSpec"/>).
    ///
    /// <see cref="Materials"/> deliberately uses the vanilla <see cref="GoodAmountSpec"/> array:
    /// the blueprint deserializer only recurses into <c>ImmutableArray&lt;GoodAmountSpec&gt;</c>
    /// element-wise; a custom struct array would go through primitive serialization and corrupt
    /// the <c>{Id, Amount}</c> objects (trap B5, same pattern as <c>BuildingSpec.BuildingCost</c>).
    /// The list is converted to the pure Core <see cref="MaintenanceMaterial"/> records lazily.
    /// </summary>
    [SpecAlias("SEE.SEEMaintenanceSpec")]
    public record SEEMaintenanceSpec : ComponentSpec
    {
        /// <summary>False disables the whole maintenance system for this boiler.</summary>
        [Serialize]
        public bool Enabled { get; set; } = true;

        /// <summary>Maintenance interval in running days: 6/8/10/12 by tier.</summary>
        [Serialize]
        public float IntervalDays { get; set; } = 8f;

        /// <summary>Load coefficient floor of the wear formula (0.4).</summary>
        [Serialize]
        public float LoadFloor { get; set; } = 0.4f;

        /// <summary>Extra coefficient per unit of load (0.6); floor + range = 1.0 at full load.</summary>
        [Serialize]
        public float LoadFactorRange { get; set; } = 0.6f;

        /// <summary>Condition at or below which automatic maintenance triggers (50).</summary>
        [Serialize]
        public float TriggerThreshold { get; set; } = 50f;

        /// <summary>Maintenance downtime in game hours (2).</summary>
        [Serialize]
        public float DurationHours { get; set; } = 2f;

        /// <summary>Materials consumed per maintenance cycle, for example [{Id:"Plank",Amount:12}].</summary>
        [Serialize]
        public ImmutableArray<GoodAmountSpec> Materials { get; set; } = ImmutableArray<GoodAmountSpec>.Empty;

        /// <summary>
        /// Converts the blueprint materials into validated Core records. Null/empty ids and
        /// non-positive amounts are skipped; the amount upper bound only defends against corrupt
        /// data (the shipped values are authored in <c>engines.json</c>).
        /// </summary>
        /// <returns>Non-null list of materials; empty when the spec declares none.</returns>
        public IReadOnlyList<MaintenanceMaterial> ResolveMaterials()
        {
            if (Materials.IsDefaultOrEmpty)
            {
                return Array.Empty<MaintenanceMaterial>();
            }

            var materials = new List<MaintenanceMaterial>(Materials.Length);
            for (int i = 0; i < Materials.Length; i++)
            {
                GoodAmountSpec? material = Materials[i];
                if (material == null || string.IsNullOrWhiteSpace(material.Id) || material.Amount <= 0)
                {
                    continue;
                }

                int amount = material.Amount > 10000 ? 10000 : material.Amount;
                materials.Add(new MaintenanceMaterial(material.Id.Trim(), amount));
            }

            return materials;
        }

        /// <summary>Returns the configured interval clamped to a usable positive value.</summary>
        /// <returns>Interval in running days; the Core default when the blueprint value is invalid.</returns>
        public float ResolveIntervalDays()
        {
            if (float.IsNaN(IntervalDays) || IntervalDays <= 0f)
            {
                return 8f;
            }

            return IntervalDays;
        }

        /// <summary>Returns the configured downtime clamped to a positive game-hour value.</summary>
        /// <returns>Downtime in hours; 2 when the blueprint value is invalid.</returns>
        public float ResolveDurationHours()
        {
            if (float.IsNaN(DurationHours) || DurationHours <= 0f)
            {
                return 2f;
            }

            return DurationHours;
        }

        /// <summary>Returns the configured trigger threshold clamped to 0..100.</summary>
        /// <returns>Condition threshold; 50 when the blueprint value is invalid.</returns>
        public float ResolveTriggerThreshold()
        {
            if (float.IsNaN(TriggerThreshold) || TriggerThreshold <= 0f)
            {
                return 50f;
            }

            return TriggerThreshold > 100f ? 100f : TriggerThreshold;
        }
    }
}
