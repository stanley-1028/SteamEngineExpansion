using System.Globalization;

namespace SEE.Tools.GenerateBlueprints;

/// <summary>One of the five frozen generator gates (requirements 11.3 step 5 / CONTRACT_SEE 4.10).</summary>
internal sealed record GateResult(int Id, string Name, bool Pass, string Detail);

/// <summary>
/// Step 5 of the generator: the five gates. Any failure aborts the run before a file is written
/// ("no half-generated output").
/// </summary>
internal static class Gates
{
    /// <summary>Vanilla SteamEngine efficiency baseline per 100 horsepower (requirements 5.2 / 8).</summary>
    public const decimal VanillaFuelPer100Hp = 0.025m;

    /// <summary>Vanilla SteamEngine water baseline per 100 horsepower.</summary>
    public const decimal VanillaWaterPer100Hp = 0.075m;

    private static readonly string[] ForbiddenSpecTokens =
    {
        "BuildingPipeSpec",
        "PowerLineSpec",
        "WorkplaceSpec",
        "GoodPoweredGeneratorSpec",
        "PoweredGoodConsumingBuildingSpec",
    };

    /// <summary>Runs all five gates and throws when one of them does not pass.</summary>
    public static IReadOnlyList<GateResult> Run(
        Catalog catalog,
        IReadOnlyList<DerivedRow> rows,
        IReadOnlyDictionary<string, string> blueprintTexts)
    {
        var results = new List<GateResult>
        {
            TankToFuelRatio(rows),
            WaterTankSeparation(rows),
            EfficiencyCriterion(catalog),
            CapacityTable(rows),
            ForbiddenSpecs(blueprintTexts),
        };

        var failed = results.Where(result => !result.Pass)
            .Select(result => "gate " + result.Id + " (" + result.Name + "): " + result.Detail)
            .ToList();
        if (failed.Count > 0)
        {
            throw new GeneratorException("gate failure:" + Environment.NewLine + string.Join(Environment.NewLine, failed));
        }

        return results;
    }

    private static GateResult TankToFuelRatio(IReadOnlyList<DerivedRow> rows)
    {
        var minimum = decimal.MaxValue;
        var minimumLabel = string.Empty;
        foreach (var row in rows)
        {
            var ratio = (decimal)row.Engine.WaterTankGoods / row.Engine.FuelStock;
            if (ratio < minimum)
            {
                minimum = ratio;
                minimumLabel = row.Engine.Size + "." + row.Engine.Tier;
            }
        }

        var pass = minimum >= 8m;
        return new GateResult(1, "water tank / fuel stock >= 8x", pass,
            "min " + Show(minimum) + " (" + minimumLabel + ")");
    }

    /// <summary>
    /// Gate 2 (repurposed by the 2026-11-08 hauling fix): the water tank must be a dedicated
    /// inventory, never a second GoodConsuming good. Before the fix the tank shared the fuel
    /// inventory and the gate measured the "mixing volume error" of that shared capacity; with a
    /// separate public input inventory the only invariants left are (a) no Water in ConsumedGoods
    /// and (b) the tank size carried by the boiler spec equals the table value.
    /// </summary>
    private static GateResult WaterTankSeparation(IReadOnlyList<DerivedRow> rows)
    {
        var failures = new List<string>();
        foreach (var row in rows)
        {
            foreach (var variant in row.Variants)
            {
                if (variant.WaterTankGoods != row.Engine.WaterTankGoods)
                {
                    failures.Add(variant.TemplateName + ": water tank differs from the table");
                }

                foreach (var rate in variant.ConsumedGoods)
                {
                    if (string.Equals(rate.GoodId, "Water", StringComparison.Ordinal))
                    {
                        failures.Add(variant.TemplateName + ": Water must not be a ConsumedGood");
                    }
                }
            }
        }

        return new GateResult(2, "dedicated water tank (no Water in ConsumedGoods, table tank size)", failures.Count == 0,
            "12 rows x 2 factions" + (failures.Count == 0 ? string.Empty : "; " + string.Join("; ", failures)));
    }

