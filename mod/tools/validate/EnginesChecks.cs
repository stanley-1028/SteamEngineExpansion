using System.Globalization;
using System.Text.Json;

namespace SEE.Tools.Validate;

/// <summary>
/// Validates <c>data/engines.json</c>, the single authoritative number source for SEE
/// (REQ-ARCH-04, requirements §11.3):
///
/// <list type="bullet">
/// <item>valid JSON, root object with an integer <c>dataVersion</c> and an <c>engines</c> array;</item>
/// <item>exactly 12 entries, one per (size, tier) of Small/Medium/Large x T1–T4, no duplicates;</item>
/// <item>every documented field present with the documented type;</item>
/// <item>derived values recomputed and compared (no hand-copied drift):
/// fuelPerHour = round(hp/100 x tierFuelRate, 2), waterPerHour = round(hp/100 x tierWaterRate, 2),
/// waterTankGoods = round(waterPerHour x 200), fuelStock = ceil(fuelPerHour x 25);</item>
/// <item>maintenance interval and materials match the §4.1 tier table.</item>
/// </list>
///
/// The derived math uses <see cref="decimal"/> on purpose: binary floating point rounds
/// 460/100 x 0.025 to 0.114999… and would wrongly reject the authoritative 0.12.
/// </summary>
internal sealed class EnginesChecks
{
    public const int ExpectedEngineCount = 12;

    private static readonly string[] Sizes = { "Small", "Medium", "Large" };
    private static readonly string[] Tiers = { "T1", "T2", "T3", "T4" };

    private static readonly Dictionary<string, decimal> TierFuelRate = new(StringComparer.Ordinal)
    {
        ["T1"] = 0.032m,
        ["T2"] = 0.025m,
        ["T3"] = 0.021m,
        ["T4"] = 0.016m,
    };

    private static readonly Dictionary<string, decimal> TierWaterRate = new(StringComparer.Ordinal)
    {
        ["T1"] = 0.080m,
        ["T2"] = 0.070m,
        ["T3"] = 0.040m,
        ["T4"] = 0.018m,
    };

    private static readonly Dictionary<string, int> TierIntervalDays = new(StringComparer.Ordinal)
    {
        ["T1"] = 6,
        ["T2"] = 8,
        ["T3"] = 10,
        ["T4"] = 12,
    };

    private static readonly Dictionary<string, (string Good, int Amount)[]> TierMaterials = new(StringComparer.Ordinal)
    {
        ["T1"] = new[] { ("Log", 8) },
        ["T2"] = new[] { ("Plank", 12), ("Gear", 2) },
        ["T3"] = new[] { ("MetalBlock", 10), ("Plank", 10) },
        ["T4"] = new[] { ("MetalBlock", 12), ("Gear", 6) },
    };

    private static readonly HashSet<string> KnownGoods =
        new(StringComparer.Ordinal) { "Log", "Plank", "Gear", "MetalBlock" };

    private const int FullInventoryWorkHours = 25;

    private readonly string _dataDir;
    private readonly Report _report;

    public EnginesChecks(string dataDir, Report report)
    {
        _dataDir = dataDir;
        _report = report;
    }

    /// <summary>Number of engine rows found (0 when the file is missing/broken).</summary>
    public int EngineCount { get; private set; }

    /// <summary>Declared dataVersion (0 when unknown).</summary>
    public int DataVersion { get; private set; }

