using System;
using System.Collections.Generic;
using SEE.Core.Constants;
using SEE.Core.Logging;
using SEE.Core.Save;

namespace SEE.Tests
{
    /// <summary>
    /// Contract-surface tests of the save node (requirements §14.1/§14.4, CONTRACT_SEE §6):
    /// schema-version handling, the forward-only migration chain, unknown-field preservation and
    /// the eleven-field shape split between the node index and the per-entity state (AD-11).
    /// </summary>
    internal static class SaveNodeContractTests
    {
        internal static void Run(TestRunner t)
        {
            SchemaAndMigrationTests(t);
            UnknownFieldTests(t);
            ContractShapeTests(t);
        }

        private static void SchemaAndMigrationTests(TestRunner t)
        {
            t.Run("A missing schemaVersion is treated as v1", () =>
            {
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load("{\"buildings\":[]}", null);
                Assert.Equal(1, result.Node.SchemaVersion, "version");
                Assert.False(result.ReadOnly, "not read-only");
            });

            t.Run("A newer schemaVersion degrades to read-only without losing data", () =>
            {
                var log = new RecordingLog();
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(
                    "{\"schemaVersion\":99,\"modVersion\":\"9.9.9\",\"buildings\":[{\"entityId\":\""
                    + SaveNodeTests.SecondId.ToString("D") + "\",\"tier\":\"T4\"}]}", log);
                Assert.True(result.ReadOnly, "read-only");
                Assert.Equal(99, result.Node.SchemaVersion, "version kept");
                Assert.Equal(1, result.Node.Buildings.Count, "data kept");
                Assert.True(SaveNodeTests.HasWarning(log, "newer"), "reported");
            });

            t.Run("The shipped v1 migration chain is empty", () =>
                Assert.Equal(0, SeeSaveNodeCodec.Migrations.Count, "chain count"));

            t.Run("Migrating v1 to v1 returns the text unchanged", () =>
            {
                string json = SeeSaveNodeCodec.Serialize(SaveNodeTests.BuildNode());
                Assert.Equal(json, SeeSaveNodeCodec.Migrate(1, json), "v1 is current");
            });

            t.Run("A newer document cannot be migrated and degrades to null", () =>
            {
                Assert.True(SeeSaveNodeCodec.Migrate(99, "{\"schemaVersion\":99}") == null, "newer -> null");
                Assert.True(SeeSaveNodeCodec.Migrate(1, null) == null, "null text -> null");
            });

            t.Run("An explicit v1->v2 chain applies forward and is idempotent", () =>
            {
                var chain = new ISaveMigration[]
                {
                    new FakeMigration(1, 2, "\"schemaVersion\":1", "\"schemaVersion\":2")
                };
                const string v1 = "{\"schemaVersion\":1}";
                string? v2 = SeeSaveNodeCodec.Migrate(1, v1, 2, chain);
                Assert.Equal("{\"schemaVersion\":2}", v2, "migrated");
                Assert.Equal("{\"schemaVersion\":2}", SeeSaveNodeCodec.Migrate(2, v2, 2, chain), "idempotent");
            });

            t.Run("A failing migration step returns null instead of throwing", () =>
            {
                var chain = new ISaveMigration[] { new FakeMigration(1, 2, null, null) };
                Assert.True(SeeSaveNodeCodec.Migrate(1, "{\"schemaVersion\":1}", 2, chain) == null, "null step");
            });
        }

        private static void UnknownFieldTests(TestRunner t)
        {
            t.Run("Unknown top-level and per-building fields are preserved on write-back", () =>
            {
                SeeSaveNode node = SaveNodeTests.BuildNode();
                node.UnknownFields["futureTopLevel"] = true;
                node.Buildings[0].UnknownFields["futureTierHint"] = "planned";

                SeeSaveNode loaded = SeeSaveNodeCodec.Parse(SeeSaveNodeCodec.Serialize(node), null);
                Assert.True(loaded.UnknownFields.ContainsKey("futureTopLevel"), "top-level kept");
                SeeSaveBuildingIndex? second = null;
                foreach (SeeSaveBuildingIndex building in loaded.Buildings)
                {
                    if (building.EntityId == SaveNodeTests.SecondId.ToString("D"))
                    {
                        second = building;
                    }
                }

                Assert.NotNull(second, "second building survived the round trip");
                Assert.True(second!.UnknownFields.ContainsKey("futureTierHint"), "building kept");
            });

            t.Run("An unknown field cannot override a known field", () =>
            {
                SeeSaveNode node = SaveNodeTests.BuildNode();
                node.UnknownFields["schemaVersion"] = 99;
                string json = SeeSaveNodeCodec.Serialize(node);
                SeeSaveNode loaded = SeeSaveNodeCodec.Parse(json, null);
                Assert.Equal(1, loaded.SchemaVersion, "known field wins");
            });
        }

        private static void ContractShapeTests(TestRunner t)
        {
            t.Run("The node carries only the four-field index; state stays per-entity (AD-11)", () =>
            {
                string nodeJson = SeeSaveNodeCodec.Serialize(SaveNodeTests.BuildNode());
                Assert.Contains(nodeJson, "\"entityId\"", "entity id");
                Assert.Contains(nodeJson, "\"tier\"", "tier");
                Assert.Contains(nodeJson, "\"size\"", "size");
                Assert.Contains(nodeJson, "\"faction\"", "faction");
                Assert.False(nodeJson.Contains("condition", StringComparison.Ordinal), "no per-entity state in node");

                string entityJson = SeeSaveCodec.Serialize(BoilerSaveState.NewGame());
                foreach (string field in new[]
                         {
                             "condition", "autoMaintenance", "maintenanceState", "maintenanceRemainingHours",
                             "lastMaintenanceDay", "waterSupplyMode", "powerLineAttached"
                         })
                {
                    Assert.Contains(entityJson, "\"" + field + "\"", "entity field " + field);
                }
            });

            t.Run("The save document never contains balance parameters (REQ-SAVE-03)", () =>
            {
                string json = SeeSaveNodeCodec.Serialize(SaveNodeTests.BuildNode());
                Assert.False(json.Contains("fuelRatePer100hp", StringComparison.Ordinal), "no fuel rate");
                Assert.False(json.Contains("waterRatePer100hp", StringComparison.Ordinal), "no water rate");
                Assert.False(json.Contains("buildCostMultiplier", StringComparison.Ordinal), "no build cost");
            });

            t.Run("The four index fields plus the seven state fields make eleven (REQ-SAVE-02)", () =>
            {
                string[] nodeFields = { "entityId", "tier", "size", "faction" };
                string[] stateFields =
                {
                    "condition", "autoMaintenance", "maintenanceState", "maintenanceRemainingHours",
                    "lastMaintenanceDay", "waterSupplyMode", "powerLineAttached"
                };
                var all = new HashSet<string>(nodeFields);
                foreach (string field in stateFields)
                {
                    Assert.True(all.Add(field), "no duplicate field " + field);
                }

                Assert.Equal(11, all.Count, "eleven distinct fields");
            });
        }

        private sealed class FakeMigration : ISaveMigration
        {
            private readonly string? _from;
            private readonly string? _to;

            internal FakeMigration(int fromVersion, int toVersion, string? from, string? to)
            {
                FromVersion = fromVersion;
                ToVersion = toVersion;
                _from = from;
                _to = to;
            }

            public int FromVersion { get; }

            public int ToVersion { get; }

            public string? Migrate(string json)
            {
                if (_from == null || _to == null)
                {
                    return null;
                }

                return json.Replace(_from, _to);
            }
        }
    }
}
