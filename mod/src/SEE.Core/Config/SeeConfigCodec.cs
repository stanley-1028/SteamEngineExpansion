using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Json;
using SEE.Core.Logging;

namespace SEE.Core.Config
{
    /// <summary>
    /// Reads and writes <c>SEE_Config.json</c> (config schema 1, requirements §20). Parsing never
    /// throws and never trusts the file: invalid JSON, wrong types, NaN, infinities and
    /// out-of-range values degrade to defaults or are clamped, with one
    /// <c>SEE/Warn/Config</c> warning per rejected value (REQ-SAFE-01).
    /// </summary>
    public static class SeeConfigCodec
    {
        /// <summary>Parses and validates a config document; any failure yields the compiled defaults.</summary>
        /// <param name="json">Raw JSON text; null/empty is treated as "use defaults".</param>
        /// <param name="log">Diagnostics sink; null is tolerated (tests).</param>
        /// <returns>A validated, immutable config.</returns>
        public static SeeConfig Parse(string json, ISeeLog? log)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                ConfigFieldReader.Warn(log, "config", "empty document; using defaults");
                return SeeConfig.Default;
            }

            try
            {
                if (MiniJson.Parse(json) is IDictionary<string, object?> root)
                {
                    return SeeConfigValidation.Parse(root, log);
                }

                ConfigFieldReader.Warn(log, "config", "root is not an object; using defaults");
                return SeeConfig.Default;
            }
            catch (Exception exception)
            {
                // Defensive: a malformed file must never break mod startup (REQ-SAFE-01).
                ConfigFieldReader.Warn(log, "config", "invalid JSON (" + exception.GetType().Name + "); using defaults");
                return SeeConfig.Default;
            }
        }

        /// <summary>Serializes a config to the canonical schema 1 document (stable key order).</summary>
        /// <param name="config">Validated config to write.</param>
        /// <returns>Compact JSON text.</returns>
        public static string Serialize(SeeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var root = new Dictionary<string, object?>
            {
                ["configSchemaVersion"] = config.ConfigSchemaVersion,
                ["balance"] = SerializeBalance(config.Balance),
                ["maintenance"] = SerializeMaintenance(config.Maintenance),
                ["compat"] = SerializeCompat(config.Compat),
                ["perf"] = SerializePerf(config.Perf),
                ["anim"] = SerializeAnim(config.Anim),
                ["art"] = SerializeArt(config.Art),
                ["ui"] = SerializeUi(config.Ui)
            };
            return MiniJson.Write(root);
        }

        /// <summary>
        /// Serializes a config back to the canonical schema 1 document while preserving unknown
        /// properties from <paramref name="originalJson"/> (CONTRACT_SEE §7.2: "未知欄位保留").
        /// Known keys always win; malformed or absent source text simply yields the canonical
        /// document. Never throws.
        /// </summary>
        /// <param name="config">Validated config to write.</param>
        /// <param name="originalJson">JSON the config was loaded from, or null.</param>
        /// <returns>Compact JSON text including the preserved unknown properties.</returns>
        public static string Serialize(SeeConfig config, string? originalJson)
        {
            string canonical = Serialize(config);
            if (string.IsNullOrWhiteSpace(originalJson))
            {
                return canonical;
            }

            try
            {
                if (!(MiniJson.Parse(originalJson!) is IDictionary<string, object?> source)
                    || !(MiniJson.Parse(canonical) is IDictionary<string, object?> target))
                {
                    return canonical;
                }

                ConfigUnknownFields.Merge(source, target);
                return MiniJson.Write(target);
            }
            catch (Exception)
            {
                // A malformed source document must never break the write-back (REQ-SAFE-01).
                return canonical;
            }
        }

        private static Dictionary<string, object?> SerializeBalance(BalanceConfig balance)
        {
            return new Dictionary<string, object?>
            {
                ["fuelRatePer100hp"] = ToKeyedObject(ConfigFieldReader.TierKeys, balance.FuelRatePer100Hp),
                ["waterRatePer100hp"] = ToKeyedObject(ConfigFieldReader.TierKeys, balance.WaterRatePer100Hp),
                ["faction"] = new Dictionary<string, object?>
                {
                    ["outputMultiplier"] = new Dictionary<string, object?>
                    {
                        ["Folktails"] = balance.FactionOutputMultiplierFolktails,
                        ["IronTeeth"] = balance.FactionOutputMultiplierIronTeeth
                    },
                    ["fuelMultiplier"] = new Dictionary<string, object?>
                    {
                        ["Folktails"] = balance.FactionFuelMultiplierFolktails,
                        ["IronTeeth"] = balance.FactionFuelMultiplierIronTeeth
                    }
                },
                ["fullInventoryWorkHours"] = balance.FullInventoryWorkHours,
                ["waterTankHours"] = balance.WaterTankHours,
                ["fuelScaling"] = balance.FuelScaling.ToString(),
                ["standbyLoadRatio"] = balance.StandbyLoadRatio,
                ["buildCostMultiplier"] = balance.BuildCostMultiplier
            };
        }

        private static Dictionary<string, object?> SerializeMaintenance(MaintenanceConfig maintenance)
        {
            return new Dictionary<string, object?>
            {
                ["enabled"] = maintenance.Enabled,
                ["intervalDays"] = ToKeyedObject(ConfigFieldReader.TierKeys, maintenance.IntervalDays),
                ["loadFactorBase"] = maintenance.LoadFactorBase,
                ["autoThreshold"] = maintenance.AutoThreshold,
                ["manualWarnAbove"] = maintenance.ManualWarnAbove,
                ["durationHours"] = maintenance.DurationHours,
                ["manualMaterialsEnabled"] = maintenance.ManualMaterialsEnabled
            };
        }

        private static Dictionary<string, object?> SerializeCompat(CompatConfig compat)
        {
            return new Dictionary<string, object?>
            {
                ["waterSupplyMode"] = compat.WaterSupplyMode.ToString(),
                ["detectionBudgetSeconds"] = compat.DetectionBudgetSeconds,
                ["enablePowerLineInjection"] = compat.EnablePowerLineInjection,
                ["beltFuelMode"] = compat.BeltFuelMode.ToString(),
                ["logThrottleSeconds"] = compat.LogThrottleSeconds,
                ["pipePollutionWarn"] = compat.PipePollutionWarn
            };
        }

        private static Dictionary<string, object?> SerializePerf(PerfConfig perf)
        {
            return new Dictionary<string, object?>
            {
                ["tickSeconds"] = perf.TickSeconds,
                ["uiRefreshHz"] = perf.UiRefreshHz,
                ["badgeRefreshHz"] = perf.BadgeRefreshHz,
                ["maxBadgesVisible"] = perf.MaxBadgesVisible,
                ["lodDistances"] = ToObjects(perf.LodDistances)
            };
        }

        private static Dictionary<string, object?> SerializeAnim(AnimConfig anim)
        {
            return new Dictionary<string, object?>
            {
                ["enable"] = anim.Enable,
                ["updateHz"] = anim.UpdateHz,
                ["lod0MaxDistance"] = anim.Lod0MaxDistance,
                ["lod1MaxDistance"] = anim.Lod1MaxDistance,
                ["vfxEmissionScale"] = anim.VfxEmissionScale
            };
        }

        private static Dictionary<string, object?> SerializeArt(ArtConfig art)
        {
            return new Dictionary<string, object?>
            {
                ["trisLod0"] = ToKeyedObject(ConfigFieldReader.SizeKeys, art.TrisLod0),
                ["lod1Ratio"] = art.Lod1Ratio,
                ["lod2Ratio"] = art.Lod2Ratio,
                ["textureSize"] = new Dictionary<string, object?>
                {
                    ["Small"] = art.TextureSizeSmall,
                    ["Medium"] = art.TextureSizeLarge,
                    ["Large"] = art.TextureSizeLarge
                }
            };
        }

        private static Dictionary<string, object?> SerializeUi(UiConfig ui)
        {
            return new Dictionary<string, object?>
            {
                ["lowSupplyHours"] = ui.LowSupplyHours,
                ["showIdleBadge"] = ui.ShowIdleBadge
            };
        }

        private static Dictionary<string, object?> ToKeyedObject(string[] keys, IReadOnlyList<float> values)
        {
            var table = new Dictionary<string, object?>(keys.Length);
            for (int i = 0; i < keys.Length && i < values.Count; i++)
            {
                table[keys[i]] = values[i];
            }

            return table;
        }

        private static Dictionary<string, object?> ToKeyedObject(string[] keys, IReadOnlyList<int> values)
        {
            var table = new Dictionary<string, object?>(keys.Length);
            for (int i = 0; i < keys.Length && i < values.Count; i++)
            {
                table[keys[i]] = values[i];
            }

            return table;
        }

        private static List<object?> ToObjects(IReadOnlyList<float> values)
        {
            var list = new List<object?>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                list.Add(values[i]);
            }

            return list;
        }

        private static List<object?> ToObjects(IReadOnlyList<int> values)
        {
            var list = new List<object?>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                list.Add(values[i]);
            }

            return list;
        }
    }
}