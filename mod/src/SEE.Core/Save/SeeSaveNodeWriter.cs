using System;
using System.Collections.Generic;
using SEE.Core.Constants;

namespace SEE.Core.Save
{
    /// <summary>
    /// Serialization half of the world save node codec (CONTRACT_SEE §6.1/§6.4): builds the JSON
    /// tree in the documented shape, preserving unknown fields, so <see cref="SeeSaveChecksum"/> can
    /// produce the canonical form and hash it. Internal helper of <see cref="SeeSaveNodeCodec"/>.
    /// </summary>
    internal static class SeeSaveNodeWriter
    {
        internal static readonly string[] KnownTopLevel =
        {
            "schemaVersion", "modVersion", "checksum", "buildings", "compat"
        };

        internal static readonly string[] KnownBuilding = { "entityId", "tier", "size", "faction" };

        internal static Dictionary<string, object?> BuildTree(SeeSaveNode node)
        {
            var tree = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["schemaVersion"] = node.SchemaVersion,
                ["modVersion"] = node.ModVersion ?? string.Empty,
                ["buildings"] = BuildBuildings(node),
                ["compat"] = BuildCompat(node.Compat)
            };

            foreach (KeyValuePair<string, object?> pair in node.UnknownFields)
            {
                if (!IsKnown(KnownTopLevel, pair.Key) && !tree.ContainsKey(pair.Key))
                {
                    tree[pair.Key] = pair.Value;
                }
            }

            return tree;
        }

        internal static bool IsKnown(string[] known, string key)
        {
            for (int i = 0; i < known.Length; i++)
            {
                if (string.Equals(known[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static void RemoveKey(IDictionary<string, object?> map, string key)
        {
            string? existing = null;
            foreach (string candidate in map.Keys)
            {
                if (string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase))
                {
                    existing = candidate;
                    break;
                }
            }

            if (existing != null)
            {
                map.Remove(existing);
            }
        }

        private static List<object?> BuildBuildings(SeeSaveNode node)
        {
            var entries = new List<object?>(node.Buildings.Count);
            foreach (SeeSaveBuildingIndex index in node.Buildings)
            {
                var map = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["entityId"] = index.EntityId,
                    ["tier"] = index.Tier.ToString(),
                    ["size"] = index.Size.ToString(),
                    ["faction"] = index.Faction.ToString()
                };

                foreach (KeyValuePair<string, object?> pair in index.UnknownFields)
                {
                    if (!IsKnown(KnownBuilding, pair.Key) && !map.ContainsKey(pair.Key))
                    {
                        map[pair.Key] = pair.Value;
                    }
                }

                entries.Add(map);
            }

            return entries;
        }

        private static Dictionary<string, object?> BuildCompat(SeeCompatSnapshot compat)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["detected"] = ToList(compat.Detected),
                ["enabled"] = ToList(compat.Enabled),
                ["failed"] = ToList(compat.Failed)
            };
        }

        private static List<object?> ToList(List<string> values)
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
