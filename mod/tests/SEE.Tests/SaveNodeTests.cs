using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Json;
using SEE.Core.Logging;
using SEE.Core.Save;

namespace SEE.Tests
{
    /// <summary>
    /// Tests for the world save node codec (<see cref="SeeSaveNodeCodec"/>,
    /// <see cref="SeeSaveChecksum"/>; requirements §14.1/§14.3/§14.5, CONTRACT_SEE §6):
    /// round-trip, canonical checksum, corruption recovery and the twelve-class damage isolation.
    /// </summary>
    internal static class SaveNodeTests
    {
        internal static readonly Guid FirstId = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");
        internal static readonly Guid SecondId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        internal static void Run(TestRunner t)
        {
            RoundtripTests(t);
            ChecksumTests(t);
            CorruptionTests(t);
        }

        private static void RoundtripTests(TestRunner t)
        {
            t.Run("A v1 SEE node round-trips through Serialize/Load", () =>
            {
                SeeSaveNode node = BuildNode();
                string json = SeeSaveNodeCodec.Serialize(node);
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(json, null);

                Assert.False(result.Damaged, "round trip is clean");
                Assert.False(result.ReadOnly, "not read-only");
                Assert.Equal(1, result.Node.SchemaVersion, "schema version");
                Assert.Equal("0.1.0", result.Node.ModVersion, "mod version");
                Assert.Contains(result.Node.Checksum, "sha256:", "checksum prefix");
                Assert.Equal(2, result.Node.Buildings.Count, "building count");
                Assert.Equal(FirstId.ToString("D"), result.Node.Buildings[0].EntityId, "first entity");
                Assert.Equal(BoilerTier.T2, result.Node.Buildings[0].Tier, "first tier");
                Assert.Equal(BoilerSize.Medium, result.Node.Buildings[0].Size, "first size");
                Assert.Equal(BoilerFaction.Folktails, result.Node.Buildings[0].Faction, "first faction");
                Assert.Equal(SecondId.ToString("D"), result.Node.Buildings[1].EntityId, "second entity");
                Assert.Equal(BoilerTier.T1, result.Node.Buildings[1].Tier, "second tier");
            });

            t.Run("The compat snapshot round-trips as three string arrays", () =>
            {
                SeeSaveNode node = BuildNode();
                node.Compat.Detected.Add("TimberPipes");
                node.Compat.Enabled.Add("SEE.Adapter.TimberPipes");
                node.Compat.Failed.Add("SEE.Compat.Missing");

                SeeSaveNode loaded = SeeSaveNodeCodec.Parse(SeeSaveNodeCodec.Serialize(node), null);
                Assert.Equal(1, loaded.Compat.Detected.Count, "detected");
                Assert.Equal("TimberPipes", loaded.Compat.Detected[0], "detected value");
                Assert.Equal(1, loaded.Compat.Enabled.Count, "enabled");
                Assert.Equal(1, loaded.Compat.Failed.Count, "failed");
            });

            t.Run("A clean node loads with an empty warning log", () =>
            {
                var log = new RecordingLog();
                string json = SeeSaveNodeCodec.Serialize(BuildNode());
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(json, log);
                Assert.False(result.Damaged, "no repairs");
                Assert.Equal(0, result.Repairs, "repair count");
                Assert.Equal(0, log.Entries.Count, "no warnings");
            });

            t.Run("A null or blank node becomes the safe new-game node", () =>
            {
                Assert.Equal(0, SeeSaveNodeCodec.Parse(null, null).Buildings.Count, "null");
                Assert.Equal(0, SeeSaveNodeCodec.Parse("   ", null).Buildings.Count, "blank");
                Assert.Equal(1, SeeSaveNodeCodec.Parse(null, null).SchemaVersion, "version");
            });
        }

