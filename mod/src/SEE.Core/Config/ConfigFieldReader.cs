using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SEE.Core.Json;
using SEE.Core.Logging;

namespace SEE.Core.Config
{
    /// <summary>
    /// Scalar/collection field readers for config schema 1 (requirements §20, CONTRACT_SEE §7).
    /// Every read is defensive: a missing key keeps the fallback silently, a wrong-typed or
    /// non-finite value keeps the fallback with one throttled warning, and an out-of-range number
    /// is clamped with a warning (REQ-SAFE-01).
    ///
    /// Two JSON layouts are accepted for the per-tier/per-size tables, because the shipped
    /// <c>SEE_Config.json</c> uses the documented nested-object form
    /// (<c>"fuelRatePer100hp": {"T1": 0.032, …}</c>) while tests may use compact arrays
    /// (<c>"fuelRatePer100hp": [0.032, …]</c>). Internal helper of <see cref="SeeConfigCodec"/>.
    /// </summary>
    internal static class ConfigFieldReader
    {
        /// <summary>Tier table keys in enum order (see <c>BoilerTier</c>).</summary>
        internal static readonly string[] TierKeys = { "T1", "T2", "T3", "T4" };

        /// <summary>Size table keys in enum order (see <c>BoilerSize</c>).</summary>
        internal static readonly string[] SizeKeys = { "Small", "Medium", "Large" };

        /// <summary>Case-insensitive child object lookup; null when absent or not an object.</summary>
        internal static IDictionary<string, object?>? Child(IDictionary<string, object?>? parent, string key)
        {
            return MiniJson.TryGet(parent, key, out object? value)
                && value is IDictionary<string, object?> map
                    ? map
                    : null;
        }

        /// <summary>Case-insensitive child array lookup; null when absent or not an array.</summary>
        internal static IList<object?>? ListChild(IDictionary<string, object?>? parent, string key)
        {
            return MiniJson.TryGet(parent, key, out object? value) && value is IList<object?> list ? list : null;
        }

        /// <summary>Reads a finite number clamped into min..max, otherwise the fallback.</summary>
        internal static float ReadFloat(
            IDictionary<string, object?>? map,
            string key,
            float fallback,
            float min,
            float max,
            string path,
            ISeeLog? log)
        {
            if (!MiniJson.TryGet(map, key, out object? raw))
            {
                return fallback;
            }

            return ConvertFloat(raw, fallback, min, max, path + "." + key, log);
        }

        /// <summary>Reads a nested float following a chain of child objects.</summary>
        internal static float ReadNestedFloat(
            IDictionary<string, object?>? parent,
            string[] keys,
            float fallback,
            float min,
            float max,
            string path,
            ISeeLog? log)
        {
            IDictionary<string, object?>? current = parent;
            for (int i = 0; i < keys.Length; i++)
            {
                if (current == null)
                {
                    return fallback;
                }

                string label = path + "." + keys[i];
                if (i == keys.Length - 1)
                {
                    return ReadFloat(current, keys[i], fallback, min, max, path, log);
                }

                current = Child(current, keys[i]);
                if (current == null)
                {
                    return fallback;
                }

                _ = label;
            }

            return fallback;
        }

        /// <summary>Reads a whole number clamped into min..max, otherwise the fallback.</summary>
        internal static int ReadInt(
            IDictionary<string, object?>? map,
            string key,
            int fallback,
            int min,
            int max,
            string path,
            ISeeLog? log)
        {
            if (!MiniJson.TryGet(map, key, out object? raw))
            {
                return fallback;
            }

            return ConvertInt(raw, fallback, min, max, path + "." + key, log);
        }

        /// <summary>Reads a nested whole number following a chain of child objects.</summary>
        internal static int ReadNestedInt(
            IDictionary<string, object?>? parent,
            string[] keys,
            int fallback,
            int min,
            int max,
            string path,
            ISeeLog? log)
        {
            IDictionary<string, object?>? current = parent;
            for (int i = 0; i < keys.Length; i++)
            {
                if (current == null)
                {
                    return fallback;
                }

                if (i == keys.Length - 1)
                {
                    return ReadInt(current, keys[i], fallback, min, max, path, log);
                }

                current = Child(current, keys[i]);
            }

            return fallback;
        }

        /// <summary>Reads a boolean, otherwise the fallback.</summary>
        internal static bool ReadBool(
            IDictionary<string, object?>? map,
            string key,
            bool fallback,
            string path,
            ISeeLog? log)
        {
            if (!MiniJson.TryGet(map, key, out object? raw))
            {
                return fallback;
            }

            if (raw is bool flag)
            {
                return flag;
            }

            Warn(log, path + "." + key, "expected a boolean");
            return fallback;
        }