    public void Validate()
    {
        var path = Path.Combine(_dataDir, "engines.json");
        if (!File.Exists(path))
        {
            _report.Error("engines", "data/engines.json is missing: it is the single authoritative number source "
                                     + "for the 12 engines (REQ-ARCH-04)");
            return;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path));
        }
        catch (JsonException ex)
        {
            _report.Error("engines", "engines.json is not valid JSON: " + ex.Message);
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                _report.Error("engines", "engines.json root must be an object ({\"dataVersion\":1,\"engines\":[...]})");
                return;
            }

            ValidateDataVersion(root);

            if (!root.TryGetProperty("engines", out var engines) || engines.ValueKind != JsonValueKind.Array)
            {
                _report.Error("engines", "engines.json must contain an 'engines' array");
                return;
            }

            var entries = engines.EnumerateArray().ToList();
            EngineCount = entries.Count;
            if (entries.Count != ExpectedEngineCount)
            {
                _report.Error("engines", "engines.json must list exactly " + ExpectedEngineCount
                                         + " engines (3 sizes x 4 tiers), found " + entries.Count);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;
            foreach (var entry in entries)
            {
                var label = "engines[" + index + "]";
                ValidateEngine(entry, label, seen);
                index++;
            }

            foreach (var size in Sizes)
            {
                foreach (var tier in Tiers)
                {
                    var key = size + "." + tier;
                    if (!seen.Contains(key))
                    {
                        _report.Error("engines", "engines.json has no row for " + key);
                    }
                }
            }
        }
    }

    private void ValidateDataVersion(JsonElement root)
    {
        if (!root.TryGetProperty("dataVersion", out var version))
        {
            _report.Error("engines", "engines.json is missing 'dataVersion'");
            return;
        }

        if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number))
        {
            _report.Error("engines", "engines.json 'dataVersion' must be an integer");
            return;
        }

        DataVersion = number;
        if (number < 1)
        {
            _report.Error("engines", "engines.json 'dataVersion' must be >= 1, found " + number);
        }
    }

    private void ValidateEngine(JsonElement entry, string label, HashSet<string> seen)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            _report.Error("engines", label + " must be an object");
            return;
        }

        if (!TryString(entry, "size", label, out var size) || !TryString(entry, "tier", label, out var tier))
        {
            return;
        }

        if (!Sizes.Contains(size, StringComparer.Ordinal))
        {
            _report.Error("engines", label + " has unsupported size '" + size + "'");
        }

        if (!Tiers.Contains(tier, StringComparer.Ordinal))
        {
            _report.Error("engines", label + " has unsupported tier '" + tier + "'");
        }

        label = label + " (" + size + "." + tier + ")";

        if (!seen.Add(size + "." + tier))
        {
            _report.Error("engines", label + " duplicates another (size,tier) row");
        }

        // --- structure -------------------------------------------------------------------
        ValidateFootprint(entry, label);
        TryPositiveInt(entry, "height", label);
        TryPositiveInt(entry, "ratedHorsepower", label);
        TryPositiveInt(entry, "scienceCost", label);
        ValidateGoodsArray(entry, "buildCost", label, allowEmpty: false);
        TryPositiveInt(entry, "fuelStock", label);
        TryPositiveInt(entry, "waterTankGoods", label);
        TryPositiveInt(entry, "maintenanceIntervalDays", label);
        ValidateGoodsArray(entry, "maintenanceMaterials", label, allowEmpty: false);

        if (!TryPositiveInt(entry, "fullInventoryWorkHours", label, out var workHours))
        {
            return;
        }

        if (workHours != FullInventoryWorkHours)
        {
            _report.Error("engines", label + ": fullInventoryWorkHours must be " + FullInventoryWorkHours
                                     + " (all 12 files), found " + workHours);
        }

        if (!TryDecimal(entry, "fuelPerHour", label, out var fuelPerHour)
            || !TryDecimal(entry, "waterPerHour", label, out var waterPerHour)
            || !TryPositiveInt(entry, "ratedHorsepower", label, out var horsepower))
        {
            return;
        }

        if (fuelPerHour <= 0m)
        {
            _report.Error("engines", label + ": fuelPerHour must be positive");
        }

        if (waterPerHour <= 0m)
        {
            _report.Error("engines", label + ": waterPerHour must be positive");
        }

        if (!Tiers.Contains(tier, StringComparer.Ordinal))
        {
            return;
        }

        // --- derived verification (REQ-IMPL-02 / requirements §11.3) ----------------------
        var expectedFuel = Math.Round(horsepower / 100m * TierFuelRate[tier], 2, MidpointRounding.AwayFromZero);
        if (fuelPerHour != expectedFuel)
        {
            _report.Error("engines", label + ": fuelPerHour = " + Show(fuelPerHour) + " but round(ratedHorsepower/100 x "
                                     + Show(TierFuelRate[tier]) + ", 2) = " + Show(expectedFuel));
        }

        var expectedWater = Math.Round(horsepower / 100m * TierWaterRate[tier], 2, MidpointRounding.AwayFromZero);
        if (waterPerHour != expectedWater)
        {
            _report.Error("engines", label + ": waterPerHour = " + Show(waterPerHour) + " but round(ratedHorsepower/100 x "
                                     + Show(TierWaterRate[tier]) + ", 2) = " + Show(expectedWater));
        }

        if (!TryPositiveInt(entry, "waterTankGoods", label, out var waterTankGoods)
            || !TryPositiveInt(entry, "fuelStock", label, out var fuelStock)
            || !TryPositiveInt(entry, "maintenanceIntervalDays", label, out var interval))
        {
            return;
        }

        var expectedTank = (int)Math.Round(waterPerHour * 200m, 0, MidpointRounding.AwayFromZero);
        if (waterTankGoods != expectedTank)
        {
            _report.Error("engines", label + ": waterTankGoods = " + waterTankGoods + " but round(waterPerHour x 200) = "
                                     + expectedTank);
        }

        var expectedStock = (int)Math.Ceiling(fuelPerHour * FullInventoryWorkHours);
        if (fuelStock != expectedStock)
        {
            _report.Error("engines", label + ": fuelStock = " + fuelStock + " but ceil(fuelPerHour x "
                                     + FullInventoryWorkHours + ") = " + expectedStock);
        }

        if (interval != TierIntervalDays[tier])
        {
            _report.Error("engines", label + ": maintenanceIntervalDays = " + interval + " but " + tier + " requires "
                                     + TierIntervalDays[tier] + " (requirements §4.1)");
        }

        ValidateMaintenanceMaterials(entry, label, tier);
    }

    private void ValidateFootprint(JsonElement entry, string label)
    {
        if (!entry.TryGetProperty("footprint", out var footprint) || footprint.ValueKind != JsonValueKind.Object)
        {
            _report.Error("engines", label + " is missing the 'footprint' object {x,y}");
            return;
        }

        foreach (var axis in new[] { "x", "y" })
        {
            if (!footprint.TryGetProperty(axis, out var value)
                || value.ValueKind != JsonValueKind.Number
                || !value.TryGetInt32(out var number))
            {
                _report.Error("engines", label + " footprint." + axis + " must be an integer");
                continue;
            }

            if (number <= 0)
            {
                _report.Error("engines", label + " footprint." + axis + " must be positive");
            }
        }
    }

    private void ValidateGoodsArray(JsonElement entry, string propertyName, string label, bool allowEmpty)
    {
        if (!entry.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            _report.Error("engines", label + " is missing the '" + propertyName + "' array");
            return;
        }

        var items = array.EnumerateArray().ToList();
        if (!allowEmpty && items.Count == 0)
        {
            _report.Error("engines", label + ": '" + propertyName + "' must not be empty");
        }

        var index = 0;
        foreach (var item in items)
        {
            var location = label + "." + propertyName + "[" + index + "]";
            if (item.ValueKind != JsonValueKind.Object)
            {
                _report.Error("engines", location + " must be an object {good,amount}");
                index++;
                continue;
            }

            if (!TryString(item, "good", location, out var good))
            {
                index++;
                continue;
            }

            if (!KnownGoods.Contains(good))
            {
                _report.Error("engines", location + " uses unknown good '" + good
                                         + "' (existing goods only: " + string.Join(", ", KnownGoods) + ")");
            }

            if (!item.TryGetProperty("amount", out var amount)
                || amount.ValueKind != JsonValueKind.Number
                || !amount.TryGetInt32(out var number)
                || number <= 0)
            {
                _report.Error("engines", location + " must have a positive integer 'amount'");
            }

            index++;
        }
    }

    private void ValidateMaintenanceMaterials(JsonElement entry, string label, string tier)
    {
        if (!entry.TryGetProperty("maintenanceMaterials", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var actual = new List<(string Good, int Amount)>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (item.TryGetProperty("good", out var good) && good.ValueKind == JsonValueKind.String
                && item.TryGetProperty("amount", out var amount) && amount.ValueKind == JsonValueKind.Number
                && amount.TryGetInt32(out var number))
            {
                actual.Add((good.GetString() ?? string.Empty, number));
            }
        }

        var expected = TierMaterials[tier];
        var matches = actual.Count == expected.Length;
        if (matches)
        {
            var remaining = actual.ToList();
            foreach (var expectedItem in expected)
            {
                var position = remaining.FindIndex(item =>
                    string.Equals(item.Good, expectedItem.Good, StringComparison.Ordinal) && item.Amount == expectedItem.Amount);
                if (position < 0)
                {
                    matches = false;
                    break;
                }

                remaining.RemoveAt(position);
            }
        }

        if (!matches)
        {
            _report.Error("engines", label + ": maintenanceMaterials must be the " + tier + " table from requirements "
                                     + "§4.1 (" + FormatMaterials(expected) + "), found ["
                                     + string.Join(", ", actual.Select(item => item.Good + " " + item.Amount)) + "]");
        }
    }

    private static string FormatMaterials((string Good, int Amount)[] materials) =>
        string.Join(" + ", materials.Select(item => item.Good + " " + item.Amount));

    private bool TryString(JsonElement parent, string name, string label, out string value)
    {
        if (parent.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString() ?? string.Empty;
            if (value.Length > 0)
            {
                return true;
            }
        }

        _report.Error("engines", label + " is missing the string field '" + name + "'");
        value = string.Empty;
        return false;
    }

    private bool TryDecimal(JsonElement parent, string name, string label, out decimal value)
    {
        if (parent.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number
            && element.TryGetDecimal(out value))
        {
            return true;
        }

        _report.Error("engines", label + " is missing the numeric field '" + name + "'");
        value = 0m;
        return false;
    }

    private bool TryPositiveInt(JsonElement parent, string name, string label) =>
        TryPositiveInt(parent, name, label, out _);

    private bool TryPositiveInt(JsonElement parent, string name, string label, out int value)
    {
        if (parent.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out value))
        {
            if (value > 0)
            {
                return true;
            }

            _report.Error("engines", label + ": '" + name + "' must be positive, found " + value);
            return false;
        }

        _report.Error("engines", label + " is missing the integer field '" + name + "'");
        value = 0;
        return false;
    }

    private static string Show(decimal value) => value.ToString("0.00##", CultureInfo.InvariantCulture);
}
