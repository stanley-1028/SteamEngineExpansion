using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Json;
using SEE.Core.Logging;
using SEE.Core.Save;

namespace SEE.Tests
{
    /// <summary>
    /// L2 save contract tests of the gameplay save bridge (CONTRACT_SEE §6, D-6 hybrid form / AD-11):
    /// the per-entity component keeps the frozen seven fields, the world node keeps the four-field
    /// index plus compat block and checksum, and the gameplay key table matches the Core codec
    /// contract. All tests run offline against the pure Core codec plus source scanning.
    /// </summary>
    internal static class SaveBridgeContractTests
    {
        private static readonly string[] EntityFields =
        {
            "condition", "autoMaintenance", "maintenanceState", "maintenanceRemainingHours",
            "lastMaintenanceDay", "waterSupplyMode", "powerLineAttached"
        };

        private static readonly string[] NodeFields =
        {
            "schemaVersion", "modVersion", "checksum", "document"
        };

        internal static void Run(TestRunner t)
        {
            EntityContractTests(t);
            NodeContractTests(t);
            SourceContractTests(t);
        }

        private static void EntityContractTests(TestRunner t)
        {
            t.Run("A per-entity state round-trips all seven mutable fields", () =>
            {
                var state = new BoilerSaveState
                {
                    EntityId = "0f8fad5b-d9cb-469f-a165-70867728950e",
                    Tier = BoilerTier.T3,
                    Size = BoilerSize.Large,
                    Faction = BoilerFaction.IronTeeth,
                    Condition = 42.5f,
                    AutoMaintenance = false,
                    MaintenancePhase = MaintenancePhase.Maintaining,
                    MaintenanceRemainingHours = 1.25f,
                    LastMaintenanceDay = 17.5f,
                    WaterSupplyMode = WaterSupplyMode.Valve,
                    PowerLineAttached = true
                };
                string json = SeeSaveCodec.Serialize(state);
                BoilerSaveState? parsed = SeeSaveCodec.Parse(json, null);
                BoilerSaveState safe = SeeSaveCodec.Sanitize(parsed, 30f, null);

                Assert.Equal(BoilerTier.T3, safe.Tier, "tier");
                Assert.Equal(42.5f, safe.Condition, "condition");
                Assert.False(safe.AutoMaintenance, "auto maintenance");
                Assert.Equal(MaintenancePhase.Maintaining, safe.MaintenancePhase, "phase");
                Assert.Equal(1.25f, safe.MaintenanceRemainingHours, "remaining hours");
                Assert.Equal(WaterSupplyMode.Valve, safe.WaterSupplyMode, "water mode");
                Assert.True(safe.PowerLineAttached, "power line flag");
            });

            t.Run("A missing per-entity component means a new boiler with safe defaults", () =>
            {
                BoilerSaveState safe = SeeSaveCodec.Sanitize(null, 0f, null);
                Assert.Equal(SEEConst.MaxCondition, safe.Condition, "condition 100");
                Assert.True(safe.AutoMaintenance, "auto on");
                Assert.Equal(MaintenancePhase.None, safe.MaintenancePhase, "phase none");
                Assert.Equal(0f, safe.MaintenanceRemainingHours, "no downtime");
                Assert.Equal(WaterSupplyMode.Auto, safe.WaterSupplyMode, "mode auto");
                Assert.False(safe.PowerLineAttached, "no power line");
            });

            t.Run("A corrupt condition resets the boiler and disables auto maintenance", () =>
            {
                var log = new RecordingLog();
                var raw = new BoilerSaveState { Condition = float.NaN, AutoMaintenance = true };
                BoilerSaveState safe = SeeSaveCodec.Sanitize(raw, 10f, log);
                Assert.Equal(SEEConst.MaxCondition, safe.Condition, "condition reset");
                Assert.False(safe.AutoMaintenance, "auto disabled");
                Assert.True(HasWarning(log), "warning reported");
            });

            t.Run("An unknown maintenance state falls back to None", () =>
            {
                var log = new RecordingLog();
                BoilerSaveState? parsed = SeeSaveCodec.Parse(
                    "{\"schemaVersion\":1,\"maintenanceState\":\"SomethingNew\"}", log);
                BoilerSaveState safe = SeeSaveCodec.Sanitize(parsed, 0f, log);
                Assert.Equal(MaintenancePhase.None, safe.MaintenancePhase, "phase none");
            });

            t.Run("Remaining downtime is clamped to [0,2] and zeroed when not maintaining", () =>
            {
                var log = new RecordingLog();
                BoilerSaveState? parsed = SeeSaveCodec.Parse(
                    "{\"schemaVersion\":1,\"maintenanceState\":\"Maintaining\","
                    + "\"maintenanceRemainingHours\":99}", log);
                Assert.Equal(SEEConst.MaxMaintenanceRemainingHours,
                    SeeSaveCodec.Sanitize(parsed, 0f, log).MaintenanceRemainingHours, "clamped");

                BoilerSaveState? idle = SeeSaveCodec.Parse(
                    "{\"schemaVersion\":1,\"maintenanceState\":\"None\","
                    + "\"maintenanceRemainingHours\":1.5}", log);
                Assert.Equal(0f, SeeSaveCodec.Sanitize(idle, 0f, log).MaintenanceRemainingHours,
                    "zeroed outside maintaining");
            });

            t.Run("A newer schemaVersion degrades per-entity data read-only without losing it", () =>
            {
                var log = new RecordingLog();
                BoilerSaveState? parsed = SeeSaveCodec.Parse(
                    "{\"schemaVersion\":99,\"condition\":33.5}", log);
                BoilerSaveState safe = SeeSaveCodec.Sanitize(parsed, 0f, log);
                Assert.Equal(33.5f, safe.Condition, "condition kept");
                Assert.True(HasWarning(log), "newer schema reported");
            });
        }

