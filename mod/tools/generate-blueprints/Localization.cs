using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SEE.Tools.GenerateBlueprints;

/// <summary>One localization row: the key plus the three shipped languages.</summary>
internal sealed record LocRow(string Key, string English, string Traditional, string Simplified);

/// <summary>
/// Builds the two shipped localization CSVs (requirements 15.5, contract 5).
///
/// The twelve display names are generated from the engine table; the description, flavor, status
/// and panel strings come verbatim from <c>data/loc_keys.json</c> (the authored static segment file).
/// The generator re-validates the frozen 63-key set (54-key requirements baseline plus the 9 P4B
/// additions), the placeholder sets and the Traditional-only character rule before writing a single byte.
/// </summary>
internal static class Localization
{
    public const int ExpectedKeyCount = 63;

    private static readonly string[] Sizes = { "Small", "Medium", "Large" };
    private static readonly string[] Tiers = { "T1", "T2", "T3", "T4" };
    private static readonly string[] Roman = { "I", "II", "III", "IV" };
    private static readonly string[] TraditionalSize = { "小型", "中型", "大型" };
    // Simplified display names: the game may run in zhCN (Player.log 2026-11-08 showed 63 missing keys).
    private static readonly string[] SimplifiedSize = { "小型", "中型", "大型" };
    private static readonly string[] ForbiddenKeyFragments = { "SteamEngine", "Building.SteamEngine.", "SteamPlant" };
    private static readonly string[] AllowedKeyPrefixes = { "Building.SEE.", "Status.SEE.", "UI.SEE." };

    /// <summary>Simplified-only characters from the SEE vocabulary; copied from the validator contract.</summary>
    private const string SimplifiedOnly =
        "这个们时间开关发动设备产业务维护状态压阀门铁齿炉烧热输带网电线检测无数据传类错"
        + "误运转暂继续补给与为会说话问题实现计画优变应该显隐权限链储读写删选择档库车号轮轴"
        + "灯费启闭资";

    /// <summary>Builds and validates the full 63-row localization table.</summary>
    public static IReadOnlyList<LocRow> Build(Catalog catalog, string staticTextsPath)
    {
        if (!File.Exists(staticTextsPath))
        {
            throw new GeneratorException("localization source not found: " + staticTextsPath);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(staticTextsPath));
        }
        catch (JsonException exception)
        {
            throw new GeneratorException("loc_keys.json is not valid JSON: " + exception.Message);
        }

