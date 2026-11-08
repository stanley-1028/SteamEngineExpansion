using System;
using System.Collections.Generic;
using SEE.Core.Json;
using SEE.Integration.Detection;

namespace SEE.Integration.Patching
{
    /// <summary>
    /// Builds the conditional <c>PowerLineSpec</c> JSON fragment merged into SEE boiler blueprints
    /// (requirements §10.4/§8.7, REQ-POWER-16). This is the non-Harmony injection path proven from
    /// the game IL: <c>SpecService</c> calls every <c>IBlueprintModifierProvider.GetModifiers(path)</c>
    /// and merges the returned JSON documents into the bundle before deserializing the blueprint
    /// (<c>BlueprintFileBundle.AddJson</c> → <c>SerializedObjectReaderWriter.ReadJsons</c> →
    /// <c>JsonMerger.Merge</c>). See <c>docs/API_FACTS_SEE.md</c> §7.3.
    /// <para>
    /// The main blueprint files never contain <c>PowerLineSpec</c>: the fragment is produced only
    /// when Power Lines is detected and <c>compat.enablePowerLineInjection</c> is on, so the
    /// blueprint stays loadable with or without the optional mod (REQ-COMPAT-11).
    /// </para>
    /// </summary>
    public static class PowerLineInjectionModifier
    {
        /// <summary>Source name reported to the blueprint merger.</summary>
        public const string ModifierName = "SEE.PowerLines";

        /// <summary>Connection point count per boiler: left, right and back faces (requirements §10.4).</summary>
        public const int ConnectionPointCount = 3;

        /// <summary>Default maximum connections copied from the vanilla generator default.</summary>
        public const int DefaultMaxConnections = 4;

        /// <summary>Default maximum cable length in metres.</summary>
        public const float DefaultMaxConnectionLength = 5.0f;

        /// <summary>
        /// Builds the modifier JSON for one boiler size. Coordinates follow requirements §10.4:
        /// <c>(0, S/2, 0.5)</c>, <c>(S, S/2, 0.5)</c> and <c>(S/2, S, 0.5)</c> with 0.5 m height.
        /// </summary>
        /// <param name="size">Footprint side length S: 2 (Small), 3 (Medium) or 4 (Large).</param>
        /// <returns>Compact JSON object with a single <c>PowerLineSpec</c> key.</returns>
        public static string BuildModifierJson(int size)
        {
            if (size < 2 || size > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "boiler size must be 2, 3 or 4");
            }

            double half = size / 2.0;
            var points = new List<object?>(ConnectionPointCount)
            {
                Point(0.0, half),
                Point(size, half),
                Point(half, size)
            };

            var spec = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["ConnectionLocations"] = points,
                ["MaxConnections"] = DefaultMaxConnections,
                ["MaxConnectionLength"] = (double)DefaultMaxConnectionLength
            };

            var root = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["PowerLineSpec"] = spec
            };
            return MiniJson.Write(root);
        }

        /// <summary>
        /// Pure injection gate: the fragment is only produced when the config switch is on and the
        /// three-stage detection returned <see cref="Availability.Available"/> (REQ-COMPAT-11).
        /// </summary>
        /// <param name="configEnabled"><c>compat.enablePowerLineInjection</c>.</param>
        /// <param name="powerLinesLevel">Detection level of Power Lines.</param>
        /// <returns>True when a fragment may be returned.</returns>
        public static bool ShouldInject(bool configEnabled, Availability powerLinesLevel)
        {
            return configEnabled && powerLinesLevel == Availability.Available;
        }

        /// <summary>Maps a boiler footprint keyword to its side length.</summary>
        /// <param name="sizeName">"Small", "Medium" or "Large".</param>
        /// <param name="size">Side length when recognised.</param>
        /// <returns>True when the name is a boiler size.</returns>
        public static bool TryParseSizeName(string? sizeName, out int size)
        {
            if (string.Equals(sizeName, "Small", StringComparison.OrdinalIgnoreCase))
            {
                size = 2;
                return true;
            }

            if (string.Equals(sizeName, "Medium", StringComparison.OrdinalIgnoreCase))
            {
                size = 3;
                return true;
            }

            if (string.Equals(sizeName, "Large", StringComparison.OrdinalIgnoreCase))
            {
                size = 4;
                return true;
            }

            size = 0;
            return false;
        }

        /// <summary>
        /// Parses one SEE boiler blueprint path and builds its modifier JSON. The path is the one the
        /// game passes to modifier providers (<c>Buildings/Power/SteamBoiler/SteamBoiler.&lt;Size&gt;.&lt;Tier&gt;.&lt;Faction&gt;</c>,
        /// without extension), but a trailing ".blueprint.json" is tolerated.
        /// </summary>
        /// <param name="blueprintPath">Blueprint path supplied by the game.</param>
        /// <param name="json">Modifier JSON when the path belongs to a SEE boiler.</param>
        /// <returns>True when a fragment was produced.</returns>
        public static bool TryBuildForPath(string? blueprintPath, out string json)
        {
            json = string.Empty;
            if (string.IsNullOrEmpty(blueprintPath))
            {
                return false;
            }

            if (blueprintPath!.IndexOf("Buildings/Power/SteamBoiler/", StringComparison.Ordinal) < 0)
            {
                return false;
            }

            string name = blueprintPath;
            int slash = name.LastIndexOf('/');
            if (slash >= 0)
            {
                name = name.Substring(slash + 1);
            }

            // Template name: SteamBoiler.<Size>.<Tier>.<Faction>.
            string[] parts = name.Split('.');
            if (parts.Length < 4 || !string.Equals(parts[0], "SteamBoiler", StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryParseSizeName(parts[1], out int size))
            {
                return false;
            }

            json = BuildModifierJson(size);
            return true;
        }

        private static object? Point(double x, double y)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["X"] = x,
                ["Y"] = y,
                ["Z"] = 0.5
            };
        }
    }
}
