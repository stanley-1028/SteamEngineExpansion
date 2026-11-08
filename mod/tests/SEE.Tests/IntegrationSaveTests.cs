using System;
using System.Collections.Generic;
using System.Text;
using SEE.Core.Abstractions;
using SEE.Core.Json;
using SEE.Core.Logging;
using SEE.Core.Save;
using SEE.Integration.Adapters;
using SEE.Integration.Detection;
using SEE.Integration.Save;

namespace SEE.Tests
{
    /// <summary>
    /// P5 save serialization complementing the P2/P3 codec tests (CONTRACT_SEE §6, REQ-SAVE-01..14):
    /// world-node round-trip, checksum semantics, read-only degradation, 12 corruption classes,
    /// unknown-field preservation, duplicate/orphan isolation, compat recomputation after adding or
    /// removing an optional mod, and the 1,000-row size budget.
    /// </summary>
    internal static class IntegrationSaveTests
    {
        internal static void Run(TestRunner t)
        {
            RoundTripTests(t);
            ChecksumTests(t);
            MigrationTests(t);
            CorruptionTests(t);
            CompatTests(t);
            VolumeTests(t);
        }

        private static void RoundTripTests(TestRunner t)
        {
            t.Run("P5 save: world node round-trips twelve index rows and the compat snapshot", () =>
            {
                SeeSaveNode node = BuildNode(12);
                node.Compat.Detected.Add("TimberPipes");
                node.Compat.Enabled.Add(CompatAnchors.TimberPipesAdapterId);
                node.Compat.Failed.Add("powerlines-anchor-missing");

                string json = SaveSerializer.Serialize(node);
                SeeSaveLoadResult result = SaveSerializer.Load(json, null);

                Assert.Equal(12, result.Node.Buildings.Count, "row count");
                Assert.Equal(node.Buildings[0].EntityId, result.Node.Buildings[0].EntityId, "first id");
                Assert.Equal(BoilerTier.T2, result.Node.Buildings[1].Tier, "tier");
                Assert.Equal(BoilerSize.Large, result.Node.Buildings[2].Size, "size");
                Assert.Equal(1, result.Node.Compat.Detected.Count, "detected count");
                Assert.Equal(1, result.Node.Compat.Enabled.Count, "enabled count");
                Assert.Equal(1, result.Node.Compat.Failed.Count, "failed count");
                Assert.False(result.ReadOnly, "read-only");
                Assert.False(result.Damaged, "damaged");
            });

            t.Run("P5 save: serialize is deterministic (byte-stable round trip)", () =>
            {
                SeeSaveNode node = BuildNode(6);
                string first = SaveSerializer.Serialize(node);
                SeeSaveNode reparsed = SaveSerializer.Load(first, null).Node;
                string second = SaveSerializer.Serialize(reparsed);
                Assert.Equal(first, second, "byte-stable");
                Assert.Equal(first, SaveSerializer.Normalize(first), "already canonical");
            });

            t.Run("P5 save: missing node is a new game, not damage", () =>
            {
                SeeSaveLoadResult result = SaveSerializer.Load(null, null);
                Assert.Equal(0, result.Node.Buildings.Count, "no rows");
                Assert.False(result.Damaged, "not damaged");
                Assert.Equal(1, result.Node.SchemaVersion, "schema v1");
            });

            t.Run("P5 save: index rendering keeps only the four contract fields", () =>
            {
                SeeSaveNode node = BuildNode(3);
                string index = SaveSerializer.SerializeIndex(node);
                Assert.Contains(index, "entityId", "entityId key");
                Assert.Contains(index, "\"tier\"", "tier key");
                Assert.False(index.Contains("checksum", StringComparison.Ordinal), "no checksum");
                Assert.False(index.Contains("compat", StringComparison.Ordinal), "no compat");
            });
        }

