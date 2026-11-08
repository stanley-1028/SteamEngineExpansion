using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SEE.Core.Json;

namespace SEE.Core.Save
{
    /// <summary>
    /// Canonical JSON form and SHA-256 checksum of the world save node (requirements §14.3,
    /// REQ-SAVE-04; CONTRACT_SEE §6.4).
    ///
    /// Canonical rules: object keys sorted ordinally, no whitespace, invariant-culture numbers with
    /// four decimals for non-integral values, the root <c>buildings</c> array sorted by
    /// <c>entityId</c>. The checksum is computed over the node <b>without</b> the <c>checksum</c>
    /// field and formatted as <c>sha256:&lt;lowercase hex&gt;</c>. A mismatch never rejects a load.
    ///
    /// This type is pure BCL; it lives in Core (CONTRACT_SEE §2.1 "存檔編解碼") so the offline test
    /// runner can cover it without the game or the Integration assembly.
    /// </summary>
    public static class SeeSaveChecksum
    {
        /// <summary>Prefix of every checksum string.</summary>
        public const string Prefix = "sha256:";

        /// <summary>Decimal places used by the canonical form for non-integral numbers.</summary>
        public const int CanonicalDecimals = 4;

        /// <summary>Computes the checksum of an already canonical JSON document.</summary>
        /// <param name="canonicalJson">Canonical JSON text (see <see cref="Canonicalize"/>).</param>
        /// <returns>The <c>sha256:&lt;hex&gt;</c> string.</returns>
        public static string Compute(string canonicalJson)
        {
            if (canonicalJson == null)
            {
                throw new ArgumentNullException(nameof(canonicalJson));
            }

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonicalJson));
                var builder = new StringBuilder(Prefix.Length + (hash.Length * 2));
                builder.Append(Prefix);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        /// <summary>Verifies one checksum without throwing; any malformed value is a mismatch.</summary>
        /// <param name="canonicalJson">Canonical JSON text.</param>
        /// <param name="expected">Expected <c>sha256:&lt;hex&gt;</c> string, or null.</param>
        /// <returns>True when the recomputed checksum equals the expected one.</returns>
        public static bool Verify(string canonicalJson, string? expected)
        {
            if (canonicalJson == null || string.IsNullOrWhiteSpace(expected))
            {
                return false;
            }

            try
            {
                string actual = Compute(canonicalJson);
                return string.Equals(actual, expected!.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Converts any JSON tree into the canonical form used for hashing. The root
        /// <c>buildings</c> array is sorted by <c>entityId</c>; other array order is preserved.
        /// </summary>
        /// <param name="tree">Parsed JSON tree (dictionary/list/string/number/bool/null).</param>
        /// <returns>Canonical compact JSON text.</returns>
        /// <exception cref="FormatException">When the tree contains NaN/Infinity or an unsupported value.</exception>
        public static string Canonicalize(object? tree)
        {
            SortBuildings(tree);
            var builder = new StringBuilder(256);
            WriteCanonical(builder, tree);
            return builder.ToString();
        }

        private static void SortBuildings(object? tree)
        {
            if (tree is IDictionary<string, object?> root
                && root.TryGetValue("buildings", out object? buildings)
                && buildings is List<object?> list)
            {
                list.Sort(CompareBuildings);
            }
        }

        private static int CompareBuildings(object? left, object? right)
        {
            string leftId = left is IDictionary<string, object?> leftMap
                ? MiniJson.GetString(leftMap, "entityId", string.Empty)
                : string.Empty;
            string rightId = right is IDictionary<string, object?> rightMap
                ? MiniJson.GetString(rightMap, "entityId", string.Empty)
                : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }

        private static void WriteCanonical(StringBuilder builder, object? value)
        {
            switch (value)
            {
                case null:
                    builder.Append("null");
                    return;
                case string text:
                    WriteString(builder, text);
                    return;
                case bool flag:
                    builder.Append(flag ? "true" : "false");
                    return;
                case IDictionary<string, object?> map:
                    WriteObject(builder, map);
                    return;
                case IList list:
                    WriteArray(builder, list);
                    return;
                case double number:
                    WriteNumber(builder, number);
                    return;
                case float number:
                    WriteNumber(builder, number);
                    return;
                case int number:
                    builder.Append(number.ToString(CultureInfo.InvariantCulture));
                    return;
                case long number:
                    builder.Append(number.ToString(CultureInfo.InvariantCulture));
                    return;
                case short number:
                    builder.Append(number.ToString(CultureInfo.InvariantCulture));
                    return;
                case byte number:
                    builder.Append(number.ToString(CultureInfo.InvariantCulture));
                    return;
                case decimal number:
                    builder.Append(number.ToString(CultureInfo.InvariantCulture));
                    return;
                default:
                    throw new FormatException("Unsupported canonical JSON value: " + value.GetType().Name);
            }
        }

        private static void WriteObject(StringBuilder builder, IDictionary<string, object?> map)
        {
            var keys = new List<string>(map.Keys);
            keys.Sort(StringComparer.Ordinal);
            builder.Append('{');
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                WriteString(builder, keys[i]);
                builder.Append(':');
                WriteCanonical(builder, map[keys[i]]);
            }

            builder.Append('}');
        }

        private static void WriteArray(StringBuilder builder, IList list)
        {
            builder.Append('[');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                WriteCanonical(builder, list[i]);
            }

            builder.Append(']');
        }

        private static void WriteNumber(StringBuilder builder, double number)
        {
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                throw new FormatException("JSON cannot represent NaN or Infinity.");
            }

            if (number == Math.Truncate(number) && Math.Abs(number) < 1e15)
            {
                // Integral values keep an integer form so a parse/write cycle is byte stable
                // (JSON parses every number as double; padding integers would break the hash).
                builder.Append(((long)number).ToString(CultureInfo.InvariantCulture));
                return;
            }

            builder.Append(number.ToString("F" + CanonicalDecimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));
        }

        private static void WriteString(StringBuilder builder, string text)
        {
            builder.Append('"');
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            builder.Append('"');
        }
    }
}
