using System;
using System.Collections.Generic;
using SEE.Core.Constants;
using SEE.Core.Json;
using SEE.Core.Logging;

namespace SEE.Core.Config
{
    /// <summary>
    /// Config schema 1 validation and clamping (requirements §20, CONTRACT_SEE §7, REQ-SAFE-01).
    /// Every read is defensive: wrong types, NaN, infinities and out-of-range values degrade to the
    /// documented default or clamp to the allowed range, reported through <c>SEE/Warn/Config</c>.
    /// Unknown properties are ignored so a newer/older file still loads; the shipped file's
    /// nested-object tables and the tests' compact arrays are both accepted. Internal helper of
    /// <see cref="SeeConfigCodec"/>.
    /// </summary>
    internal static class SeeConfigValidation
    {
        internal const float MinFuelRate = 0.010f;
        internal const float MaxFuelRate = 0.040f;
        internal const float MinWaterRate = 0.010f;
        internal const float MaxWaterRate = 0.100f;
        internal const float MinOutputMultiplier = 0.95f;
        internal const float MaxOutputMultiplier = 1.10f;
        internal const float MinFuelMultiplier = 0.80f;
        internal const float MaxFuelMultiplier = 1.10f;
        internal const float MinInventoryHours = 10f;
        internal const float MaxInventoryHours = 50f;
        internal const float MinTankHours = 100f;
        internal const float MaxTankHours = 300f;
        internal const float MinBuildCostMultiplier = 0.80f;
        internal const float MaxBuildCostMultiplier = 1.20f;
        internal const float MinStandbyLoadRatio = 0f;
        internal const float MaxStandbyLoadRatio = 1f;
        internal const float MinIntervalDays = 4f;
        internal const float MaxIntervalDays = 20f;
        internal const float MinLoadFloor = 0.2f;
        internal const float MaxLoadFloor = 0.8f;
        internal const float MinAutoThreshold = 20f;
        internal const float MaxAutoThreshold = 80f;
        internal const float MinManualWarn = 50f;
        internal const float MaxManualWarn = 100f;
        internal const float MinDurationHours = 1f;
        internal const float MaxDurationHours = 8f;
        internal const float MinDetectBudget = 1f;
        internal const float MaxDetectBudget = 5f;
        internal const float MinLogThrottle = 10f;
        internal const float MaxLogThrottle = 120f;
        internal const float MinTickSeconds = 0.5f;
        internal const float MaxTickSeconds = 2.0f;
        internal const float MinUiRefreshHz = 1f;
        internal const float MaxUiRefreshHz = 10f;
        internal const float MinBadgeRefreshHz = 0.5f;
        internal const float MaxBadgeRefreshHz = 2f;
        internal const int MinBadges = 16;
        internal const int MaxBadges = 256;
        internal const float MinLodDistance = 10f;
        internal const float MaxLodDistance = 80f;
        internal const float MinLowSupplyHours = 2f;
        internal const float MaxLowSupplyHours = 24f;
        internal const float MinAnimHz = 15f;
        internal const float MaxAnimHz = 60f;
        internal const float MinAnimLod0 = 10f;
        internal const float MaxAnimLod0 = 30f;
        internal const float MinAnimLod1 = 25f;
        internal const float MaxAnimLod1 = 60f;
        internal const float MinVfxScale = 0f;
        internal const float MaxVfxScale = 2f;
        internal const float MinLod1Ratio = 0.30f;
        internal const float MaxLod1Ratio = 0.60f;
        internal const float MinLod2Ratio = 0.05f;
        internal const float MaxLod2Ratio = 0.20f;
        internal const int MinTriangleBudget = 1000;
        internal const int MaxTriangleBudget = 20000;
        internal const int MinTextureSize = 256;
        internal const int MaxTextureSize = 2048;

        /// <summary>Builds a validated config from an already-parsed JSON object.</summary>
        internal static SeeConfig Parse(IDictionary<string, object?> root, ISeeLog? log)
        {
            SeeConfig defaults = SeeConfig.Default;
            int version = ReadSchemaVersion(root, log);
            BalanceConfig balance = ReadBalance(ConfigFieldReader.Child(root, "balance"), defaults.Balance, log);
            MaintenanceConfig maintenance = ReadMaintenance(ConfigFieldReader.Child(root, "maintenance"), defaults.Maintenance, log);
            CompatConfig compat = ReadCompat(ConfigFieldReader.Child(root, "compat"), defaults.Compat, log);
            PerfConfig perf = ReadPerf(ConfigFieldReader.Child(root, "perf"), defaults.Perf, log);
            AnimConfig anim = ReadAnim(ConfigFieldReader.Child(root, "anim"), defaults.Anim, log);
            ArtConfig art = ReadArt(ConfigFieldReader.Child(root, "art"), defaults.Art, log);
            UiConfig ui = ReadUi(ConfigFieldReader.Child(root, "ui"), defaults.Ui, log);
            return new SeeConfig(version, balance, maintenance, compat, perf, anim, art, ui);
        }