        private static void NodeContractTests(TestRunner t)
        {
            t.Run("The world node document carries schema, four-field index, compat and checksum", () =>
            {
                string document = SeeSaveNodeCodec.Serialize(BuildNode());
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(document, null);
                Assert.False(result.ReadOnly, "not read-only");
                Assert.Equal(1, result.Node.SchemaVersion, "schema");
                Assert.Equal(2, result.Node.Buildings.Count, "two index rows");

                var root = (IDictionary<string, object?>)MiniJson.Parse(document)!;
                Assert.True(root.ContainsKey("buildings"), "buildings array");
                Assert.True(root.ContainsKey("compat"), "compat block");
                Assert.True(root.ContainsKey("checksum"), "checksum");
                foreach (string field in new[] { "entityId", "tier", "size", "faction" })
                {
                    var first = (IDictionary<string, object?>)((List<object?>)root["buildings"]!)[0]!;
                    Assert.True(first.ContainsKey(field), "index field " + field);
                }

                Assert.False(document.Contains("condition", StringComparison.Ordinal),
                    "per-entity state must not be in the node");
            });

            t.Run("The node checksum verifies after a round trip and a tampered checksum warns", () =>
            {
                string document = SeeSaveNodeCodec.Serialize(BuildNode());
                Assert.True(SeeSaveNodeCodec.ComputeChecksum(document) == SeeSaveNodeCodec.Load(document, null).Node.Checksum,
                    "checksum matches");
                string tampered = document.Replace("\"checksum\":\"sha256:", "\"checksum\":\"sha256:dead");
                var log = new RecordingLog();
                SeeSaveNodeCodec.Load(tampered, log);
                Assert.True(HasWarning(log), "mismatch reported");
            });

            t.Run("The node index is canonical (buildings sorted by entityId)", () =>
            {
                string document = SeeSaveNodeCodec.Serialize(BuildNode());
                int first = document.IndexOf("0f8fad5b", StringComparison.Ordinal);
                int second = document.IndexOf("11111111", StringComparison.Ordinal);
                Assert.True(first >= 0 && second >= 0 && first < second, "ascending entity ids");
            });

            t.Run("A newer node schema degrades read-only and keeps the loaded data", () =>
            {
                var log = new RecordingLog();
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(
                    "{\"schemaVersion\":99,\"buildings\":[{\"entityId\":\"0f8fad5b-d9cb-469f-a165-70867728950e\"}]}",
                    log);
                Assert.True(result.ReadOnly, "read-only");
                Assert.Equal(1, result.Node.Buildings.Count, "data kept");
                Assert.True(HasWarning(log), "reported");
            });

            t.Run("A node written by an older build without document still parses as a fresh index", () =>
            {
                SeeSaveNode empty = SeeSaveNodeCodec.Parse("{\"schemaVersion\":1,\"modVersion\":\"0.0.9\"}", null);
                Assert.Equal(1, empty.SchemaVersion, "schema");
                Assert.Equal(0, empty.Buildings.Count, "empty index");
            });

            t.Run("Corrupt node JSON keeps loading with an empty repair result", () =>
            {
                var log = new RecordingLog();
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load("not-json", log);
                Assert.Equal(0, result.Node.Buildings.Count, "empty index");
                Assert.True(result.Damaged, "damage flagged");
                Assert.True(HasWarning(log), "reported");
            });
        }

