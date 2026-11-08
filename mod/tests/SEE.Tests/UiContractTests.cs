using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.Core.Abstractions;
using SEE.Core.Config;
using SEE.UI.Panels;
using SEE.UI.Status;

namespace SEE.Tests
{
    /// <summary>
    /// P4 UI contract tests (CONTRACT_SEE §5, requirements §15.2–§15.7, REQ-UI-01..12,
    /// REQ-OPS-01..04). They exercise only the pure UI data layer of <c>SEE.UI</c> - the same
    /// classes the UIElements fragment consumes - so the whole panel contract is verified offline
    /// without the game: status/priority mapping, every panel row, the frozen 54 localization
    /// keys, the 4 Hz refresh gate, the badge policy and the "no gameplay reference" rule.
    /// </summary>
    internal static class UiContractTests
    {
        private static readonly string[] FrozenLocKeys =
        {
            "Building.SEE.SteamBoiler.Small.T1.DisplayName",
            "Building.SEE.SteamBoiler.Medium.T1.DisplayName",
            "Building.SEE.SteamBoiler.Large.T1.DisplayName",
            "Building.SEE.SteamBoiler.Small.T2.DisplayName",
            "Building.SEE.SteamBoiler.Medium.T2.DisplayName",
            "Building.SEE.SteamBoiler.Large.T2.DisplayName",
            "Building.SEE.SteamBoiler.Small.T3.DisplayName",
            "Building.SEE.SteamBoiler.Medium.T3.DisplayName",
            "Building.SEE.SteamBoiler.Large.T3.DisplayName",
            "Building.SEE.SteamBoiler.Small.T4.DisplayName",
            "Building.SEE.SteamBoiler.Medium.T4.DisplayName",
            "Building.SEE.SteamBoiler.Large.T4.DisplayName",
            "Building.SEE.SteamBoiler.Description",
            "Building.SEE.SteamBoiler.Flavor.T1",
            "Building.SEE.SteamBoiler.Flavor.T2",
            "Building.SEE.SteamBoiler.Flavor.T3",
            "Building.SEE.SteamBoiler.Flavor.T4",
            "Status.SEE.Running",
            "Status.SEE.Idle",
            "Status.SEE.Paused",
            "Status.SEE.NoFuel",
            "Status.SEE.NoWater",
            "Status.SEE.NeedsMaintenance",
            "Status.SEE.Critical",
            "Status.SEE.Worn",
            "Status.SEE.MaintenanceDue",
            "Status.SEE.Maintaining",
            "Status.SEE.MissingMaintenanceMaterials",
            "Status.SEE.NoAccessForMaintenance",
            "Status.SEE.LowFuel",
            "Status.SEE.LowWater",
            "Status.SEE.PipeSupply",
            "Status.SEE.BeltSupply",
            "Status.SEE.BeltNoPower",
            "Status.SEE.PipeContaminated",
            "Status.SEE.WaterMode.Tank",
            "Status.SEE.WaterMode.Valve",
            "Status.SEE.WaterMode.Hauler",
            "Status.SEE.WaterMode.Auto",
            "UI.SEE.Panel.Power",
            "UI.SEE.Panel.Network",
            "UI.SEE.Panel.Output",
            "UI.SEE.Panel.Fuel",
            "UI.SEE.Panel.Water",
            "UI.SEE.Panel.Condition",
            "UI.SEE.Panel.NextMaintenance",
            "UI.SEE.Panel.Materials",
            "UI.SEE.Panel.WaterSupplyMode",
            "UI.SEE.Panel.WaterBeaverEquivalent",
            "UI.SEE.Panel.Efficiency",
            "UI.SEE.Panel.MaterialsDetail",
            "UI.SEE.Panel.MaterialEntry",
            "UI.SEE.Panel.NextMaintenanceDay",
            "UI.SEE.Hint.FixNoFuel",
            "UI.SEE.Hint.FixNoWater",
            "UI.SEE.Hint.FixNeedsMaintenance",
            "UI.SEE.WaterModeUnavailable",
            "UI.SEE.Action.Pause",
            "UI.SEE.Action.Resume",
            "UI.SEE.Action.MaintainNow",
            "UI.SEE.Action.AutoMaintenance",
            "UI.SEE.Error.MaterialsMissing",
            "UI.SEE.Error.BeltNoPower"
        };

        private static readonly string[] SimplifiedCharacters =
        {
            "\u9505", "\u7ef4", "\u62a4", "\u53d1", "\u52a8", "\u9f7f", "\u8f6e", "\u538b"
        };

        /// <summary>
        /// Frozen keys the panel cannot paint yet because the read model does not expose the
        /// corresponding state: the four integration-supply hints (pipe supply, belt supply, belt
        /// power, pipe contamination; REQ-UI-03/§7) plus missing-material counts and conveyor
        /// power errors. They stay in the shipped CSV (CONTRACT_SEE §5.2) and are listed here
        /// explicitly so the packaged-vs-used diff remains empty and honest.
        ///
        /// P4B keeps <c>UI.SEE.Panel.NextMaintenance</c> / <c>UI.SEE.Panel.Materials</c> in the
        /// shipped set for compatibility, but the panel now uses the detail rows
        /// <c>UI.SEE.Panel.NextMaintenanceDay</c> / <c>UI.SEE.Panel.MaterialsDetail</c>.</summary>
        private static readonly string[] ReservedLocKeys =
        {
            "Status.SEE.PipeSupply",
            "Status.SEE.BeltSupply",
            "Status.SEE.BeltNoPower",
            "Status.SEE.PipeContaminated",
            "UI.SEE.Error.MaterialsMissing",
            "UI.SEE.Error.BeltNoPower",
            "UI.SEE.Panel.NextMaintenance",
            "UI.SEE.Panel.Materials"
        };

        internal static void Run(TestRunner t)
        {
            LocalizationTests(t);
            StatusDescriptorTests(t);
            PanelModelTests(t);
            PanelStateTests(t);
            BadgeTests(t);
            RefreshPolicyTests(t);
            OptionsTests(t);
            SourceScanTests(t);
        }

        // ------------------------------------------------------------------ localization

