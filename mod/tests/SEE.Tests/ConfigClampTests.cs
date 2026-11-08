using SEE.Core.Abstractions;
using SEE.Core.Config;
using SEE.Core.Logging;

namespace SEE.Tests
{
    /// <summary>
    /// Clamp tests for <see cref="SeeConfigCodec"/> (requirements §20, REQ-SAFE-01): every
    /// out-of-range or wrong-typed value must clamp/fall back to the documented bound and record a
    /// <c>SEE/Warn/Config</c> entry, never throw.
    /// </summary>
    internal static class ConfigClampTests
    {
        internal static void Run(TestRunner t)
        {
            BalanceClampTests(t);
            MaintenanceClampTests(t);
            CompatClampTests(t);
            PerfAnimArtUiClampTests(t);
            WrongTypeTests(t);
        }

        private static void BalanceClampTests(TestRunner t)
        {
            t.Run("Fuel rates clamp into 0.010..0.040", () =>
            {
                var log = new RecordingLog();
                var config = SeeConfigCodec.Parse(
                    "{\"balance\":{\"fuelRatePer100hp\":[9.0,0.025,-4.0,0.001]}}", log);
                Assert.Near(0.040, config.Balance.FuelRatePer100Hp[0], 1e-6, "high clamped");
                Assert.Near(0.025, config.Balance.FuelRatePer100Hp[1], 1e-6, "inside unchanged");
                Assert.Near(0.010, config.Balance.FuelRatePer100Hp[2], 1e-6, "negative clamped");
                Assert.Near(0.010, config.Balance.FuelRatePer100Hp[3], 1e-6, "tiny clamped");
                Assert.True(log.Entries.Count >= 3, "each clamp is reported");
            });

            t.Run("Water rates clamp into 0.010..0.100", () =>
            {
                var config = SeeConfigCodec.Parse("{\"balance\":{\"waterRatePer100hp\":[1.0,0.07,0.04,0.0]}}", null);
                Assert.Near(0.100, config.Balance.WaterRatePer100Hp[0], 1e-6, "high clamped");
                Assert.Near(0.010, config.Balance.WaterRatePer100Hp[3], 1e-6, "zero clamped");
            });

            t.Run("Faction multipliers clamp into their documented bands", () =>
            {
                var config = SeeConfigCodec.Parse(
                    "{\"balance\":{\"faction\":{\"outputMultiplier\":{\"Folktails\":5.0,\"IronTeeth\":0.1},"
                    + "\"fuelMultiplier\":{\"Folktails\":-2.0,\"IronTeeth\":7.0}}}}", null);
                Assert.Near(1.10, config.Balance.FactionOutputMultiplierFolktails, 1e-6, "output high");
                Assert.Near(0.95, config.Balance.FactionOutputMultiplierIronTeeth, 1e-6, "output low");
                Assert.Near(0.80, config.Balance.FactionFuelMultiplierFolktails, 1e-6, "fuel low");
                Assert.Near(1.10, config.Balance.FactionFuelMultiplierIronTeeth, 1e-6, "fuel high");
            });

            t.Run("Shipped nested tables are honoured (AD-13 canonical form)", () =>
            {
                var config = SeeConfigCodec.Parse(
                    "{\"balance\":{\"fuelRatePer100hp\":{\"T1\":0.033,\"T2\":0.025,\"T3\":0.021,\"T4\":0.016},"
                    + "\"waterRatePer100hp\":{\"T1\":0.08,\"T2\":0.07,\"T3\":0.04,\"T4\":0.018},"
                    + "\"faction\":{\"outputMultiplier\":{\"Folktails\":1.02,\"IronTeeth\":1.05},"
                    + "\"fuelMultiplier\":{\"Folktails\":0.90,\"IronTeeth\":1.00}}},"
                    + "\"maintenance\":{\"intervalDays\":{\"T1\":7,\"T2\":8,\"T3\":10,\"T4\":12}},"
                    + "\"art\":{\"trisLod0\":{\"Small\":3600,\"Medium\":6000,\"Large\":9000},"
                    + "\"textureSize\":{\"Small\":1024,\"Medium\":1024,\"Large\":1024}}}", null);
                Assert.Near(0.033, config.Balance.FuelRatePer100Hp[0], 1e-6, "nested T1 fuel");
                Assert.Near(1.02, config.Balance.FactionOutputMultiplierFolktails, 1e-6, "nested FT output");
                Assert.Near(7.0, config.Maintenance.IntervalDays[0], 1e-6, "nested T1 interval");
                Assert.Equal(3600, config.Art.TrisLod0[0], "nested tris Small");
                Assert.Equal(1024, config.Art.TextureSizeSmall, "nested textureSize Small");
            });

            t.Run("Inventory/tank hours clamp into their bands", () =>
            {
                var config = SeeConfigCodec.Parse(
                    "{\"balance\":{\"fullInventoryWorkHours\":1,\"waterTankHours\":9999,"
                    + "\"buildCostMultiplier\":-3,\"fuelScaling\":\"unknownMode\"}}", null);
                Assert.Near(10.0, config.Balance.FullInventoryWorkHours, 1e-6, "inventory low");
                Assert.Near(300.0, config.Balance.WaterTankHours, 1e-6, "tank high");
                Assert.Near(0.80, config.Balance.BuildCostMultiplier, 1e-6, "build cost low");
                Assert.Equal(FuelScalingMode.Proportional, config.Balance.FuelScaling, "unknown enum falls back");
            });
            // 2026-11-08 hotfix-3: the night standby floor is a ratio and clamps into 0..1.
            t.Run("standbyLoadRatio clamps into 0..1", () =>
            {
                Assert.Near(1.0, SeeConfigCodec.Parse(
                    "{\"balance\":{\"standbyLoadRatio\":5}}", null).Balance.StandbyLoadRatio, 1e-6, "high");
                Assert.Near(0.0, SeeConfigCodec.Parse(
                    "{\"balance\":{\"standbyLoadRatio\":-2}}", null).Balance.StandbyLoadRatio, 1e-6, "low");
                Assert.Near(0.35, SeeConfigCodec.Parse(
                    "{\"balance\":{\"standbyLoadRatio\":0.35}}", null).Balance.StandbyLoadRatio, 1e-6, "inside");
            });
        }

