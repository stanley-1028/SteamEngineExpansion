using System;
using System.Collections.Generic;
using SEE.Core.Constants;
using SEE.Core.Json;
using SEE.Core.Logging;

namespace SEE.Core.Save
{
    /// <summary>
    /// Codec of the world save node <c>SEE</c> (requirements §14.1/§14.3/§14.4/§14.5;
    /// CONTRACT_SEE §6, D-6 hybrid form, AD-11).
    ///
    /// <list type="bullet">
    /// <item><b>Parse/Load</b>: never throws and never rejects a save. Invalid fields are repaired
    /// in place, one damaged building row is isolated, and a checksum mismatch only warns (§14.5).</item>
    /// <item><b>Serialize</b>: writes the canonical form (sorted keys, four decimals for fractional
    /// numbers, buildings sorted by entityId) and the checksum of the node without that field
    /// (§14.3).</item>
    /// <item><b>Migrate</b>: forward-only chain, v1 ships zero steps; a newer schema returns null
    /// so the caller degrades to a read-only load instead of discarding data (§14.4).</item>
    /// </list>
    ///
    /// The per-boiler mutable state stays in the per-entity component (<see cref="SeeSaveCodec"/>/
    /// <see cref="BoilerSaveState"/>); together the node and the entity hold the eleven §14.1
    /// fields. Unknown JSON properties are preserved on both levels (REQ-SAVE-02).
    /// </summary>
    public static class SeeSaveNodeCodec
    {
        /// <summary>Schema version this codec writes (shared by the node and the entities).</summary>
        public static int CurrentSchemaVersion => SEEVersion.SaveSchemaVersion;

        /// <summary>Migration chain; empty for schema v1 (requirements §14.4).</summary>
        public static IReadOnlyList<ISaveMigration> Migrations => SeeSaveCodec.Migrations;

        /// <summary>Parses a node and returns it without the load-result metadata.</summary>
        /// <param name="json">JSON text of the node; null/blank yields an empty node.</param>
        /// <param name="log">Diagnostics sink; null is tolerated.</param>
        /// <returns>The repaired node; never null.</returns>
        public static SeeSaveNode Parse(string? json, ISeeLog? log)
        {
            return SeeSaveNodeParser.Load(json, log).Node;
        }

        /// <summary>Parses, repairs and verifies a node, reporting the degradation flags.</summary>
        /// <param name="json">JSON text of the node; null/blank yields a new-game node.</param>
        /// <param name="log">Diagnostics sink; null is tolerated.</param>
        /// <returns>The repaired node plus <c>ReadOnly</c>/<c>Damaged</c>/<c>Repairs</c>.</returns>
        public static SeeSaveLoadResult Load(string? json, ISeeLog? log)
        {
            return SeeSaveNodeParser.Load(json, log);
        }

        /// <summary>
        /// Serializes the node to the canonical form and appends the checksum computed over the
        /// document without the <c>checksum</c> field.
        /// </summary>
        /// <param name="node">Node to write; must not be null.</param>
        /// <returns>Canonical JSON text including <c>checksum</c>.</returns>
        public static string Serialize(SeeSaveNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Dictionary<string, object?> tree = SeeSaveNodeWriter.BuildTree(node);
            string canonical = SeeSaveChecksum.Canonicalize(tree);
            tree["checksum"] = SeeSaveChecksum.Compute(canonical);
            return SeeSaveChecksum.Canonicalize(tree);
        }

        /// <summary>Recomputes the checksum of an already serialized node document.</summary>
        /// <param name="serializedNode">Text produced by <see cref="Serialize"/>.</param>
        /// <returns>The expected <c>sha256:&lt;hex&gt;</c> string, or null for invalid input.</returns>
        public static string? ComputeChecksum(string serializedNode)
        {
            if (string.IsNullOrWhiteSpace(serializedNode))
            {
                return null;
            }

            try
            {
                if (!(MiniJson.Parse(serializedNode!) is IDictionary<string, object?> root))
                {
                    return null;
                }

                string canonical = SeeSaveChecksum.Canonicalize(WithoutChecksum(root));
                return SeeSaveChecksum.Compute(canonical);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Applies the migration chain from <paramref name="fromVersion"/> to the current schema.
        /// v1 is already current, so it returns the text unchanged; a newer version returns null
        /// (the caller degrades to a read-only load). A failed step returns null and never throws.
        /// </summary>
        /// <param name="fromVersion">Schema version of <paramref name="json"/>.</param>
        /// <param name="json">Serialized document to migrate.</param>
        /// <returns>Migrated text, or null when the version cannot be migrated.</returns>
        public static string? Migrate(int fromVersion, string? json)
        {
            return Migrate(fromVersion, json, CurrentSchemaVersion, null);
        }

        /// <summary>
        /// Applies an explicit migration chain to an explicit target version. Used by tests to model
        /// a future v2 step while the shipped chain stays empty (requirements §14.4).
        /// </summary>
        /// <param name="fromVersion">Schema version of <paramref name="json"/>.</param>
        /// <param name="json">Serialized document to migrate.</param>
        /// <param name="targetVersion">Target schema version (normally <see cref="CurrentSchemaVersion"/>).</param>
        /// <param name="chain">Chain to apply; null uses the shipped chain.</param>
        /// <returns>The migrated text, or null when it cannot reach the target.</returns>
        public static string? Migrate(
            int fromVersion,
            string? json,
            int targetVersion,
            IReadOnlyList<ISaveMigration>? chain)
        {
            if (json == null || fromVersion < 0 || fromVersion > targetVersion)
            {
                return null;
            }

            IReadOnlyList<ISaveMigration> steps = chain ?? Migrations;
            string current = json;
            int version = fromVersion;
            for (int i = 0; i < steps.Count; i++)
            {
                ISaveMigration step = steps[i];
                if (step.FromVersion != version)
                {
                    continue;
                }

                try
                {
                    string? migrated = step.Migrate(current);
                    if (migrated == null)
                    {
                        return null;
                    }

                    current = migrated;
                    version = step.ToVersion;
                }
                catch (Exception)
                {
                    // A migration must never throw into the game loop (requirements §14.4).
                    return null;
                }
            }

            return version == targetVersion ? current : null;
        }

        private static IDictionary<string, object?> WithoutChecksum(IDictionary<string, object?> root)
        {
            var copy = new Dictionary<string, object?>(root, StringComparer.Ordinal);
            string? key = null;
            foreach (string candidate in copy.Keys)
            {
                if (string.Equals(candidate, "checksum", StringComparison.OrdinalIgnoreCase))
                {
                    key = candidate;
                    break;
                }
            }

            if (key != null)
            {
                copy.Remove(key);
            }

            return copy;
        }
    }
}
