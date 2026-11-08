using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Json;
using SEE.Core.Logging;

namespace SEE.Core.Save
{
    /// <summary>
    /// Defensive parser of the world save node (requirements §14.5, REQ-SAVE-04/06). Every read is
    /// guarded: missing fields keep their documented default, wrong types are repaired in place,
    /// one damaged building row never fails the whole node, and a checksum mismatch only reports a
    /// warning. Internal helper of <see cref="SeeSaveNodeCodec"/>.
    /// </summary>
    internal static class SeeSaveNodeParser
    {
        internal static SeeSaveLoadResult Load(string? json, ISeeLog? log)
        {
            var state = new SeeParseState(log);
            if (string.IsNullOrWhiteSpace(json))
            {
                // No node: a new world or a save written before SEE existed. Not damage.
                return Complete(state, SeeSaveNode.NewGame(), false);
            }

            object? tree;
            try
            {
                tree = MiniJson.Parse(json!);
            }
            catch (Exception)
            {
                state.AddRepair("SEE save node is not valid JSON; rebuilding an empty node");
                return Complete(state, SeeSaveNode.NewGame(), false);
            }

            if (!(tree is IDictionary<string, object?> root))
            {
                state.AddRepair("SEE save node is not a JSON object; rebuilding an empty node");
                return Complete(state, SeeSaveNode.NewGame(), false);
            }

            return LoadRoot(root, state);
        }

        private static SeeSaveLoadResult Complete(SeeParseState state, SeeSaveNode node, bool readOnly)
        {
            var result = new SeeSaveLoadResult(node, readOnly, state.Damaged, state.Repairs);
            result.Report(state.Log);
            return result;
        }

        private static SeeSaveLoadResult LoadRoot(IDictionary<string, object?> root, SeeParseState state)
        {
            var node = new SeeSaveNode();
            bool readOnly = ReadHeader(root, node, state);
            ParseBuildings(root, node, state);
            ParseCompat(root, node, state);
            CollectUnknown(root, node);
            return Complete(state, node, readOnly);
        }

        private static bool ReadHeader(IDictionary<string, object?> root, SeeSaveNode node, SeeParseState state)
        {
            node.SchemaVersion = ReadSchemaVersion(root, state);
            node.ModVersion = ReadRequiredString(root, "modVersion", SEEVersion.ModVersion, state);
            string checksum = ReadRequiredString(root, "checksum", string.Empty, state);
            node.Checksum = checksum;
            VerifyChecksum(root, checksum, state);
            return node.SchemaVersion > SEEVersion.SaveSchemaVersion;
        }

        private static int ReadSchemaVersion(IDictionary<string, object?> root, SeeParseState state)
        {
            if (!MiniJson.TryGet(root, "schemaVersion", out object? raw))
            {
                return SEEVersion.SaveSchemaVersion;
            }

            if (raw is double number && !double.IsNaN(number) && !double.IsInfinity(number)
                && number >= 0.0 && number <= int.MaxValue)
            {
                int version = (int)number;
                return version < 1 ? 1 : version;
            }

            state.AddRepair("schemaVersion is not a valid integer; treating the node as v1");
            return 1;
        }

        private static string ReadRequiredString(
            IDictionary<string, object?> root,
            string key,
            string fallback,
            SeeParseState state)
        {
            if (!MiniJson.TryGet(root, key, out object? raw))
            {
                return fallback;
            }

            if (raw is string text)
            {
                return text;
            }

            state.AddRepair(key + " is not a string; using the default");
            return fallback;
        }

        private static void VerifyChecksum(IDictionary<string, object?> root, string stored, SeeParseState state)
        {
            if (string.IsNullOrWhiteSpace(stored))
            {
                // Requirement §14.8 case 5: a deleted checksum only warns; the node still loads.
                state.Warn("checksum is missing; loading without verification");
                return;
            }

            var withoutChecksum = new Dictionary<string, object?>(root, StringComparer.Ordinal);
            SeeSaveNodeWriter.RemoveKey(withoutChecksum, "checksum");
            try
            {
                string canonical = SeeSaveChecksum.Canonicalize(withoutChecksum);
                if (!SeeSaveChecksum.Verify(canonical, stored))
                {
                    state.Warn("checksum mismatch; loading the data without rejecting the save");
                }
            }
            catch (Exception)
            {
                state.AddRepair("checksum could not be verified");
            }
        }

        private static void ParseBuildings(
            IDictionary<string, object?> root,
            SeeSaveNode node,
            SeeParseState state)
        {
            if (!MiniJson.TryGet(root, "buildings", out object? raw))
            {
                // Requirement §14.5: a missing buildings array is an empty index, not damage.
                return;
            }

            if (!(raw is IList<object?> list))
            {
                state.AddRepair("buildings is not an array; using an empty index");
                return;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < list.Count; i++)
            {
                var map = list[i] as IDictionary<string, object?>;
                if (map == null)
                {
                    state.AddRepair("building entry " + i + " is not an object; skipped");
                    continue;
                }

                string? entityId = ReadEntityId(map, i, state);
                if (entityId == null)
                {
                    continue;
                }

                if (!seen.Add(entityId))
                {
                    state.AddRepair("duplicate entityId " + entityId + "; keeping the first entry");
                    continue;
                }

                node.Buildings.Add(ReadBuilding(map, entityId, state));
            }
        }

