using System;
using SEE.Core.Json;
using SEE.Core.Logging;
using SEE.Core.Save;

namespace SEE.Integration.Save
{
    /// <summary>
    /// Bridge between the runtime save path and the frozen Core codec of the world save node
    /// (requirements §14.1/§14.3, D-6 hybrid form / AD-11). All parsing, repairing, canonicalisation
    /// and hashing live in <see cref="SeeSaveNodeCodec"/> so the Integration layer cannot drift from
    /// the offline-tested codec.
    ///
    /// The world node (<c>Singletons.SEE</c>) only carries <c>schemaVersion</c>, <c>modVersion</c>,
    /// <c>checksum</c>, the four-field <c>buildings[]</c> index and the <c>compat</c> snapshot; the
    /// seven mutable per-boiler fields stay in the per-entity <c>ComponentKey("SEE")</c> component
    /// (REQ-SAVE-03, AD-11).
    /// </summary>
    public static class SaveSerializer
    {
        /// <summary>Decimal places of every canonical number (requirements §14.3).</summary>
        public const int CanonicalDecimals = SeeSaveChecksum.CanonicalDecimals;

        /// <summary>
        /// Serializes the world node in canonical form and appends its checksum.
        /// </summary>
        /// <param name="node">Node to write; must not be null.</param>
        /// <returns>Canonical JSON text including <c>checksum</c>.</returns>
        public static string Serialize(SeeSaveNode node)
        {
            return SeeSaveNodeCodec.Serialize(node);
        }

        /// <summary>
        /// Parses, repairs and verifies a world node. Never throws and never rejects a save
        /// (requirements §14.5).
        /// </summary>
        /// <param name="json">JSON text of the node; null/blank yields a new-game node.</param>
        /// <param name="log">Diagnostics sink; null is tolerated.</param>
        /// <returns>The repaired node plus the degradation flags.</returns>
        public static SeeSaveLoadResult Load(string? json, ISeeLog? log)
        {
            return SeeSaveNodeCodec.Load(json, log);
        }

        /// <summary>Serializes only the four-field building index (diagnostics/tests).</summary>
        /// <param name="node">Node whose index should be rendered.</param>
        /// <returns>Compact JSON of the index array.</returns>
        public static string SerializeIndex(SeeSaveNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var rows = new System.Collections.Generic.List<object?>(node.Buildings.Count);
            for (int i = 0; i < node.Buildings.Count; i++)
            {
                SeeSaveBuildingIndex row = node.Buildings[i];
                rows.Add(new System.Collections.Generic.Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["entityId"] = row.EntityId,
                    ["tier"] = row.Tier.ToString(),
                    ["size"] = row.Size.ToString(),
                    ["faction"] = row.Faction.ToString()
                });
            }

            return MiniJson.Write(rows);
        }

        /// <summary>Canonicalises any JSON document (sorted keys, four decimals, sorted buildings).</summary>
        /// <param name="json">JSON text to normalise.</param>
        /// <returns>Canonical JSON text.</returns>
        public static string Normalize(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return SeeSaveChecksum.Canonicalize(MiniJson.Parse(json));
        }

        /// <summary>Recomputes the checksum of an already serialized node document.</summary>
        /// <param name="serializedNode">Text produced by <see cref="Serialize"/>.</param>
        /// <returns>The expected <c>sha256:&lt;hex&gt;</c> string, or null for invalid input.</returns>
        public static string? ComputeChecksum(string serializedNode)
        {
            return SeeSaveNodeCodec.ComputeChecksum(serializedNode);
        }
    }
}
