using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Config;

namespace SEE.Tests
{
    /// <summary>
    /// Unit tests for <see cref="SeeConfig"/> and <see cref="SeeConfigCodec.Parse(string, SEE.Core.Logging.ISeeLog?)"/>
    /// defaults (requirements §20, config schema 1): missing fields keep the documented default,
    /// unknown fields are ignored, and the default values match the §20 table.
    /// </summary>
    internal static class ConfigTests
    {
        internal static void Run(TestRunner t)
        {
            DefaultTests(t);
            MissingFieldTests(t);
            UnknownFieldTests(t);
            UnknownWriteBackTests(t);
            RulesTests(t);
        }

        private static void UnknownWriteBackTests(TestRunner t)
        {
            t.Run("Write-back keeps unknown top-level and group properties", () =>
            {
                const string source = "{\"configSchemaVersion\":1,\"modding\":{\"x\":1},"
                                      + "\"balance\":{\"mysteryKnob\":123,\"fullInventoryWorkHours\":30}}";
                var config = SeeConfigCodec.Parse(source, null);
                string written = SeeConfigCodec.Serialize(config, source);
                Assert.Contains(written, "mysteryKnob", "unknown balance key kept");
                Assert.Contains(written, "\"modding\"", "unknown group kept");
                Assert.Contains(written, "\"fullInventoryWorkHours\":30", "known value written");
            });

            t.Run("Write-back without a source document stays canonical", () =>
            {
                string written = SeeConfigCodec.Serialize(SeeConfig.Default, null);
                Assert.Contains(written, "\"configSchemaVersion\":1", "schema");
                Assert.False(written.Contains("mysteryKnob", System.StringComparison.Ordinal), "no invented keys");
            });

            t.Run("A malformed source document cannot break write-back", () =>
            {
                string written = SeeConfigCodec.Serialize(SeeConfig.Default, "{not json");
                Assert.Contains(written, "\"balance\"", "canonical groups still written");
                Assert.Equal(1, SeeConfigCodec.Parse(written, null).ConfigSchemaVersion, "still loadable");
            });

            t.Run("Serializing then parsing preserved unknown fields keeps the known values", () =>
            {
                const string source = "{\"maintenance\":{\"mysteryMaintenance\":true,\"durationHours\":4}}";
                var config = SeeConfigCodec.Parse(source, null);
                string written = SeeConfigCodec.Serialize(config, source);
                var reread = SeeConfigCodec.Parse(written, null);
                Assert.Near(4.0, reread.Maintenance.DurationHours, 1e-6, "known value");
                Assert.Contains(written, "mysteryMaintenance", "unknown value");
            });
        }