        private static void ChecksumTests(TestRunner t)
        {
            t.Run("P5 checksum: verifies a freshly serialized node", () =>
            {
                SeeSaveNode node = BuildNode(2);
                string json = SaveSerializer.Serialize(node);
                string embedded = SaveSerializer.Load(json, null).Node.Checksum;
                Assert.Equal(SaveSerializer.ComputeChecksum(json), embedded, "embedded checksum");
                Assert.True(SaveSerializer.ComputeChecksum(json)!.StartsWith(SaveChecksum.Prefix, StringComparison.Ordinal),
                    "checksum prefix");
                Assert.Equal(json, SaveSerializer.Normalize(json), "serialized form is canonical");
            });

            t.Run("P5 checksum: tampering warns but never rejects the load or overwrites data", () =>
            {
                string json = SaveSerializer.Serialize(BuildNode(2));
                string tampered = json.Replace("\"modVersion\":\"0.1.0\"", "\"modVersion\":\"9.9.9\"");
                var log = new RecordingLog();
                SeeSaveLoadResult result = SaveSerializer.Load(tampered, log);
                Assert.Equal(2, result.Node.Buildings.Count, "rows still loaded");
                Assert.Equal("9.9.9", result.Node.ModVersion, "tampered value kept, not overwritten");
                bool warned = false;
                foreach (string entry in log.Entries)
                {
                    if (entry.Contains("checksum mismatch", StringComparison.Ordinal))
                    {
                        warned = true;
                    }
                }

                Assert.True(warned, "checksum warning logged");
            });

            t.Run("P5 checksum: a deleted checksum loads with a warning", () =>
            {
                string json = "{\"schemaVersion\":1,\"modVersion\":\"0.1.0\",\"buildings\":[],\"compat\":{}}";
                var log = new RecordingLog();
                SeeSaveLoadResult result = SaveSerializer.Load(json, log);
                Assert.False(result.Damaged, "not damage");
                Assert.True(log.Entries.Count > 0, "warning recorded");
            });
        }

        private static void MigrationTests(TestRunner t)
        {
            t.Run("P5 migrator: current schema passes through", () =>
            {
                SaveMigrationResult result = SaveMigrator.Migrate(1, "{}");
                Assert.True(result.Success, "success");
                Assert.False(result.ReadOnly, "writable");
            });

            t.Run("P5 migrator: newer schema is read-only and keeps the document", () =>
            {
                SaveMigrationResult result = SaveMigrator.Migrate(99, "{\"schemaVersion\":99}");
                Assert.False(result.Success, "not migrated");
                Assert.True(result.ReadOnly, "read-only");
                Assert.Equal("{\"schemaVersion\":99}", result.Json, "document kept");
                Assert.Equal(1, SaveMigrator.CurrentSchemaVersion, "current version");
            });

            t.Run("P5 migrator: null/negative input never throws", () =>
            {
                Assert.False(SaveMigrator.Migrate(1, null).Success, "null");
                Assert.False(SaveMigrator.Migrate(-1, "{}").Success, "negative");
            });
        }

