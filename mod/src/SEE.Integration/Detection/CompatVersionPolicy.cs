using System;

namespace SEE.Integration.Detection
{
    /// <summary>
    /// Outcome of the version stage of the three-stage detection (requirements §10.6/§10.7).
    /// <para>
    /// The installed Workshop DLLs report <c>1.0.0+&lt;hash&gt;</c> as their assembly informational
    /// version while their manifests say 11.4.0 / 11.1.1 / 11.4.0. The two schemes are therefore not
    /// comparable: a version with a different major band is recorded as a diagnostic note but never
    /// blocks the adapter ("最低門檻僅作提示不阻擋"). Only a same-major version below the verified
    /// minimum is treated as a version mismatch and degrades the optional feature (REQ-COMPAT-10).
    /// </para>
    /// </summary>
    public enum VersionGateOutcome
    {
        /// <summary>The version scheme differs from the manifest minimum; recorded, never blocking.</summary>
        NotComparable = 0,

        /// <summary>The detected version is at or above the verified minimum.</summary>
        Verified = 1,

        /// <summary>A same-major version is below the verified minimum; treat as not installed.</summary>
        BelowMinimum = 2
    }

    /// <summary>Pure version-gate helper of the compatibility detector (requirements §10.6).</summary>
    public static class CompatVersionPolicy
    {
        /// <summary>Note stored when the assembly version scheme cannot be compared with the manifest.</summary>
        public const string AssemblyMetadataVersionNote = "assembly-metadata-version";

        /// <summary>Note stored when a same-major version failed the minimum gate.</summary>
        public const string BelowMinimumNote = "below-minimum";

        /// <summary>Evaluates one detected version against the verified manifest minimum.</summary>
        /// <param name="detectedVersion">Version text read from the assembly; null/empty when unknown.</param>
        /// <param name="minimumVersion">Verified manifest minimum; null/empty disables the gate.</param>
        /// <returns>The gate outcome; never throws.</returns>
        public static VersionGateOutcome Evaluate(string? detectedVersion, string? minimumVersion)
        {
            Version? actual = TryParse(detectedVersion);
            Version? minimum = TryParse(minimumVersion);
            if (actual == null || minimum == null)
            {
                return VersionGateOutcome.NotComparable;
            }

            if (actual.Major != minimum.Major)
            {
                // The DLL (1.0.0) and the manifest (11.x) use different schemes: a hint, not a block.
                return VersionGateOutcome.NotComparable;
            }

            if (actual.Major > minimum.Major)
            {
                return VersionGateOutcome.Verified;
            }

            if (actual.Minor != minimum.Minor)
            {
                return actual.Minor > minimum.Minor ? VersionGateOutcome.Verified : VersionGateOutcome.BelowMinimum;
            }

            return actual.Build >= minimum.Build ? VersionGateOutcome.Verified : VersionGateOutcome.BelowMinimum;
        }

        /// <summary>Parses the numeric prefix of a version string ("11.4.0", "1.0.0+abc", "11.4").</summary>
        /// <param name="text">Raw version text.</param>
        /// <returns>A parsed <see cref="Version"/>, or null when it cannot be parsed.</returns>
        public static Version? TryParse(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            try
            {
                int cut = text!.IndexOfAny(new[] { '+', '-', ' ' });
                string core = cut > 0 ? text.Substring(0, cut) : text;
                if (core.Length == 0)
                {
                    return null;
                }

                // Pad "11.4" to "11.4.0" because Version.TryParse requires at least two components.
                int dots = CountDots(core);
                if (dots == 0)
                {
                    core += ".0.0";
                }
                else if (dots == 1)
                {
                    core += ".0";
                }

                return Version.TryParse(core, out Version? parsed) ? parsed : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static int CountDots(string text)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '.')
                {
                    count++;
                }
            }

            return count;
        }
    }
}