    private static GateResult EfficiencyCriterion(Catalog catalog)
    {
        var failures = new List<string>();
        var details = new List<string>();
        foreach (var tier in new[] { "T2", "T3", "T4" })
        {
            var fuel = catalog.TierFuelRate[tier];
            var water = catalog.TierWaterRate[tier];
            details.Add(tier + " fuel " + Show(fuel) + "/" + Show(VanillaFuelPer100Hp)
                        + " water " + Show(water) + "/" + Show(VanillaWaterPer100Hp));
            if (fuel > VanillaFuelPer100Hp)
            {
                failures.Add(tier + " fuel " + Show(fuel) + " > " + Show(VanillaFuelPer100Hp));
            }

            if (water > VanillaWaterPer100Hp)
            {
                failures.Add(tier + " water " + Show(water) + " > " + Show(VanillaWaterPer100Hp));
            }
        }

        var tierOneLimit = catalog.TierFuelRate["T2"] * 1.3m;
        details.Add("T1 fuel " + Show(catalog.TierFuelRate["T1"]) + " <= 1.3 x T2 " + Show(tierOneLimit));
        if (catalog.TierFuelRate["T1"] > tierOneLimit)
        {
            failures.Add("T1 fuel " + Show(catalog.TierFuelRate["T1"]) + " > 1.3 x T2 " + Show(tierOneLimit));
        }

        return new GateResult(3, "efficiency criterion (T2-T4 <= vanilla, T1 <= 1.3 x T2)",
            failures.Count == 0,
            failures.Count == 0 ? string.Join(", ", details) : string.Join("; ", failures));
    }

    private static GateResult CapacityTable(IReadOnlyList<DerivedRow> rows)
    {
        var failures = new List<string>();
        foreach (var row in rows)
        {
            foreach (var variant in row.Variants)
            {
                if (variant.FuelStock != row.Engine.FuelStock || variant.WaterTankGoods != row.Engine.WaterTankGoods)
                {
                    failures.Add(variant.TemplateName + ": capacity differs from the table");
                }

                // Simulate the vanilla float32 CeilToInt capacity derivation (fuel goods only: the
                // dedicated water tank is an inventory created in code, not a consumed good).
                var derived = 0;
                foreach (var rate in variant.ConsumedGoods)
                {
                    derived += (int)MathF.Ceiling((float)rate.GoodPerHour * (float)row.Engine.FullInventoryWorkHours);
                }

                if (derived != row.Engine.FuelStock)
                {
                    failures.Add(variant.TemplateName + ": GoodConsumingBuildingSpec derives " + derived
                                 + " fuel goods, table says " + row.Engine.FuelStock);
                }
            }
        }

        return new GateResult(4, "capacity equals the table cell by cell", failures.Count == 0,
            "12 rows x 2 factions, float32 ceil derivation verified"
            + (failures.Count == 0 ? string.Empty : "; " + string.Join("; ", failures)));
    }

    private static GateResult ForbiddenSpecs(IReadOnlyDictionary<string, string> blueprintTexts)
    {
        var hits = new List<string>();
        foreach (var pair in blueprintTexts)
        {
            foreach (var token in ForbiddenSpecTokens)
            {
                if (pair.Value.Contains(token, StringComparison.Ordinal))
                {
                    hits.Add(Path.GetFileName(pair.Key) + " contains " + token);
                }
            }
        }

        return new GateResult(5, "no BuildingPipeSpec / PowerLineSpec / WorkplaceSpec", hits.Count == 0,
            "24 blueprints scanned" + (hits.Count == 0 ? string.Empty : "; " + string.Join("; ", hits)));
    }

    private static string Show(decimal value) => value.ToString("0.#####", CultureInfo.InvariantCulture);

    private static string Percent(decimal value) =>
        (value * 100m).ToString("0.0", CultureInfo.InvariantCulture) + "%";
}