        private static void CorruptionTests(TestRunner t)
        {
            t.Run("P5 corruption: twelve damage classes are repaired row by row, never rejected", () =>
            {
                string validId = "11111111-1111-1111-1111-111111111111";
                string secondId = "22222222-2222-2222-2222-222222222222";
                var cases = new List<string>
                {
                    // 1 node is not an object
                    "[]",
                    // 2 schemaVersion is not an integer
                    "{\"schemaVersion\":\"new\",\"buildings\":[]}",
                    // 3 buildings is not an array
                    "{\"schemaVersion\":1,\"buildings\":{}}",
                    // 4 a building row is not an object
                    "{\"schemaVersion\":1,\"buildings\":[1]}",
                    // 5 entityId is not a GUID
                    "{\"schemaVersion\":1,\"buildings\":[{\"entityId\":\"nope\"}]}",
                    // 6 tier/size/faction are invalid
                    "{\"schemaVersion\":1,\"buildings\":[{\"entityId\":\"" + validId + "\",\"tier\":\"T9\",\"size\":\"Huge\",\"faction\":\"Bots\"}]}",
                    // 7 duplicate entityId
                    "{\"schemaVersion\":1,\"buildings\":[{\"entityId\":\"" + validId + "\"},{\"entityId\":\"" + validId + "\"}]}",
                    // 8 compat is not an object
                    "{\"schemaVersion\":1,\"buildings\":[],\"compat\":[]}",
                    // 9 compat.detected is not an array
                    "{\"schemaVersion\":1,\"buildings\":[],\"compat\":{\"detected\":\"TimberPipes\"}}",
                    // 10 compat entry is not a string
                    "{\"schemaVersion\":1,\"buildings\":[],\"compat\":{\"failed\":[1]}}",
                    // 11 checksum is not a string
                    "{\"schemaVersion\":1,\"checksum\":5,\"buildings\":[]}",
                    // 12 modVersion is not a string
                    "{\"schemaVersion\":1,\"modVersion\":7,\"buildings\":[{\"entityId\":\"" + secondId + "\"}]}"
                };

                foreach (string json in cases)
                {
                    SeeSaveLoadResult result = SaveSerializer.Load(json, null);
                    Assert.NotNull(result.Node, "node survives: " + json);
                }

                SeeSaveLoadResult invalidTier = SaveSerializer.Load(cases[5], null);
                Assert.Equal(BoilerTier.T1, invalidTier.Node.Buildings[0].Tier, "tier default");
                Assert.Equal(BoilerSize.Small, invalidTier.Node.Buildings[0].Size, "size default");

                SeeSaveLoadResult duplicate = SaveSerializer.Load(cases[6], null);
                Assert.Equal(1, duplicate.Node.Buildings.Count, "duplicate dropped");
                Assert.True(duplicate.Damaged, "repair flagged");
            });

            t.Run("P5 corruption: read-only degradation keeps every row", () =>
            {
                string json = "{\"schemaVersion\":99,\"modVersion\":\"9.9.9\",\"buildings\":[{\"entityId\":\"33333333-3333-3333-3333-333333333333\",\"tier\":\"T3\",\"size\":\"Medium\",\"faction\":\"IronTeeth\"}],\"compat\":{}}";
                SeeSaveLoadResult result = SaveSerializer.Load(json, null);
                Assert.True(result.ReadOnly, "read-only");
                Assert.Equal(1, result.Node.Buildings.Count, "data kept");
                Assert.Equal(BoilerTier.T3, result.Node.Buildings[0].Tier, "tier kept");
            });

            t.Run("P5 corruption: unknown top-level and row fields survive the round trip", () =>
            {
                string json = "{\"schemaVersion\":1,\"modVersion\":\"0.1.0\",\"buildings\":[{\"entityId\":\"44444444-4444-4444-4444-444444444444\",\"tier\":\"T1\",\"size\":\"Small\",\"faction\":\"Folktails\",\"futureRow\":\"keep-me\"}],\"compat\":{},\"futureTop\":42}";
                SeeSaveLoadResult result = SaveSerializer.Load(json, null);
                Assert.True(result.Node.UnknownFields.ContainsKey("futureTop"), "top-level unknown kept");
                Assert.True(result.Node.Buildings[0].UnknownFields.ContainsKey("futureRow"), "row unknown kept");

                string rewritten = SaveSerializer.Serialize(result.Node);
                Assert.Contains(rewritten, "futureTop", "top-level written back");
                Assert.Contains(rewritten, "keep-me", "row field written back");
            });

            t.Run("P5 corruption: missing buildings is an empty index, not damage", () =>
            {
                SeeSaveLoadResult result = SaveSerializer.Load("{\"schemaVersion\":1,\"modVersion\":\"0.1.0\"}", null);
                Assert.Equal(0, result.Node.Buildings.Count, "empty index");
                Assert.False(result.Damaged, "not damaged");
            });
        }

