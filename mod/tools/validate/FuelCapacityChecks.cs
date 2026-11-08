using System.Globalization;
using System.Text.Json;

namespace SEE.Tools.Validate;

/// <summary>
/// Cross-checks the shipped blueprint(s) against <c>data/engines.json</c> where the requirements
/// pin a capacity rather than a rate (Lead ruling D-3 / AD-3):
///
/// <list type="bullet">
/// <item>fuel capacity must be the value the vanilla <c>GoodConsumingBuildingSpec</c> derives:
/// single-fuel tiers <c>ceil(GoodPerHour x FullInventoryWorkHours)</c>, dual-fuel tiers
/// <c>sum(ceil(GoodPerHour_i x FullInventoryWorkHours))</c>, and it must equal the tabulated
/// <c>fuelStock</c> for that size/tier;</item>
/// <item>the SEE boiler spec must declare the tabulated <c>fuelStock</c> capacity, and the
/// dedicated water inventory's capacity must come from the tabulated <c>waterTankGoods</c> value
/// (bug fix 2026-11-08: the tank is a code-created public input inventory, not a blueprint
/// <c>StockpileSpec</c> and not a GoodConsuming good).</item>
/// </list>
///
/// The fuel rate itself is deliberately NOT compared with <c>fuelPerHour</c>: P3 drives
/// proportional consumption from the controller (the vanilla consumer would permanently pause),
/// so the blueprint only has to produce the tabulated capacity.
/// </summary>
internal sealed class FuelCapacityChecks
{
    private sealed record EngineRow(int FuelStock, int WaterTankGoods);

    private readonly string _dataDir;
    private readonly Report _report;
    private readonly Dictionary<string, EngineRow> _engines = new(StringComparer.Ordinal);

    public FuelCapacityChecks(string dataDir, Report report)
    {
        _dataDir = dataDir;
        _report = report;
    }