        private static void DefaultTests(TestRunner t)
        {
            t.Run("Default config schema version is 1", () =>
                Assert.Equal(1, SeeConfig.Default.ConfigSchemaVersion, "schema"));

            t.Run("Default balance values match §20", () =>
            {
                var balance = SeeConfig.Default.Balance;
                Assert.Equal(FuelScalingMode.Proportional, balance.FuelScaling, "fuelScaling");
                Assert.Near(25.0, balance.FullInventoryWorkHours, 1e-6, "fullInventoryWorkHours");
                Assert.Near(200.0, balance.WaterTankHours, 1e-6, "waterTankHours");
                Assert.Near(1.00, balance.FactionOutputMultiplierFolktails, 1e-6, "FT output");
                Assert.Near(1.05, balance.FactionOutputMultiplierIronTeeth, 1e-6, "IT output");
                Assert.Near(0.90, balance.FactionFuelMultiplierFolktails, 1e-6, "FT fuel");
                Assert.Near(1.00, balance.FactionFuelMultiplierIronTeeth, 1e-6, "IT fuel");
                Assert.Near(1.00, balance.BuildCostMultiplier, 1e-6, "buildCostMultiplier");
                Assert.Near(0.10, balance.StandbyLoadRatio, 1e-6, "standbyLoadRatio");
                Assert.Near(0.032, balance.FuelRatePer100Hp[0], 1e-6, "T1 fuel");
                Assert.Near(0.025, balance.FuelRatePer100Hp[1], 1e-6, "T2 fuel");
                Assert.Near(0.021, balance.FuelRatePer100Hp[2], 1e-6, "T3 fuel");
                Assert.Near(0.016, balance.FuelRatePer100Hp[3], 1e-6, "T4 fuel");
            });

            t.Run("Default maintenance values match §20", () =>
            {
                var maintenance = SeeConfig.Default.Maintenance;
                Assert.True(maintenance.Enabled, "enabled");
                Assert.Near(50.0, maintenance.AutoThreshold, 1e-6, "autoThreshold");
                Assert.Near(90.0, maintenance.ManualWarnAbove, 1e-6, "manualWarnAbove");
                Assert.Near(2.0, maintenance.DurationHours, 1e-6, "durationHours");
                Assert.Near(0.4, maintenance.LoadFactorBase, 1e-6, "loadFactorBase");
                Assert.True(maintenance.ManualMaterialsEnabled, "manualMaterialsEnabled");
                Assert.Near(6.0, maintenance.IntervalDays[0], 1e-6, "T1 interval");
                Assert.Near(8.0, maintenance.IntervalDays[1], 1e-6, "T2 interval");
                Assert.Near(10.0, maintenance.IntervalDays[2], 1e-6, "T3 interval");
                Assert.Near(12.0, maintenance.IntervalDays[3], 1e-6, "T4 interval");
            });

            t.Run("Default compat/perf/anim/art/ui values match §20", () =>
            {
                Assert.Equal(WaterSupplyMode.Auto, SeeConfig.Default.Compat.WaterSupplyMode, "waterSupplyMode");
                Assert.True(SeeConfig.Default.Compat.EnablePowerLineInjection, "enablePowerLineInjection");
                Assert.Near(30.0, SeeConfig.Default.Compat.LogThrottleSeconds, 1e-6, "logThrottleSeconds");
                Assert.Near(1.0, SeeConfig.Default.Perf.TickSeconds, 1e-6, "tickSeconds");
                Assert.Near(4.0, SeeConfig.Default.Perf.UiRefreshHz, 1e-6, "uiRefreshHz");
                Assert.Equal(64, SeeConfig.Default.Perf.MaxBadgesVisible, "maxBadgesVisible");
                Assert.Near(18.0, SeeConfig.Default.Perf.LodDistances[0], 1e-6, "LOD0 distance");
                Assert.Near(40.0, SeeConfig.Default.Perf.LodDistances[1], 1e-6, "LOD1 distance");
                Assert.True(SeeConfig.Default.Anim.Enable, "anim enabled");
                Assert.Near(0.50, SeeConfig.Default.Art.Lod1Ratio, 1e-6, "lod1Ratio");
                Assert.Near(0.15, SeeConfig.Default.Art.Lod2Ratio, 1e-6, "lod2Ratio");
                Assert.False(SeeConfig.Default.Ui.ShowIdleBadge, "showIdleBadge");
                Assert.Near(8.0, SeeConfig.Default.Ui.LowSupplyHours, 1e-6, "lowSupplyHours");
            });
        }

