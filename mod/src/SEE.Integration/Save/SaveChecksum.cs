using System;
using SEE.Core.Json;
using SEE.Core.Save;

namespace SEE.Integration.Save
{
    /// <summary>
    /// SHA-256 checksum bridge of the world save node (requirements §14.3, REQ-SAVE-04).
    ///
    /// The algorithm and the canonical JSON form live in <see cref="SeeSaveChecksum"/> (SEE.Core),
    /// so the offline codec tests and the runtime integration hash exactly the same bytes. A
    /// mismatch only produces one warning and triggers per-boiler validation — it never rejects a
    /// load, never overwrites the original save and is not anti-cheat.
    /// </summary>
    public static class SaveChecksum
    {
        /// <summary>Prefix of the checksum string.</summary>
        public const string Prefix = SeeSaveChecksum.Prefix;

        /// <summary>Decimal places of every canonical number.</summary>
        public const int CanonicalDecimals = SeeSaveChecksum.CanonicalDecimals;

        /// <summary>Computes the checksum of an already canonical JSON document.</summary>
        /// <param name="normalizedJson">Canonical JSON text.</param>
        /// <returns>The <c>sha256:&lt;hex&gt;</c> string.</returns>
        public static string Compute(string normalizedJson)
        {
            return SeeSaveChecksum.Compute(normalizedJson);
        }

        /// <summary>Converts any JSON document into the canonical form used for hashing.</summary>
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

        /// <summary>Verifies one checksum without throwing; any malformed value is a mismatch.</summary>
        /// <param name="normalizedJson">Canonical JSON text.</param>
        /// <param name="expected">Expected <c>sha256:&lt;hex&gt;</c> string, or null.</param>
        /// <returns>True when the recomputed checksum equals the expected one.</returns>
        public static bool Verify(string normalizedJson, string? expected)
        {
            return SeeSaveChecksum.Verify(normalizedJson, expected);
        }
    }
}
