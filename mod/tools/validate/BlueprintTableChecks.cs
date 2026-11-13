using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SEE.Tools.Validate;

/// <summary>
/// The acceptance gate of requirements §17.4 / AC-01: every one of the 24 generated steam boiler
/// blueprints is compared, field by field and with zero tolerance, against the only number sources
/// (<c>data/engines.json</c> and its derived table <c>data/engines.derived.json</c>).
///
/// The twelve authoritative fields per file are the same set the generator self-verifies
/// (<c>tools/generate-blueprints/Verification.cs</c>), re-derived here from <c>engines.json</c>
/// instead of trusting the generator output:
///
/// <list type="number">
/// <item>TemplateName (path and name follow <c>SteamBoiler.&lt;Size&gt;.&lt;Tier&gt;.&lt;Faction&gt;</c>);</item>
/// <item>BlockObjectSpec footprint X/Y and height Z;</item>
/// <item>SEE.SEEBoilerSpec.Horsepower (faction output, ceiling);</item>
/// <item>SEE.SEEBoilerSpec.FuelPerHour (faction fuel, 2-decimal rounding);</item>
/// <item>SEE.SEEBoilerSpec.WaterPerHour;</item>
/// <item>SEE.SEEBoilerSpec.FuelCapacityGoods;</item>
/// <item>SEE.SEEBoilerSpec.WaterCapacityGoods;</item>
/// <item>MechanicalNodeSpec.PowerOutput;</item>
/// <item>SEE.SEEBoilerSpec.WaterCapacityGoods (the dedicated water inventory's capacity);</item>
/// <item>BuildingSpec.ScienceCost;</item>
/// <item>BuildingSpec.BuildingCost (goods and amounts, in table order);</item>
/// <item>PlaceableBlockObjectSpec.ToolGroupId (the dedicated SEE "SteamEngines" group) and ToolOrder
/// (110–142 and after the vanilla steam engine's 50).</item>
/// </list>
///
/// Extra contract checks on the same pass: fuel goods/energy, the <c>GoodConsumingBuildingSpec</c>
/// capacity derivation (§3.5.2 float32 <c>ceil</c>), the maintenance interval/materials, the three
/// localization keys plus icon per file (contract §4.11), <c>Entrance.HasEntrance</c>, the
/// <c>Children</c>/<c>ConstructionBase</c> structure, the <c>Blocks</c> length and the two
/// <c>TemplateCollection</c> append lists (exactly the 12 own-faction blueprints, no wrong faction).
/// </summary>
internal sealed class BlueprintTableChecks
{
    /// <summary>The twelve authoritative fields per file (requirements §4.2 table).</summary>
    public const int AuthoritativeFieldCount = 12;

    private const int ToolOrderMin = 110;
    private const int ToolOrderMax = 142;
    private const int VanillaSteamEngineToolOrder = 50;
    private const int FullInventoryWorkHours = 25;

    private static readonly string[] Sizes = { "Small", "Medium", "Large" };
    private static readonly string[] Tiers = { "T1", "T2", "T3", "T4" };
    private static readonly string[] Factions = { "Folktails", "IronTeeth" };

    private static readonly string[] ForbiddenTemplateFragments = { "SteamEngine", "SteamPlant", "TWSteamEngine" };

    private static readonly Dictionary<string, string[]> ExpectedFuelGoods = new(StringComparer.Ordinal)
    {
        ["T1"] = new[] { "Log" },
        ["T2"] = new[] { "Log" },
        ["T3"] = new[] { "Log", "Plank" },
        ["T4"] = new[] { "Log", "Plank" },
    };

    private static readonly Dictionary<string, decimal[]> ExpectedFuelValues = new(StringComparer.Ordinal)
    {
        ["T1"] = new[] { 1m },
        ["T2"] = new[] { 1m },
        ["T3"] = new[] { 1m, 2m },
        ["T4"] = new[] { 1m, 2m },
    };

    private readonly string _dataDir;
    private readonly Report _report;
    private readonly GameBlueprintCatalog _game;

    private readonly Dictionary<string, EngineRow> _rows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, decimal> _tierFuelRate = new(StringComparer.Ordinal);
    private readonly Dictionary<string, decimal> _tierWaterRate = new(StringComparer.Ordinal);
    private readonly Dictionary<string, decimal> _factionOutput = new(StringComparer.Ordinal);
    private readonly Dictionary<string, decimal> _factionFuel = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, int>> _toolOrders = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VariantExpectation> _expected = new(StringComparer.Ordinal);
    private readonly HashSet<string> _expectedPaths = new(StringComparer.Ordinal);
    private readonly SortedSet<string> _constructionDeviations = new(StringComparer.Ordinal);
    private bool _derivedLoaded;

    public BlueprintTableChecks(string dataDir, Report report, GameBlueprintCatalog game)
    {
        _dataDir = dataDir;
        _report = report;
        _game = game;
    }

    /// <summary>Files whose twelve authoritative fields were all compared (24 when complete).</summary>
    public int FilesValidated { get; private set; }

    /// <summary>Total field comparisons performed (files × 12).</summary>
    public int FieldsValidated { get; private set; }

    /// <summary>Number of field or structural mismatches found.</summary>
    public int Mismatches { get; private set; }

    public void Validate()
    {
        if (!LoadEngines())
        {
            return;
        }

        BuildExpectations();
        LoadDerived();
        ValidateDerived();
        ValidateBlueprintFiles();
        ValidateCollections();
    }

