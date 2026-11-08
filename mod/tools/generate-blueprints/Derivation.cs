using System.Globalization;

namespace SEE.Tools.GenerateBlueprints;

/// <summary>One declared fuel good and the per-hour rate the vanilla consumer derives its capacity from.</summary>
internal sealed record ConsumedRate(string GoodId, decimal GoodPerHour);

/// <summary>One (engine row, faction) expansion: everything a single blueprint file needs.</summary>
internal sealed record FactionVariant(
    EngineRow Engine,
    FactionDef Faction,
    int Horsepower,
    decimal FuelPerHour,
    decimal WaterPerHour,
    int FuelStock,
    int WaterTankGoods,
    IReadOnlyList<string> FuelGoodIds,
    IReadOnlyList<decimal> FuelValues,
    IReadOnlyList<ConsumedRate> ConsumedGoods,
    int ToolOrder,
    string TemplateName,
    string TemplatePath,
    string GeneratedRelativePath,
    string IconName);

/// <summary>Base row plus its two faction expansions.</summary>
internal sealed record DerivedRow(
    EngineRow Engine,
    decimal FuelTierRate,
    decimal WaterTierRate,
    bool FactionFuelCollapsed,
    IReadOnlyList<FactionVariant> Variants);

/// <summary>
/// Step 3 of the generator: expand every base row to Folktails and IronTeeth.
///
/// Contract rules (CONTRACT_SEE 4.2 / 11.3): horsepower is ceil(base x faction output); fuel is
/// round(base fuel x faction fuel, 2); water, tank, fuel stock, science cost, build cost and
/// maintenance stay identical for both factions. The vanilla fuel consumer only declares the
/// capacity derivation rate, not the real consumption rate (the SEE controller owns consumption):
/// T1/T2 use <c>(fuelStock - 0.5) / workHours</c> on Log, T3/T4 use
/// <c>(fuelStock / 2 - 0.5) / workHours</c> on both Log and Plank (AD-3 final formula).
/// </summary>
internal static class Derivation
{
    public static IReadOnlyList<DerivedRow> Expand(Catalog catalog)
    {
        var rows = new List<DerivedRow>(catalog.Engines.Count);
        foreach (var engine in catalog.Engines)
        {
            var variants = catalog.Factions.Select(faction => Expand(engine, faction, catalog)).ToList();
            var folktailsFuel = variants[0].FuelPerHour;
            var ironTeethFuel = variants[1].FuelPerHour;
            rows.Add(new DerivedRow(
                engine,
                catalog.TierFuelRate[engine.Tier],
                catalog.TierWaterRate[engine.Tier],
                folktailsFuel == ironTeethFuel,
                variants));
        }

        return rows;
    }

    private static FactionVariant Expand(EngineRow engine, FactionDef faction, Catalog catalog)
    {
        var horsepower = (int)decimal.Ceiling(engine.RatedHorsepower * faction.OutputMultiplier);
        var fuelPerHour = CatalogLoader.Round2(engine.FuelPerHour * faction.FuelMultiplier);
        var fuelGoodIds = engine.Tier is "T1" or "T2" ? new[] { "Log" } : new[] { "Log", "Plank" };
        var fuelValues = fuelGoodIds.Length == 1 ? new[] { 1.0m } : new[] { 1.0m, 2.0m };
        var consumed = BuildConsumedGoods(engine.FuelStock, engine.WaterTankGoods,
            catalog.FullInventoryWorkHours, engine.Tier);
        var templateName = "SteamBoiler." + engine.Size + "." + engine.Tier + "." + faction.Name;
        return new FactionVariant(
            engine,
            faction,
            horsepower,
            fuelPerHour,
            engine.WaterPerHour,
            engine.FuelStock,
            engine.WaterTankGoods,
            fuelGoodIds,
            fuelValues,
            consumed,
            catalog.ToolOrders[engine.Size + "." + engine.Tier],
            templateName,
            "Buildings/Power/SteamBoiler/" + templateName + ".blueprint",
            "data/Buildings/Power/SteamBoiler/" + templateName + ".blueprint.json",
            "SteamBoiler" + engine.Size + engine.Tier + faction.Name);
    }

    /// <summary>
    /// Builds the <c>GoodConsumingBuildingSpec</c> goods list: fuel goods only.
    ///
    /// History: v0.1.1 moved the water tank into this list as a hotfix for a
    /// <c>NoHaulingPostStatus</c> double-init crash (Stockpile + GoodConsumingBuilding on one
    /// building). That made the engine depend on a single combined inventory and gave up the
    /// dedicated tank. Bug fix 2026-11-08 restores a dedicated water tank as a plain public input
    /// <c>Inventory</c> created in code (<c>SEEWaterInventoryInitializer</c>), which has neither a
    /// second status initializer nor a second GoodConsuming declaration; the GoodConsuming list is
    /// back to fuel only. Haulers fill the dedicated tank through the normal fill-input chain.
    /// </summary>
    private static IReadOnlyList<ConsumedRate> BuildConsumedGoods(
        int fuelStock, int waterTankGoods, int workHours, string tier)
    {
        var rates = new List<ConsumedRate>(3);
        if (tier is "T1" or "T2")
        {
            rates.Add(new ConsumedRate("Log", (fuelStock - 0.5m) / workHours));
        }
        else
        {
            var perItem = (fuelStock / 2m - 0.5m) / workHours;
            rates.Add(new ConsumedRate("Log", perItem));
            rates.Add(new ConsumedRate("Plank", perItem));
        }

        // Bug fix 2026-11-08: Water is deliberately NOT a GoodConsuming good any more. It has its
        // own public input inventory (SEEWaterInventoryInitializer) so ordinary haulers can deliver
        // it and the tank capacity never shares the fuel inventory. The waterTankGoods parameter is
        // kept (and asserted) so the tabulated size stays the single source of truth.
        if (waterTankGoods <= 0)
        {
            throw new GeneratorException("waterTankGoods must be positive for " + tier);
        }

        return rates;
    }

    /// <summary>The dataVersion-ordered list of blueprint variants (tier-major, then row order).</summary>
    public static IReadOnlyList<FactionVariant> Variants(IReadOnlyList<DerivedRow> rows) =>
        rows.SelectMany(row => row.Variants).ToList();

    /// <summary>Formats a decimal exactly like the generated JSON does (used by diagnostics and the derived table).</summary>
    public static string Text(decimal value) =>
        value.ToString("0.############################", CultureInfo.InvariantCulture);
}

