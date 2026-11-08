using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SEE.Tools.GenerateBlueprints;

/// <summary>One generated file: mod-relative path (forward slashes) plus exact bytes.</summary>
internal sealed record GeneratedFile(string RelativePath, byte[] Bytes);

/// <summary>
/// Step 6/7 output helpers: template collections, the derived reconciliation table, the golden hash
/// file, and the generated-file README. Everything is byte-stable so <c>--check</c> is a pure diff.
/// </summary>
internal static class Outputs
{
    /// <summary>Canonical relative path of the golden hash file.</summary>
    public const string GoldenPath = "tests/SEE.Tests/Golden/blueprints.sha256";

    /// <summary>Marker line used in every generated document (AD-7: no comments inside JSON).</summary>
    public const string GeneratedNotice =
        "GENERATED, DO NOT EDIT - produced by mod/tools/generate-blueprints from mod/data/engines.json";

    /// <summary>Builds one faction template collection that appends the twelve blueprint paths.</summary>
    public static JsonObject TemplateCollection(string collectionId, IReadOnlyList<FactionVariant> variants)
    {
        var append = new JsonArray();
        foreach (var variant in variants)
        {
            append.Add(new JsonString(variant.TemplatePath));
        }

        return new JsonObject()
            .Add("TemplateCollectionSpec", new JsonObject()
                .Add("CollectionId", new JsonString(collectionId))
                .Add("Blueprints#append", append));
    }

    /// <summary>Builds data/engines.derived.json, the per-cell reconciliation table used by P7/P6.</summary>
    public static JsonObject DerivedJson(
        Catalog catalog,
        IReadOnlyList<DerivedRow> rows,
        IReadOnlyList<GateResult> gates,
        string sourceSha256,
        VerificationResult verification)
    {
        var collapsed = new JsonArray();
        foreach (var row in rows.Where(row => row.FactionFuelCollapsed))
        {
            collapsed.Add(new JsonString(row.Engine.Size + "." + row.Engine.Tier));
        }

        var gateArray = new JsonArray();
        foreach (var gate in gates)
        {
            gateArray.Add(new JsonObject()
                .Add("id", Numbers.Integer(gate.Id))
                .Add("name", new JsonString(gate.Name))
                .Add("pass", new JsonBool(gate.Pass))
                .Add("detail", new JsonString(gate.Detail)));
        }

        var rowArray = new JsonArray();
        foreach (var row in rows)
        {
            rowArray.Add(new JsonObject()
                .Add("size", new JsonString(row.Engine.Size))
                .Add("tier", new JsonString(row.Engine.Tier))
                .Add("footprint", new JsonObject()
                    .Add("x", Numbers.Integer(row.Engine.FootprintX))
                    .Add("y", Numbers.Integer(row.Engine.FootprintY)))
                .Add("height", Numbers.Integer(row.Engine.Height))
                .Add("ratedHorsepower", Numbers.Integer(row.Engine.RatedHorsepower))
                .Add("scienceCost", Numbers.Integer(row.Engine.ScienceCost))
                .Add("buildCost", Goods(row.Engine.BuildCost))
                .Add("fuelPerHour", Numbers.Decimal(row.Engine.FuelPerHour))
                .Add("waterPerHour", Numbers.Decimal(row.Engine.WaterPerHour))
                .Add("fuelStock", Numbers.Integer(row.Engine.FuelStock))
                .Add("waterTankGoods", Numbers.Integer(row.Engine.WaterTankGoods))
                .Add("fullInventoryWorkHours", Numbers.Integer(row.Engine.FullInventoryWorkHours))
                .Add("maintenanceIntervalDays", Numbers.Integer(row.Engine.MaintenanceIntervalDays))
                .Add("maintenanceMaterials", Goods(row.Engine.MaintenanceMaterials))
                .Add("factionFuelCollapsed", new JsonBool(row.FactionFuelCollapsed))
                .Add("variants", Variants(row.Variants)));
        }

        return new JsonObject()
            .Add("generated", new JsonBool(true))
            .Add("notice", new JsonString(GeneratedNotice))
            .Add("generator", new JsonString("tools/generate-blueprints"))
            .Add("dataVersion", Numbers.Integer(catalog.DataVersion))
            .Add("source", new JsonString("data/engines.json"))
            .Add("sourceSha256", new JsonString(sourceSha256))
            .Add("blueprintCount", Numbers.Integer(rows.Count * catalog.Factions.Count))
            .Add("authoritativeFields", new JsonObject()
                .Add("files", Numbers.Integer(verification.Files))
                .Add("fieldsPerFile", Numbers.Integer(verification.FieldsPerFile)))
            .Add("gates", gateArray)
            .Add("factionFuelCollapsed", collapsed)
            .Add("rows", rowArray);
    }