        private static void ChecksumTests(TestRunner t)
        {
            t.Run("Serialize writes a checksum that verifies against the canonical document", () =>
            {
                string json = SeeSaveNodeCodec.Serialize(BuildNode());
                SeeSaveNode node = SeeSaveNodeCodec.Parse(json, null);
                Assert.True(SeeSaveChecksum.Verify(
                    SeeSaveChecksum.Canonicalize(WithoutChecksum(json)), node.Checksum), "checksum verifies");
            });

            t.Run("A checksum mismatch only warns; the data is kept", () =>
            {
                var log = new RecordingLog();
                string json = ReplaceChecksum(SeeSaveNodeCodec.Serialize(BuildNode()), "sha256:deadbeef");
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(json, log);
                Assert.Equal(2, result.Node.Buildings.Count, "buildings kept");
                Assert.True(HasWarning(log, "checksum mismatch"), "mismatch reported");
            });

            t.Run("A deleted checksum warns and still loads the node", () =>
            {
                var log = new RecordingLog();
                string json = MiniJson.Write(WithoutChecksumTree(SeeSaveNodeCodec.Serialize(BuildNode())));
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(json, log);
                Assert.Equal(2, result.Node.Buildings.Count, "buildings kept");
                Assert.True(HasWarning(log, "checksum is missing"), "missing checksum reported");
            });

            t.Run("The canonical form sorts keys and buildings and keeps four decimals", () =>
            {
                string json = SeeSaveNodeCodec.Serialize(BuildNode());
                int buildings = json.IndexOf("\"buildings\"", StringComparison.Ordinal);
                int checksum = json.IndexOf("\"checksum\"", StringComparison.Ordinal);
                int compat = json.IndexOf("\"compat\"", StringComparison.Ordinal);
                int modVersion = json.IndexOf("\"modVersion\"", StringComparison.Ordinal);
                int schemaVersion = json.IndexOf("\"schemaVersion\"", StringComparison.Ordinal);
                Assert.True(buildings >= 0 && buildings < checksum, "keys are ordinal-sorted");
                Assert.True(checksum < compat && compat < modVersion && modVersion < schemaVersion, "key order");
                Assert.True(json.IndexOf(FirstId.ToString("D"), StringComparison.Ordinal)
                            < json.IndexOf(SecondId.ToString("D"), StringComparison.Ordinal), "buildings sorted");
                Assert.Contains(json, "\"futureRatio\":0.5000", "fractional unknown number keeps four decimals");
            });

            t.Run("Non-finite numbers cannot be serialized into a save", () =>
            {
                SeeSaveNode node = BuildNode();
                node.UnknownFields["bad"] = double.NaN;
                Assert.Throws<FormatException>(() => SeeSaveNodeCodec.Serialize(node), "NaN rejected");
            });
        }