        private static void LocalizationTests(TestRunner t)
        {
            t.Run("enUS and zhTW ship exactly the canonical SEE localization keys (63: 54 baseline + 9 P4B)", () =>
            {
                var expected = new HashSet<string>(FrozenLocKeys, StringComparer.Ordinal);
                Assert.Equal(63, expected.Count, "frozen key count");
                HashSet<string> enUs = ReadLocKeys("enUS.csv");
                HashSet<string> zhTw = ReadLocKeys("zhTW.csv");
                AssertSetEqual(expected, enUs, "enUS key set");
                AssertSetEqual(expected, zhTw, "zhTW key set");
            });

            t.Run("enUS and zhTW define the same SEE key set with the same placeholders", () =>
            {
                Dictionary<string, string> enUs = ReadLocTexts("enUS.csv");
                Dictionary<string, string> zhTw = ReadLocTexts("zhTW.csv");
                foreach (var pair in enUs)
                {
                    Assert.True(zhTw.ContainsKey(pair.Key), "zhTW missing key " + pair.Key);
                    Assert.Equal(Placeholders(pair.Value), Placeholders(zhTw[pair.Key]),
                        "placeholder mismatch for " + pair.Key);
                }
            });

            t.Run("Every localization key the UI can reference exists in both locales", () =>
            {
                HashSet<string> used = UsedLocKeys();
                HashSet<string> enUs = ReadLocKeys("enUS.csv");
                HashSet<string> zhTw = ReadLocKeys("zhTW.csv");
                foreach (string key in used)
                {
                    Assert.True(enUs.Contains(key), "UI key missing from enUS: " + key);
                    Assert.True(zhTw.Contains(key), "UI key missing from zhTW: " + key);
                }
            });

            t.Run("The shipped SEE keys are exactly UI keys plus blueprint label keys (diff empty)", () =>
            {
                HashSet<string> usedByUi = UsedLocKeys();
                HashSet<string> usedByBlueprints = ReadBlueprintLocKeys();
                var combined = new HashSet<string>(usedByUi, StringComparer.Ordinal);
                combined.UnionWith(usedByBlueprints);
                combined.UnionWith(ReservedLocKeys);
                HashSet<string> packaged = ReadLocKeys("enUS.csv");
                AssertSetEqual(combined, packaged, "packaged keys vs (UI + blueprint label + reserved keys)");
            });

            t.Run("All UI keys use only the three allowed SER prefixes", () =>
            {
                foreach (string key in UsedLocKeys())
                {
                    bool allowed = key.StartsWith("Building.SEE.", StringComparison.Ordinal)
                                   || key.StartsWith("Status.SEE.", StringComparison.Ordinal)
                                   || key.StartsWith("UI.SEE.", StringComparison.Ordinal);
                    Assert.True(allowed, "key outside the frozen prefixes: " + key);
                    Assert.False(key.Contains("SteamEngine", StringComparison.Ordinal),
                        "forbidden legacy prefix in " + key);
                    Assert.False(key.Contains("SteamPlant", StringComparison.Ordinal),
                        "forbidden legacy prefix in " + key);
                }
            });

            t.Run("zhTW contains no simplified characters", () =>
            {
                Dictionary<string, string> zhTw = ReadLocTexts("zhTW.csv");
                foreach (var pair in zhTw)
                {
                    foreach (string simplified in SimplifiedCharacters)
                    {
                        Assert.False(pair.Value.Contains(simplified, StringComparison.Ordinal),
                            "simplified character in zhTW key " + pair.Key);
                    }
                }
            });
        }

        private static HashSet<string> ReadLocKeys(string file)
        {
            return new HashSet<string>(ReadLocTexts(file).Keys, StringComparer.Ordinal);
        }

        private static Dictionary<string, string> ReadLocTexts(string file)
        {
            string path = TestPaths.FromMod("data", "Localizations", file);
            Assert.True(File.Exists(path), "missing localization file " + path);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.Length == 0 || line.StartsWith("ID,", StringComparison.Ordinal))
                {
                    continue;
                }

                int comma = line.IndexOf(',');
                if (comma <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, comma);
                if (!key.StartsWith("Building.SEE.", StringComparison.Ordinal)
                    && !key.StartsWith("Status.SEE.", StringComparison.Ordinal)
                    && !key.StartsWith("UI.SEE.", StringComparison.Ordinal))
                {
                    continue;
                }

                result[key] = line.Substring(comma + 1);
            }

