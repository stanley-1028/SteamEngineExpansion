using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Maintenance;

namespace SEE.Core.Config
{
    /// <summary>
    /// Validated in-memory view of <c>SEE_Config.json</c> (config schema 1, requirements §20).
    /// <see cref="SeeConfigCodec.Parse"/> produces it; gameplay reads it, it is never mutated after
    /// construction (REQ-NFR-09). Missing fields keep their documented defaults, unknown fields are
    /// ignored and out-of-range values are clamped with a throttled warning.
    /// </summary>
    public sealed class SeeConfig
    {
        internal SeeConfig(
            int configSchemaVersion,
            BalanceConfig balance,
            MaintenanceConfig maintenance,
            CompatConfig compat,
            PerfConfig perf,
            AnimConfig anim,
            ArtConfig art,
            UiConfig ui)
        {
            ConfigSchemaVersion = configSchemaVersion;
            Balance = balance ?? throw new ArgumentNullException(nameof(balance));
            Maintenance = maintenance ?? throw new ArgumentNullException(nameof(maintenance));
            Compat = compat ?? throw new ArgumentNullException(nameof(compat));
            Perf = perf ?? throw new ArgumentNullException(nameof(perf));
            Anim = anim ?? throw new ArgumentNullException(nameof(anim));
            Art = art ?? throw new ArgumentNullException(nameof(art));
            Ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        /// <summary>Schema version the file was written with.</summary>
        public int ConfigSchemaVersion { get; }

        /// <summary>Balance parameters (§9.1).</summary>
        public BalanceConfig Balance { get; }

        /// <summary>Maintenance parameters (§9.2).</summary>
        public MaintenanceConfig Maintenance { get; }

        /// <summary>Compatibility and integration parameters (§9.3).</summary>
        public CompatConfig Compat { get; }

        /// <summary>Performance and UI parameters (§9.4).</summary>
        public PerfConfig Perf { get; }

        /// <summary>Animation parameters (§9.5).</summary>
        public AnimConfig Anim { get; }

        /// <summary>Art parameters (§9.6).</summary>
        public ArtConfig Art { get; }

        /// <summary>Panel parameters (§9.4 <c>ui.*</c>).</summary>
        public UiConfig Ui { get; }

        /// <summary>The shipped defaults; also the fallback when the file is absent or corrupt.</summary>
        public static SeeConfig Default { get; } = BuildDefault();

        /// <summary>Returns the maintenance rules of one tier, ready for <see cref="MaintenanceLedger"/>.</summary>
        /// <param name="tier">Tier id; out-of-range values fall back to T1.</param>
        /// <param name="autoEnabled">Whether automatic maintenance is enabled for this boiler.</param>
        /// <param name="materials">Materials of this blueprint; null is treated as empty.</param>
        /// <returns>Immutable rules for the ledger.</returns>
        public MaintenanceRules RulesFor(
            BoilerTier tier,
            bool autoEnabled,
            IReadOnlyList<MaintenanceMaterial>? materials)
        {
            int index = TierIndex(tier);
            return new MaintenanceRules(
                enabled: Maintenance.Enabled,
                auto: autoEnabled,
                autoThreshold: Maintenance.AutoThreshold,
                durationHours: Maintenance.DurationHours,
                intervalDays: Maintenance.IntervalDays[index],
                materials: materials ?? Array.Empty<MaintenanceMaterial>(),
                manualWarnAbove: Maintenance.ManualWarnAbove);
        }

        /// <summary>Returns the configured maintenance interval of one tier in running days.</summary>
        /// <param name="tier">Tier id; out-of-range values fall back to T1.</param>
        /// <returns>Interval in running days.</returns>
        public float IntervalDaysFor(BoilerTier tier) => Maintenance.IntervalDays[TierIndex(tier)];

        /// <summary>Clamps an enum value to a valid array index (defensive against corrupt specs).</summary>
        /// <param name="tier">Tier id.</param>
        /// <returns>Index 0..3.</returns>
        internal static int TierIndex(BoilerTier tier)
        {
            int value = (int)tier;
            return value < 0 || value > 3 ? 0 : value;
        }

        private static SeeConfig BuildDefault()
        {
            var fuel = new ReadOnlyCollection<float>(new[] { 0.032f, 0.025f, 0.021f, 0.016f });
            var water = new ReadOnlyCollection<float>(new[] { 0.040f, 0.035f, 0.020f, 0.010f });
            var intervals = new ReadOnlyCollection<float>(new[] { 6f, 8f, 10f, 12f });
            var lod = new ReadOnlyCollection<float>(new[] { 18f, 40f });
            var tris = new ReadOnlyCollection<int>(new[] { 3500, 6000, 9000 });

            return new SeeConfig(
                SEEVersion.ConfigSchemaVersion,
                new BalanceConfig(
                    fuel,
                    water,
                    factionOutputMultiplierFolktails: 1.00f,
                    factionOutputMultiplierIronTeeth: 1.05f,
                    factionFuelMultiplierFolktails: 0.90f,
                    factionFuelMultiplierIronTeeth: 1.00f,
                    fullInventoryWorkHours: 25f,
                    waterTankHours: 200f,
                    fuelScaling: FuelScalingMode.Proportional,
                    standbyLoadRatio: 0.10f,
                    buildCostMultiplier: 1.00f),
                new MaintenanceConfig(
                    enabled: true,
                    intervalDays: intervals,
                    loadFactorBase: 0.4f,
                    autoThreshold: 50f,
                    manualWarnAbove: 90f,
                    durationHours: 2f,
                    manualMaterialsEnabled: true),
                new CompatConfig(
                    waterSupplyMode: WaterSupplyMode.Auto,
                    detectionBudgetSeconds: 2f,
                    enablePowerLineInjection: true,
                    beltFuelMode: BeltFuelMode.Auto,
                    logThrottleSeconds: 30f,
                    pipePollutionWarn: true),
                new PerfConfig(
                    tickSeconds: 1.0f,
                    uiRefreshHz: 4f,
                    badgeRefreshHz: 1f,
                    maxBadgesVisible: 64,
                    lodDistances: lod),
                new AnimConfig(
                    enable: true,
                    updateHz: 30f,
                    lod0MaxDistance: 18f,
                    lod1MaxDistance: 40f,
                    vfxEmissionScale: 1.0f),
                new ArtConfig(
                    trisLod0: tris,
                    lod1Ratio: 0.50f,
                    lod2Ratio: 0.15f,
                    textureSizeSmall: 512,
                    textureSizeLarge: 1024),
                new UiConfig(lowSupplyHours: 8f, showIdleBadge: false));
        }
    }
}