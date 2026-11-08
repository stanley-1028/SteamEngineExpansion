namespace SEE.Tools.GenerateBlueprints;

/// <summary>Summary of the authoritative-field cross-check (one line in the generator report).</summary>
internal sealed record VerificationResult(int Files, int FieldsPerFile, int ExtraChecks);

/// <summary>
/// Step 2/4 self-verification: every generated blueprint is read back and its twelve authoritative
/// fields are compared with the derived row (zero tolerance). This is the same set the acceptance
/// script checks, so a builder mistake cannot slip into the golden hash.
/// </summary>
internal static class Verification
{
    public const int AuthoritativeFieldCount = 12;

    /// <summary>Verifies every blueprint tree against its variant; throws on the first batch of mismatches.</summary>
    public static VerificationResult Verify(
        IReadOnlyList<FactionVariant> variants,
        IReadOnlyDictionary<string, JsonObject> trees)
    {
        var errors = new List<string>();
        var extraChecks = 0;
        var toolOrders = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var variant in variants)
        {
            if (!trees.TryGetValue(variant.GeneratedRelativePath, out var root))
            {
                errors.Add(variant.TemplateName + ": blueprint tree not built");
                continue;
            }

            var context = variant.TemplateName;
            var template = Object(root, "TemplateSpec", errors, context);
            if (template is not null)
            {
                Equal(Str(template, "TemplateName", errors, context), variant.TemplateName,
                    "TemplateName", context, errors);
            }

            var boiler = Object(root, "SEE.SEEBoilerSpec", errors, context);
            if (boiler is not null)
            {
                Equal(Int(boiler, "Horsepower", errors, context), variant.Horsepower, "Horsepower", context, errors);
                Equal(Dec(boiler, "FuelPerHour", errors, context), variant.FuelPerHour, "FuelPerHour", context, errors);
                Equal(Dec(boiler, "WaterPerHour", errors, context), variant.WaterPerHour, "WaterPerHour", context, errors);
                Equal(Int(boiler, "FuelCapacityGoods", errors, context), variant.FuelStock,
                    "FuelCapacityGoods", context, errors);
                Equal(Int(boiler, "WaterCapacityGoods", errors, context), variant.WaterTankGoods,
                    "WaterCapacityGoods", context, errors);
            }

            var mechanical = Object(root, "MechanicalNodeSpec", errors, context);
            if (mechanical is not null)
            {
                Equal(Int(mechanical, "PowerOutput", errors, context), variant.Horsepower,
                    "MechanicalNodeSpec.PowerOutput", context, errors);
            }

            // A liquid StockpileSpec must NOT be present: Stockpile + GoodConsumingBuilding on one
            // building double-initialised NoHaulingPostStatus and crashed the construction finish.
            // The water tank is a plain Inventory created by SEEWaterInventoryInitializer instead.
            if (root.TryGet("StockpileSpec", out _))
            {
                errors.Add(context + ": StockpileSpec is forbidden (double NoHaulingPostStatus crash); "
                                   + "water lives in the dedicated SEEWaterInventoryInitializer inventory");
            }

            var consumer = Object(root, "GoodConsumingBuildingSpec", errors, context);
            if (consumer is not null)
            {
                Equal(Int(consumer, "FullInventoryWorkHours", errors, context),
                    variant.Engine.FullInventoryWorkHours, "GoodConsumingBuildingSpec.FullInventoryWorkHours",
                    context, errors);
                var derived = ConsumedCapacity(consumer, errors, context);
                Equal(derived, variant.FuelStock,
                    "GoodConsumingBuildingSpec capacity (fuel only)", context, errors);
                VerifyNoWaterGood(consumer, context, errors);
            }

            var building = Object(root, "BuildingSpec", errors, context);
            if (building is not null)
            {
                Equal(Int(building, "ScienceCost", errors, context), variant.Engine.ScienceCost,
                    "BuildingSpec.ScienceCost", context, errors);
                VerifyCosts(building, variant.Engine.BuildCost, context, errors);
            }

            var blockObject = Object(root, "BlockObjectSpec", errors, context);
            if (blockObject is not null)
            {
                VerifyFootprint(blockObject, variant, context, errors);
            }

            var maintenance = Object(root, "SEE.SEEMaintenanceSpec", errors, context);
            if (maintenance is not null)
            {
                Equal(Dec(maintenance, "IntervalDays", errors, context), (decimal)variant.Engine.MaintenanceIntervalDays,
                    "SEEMaintenanceSpec.IntervalDays", context, errors);
                VerifyMaterials(maintenance, variant.Engine.MaintenanceMaterials, context, errors);
            }

            var placeable = Object(root, "PlaceableBlockObjectSpec", errors, context);
            if (placeable is not null)
            {
                var toolGroup = Str(placeable, "ToolGroupId", errors, context);
                Equal(toolGroup, "Power", "PlaceableBlockObjectSpec.ToolGroupId", context, errors);
                var order = Int(placeable, "ToolOrder", errors, context);
                if (order < 110 || order > 142)
                {
                    errors.Add(context + ": ToolOrder " + order + " is outside the contract range 110-142");
                }

                toolOrders[context] = order;
                extraChecks++;
            }

            VerifyLocKeys(root, variant, context, errors);
            extraChecks++;


        }

