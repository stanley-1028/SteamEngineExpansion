using System;

namespace SEE.Integration.Detection
{
    /// <summary>
    /// One frozen probe description of an optional mod: stable ids, assembly simple names, the
    /// anchor type names verified against the installed Workshop DLLs and the manifest minimum
    /// version used only as a diagnostic hint (requirements §10.6).
    ///
    /// Every anchor below was read from the real metadata with <c>tools/ApiIndex dump</c> on
    /// 2026-11-06 (game 1.1.2.8, Workshop 3801726769 / 3766893705 / 3815376360). See
    /// <c>docs/API_FACTS_SEE.md</c> §7 for the raw dump evidence.
    /// </summary>
    public sealed class CompatProbeDefinition
    {
        /// <summary>Creates a probe description.</summary>
        /// <param name="modId">Stable mod id ("TimberPipes", "PowerLines", "ConveyorBelt").</param>
        /// <param name="adapterId">Stable adapter id written into the save compat block.</param>
        /// <param name="assemblyNames">Accepted assembly simple names (before normalisation).</param>
        /// <param name="anchorTypes">Full type names that must all resolve.</param>
        /// <param name="minimumVersion">Manifest version the mod was verified against; a hint only.</param>
        public CompatProbeDefinition(
            string modId,
            string adapterId,
            string[] assemblyNames,
            string[] anchorTypes,
            string minimumVersion)
        {
            if (string.IsNullOrEmpty(modId))
            {
                throw new ArgumentException("modId is required", nameof(modId));
            }

            ModId = modId;
            AdapterId = adapterId ?? string.Empty;
            AssemblyNames = Copy(assemblyNames);
            AnchorTypes = Copy(anchorTypes);
            MinimumVersion = minimumVersion ?? string.Empty;
        }

        /// <summary>Stable mod id.</summary>
        public string ModId { get; }

        /// <summary>Stable adapter id ("SEE.Adapter.TimberPipes", ...); empty for the legacy probe.</summary>
        public string AdapterId { get; }

        /// <summary>Assembly simple names this probe accepts.</summary>
        public string[] AssemblyNames { get; }

        /// <summary>Anchor type full names that must all resolve.</summary>
        public string[] AnchorTypes { get; }

        /// <summary>Verified manifest version; compared only when the assembly version shares its major band.</summary>
        public string MinimumVersion { get; }