        private static void SourceContractTests(TestRunner t)
        {
            t.Run("The gameplay save key table matches the Core per-entity contract", () =>
            {
                string source = File.ReadAllText(TestPaths.FromMod("src", "SEE.Gameplay", "SeeSaveKeys.cs"));
                var propertyKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (Match match in Regex.Matches(source, "new PropertyKey<[^>]+>\\(\"([^\"]+)\"\\)"))
                {
                    propertyKeys.Add(match.Groups[1].Value);
                }

                var expectedProperties = new HashSet<string>(StringComparer.Ordinal);
                foreach (string field in EntityFields)
                {
                    expectedProperties.Add(field);
                }

                foreach (string field in NodeFields)
                {
                    expectedProperties.Add(field);
                }

                Assert.Equal(expectedProperties.Count, propertyKeys.Count, "property key count");
                foreach (string field in expectedProperties)
                {
                    Assert.True(propertyKeys.Contains(field), "missing key " + field);
                }

                Assert.Contains(source, "new SingletonKey(SEEConst.SaveComponentKey)", "world node key");
                Assert.Contains(source, "new ComponentKey(SEEConst.SaveComponentKey)", "entity component key");
                Assert.Equal("SEE", SEEConst.SaveComponentKey, "shared SEE key name");

                string entityJson = SeeSaveCodec.Serialize(BoilerSaveState.NewGame());
                foreach (string field in EntityFields)
                {
                    Assert.Contains(entityJson, "\"" + field + "\"", "Core entity field " + field);
                }
            });

            t.Run("The gameplay bridge saves the canonical Core node document", () =>
            {
                string source = File.ReadAllText(TestPaths.FromMod("src", "SEE.Gameplay", "Save", "SeeSaveBridge.cs"));
                Assert.Contains(source, "ISaveableSingleton", "saveable singleton");
                Assert.Contains(source, "ILoadableSingleton", "loadable singleton");
                Assert.Contains(source, "SeeSaveNodeCodec.Serialize", "canonical node codec");
                Assert.Contains(source, "SeeSaveKeys.Document", "document key");
                Assert.Contains(source, "SeeSaveNodeCodec.ComputeChecksum", "checksum");
                Assert.False(source.Contains("condition", StringComparison.Ordinal),
                    "the world node must not persist per-entity state");
            });

            t.Run("The controller is the per-entity save participant of the seven fields", () =>
            {
                string save = File.ReadAllText(
                    TestPaths.FromMod("src", "SEE.Gameplay", "Components", "SEEBoilerController.Save.cs"));
                string baseSource = File.ReadAllText(
                    TestPaths.FromMod("src", "SEE.Gameplay", "Components", "SEEBoilerController.cs"));
                Assert.Contains(baseSource, "IPersistentEntity", "persistent entity contract");
                Assert.Contains(save, "void Save(IEntitySaver", "Save signature");
                Assert.Contains(save, "void Load(IEntityLoader", "Load signature");
                foreach (string field in EntityFields)
                {
                    string keyName = ToKeyName(field);
                    Assert.Contains(save, "SeeSaveKeys." + keyName, "key usage " + keyName);
                }
            });
        }

        private static string ToKeyName(string field)
        {
            switch (field)
            {
                case "condition": return "Condition";
                case "autoMaintenance": return "AutoMaintenance";
                case "maintenanceState": return "MaintenanceState";
                case "maintenanceRemainingHours": return "MaintenanceRemainingHours";
                case "lastMaintenanceDay": return "LastMaintenanceDay";
                case "waterSupplyMode": return "WaterSupplyMode";
                case "powerLineAttached": return "PowerLineAttached";
                default: return field;
            }
        }

        private static SeeSaveNode BuildNode()
        {
            var node = new SeeSaveNode();
            node.Buildings.Add(new SeeSaveBuildingIndex
            {
                EntityId = "11111111-2222-3333-4444-555555555555",
                Tier = BoilerTier.T2,
                Size = BoilerSize.Medium,
                Faction = BoilerFaction.Folktails
            });
            node.Buildings.Add(new SeeSaveBuildingIndex
            {
                EntityId = "0f8fad5b-d9cb-469f-a165-70867728950e",
                Tier = BoilerTier.T4,
                Size = BoilerSize.Large,
                Faction = BoilerFaction.IronTeeth
            });
            node.Compat.Detected.Add("TimberPipes");
            node.Compat.Enabled.Add("SEE.Adapter.TimberPipes");
            return node;
        }

        private static bool HasWarning(RecordingLog log)
        {
            foreach (string entry in log.Entries)
            {
                if (entry.StartsWith("Warn|", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