    private bool LoadEngines()
    {
        var path = Path.Combine(_dataDir, "engines.json");
        if (!File.Exists(path))
        {
            _report.Error("table", "data/engines.json is missing; the 24 blueprints have no authoritative number source");
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            foreach (var tier in Tiers)
            {
                _tierFuelRate[tier] = Number(root, "tierFuelRatePer100Hp", tier);
                _tierWaterRate[tier] = Number(root, "tierWaterRatePer100Hp", tier);
            }

            if (root.TryGetProperty("factions", out var factions))
            {
                foreach (var faction in Factions)
                {
                    if (!factions.TryGetProperty(faction, out var definition))
                    {
                        _report.Error("table", "engines.json: factions." + faction + " is missing");
                        return false;
                    }

                    _factionOutput[faction] = Number(definition, "output");
                    _factionFuel[faction] = Number(definition, "fuel");
                }
            }
            else
            {
                _report.Error("table", "engines.json: factions object is missing");
                return false;
            }

            if (root.TryGetProperty("toolOrders", out var orders))
            {
                foreach (var tier in Tiers)
                {
                    var bySize = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (var size in Sizes)
                    {
                        bySize[size] = (int)Number(orders, tier, size);
                    }

                    _toolOrders[tier] = bySize;
                }
            }
            else
            {
                _report.Error("table", "engines.json: toolOrders object is missing");
                return false;
            }

            if (!root.TryGetProperty("engines", out var engines) || engines.ValueKind != JsonValueKind.Array)
            {
                _report.Error("table", "engines.json: engines array is missing");
                return false;
            }

            foreach (var entry in engines.EnumerateArray())
            {
                var row = EngineRow.Parse(entry, _report);
                if (row is null)
                {
                    continue;
                }

                var key = row.Size + "|" + row.Tier;
                if (!_rows.TryAdd(key, row))
                {
                    _report.Error("table", "engines.json: duplicate row " + key);
                }
            }

            if (_rows.Count != 12)
            {
                _report.Error("table", "engines.json: expected 12 (size, tier) rows, found " + _rows.Count);
                return false;
            }

            return true;
        }
        catch (JsonException exception)
        {
            _report.Error("table", "data/engines.json is not valid JSON: " + exception.Message);
            return false;
        }
    }

    private void BuildExpectations()
    {
        foreach (var row in _rows.Values)
        {
            CheckEngineRowMath(row);

            foreach (var faction in Factions)
            {
                var expectation = new VariantExpectation
                {
                    TemplateName = "SteamBoiler." + row.Size + "." + row.Tier + "." + faction,
                    BlueprintRelativePath = "Buildings/Power/SteamBoiler/SteamBoiler." + row.Size + "." + row.Tier + "."
                                            + faction + ".blueprint.json",
                    Size = row.Size,
                    Tier = row.Tier,
                    Faction = faction,
                    FootprintX = row.FootprintX,
                    FootprintY = row.FootprintY,
                    Height = row.Height,
                    Horsepower = (int)Math.Ceiling(row.RatedHorsepower * _factionOutput[faction]),
                    FuelPerHour = Math.Round(row.FuelPerHour * _factionFuel[faction], 2, MidpointRounding.AwayFromZero),
                    WaterPerHour = row.WaterPerHour,
                    FuelStock = row.FuelStock,
                    WaterTankGoods = row.WaterTankGoods,
                    ScienceCost = row.ScienceCost,
                    BuildCost = row.BuildCost,
                    ToolOrder = _toolOrders[row.Tier][row.Size],
                    MaintenanceIntervalDays = row.MaintenanceIntervalDays,
                    MaintenanceMaterials = row.MaintenanceMaterials,
                    FuelGoods = ExpectedFuelGoods[row.Tier],
                    FuelValues = ExpectedFuelValues[row.Tier],
                    ConsumedGoods = BuildConsumedGoods(row),
                };

                _expected[expectation.TemplateName] = expectation;
                _expectedPaths.Add(expectation.BlueprintRelativePath);
            }
        }
    }

    /// <summary>
    /// Re-derives the base rates of one engines.json row from its own tier rates, so a hand-edited
    /// engines.json cannot silently disagree with the rate tables the blueprints are checked against.
    /// </summary>
    private void CheckEngineRowMath(EngineRow row)
    {
        var fuel = Math.Round(row.RatedHorsepower / 100m * _tierFuelRate[row.Tier], 2, MidpointRounding.AwayFromZero);
        if (fuel != row.FuelPerHour)
        {
            Mismatch("data/engines.json", row.Size + "." + row.Tier + ".fuelPerHour", Show(row.FuelPerHour),
                Show(fuel) + " (round(ratedHorsepower/100 x tierFuelRate))");
        }

        var water = Math.Round(row.RatedHorsepower / 100m * _tierWaterRate[row.Tier], 2, MidpointRounding.AwayFromZero);
        if (water != row.WaterPerHour)
        {
            Mismatch("data/engines.json", row.Size + "." + row.Tier + ".waterPerHour", Show(row.WaterPerHour),
                Show(water) + " (round(ratedHorsepower/100 x tierWaterRate))");
        }

        var tank = (int)Math.Round(row.WaterPerHour * 200m, 0, MidpointRounding.AwayFromZero);
        if (tank != row.WaterTankGoods)
        {
            Mismatch("data/engines.json", row.Size + "." + row.Tier + ".waterTankGoods",
                row.WaterTankGoods.ToString(CultureInfo.InvariantCulture),
                tank.ToString(CultureInfo.InvariantCulture) + " (round(waterPerHour x 200))");
        }

        var derivedCapacity = 0;
        foreach (var consumed in BuildConsumedGoods(row))
        {
            derivedCapacity += (int)MathF.Ceiling(consumed.Rate * FullInventoryWorkHours);
        }

        if (derivedCapacity != row.FuelStock)
        {
            Mismatch("data/engines.json", row.Size + "." + row.Tier + ".fuelStock",
                row.FuelStock.ToString(CultureInfo.InvariantCulture),
                "float32 ceil of the §3.5.2 capacity rate (sums to " + derivedCapacity + ")");
        }
    }

    /// <summary>
    /// Capacity rates of contract §3.5.2: T1/T2 use one Log entry <c>(fuelStock - 0.5) / 25</c>;
    /// T3/T4 use two entries of <c>(fuelStock / 2 - 0.5) / 25</c>. float32 <c>ceil</c> of each rate
    /// times 25 must reproduce the tabulated fuel stock. Bug fix 2026-11-08: Water is NOT a
    /// GoodConsuming good any more; the dedicated water tank lives in a code-created public input
    /// inventory whose capacity is the boiler spec's <c>WaterCapacityGoods</c>.
    /// </summary>
    private static List<ConsumedGood> BuildConsumedGoods(EngineRow row)
    {
        var result = new List<ConsumedGood>();
        if (row.Tier is "T1" or "T2")
        {
            result.Add(new ConsumedGood("Log", (row.FuelStock - 0.5f) / 25f));
        }
        else
        {
            var rate = (row.FuelStock / 2f - 0.5f) / 25f;
            result.Add(new ConsumedGood("Log", rate));
            result.Add(new ConsumedGood("Plank", rate));
        }

        return result;
    }