        private static void CompatTests(TestRunner t)
        {
            t.Run("P5 compat: snapshot is rebuilt from live detection, not from saved arrays", () =>
            {
                CompatRig rig = new CompatRig(
                    timberPipes: Statuses.Available(CompatAnchors.TimberPipes),
                    powerLines: Statuses.Degraded(CompatAnchors.PowerLines, "anchor-missing: PowerLines.Specs.PowerLineSpec"))
                    .Initialize();
                SeeCompatSnapshot snapshot = CompatSnapshotBuilder.Build(
                    new[] { rig.Pipes.Status, rig.Power.Status, rig.Belts.Status },
                    new IExternalAdapter[] { rig.Pipes, rig.Power, rig.Belts });

                Assert.True(snapshot.Detected.Contains(CompatAnchors.TimberPipes), "TP detected");
                Assert.True(snapshot.Detected.Contains(CompatAnchors.PowerLines), "PL detected");
                Assert.False(snapshot.Detected.Contains(CompatAnchors.ConveyorBelt), "CB absent");
                Assert.True(snapshot.Enabled.Contains(CompatAnchors.TimberPipesAdapterId), "TP enabled");
                Assert.Equal(1, snapshot.Failed.Count, "one failure");
                Assert.Contains(snapshot.Failed[0], "powerlines", "failure key is stable and id-only");
                Assert.False(snapshot.Failed[0].Contains("."), "no external type names in failure keys");
            });

            t.Run("P5 compat: removing every mod clears the block on the next save", () =>
            {
                CompatRig rig = new CompatRig().Initialize();
                SeeCompatSnapshot after = CompatSnapshotBuilder.Build(
                    new[]
                    {
                        Statuses.Absent(CompatAnchors.TimberPipes),
                        Statuses.Absent(CompatAnchors.PowerLines),
                        Statuses.Absent(CompatAnchors.ConveyorBelt)
                    },
                    new IExternalAdapter[] { rig.Pipes, rig.Power, rig.Belts });

                var saved = new SeeCompatSnapshot();
                saved.Detected.Add("TimberPipes");
                saved.Enabled.Add(CompatAnchors.TimberPipesAdapterId);
                Assert.False(CompatSnapshotBuilder.ContentEquals(saved, after), "stale snapshot differs");
                Assert.Equal(0, after.Detected.Count, "detected cleared");
                Assert.Equal(0, after.Enabled.Count, "enabled cleared");
                Assert.Equal(0, after.Failed.Count, "failed cleared");
            });

            t.Run("P5 compat: content comparison ignores array order", () =>
            {
                var left = new SeeCompatSnapshot();
                left.Detected.Add("TimberPipes");
                left.Detected.Add("PowerLines");
                var right = new SeeCompatSnapshot();
                right.Detected.Add("PowerLines");
                right.Detected.Add("TimberPipes");
                Assert.True(CompatSnapshotBuilder.ContentEquals(left, right), "order-insensitive");
            });
        }

        private static void VolumeTests(TestRunner t)
        {
            t.Run("P5 save: 1,000 building rows stay far below the 400 KB zip budget", () =>
            {
                SeeSaveNode node = BuildNode(1000);
                string json = SaveSerializer.Serialize(node);
                Assert.True(Encoding.UTF8.GetByteCount(json) < 400 * 1024,
                    "uncompressed JSON below 400 KB: " + Encoding.UTF8.GetByteCount(json));
                SeeSaveLoadResult result = SaveSerializer.Load(json, null);
                Assert.Equal(1000, result.Node.Buildings.Count, "row count");
                Assert.False(result.Damaged, "no repairs");
            });
        }

        private static SeeSaveNode BuildNode(int count)
        {
            var node = new SeeSaveNode
            {
                SchemaVersion = 1,
                ModVersion = "0.1.0"
            };
            for (int i = 0; i < count; i++)
            {
                node.Buildings.Add(new SeeSaveBuildingIndex
                {
                    EntityId = EntityId(i),
                    Tier = (BoilerTier)(i % 4),
                    Size = (BoilerSize)(i % 3),
                    Faction = i % 2 == 0 ? BoilerFaction.Folktails : BoilerFaction.IronTeeth
                });
            }

            return node;
        }

        private static string EntityId(int index)
        {
            return "00000000-0000-0000-0000-" + index.ToString("D12");
        }
    }
}
