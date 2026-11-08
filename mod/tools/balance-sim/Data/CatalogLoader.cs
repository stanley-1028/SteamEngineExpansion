using System.Security.Cryptography;
using System.Text.Json;
using SEE.Core.Abstractions;
using SEE.Core.Formulas;

namespace SEE.Tools.BalanceSim.Data;

/// <summary>
/// Loads and cross-checks <c>data/engines.json</c> and <c>data/engines.derived.json</c>
/// (requirements §4.1, §11.3; PLAN §3 SEE-P7). The loader is read-only: a mismatch is reported
/// as a hard failure instead of silently "fixing" data, because P7 must never change balance
/// values by itself (the value source stays owned by the data card).
/// </summary>
internal static class CatalogLoader
{
    private static readonly BoilerSize[] Sizes = { BoilerSize.Small, BoilerSize.Medium, BoilerSize.Large };
    private static readonly BoilerTier[] Tiers = { BoilerTier.T1, BoilerTier.T2, BoilerTier.T3, BoilerTier.T4 };

    /// <summary>Loads both files from the mod root and runs every structural cross-check.</summary>
    public static LoadedData Load(string modRoot)
    {
        string enginesPath = Path.Combine(modRoot, "data", "engines.json");
        string derivedPath = Path.Combine(modRoot, "data", "engines.derived.json");
        if (!File.Exists(enginesPath))
        {
            throw new BalanceSimException("missing " + enginesPath);
        }

        if (!File.Exists(derivedPath))
        {
            throw new BalanceSimException("missing " + derivedPath + " (run tools/generate-blueprints first)");
        }

        byte[] enginesBytes = File.ReadAllBytes(enginesPath);
        string sha256 = Convert.ToHexString(SHA256.HashData(enginesBytes)).ToLowerInvariant();
        EngineCatalog catalog = ParseCatalog(ReadDocument(enginesBytes, enginesPath), sha256);
        DerivedCatalog derived = ParseDerived(ReadDocument(File.ReadAllBytes(derivedPath), derivedPath));

        var checks = new CrossCheckBag();
        ValidateCatalog(catalog, checks);
        ValidateDerived(catalog, derived, sha256, checks);
        if (checks.Errors.Count > 0)
        {
            throw new BalanceSimException("data cross-check failed ("
                                          + checks.Errors.Count + " problem(s)):"
                                          + Environment.NewLine + "  - "
                                          + string.Join(Environment.NewLine + "  - ", checks.Errors));
        }

        return new LoadedData
        {
            Catalog = catalog,
            Derived = derived,
            EnginesSha256 = sha256,
            CrossCheckCount = checks.Passed,
        };
    }

