using System.Globalization;
using System.Text.Json;

namespace SEE.Tools.GenerateBlueprints;

/// <summary>One good/amount pair, for build cost or maintenance materials.</summary>
internal sealed record GoodAmount(string Good, int Amount);

/// <summary>One of the two playable factions and its two balance multipliers (requirements 4.4).</summary>
internal sealed record FactionDef(string Name, decimal OutputMultiplier, decimal FuelMultiplier);

/// <summary>One of the twelve authoritative engine rows (requirements 4.1 / 11.3).</summary>
internal sealed record EngineRow(
    string Size,
    string Tier,
    int FootprintX,
    int FootprintY,
    int Height,
    int RatedHorsepower,
    int ScienceCost,
    IReadOnlyList<GoodAmount> BuildCost,
    decimal FuelPerHour,
    decimal WaterPerHour,
    int FuelStock,
    int WaterTankGoods,
    int FullInventoryWorkHours,
    int MaintenanceIntervalDays,
    IReadOnlyList<GoodAmount> MaintenanceMaterials);

/// <summary>The validated contents of data/engines.json, the single source of every blueprint value.</summary>
internal sealed class Catalog
{
    public Catalog(
        int dataVersion,
        int fullInventoryWorkHours,
        IReadOnlyDictionary<string, decimal> tierFuelRate,
        IReadOnlyDictionary<string, decimal> tierWaterRate,
        int waterTankHours,
        IReadOnlyList<FactionDef> factions,
        IReadOnlyDictionary<string, int> toolOrders,
        IReadOnlyDictionary<string, string> finishedModels,
        IReadOnlyDictionary<string, string> constructionStageModels,
        IReadOnlyDictionary<string, string> constructionBases,
        IReadOnlyList<EngineRow> engines)
    {
        DataVersion = dataVersion;
        FullInventoryWorkHours = fullInventoryWorkHours;
        TierFuelRate = tierFuelRate;
        TierWaterRate = tierWaterRate;
        WaterTankHours = waterTankHours;
        Factions = factions;
        ToolOrders = toolOrders;
        FinishedModels = finishedModels;
        ConstructionStageModels = constructionStageModels;
        ConstructionBases = constructionBases;
        Engines = engines;
    }

    public int DataVersion { get; }

    public int FullInventoryWorkHours { get; }

    public IReadOnlyDictionary<string, decimal> TierFuelRate { get; }

    public IReadOnlyDictionary<string, decimal> TierWaterRate { get; }

    public int WaterTankHours { get; }

    /// <summary>Folktails first, IronTeeth second (deterministic blueprint/collection ordering).</summary>
    public IReadOnlyList<FactionDef> Factions { get; }

    /// <summary>Per (size, tier) build-menu order, keyed <c>"Small.T1"</c>.</summary>
    public IReadOnlyDictionary<string, int> ToolOrders { get; }

    /// <summary>Faction name to the placeholder finished model path (P9 replaces these).</summary>
    public IReadOnlyDictionary<string, string> FinishedModels { get; }

    /// <summary>Faction name to the placeholder construction-stage model path.</summary>
    public IReadOnlyDictionary<string, string> ConstructionStageModels { get; }

    /// <summary>Size name to the closest available vanilla construction base path.</summary>
    public IReadOnlyDictionary<string, string> ConstructionBases { get; }

    public IReadOnlyList<EngineRow> Engines { get; }
}

/// <summary>
/// Step 1 and 2 of the generator: load data/engines.json, validate its structure (fields, types,
/// enumerations, 12 unique (size,tier) rows) and re-derive every rate/capacity instead of trusting
/// the file. Any inconsistency aborts the run before a single file is written.
/// </summary>
internal static class CatalogLoader
{
    public static readonly string[] Sizes = { "Small", "Medium", "Large" };
    public static readonly string[] Tiers = { "T1", "T2", "T3", "T4" };