        private static void MaintenanceClampTests(TestRunner t)
        {
            t.Run("Interval days clamp into 4..20", () =>
            {
                var config = SeeConfigCodec.Parse(
                    "{\"maintenance\":{\"intervalDays\":[1,99,10,12]}}", null);
                Assert.Near(4.0, config.Maintenance.IntervalDays[0], 1e-6, "low");
                Assert.Near(20.0, config.Maintenance.IntervalDays[1], 1e-6, "high");
                Assert.Near(10.0, config.Maintenance.IntervalDays[2], 1e-6, "inside");
            });

            t.Run("Maintenance scalars clamp into their bands", () =>
            {
                var config = SeeConfigCodec.Parse(
                    "{\"maintenance\":{\"loadFactorBase\":0.99,\"autoThreshold\":0,"
                    + "\"manualWarnAbove\":1000,\"durationHours\":0.1}}", null);
                Assert.Near(0.80, config.Maintenance.LoadFactorBase, 1e-6, "load factor");
                Assert.Near(20.0, config.Maintenance.AutoThreshold, 1e-6, "threshold");
                Assert.Near(100.0, config.Maintenance.ManualWarnAbove, 1e-6, "manual warn");
                Assert.Near(1.0, config.Maintenance.DurationHours, 1e-6, "duration");
            });

            t.Run("Disabling maintenance is a valid boolean switch", () =>
            {
                var config = SeeConfigCodec.Parse("{\"maintenance\":{\"enabled\":false,\"manualMaterialsEnabled\":false}}", null);
                Assert.False(config.Maintenance.Enabled, "enabled");
                Assert.False(config.Maintenance.ManualMaterialsEnabled, "manual materials");
            });
        }

        private static void CompatClampTests(TestRunner t)
        {
            t.Run("Compat values clamp and enums fall back", () =>
            {
                var config = SeeConfigCodec.Parse(
                    "{\"compat\":{\"waterSupplyMode\":\"pipe\",\"detectionBudgetSeconds\":99,"
                    + "\"logThrottleSeconds\":1,\"beltFuelMode\":\"belt\",\"pipePollutionWarn\":false}}", null);
                Assert.Equal(WaterSupplyMode.Auto, config.Compat.WaterSupplyMode, "water mode");
                Assert.Near(5.0, config.Compat.DetectionBudgetSeconds, 1e-6, "budget");
                Assert.Near(10.0, config.Compat.LogThrottleSeconds, 1e-6, "throttle");
                Assert.Equal(BeltFuelMode.Auto, config.Compat.BeltFuelMode, "belt mode");
                Assert.False(config.Compat.PipePollutionWarn, "pollution warn");
            });
        }