    public void Validate()
    {
        if (!LoadEngines())
        {
            return;
        }

        var blueprints = Directory
            .GetFiles(_dataDir, "*.blueprint.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var file in blueprints)
        {
            ValidateBlueprint(file, Path.GetRelativePath(_dataDir, file).Replace('\\', '/'));
        }
    }

    private bool LoadEngines()
    {
        var path = Path.Combine(_dataDir, "engines.json");
        if (!File.Exists(path))
        {
            // EnginesChecks already reports the missing file; do not pile a second error on top.
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("engines", out var engines)
                || engines.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var entry in engines.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var size = String(entry, "size");
                var tier = String(entry, "tier");
                if (size is null || tier is null)
                {
                    continue;
                }

                if (!entry.TryGetProperty("fuelStock", out var stock) || !stock.TryGetInt32(out var fuelStock)
                    || !entry.TryGetProperty("waterTankGoods", out var tank) || !tank.TryGetInt32(out var waterTank))
                {
                    continue;
                }

                _engines[size + "." + tier] = new EngineRow(fuelStock, waterTank);
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return _engines.Count > 0;
    }

    private void ValidateBlueprint(string path, string relative)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path));
        }
        catch (JsonException ex)
        {
            _report.Error("blueprint", relative + ": invalid JSON: " + ex.Message);
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            // Only engine blueprints carry the fuel consumer. Template collections and other
            // data documents also end in .blueprint.json and are validated by the generic
            // blueprint pass instead.
            if (TryObject(root, "GoodConsumingBuildingSpec") is null
                && TryObject(root, ContractSpecs.BoilerKey) is null)
            {
                return;
            }

            var size = String(root, "TemplateSpec") is not null && root.TryGetProperty("TemplateSpec", out var template)
                       && template.ValueKind == JsonValueKind.Object
                ? String(template, "TemplateName")
                : null;
            var (sizeFromName, tierFromName) = ParseTemplateName(size);

            // The SEE spec is the authoritative identity carrier; TemplateName is the fallback.
            var boiler = TryObject(root, ContractSpecs.BoilerKey);
            var specSize = boiler is null ? null : String(boiler.Value, "Size");
            var specTier = boiler is null ? null : String(boiler.Value, "Tier");
            var rowKey = (specSize ?? sizeFromName) is { } s && (specTier ?? tierFromName) is { } t ? s + "." + t : null;
            if (rowKey is null || !_engines.TryGetValue(rowKey, out var engine))
            {
                _report.Error("blueprint", relative + ": cannot match the blueprint to an engines.json row "
                                            + "(need " + ContractSpecs.BoilerKey + ".Size/Tier or a "
                                            + "TemplateName like SteamBoiler.<Size>.<Tier>.<Faction>)");
                return;
            }

            ValidateFuelCapacity(relative, root, engine);
            ValidateWaterCapacity(relative, root, engine);
        }
    }

    private void ValidateFuelCapacity(string relative, JsonElement root, EngineRow engine)
    {
        var consumer = TryObject(root, "GoodConsumingBuildingSpec");
        if (consumer is null)
        {
            _report.Error("blueprint", relative + ": missing GoodConsumingBuildingSpec (the fuel inventory declaration)");
            return;
        }

        if (!consumer.Value.TryGetProperty("FullInventoryWorkHours", out var hoursElement)
            || hoursElement.ValueKind != JsonValueKind.Number
            || !hoursElement.TryGetDecimal(out var workHours))
        {
            _report.Error("blueprint", relative + ": GoodConsumingBuildingSpec.FullInventoryWorkHours must be a number");
            return;
        }

        if (!consumer.Value.TryGetProperty("ConsumedGoods", out var consumed)
            || consumed.ValueKind != JsonValueKind.Array)
        {
            _report.Error("blueprint", relative + ": GoodConsumingBuildingSpec.ConsumedGoods must be an array");
            return;
        }

        var rates = new List<decimal>();
        foreach (var item in consumed.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (item.TryGetProperty("GoodPerHour", out var rateElement) && rateElement.ValueKind == JsonValueKind.Number
                && rateElement.TryGetDecimal(out var rate))
            {
                rates.Add(rate);
            }
        }

        if (rates.Count == 0)
        {
            _report.Error("blueprint", relative + ": GoodConsumingBuildingSpec.ConsumedGoods has no GoodPerHour entries");
            return;
        }

        var derived = 0m;
        foreach (var item in consumed.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            // Fuel only: the dedicated water tank (2026-11-08 fix) is not a GoodConsuming good, and
            // ValidateWaterCapacity rejects a Water entry here.
            if (item.TryGetProperty("GoodPerHour", out var rateElement) && rateElement.ValueKind == JsonValueKind.Number
                && rateElement.TryGetDecimal(out var rate))
            {
                derived += Math.Ceiling(rate * workHours);
            }
        }

        if (derived != engine.FuelStock)
        {
            _report.Error("blueprint", relative + ": GoodConsumingBuildingSpec derives fuel capacity "
                                        + Show(derived) + " (sum of ceil(GoodPerHour x FullInventoryWorkHours)); the "
                                        + "engines.json fuelStock is " + engine.FuelStock);
        }

        var boiler = TryObject(root, ContractSpecs.BoilerKey);
        if (boiler is not null && boiler.Value.TryGetProperty("FuelCapacityGoods", out var capacity))
        {
            if (capacity.ValueKind != JsonValueKind.Number || !capacity.TryGetInt32(out var declared)
                || declared != engine.FuelStock)
            {
                _report.Error("blueprint", relative + ": SEE." + ContractSpecs.BoilerKey + ".FuelCapacityGoods must be "
                                            + engine.FuelStock + " (engines.json)");
            }
        }
    }

    /// <summary>
    /// Bug fix 2026-11-08: the water tank is a dedicated public input <c>Inventory</c> created in
    /// code by <c>SEEWaterInventoryInitializer</c>. The blueprint therefore must NOT declare a
    /// <c>StockpileSpec</c> (that created a second <c>NoHaulingPostStatus</c> initializer and
    /// crashed the construction finish) and must NOT list Water as a GoodConsuming good (that would
    /// put the tank back into the shared fuel inventory). The tank size is carried by the boiler
    /// spec and must equal the tabulated <c>waterTankGoods</c>.
    /// </summary>
    private void ValidateWaterCapacity(string relative, JsonElement root, EngineRow engine)
    {
        if (root.TryGetProperty("StockpileSpec", out _))
        {
            _report.Error("blueprint", relative + ": StockpileSpec is forbidden on SEE boilers "
                                        + "(double NoHaulingPostStatus initializer crash)");
        }

        if (TryGetGoodPerHour(root, "Water", out _))
        {
            _report.Error("blueprint", relative + ": Water must not be a GoodConsumingBuildingSpec good; "
                                        + "the dedicated SEEWaterInventoryInitializer inventory owns the tank");
        }

        var boiler = TryObject(root, ContractSpecs.BoilerKey);
        if (boiler is null
            || !boiler.Value.TryGetProperty("WaterCapacityGoods", out var declared)
            || declared.ValueKind != JsonValueKind.Number
            || !declared.TryGetInt32(out var value)
            || value != engine.WaterTankGoods)
        {
            _report.Error("blueprint", relative + ": SEE." + ContractSpecs.BoilerKey + ".WaterCapacityGoods must be "
                                        + engine.WaterTankGoods + " (engines.json); the dedicated water inventory's "
                                        + "capacity is derived from it");
        }
    }

    private static bool TryGetGoodPerHour(JsonElement root, string goodId, out float rate)
    {
        rate = 0f;
        if (!root.TryGetProperty("GoodConsumingBuildingSpec", out var consumer)
            || !consumer.TryGetProperty("ConsumedGoods", out var goods)
            || goods.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in goods.EnumerateArray())
        {
            if (item.TryGetProperty("GoodId", out var id) && id.GetString() == goodId
                && item.TryGetProperty("GoodPerHour", out var perHour))
            {
                rate = perHour.GetSingle();
                return true;
            }
        }

        return false;
    }

    private static (string? Size, string? Tier) ParseTemplateName(string? templateName)
    {
        if (string.IsNullOrEmpty(templateName))
        {
            return (null, null);
        }

        // SteamBoiler.<Size>.<Tier>.<Faction>
        var tokens = templateName.Split('.');
        if (tokens.Length < 4 || !string.Equals(tokens[0], "SteamBoiler", StringComparison.Ordinal))
        {
            return (null, null);
        }

        return (tokens[1], tokens[2]);
    }

    private static JsonElement? TryObject(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : null;

    private static string? String(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string Show(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
