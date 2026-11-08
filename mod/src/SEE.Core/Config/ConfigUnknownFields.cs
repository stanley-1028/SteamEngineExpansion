using System;
using System.Collections.Generic;

namespace SEE.Core.Config
{
    /// <summary>
    /// Preserves unknown configuration properties on write-back (CONTRACT_SEE §7.2: unknown fields
    /// are kept verbatim, missing fields use defaults). A newer or older settings file therefore
    /// survives a save/load cycle without losing its custom keys, while the known keys stay
    /// authoritative (a known key can never be overridden by a stale copy).
    /// </summary>
    internal static class ConfigUnknownFields
    {
        /// <summary>Deep-merges unknown properties of <paramref name="source"/> into <paramref name="target"/>.</summary>
        /// <param name="source">Document parsed from the original file.</param>
        /// <param name="target">Freshly serialized canonical document.</param>
        internal static void Merge(
            IDictionary<string, object?>? source,
            IDictionary<string, object?>? target)
        {
            if (source == null || target == null)
            {
                return;
            }

            foreach (KeyValuePair<string, object?> pair in source)
            {
                string? existingKey = FindKey(target, pair.Key);
                if (existingKey == null)
                {
                    // Unknown top-level property (or unknown child): keep it verbatim.
                    target[pair.Key] = pair.Value;
                    continue;
                }

                // Known key: when both sides are objects, recurse so unknown children are kept
                // (for example "balance": { "mysteryKnob": 123 }); otherwise the known value wins.
                if (target[existingKey] is IDictionary<string, object?> targetChild
                    && pair.Value is IDictionary<string, object?> sourceChild)
                {
                    Merge(sourceChild, targetChild);
                }
            }
        }

        private static string? FindKey(IDictionary<string, object?> map, string key)
        {
            if (map.ContainsKey(key))
            {
                return key;
            }

            foreach (string existing in map.Keys)
            {
                if (string.Equals(existing, key, StringComparison.OrdinalIgnoreCase))
                {
                    return existing;
                }
            }

            return null;
        }
    }
}