        private static int ReadSchemaVersion(IDictionary<string, object?> root, ISeeLog? log)
        {
            int version = MiniJson.GetInt(root, "configSchemaVersion", SEEVersion.ConfigSchemaVersion);
            if (version < SEEVersion.MinSupportedConfigSchemaVersion || version > SEEVersion.ConfigSchemaVersion)
            {
                ConfigFieldReader.Warn(
                    log,
                    "configSchemaVersion",
                    "unsupported value " + version + "; using " + SEEVersion.ConfigSchemaVersion);
                return SEEVersion.ConfigSchemaVersion;
            }

            return version;
        }

        private static BalanceConfig ReadBalance(
            IDictionary<string, object?>? map,
            BalanceConfig defaults,
            ISeeLog? log)
        {
            return new BalanceConfig(
                ConfigFieldReader.ReadFloatArray(map, "fuelRatePer100hp", defaults.FuelRatePer100Hp, MinFuelRate, MaxFuelRate, "balance", log, ConfigFieldReader.TierKeys),
                ConfigFieldReader.ReadFloatArray(map, "waterRatePer100hp", defaults.WaterRatePer100Hp, MinWaterRate, MaxWaterRate, "balance", log, ConfigFieldReader.TierKeys),
                ConfigFieldReader.ReadNestedFloat(map, new[] { "faction", "outputMultiplier", "Folktails" }, defaults.FactionOutputMultiplierFolktails, MinOutputMultiplier, MaxOutputMultiplier, "balance", log),
                ConfigFieldReader.ReadNestedFloat(map, new[] { "faction", "outputMultiplier", "IronTeeth" }, defaults.FactionOutputMultiplierIronTeeth, MinOutputMultiplier, MaxOutputMultiplier, "balance", log),
                ConfigFieldReader.ReadNestedFloat(map, new[] { "faction", "fuelMultiplier", "Folktails" }, defaults.FactionFuelMultiplierFolktails, MinFuelMultiplier, MaxFuelMultiplier, "balance", log),
                ConfigFieldReader.ReadNestedFloat(map, new[] { "faction", "fuelMultiplier", "IronTeeth" }, defaults.FactionFuelMultiplierIronTeeth, MinFuelMultiplier, MaxFuelMultiplier, "balance", log),
                ConfigFieldReader.ReadFloat(map, "fullInventoryWorkHours", defaults.FullInventoryWorkHours, MinInventoryHours, MaxInventoryHours, "balance", log),
                ConfigFieldReader.ReadFloat(map, "waterTankHours", defaults.WaterTankHours, MinTankHours, MaxTankHours, "balance", log),
                ConfigFieldReader.ReadEnum(map, "fuelScaling", defaults.FuelScaling, "balance", log),
                ConfigFieldReader.ReadFloat(map, "standbyLoadRatio", defaults.StandbyLoadRatio, MinStandbyLoadRatio, MaxStandbyLoadRatio, "balance", log),
                ConfigFieldReader.ReadFloat(map, "buildCostMultiplier", defaults.BuildCostMultiplier, MinBuildCostMultiplier, MaxBuildCostMultiplier, "balance", log));
        }

        private static MaintenanceConfig ReadMaintenance(
            IDictionary<string, object?>? map,
            MaintenanceConfig defaults,
            ISeeLog? log)
        {
            return new MaintenanceConfig(
                ConfigFieldReader.ReadBool(map, "enabled", defaults.Enabled, "maintenance", log),
                ConfigFieldReader.ReadFloatArray(map, "intervalDays", defaults.IntervalDays, MinIntervalDays, MaxIntervalDays, "maintenance", log, ConfigFieldReader.TierKeys),
                ConfigFieldReader.ReadFloat(map, "loadFactorBase", defaults.LoadFactorBase, MinLoadFloor, MaxLoadFloor, "maintenance", log),
                ConfigFieldReader.ReadFloat(map, "autoThreshold", defaults.AutoThreshold, MinAutoThreshold, MaxAutoThreshold, "maintenance", log),
                ConfigFieldReader.ReadFloat(map, "manualWarnAbove", defaults.ManualWarnAbove, MinManualWarn, MaxManualWarn, "maintenance", log),
                ConfigFieldReader.ReadFloat(map, "durationHours", defaults.DurationHours, MinDurationHours, MaxDurationHours, "maintenance", log),
                ConfigFieldReader.ReadBool(map, "manualMaterialsEnabled", defaults.ManualMaterialsEnabled, "maintenance", log));
        }