        using (document)
        {
            var root = document.RootElement;
            var segments = RequireObject(root, "segments", "loc_keys.json");
            var rows = new List<LocRow>(ExpectedKeyCount);
            AddDisplayNames(rows);
            AddSingleton(rows, segments, "description", "Building.SEE.SteamBoiler.Description");
            AddFlavors(rows, segments);
            AddList(rows, segments, "status", 22);
            AddList(rows, segments, "panel", 24);

            if (rows.Count != ExpectedKeyCount)
            {
                throw new GeneratorException("localization: expected " + ExpectedKeyCount + " keys, built " + rows.Count);
            }

            Validate(rows);
            _ = catalog;
            return rows;
        }
    }

    /// <summary>Renders one CSV file (UTF-8 without BOM, exactly three columns, LF line endings).</summary>
    public static string ToCsv(IReadOnlyList<LocRow> rows, string language)
    {
        var builder = new StringBuilder(4096);
        builder.Append("ID,Text,Comment\n");
        foreach (var row in rows)
        {
            string text = language switch
            {
                "zhTW" => row.Traditional,
                "zhCN" => row.Simplified,
                _ => row.English,
            };
            AppendField(builder, row.Key);
            builder.Append(',');
            AppendField(builder, text);
            builder.Append(',');
            AppendField(builder, "req 15.5");
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static void AddDisplayNames(List<LocRow> rows)
    {
        for (var tierIndex = 0; tierIndex < Tiers.Length; tierIndex++)
        {
            for (var sizeIndex = 0; sizeIndex < Sizes.Length; sizeIndex++)
            {
                var key = "Building.SEE.SteamBoiler." + Sizes[sizeIndex] + "." + Tiers[tierIndex] + ".DisplayName";
                var english = Sizes[sizeIndex] + " Steam Engine Mk." + Roman[tierIndex];
                var traditional = TraditionalSize[sizeIndex] + "蒸汽引擎 Mk." + Roman[tierIndex];
                var simplified = SimplifiedSize[sizeIndex] + "蒸汽引擎 Mk." + Roman[tierIndex];
                rows.Add(new LocRow(key, english, traditional, simplified));
            }
        }
    }

    private static void AddSingleton(List<LocRow> rows, JsonElement segments, string name, string key)
    {
        var segment = RequireObject(segments, name, "loc_keys.json.segments");
        rows.Add(new LocRow(key, RequireLocalized(segment, name, "enUS"), RequireLocalized(segment, name, "zhTW"),
            RequireLocalized(segment, name, "zhCN")));
    }

    private static void AddFlavors(List<LocRow> rows, JsonElement segments)
    {
        var flavors = RequireObject(segments, "flavor", "loc_keys.json.segments");
        foreach (var tier in Tiers)
        {
            var flavor = RequireObject(flavors, tier, "loc_keys.json.segments.flavor");
            rows.Add(new LocRow(
                "Building.SEE.SteamBoiler.Flavor." + tier,
                RequireLocalized(flavor, "flavor." + tier, "enUS"),
                RequireLocalized(flavor, "flavor." + tier, "zhTW"),
                RequireLocalized(flavor, "flavor." + tier, "zhCN")));
        }
    }

    private static void AddList(List<LocRow> rows, JsonElement segments, string name, int expectedCount)
    {
        if (!segments.TryGetProperty(name, out var list) || list.ValueKind != JsonValueKind.Array)
        {
            throw new GeneratorException("loc_keys.json.segments: '" + name + "' must be an array");
        }

        var items = list.EnumerateArray().ToList();
        if (items.Count != expectedCount)
        {
            throw new GeneratorException("loc_keys.json.segments." + name + ": expected " + expectedCount
                                         + " entries, found " + items.Count);
        }

        foreach (var item in items)
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new GeneratorException("loc_keys.json.segments." + name + ": entries must be objects");
            }

            var key = RequireString(item, "key", "loc_keys.json.segments." + name);
            rows.Add(new LocRow(key, RequireString(item, "enUS", key), RequireString(item, "zhTW", key),
                RequireString(item, "zhCN", key)));
        }
    }

    private static void Validate(IReadOnlyList<LocRow> rows)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var simplified = new HashSet<char>(SimplifiedOnly.ToCharArray());
        foreach (var row in rows)
        {
            if (!seen.Add(row.Key))
            {
                throw new GeneratorException("localization: duplicate key '" + row.Key + "'");
            }

            if (!AllowedKeyPrefixes.Any(prefix => row.Key.StartsWith(prefix, StringComparison.Ordinal)))
            {
                throw new GeneratorException("localization: '" + row.Key + "' uses a forbidden key prefix");
            }

            foreach (var fragment in ForbiddenKeyFragments)
            {
                if (row.Key.Contains(fragment, StringComparison.Ordinal))
                {
                    throw new GeneratorException("localization: '" + row.Key + "' contains forbidden fragment '"
                                                 + fragment + "'");
                }
            }

            if (row.English.Length == 0 || row.Traditional.Length == 0 || row.Simplified.Length == 0)
            {
                throw new GeneratorException("localization: '" + row.Key + "' has an empty text");
            }

            if (row.English == row.Key || row.Traditional == row.Key || row.Simplified == row.Key)
            {
                throw new GeneratorException("localization: '" + row.Key + "' still renders the raw key");
            }

            var englishPlaceholders = Placeholders(row.English);
            var traditionalPlaceholders = Placeholders(row.Traditional);
            var simplifiedPlaceholders = Placeholders(row.Simplified);
            if (!englishPlaceholders.SequenceEqual(traditionalPlaceholders)
                || !englishPlaceholders.SequenceEqual(simplifiedPlaceholders))
            {
                throw new GeneratorException("localization: '" + row.Key
                                             + "' placeholder mismatch between enUS and a Chinese language");
            }

            foreach (var ch in row.Traditional)
            {
                if (simplified.Contains(ch))
                {
                    throw new GeneratorException("localization: zhTW '" + row.Key + "' contains simplified character '"
                                                 + ch + "'");
                }
            }
        }

        if (seen.Count != ExpectedKeyCount)
        {
            throw new GeneratorException("localization: expected " + ExpectedKeyCount + " keys, found " + seen.Count);
        }
    }

    private static List<int> Placeholders(string text)
    {
        var result = new List<int>();
        var index = 0;
        while (index < text.Length)
        {
            var start = text.IndexOf('{', index);
            if (start < 0)
            {
                break;
            }

            var end = text.IndexOf('}', start + 1);
            if (end < 0)
            {
                break;
            }

            if (int.TryParse(text.AsSpan(start + 1, end - start - 1), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var number))
            {
                result.Add(number);
            }

            index = end + 1;
        }

        result.Sort();
        return result;
    }

    private static void AppendField(StringBuilder builder, string value)
    {
        if (value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal)
            || value.Contains('\n', StringComparison.Ordinal) || value.Contains('\r', StringComparison.Ordinal))
        {
            builder.Append('"').Append(value.Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"');
            return;
        }

        builder.Append(value);
    }

    private static string RequireLocalized(JsonElement element, string context, string language) =>
        RequireString(element, language, "loc_keys.json " + context);

    private static JsonElement RequireObject(JsonElement parent, string name, string context)
    {
        if (parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object)
        {
            return value;
        }

        throw new GeneratorException(context + ": '" + name + "' must be an object");
    }

    private static string RequireString(JsonElement parent, string name, string context)
    {
        if (parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString()))
        {
            return value.GetString()!;
        }

        throw new GeneratorException(context + ": '" + name + "' must be a non-empty string");
    }
}