        private static void CorruptionTests(TestRunner t)
        {
            t.Run("Invalid JSON is rebuilt as an empty node and reported", () =>
            {
                var log = new RecordingLog();
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load("{not json", log);
                Assert.True(result.Damaged, "damaged");
                Assert.True(result.Repairs >= 1, "repairs");
                Assert.Equal(0, result.Node.Buildings.Count, "empty node");
                Assert.True(HasWarning(log, "not valid JSON"), "reported");
            });

            t.Run("A missing buildings array is an empty index, not damage", () =>
            {
                var log = new RecordingLog();
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(
                    "{\"schemaVersion\":1,\"modVersion\":\"0.1.0\"}", log);
                Assert.Equal(0, result.Node.Buildings.Count, "empty");
                Assert.False(result.Damaged, "not damage (requirements §14.5)");
            });

            t.Run("A buildings value of the wrong type is repaired to an empty index", () =>
            {
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load("{\"schemaVersion\":1,\"buildings\":{}}", null);
                Assert.Equal(0, result.Node.Buildings.Count, "empty");
                Assert.True(result.Damaged, "reported as damage");
            });

            t.Run("A missing building field keeps its documented default", () =>
            {
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(
                    "{\"schemaVersion\":1,\"buildings\":[{\"entityId\":\"" + FirstId.ToString("D") + "\"}]}", null);
                Assert.Equal(1, result.Node.Buildings.Count, "kept");
                Assert.Equal(BoilerTier.T1, result.Node.Buildings[0].Tier, "default tier");
                Assert.Equal(BoilerSize.Small, result.Node.Buildings[0].Size, "default size");
                Assert.Equal(BoilerFaction.Folktails, result.Node.Buildings[0].Faction, "default faction");
            });

            t.Run("A damaged building row is isolated and the other rows load", () =>
            {
                string json = "{\"schemaVersion\":1,\"buildings\":["
                              + "{\"entityId\":\"not-a-guid\",\"tier\":\"T1\"},"
                              + "{\"entityId\":\"" + SecondId.ToString("D") + "\",\"tier\":\"T3\","
                              + "\"size\":\"Large\",\"faction\":\"IronTeeth\"}]}";
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(json, null);
                Assert.Equal(1, result.Node.Buildings.Count, "only the valid row");
                Assert.Equal(BoilerTier.T3, result.Node.Buildings[0].Tier, "valid row kept");
                Assert.True(result.Damaged, "damage reported");
            });

            t.Run("Duplicate entity ids keep the first entry and drop the rest", () =>
            {
                string id = FirstId.ToString("D");
                string json = "{\"schemaVersion\":1,\"buildings\":["
                              + "{\"entityId\":\"" + id + "\",\"tier\":\"T2\"},"
                              + "{\"entityId\":\"" + id + "\",\"tier\":\"T4\"}]}";
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(json, null);
                Assert.Equal(1, result.Node.Buildings.Count, "one entry");
                Assert.Equal(BoilerTier.T2, result.Node.Buildings[0].Tier, "first wins");
            });

            t.Run("An invalid enum name falls back to the default and is repaired", () =>
            {
                string json = "{\"schemaVersion\":1,\"buildings\":[{\"entityId\":\""
                              + FirstId.ToString("D") + "\",\"tier\":\"T9\",\"size\":\"Gigantic\",\"faction\":\"Beavers\"}]}";
                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(json, null);
                Assert.Equal(BoilerTier.T1, result.Node.Buildings[0].Tier, "tier fallback");
                Assert.Equal(BoilerSize.Small, result.Node.Buildings[0].Size, "size fallback");
                Assert.Equal(BoilerFaction.Folktails, result.Node.Buildings[0].Faction, "faction fallback");
                Assert.True(result.Damaged, "reported");
            });

            t.Run("A NaN entity condition resets only that boiler (REQ-SAVE-06)", () =>
            {
                var broken = SeeSaveCodec.Sanitize(
                    new BoilerSaveState { EntityId = FirstId.ToString("D"), Condition = float.NaN }, 10f, null);
                var healthy = SeeSaveCodec.Sanitize(
                    new BoilerSaveState { EntityId = SecondId.ToString("D"), Condition = 42f }, 10f, null);

                Assert.Near(100.0, broken.Condition, 1e-6, "broken boiler reset");
                Assert.False(broken.AutoMaintenance, "broken boiler auto maintenance off");
                Assert.Near(42.0, healthy.Condition, 1e-6, "healthy boiler untouched");
            });
        }

        internal static SeeSaveNode BuildNode()
        {
            var node = new SeeSaveNode();
            node.Buildings.Add(new SeeSaveBuildingIndex
            {
                EntityId = SecondId.ToString("D"),
                Tier = BoilerTier.T1,
                Size = BoilerSize.Small,
                Faction = BoilerFaction.IronTeeth
            });
            node.Buildings.Add(new SeeSaveBuildingIndex
            {
                EntityId = FirstId.ToString("D"),
                Tier = BoilerTier.T2,
                Size = BoilerSize.Medium,
                Faction = BoilerFaction.Folktails
            });
            node.UnknownFields["futureRatio"] = 0.5;
            return node;
        }

        internal static IDictionary<string, object?> WithoutChecksum(string json)
        {
            return WithoutChecksumTree(json);
        }

        internal static IDictionary<string, object?> WithoutChecksumTree(string json)
        {
            var root = (IDictionary<string, object?>)MiniJson.Parse(json)!;
            root.Remove("checksum");
            return root;
        }

        internal static string ReplaceChecksum(string json, string checksum)
        {
            var root = (IDictionary<string, object?>)MiniJson.Parse(json)!;
            root["checksum"] = checksum;
            return MiniJson.Write(root);
        }

        internal static bool HasWarning(RecordingLog log, string fragment)
        {
            foreach (string entry in log.Entries)
            {
                if (entry.StartsWith("Warn|", StringComparison.Ordinal)
                    && entry.IndexOf(fragment, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