        private static string[] Copy(string[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<string>();
            }

            var copy = new string[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }

    /// <summary>
    /// Frozen detection constants of the three optional mods (requirements §10.6; anchors verified
    /// against the installed workshop DLL metadata). These are strings on purpose: SEE.Integration
    /// never references the external assemblies, it only asks the already-loaded AppDomain whether
    /// the assembly simple name and the anchor types are present (REQ-COMPAT-08).
    /// </summary>
    public static class CompatAnchors
    {
        /// <summary>Stable mod id of Timber Pipes.</summary>
        public const string TimberPipes = "TimberPipes";

        /// <summary>Stable mod id of Power Lines.</summary>
        public const string PowerLines = "PowerLines";

        /// <summary>Stable mod id of Conveyor Belts v2 (manifest Id "ConveyorBelt").</summary>
        public const string ConveyorBelt = "ConveyorBelt";

        /// <summary>Adapter id of the Timber Pipes adapter (written into the save compat block).</summary>
        public const string TimberPipesAdapterId = "SEE.Adapter.TimberPipes";

        /// <summary>Adapter id of the Power Lines adapter.</summary>
        public const string PowerLinesAdapterId = "SEE.Adapter.PowerLines";

        /// <summary>Adapter id of the Conveyor Belts v2 adapter (requirements §10.3.3 plural form).</summary>
        public const string ConveyorBeltAdapterId = "SEE.Adapter.ConveyorBelts";

        /// <summary>Stable mod id used for the unsupported legacy Power Lines build (X3, REQ-COMPAT-16).</summary>
        public const string LegacyPowerLines = "Opolame.PowerLines";

        /// <summary>Oldest Timber Pipes version the mod was verified against (hint only, never blocks).</summary>
        public const string TimberPipesMinVersion = "11.4.0";

        /// <summary>Oldest Power Lines version the mod was verified against (hint only).</summary>
        public const string PowerLinesMinVersion = "11.1.1";

        /// <summary>Oldest Conveyor Belts version the mod was verified against (hint only).</summary>
        public const string ConveyorBeltMinVersion = "11.4.0";

        /// <summary>Assembly simple names of Timber Pipes.</summary>
        public static readonly string[] TimberPipesAssemblies = { "TimberPipes" };

        /// <summary>Assembly simple names of Power Lines.</summary>
        public static readonly string[] PowerLinesAssemblies = { "PowerLines" };

        /// <summary>Assembly simple names of Conveyor Belts v2 (the algorithm assembly ships too).</summary>
        public static readonly string[] ConveyorBeltAssemblies = { "ConveyorBelt", "ConveyorBelt.Algorithm" };

        /// <summary>Assembly simple names of the unsupported legacy Power Lines build.</summary>
        public static readonly string[] LegacyPowerLinesAssemblies = { "Opolame.PowerLines" };

        /// <summary>Anchor types of Timber Pipes; all must resolve for <c>Available</c>.</summary>
        public static readonly string[] TimberPipesTypes =
        {
            "TimberPipes.Specs.BuildingPipeSpec",
            "TimberPipes.Specs.PipePortState",
            "TimberPipes.Models.PipeFluids"
        };

        /// <summary>Anchor types of Power Lines.</summary>
        public static readonly string[] PowerLinesTypes =
        {
            "PowerLines.Specs.PowerLineSpec",
            "PowerLines.Components.PowerLineComponent"
        };

        /// <summary>Anchor types of Conveyor Belts v2.</summary>
        public static readonly string[] ConveyorBeltTypes =
        {
            "ConveyorBelt.Specs.BeltSystemSpec",
            "ConveyorBelt.Components.BuildingBeltTarget"
        };

        /// <summary>Timber Pipes probe description.</summary>
        public static readonly CompatProbeDefinition TimberPipesProbe = new CompatProbeDefinition(
            TimberPipes, TimberPipesAdapterId, TimberPipesAssemblies, TimberPipesTypes, TimberPipesMinVersion);

        /// <summary>Power Lines probe description.</summary>
        public static readonly CompatProbeDefinition PowerLinesProbe = new CompatProbeDefinition(
            PowerLines, PowerLinesAdapterId, PowerLinesAssemblies, PowerLinesTypes, PowerLinesMinVersion);

        /// <summary>Conveyor Belts v2 probe description.</summary>
        public static readonly CompatProbeDefinition ConveyorBeltProbe = new CompatProbeDefinition(
            ConveyorBelt, ConveyorBeltAdapterId, ConveyorBeltAssemblies, ConveyorBeltTypes, ConveyorBeltMinVersion);

        /// <summary>
        /// Legacy Power Lines probe: assembly-name only (no anchor, no adapter). It is never
        /// integrated or patched, only reported to the player (REQ-COMPAT-16).
        /// </summary>
        public static readonly CompatProbeDefinition LegacyPowerLinesProbe = new CompatProbeDefinition(
            LegacyPowerLines, string.Empty, LegacyPowerLinesAssemblies, Array.Empty<string>(), string.Empty);

        /// <summary>Fixed detection order (Timber Pipes, Power Lines, Conveyor Belts).</summary>
        public static readonly CompatProbeDefinition[] All = { TimberPipesProbe, PowerLinesProbe, ConveyorBeltProbe };

        /// <summary>Finds a probe description by exact mod id; null when unknown or empty.</summary>
        /// <param name="modId">Stable mod id.</param>
        /// <returns>The description or null.</returns>
        public static CompatProbeDefinition? ById(string? modId)
        {
            if (string.IsNullOrEmpty(modId))
            {
                return null;
            }

            if (string.Equals(modId, LegacyPowerLines, StringComparison.Ordinal))
            {
                return LegacyPowerLinesProbe;
            }

            for (int i = 0; i < All.Length; i++)
            {
                if (string.Equals(All[i].ModId, modId, StringComparison.Ordinal))
                {
                    return All[i];
                }
            }

            return null;
        }

        /// <summary>Normalises an assembly name for tolerant comparison ("Timber Pipes" == "TimberPipes").</summary>
        /// <param name="value">Raw name; null/empty yields the empty string.</param>
        /// <returns>Lower-case alphanumeric form.</returns>
        public static string Normalize(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder(value!.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
            }

            return builder.ToString();
        }
    }
}