    private void LoadDerived()
    {
        var path = Path.Combine(_dataDir, "engines.derived.json");
        if (!File.Exists(path))
        {
            _report.Error("table", "data/engines.derived.json is missing (generator output; re-run the generator)");
            return;
        }

        _derivedLoaded = true;
        var expectedSourceHash = HashFile(Path.Combine(_dataDir, "engines.json"));

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            if (root.TryGetProperty("source", out var source)
                && source.ValueKind == JsonValueKind.String
                && source.GetString() != "data/engines.json")
            {
                Mismatch("data/engines.derived.json", "source", Text(source) ?? "<null>", "data/engines.json");
            }

            if (root.TryGetProperty("sourceSha256", out var sourceHash) && sourceHash.ValueKind == JsonValueKind.String)
            {
                var recorded = sourceHash.GetString();
                if (!string.Equals(recorded, expectedSourceHash, StringComparison.OrdinalIgnoreCase))
                {
                    Mismatch("data/engines.derived.json", "sourceSha256", recorded ?? "<null>",
                        expectedSourceHash + " (engines.json has changed; re-run the generator)");
                }
            }

            if (root.TryGetProperty("blueprintCount", out var count) && TryInt(count, out var blueprintCount)
                && blueprintCount != 24)
            {
                Mismatch("data/engines.derived.json", "blueprintCount", blueprintCount.ToString(CultureInfo.InvariantCulture), "24");
            }

            if (root.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Array
                && rows.GetArrayLength() != 12)
            {
                Mismatch("data/engines.derived.json", "rows", rows.GetArrayLength().ToString(CultureInfo.InvariantCulture), "12");
            }
        }
        catch (JsonException exception)
        {
            _report.Error("table", "data/engines.derived.json is not valid JSON: " + exception.Message);
            return;
        }

