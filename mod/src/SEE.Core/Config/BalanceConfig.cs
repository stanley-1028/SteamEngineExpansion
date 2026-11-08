using System;
using System.Collections.Generic;

namespace SEE.Core.Config
{
    /// <summary>
    /// Resolved <c>balance.*</c> configuration (requirements §9.1/§20.1). Values are already
    /// clamped by <see cref="SeeConfigCodec"/>; this type is immutable after construction.
    /// </summary>
    public sealed class BalanceConfig
    {
        internal BalanceConfig(
            IReadOnlyList<float> fuelRatePer100Hp,
            IReadOnlyList<float> waterRatePer100Hp,
            float factionOutputMultiplierFolktails,
            float factionOutputMultiplierIronTeeth,
            float factionFuelMultiplierFolktails,
            float factionFuelMultiplierIronTeeth,
            float fullInventoryWorkHours,
            float waterTankHours,
            FuelScalingMode fuelScaling,
            float standbyLoadRatio,
            float buildCostMultiplier)
        {
            FuelRatePer100Hp = fuelRatePer100Hp ?? throw new ArgumentNullException(nameof(fuelRatePer100Hp));
            WaterRatePer100Hp = waterRatePer100Hp ?? throw new ArgumentNullException(nameof(waterRatePer100Hp));
            FactionOutputMultiplierFolktails = factionOutputMultiplierFolktails;
            FactionOutputMultiplierIronTeeth = factionOutputMultiplierIronTeeth;
            FactionFuelMultiplierFolktails = factionFuelMultiplierFolktails;
            FactionFuelMultiplierIronTeeth = factionFuelMultiplierIronTeeth;
            FullInventoryWorkHours = fullInventoryWorkHours;
            WaterTankHours = waterTankHours;
            FuelScaling = fuelScaling;
            StandbyLoadRatio = standbyLoadRatio;
            BuildCostMultiplier = buildCostMultiplier;
        }

        /// <summary>Fuel items per 100 hp per game hour, indexed by <see cref="Abstractions.BoilerTier"/>.</summary>
        public IReadOnlyList<float> FuelRatePer100Hp { get; }

        /// <summary>Water items per 100 hp per game hour, indexed by tier.</summary>
        public IReadOnlyList<float> WaterRatePer100Hp { get; }

        /// <summary>Folktails output multiplier (1.00).</summary>
        public float FactionOutputMultiplierFolktails { get; }

        /// <summary>Iron Teeth output multiplier (1.05).</summary>
        public float FactionOutputMultiplierIronTeeth { get; }

        /// <summary>Folktails fuel multiplier (0.90).</summary>
        public float FactionFuelMultiplierFolktails { get; }

        /// <summary>Iron Teeth fuel multiplier (1.00).</summary>
        public float FactionFuelMultiplierIronTeeth { get; }

        /// <summary>Fuel inventory capacity expressed in full-load working hours (25).</summary>
        public float FullInventoryWorkHours { get; }

        /// <summary>Water tank capacity expressed in full-load working hours (200).</summary>
        public float WaterTankHours { get; }

        /// <summary>Fuel consumption model.</summary>
        public FuelScalingMode FuelScaling { get; }

        /// <summary>
        /// Minimum output/load fraction kept while the boiler is supplied and running (0..1, 0.10 by
        /// default). 2026-11-08 hotfix-3: the network demand collapses to 0 at night because the
        /// worker buildings disable their power consumption outside working hours, which used to
        /// turn every boiler into <c>Idle</c> (output 0, no fuel/water, flywheel stopped). A positive
        /// standby ratio keeps the boiler running at this low load instead, so it never stops while
        /// fuel and water remain; 0 restores the old idle-at-zero-demand behaviour.
        /// </summary>
        public float StandbyLoadRatio { get; }

        /// <summary>Global construction cost multiplier (1.00, range 0.80..1.20).</summary>
        public float BuildCostMultiplier { get; }
    }
}