    public static readonly IReadOnlyDictionary<string, decimal> DefaultTierFuelRate =
        new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["T1"] = 0.032m,
            ["T2"] = 0.025m,
            ["T3"] = 0.021m,
            ["T4"] = 0.016m,
        };

    public static readonly IReadOnlyDictionary<string, decimal> DefaultTierWaterRate =
        new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["T1"] = 0.080m,
            ["T2"] = 0.070m,
            ["T3"] = 0.040m,
            ["T4"] = 0.018m,
        };

    public static readonly IReadOnlyDictionary<string, (int X, int Y, int Z)> Structure =
        new Dictionary<string, (int X, int Y, int Z)>(StringComparer.Ordinal)
        {
            ["Small"] = (2, 2, 3),
            ["Medium"] = (3, 3, 3),
            ["Large"] = (4, 4, 4),
        };

    public static readonly IReadOnlyDictionary<string, int> TierIntervalDays =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["T1"] = 6,
            ["T2"] = 8,
            ["T3"] = 10,
            ["T4"] = 12,
        };

    public static readonly IReadOnlyDictionary<string, GoodAmount[]> TierMaterials =
        new Dictionary<string, GoodAmount[]>(StringComparer.Ordinal)
        {
            ["T1"] = new[] { new GoodAmount("Log", 8) },
            ["T2"] = new[] { new GoodAmount("Plank", 12), new GoodAmount("Gear", 2) },
            ["T3"] = new[] { new GoodAmount("MetalBlock", 10), new GoodAmount("Plank", 10) },
            ["T4"] = new[] { new GoodAmount("MetalBlock", 12), new GoodAmount("Gear", 6) },
        };

    private static readonly HashSet<string> KnownGoods =
        new(StringComparer.Ordinal) { "Log", "Plank", "Gear", "MetalBlock" };

    private static readonly string[] ForbiddenKeyFragments = { "SteamEngine", "SteamPlant", "Building.SteamEngine." };

    /// <summary>Loads and fully validates data/engines.json; throws <see cref="GeneratorException"/> on any defect.</summary>
    public static Catalog Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new GeneratorException("engines.json not found: " + path);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path));
        }
        catch (JsonException exception)
        {
            throw new GeneratorException("engines.json is not valid JSON: " + exception.Message);
        }

        using (document)
        {
            var errors = new Errors();
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new GeneratorException("engines.json root must be an object");
            }

            var dataVersion = RequireInt(root, "dataVersion", "engines.json", errors);
            var fullInventoryWorkHours = RequireInt(root, "fullInventoryWorkHours", "engines.json", errors);
            if (fullInventoryWorkHours <= 0)
            {
                errors.Add("engines.json: fullInventoryWorkHours must be positive");
            }

            var tierFuelRate = RequireTierMap(root, "tierFuelRatePer100Hp", errors);
            var tierWaterRate = RequireTierMap(root, "tierWaterRatePer100Hp", errors);
            var waterTankHours = RequireInt(root, "waterTankHours", "engines.json", errors);
            if (waterTankHours <= 0)
            {
                errors.Add("engines.json: waterTankHours must be positive");
            }

            var factions = RequireFactions(root, errors);
            var toolOrders = RequireToolOrders(root, errors);
            var finishedModels = RequireStringMap(root, "artPlaceholders", "models", "finished", errors);
            var stageModels = RequireStringMap(root, "artPlaceholders", "models", "constructionStage", errors);
            var constructionBases = RequireStringMap(root, "artPlaceholders", "constructionBases", null, errors);

            var engines = RequireEngines(root, fullInventoryWorkHours, errors);
            ValidateEngines(engines, tierFuelRate, tierWaterRate, waterTankHours, fullInventoryWorkHours, errors);

            if (errors.Count > 0)
            {
                throw new GeneratorException("engines.json validation failed:" + Environment.NewLine
                    + string.Join(Environment.NewLine, errors.Messages.Select(message => "  - " + message)));
            }

            return new Catalog(
                dataVersion,
                fullInventoryWorkHours,
                tierFuelRate,
                tierWaterRate,
                waterTankHours,
                factions,
                toolOrders,
                finishedModels,
                stageModels,
                constructionBases,
                engines);
        }
    }

    private static IReadOnlyDictionary<string, decimal> RequireTierMap(JsonElement root, string name, Errors errors)
    {
        var map = new Dictionary<string, decimal>(StringComparer.Ordinal);
        if (!TryObject(root, name, "engines.json", errors, out var element))
        {
            return map;
        }

        foreach (var tier in Tiers)
        {
            if (TryDecimal(element, tier, "engines.json." + name, errors, out var value))
            {
                if (value <= 0m)
                {
                    errors.Add("engines.json." + name + "." + tier + " must be positive");
                }

                map[tier] = value;
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!Tiers.Contains(property.Name, StringComparer.Ordinal))
            {
                errors.Add("engines.json." + name + ": unknown tier '" + property.Name + "'");
            }
        }

        return map;
    }

    private static IReadOnlyList<FactionDef> RequireFactions(JsonElement root, Errors errors)
    {
        var factions = new List<FactionDef>();
        if (!TryObject(root, "factions", "engines.json", errors, out var element))
        {
            return factions;
        }

        foreach (var expected in new[] { "Folktails", "IronTeeth" })
        {
            if (!TryObject(element, expected, "engines.json.factions", errors, out var faction))
            {
                continue;
            }

            var output = RequireDecimal(faction, "output", "engines.json.factions." + expected, errors);
            var fuel = RequireDecimal(faction, "fuel", "engines.json.factions." + expected, errors);
            if (output < 0.95m || output > 1.10m)
            {
                errors.Add("engines.json.factions." + expected + ".output = " + Show(output)
                           + " is outside the config clamp range [0.95, 1.10]");
            }

            if (fuel < 0.80m || fuel > 1.10m)
            {
                errors.Add("engines.json.factions." + expected + ".fuel = " + Show(fuel)
                           + " is outside the config clamp range [0.80, 1.10]");
            }

            factions.Add(new FactionDef(expected, output, fuel));
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.Name is not ("Folktails" or "IronTeeth"))
            {
                errors.Add("engines.json.factions: unknown faction '" + property.Name + "'");
            }
        }

        return factions;
    }

    private static IReadOnlyDictionary<string, int> RequireToolOrders(JsonElement root, Errors errors)
    {
        var orders = new Dictionary<string, int>(StringComparer.Ordinal);
        if (!TryObject(root, "toolOrders", "engines.json", errors, out var element))
        {
            return orders;
        }

        foreach (var tier in Tiers)
        {
            if (!TryObject(element, tier, "engines.json.toolOrders", errors, out var row))
            {
                continue;
            }

            foreach (var size in Sizes)
            {
                if (TryInt(row, size, "engines.json.toolOrders." + tier, errors, out var order))
                {
                    orders[size + "." + tier] = order;
                }
            }

            foreach (var property in row.EnumerateObject())
            {
                if (!Sizes.Contains(property.Name, StringComparer.Ordinal))
                {
                    errors.Add("engines.json.toolOrders." + tier + ": unknown size '" + property.Name + "'");
                }
            }
        }

        var used = new HashSet<int>();
        foreach (var pair in orders)
        {
            if (pair.Value < 110 || pair.Value > 142)
            {
                errors.Add("engines.json.toolOrders: " + pair.Key + " = " + pair.Value
                           + " is outside the contract range 110-142");
            }

            if (!used.Add(pair.Value))
            {
                errors.Add("engines.json.toolOrders: duplicate ToolOrder " + pair.Value);
            }
        }

        return orders;
    }

    private static IReadOnlyDictionary<string, string> RequireStringMap(
        JsonElement root, string section, string group, string? leaf, Errors errors)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var context = "engines.json." + section;
        if (!TryObject(root, section, "engines.json", errors, out var sectionElement))
        {
            return map;
        }

        if (!TryObject(sectionElement, group, context, errors, out var groupElement))
        {
            return map;
        }

        if (leaf is null)
        {
            foreach (var size in Sizes)
            {
                if (TryString(groupElement, size, context + "." + group, errors, out var value))
                {
                    map[size] = value;
                }
            }

            return map;
        }

        foreach (var faction in new[] { "Folktails", "IronTeeth" })
        {
            if (!TryObject(groupElement, faction, context + "." + group, errors, out var factionElement))
            {
                continue;
            }

            if (TryString(factionElement, leaf, context + "." + group + "." + faction, errors, out var value))
            {
                map[faction] = value;
            }
        }

        return map;
    }

    private static IReadOnlyList<EngineRow> RequireEngines(JsonElement root, int fullInventoryWorkHours, Errors errors)
    {
        var engines = new List<EngineRow>();
        if (!TryArray(root, "engines", "engines.json", errors, out var array))
        {
            return engines;
        }

        var index = 0;
        foreach (var entry in array.EnumerateArray())
        {
            var label = "engines.json.engines[" + index + "]";
            index++;
            if (entry.ValueKind != JsonValueKind.Object)
            {
                errors.Add(label + " must be an object");
                continue;
            }

            var size = RequireString(entry, "size", label, errors);
            var tier = RequireString(entry, "tier", label, errors);
            var context = label + " (" + size + "." + tier + ")";

            if (!Sizes.Contains(size, StringComparer.Ordinal))
            {
                errors.Add(context + ": unsupported size '" + size + "'");
            }

            if (!Tiers.Contains(tier, StringComparer.Ordinal))
            {
                errors.Add(context + ": unsupported tier '" + tier + "'");
            }

            var footprintX = 0;
            var footprintY = 0;
            if (TryObject(entry, "footprint", context, errors, out var footprint))
            {
                footprintX = RequireInt(footprint, "x", context + ".footprint", errors);
                footprintY = RequireInt(footprint, "y", context + ".footprint", errors);
            }

            engines.Add(new EngineRow(
                size,
                tier,
                footprintX,
                footprintY,
                RequireInt(entry, "height", context, errors),
                RequireInt(entry, "ratedHorsepower", context, errors),
                RequireInt(entry, "scienceCost", context, errors),
                RequireGoods(entry, "buildCost", context, errors),
                RequireDecimal(entry, "fuelPerHour", context, errors),
                RequireDecimal(entry, "waterPerHour", context, errors),
                RequireInt(entry, "fuelStock", context, errors),
                RequireInt(entry, "waterTankGoods", context, errors),
                RequireInt(entry, "fullInventoryWorkHours", context, errors),
                RequireInt(entry, "maintenanceIntervalDays", context, errors),
                RequireGoods(entry, "maintenanceMaterials", context, errors)));
        }

        if (engines.Count != Sizes.Length * Tiers.Length)
        {
            errors.Add("engines.json: expected " + (Sizes.Length * Tiers.Length) + " engine rows, found " + engines.Count);
        }

        var duplicates = engines
            .GroupBy(engine => engine.Size + "." + engine.Tier, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        foreach (var duplicate in duplicates)
        {
            errors.Add("engines.json: duplicate (size,tier) row " + duplicate);
        }

        foreach (var size in Sizes)
        {
            foreach (var tier in Tiers)
            {
                if (!engines.Any(engine => string.Equals(engine.Size, size, StringComparison.Ordinal)
                                           && string.Equals(engine.Tier, tier, StringComparison.Ordinal)))
                {
                    errors.Add("engines.json: missing row for " + size + "." + tier);
                }
            }
        }

        _ = fullInventoryWorkHours;
        return engines;
    }

    private static IReadOnlyList<GoodAmount> RequireGoods(JsonElement parent, string name, string context, Errors errors)
    {
        var goods = new List<GoodAmount>();
        if (!TryArray(parent, name, context, errors, out var array))
        {
            return goods;
        }

        if (array.GetArrayLength() == 0)
        {
            errors.Add(context + "." + name + " must not be empty");
        }

        var index = 0;
        foreach (var item in array.EnumerateArray())
        {
            var label = context + "." + name + "[" + index + "]";
            index++;
            if (item.ValueKind != JsonValueKind.Object)
            {
                errors.Add(label + " must be an object {good,amount}");
                continue;
            }

            var good = RequireString(item, "good", label, errors);
            var amount = RequireInt(item, "amount", label, errors);
            if (!KnownGoods.Contains(good))
            {
                errors.Add(label + ": unknown good '" + good + "' (only existing goods are allowed)");
            }

            if (amount <= 0)
            {
                errors.Add(label + ": amount must be positive");
            }

            if (goods.Any(existing => string.Equals(existing.Good, good, StringComparison.Ordinal)))
            {
                errors.Add(label + ": duplicate good '" + good + "'");
            }

            goods.Add(new GoodAmount(good, amount));
        }

        return goods;
    }

    private static void ValidateEngines(
        IReadOnlyList<EngineRow> engines,
        IReadOnlyDictionary<string, decimal> tierFuelRate,
        IReadOnlyDictionary<string, decimal> tierWaterRate,
        int waterTankHours,
        int fullInventoryWorkHours,
        Errors errors)
    {
        foreach (var engine in engines)
        {
            var context = "engines.json " + engine.Size + "." + engine.Tier;
            if (!Structure.TryGetValue(engine.Size, out var structure))
            {
                continue;
            }

            if (engine.FootprintX != structure.X || engine.FootprintY != structure.Y)
            {
                errors.Add(context + ": footprint must be " + structure.X + "x" + structure.Y
                           + ", found " + engine.FootprintX + "x" + engine.FootprintY);
            }

            if (engine.Height != structure.Z)
            {
                errors.Add(context + ": height must be " + structure.Z + ", found " + engine.Height);
            }

            if (engine.FullInventoryWorkHours != fullInventoryWorkHours)
            {
                errors.Add(context + ": fullInventoryWorkHours must equal the top-level value "
                           + fullInventoryWorkHours + ", found " + engine.FullInventoryWorkHours);
            }

            if (engine.RatedHorsepower <= 0 || engine.ScienceCost <= 0)
            {
                errors.Add(context + ": ratedHorsepower/scienceCost must be positive");
            }

            if (engine.FuelPerHour <= 0m || engine.WaterPerHour <= 0m)
            {
                errors.Add(context + ": fuelPerHour/waterPerHour must be positive");
            }

            if (!TierIntervalDays.TryGetValue(engine.Tier, out var expectedInterval))
            {
                continue;
            }

            if (engine.MaintenanceIntervalDays != expectedInterval)
            {
                errors.Add(context + ": maintenanceIntervalDays must be " + expectedInterval
                           + " for " + engine.Tier + ", found " + engine.MaintenanceIntervalDays);
            }

            var expectedMaterials = TierMaterials[engine.Tier];
            if (!SameMaterials(engine.MaintenanceMaterials, expectedMaterials))
            {
                errors.Add(context + ": maintenanceMaterials must be " + FormatMaterials(expectedMaterials));
            }

            if (!tierFuelRate.TryGetValue(engine.Tier, out var fuelRate)
                || !tierWaterRate.TryGetValue(engine.Tier, out var waterRate))
            {
                continue;
            }

            var expectedFuel = Round2(engine.RatedHorsepower / 100m * fuelRate);
            if (engine.FuelPerHour != expectedFuel)
            {
                errors.Add(context + ": fuelPerHour = " + Show(engine.FuelPerHour) + " but round(hp/100 x "
                           + Show(fuelRate) + ", 2) = " + Show(expectedFuel));
            }

            var expectedWater = Round2(engine.RatedHorsepower / 100m * waterRate);
            if (engine.WaterPerHour != expectedWater)
            {
                errors.Add(context + ": waterPerHour = " + Show(engine.WaterPerHour) + " but round(hp/100 x "
                           + Show(waterRate) + ", 2) = " + Show(expectedWater));
            }

            var expectedTank = Round0(engine.WaterPerHour * waterTankHours);
            if (engine.WaterTankGoods != expectedTank)
            {
                errors.Add(context + ": waterTankGoods = " + engine.WaterTankGoods + " but round(waterPerHour x "
                           + waterTankHours + ") = " + expectedTank);
            }

            var expectedStock = (int)decimal.Ceiling(engine.FuelPerHour * fullInventoryWorkHours);
            if (engine.FuelStock != expectedStock)
            {
                errors.Add(context + ": fuelStock = " + engine.FuelStock + " but ceil(fuelPerHour x "
                           + fullInventoryWorkHours + ") = " + expectedStock);
            }
        }
    }

    /// <summary>Rounds to two decimals with midpoint-away-from-zero (the contract derivation rule).</summary>
    public static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Rounds to an integer with midpoint-away-from-zero.</summary>
    public static decimal Round0(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);

    private static bool SameMaterials(IReadOnlyList<GoodAmount> actual, IReadOnlyList<GoodAmount> expected)
    {
        if (actual.Count != expected.Count)
        {
            return false;
        }

        var used = new bool[expected.Count];
        foreach (var item in actual)
        {
            var match = false;
            for (var index = 0; index < expected.Count; index++)
            {
                if (used[index])
                {
                    continue;
                }

                if (string.Equals(expected[index].Good, item.Good, StringComparison.Ordinal)
                    && expected[index].Amount == item.Amount)
                {
                    used[index] = true;
                    match = true;
                    break;
                }
            }

            if (!match)
            {
                return false;
            }
        }

        return true;
    }

    private static string FormatMaterials(IReadOnlyList<GoodAmount> materials) =>
        string.Join(" + ", materials.Select(item => item.Good + " " + item.Amount));

    private static string Show(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static bool TryObject(JsonElement parent, string name, string context, Errors errors, out JsonElement value)
    {
        value = default;
        if (parent.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        errors.Add(context + ": '" + name + "' must be an object");
        return false;
    }

    private static bool TryArray(JsonElement parent, string name, string context, Errors errors, out JsonElement value)
    {
        value = default;
        if (parent.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        errors.Add(context + ": '" + name + "' must be an array");
        return false;
    }

    private static bool TryInt(JsonElement parent, string name, string context, Errors errors, out int value)
    {
        value = 0;
        if (parent.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out value))
        {
            return true;
        }

        errors.Add(context + ": '" + name + "' must be an integer");
        return false;
    }

    private static bool TryDecimal(JsonElement parent, string name, string context, Errors errors, out decimal value)
    {
        value = 0m;
        if (parent.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number
            && element.TryGetDecimal(out value))
        {
            return true;
        }

        errors.Add(context + ": '" + name + "' must be a number");
        return false;
    }

    private static bool TryString(JsonElement parent, string name, string context, Errors errors, out string value)
    {
        value = string.Empty;
        if (parent.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(element.GetString()))
        {
            value = element.GetString()!;
            return true;
        }

        errors.Add(context + ": '" + name + "' must be a non-empty string");
        return false;
    }

    private static int RequireInt(JsonElement parent, string name, string context, Errors errors) =>
        TryInt(parent, name, context, errors, out var value) ? value : 0;

    private static decimal RequireDecimal(JsonElement parent, string name, string context, Errors errors) =>
        TryDecimal(parent, name, context, errors, out var value) ? value : 0m;

    private static string RequireString(JsonElement parent, string name, string context, Errors errors) =>
        TryString(parent, name, context, errors, out var value) ? value : string.Empty;

    /// <summary>Collects structural findings; the caller decides when to fail.</summary>
    private sealed class Errors
    {
        private readonly List<string> _messages = new();

        public int Count => _messages.Count;

        public IReadOnlyList<string> Messages => _messages;

        public void Add(string message) => _messages.Add(message);
    }
}