        private static CompatConfig ReadCompat(
            IDictionary<string, object?>? map,
            CompatConfig defaults,
            ISeeLog? log)
        {
            return new CompatConfig(
                ConfigFieldReader.ReadEnum(map, "waterSupplyMode", defaults.WaterSupplyMode, "compat", log),
                ConfigFieldReader.ReadFloat(map, "detectionBudgetSeconds", defaults.DetectionBudgetSeconds, MinDetectBudget, MaxDetectBudget, "compat", log),
                ConfigFieldReader.ReadBool(map, "enablePowerLineInjection", defaults.EnablePowerLineInjection, "compat", log),
                ConfigFieldReader.ReadEnum(map, "beltFuelMode", defaults.BeltFuelMode, "compat", log),
                ConfigFieldReader.ReadFloat(map, "logThrottleSeconds", defaults.LogThrottleSeconds, MinLogThrottle, MaxLogThrottle, "compat", log),
                ConfigFieldReader.ReadBool(map, "pipePollutionWarn", defaults.PipePollutionWarn, "compat", log));
        }

        private static PerfConfig ReadPerf(
            IDictionary<string, object?>? map,
            PerfConfig defaults,
            ISeeLog? log)
        {
            return new PerfConfig(
                ConfigFieldReader.ReadFloat(map, "tickSeconds", defaults.TickSeconds, MinTickSeconds, MaxTickSeconds, "perf", log),
                ConfigFieldReader.ReadFloat(map, "uiRefreshHz", defaults.UiRefreshHz, MinUiRefreshHz, MaxUiRefreshHz, "perf", log),
                ConfigFieldReader.ReadFloat(map, "badgeRefreshHz", defaults.BadgeRefreshHz, MinBadgeRefreshHz, MaxBadgeRefreshHz, "perf", log),
                ConfigFieldReader.ReadInt(map, "maxBadgesVisible", defaults.MaxBadgesVisible, MinBadges, MaxBadges, "perf", log),
                ConfigFieldReader.ReadFloatArray(map, "lodDistances", defaults.LodDistances, MinLodDistance, MaxLodDistance, "perf", log));
        }

        private static AnimConfig ReadAnim(
            IDictionary<string, object?>? map,
            AnimConfig defaults,
            ISeeLog? log)
        {
            return new AnimConfig(
                ConfigFieldReader.ReadBool(map, "enable", defaults.Enable, "anim", log),
                ConfigFieldReader.ReadFloat(map, "updateHz", defaults.UpdateHz, MinAnimHz, MaxAnimHz, "anim", log),
                ConfigFieldReader.ReadFloat(map, "lod0MaxDistance", defaults.Lod0MaxDistance, MinAnimLod0, MaxAnimLod0, "anim", log),
                ConfigFieldReader.ReadFloat(map, "lod1MaxDistance", defaults.Lod1MaxDistance, MinAnimLod1, MaxAnimLod1, "anim", log),
                ConfigFieldReader.ReadFloat(map, "vfxEmissionScale", defaults.VfxEmissionScale, MinVfxScale, MaxVfxScale, "anim", log));
        }

        private static ArtConfig ReadArt(
            IDictionary<string, object?>? map,
            ArtConfig defaults,
            ISeeLog? log)
        {
            return new ArtConfig(
                ConfigFieldReader.ReadIntArray(map, "trisLod0", defaults.TrisLod0, MinTriangleBudget, MaxTriangleBudget, "art", log, ConfigFieldReader.SizeKeys),
                ConfigFieldReader.ReadFloat(map, "lod1Ratio", defaults.Lod1Ratio, MinLod1Ratio, MaxLod1Ratio, "art", log),
                ConfigFieldReader.ReadFloat(map, "lod2Ratio", defaults.Lod2Ratio, MinLod2Ratio, MaxLod2Ratio, "art", log),
                ConfigFieldReader.ReadNestedInt(map, new[] { "textureSize", "Small" }, defaults.TextureSizeSmall, MinTextureSize, MaxTextureSize, "art", log),
                ConfigFieldReader.ReadNestedInt(map, new[] { "textureSize", "Large" }, defaults.TextureSizeLarge, MinTextureSize, MaxTextureSize, "art", log));
        }

        private static UiConfig ReadUi(
            IDictionary<string, object?>? map,
            UiConfig defaults,
            ISeeLog? log)
        {
            return new UiConfig(
                ConfigFieldReader.ReadFloat(map, "lowSupplyHours", defaults.LowSupplyHours, MinLowSupplyHours, MaxLowSupplyHours, "ui", log),
                ConfigFieldReader.ReadBool(map, "showIdleBadge", defaults.ShowIdleBadge, "ui", log));
        }
    }
}