            return result;
        }

        private static string Placeholders(string text)
        {
            var found = new List<string>();
            for (int i = 0; i < text.Length - 2; i++)
            {
                if (text[i] == '{' && char.IsDigit(text[i + 1]) && text[i + 2] == '}')
                {
                    found.Add(text.Substring(i, 3));
                }
            }

            found.Sort(StringComparer.Ordinal);
            return string.Join(",", found);
        }

        /// <summary>Every frozen key the panel code can put on screen.</summary>
        private static HashSet<string> UsedLocKeys()
        {
            var keys = new HashSet<string>(StringComparer.Ordinal)
            {
                StatusDescriptors.DisplayNameLocKey(BoilerSize.Small, BoilerTier.T1),
                StatusDescriptors.DisplayNameLocKey(BoilerSize.Medium, BoilerTier.T2),
                StatusDescriptors.DisplayNameLocKey(BoilerSize.Large, BoilerTier.T3),
                StatusDescriptors.DisplayNameLocKey(BoilerSize.Small, BoilerTier.T4),
                SEE.Core.Constants.SeeLocKeys.Description,
                SEE.Core.Constants.SeeLocKeys.UiPanelPower,
                SEE.Core.Constants.SeeLocKeys.UiPanelNetwork,
                SEE.Core.Constants.SeeLocKeys.UiPanelOutput,
                SEE.Core.Constants.SeeLocKeys.UiPanelFuel,
                SEE.Core.Constants.SeeLocKeys.UiPanelWater,
                SEE.Core.Constants.SeeLocKeys.UiPanelCondition,
                SEE.Core.Constants.SeeLocKeys.UiPanelNextMaintenanceDay,
                SEE.Core.Constants.SeeLocKeys.UiPanelMaterialsDetail,
                SEE.Core.Constants.SeeLocKeys.UiPanelMaterialEntry,
                SEE.Core.Constants.SeeLocKeys.UiPanelEfficiency,
                SEE.Core.Constants.SeeLocKeys.UiPanelWaterBeaverEquivalent,
                SEE.Core.Constants.SeeLocKeys.UiHintFixNoFuel,
                SEE.Core.Constants.SeeLocKeys.UiHintFixNoWater,
                SEE.Core.Constants.SeeLocKeys.UiHintFixNeedsMaintenance,
                SEE.Core.Constants.SeeLocKeys.UiWaterModeUnavailable,
                SEE.Core.Constants.SeeLocKeys.UiPanelWaterSupplyMode,
                SEE.Core.Constants.SeeLocKeys.UiActionPause,
                SEE.Core.Constants.SeeLocKeys.UiActionResume,
                SEE.Core.Constants.SeeLocKeys.UiActionMaintainNow,
                SEE.Core.Constants.SeeLocKeys.UiActionAutoMaintenance
            };

            foreach (BoilerSize size in Enum.GetValues(typeof(BoilerSize)))
            {
                foreach (BoilerTier tier in Enum.GetValues(typeof(BoilerTier)))
                {
                    keys.Add(StatusDescriptors.DisplayNameLocKey(size, tier));
                }
            }

            foreach (BoilerStatus status in Enum.GetValues(typeof(BoilerStatus)))
            {
                keys.Add(StatusDescriptors.StatusLocKey(status));
            }

            foreach (MaintenancePhase phase in Enum.GetValues(typeof(MaintenancePhase)))
            {
                string key = StatusDescriptors.MaintenanceLocKey(phase);
                if (key.Length > 0)
                {
                    keys.Add(key);
                }
            }

            foreach (WaterSupplyMode mode in Enum.GetValues(typeof(WaterSupplyMode)))
            {
                keys.Add(StatusDescriptors.WaterSupplyModeLocKey(mode));
            }

            keys.Add(SEE.Core.Constants.SeeLocKeys.StatusLowFuel);
            keys.Add(SEE.Core.Constants.SeeLocKeys.StatusLowWater);
            return keys;
        }

        /// <summary>Localization keys the generated blueprints reference (label section B).</summary>
        private static HashSet<string> ReadBlueprintLocKeys()
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            string folder = TestPaths.FromMod("data", "Buildings", "Power", "SteamBoiler");
            foreach (string file in Directory.GetFiles(folder, "*.blueprint.json"))
            {
                string text = File.ReadAllText(file);
                int index = 0;
                while ((index = text.IndexOf("Building.SEE.", index, StringComparison.Ordinal)) >= 0)
                {
                    int end = index;
                    while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '.'))
                    {
                        end++;
                    }

                    keys.Add(text.Substring(index, end - index));
                    index = end;
                }
            }

            return keys;
        }

        private static void AssertSetEqual(HashSet<string> expected, HashSet<string> actual, string label)
        {
            var missing = expected.Where(key => !actual.Contains(key)).ToList();
            var extra = actual.Where(key => !expected.Contains(key)).ToList();
            Assert.True(missing.Count == 0, label + ": missing " + string.Join(", ", missing));
            Assert.True(extra.Count == 0, label + ": unexpected " + string.Join(", ", extra));
        }

        /// <summary>Builds a panel for one status and returns the repair-guidance key.</summary>
        private static string FixHintFor(BoilerStatus status) =>
            BoilerPanelModelBuilder.Build(new FakeBoilerView { Status = status }, BoilerPanelOptions.Default).FixHintKey;

        // ------------------------------------------------------------------ status descriptors

        private static void StatusDescriptorTests(TestRunner t)
        {
            t.Run("The nine main states plus Maintaining map to the frozen status keys in priority order", () =>
            {
                Assert.Equal("Status.SEE.Paused", StatusDescriptors.StatusLocKey(BoilerStatus.Paused), "paused");
                Assert.Equal("Status.SEE.Maintaining", StatusDescriptors.StatusLocKey(BoilerStatus.Maintaining), "maintaining");
                Assert.Equal("Status.SEE.NoFuel", StatusDescriptors.StatusLocKey(BoilerStatus.NoFuel), "no fuel");
                Assert.Equal("Status.SEE.NoWater", StatusDescriptors.StatusLocKey(BoilerStatus.NoWater), "no water");
                Assert.Equal("Status.SEE.NeedsMaintenance", StatusDescriptors.StatusLocKey(BoilerStatus.NeedsMaintenance),
                    "needs maintenance");
                Assert.Equal("Status.SEE.Critical", StatusDescriptors.StatusLocKey(BoilerStatus.Critical), "critical");
                Assert.Equal("Status.SEE.Worn", StatusDescriptors.StatusLocKey(BoilerStatus.Worn), "worn");
                Assert.Equal("Status.SEE.MaintenanceDue", StatusDescriptors.StatusLocKey(BoilerStatus.MaintenanceDue),
                    "maintenance due");
                Assert.Equal("Status.SEE.Idle", StatusDescriptors.StatusLocKey(BoilerStatus.Idle), "idle");
                Assert.Equal("Status.SEE.Running", StatusDescriptors.StatusLocKey(BoilerStatus.Running), "running");
                Assert.Equal(0, StatusDescriptors.BadgePriority(BoilerStatus.Paused), "paused priority");
                Assert.Equal(9, StatusDescriptors.BadgePriority(BoilerStatus.Running), "running priority");
            });

            t.Run("Status colour bands follow requirements 15.3", () =>
            {
                Assert.Equal(StatusColorBand.Grey, StatusDescriptors.StatusColor(BoilerStatus.Paused), "paused");
                Assert.Equal(StatusColorBand.Orange, StatusDescriptors.StatusColor(BoilerStatus.NoFuel), "no fuel");
                Assert.Equal(StatusColorBand.Blue, StatusDescriptors.StatusColor(BoilerStatus.NoWater), "no water");
                Assert.Equal(StatusColorBand.Red, StatusDescriptors.StatusColor(BoilerStatus.NeedsMaintenance),
                    "needs maintenance");
                Assert.Equal(StatusColorBand.Red, StatusDescriptors.StatusColor(BoilerStatus.Critical), "critical");
                Assert.Equal(StatusColorBand.Orange, StatusDescriptors.StatusColor(BoilerStatus.Worn), "worn");
                Assert.Equal(StatusColorBand.Yellow, StatusDescriptors.StatusColor(BoilerStatus.MaintenanceDue),
                    "maintenance due");
                Assert.Equal(StatusColorBand.Grey, StatusDescriptors.StatusColor(BoilerStatus.Idle), "idle");
                Assert.Equal(StatusColorBand.Neutral, StatusDescriptors.StatusColor(BoilerStatus.Running), "running");
            });

            t.Run("Only Maintaining carries the {0} argument", () =>
            {
                foreach (BoilerStatus status in Enum.GetValues(typeof(BoilerStatus)))
                {
                    Assert.Equal(status == BoilerStatus.Maintaining, StatusDescriptors.StatusHasArgument(status),
                        "argument flag for " + status);
                }
            });

            t.Run("Maintenance phases map to reason keys; MissingMaterials and NoAccess are reasons", () =>
            {
                Assert.Equal(string.Empty, StatusDescriptors.MaintenanceLocKey(MaintenancePhase.None), "none");
                Assert.Equal("Status.SEE.Maintaining", StatusDescriptors.MaintenanceLocKey(MaintenancePhase.Maintaining),
                    "maintaining");
                Assert.Equal("Status.SEE.MissingMaintenanceMaterials",
                    StatusDescriptors.MaintenanceLocKey(MaintenancePhase.MissingMaterials), "missing materials");
                Assert.Equal("Status.SEE.NoAccessForMaintenance",
                    StatusDescriptors.MaintenanceLocKey(MaintenancePhase.NoAccess), "no access");
                Assert.False(StatusDescriptors.IsMaintenanceReason(MaintenancePhase.None), "none is not a reason");
                Assert.True(StatusDescriptors.IsMaintenanceReason(MaintenancePhase.MissingMaterials), "materials reason");
                Assert.True(StatusDescriptors.IsMaintenanceReason(MaintenancePhase.NoAccess), "access reason");
            });

            t.Run("The four water supply modes keep their keys and display index", () =>
            {
                Assert.Equal("Status.SEE.WaterMode.Auto", StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Auto), "auto");
                Assert.Equal("Status.SEE.WaterMode.Tank", StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Tank), "tank");
                Assert.Equal("Status.SEE.WaterMode.Valve", StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Valve), "valve");
                Assert.Equal("Status.SEE.WaterMode.Hauler", StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Hauler), "hauler");
                for (int index = 0; index < 4; index++)
                {
                    WaterSupplyMode mode = StatusDescriptors.WaterSupplyModeAt(index);
                    Assert.Equal(index, StatusDescriptors.WaterSupplyModeIndex(mode), "mode index " + index);
                }
            });
        }

        // ------------------------------------------------------------------ panel model

        private static void PanelModelTests(TestRunner t)
        {
            t.Run("The running boiler panel exposes every information block", () =>
            {
                var view = new FakeBoilerView();
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                Assert.True(model.HasData, "finished boiler has data");
                Assert.Equal("Building.SEE.SteamBoiler.Medium.T2.DisplayName", model.TitleKey, "title");
                Assert.Equal("Building.SEE.SteamBoiler.Description", model.TitleTooltipKey, "title tooltip");
                Assert.Equal("Status.SEE.Running", model.StatusKey, "status");
                Assert.Equal("UI.SEE.Panel.Power", model.PowerHeaderKey, "power header");
                Assert.Equal("UI.SEE.Panel.Fuel", model.FuelHeaderKey, "fuel header");
                Assert.Equal("UI.SEE.Panel.Water", model.WaterHeaderKey, "water header");
                Assert.Equal("UI.SEE.Panel.Condition", model.ConditionKey, "condition");
                Assert.Equal("UI.SEE.Panel.Efficiency", model.EfficiencyKey, "efficiency");
                Assert.Equal("UI.SEE.Panel.NextMaintenanceDay", model.NextMaintenanceKey, "next maintenance day");
                Assert.Equal("UI.SEE.Panel.MaterialsDetail", model.MaterialsKey, "materials detail");
                Assert.Equal("UI.SEE.Panel.WaterSupplyMode", model.WaterModeHeaderKey, "water mode header");
                Assert.Equal("Status.SEE.WaterMode.Auto", model.WaterModeKey, "water mode");
            });

            t.Run("Power and network rows use the actual / rated / supply / demand values", () =>
            {
                var view = new FakeBoilerView { ActualOutput = 391, RatedHorsepower = 460, NetworkSupplyHp = 3120, NetworkDemandHp = 2400 };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                Assert.Equal("UI.SEE.Panel.Output", model.OutputKey, "output key");
                Assert.Equal("391", model.OutputActual, "actual output");
                Assert.Equal("460", model.OutputRated, "rated output");
                Assert.Equal("UI.SEE.Panel.Network", model.NetworkKey, "network key");
                Assert.Equal("3120", model.NetworkSupply, "supply");
                Assert.Equal("2400", model.NetworkDemand, "demand");
            });

            t.Run("Fuel and water rows show stock, capacity, rate, hours and m3", () =>
            {
                var view = new FakeBoilerView { FuelInStock = 3, FuelCapacity = 3, FuelPerHour = 0.11f, WaterInStock = 64, WaterCapacity = 64, WaterPerHour = 0.32f };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                Assert.Equal("3 / 3   0.11/h   27.3 h", model.FuelDetail, "fuel detail");
                Assert.Equal("64 / 64   12.8 m\u00b3   0.32/h   200.0 h", model.WaterDetail, "water detail");
                Assert.Equal("UI.SEE.Panel.WaterBeaverEquivalent", model.WaterBeaverKey, "beaver key");
                Assert.Equal("3.6", model.WaterBeaverText, "0.32*24/2.12");
            });

            t.Run("G1: the beaver-drinking line uses the read-view value when present", () =>
            {
                var view = new FakeBoilerView { WaterBeaverEquivalent = 6.2f, WaterPerHour = 0.50f };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                Assert.Equal("UI.SEE.Panel.WaterBeaverEquivalent", model.WaterBeaverKey, "beaver key");
                Assert.Equal("6.2", model.WaterBeaverText, "read-view value wins over the fallback");

                var fallback = new FakeBoilerView { WaterPerHour = 0.50f };
                Assert.Equal("5.7", BoilerPanelModelBuilder.Build(fallback, BoilerPanelOptions.Default).WaterBeaverText,
                    "0.50*24/2.12 when the view cannot provide a value");
            });

            t.Run("G3: the power block shows the network efficiency as whole percent", () =>
            {
                var view = new FakeBoilerView { NetworkEfficiency = 0.987f };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                Assert.Equal("UI.SEE.Panel.Efficiency", model.EfficiencyKey, "efficiency key");
                Assert.Equal("99", model.EfficiencyText, "0.987 -> 99 %");
                Assert.Equal("0", BoilerPanelModelBuilder.Build(
                    new FakeBoilerView { NetworkEfficiency = float.NaN }, BoilerPanelOptions.Default).EfficiencyText,
                    "NaN degrades to 0");
            });

            t.Run("G4: repair guidance follows the blocking state, at most one line", () =>
            {
                Assert.Equal("UI.SEE.Hint.FixNoFuel", FixHintFor(BoilerStatus.NoFuel), "no fuel");
                Assert.Equal("UI.SEE.Hint.FixNoWater", FixHintFor(BoilerStatus.NoWater), "no water");
                Assert.Equal("UI.SEE.Hint.FixNeedsMaintenance", FixHintFor(BoilerStatus.NeedsMaintenance),
                    "needs maintenance");
                Assert.Equal("UI.SEE.Hint.FixNeedsMaintenance", FixHintFor(BoilerStatus.MaintenanceDue),
                    "maintenance due");
                Assert.Equal(string.Empty, FixHintFor(BoilerStatus.Running), "healthy boiler has no guidance");
                Assert.Equal(string.Empty, FixHintFor(BoilerStatus.Paused), "paused boiler has no guidance");
            });

            t.Run("G5: maintenance block shows material list, missing count and next day", () =>
            {
                var view = new FakeBoilerView
                {
                    NextMaintenanceDay = 42.47f,
                    MissingMaterialCount = 2,
                    MaintenanceMaterials = new[]
                    {
                        new MaintenanceMaterialStatus("Log", 8, 3),
                        new MaintenanceMaterialStatus("Plank", 12, 0)
                    }
                };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                Assert.Equal("UI.SEE.Panel.NextMaintenanceDay", model.NextMaintenanceKey, "next day key");
                Assert.Equal("42.5", model.NextMaintenanceDays, "absolute day, one decimal");
                Assert.Equal("88", model.ConditionText, "condition 87.5 rounds away from zero");
                Assert.Equal(88, model.ConditionPercent, "condition percent");
                Assert.Equal("UI.SEE.Panel.MaterialsDetail", model.MaterialsKey, "materials key");
                Assert.Equal(2, model.MaterialLines.Length, "two material lines");
                Assert.Equal("Log 3/8, Plank 0/12", model.MaterialsText, "joined list");
                Assert.Equal("2", model.MissingMaterialsText, "two kinds short");
            });

            t.Run("G5: an unavailable read value shows the unknown glyph, never a raw key", () =>
            {
                var view = new FakeBoilerView { NextMaintenanceDay = 0f };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                Assert.Equal(PanelNumbers.Unknown, model.NextMaintenanceDays, "unknown day");
                Assert.Equal(PanelNumbers.Unknown, model.MaterialsText, "unknown material list");
                Assert.Equal(PanelNumbers.Unknown, model.MissingMaterialsText, "unknown missing count");
                Assert.Equal("UI.SEE.Panel.MaterialsDetail", model.MaterialsKey, "frozen key is used");
            });

            t.Run("Maintenance disabled by config hides the whole maintenance block", () =>
            {
                var view = new FakeBoilerView { MaintenancePhase = MaintenancePhase.MissingMaterials };
                var options = new BoilerPanelOptions(maintenanceEnabled: false);
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, options);
                Assert.False(model.MaintenanceVisible, "maintenance block hidden");
                Assert.Equal(string.Empty, model.SubStatusKey, "no reason line while disabled");
                Assert.Equal(string.Empty, model.FixHintKey, "no repair guidance while maintenance is off");
            });

            t.Run("The selected water mode reports why it cannot run", () =>
            {
                var view = new FakeBoilerView
                {
                    WaterSupplyMode = WaterSupplyMode.Tank,
                    WaterSupplyAvailability = WaterSupplyAvailability.TimberPipesMissing
                };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                Assert.Equal("UI.SEE.WaterModeUnavailable", model.WaterModeUnavailableKey, "warning key");
                Assert.Equal("Status.SEE.WaterMode.Tank", model.WaterModeUnavailableArgumentKey, "mode argument");
                Assert.Equal(string.Empty, BoilerPanelModelBuilder.Build(
                    new FakeBoilerView(), BoilerPanelOptions.Default).WaterModeUnavailableKey,
                    "available mode shows no warning");
            });

            t.Run("A NaN / infinite view degrades instead of leaking numbers", () =>
            {
                var view = new FakeBoilerView
                {
                    Condition = float.NaN,
                    FuelPerHour = float.PositiveInfinity,
                    WaterPerHour = float.NaN,
                    FuelInStock = -5,
                    ActualOutput = -1
                };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                Assert.Equal("0", model.ConditionText, "NaN condition");
                Assert.Equal("0", model.OutputActual, "negative output");
                Assert.False(model.FuelDetail.Contains("NaN", StringComparison.Ordinal), "no NaN in fuel row");
                Assert.False(model.WaterDetail.Contains("NaN", StringComparison.Ordinal), "no NaN in water row");
                Assert.False(model.NextMaintenanceDays.Contains("NaN", StringComparison.Ordinal), "no NaN in countdown");
            });

            t.Run("An unfinished building has no panel", () =>
            {
                var view = new FakeBoilerView { IsFinished = false };
                Assert.False(BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default).HasData, "unfinished hides");
                Assert.False(BoilerPanelModelBuilder.Build(null, BoilerPanelOptions.Default).HasData, "null hides");
            });
        }

        // ------------------------------------------------------------------ panel states

        private static void PanelStateTests(TestRunner t)
        {
            t.Run("Paused shows the resume action and the paused badge", () =>
            {
                var view = new FakeBoilerView { IsPaused = true, Status = BoilerStatus.Paused };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                Assert.Equal("Status.SEE.Paused", model.StatusKey, "status");
                Assert.Equal("UI.SEE.Action.Resume", model.PauseKey, "resume label");
                Assert.True(model.BadgeVisible, "paused badge");
                Assert.Equal(0, model.BadgePriority, "highest priority");
            });

            t.Run("Maintaining shows the remaining hours and blocks the maintain button", () =>
            {
                var view = new FakeBoilerView
                {
                    Status = BoilerStatus.Maintaining,
                    MaintenancePhase = MaintenancePhase.Maintaining,
                    MaintenanceRemainingHours = 1.5f
                };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                Assert.Equal("Status.SEE.Maintaining", model.StatusKey, "status");
                Assert.Equal("1.5", model.StatusArgument, "remaining hours");
                Assert.False(model.CanMaintain, "maintenance in progress");
            });

            t.Run("Missing materials / no access show the reason and disable the button", () =>
            {
                var materials = BoilerPanelModelBuilder.Build(
                    new FakeBoilerView { Status = BoilerStatus.NeedsMaintenance, MaintenancePhase = MaintenancePhase.MissingMaterials },
                    BoilerPanelOptions.Default);
                Assert.Equal("Status.SEE.MissingMaintenanceMaterials", materials.SubStatusKey, "materials reason");
                Assert.False(materials.CanMaintain, "materials block the button");
                var access = BoilerPanelModelBuilder.Build(
                    new FakeBoilerView { Status = BoilerStatus.NeedsMaintenance, MaintenancePhase = MaintenancePhase.NoAccess },
                    BoilerPanelOptions.Default);
                Assert.Equal("Status.SEE.NoAccessForMaintenance", access.SubStatusKey, "access reason");
                Assert.False(access.CanMaintain, "no access blocks the button");
            });

            t.Run("A perfect-condition boiler disables the manual maintenance (C-5)", () =>
            {
                var view = new FakeBoilerView { Condition = 100f };
                Assert.False(BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default).CanMaintain, "condition 100");
                var worn = new FakeBoilerView { Condition = 99f };
                Assert.True(BoilerPanelModelBuilder.Build(worn, BoilerPanelOptions.Default).CanMaintain, "condition 99");
            });

            t.Run("Low fuel / low water hints appear below ui.lowSupplyHours, at most two", () =>
            {
                var both = new FakeBoilerView { FuelInStock = 1, FuelPerHour = 0.5f, WaterInStock = 1, WaterPerHour = 0.5f };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(both, BoilerPanelOptions.Default);
                Assert.Equal(2, model.Hints.Length, "two hints");
                Assert.Equal("Status.SEE.LowFuel", model.Hints[0].LocKey, "fuel first");
                Assert.Equal("Status.SEE.LowWater", model.Hints[1].LocKey, "water second");
                Assert.Equal("2.0", model.Hints[0].Argument, "1/0.5");
                var safe = new FakeBoilerView { FuelInStock = 100, WaterInStock = 100 };
                Assert.Equal(0, BoilerPanelModelBuilder.Build(safe, BoilerPanelOptions.Default).Hints.Length, "no hints");
            });

            t.Run("NoFuel does not add a redundant low-fuel hint", () =>
            {
                var view = new FakeBoilerView { Status = BoilerStatus.NoFuel, FuelInStock = 0 };
                BoilerPanelModel model = BoilerPanelModelBuilder.Build(view, BoilerPanelOptions.Default);
                foreach (PanelHint hint in model.Hints)
                {
                    Assert.False(hint.LocKey == "Status.SEE.LowFuel", "low fuel hint duplicates NoFuel");
                }
            });

            t.Run("Badge visibility follows the frozen priority and the idle default", () =>
            {
                Assert.False(BoilerBadgePolicy.IsVisible(BoilerStatus.Running, true), "running shows no badge");
                Assert.False(BoilerBadgePolicy.IsVisible(BoilerStatus.Idle, false), "idle hidden by default");
                Assert.True(BoilerBadgePolicy.IsVisible(BoilerStatus.Idle, true), "idle shown when configured");
                Assert.True(BoilerBadgePolicy.IsVisible(BoilerStatus.Critical, false), "critical always shown");
                Assert.Equal("Status.SEE.Critical", BoilerBadgePolicy.LocKey(BoilerStatus.Critical, false), "badge key");
                Assert.Equal(string.Empty, BoilerBadgePolicy.LocKey(BoilerStatus.Running, false), "running key");
            });
        }

        // ------------------------------------------------------------------ badge policy

        private static void BadgeTests(TestRunner t)
        {
            t.Run("The world badge runs at badgeRefreshHz, clamped to 0.5..2 Hz", () =>
            {
                Assert.Near(1.0, BoilerBadgePolicy.RefreshIntervalSeconds(1f), 1e-6, "1 Hz");
                Assert.Near(1.0 / 0.67, BoilerBadgePolicy.RefreshIntervalSeconds(0.67f), 1e-3, "0.67 Hz in range");
                Assert.Near(2.0, BoilerBadgePolicy.RefreshIntervalSeconds(0.2f), 1e-6, "clamps to 0.5 Hz");
                Assert.Near(0.5, BoilerBadgePolicy.RefreshIntervalSeconds(10f), 1e-6, "10 Hz clamps to 2 Hz");
                Assert.Near(1.0, BoilerBadgePolicy.RefreshIntervalSeconds(float.NaN), 1e-6, "NaN falls back to 1 Hz");
            });

            t.Run("perf.maxBadgesVisible clamps to 16..256", () =>
            {
                Assert.Equal(64, BoilerBadgePolicy.VisibleLimit(64), "default");
                Assert.Equal(16, BoilerBadgePolicy.VisibleLimit(1), "minimum");
                Assert.Equal(256, BoilerBadgePolicy.VisibleLimit(999), "maximum");
                Assert.Equal(64, BoilerBadgePolicy.VisibleLimit(0), "zero falls back to default");
            });

            t.Run("Badge pass scheduling is deterministic", () =>
            {
                Assert.False(BoilerBadgePolicy.IsDue(0.4f, 1f), "not due");
                Assert.True(BoilerBadgePolicy.IsDue(1f, 1f), "due at the boundary");
                Assert.Near(2.0, BoilerBadgePolicy.NextPassTime(1f, 1f), 1e-6, "next pass");
            });
        }

        // ------------------------------------------------------------------ refresh policy

        private static void RefreshPolicyTests(TestRunner t)
        {
            t.Run("The panel refresh is capped at 4 Hz (250 ms) and clamped to 1..10 Hz", () =>
            {
                Assert.Near(0.25, BoilerPanelRefreshPolicy.IntervalSeconds(4f), 1e-6, "4 Hz -> 250 ms");
                Assert.Near(1.0, BoilerPanelRefreshPolicy.IntervalSeconds(0.1f), 1e-6, "floor 1 Hz");
                Assert.Near(0.1, BoilerPanelRefreshPolicy.IntervalSeconds(100f), 1e-6, "ceiling 10 Hz");
                Assert.Near(0.25, BoilerPanelRefreshPolicy.IntervalSeconds(float.NaN), 1e-6, "NaN -> default 4 Hz");
            });

            t.Run("A closed panel never queries", () =>
            {
                Assert.False(BoilerPanelRefreshPolicy.ShouldQuery(false, true), "closed panel with stale id");
                Assert.False(BoilerPanelRefreshPolicy.ShouldQuery(true, false), "open panel without id");
                Assert.True(BoilerPanelRefreshPolicy.ShouldQuery(true, true), "open panel with id");
            });

            t.Run("The 4 Hz gate allows at most one read per window", () =>
            {
                float interval = BoilerPanelRefreshPolicy.IntervalSeconds(4f);
                float next = BoilerPanelRefreshPolicy.NextAllowedTime(10f, interval);
                Assert.Near(10.25, next, 1e-6, "next window");
                Assert.False(BoilerPanelRefreshPolicy.IsDue(10.1f, next), "within the window");
                Assert.True(BoilerPanelRefreshPolicy.IsDue(10.25f, next), "window opened");
            });

            t.Run("The fingerprint changes exactly when a rendered value changes", () =>
            {
                var view = new FakeBoilerView();
                long baseline = BoilerPanelFingerprint.Compute(view, BoilerPanelOptions.Default);
                Assert.Equal(baseline, BoilerPanelFingerprint.Compute(new FakeBoilerView(), BoilerPanelOptions.Default),
                    "identical views share a fingerprint");
                view.Condition = 52f;
                long changed = BoilerPanelFingerprint.Compute(view, BoilerPanelOptions.Default);
                Assert.True(changed != baseline, "condition change is detected");
                view.WaterInStock = 63;
                Assert.True(BoilerPanelFingerprint.Compute(view, BoilerPanelOptions.Default) != changed, "water change");
                long afterWater = BoilerPanelFingerprint.Compute(view, BoilerPanelOptions.Default);
                view.NetworkEfficiency = 0.42f;
                long afterEfficiency = BoilerPanelFingerprint.Compute(view, BoilerPanelOptions.Default);
                Assert.True(afterEfficiency != afterWater, "efficiency change is detected");
                view.MaintenanceMaterials = new[] { new MaintenanceMaterialStatus("Log", 8, 3) };
                long afterMaterials = BoilerPanelFingerprint.Compute(view, BoilerPanelOptions.Default);
                Assert.True(afterMaterials != afterEfficiency, "material change is detected");
                view.MissingMaterialCount = 1;
                long afterMissing = BoilerPanelFingerprint.Compute(view, BoilerPanelOptions.Default);
                Assert.True(afterMissing != afterMaterials, "missing-count change is detected");
                view.WaterSupplyAvailability = WaterSupplyAvailability.TimberPipesMissing;
                Assert.True(BoilerPanelFingerprint.Compute(view, BoilerPanelOptions.Default) != afterMissing,
                    "availability change is detected");
            });
        }

        // ------------------------------------------------------------------ options

        private static void OptionsTests(TestRunner t)
        {
            t.Run("The shipped config projects onto the panel options", () =>
            {
                BoilerPanelOptions options = BoilerPanelOptions.FromConfig(SeeConfig.Default);
                Assert.Near(4.0, options.UiRefreshHz, 1e-6, "uiRefreshHz");
                Assert.Near(8.0, options.LowSupplyHours, 1e-6, "lowSupplyHours");
                Assert.False(options.ShowIdleBadge, "showIdleBadge default");
                Assert.True(options.MaintenanceEnabled, "maintenance enabled");
                Assert.Near(1.0, options.BadgeRefreshHz, 1e-6, "badgeRefreshHz");
                Assert.Equal(64, options.MaxBadgesVisible, "maxBadgesVisible");
                Assert.Near(6f, options.IntervalDaysFor(BoilerTier.T1), 1e-6, "T1 interval");
                Assert.Near(12f, options.IntervalDaysFor(BoilerTier.T4), 1e-6, "T4 interval");
                Assert.Equal("UI.SEE.Panel.WaterBeaverEquivalent", options.WaterBeaverEquivalentLocKey,
                    "P4B froze the beaver key");
            });

            t.Run("Out-of-range options clamp to the contract ranges", () =>
            {
                var options = new BoilerPanelOptions(uiRefreshHz: float.NaN, lowSupplyHours: -3f, maxBadgesVisible: 999);
                Assert.Near(4.0, options.UiRefreshHz, 1e-6, "NaN refresh");
                Assert.Near(2.0, options.LowSupplyHours, 1e-6, "low supply floor");
                Assert.Equal(256, options.MaxBadgesVisible, "badge ceiling");
                Assert.Near(2.12, options.BeaverWaterPerDay, 1e-6, "beaver default");
            });
        }

        // ------------------------------------------------------------------ source scan

        private static void SourceScanTests(TestRunner t)
        {
            t.Run("SEE.UI sources never mention the gameplay or integration assembly names", () =>
            {
                string root = TestPaths.FromMod("src", "SEE.UI");
                string[] forbidden = { "SEE." + "Gameplay", "SEE." + "Integration" };
                foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                        || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string text = File.ReadAllText(file);
                    foreach (string token in forbidden)
                    {
                        Assert.False(text.Contains(token, StringComparison.Ordinal),
                            "forbidden literal '" + token + "' in " + file);
                    }
                }
            });

            t.Run("The panel never formats strings while updating every frame", () =>
            {
                string fragment = File.ReadAllText(TestPaths.FromMod("src", "SEE.UI", "Panels", "BoilerPanelFragment.cs"));
                int updateStart = fragment.IndexOf("public void UpdateFragment()", StringComparison.Ordinal);
                Assert.True(updateStart >= 0, "UpdateFragment exists");
                int updateEnd = fragment.IndexOf("public void ClearFragment()", StringComparison.Ordinal);
                string body = fragment.Substring(updateStart, updateEnd - updateStart);
                Assert.False(body.Contains("_loc.T(", StringComparison.Ordinal), "no localization in UpdateFragment");
                Assert.False(body.Contains("string.Format", StringComparison.Ordinal), "no string.Format in UpdateFragment");
                Assert.False(body.Contains("+ \"", StringComparison.Ordinal), "no concatenation in UpdateFragment");
            });
        }

        /// <summary>
        /// Prints the four P4B panel rows exactly as the pure UI model builds them (acceptance
        /// evidence for G1/G3/G4/G5). Invoked with <c>--evidence</c>; runs no game code.
        /// </summary>
        internal static void PrintPanelEvidence()
        {
            BoilerPanelModel g1 = BoilerPanelModelBuilder.Build(
                new FakeBoilerView { WaterPerHour = 0.50f }, BoilerPanelOptions.Default);
            Console.WriteLine("G1 | " + g1.WaterBeaverKey + " | " + g1.WaterBeaverText);

            BoilerPanelModel g3 = BoilerPanelModelBuilder.Build(
                new FakeBoilerView { NetworkEfficiency = 0.987f }, BoilerPanelOptions.Default);
            Console.WriteLine("G3 | " + g3.EfficiencyKey + " | " + g3.EfficiencyText + "%");

            BoilerPanelModel g4 = BoilerPanelModelBuilder.Build(
                new FakeBoilerView { Status = BoilerStatus.NoFuel }, BoilerPanelOptions.Default);
            Console.WriteLine("G4 | " + g4.StatusKey + " -> " + g4.FixHintKey);

            BoilerPanelModel g5 = BoilerPanelModelBuilder.Build(new FakeBoilerView
            {
                NextMaintenanceDay = 42.47f,
                MissingMaterialCount = 2,
                MaintenanceMaterials = new[]
                {
                    new MaintenanceMaterialStatus("Log", 8, 3),
                    new MaintenanceMaterialStatus("Plank", 12, 0)
                }
            }, BoilerPanelOptions.Default);
            Console.WriteLine("G5 | " + g5.MaterialsKey + " | " + g5.MaterialsText
                              + " | " + g5.MissingMaterialsText + " | " + g5.NextMaintenanceKey
                              + " | day " + g5.NextMaintenanceDays);
        }

        /// <summary>Minimal settable read model used by the pure UI contract tests.</summary>
        private sealed class FakeBoilerView : IBoilerStatusView
        {
            public Guid EntityId { get; set; } = Guid.NewGuid();
            public BoilerStatus Status { get; set; } = BoilerStatus.Running;
            public MaintenancePhase MaintenancePhase { get; set; } = MaintenancePhase.None;
            public BoilerTier Tier { get; set; } = BoilerTier.T2;
            public BoilerSize Size { get; set; } = BoilerSize.Medium;
            public BoilerFaction Faction { get; set; } = BoilerFaction.Folktails;
            public bool IsFinished { get; set; } = true;
            public bool IsPaused { get; set; }
            public int RatedHorsepower { get; set; } = 460;
            public int ActualOutput { get; set; } = 430;
            public float Condition { get; set; } = 87.5f;
            public float ConditionMultiplier { get; set; } = 1f;
            public float LoadRatio { get; set; } = 0.9f;
            public int NetworkSupplyHp { get; set; } = 3120;
            public int NetworkDemandHp { get; set; } = 2400;
            public float NetworkEfficiency { get; set; } = 0.99f;
            public int FuelInStock { get; set; } = 3;
            public int FuelCapacity { get; set; } = 3;
            public int WaterInStock { get; set; } = 64;
            public int WaterCapacity { get; set; } = 64;
            public float FuelPerHour { get; set; } = 0.11f;
            public float WaterPerHour { get; set; } = 0.32f;
            public bool AutoMaintenance { get; set; } = true;
            public float MaintenanceRemainingHours { get; set; }
            public float LastMaintenanceDay { get; set; }
            public float MaintenanceTriggerThreshold { get; set; } = 50f;
            public WaterSupplyMode WaterSupplyMode { get; set; } = WaterSupplyMode.Auto;
            public bool PowerLineAttached { get; set; }
            public float WaterBeaverEquivalent { get; set; }
            public IReadOnlyList<string> FuelGoods { get; set; } = new[] { "Log" };
            public IReadOnlyList<MaintenanceMaterialStatus> MaintenanceMaterials { get; set; } =
                Array.Empty<MaintenanceMaterialStatus>();
            public float NextMaintenanceDay { get; set; }
            public int MissingMaterialCount { get; set; }
            public WaterSupplyAvailability WaterSupplyAvailability { get; set; } = WaterSupplyAvailability.Available;
            public bool PipeSupplyActive { get; set; }
            public bool BeltSupplyActive { get; set; }
            public bool BeltHasPower { get; set; } = true;
        }
    }
}