        private static void MissingFieldTests(TestRunner t)
        {
            t.Run("Empty document yields the compiled defaults", () =>
            {
                var config = SeeConfigCodec.Parse("{}", null);
                Assert.Equal(1, config.ConfigSchemaVersion, "schema");
                Assert.Near(25.0, config.Balance.FullInventoryWorkHours, 1e-6, "fullInventoryWorkHours");
                Assert.Near(2.0, config.Maintenance.DurationHours, 1e-6, "durationHours");
            });

            t.Run("Null/blank document yields the compiled defaults", () =>
            {
                Assert.Equal(1, SeeConfigCodec.Parse(null!, null).ConfigSchemaVersion, "null");
                Assert.Equal(1, SeeConfigCodec.Parse(string.Empty, null).ConfigSchemaVersion, "empty");
                Assert.Equal(1, SeeConfigCodec.Parse("   ", null).ConfigSchemaVersion, "blank");
            });

            t.Run("A single provided field keeps every other default", () =>
            {
                var config = SeeConfigCodec.Parse("{\"maintenance\":{\"durationHours\":4}}", null);
                Assert.Near(4.0, config.Maintenance.DurationHours, 1e-6, "provided value");
                Assert.Near(50.0, config.Maintenance.AutoThreshold, 1e-6, "sibling default");
                Assert.Near(8.0, config.Maintenance.IntervalDays[1], 1e-6, "other group default");
            });

            t.Run("standbyLoadRatio defaults to 0.10 and accepts an override", () =>
            {
                Assert.Near(0.10, SeeConfigCodec.Parse("{}", null).Balance.StandbyLoadRatio, 1e-6, "default");
                Assert.Near(0.25, SeeConfigCodec.Parse(
                    "{\"balance\":{\"standbyLoadRatio\":0.25}}", null).Balance.StandbyLoadRatio, 1e-6, "override");
            });

            t.Run("A broken document falls back to the defaults instead of throwing", () =>
                Assert.Equal(1, SeeConfigCodec.Parse("{not json", null).ConfigSchemaVersion, "invalid JSON"));
        }

        private static void UnknownFieldTests(TestRunner t)
        {
            t.Run("Unknown keys inside a group are ignored", () =>
            {
                var config = SeeConfigCodec.Parse(
                    "{\"balance\":{\"mysteryKnob\":123,\"fullInventoryWorkHours\":30}}", null);
                Assert.Near(30.0, config.Balance.FullInventoryWorkHours, 1e-6, "known key applied");
            });

            t.Run("Unknown top-level groups are ignored", () =>
            {
                var config = SeeConfigCodec.Parse("{\"modding\":{\"x\":1},\"ui\":{\"lowSupplyHours\":10}}", null);
                Assert.Near(10.0, config.Ui.LowSupplyHours, 1e-6, "known group applied");
            });

            t.Run("A newer schema version is reported but still loads", () =>
            {
                var log = new SEE.Core.Logging.RecordingLog();
                var config = SeeConfigCodec.Parse("{\"configSchemaVersion\":99}", log);
                Assert.Equal(1, config.ConfigSchemaVersion, "falls back to the current schema");
                Assert.True(log.Entries.Count > 0, "a warning was recorded");
            });
        }

        private static void RulesTests(TestRunner t)
        {
            t.Run("RulesFor maps each tier to its §20 interval", () =>
            {
                var config = SeeConfig.Default;
                var materials = new List<MaintenanceMaterial>();
                Assert.Near(6.0, config.RulesFor(BoilerTier.T1, true, materials).IntervalDays, 1e-6, "T1");
                Assert.Near(8.0, config.RulesFor(BoilerTier.T2, true, materials).IntervalDays, 1e-6, "T2");
                Assert.Near(10.0, config.RulesFor(BoilerTier.T3, true, materials).IntervalDays, 1e-6, "T3");
                Assert.Near(12.0, config.RulesFor(BoilerTier.T4, true, materials).IntervalDays, 1e-6, "T4");
            });

            t.Run("RulesFor carries threshold, duration, auto flag and materials", () =>
            {
                var config = SeeConfig.Default;
                var materials = new List<MaintenanceMaterial> { new MaintenanceMaterial("Plank", 12) };
                var rules = config.RulesFor(BoilerTier.T2, autoEnabled: false, materials);
                Assert.Near(50.0, rules.AutoThreshold, 1e-6, "threshold");
                Assert.Near(2.0, rules.DurationHours, 1e-6, "duration");
                Assert.Near(90.0, rules.ManualWarnAbove, 1e-6, "manual warn threshold (C-5)");
                Assert.True(rules.Enabled, "enabled");
                Assert.False(rules.Auto, "auto flag comes from the caller");
                Assert.Equal(1, rules.Materials.Count, "materials");
            });

            t.Run("IntervalDaysFor returns the configured tier interval", () =>
            {
                Assert.Near(10.0, SeeConfig.Default.IntervalDaysFor(BoilerTier.T3), 1e-6, "T3 interval");
            });
        }
    }
}