        /// <summary>Reads an enum from its name (case-insensitive), otherwise the fallback.</summary>
        internal static T ReadEnum<T>(
            IDictionary<string, object?>? map,
            string key,
            T fallback,
            string path,
            ISeeLog? log)
            where T : struct
        {
            if (!MiniJson.TryGet(map, key, out object? raw))
            {
                return fallback;
            }

            if (raw is string text
                && Enum.TryParse(text, true, out T parsed)
                && Enum.IsDefined(typeof(T), parsed))
            {
                return parsed;
            }

            Warn(log, path + "." + key, "expected one of the documented enum names");
            return fallback;
        }

        /// <summary>
        /// Reads a fixed-length float table. Accepts a JSON array (index order) or, when
        /// <paramref name="objectKeys"/> is supplied, a JSON object keyed like {"T1": …}.
        /// </summary>
        internal static IReadOnlyList<float> ReadFloatArray(
            IDictionary<string, object?>? map,
            string key,
            IReadOnlyList<float> fallback,
            float min,
            float max,
            string path,
            ISeeLog? log,
            string[]? objectKeys = null)
        {
            var values = new float[fallback.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = fallback[i];
            }

            if (!MiniJson.TryGet(map, key, out object? raw))
            {
                return new ReadOnlyCollection<float>(values);
            }

            if (raw is IList<object?> list)
            {
                for (int i = 0; i < list.Count && i < values.Length; i++)
                {
                    values[i] = ConvertFloat(list[i], values[i], min, max, path + "." + key + "[" + i + "]", log);
                }

                return new ReadOnlyCollection<float>(values);
            }

            if (raw is IDictionary<string, object?> table && objectKeys != null)
            {
                for (int i = 0; i < objectKeys.Length && i < values.Length; i++)
                {
                    if (MiniJson.TryGet(table, objectKeys[i], out object? entry))
                    {
                        values[i] = ConvertFloat(entry, values[i], min, max, path + "." + key + "." + objectKeys[i], log);
                    }
                }

                return new ReadOnlyCollection<float>(values);
            }

            Warn(log, path + "." + key, "expected an array or a keyed object");
            return new ReadOnlyCollection<float>(values);
        }

        /// <summary>
        /// Reads a fixed-length int table. Accepts a JSON array (index order) or, when
        /// <paramref name="objectKeys"/> is supplied, a JSON object keyed like {"Small": …}.
        /// </summary>
        internal static IReadOnlyList<int> ReadIntArray(
            IDictionary<string, object?>? map,
            string key,
            IReadOnlyList<int> fallback,
            int min,
            int max,
            string path,
            ISeeLog? log,
            string[]? objectKeys = null)
        {
            var values = new int[fallback.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = fallback[i];
            }

            if (!MiniJson.TryGet(map, key, out object? raw))
            {
                return new ReadOnlyCollection<int>(values);
            }

            if (raw is IList<object?> list)
            {
                for (int i = 0; i < list.Count && i < values.Length; i++)
                {
                    values[i] = ConvertInt(list[i], values[i], min, max, path + "." + key + "[" + i + "]", log);
                }

                return new ReadOnlyCollection<int>(values);
            }

            if (raw is IDictionary<string, object?> table && objectKeys != null)
            {
                for (int i = 0; i < objectKeys.Length && i < values.Length; i++)
                {
                    if (MiniJson.TryGet(table, objectKeys[i], out object? entry))
                    {
                        values[i] = ConvertInt(entry, values[i], min, max, path + "." + key + "." + objectKeys[i], log);
                    }
                }

                return new ReadOnlyCollection<int>(values);
            }

            Warn(log, path + "." + key, "expected an array or a keyed object");
            return new ReadOnlyCollection<int>(values);
        }

        /// <summary>Reports a rejected/clamped config value; a broken log sink never breaks loading.</summary>
        internal static void Warn(ISeeLog? log, string path, string reason)
        {
            try
            {
                log?.Warn(SeeLogKeys.ConfigInvalid, path + ": " + reason);
            }
            catch (Exception)
            {
                // Logging is diagnostics only; config loading must continue.
            }
        }

        private static float ConvertFloat(object? raw, float fallback, float min, float max, string path, ISeeLog? log)
        {
            if (raw is not double number || double.IsNaN(number) || double.IsInfinity(number))
            {
                Warn(log, path, "expected a finite number");
                return fallback;
            }

            double clamped = Clamp(number, min, max);
            if (clamped != number)
            {
                Warn(log, path, "clamped " + number + " to " + clamped);
            }

            return (float)clamped;
        }

        private static int ConvertInt(object? raw, int fallback, int min, int max, string path, ISeeLog? log)
        {
            if (raw is not double number || double.IsNaN(number) || double.IsInfinity(number))
            {
                Warn(log, path, "expected a whole number");
                return fallback;
            }

            double clamped = Math.Round(Clamp(number, min, max), MidpointRounding.AwayFromZero);
            if (clamped != number)
            {
                Warn(log, path, "clamped " + number + " to " + clamped);
            }

            return (int)clamped;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}