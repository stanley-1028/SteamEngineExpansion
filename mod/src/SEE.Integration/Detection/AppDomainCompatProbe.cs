using System;
using System.Reflection;

namespace SEE.Integration.Detection
{
    /// <summary>
    /// Real three-stage probe (requirements §10.6, REQ-COMPAT-09): loaded assembly list → normalised
    /// assembly simple name → anchor type resolution with <see cref="Assembly.GetType(string,bool)"/>.
    /// No type is constructed and no member is invoked; every reflection call is individually
    /// guarded, so a half-loaded external mod degrades detection but never breaks SEE.
    /// </summary>
    public sealed class AppDomainCompatProbe : ICompatProbe
    {
        /// <inheritdoc />
        public CompatProbeResult Probe(string modId)
        {
            CompatProbeDefinition? definition = CompatAnchors.ById(modId);
            if (definition == null)
            {
                return CompatProbeResult.AssemblyNotFound();
            }

            Assembly[] assemblies;
            try
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception exception)
            {
                return CompatProbeResult.ProbeFailed("assembly-list-unavailable:" + exception.GetType().Name);
            }

            Assembly? match = null;
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly candidate = assemblies[i];
                string? simpleName = SafeName(candidate);
                if (simpleName == null)
                {
                    continue;
                }

                if (MatchesAny(simpleName, definition.AssemblyNames))
                {
                    match = candidate;
                    break;
                }
            }

            if (match == null)
            {
                return CompatProbeResult.AssemblyNotFound();
            }

            string? version = TryReadVersion(match);
            for (int i = 0; i < definition.AnchorTypes.Length; i++)
            {
                string anchor = definition.AnchorTypes[i];
                try
                {
                    // throwOnError:false returns null instead of throwing for an absent type.
                    if (match.GetType(anchor, false) == null)
                    {
                        return CompatProbeResult.AnchorNotFound(version, anchor);
                    }
                }
                catch (Exception exception)
                {
                    return CompatProbeResult.ProbeFailed("anchor-error:" + exception.GetType().Name);
                }
            }

            return CompatProbeResult.AssemblyFound(version);
        }

        private static bool MatchesAny(string assemblyName, string[] candidates)
        {
            string normalized = CompatAnchors.Normalize(assemblyName);
            if (normalized.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                if (string.Equals(normalized, CompatAnchors.Normalize(candidates[i]), StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string? SafeName(Assembly assembly)
        {
            try
            {
                return assembly.GetName().Name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string? TryReadVersion(Assembly assembly)
        {
            try
            {
                var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (informational != null && !string.IsNullOrEmpty(informational.InformationalVersion))
                {
                    return informational.InformationalVersion.Trim();
                }

                return assembly.GetName().Version?.ToString();
            }
            catch (Exception)
            {
                // Version metadata is best effort: a miss only leaves the diagnostic field null.
                return null;
            }
        }
    }
}