    private static JsonObject Variants(IReadOnlyList<FactionVariant> variants)
    {
        var result = new JsonObject();
        foreach (var variant in variants)
        {
            var consumed = new JsonArray();
            foreach (var rate in variant.ConsumedGoods)
            {
                consumed.Add(new JsonObject()
                    .Add("goodId", new JsonString(rate.GoodId))
                    .Add("goodPerHour", Numbers.Decimal(rate.GoodPerHour)));
            }

            var values = new JsonArray();
            foreach (var value in variant.FuelValues)
            {
                values.Add(Numbers.Decimal(value));
            }

            result.Add(variant.Faction.Name, new JsonObject()
                .Add("horsepower", Numbers.Integer(variant.Horsepower))
                .Add("fuelPerHour", Numbers.Decimal(variant.FuelPerHour))
                .Add("waterPerHour", Numbers.Decimal(variant.WaterPerHour))
                .Add("fuelStock", Numbers.Integer(variant.FuelStock))
                .Add("waterTankGoods", Numbers.Integer(variant.WaterTankGoods))
                .Add("fuelGoodIds", new JsonArray().AddStrings(variant.FuelGoodIds))
                .Add("fuelValues", values)
                .Add("consumedGoods", consumed)
                .Add("toolOrder", Numbers.Integer(variant.ToolOrder))
                .Add("templateName", new JsonString(variant.TemplateName))
                .Add("blueprint", new JsonString(variant.GeneratedRelativePath)));
        }

        return result;
    }

    private static JsonArray Goods(IReadOnlyList<GoodAmount> goods)
    {
        var array = new JsonArray();
        foreach (var item in goods)
        {
            array.Add(new JsonObject()
                .Add("good", new JsonString(item.Good))
                .Add("amount", Numbers.Integer(item.Amount)));
        }

        return array;
    }

    /// <summary>Renders the sha256sum-style golden file over the 24 blueprint files.</summary>
    public static string GoldenHashes(IEnumerable<GeneratedFile> files)
    {
        var builder = new StringBuilder();
        foreach (var file in files.OrderBy(file => file.RelativePath, StringComparer.Ordinal))
        {
            builder.Append(Sha256(file.Bytes)).Append("  ").Append(file.RelativePath).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>Lowercase hex SHA-256 of the given bytes.</summary>
    public static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    /// <summary>UTF-8 encoding without a BOM (the game reads BOM-less CSV; AD-8).</summary>
    public static byte[] Utf8(string text) => new UTF8Encoding(false).GetBytes(text);

    /// <summary>README placed next to the generated blueprints (JSON has no comment syntax).</summary>
    public static string BlueprintReadme() =>
        "# Generated steam boiler blueprints\n"
        + "\n"
        + GeneratedNotice + "\n"
        + "\n"
        + "The 24 `SteamBoiler.<Size>.<Tier>.<Faction>.blueprint.json` files in this folder are\n"
        + "generated from `mod/data/engines.json` (the single number source). Do not edit them by\n"
        + "hand: change the data file and re-run the generator.\n"
        + "\n"
        + "    dotnet run --project mod/tools/generate-blueprints -c Release -m:1\n"
        + "    dotnet run --project mod/tools/generate-blueprints -c Release -m:1 -- --check\n"
        + "\n"
        + "`--check` rebuilds every artifact in memory and fails when one byte differs from the\n"
        + "checked-in files. The golden hash of the 24 blueprints lives in\n"
        + "`tests/SEE.Tests/Golden/blueprints.sha256`; the derived reconciliation table lives in\n"
        + "`data/engines.derived.json`.\n";

    /// <summary>Formats a number the way the generated JSON does (report/diagnostic helper).</summary>
    public static string Show(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