        private static void PerfAnimArtUiClampTests(TestRunner t)
        {
            t.Run("Perf values clamp into their bands", () =>
            {
                var config = SeeConfigCodec.Parse(
                    "{\"perf\":{\"tickSeconds\":0.1,\"uiRefreshHz\":99,\"badgeRefreshHz\":0.1,"
                    + "\"maxBadgesVisible\":1,\"lodDistances\":[1,999]}}", null);
                Assert.Near(0.5, config.Perf.TickSeconds, 1e-6, "tick");
                Assert.Near(10.0, config.Perf.UiRefreshHz, 1e-6, "ui");
                Assert.Near(0.5, config.Perf.BadgeRefreshHz, 1e-6, "badge");
                Assert.Equal(16, config.Perf.MaxBadgesVisible, "badges");
                Assert.Near(10.0, config.Perf.LodDistances[0], 1e-6, "lod low");
                Assert.Near(80.0, config.Perf.LodDistances[1], 1e-6, "lod high");
            });

            t.Run("Anim values clamp into their bands", () =>
            {
                var config = SeeConfigCodec.Parse(
                    "{\"anim\":{\"updateHz\":1,\"lod0MaxDistance\":1,\"lod1MaxDistance\":999,"
                    + "\"vfxEmissionScale\":-5}}", null);
                Assert.Near(15.0, config.Anim.UpdateHz, 1e-6, "update hz");
                Assert.Near(10.0, config.Anim.Lod0MaxDistance, 1e-6, "lod0");
                Assert.Near(60.0, config.Anim.Lod1MaxDistance, 1e-6, "lod1");
                Assert.Near(0.0, config.Anim.VfxEmissionScale, 1e-6, "vfx");
            });

            t.Run("Art values clamp into their bands", () =>
            {
                var config = SeeConfigCodec.Parse(
                    "{\"art\":{\"trisLod0\":[-5,99999,6000],\"lod1Ratio\":0.99,\"lod2Ratio\":0.001,"
                    + "\"textureSize\":{\"Small\":32,\"Medium\":1024,\"Large\":99999}}}", null);
                Assert.Equal(1000, config.Art.TrisLod0[0], "tris low");
                Assert.Equal(20000, config.Art.TrisLod0[1], "tris high");
                Assert.Equal(6000, config.Art.TrisLod0[2], "tris inside");
                Assert.Near(0.60, config.Art.Lod1Ratio, 1e-6, "lod1 ratio");
                Assert.Near(0.05, config.Art.Lod2Ratio, 1e-6, "lod2 ratio");
                Assert.Equal(256, config.Art.TextureSizeSmall, "texture small");
                Assert.Equal(2048, config.Art.TextureSizeLarge, "texture large");
            });

            t.Run("Ui values clamp into their bands", () =>
            {
                var low = SeeConfigCodec.Parse("{\"ui\":{\"lowSupplyHours\":0,\"showIdleBadge\":true}}", null);
                Assert.Near(2.0, low.Ui.LowSupplyHours, 1e-6, "low supply hours low bound");
                Assert.True(low.Ui.ShowIdleBadge, "badge");

                var high = SeeConfigCodec.Parse("{\"ui\":{\"lowSupplyHours\":100}}", null);
                Assert.Near(24.0, high.Ui.LowSupplyHours, 1e-6, "low supply hours high bound");
            });
        }

        private static void WrongTypeTests(TestRunner t)
        {
            t.Run("Wrong-typed scalars keep the default and warn", () =>
            {
                var log = new RecordingLog();
                var config = SeeConfigCodec.Parse(
                    "{\"perf\":{\"tickSeconds\":\"fast\"},\"maintenance\":{\"enabled\":\"yes\"},"
                    + "\"compat\":{\"waterSupplyMode\":7}}", log);
                Assert.Near(1.0, config.Perf.TickSeconds, 1e-6, "tick fallback");
                Assert.True(config.Maintenance.Enabled, "enabled fallback");
                Assert.Equal(WaterSupplyMode.Auto, config.Compat.WaterSupplyMode, "enum fallback");
                Assert.True(log.Entries.Count >= 3, "each rejected value is reported");
            });

            t.Run("Wrong-typed arrays keep the defaults and warn", () =>
            {
                var config = SeeConfigCodec.Parse("{\"maintenance\":{\"intervalDays\":\"often\"}}", null);
                Assert.Near(6.0, config.Maintenance.IntervalDays[0], 1e-6, "T1 default");
                Assert.Near(12.0, config.Maintenance.IntervalDays[3], 1e-6, "T4 default");
            });

            t.Run("A non-object root keeps the defaults", () =>
                Assert.Equal(1, SeeConfigCodec.Parse("[1,2,3]", null).ConfigSchemaVersion, "array root"));

            t.Run("Serialize then Parse round-trips the canonical document", () =>
            {
                var written = SeeConfigCodec.Serialize(SeeConfig.Default);
                var parsed = SeeConfigCodec.Parse(written, null);
                Assert.Equal(SeeConfig.Default.ConfigSchemaVersion, parsed.ConfigSchemaVersion, "schema");
                Assert.Near(25.0, parsed.Balance.FullInventoryWorkHours, 1e-6, "inventory hours");
                Assert.Near(8.0, parsed.Maintenance.IntervalDays[1], 1e-6, "T2 interval");
                Assert.Near(0.50, parsed.Art.Lod1Ratio, 1e-6, "art ratio round-trip");
                Assert.Equal(SeeConfig.Default.Perf.MaxBadgesVisible, parsed.Perf.MaxBadgesVisible, "badges");
            });
        }
    }
}