    private static JsonElement ReadDocument(byte[] bytes, string path)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(bytes);
            return document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            throw new BalanceSimException("invalid JSON in " + path + ": " + exception.Message);
        }
    }

    private static EngineCatalog ParseCatalog(JsonElement root, string sha256)
    {
        RequireObject(root, "engines.json");
        var fuelRates = new decimal[4];
        var waterRates = new decimal[4];
        JsonElement fuelNode = Prop(root, "tierFuelRatePer100Hp", "engines.json");
        JsonElement waterNode = Prop(root, "tierWaterRatePer100Hp", "engines.json");
        for (int index = 0; index < Tiers.Length; index++)
        {
            string key = Tiers[index].ToString();
            fuelRates[index] = Decimal(fuelNode, key, "tierFuelRatePer100Hp");
            waterRates[index] = Decimal(waterNode, key, "tierWaterRatePer100Hp");
        }

        JsonElement factions = Prop(root, "factions", "engines.json");
        JsonElement enginesNode = Array(root, "engines", "engines.json");
        var engines = new List<EngineRow>(enginesNode.GetArrayLength());
        foreach (JsonElement entry in enginesNode.EnumerateArray())
        {
            engines.Add(ParseEngine(entry, engines.Count));
        }

        return new EngineCatalog
        {
            DataVersion = Int(root, "dataVersion", "engines.json"),
            FullInventoryWorkHours = Int(root, "fullInventoryWorkHours", "engines.json"),
            WaterTankHours = Int(root, "waterTankHours", "engines.json"),
            TierFuelPer100Hp = fuelRates,
            TierWaterPer100Hp = waterRates,
            Folktails = ParseFaction(factions, "Folktails"),
            IronTeeth = ParseFaction(factions, "IronTeeth"),
            Engines = engines,
        };
    }

    private static FactionRates ParseFaction(JsonElement factions, string name)
    {
        JsonElement node = Prop(factions, name, "factions");
        return new FactionRates
        {
            Name = name,
            OutputMultiplier = Double(node, "output", "factions." + name),
            FuelMultiplier = Double(node, "fuel", "factions." + name),
        };
    }

    private static EngineRow ParseEngine(JsonElement entry, int index)
    {
        string context = "engines[" + index + "]";
        JsonElement footprint = Prop(entry, "footprint", context);
        return new EngineRow
        {
            Size = ParseSize(Text(entry, "size", context)),
            Tier = ParseTier(Text(entry, "tier", context)),
            FootprintX = Int(footprint, "x", context + ".footprint"),
            FootprintY = Int(footprint, "y", context + ".footprint"),
            Height = Int(entry, "height", context),
            RatedHorsepower = Int(entry, "ratedHorsepower", context),
            ScienceCost = Int(entry, "scienceCost", context),
            BuildCost = ParseGoods(Array(entry, "buildCost", context), context + ".buildCost"),
            FuelPerHourDecimal = Decimal(entry, "fuelPerHour", context),
            WaterPerHourDecimal = Decimal(entry, "waterPerHour", context),
            FuelStock = Int(entry, "fuelStock", context),
            WaterTankGoods = Int(entry, "waterTankGoods", context),
            FullInventoryWorkHours = Int(entry, "fullInventoryWorkHours", context),
            MaintenanceIntervalDays = Int(entry, "maintenanceIntervalDays", context),
            MaintenanceMaterials = ParseGoods(
                Array(entry, "maintenanceMaterials", context), context + ".maintenanceMaterials"),
        };
    }

    private static List<GoodAmount> ParseGoods(JsonElement array, string context)
    {
        var result = new List<GoodAmount>(array.GetArrayLength());
        foreach (JsonElement item in array.EnumerateArray())
        {
            result.Add(new GoodAmount
            {
                Good = Text(item, "good", context),
                Amount = Int(item, "amount", context),
            });
        }

        return result;
    }

    private static DerivedCatalog ParseDerived(JsonElement root)
    {
        RequireObject(root, "engines.derived.json");
        JsonElement authoritative = Prop(root, "authoritativeFields", "engines.derived.json");
        JsonElement rowsNode = Array(root, "rows", "engines.derived.json");
        var rows = new List<DerivedRow>(rowsNode.GetArrayLength());
        foreach (JsonElement row in rowsNode.EnumerateArray())
        {
            rows.Add(ParseDerivedRow(row));
        }

        var gates = new List<DerivedGate>();
        foreach (JsonElement gate in Array(root, "gates", "engines.derived.json").EnumerateArray())
        {
            gates.Add(new DerivedGate
            {
                Id = Int(gate, "id", "gates[]"),
                Name = Text(gate, "name", "gates[]"),
                Pass = Bool(gate, "pass", "gates[]"),
                Detail = Text(gate, "detail", "gates[]"),
            });
        }

        return new DerivedCatalog
        {
            SourceSha256 = Text(root, "sourceSha256", "engines.derived.json"),
            BlueprintCount = Int(root, "blueprintCount", "engines.derived.json"),
            FieldsPerFile = Int(authoritative, "fieldsPerFile", "authoritativeFields"),
            Rows = rows,
            Gates = gates,
        };
    }

    private static DerivedRow ParseDerivedRow(JsonElement row)
    {
        JsonElement variants = Prop(row, "variants", "rows[]");
        var parsed = new Dictionary<BoilerFaction, FactionVariant>();
        foreach (BoilerFaction faction in new[] { BoilerFaction.Folktails, BoilerFaction.IronTeeth })
        {
            parsed[faction] = ParseVariant(Prop(variants, faction.ToString(), "rows[].variants"), faction);
        }

        return new DerivedRow
        {
            Size = ParseSize(Text(row, "size", "rows[]")),
            Tier = ParseTier(Text(row, "tier", "rows[]")),
            RatedHorsepower = Int(row, "ratedHorsepower", "rows[]"),
            Variants = parsed,
        };
    }

    private static FactionVariant ParseVariant(JsonElement node, BoilerFaction faction)
    {
        string context = "variants." + faction;
        return new FactionVariant
        {
            Faction = faction,
            Horsepower = Int(node, "horsepower", context),
            FuelPerHourDecimal = Decimal(node, "fuelPerHour", context),
            WaterPerHourDecimal = Decimal(node, "waterPerHour", context),
            FuelStock = Int(node, "fuelStock", context),
            WaterTankGoods = Int(node, "waterTankGoods", context),
            TemplateName = Text(node, "templateName", context),
        };
    }

    // --------------------------------------------------------------------------------------
    // Cross-checks
    // --------------------------------------------------------------------------------------

    private static void ValidateCatalog(EngineCatalog catalog, CrossCheckBag checks)
    {
        checks.Check(catalog.DataVersion == 1, "dataVersion is 1");
        checks.Check(catalog.FullInventoryWorkHours == 25, "fullInventoryWorkHours is 25");
        checks.Check(catalog.WaterTankHours == 200, "waterTankHours is 200");
        for (int tier = 0; tier < 4; tier++)
        {
            checks.Check(catalog.TierFuelPer100Hp[tier] == References.ExpectedFuelRatesPer100Hp[tier],
                "tier fuel rate T" + (tier + 1) + " matches §5.4");
            checks.Check(catalog.TierWaterPer100Hp[tier] == References.ExpectedWaterRatesPer100Hp[tier],
                "tier water rate T" + (tier + 1) + " matches §5.4");
        }

        checks.Check(catalog.Folktails.OutputMultiplier == 1.00 && catalog.Folktails.FuelMultiplier == 0.90,
            "Folktails multipliers are output 1.00 / fuel 0.90");
        checks.Check(catalog.IronTeeth.OutputMultiplier == 1.05 && catalog.IronTeeth.FuelMultiplier == 1.00,
            "Iron Teeth multipliers are output 1.05 / fuel 1.00");
        checks.Check(catalog.Engines.Count == 12, "engines.json has exactly 12 rows");

        var seen = new bool[3, 4];
        foreach (EngineRow row in catalog.Engines)
        {
            int size = (int)row.Size;
            int tier = (int)row.Tier;
            if (size is < 0 or > 2 || tier is < 0 or > 3)
            {
                checks.Fail(row.Id + ": undefined size/tier enum value");
                continue;
            }

            if (seen[size, tier])
            {
                checks.Fail(row.Id + ": duplicate (size, tier) row");
            }

            seen[size, tier] = true;
            ValidateRow(row, catalog, checks);
        }

        for (int size = 0; size < 3; size++)
        {
            for (int tier = 0; tier < 4; tier++)
            {
                if (!seen[size, tier])
                {
                    checks.Fail("missing row for " + Sizes[size] + "." + Tiers[tier]);
                }
            }
        }
    }

    private static void ValidateRow(EngineRow row, EngineCatalog catalog, CrossCheckBag checks)
    {
        string id = row.Id;
        int tier = (int)row.Tier;
        checks.Check(row.FootprintX == FootprintSide(row.Size) && row.FootprintY == FootprintSide(row.Size),
            id + ": footprint is " + FootprintSide(row.Size) + "x" + FootprintSide(row.Size));
        checks.Check(row.Height == (row.Size == BoilerSize.Large ? 4 : 3), id + ": height matches §4.1");
        checks.Check(row.RatedHorsepower > 0, id + ": rated horsepower is positive");
        checks.Check(row.ScienceCost > 0, id + ": science cost is positive");
        checks.Check(row.FuelPerHourDecimal > 0m && row.WaterPerHourDecimal > 0m,
            id + ": fuel and water rates are positive");
        checks.Check(row.MaintenanceIntervalDays == References.ExpectedMaintenanceIntervals[tier],
            id + ": maintenance interval is " + References.ExpectedMaintenanceIntervals[tier] + " days");

        decimal expectedFuel = Math.Round(
            catalog.TierFuelPer100Hp[tier] * row.RatedHorsepower / 100m, 2, MidpointRounding.AwayFromZero);
        decimal expectedWater = Math.Round(
            catalog.TierWaterPer100Hp[tier] * row.RatedHorsepower / 100m, 2, MidpointRounding.AwayFromZero);
        checks.Check(row.FuelPerHourDecimal == expectedFuel,
            id + ": fuel rate " + row.FuelPerHourDecimal + " equals round(" + catalog.TierFuelPer100Hp[tier]
            + " x " + row.RatedHorsepower + "/100, 2) = " + expectedFuel);
        checks.Check(row.WaterPerHourDecimal == expectedWater,
            id + ": water rate " + row.WaterPerHourDecimal + " equals round(" + catalog.TierWaterPer100Hp[tier]
            + " x " + row.RatedHorsepower + "/100, 2) = " + expectedWater);

        int fuelStock = BoilerFormulas.FuelStockGoods((float)row.FuelPerHour, (float)catalog.FullInventoryWorkHours);
        int waterTank = BoilerFormulas.WaterTankGoods((float)row.WaterPerHour, (float)catalog.WaterTankHours);
        checks.Check(fuelStock == row.FuelStock,
            id + ": FuelStockGoods = " + fuelStock + " equals the declared " + row.FuelStock);
        checks.Check(waterTank == row.WaterTankGoods,
            id + ": WaterTankGoods = " + waterTank + " equals the declared " + row.WaterTankGoods);
    }

    private static void ValidateDerived(
        EngineCatalog catalog, DerivedCatalog derived, string expectedSha, CrossCheckBag checks)
    {
        checks.Check(string.Equals(derived.SourceSha256, expectedSha, StringComparison.OrdinalIgnoreCase),
            "derived sourceSha256 equals the SHA-256 of engines.json");
        checks.Check(derived.BlueprintCount == 24, "derived blueprintCount is 24");
        checks.Check(derived.FieldsPerFile == 12, "derived authoritative fields per file is 12");
        checks.Check(derived.Rows.Count == 12, "derived has exactly 12 rows");
        checks.Check(derived.AllGatesPass, "all five P1 gates report PASS");

        foreach (EngineRow engine in catalog.Engines)
        {
            DerivedRow? derivedRow = derived.Find(engine.Size, engine.Tier);
            if (derivedRow is null)
            {
                checks.Fail("derived row missing for " + engine.Id);
                continue;
            }

            checks.Check(derivedRow.RatedHorsepower == engine.RatedHorsepower,
                engine.Id + ": derived base horsepower equals engines.json");
            ValidateVariant(engine, derivedRow, BoilerFaction.Folktails, catalog.Folktails, checks);
            ValidateVariant(engine, derivedRow, BoilerFaction.IronTeeth, catalog.IronTeeth, checks);
        }

        var combos = new HashSet<string>(StringComparer.Ordinal);
        foreach (DerivedRow row in derived.Rows)
        {
            combos.Add(row.Id);
        }

        checks.Check(combos.Count == 12, "derived rows are 12 unique size/tier combinations");
    }

    private static void ValidateVariant(
        EngineRow engine, DerivedRow derivedRow, BoilerFaction faction, FactionRates rates, CrossCheckBag checks)
    {
        string id = engine.Id + "." + faction;
        if (!derivedRow.Variants.TryGetValue(faction, out FactionVariant? variant))
        {
            checks.Fail(id + ": variant missing in engines.derived.json");
            return;
        }

        int horsepower = BoilerFormulas.FactionOutputHorsepower(engine.RatedHorsepower, (float)rates.OutputMultiplier);
        float fuel = BoilerFormulas.FactionFuelPerHour((float)engine.FuelPerHour, (float)rates.FuelMultiplier);
        checks.Check(variant.Horsepower == horsepower,
            id + ": derived horsepower " + variant.Horsepower + " equals " + horsepower);
        checks.Check(Math.Abs((double)variant.FuelPerHourDecimal - fuel) <= 1e-6,
            id + ": derived fuel " + variant.FuelPerHourDecimal + " equals " + fuel);
        checks.Check(variant.WaterPerHourDecimal == engine.WaterPerHourDecimal,
            id + ": derived water equals the base water rate");
        checks.Check(variant.FuelStock == engine.FuelStock, id + ": derived fuelStock equals the base value (D-6)");
        checks.Check(variant.WaterTankGoods == engine.WaterTankGoods,
            id + ": derived waterTankGoods equals the base value");
        checks.Check(variant.TemplateName == "SteamBoiler." + engine.Size + "." + engine.Tier + "." + faction,
            id + ": TemplateName follows the §2 naming contract");
    }

    private static int FootprintSide(BoilerSize size) => size switch
    {
        BoilerSize.Small => 2,
        BoilerSize.Medium => 3,
        _ => 4,
    };

    // --------------------------------------------------------------------------------------
    // JSON helpers
    // --------------------------------------------------------------------------------------

    private static void RequireObject(JsonElement element, string context)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new BalanceSimException(context + ": expected a JSON object");
        }
    }

    private static JsonElement Prop(JsonElement parent, string name, string context)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out JsonElement value))
        {
            throw new BalanceSimException(context + ": missing property '" + name + "'");
        }

        return value;
    }

    private static JsonElement Array(JsonElement parent, string name, string context)
    {
        JsonElement value = Prop(parent, name, context);
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new BalanceSimException(context + ": property '" + name + "' is not an array");
        }

        return value;
    }

    private static string Text(JsonElement parent, string name, string context)
    {
        JsonElement value = Prop(parent, name, context);
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new BalanceSimException(context + ": property '" + name + "' is not a string");
        }

        return value.GetString() ?? string.Empty;
    }

    private static int Int(JsonElement parent, string name, string context)
    {
        JsonElement value = Prop(parent, name, context);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int result))
        {
            throw new BalanceSimException(context + ": property '" + name + "' is not an int");
        }

        return result;
    }

    private static double Double(JsonElement parent, string name, string context)
    {
        JsonElement value = Prop(parent, name, context);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double result))
        {
            throw new BalanceSimException(context + ": property '" + name + "' is not a number");
        }

        return result;
    }

    private static decimal Decimal(JsonElement parent, string name, string context)
    {
        JsonElement value = Prop(parent, name, context);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out decimal result))
        {
            throw new BalanceSimException(context + ": property '" + name + "' is not a decimal number");
        }

        return result;
    }

    private static bool Bool(JsonElement parent, string name, string context)
    {
        JsonElement value = Prop(parent, name, context);
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new BalanceSimException(context + ": property '" + name + "' is not a bool"),
        };
    }

    private static BoilerSize ParseSize(string text) => text switch
    {
        "Small" => BoilerSize.Small,
        "Medium" => BoilerSize.Medium,
        "Large" => BoilerSize.Large,
        _ => throw new BalanceSimException("unknown size '" + text + "'"),
    };

    private static BoilerTier ParseTier(string text) => text switch
    {
        "T1" => BoilerTier.T1,
        "T2" => BoilerTier.T2,
        "T3" => BoilerTier.T3,
        "T4" => BoilerTier.T4,
        _ => throw new BalanceSimException("unknown tier '" + text + "'"),
    };

    /// <summary>Collects data cross-check results so one run lists every problem, not just the first.</summary>
    private sealed class CrossCheckBag
    {
        public List<string> Errors { get; } = new();

        public int Passed { get; private set; }

        public void Check(bool condition, string description)
        {
            if (condition)
            {
                Passed++;
            }
            else
            {
                Errors.Add(description);
            }
        }

        public void Fail(string description) => Errors.Add(description);
    }
}