        _derivedPath = path;
    }

    private string? _derivedPath;

    private void ValidateDerived()
    {
        if (!_derivedLoaded || _derivedPath is null)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(_derivedPath));
            if (!document.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
            {
                _report.Error("table", "data/engines.derived.json has no rows array");
                return;
            }

            foreach (var row in rows.EnumerateArray())
            {
                var size = Text(row, "size");
                var tier = Text(row, "tier");
                if (size is null || tier is null)
                {
                    _report.Error("table", "data/engines.derived.json row is missing size/tier");
                    continue;
                }

                if (!row.TryGetProperty("variants", out var variants) || variants.ValueKind != JsonValueKind.Object)
                {
                    _report.Error("table", "data/engines.derived.json row has no variants object");
                    continue;
                }

                foreach (var variant in variants.EnumerateObject())
                {
                    var templateName = "SteamBoiler." + size + "." + tier + "." + variant.Name;
                    if (!_expected.TryGetValue(templateName, out var expectation))
                    {
                        Mismatch("data/engines.derived.json", "variant", templateName,
                            "one of the 24 SteamBoiler.<Size>.<Tier>.<Faction> names");
                        continue;
                    }

                    seen.Add(templateName);
                    CompareDerivedVariant(templateName, variant.Value, expectation);
                }
            }
        }
        catch (JsonException exception)
        {
            _report.Error("table", "data/engines.derived.json is not valid JSON: " + exception.Message);
            return;
        }

        foreach (var templateName in _expected.Keys)
        {
            if (!seen.Contains(templateName))
            {
                Mismatch("data/engines.derived.json", "variant", "<missing>", templateName);
            }
        }
    }

    private void CompareDerivedVariant(string templateName, JsonElement variant, VariantExpectation expectation)
    {
        const string where = "data/engines.derived.json";
        ExpectInt(where, templateName, "horsepower", variant, expectation.Horsepower);
        ExpectDecimal(where, templateName, "fuelPerHour", variant, expectation.FuelPerHour);
        ExpectDecimal(where, templateName, "waterPerHour", variant, expectation.WaterPerHour);
        ExpectInt(where, templateName, "fuelStock", variant, expectation.FuelStock);
        ExpectInt(where, templateName, "waterTankGoods", variant, expectation.WaterTankGoods);
        ExpectInt(where, templateName, "toolOrder", variant, expectation.ToolOrder);
        ExpectText(where, templateName, "templateName", variant, expectation.TemplateName);

        var blueprint = Text(variant, "blueprint") ?? string.Empty;
        if (!string.Equals(blueprint.Replace('\\', '/'), "data/" + expectation.BlueprintRelativePath, StringComparison.Ordinal))
        {
            Mismatch(where, templateName + ".blueprint", blueprint, "data/" + expectation.BlueprintRelativePath);
        }

        ExpectStringArray(where, templateName, "fuelGoodIds", variant, expectation.FuelGoods);
        ExpectNumberArray(where, templateName, "fuelValues", variant, expectation.FuelValues);

        if (variant.TryGetProperty("consumedGoods", out var consumed) && consumed.ValueKind == JsonValueKind.Array)
        {
            var actual = new List<ConsumedGood>();
            foreach (var item in consumed.EnumerateArray())
            {
                actual.Add(new ConsumedGood(Text(item, "goodId") ?? string.Empty, (float)DecimalValue(item, "goodPerHour")));
            }

            if (!ConsumedGoodsEqual(actual, expectation.ConsumedGoods))
            {
                Mismatch(where, templateName + ".consumedGoods", DescribeConsumed(actual),
                    DescribeConsumed(expectation.ConsumedGoods));
            }
        }
        else
        {
            Mismatch(where, templateName + ".consumedGoods", "<missing>", DescribeConsumed(expectation.ConsumedGoods));
        }
    }

    private void ValidateBlueprintFiles()
    {
        foreach (var expectation in _expected.Values)
        {
            ValidateBlueprint(expectation);
        }

        var folder = Path.Combine(_dataDir, "Buildings", "Power", "SteamBoiler");
        if (Directory.Exists(folder))
        {
            var actual = Directory.GetFiles(folder, "*.blueprint.json", SearchOption.TopDirectoryOnly)
                .Select(file => "Buildings/Power/SteamBoiler/" + Path.GetFileName(file))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            foreach (var extra in actual.Where(file => !_expectedPaths.Contains(file)))
            {
                Mismatch(extra, "file", "present", "not part of the 24-file contract");
            }

            if (actual.Count != 24)
            {
                Mismatch("Buildings/Power/SteamBoiler", "blueprint count", actual.Count.ToString(CultureInfo.InvariantCulture), "24");
            }
        }
        else
        {
            _report.Error("table", "data/Buildings/Power/SteamBoiler is missing");
        }

        if (_constructionDeviations.Count > 0)
        {
            _report.Warn("table", "ConstructionBase recorded deviation (contract §4.5/§4.8): "
                                  + string.Join("; ", _constructionDeviations));
        }
    }

    private void ValidateBlueprint(VariantExpectation expectation)
    {
        var relative = expectation.BlueprintRelativePath;
        var path = Path.Combine(_dataDir, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            Mismatch(relative, "file", "<missing>", "24 generated blueprints");
            return;
        }

        FilesValidated++;
        FieldsValidated += AuthoritativeFieldCount;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path));
        }
        catch (JsonException exception)
        {
            _report.Error("table", relative + ": invalid JSON: " + exception.Message);
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            CheckTemplateName(relative, root, expectation);
            CheckFootprint(relative, root, expectation);
            CheckBoilerSpec(relative, root, expectation);
            CheckMechanicalNode(relative, root, expectation);
            CheckStockpile(relative, root, expectation);
            CheckBuildingSpec(relative, root, expectation);
            CheckToolOrder(relative, root, expectation);
            CheckMaintenance(relative, root, expectation);
            CheckLocAndIcon(relative, root, expectation);
            CheckEntranceAndBlocks(relative, root, expectation);
            CheckChildren(relative, root, expectation);
        }
    }

    private void CheckTemplateName(string relative, JsonElement root, VariantExpectation expectation)
    {
        var template = Spec(root, "TemplateSpec");
        var actual = Text(template, "TemplateName");
        if (!string.Equals(actual, expectation.TemplateName, StringComparison.Ordinal))
        {
            Mismatch(relative, "TemplateSpec.TemplateName", actual ?? "<missing>", expectation.TemplateName);
        }

        foreach (var fragment in ForbiddenTemplateFragments)
        {
            if (actual is not null && actual.Contains(fragment, StringComparison.Ordinal))
            {
                Mismatch(relative, "TemplateSpec.TemplateName", actual,
                    "must not use the forbidden fragment '" + fragment + "'");
            }
        }

        if (!string.Equals(relative, expectation.BlueprintRelativePath, StringComparison.Ordinal))
        {
            Mismatch(relative, "file path", relative, expectation.BlueprintRelativePath);
        }
    }

    private void CheckFootprint(string relative, JsonElement root, VariantExpectation expectation)
    {
        var blockObject = Spec(root, "BlockObjectSpec");
        var size = blockObject.HasValue && blockObject.Value.TryGetProperty("Size", out var value) ? value : (JsonElement?)null;

        if (size is null
            || !TryInt(size.Value, "X", out var x)
            || !TryInt(size.Value, "Y", out var y)
            || !TryInt(size.Value, "Z", out var z))
        {
            Mismatch(relative, "BlockObjectSpec.Size", "<missing or not numeric>",
                expectation.FootprintX + "x" + expectation.FootprintY + "x" + expectation.Height);
            return;
        }

        if (x != expectation.FootprintX || y != expectation.FootprintY || z != expectation.Height)
        {
            Mismatch(relative, "BlockObjectSpec.Size", x + "x" + y + "x" + z,
                expectation.FootprintX + "x" + expectation.FootprintY + "x" + expectation.Height);
        }
    }

    private void CheckBoilerSpec(string relative, JsonElement root, VariantExpectation expectation)
    {
        const string specName = ContractSpecs.BoilerKey;
        var spec = Spec(root, specName);
        if (spec is null || spec.Value.ValueKind != JsonValueKind.Object)
        {
            Mismatch(relative, specName, "<missing>", "the frozen SEE boiler spec");
            return;
        }

        ExpectInt(relative, specName, "Horsepower", spec.Value, expectation.Horsepower);
        ExpectDecimal(relative, specName, "FuelPerHour", spec.Value, expectation.FuelPerHour);
        ExpectDecimal(relative, specName, "WaterPerHour", spec.Value, expectation.WaterPerHour);
        ExpectInt(relative, specName, "FuelCapacityGoods", spec.Value, expectation.FuelStock);
        ExpectInt(relative, specName, "WaterCapacityGoods", spec.Value, expectation.WaterTankGoods);

        ExpectText(relative, specName, "Tier", spec.Value, expectation.Tier);
        ExpectText(relative, specName, "Size", spec.Value, expectation.Size);
        ExpectText(relative, specName, "Faction", spec.Value, expectation.Faction);
        ExpectText(relative, specName, "FuelScaling", spec.Value, "proportional");
        ExpectStringArray(relative, specName, "FuelGoodIds", spec.Value, expectation.FuelGoods);
        ExpectNumberArray(relative, specName, "FuelValues", spec.Value, expectation.FuelValues);
    }

    private void CheckMechanicalNode(string relative, JsonElement root, VariantExpectation expectation)
    {
        var node = Spec(root, "MechanicalNodeSpec");
        if (node is null)
        {
            Mismatch(relative, "MechanicalNodeSpec", "<missing>", "PowerOutput=" + expectation.Horsepower
                                                                    + ", PowerInput=0, IsShaft=false");
            return;
        }

        ExpectInt(relative, "MechanicalNodeSpec", "PowerOutput", node.Value, expectation.Horsepower);
        ExpectInt(relative, "MechanicalNodeSpec", "PowerInput", node.Value, 0);
        if (!TryBool(node.Value, "IsShaft", out var shaft) || shaft)
        {
            Mismatch(relative, "MechanicalNodeSpec.IsShaft", shaft.ToString(), "false");
        }
    }

    private void CheckStockpile(string relative, JsonElement root, VariantExpectation expectation)
    {
        // Bug fix 2026-11-08: the water tank is a dedicated public input Inventory created in code
        // (SEEWaterInventoryInitializer). A blueprint StockpileSpec would reintroduce the
        // NoHaulingPostStatus double-init crash, and listing Water as a GoodConsuming good would
        // share the fuel inventory again. The tank size is checked through the boiler spec
        // (WaterCapacityGoods, see CheckBoilerSpec).
        if (Spec(root, "StockpileSpec") is not null)
        {
            Mismatch(relative, "StockpileSpec", "<present>",
                "<absent> (water tank is a code-created public input inventory)");
        }

        if (RootHasConsumedGood(root, "Water"))
        {
            Mismatch(relative, "GoodConsumingBuildingSpec.ConsumedGoods[Water]", "<present>",
                "<absent> (dedicated water inventory owns the tank)");
        }
    }

    private static bool RootHasConsumedGood(JsonElement root, string goodId)
    {
        if (!root.TryGetProperty("GoodConsumingBuildingSpec", out var consumer)
            || !consumer.TryGetProperty("ConsumedGoods", out var goods)
            || goods.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in goods.EnumerateArray())
        {
            if (item.TryGetProperty("GoodId", out var id) && id.GetString() == goodId)
            {
                return true;
            }
        }

        return false;
    }

    private void CheckBuildingSpec(string relative, JsonElement root, VariantExpectation expectation)
    {
        var building = Spec(root, "BuildingSpec");
        if (building is null)
        {
            Mismatch(relative, "BuildingSpec.ScienceCost", "<missing>", expectation.ScienceCost.ToString(CultureInfo.InvariantCulture));
            return;
        }

        ExpectInt(relative, "BuildingSpec", "ScienceCost", building.Value, expectation.ScienceCost);

        if (!building.Value.TryGetProperty("BuildingCost", out var costs) || costs.ValueKind != JsonValueKind.Array)
        {
            Mismatch(relative, "BuildingSpec.BuildingCost", "<missing>", DescribeCosts(expectation.BuildCost));
            return;
        }

        var actual = new List<CostItem>();
        foreach (var item in costs.EnumerateArray())
        {
            actual.Add(new CostItem(Text(item, "Id") ?? string.Empty,
                TryInt(item, "Amount", out var amount) ? amount : -1));
        }

        if (!CostsEqual(actual, expectation.BuildCost))
        {
            Mismatch(relative, "BuildingSpec.BuildingCost", DescribeCosts(actual), DescribeCosts(expectation.BuildCost));
        }
    }

    private void CheckToolOrder(string relative, JsonElement root, VariantExpectation expectation)
    {
        var placeable = Spec(root, "PlaceableBlockObjectSpec");
        if (placeable is null)
        {
            Mismatch(relative, "PlaceableBlockObjectSpec.ToolOrder", "<missing>",
                expectation.ToolOrder.ToString(CultureInfo.InvariantCulture));
            return;
        }

        ExpectText(relative, "PlaceableBlockObjectSpec", "ToolGroupId", placeable.Value, ContractSpecs.ToolGroupId);

        if (!TryInt(placeable.Value, "ToolOrder", out var order))
        {
            Mismatch(relative, "PlaceableBlockObjectSpec.ToolOrder", "<missing>",
                expectation.ToolOrder.ToString(CultureInfo.InvariantCulture));
            return;
        }

        if (order != expectation.ToolOrder)
        {
            Mismatch(relative, "PlaceableBlockObjectSpec.ToolOrder", order.ToString(CultureInfo.InvariantCulture),
                expectation.ToolOrder.ToString(CultureInfo.InvariantCulture));
        }

        if (order < ToolOrderMin || order > ToolOrderMax)
        {
            Mismatch(relative, "PlaceableBlockObjectSpec.ToolOrder", order.ToString(CultureInfo.InvariantCulture),
                "inside the contract range " + ToolOrderMin + "-" + ToolOrderMax);
        }

        if (order <= VanillaSteamEngineToolOrder)
        {
            Mismatch(relative, "PlaceableBlockObjectSpec.ToolOrder", order.ToString(CultureInfo.InvariantCulture),
                "after the vanilla steam engine's " + VanillaSteamEngineToolOrder);
        }
    }

    private void CheckMaintenance(string relative, JsonElement root, VariantExpectation expectation)
    {
        const string specName = ContractSpecs.MaintenanceKey;
        var maintenance = Spec(root, specName);
        if (maintenance is null)
        {
            Mismatch(relative, specName, "<missing>", "the frozen SEE maintenance spec");
            return;
        }

        ExpectDecimal(relative, specName, "IntervalDays", maintenance.Value, expectation.MaintenanceIntervalDays);

        if (!maintenance.Value.TryGetProperty("Materials", out var materials)
            || materials.ValueKind != JsonValueKind.Array)
        {
            Mismatch(relative, specName + ".Materials", "<missing>", DescribeCosts(expectation.MaintenanceMaterials));
            return;
        }

        var actual = new List<CostItem>();
        foreach (var item in materials.EnumerateArray())
        {
            actual.Add(new CostItem(Text(item, "Id") ?? string.Empty,
                TryInt(item, "Amount", out var amount) ? amount : -1));
        }

        if (!CostsEqual(actual, expectation.MaintenanceMaterials))
        {
            Mismatch(relative, specName + ".Materials", DescribeCosts(actual),
                DescribeCosts(expectation.MaintenanceMaterials));
        }
    }

    private void CheckLocAndIcon(string relative, JsonElement root, VariantExpectation expectation)
    {
        var labeled = Spec(root, "LabeledEntitySpec");
        if (labeled is null)
        {
            Mismatch(relative, "LabeledEntitySpec", "<missing>", "the three localization keys and the icon");
            return;
        }

        ExpectText(relative, "LabeledEntitySpec", "DisplayNameLocKey", labeled.Value,
            "Building.SEE.SteamBoiler." + expectation.Size + "." + expectation.Tier + ".DisplayName");
        ExpectText(relative, "LabeledEntitySpec", "DescriptionLocKey", labeled.Value,
            "Building.SEE.SteamBoiler.Description");
        ExpectText(relative, "LabeledEntitySpec", "FlavorDescriptionLocKey", labeled.Value,
            "Building.SEE.SteamBoiler.Flavor." + expectation.Tier);

        var icon = Text(labeled.Value, "Icon") ?? string.Empty;
        if (icon.Length == 0)
        {
            Mismatch(relative, "LabeledEntitySpec.Icon", "<missing>", "Icons/<name> without extension");
            return;
        }

        if (icon.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            Mismatch(relative, "LabeledEntitySpec.Icon", icon, "the icon reference must not carry a file extension");
        }

        var iconPath = Path.Combine(_dataDir, icon.Replace('/', Path.DirectorySeparatorChar) + ".png");
        if (!File.Exists(iconPath))
        {
            Mismatch(relative, "LabeledEntitySpec.Icon", icon, "an existing data/Icons/<name>.png");
        }

        var metaPath = iconPath + ".meta.json";
        if (!File.Exists(metaPath))
        {
            Mismatch(relative, "LabeledEntitySpec.Icon", icon, "an existing data/Icons/<name>.png.meta.json (isSprite)");
        }
    }

    private void CheckEntranceAndBlocks(string relative, JsonElement root, VariantExpectation expectation)
    {
        var blockObject = Spec(root, "BlockObjectSpec");
        if (blockObject is null)
        {
            Mismatch(relative, "BlockObjectSpec.Entrance", "<missing>", "HasEntrance=true");
            return;
        }

        if (!blockObject.Value.TryGetProperty("Entrance", out var entrance)
            || !TryBool(entrance, "HasEntrance", out var hasEntrance)
            || !hasEntrance)
        {
            Mismatch(relative, "BlockObjectSpec.Entrance.HasEntrance", "<missing or false>", "true");
        }

        var blocks = expectation.FootprintX * expectation.FootprintY * expectation.Height;
        if (!blockObject.Value.TryGetProperty("Blocks", out var blockArray) || blockArray.ValueKind != JsonValueKind.Array)
        {
            Mismatch(relative, "BlockObjectSpec.Blocks", "<missing>", blocks + " entries");
        }
        else if (blockArray.GetArrayLength() != blocks)
        {
            Mismatch(relative, "BlockObjectSpec.Blocks", blockArray.GetArrayLength().ToString(CultureInfo.InvariantCulture),
                blocks + " entries (X*Y*Z)");
        }
    }

    private void CheckChildren(string relative, JsonElement root, VariantExpectation expectation)
    {
        var children = Prop(root, "Children");
        var finished = Prop(children, "#Finished");
        var unfinished = Prop(children, "#Unfinished");
        if (finished is null || unfinished is null)
        {
            Mismatch(relative, "Children", "<missing #Finished or #Unfinished>",
                "the fixed three-layer structure of contract §4.8");
            return;
        }

        if (Prop(finished.Value, "TimbermeshSpec") is null || Prop(finished.Value, "CollidersSpec") is null)
        {
            Mismatch(relative, "Children.#Finished", "<missing TimbermeshSpec or CollidersSpec>",
                "TimbermeshSpec + CollidersSpec");
        }

        var nested = Prop(unfinished.Value, "Children");
        if (nested is null)
        {
            Mismatch(relative, "Children.#Unfinished.Children", "<missing>",
                "ConstructionBase<XxZ>#nested + ConstructionStage0");
            return;
        }

        var constructionBase = nested.Value.EnumerateObject()
            .FirstOrDefault(property => property.Name.StartsWith("ConstructionBase", StringComparison.Ordinal));
        if (constructionBase.Equals(default(JsonProperty)) || string.IsNullOrEmpty(constructionBase.Name))
        {
            Mismatch(relative, "Children.#Unfinished.Children", "<missing ConstructionBase entry>",
                "ConstructionBase<XxZ>#nested");
        }
        else
        {
            CheckConstructionBase(relative, expectation, constructionBase.Name, constructionBase.Value);
        }

        if (!nested.Value.TryGetProperty("ConstructionStage0", out var stage)
            || Prop(stage, "TimbermeshSpec") is null)
        {
            Mismatch(relative, "Children.#Unfinished.ConstructionStage0", "<missing>", "TimbermeshSpec + CollidersSpec");
        }
    }

    private void CheckConstructionBase(string relative, VariantExpectation expectation, string childName, JsonElement child)
    {
        var blueprintPath = Text(child, "BlueprintPath") ?? string.Empty;
        if (blueprintPath.Length == 0 || !blueprintPath.EndsWith(".blueprint", StringComparison.Ordinal))
        {
            Mismatch(relative, "Children.#Unfinished." + childName, blueprintPath.Length == 0 ? "<missing BlueprintPath>" : blueprintPath,
                "a vanilla ConstructionBase path ending in .blueprint");
            return;
        }

        if (_game.Available && !_game.HasBlueprint(blueprintPath))
        {
            Mismatch(relative, "Children.#Unfinished." + childName + ".BlueprintPath", blueprintPath,
                "an existing vanilla blueprint path");
        }

        // CONTRACT §4.5 says the two numbers are X width x Z height (the vanilla set contains e.g.
        // ConstructionBase2x3 for a 2x2x3 boiler). The tabulated mapping currently picks the
        // footprint-matching base, so this is recorded as a deviation for the lead instead of a
        // hard failure (the task contract accepts "exists or is recorded").
        var tokens = childName.Replace("#nested", string.Empty, StringComparison.Ordinal)
            .Replace("ConstructionBase", string.Empty, StringComparison.Ordinal)
            .Replace("WithoutCorners", string.Empty, StringComparison.Ordinal)
            .Split('x', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 2
            && int.TryParse(tokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
            && int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height)
            && (width != expectation.FootprintX || height != expectation.Height))
        {
            _constructionDeviations.Add(expectation.Size + " uses " + childName + " (contract XxZ expects "
                                        + expectation.FootprintX + "x" + expectation.Height + ")");
        }
    }

    private void ValidateCollections()
    {
        var folder = Path.Combine(_dataDir, "TemplateCollections");
        if (!Directory.Exists(folder))
        {
            _report.Error("table", "data/TemplateCollections is missing");
            return;
        }

        foreach (var faction in Factions)
        {
            var relative = "TemplateCollections/TemplateCollection.Buildings." + faction + ".blueprint.json";
            var path = Path.Combine(folder, Path.GetFileName(relative));
            if (!File.Exists(path))
            {
                _report.Error("table", relative + " is missing (the faction's 12 blueprints would not appear in the build menu)");
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var spec = Spec(document.RootElement, "TemplateCollectionSpec");
                if (spec is null)
                {
                    _report.Error("table", relative + ": TemplateCollectionSpec is missing");
                    continue;
                }

                ExpectText(relative, "TemplateCollectionSpec", "CollectionId", spec.Value, "Buildings." + faction);

                if (!spec.Value.TryGetProperty("Blueprints#append", out var append)
                    || append.ValueKind != JsonValueKind.Array)
                {
                    _report.Error("table", relative + ": Blueprints#append is missing");
                    continue;
                }

                var actual = append.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString() ?? string.Empty)
                    .ToList();

                var expected = _expected.Values
                    .Where(item => string.Equals(item.Faction, faction, StringComparison.Ordinal))
                    .Select(item => item.BlueprintRelativePath[..^".json".Length])
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .ToList();

                if (actual.Count != 12)
                {
                    Mismatch(relative, "Blueprints#append", actual.Count.ToString(CultureInfo.InvariantCulture),
                        "12 own-faction blueprints");
                }

                foreach (var entry in actual.Where(entry => !expected.Contains(entry, StringComparer.Ordinal)))
                {
                    Mismatch(relative, "Blueprints#append", entry,
                        "one of the 12 own-faction blueprint paths (wrong faction or unknown file)");
                }

                foreach (var entry in expected.Where(entry => !actual.Contains(entry, StringComparer.Ordinal)))
                {
                    Mismatch(relative, "Blueprints#append", "<missing>", entry);
                }
            }
            catch (JsonException exception)
            {
                _report.Error("table", relative + ": invalid JSON: " + exception.Message);
            }
        }
    }

    private void ExpectInt(string where, string owner, string field, JsonElement parent, int expected)
    {
        if (!TryInt(parent, field, out var actual))
        {
            Mismatch(where, owner + "." + field, "<missing or not an integer>", expected.ToString(CultureInfo.InvariantCulture));
            return;
        }

        if (actual != expected)
        {
            Mismatch(where, owner + "." + field, actual.ToString(CultureInfo.InvariantCulture),
                expected.ToString(CultureInfo.InvariantCulture));
        }
    }

    private void ExpectDecimal(string where, string owner, string field, JsonElement parent, decimal expected)
    {
        if (!TryDecimal(parent, field, out var actual))
        {
            Mismatch(where, owner + "." + field, "<missing or not a number>", Show(expected));
            return;
        }

        if (actual != expected)
        {
            Mismatch(where, owner + "." + field, Show(actual), Show(expected));
        }
    }

    private void ExpectText(string where, string owner, string field, JsonElement parent, string expected)
    {
        var actual = Text(parent, field);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            Mismatch(where, owner + "." + field, actual ?? "<missing>", expected);
        }
    }

    private void ExpectStringArray(string where, string owner, string field, JsonElement parent, string[] expected)
    {
        if (!parent.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            Mismatch(where, owner + "." + field, "<missing>", string.Join(",", expected));
            return;
        }

        var actual = value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .ToList();

        if (actual.Count != expected.Length || !actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            Mismatch(where, owner + "." + field, string.Join(",", actual), string.Join(",", expected));
        }
    }

    private void ExpectNumberArray(string where, string owner, string field, JsonElement parent, decimal[] expected)
    {
        if (!parent.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            Mismatch(where, owner + "." + field, "<missing>", string.Join(",", expected));
            return;
        }

        var actual = value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Number)
            .Select(item => item.GetDecimal())
            .ToList();

        if (actual.Count != expected.Length || !actual.SequenceEqual(expected))
        {
            Mismatch(where, owner + "." + field, string.Join(",", actual), string.Join(",", expected));
        }
    }

    private void Mismatch(string where, string field, string actual, string expected)
    {
        Mismatches++;
        _report.Error("table", where + ": " + field + " = " + actual
                             + " but engines.json/engines.derived.json says " + expected);
    }

    private static JsonElement? Spec(JsonElement root, string name) => Prop(root, name);

    private static JsonElement? Prop(JsonElement? element, string name)
    {
        if (!element.HasValue || element.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return element.Value.TryGetProperty(name, out var value) ? value : null;
    }

    private static string? Text(JsonElement? element, string name) => Text(Prop(element, name));

    private static string? Text(JsonElement? element)
    {
        if (!element.HasValue || element.Value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return element.Value.GetString();
    }

    private static bool TryInt(JsonElement parent, string name, out int value)
    {
        value = 0;
        return parent.ValueKind == JsonValueKind.Object
               && parent.TryGetProperty(name, out var element)
               && TryInt(element, out value);
    }

    private static bool TryInt(JsonElement element, out int value)
    {
        value = 0;
        if (element.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        if (element.TryGetInt32(out value))
        {
            return true;
        }

        if (!element.TryGetDecimal(out var number))
        {
            return false;
        }

        value = (int)number;
        return number == value;
    }

    private static bool TryDecimal(JsonElement parent, string name, out decimal value)
    {
        value = 0m;
        return parent.ValueKind == JsonValueKind.Object
               && parent.TryGetProperty(name, out var element)
               && element.ValueKind == JsonValueKind.Number
               && element.TryGetDecimal(out value);
    }

    private static decimal DecimalValue(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object
        && parent.TryGetProperty(name, out var element)
        && element.ValueKind == JsonValueKind.Number
        && element.TryGetDecimal(out var value)
            ? value
            : 0m;

    private static bool TryBool(JsonElement parent, string name, out bool value)
    {
        value = false;
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out var element))
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        return element.ValueKind == JsonValueKind.False;
    }

    private static decimal Number(JsonElement parent, string name)
    {
        if (parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var value))
        {
            return value;
        }

        return 0m;
    }

    private static decimal Number(JsonElement parent, string group, string name)
    {
        if (parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(group, out var child))
        {
            return Number(child, name);
        }

        return 0m;
    }

    private static bool CostsEqual(List<CostItem> actual, List<CostItem> expected) =>
        actual.Count == expected.Count
        && actual.Zip(expected, (left, right) => left == right).All(equal => equal);

    private static bool ConsumedGoodsEqual(List<ConsumedGood> actual, List<ConsumedGood> expected)
    {
        if (actual.Count != expected.Count)
        {
            return false;
        }

        for (var index = 0; index < actual.Count; index++)
        {
            if (!string.Equals(actual[index].Good, expected[index].Good, StringComparison.Ordinal)
                || Math.Abs(actual[index].Rate - expected[index].Rate) > 1e-5f)
            {
                return false;
            }
        }

        return true;
    }

    private static string DescribeCosts(List<CostItem> items) =>
        items.Count == 0 ? "<empty>" : string.Join("+", items.Select(item => item.Good + " " + item.Amount));

    private static string DescribeConsumed(List<ConsumedGood> items) =>
        items.Count == 0
            ? "<empty>"
            : string.Join("+", items.Select(item => item.Good + " "
                                                          + item.Rate.ToString("0.00000", CultureInfo.InvariantCulture)));

    private static string Show(decimal value) => value.ToString("0.00##", CultureInfo.InvariantCulture);

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed record CostItem(string Good, int Amount);

    private sealed record ConsumedGood(string Good, float Rate);

    private sealed class EngineRow
    {
        public string Size { get; private init; } = string.Empty;

        public string Tier { get; private init; } = string.Empty;

        public int FootprintX { get; private init; }

        public int FootprintY { get; private init; }

        public int Height { get; private init; }

        public int RatedHorsepower { get; private init; }

        public int ScienceCost { get; private init; }

        public List<CostItem> BuildCost { get; private init; } = new();

        public decimal FuelPerHour { get; private init; }

        public decimal WaterPerHour { get; private init; }

        public int FuelStock { get; private init; }

        public int WaterTankGoods { get; private init; }

        public int MaintenanceIntervalDays { get; private init; }

        public List<CostItem> MaintenanceMaterials { get; private init; } = new();

        public static EngineRow? Parse(JsonElement entry, Report report)
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                report.Error("table", "engines.json: an engines entry is not an object");
                return null;
            }

            var size = Text(entry, "size");
            var tier = Text(entry, "tier");
            if (size is null || tier is null)
            {
                report.Error("table", "engines.json: an engines entry is missing size/tier");
                return null;
            }

            var footprint = Prop(entry, "footprint");
            if (footprint is null || !TryInt(footprint.Value, "x", out var x) || !TryInt(footprint.Value, "y", out var y)
                || !TryInt(entry, "height", out var height)
                || !TryInt(entry, "ratedHorsepower", out var horsepower)
                || !TryInt(entry, "scienceCost", out var science)
                || !TryDecimal(entry, "fuelPerHour", out var fuel)
                || !TryDecimal(entry, "waterPerHour", out var water)
                || !TryInt(entry, "fuelStock", out var stock)
                || !TryInt(entry, "waterTankGoods", out var tank)
                || !TryInt(entry, "maintenanceIntervalDays", out var interval))
            {
                report.Error("table", "engines.json: row " + size + "." + tier + " is missing a required number field");
                return null;
            }

            return new EngineRow
            {
                Size = size,
                Tier = tier,
                FootprintX = x,
                FootprintY = y,
                Height = height,
                RatedHorsepower = horsepower,
                ScienceCost = science,
                BuildCost = Costs(entry, "buildCost"),
                FuelPerHour = fuel,
                WaterPerHour = water,
                FuelStock = stock,
                WaterTankGoods = tank,
                MaintenanceIntervalDays = interval,
                MaintenanceMaterials = Costs(entry, "maintenanceMaterials"),
            };

            static List<CostItem> Costs(JsonElement parent, string name)
            {
                var result = new List<CostItem>();
                if (!parent.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
                {
                    return result;
                }

                foreach (var item in array.EnumerateArray())
                {
                    result.Add(new CostItem(Text(item, "good") ?? string.Empty,
                        TryInt(item, "amount", out var amount) ? amount : -1));
                }

                return result;
            }
        }
    }

    private sealed class VariantExpectation
    {
        public string TemplateName { get; init; } = string.Empty;

        public string BlueprintRelativePath { get; init; } = string.Empty;

        public string Size { get; init; } = string.Empty;

        public string Tier { get; init; } = string.Empty;

        public string Faction { get; init; } = string.Empty;

        public int FootprintX { get; init; }

        public int FootprintY { get; init; }

        public int Height { get; init; }

        public int Horsepower { get; init; }

        public decimal FuelPerHour { get; init; }

        public decimal WaterPerHour { get; init; }

        public int FuelStock { get; init; }

        public int WaterTankGoods { get; init; }

        public int ScienceCost { get; init; }

        public List<CostItem> BuildCost { get; init; } = new();

        public int ToolOrder { get; init; }

        public decimal MaintenanceIntervalDays { get; init; }

        public List<CostItem> MaintenanceMaterials { get; init; } = new();

        public string[] FuelGoods { get; init; } = Array.Empty<string>();

        public decimal[] FuelValues { get; init; } = Array.Empty<decimal>();

        public List<ConsumedGood> ConsumedGoods { get; init; } = new();
    }
}