        private static string? ReadEntityId(IDictionary<string, object?> map, int index, SeeParseState state)
        {
            string text = MiniJson.GetString(map, "entityId", string.Empty);
            if (string.IsNullOrWhiteSpace(text))
            {
                state.AddRepair("building entry " + index + " has no entityId; skipped");
                return null;
            }

            if (!Guid.TryParse(text, out Guid guid))
            {
                state.AddRepair("building entry " + index + " has a non-GUID entityId; skipped");
                return null;
            }

            return guid.ToString("D");
        }

        private static SeeSaveBuildingIndex ReadBuilding(
            IDictionary<string, object?> map,
            string entityId,
            SeeParseState state)
        {
            var index = new SeeSaveBuildingIndex
            {
                EntityId = entityId,
                Tier = ReadEnum(map, "tier", BoilerTier.T1, IsDefined, state),
                Size = ReadEnum(map, "size", BoilerSize.Small, IsDefined, state),
                Faction = ReadEnum(map, "faction", BoilerFaction.Folktails, IsDefined, state)
            };

            foreach (KeyValuePair<string, object?> pair in map)
            {
                if (!SeeSaveNodeWriter.IsKnown(SeeSaveNodeWriter.KnownBuilding, pair.Key))
                {
                    index.UnknownFields[pair.Key] = pair.Value;
                }
            }

            return index;
        }

        private static T ReadEnum<T>(
            IDictionary<string, object?> map,
            string key,
            T fallback,
            Func<T, bool> isDefined,
            SeeParseState state)
            where T : struct
        {
            if (!MiniJson.TryGet(map, key, out object? raw))
            {
                return fallback;
            }

            if (raw is string text && Enum.TryParse(text, true, out T parsed) && isDefined(parsed))
            {
                return parsed;
            }

            state.AddRepair("building field " + key + " has an invalid value; using the default");
            return fallback;
        }

        private static void ParseCompat(IDictionary<string, object?> root, SeeSaveNode node, SeeParseState state)
        {
            if (!MiniJson.TryGet(root, "compat", out object? raw))
            {
                return;
            }

            if (!(raw is IDictionary<string, object?> compat))
            {
                state.AddRepair("compat is not an object; using empty lists");
                return;
            }

            ReadStringArray(compat, "detected", node.Compat.Detected, state);
            ReadStringArray(compat, "enabled", node.Compat.Enabled, state);
            ReadStringArray(compat, "failed", node.Compat.Failed, state);
        }

        private static void ReadStringArray(
            IDictionary<string, object?> map,
            string key,
            List<string> target,
            SeeParseState state)
        {
            if (!MiniJson.TryGet(map, key, out object? raw))
            {
                return;
            }

            if (!(raw is IList<object?> list))
            {
                state.AddRepair("compat." + key + " is not an array; using an empty list");
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is string text)
                {
                    target.Add(text);
                }
                else
                {
                    state.AddRepair("compat." + key + " entry " + i + " is not a string; dropped");
                }
            }
        }

        private static void CollectUnknown(IDictionary<string, object?> root, SeeSaveNode node)
        {
            foreach (KeyValuePair<string, object?> pair in root)
            {
                if (!SeeSaveNodeWriter.IsKnown(SeeSaveNodeWriter.KnownTopLevel, pair.Key))
                {
                    node.UnknownFields[pair.Key] = pair.Value;
                }
            }
        }

        private static bool IsDefined(BoilerTier tier) =>
            tier == BoilerTier.T1 || tier == BoilerTier.T2 || tier == BoilerTier.T3 || tier == BoilerTier.T4;

        private static bool IsDefined(BoilerSize size) =>
            size == BoilerSize.Small || size == BoilerSize.Medium || size == BoilerSize.Large;

        private static bool IsDefined(BoilerFaction faction) =>
            faction == BoilerFaction.Folktails || faction == BoilerFaction.IronTeeth;

        /// <summary>Mutable repair counter shared by one load pass.</summary>
        internal sealed class SeeParseState
        {
            private readonly ISeeLog? _log;

            internal SeeParseState(ISeeLog? log)
            {
                _log = log;
            }

            internal ISeeLog? Log => _log;

            internal int Repairs { get; private set; }

            internal bool Damaged => Repairs > 0;

            internal void AddRepair(string message)
            {
                Repairs++;
                Warn(message);
            }

            internal void Warn(string message)
            {
                try
                {
                    _log?.Warn(SeeLogKeys.SaveCorrupt, message);
                }
                catch (Exception)
                {
                    // Diagnostics only: a broken log sink must not break loading (REQ-SAFE-02).
                }
            }
        }
    }
}