        var crossFaction = toolOrders
            .GroupBy(pair => pair.Key[..pair.Key.LastIndexOf('.')], StringComparer.Ordinal)
            .ToList();
        foreach (var group in crossFaction)
        {
            var orders = group.Select(pair => pair.Value).Distinct().ToList();
            if (orders.Count != 1)
            {
                var sizeTier = group.Key["SteamBoiler.".Length..];
                errors.Add("tool order differs between factions for " + sizeTier);
            }
        }

        if (errors.Count > 0)
        {
            throw new GeneratorException("self-verification failed:" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(error => "  - " + error)));
        }

        return new VerificationResult(variants.Count, AuthoritativeFieldCount, extraChecks);
    }

    private static int ConsumedCapacity(JsonObject consumer, List<string> errors, string context)
    {
        var array = Array(consumer, "ConsumedGoods", errors, context);
        if (array is null)
        {
            return -1;
        }

        var workHours = Int(consumer, "FullInventoryWorkHours", errors, context);
        var total = 0;
        foreach (var item in array.Items)
        {
            if (item is not JsonObject consumed)
            {
                errors.Add(context + ": ConsumedGoods entries must be objects");
                continue;
            }

            var rate = Dec(consumed, "GoodPerHour", errors, context);
            total += (int)decimal.Ceiling(rate * workHours);
        }

        return total;
    }

    /// <summary>
    /// Bug fix 2026-11-08: water must NOT be declared as a GoodConsuming good any more - the
    /// dedicated <c>SEEWaterInventoryInitializer</c> inventory owns the tank. Keeping Water here
    /// would create a second, haulable water store that shares the fuel inventory's capacity.
    /// </summary>
    private static void VerifyNoWaterGood(JsonObject consumer, string context, List<string> errors)
    {
        var array = Array(consumer, "ConsumedGoods", errors, context);
        if (array is null)
        {
            return;
        }

        foreach (var item in array.Items)
        {
            if (item is not JsonObject consumed)
            {
                continue;
            }

            if (Str(consumed, "GoodId", errors, context) == "Water")
            {
                errors.Add(context + ": ConsumedGoods must not declare Water (dedicated water tank owns it)");
            }
        }
    }

    private static void VerifyCosts(JsonObject building, IReadOnlyList<GoodAmount> expected, string context, List<string> errors)
    {
        var array = Array(building, "BuildingCost", errors, context);
        if (array is null)
        {
            return;
        }

        if (array.Items.Count != expected.Count)
        {
            errors.Add(context + ": BuildingCost has " + array.Items.Count + " entries, expected " + expected.Count);
            return;
        }

        for (var index = 0; index < expected.Count; index++)
        {
            if (array.Items[index] is not JsonObject item)
            {
                errors.Add(context + ": BuildingCost entry must be an object");
                continue;
            }

            Equal(Str(item, "Id", errors, context), expected[index].Good, "BuildingCost.Id", context, errors);
            Equal(Int(item, "Amount", errors, context), expected[index].Amount, "BuildingCost.Amount", context, errors);
        }
    }

    private static void VerifyFootprint(JsonObject blockObject, FactionVariant variant, string context, List<string> errors)
    {
        var size = Object(blockObject, "Size", errors, context);
        if (size is not null)
        {
            Equal(Int(size, "X", errors, context), variant.Engine.FootprintX, "BlockObjectSpec.Size.X", context, errors);
            Equal(Int(size, "Y", errors, context), variant.Engine.FootprintY, "BlockObjectSpec.Size.Y", context, errors);
            Equal(Int(size, "Z", errors, context), variant.Engine.Height, "BlockObjectSpec.Size.Z", context, errors);
        }

        var blocks = Array(blockObject, "Blocks", errors, context);
        if (blocks is not null)
        {
            var expected = variant.Engine.FootprintX * variant.Engine.FootprintY * variant.Engine.Height;
            Equal(blocks.Items.Count, expected, "BlockObjectSpec.Blocks count", context, errors);
        }

        var entrance = Object(blockObject, "Entrance", errors, context);
        if (entrance is not null)
        {
            var hasEntrance = entrance.TryGet("HasEntrance", out var value) && value is JsonBool { Value: true };
            if (!hasEntrance)
            {
                errors.Add(context + ": BlockObjectSpec.Entrance.HasEntrance must be true");
            }
        }
    }

    private static void VerifyMaterials(JsonObject maintenance, IReadOnlyList<GoodAmount> expected, string context, List<string> errors)
    {
        var array = Array(maintenance, "Materials", errors, context);
        if (array is null)
        {
            return;
        }

        if (array.Items.Count != expected.Count)
        {
            errors.Add(context + ": SEEMaintenanceSpec.Materials has " + array.Items.Count + " entries, expected "
                       + expected.Count);
            return;
        }

        for (var index = 0; index < expected.Count; index++)
        {
            if (array.Items[index] is not JsonObject item)
            {
                errors.Add(context + ": SEEMaintenanceSpec.Materials entry must be an object");
                continue;
            }

            Equal(Str(item, "Id", errors, context), expected[index].Good, "SEEMaintenanceSpec.Materials.Id",
                context, errors);
            Equal(Int(item, "Amount", errors, context), expected[index].Amount,
                "SEEMaintenanceSpec.Materials.Amount", context, errors);
        }
    }

    private static void VerifyLocKeys(JsonObject root, FactionVariant variant, string context, List<string> errors)
    {
        var labeled = Object(root, "LabeledEntitySpec", errors, context);
        if (labeled is null)
        {
            return;
        }

        var display = Str(labeled, "DisplayNameLocKey", errors, context);
        var description = Str(labeled, "DescriptionLocKey", errors, context);
        var flavor = Str(labeled, "FlavorDescriptionLocKey", errors, context);
        var icon = Str(labeled, "Icon", errors, context);
        Equal(display, "Building.SEE.SteamBoiler." + variant.Engine.Size + "." + variant.Engine.Tier + ".DisplayName",
            "LabeledEntitySpec.DisplayNameLocKey", context, errors);
        Equal(description, "Building.SEE.SteamBoiler.Description", "LabeledEntitySpec.DescriptionLocKey",
            context, errors);
        Equal(flavor, "Building.SEE.SteamBoiler.Flavor." + variant.Engine.Tier,
            "LabeledEntitySpec.FlavorDescriptionLocKey", context, errors);
        Equal(icon, "Icons/" + variant.IconName, "LabeledEntitySpec.Icon", context, errors);
    }

    private static JsonObject? Object(JsonObject parent, string name, List<string> errors, string context)
    {
        if (parent.TryGet(name, out var value) && value is JsonObject result)
        {
            return result;
        }

        errors.Add(context + ": missing object '" + name + "'");
        return null;
    }

    private static JsonArray? Array(JsonObject parent, string name, List<string> errors, string context)
    {
        if (parent.TryGet(name, out var value) && value is JsonArray result)
        {
            return result;
        }

        errors.Add(context + ": missing array '" + name + "'");
        return null;
    }

    private static int Int(JsonObject parent, string name, List<string> errors, string context)
    {
        if (parent.TryGet(name, out var value) && value is JsonNumber number)
        {
            return (int)number.Value;
        }

        errors.Add(context + ": missing number '" + name + "'");
        return int.MinValue;
    }

    private static decimal Dec(JsonObject parent, string name, List<string> errors, string context)
    {
        if (parent.TryGet(name, out var value) && value is JsonNumber number)
        {
            return number.Value;
        }

        errors.Add(context + ": missing number '" + name + "'");
        return decimal.MinValue;
    }

    private static string Str(JsonObject parent, string name, List<string> errors, string context)
    {
        if (parent.TryGet(name, out var value) && value is JsonString text)
        {
            return text.Value;
        }

        errors.Add(context + ": missing string '" + name + "'");
        return string.Empty;
    }

    private static void Equal<T>(T actual, T expected, string field, string context, List<string> errors)
        where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
        {
            errors.Add(context + ": " + field + " = " + actual.ToString() + " but expected " + expected.ToString());
        }
    }
}

