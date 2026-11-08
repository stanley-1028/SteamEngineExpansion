using System.Globalization;
using System.Text.Json;

namespace SEE.Tools.Validate;

/// <summary>
/// Blueprint-level gates specific to SEE boilers (Lead ruling AD-5, evidence findings-power):
///
/// <list type="bullet">
/// <item>no <c>GoodPoweredGeneratorSpec</c> / <c>PoweredGoodConsumingBuildingSpec</c>: the vanilla
/// component rewrites <c>SetOutputMultiplier</c> every tick and toggles the fuel consumer, which
/// would fight the SEE controller's output modulation (the allowed visuals/audio specs stay legal);</item>
/// <item>no <c>BuildingPipeSpec</c> / <c>PowerLineSpec</c> in the shipped JSON;</item>
/// <item>no <c>WorkplaceSpec</c>: the boiler is worker-free (REQ-CORE-02);</item>
/// <item><c>MechanicalNodeSpec.PowerOutput</c> equals <c>SEE.SEEBoilerSpec.Horsepower</c>;</item>
/// <item><c>GoodConsumingBuildingSpec.ConsumedGoods</c> only names fuel goods from
/// <c>SEE.SEEBoilerSpec.FuelGoodIds</c>; Water is owned by the dedicated water inventory
/// (bug fix 2026-11-08), not by the vanilla consumer;</item>
/// <item><c>TransputProviderSpec</c> declares no vertical <c>Top</c> transput, and exactly one
/// <c>Up</c> transput on the vanilla back-face slot. <c>Directions</c> is a
/// <c>Timberborn.Coordinates.Directions3D</c> (Down/Left/Up/Right/Bottom/Top); the vanilla
/// SteamEngine.IronTeeth proves that <c>"Up"</c> is the horizontal +Y (back) face at
/// <c>(1, 2, 0)</c> for a 3x3 footprint, not the top face.</item>
/// </list>
/// </summary>
internal sealed class BoilerBlueprintChecks
{
    private static readonly string[] BannedSpecs =
    {
        "GoodPoweredGeneratorSpec",
        "PoweredGoodConsumingBuildingSpec",
        "BuildingPipeSpec",
        "PowerLineSpec",
        "WorkplaceSpec",
        // v0.1.1 hotfix: Stockpile + GoodConsumingBuilding on one building double-initialises
        // NoHaulingPostStatus and crashes the construction finish (Player.log 2026-11-07).
        "StockpileSpec",
    };

    private static readonly string[] BannedTransputDirections = { "Top" };

    private readonly string _dataDir;
    private readonly Report _report;

    public BoilerBlueprintChecks(string dataDir, Report report)
    {
        _dataDir = dataDir;
        _report = report;
    }

    public int ValidatedCount { get; private set; }

    public void Validate()
    {
        var blueprints = Directory
            .GetFiles(_dataDir, "*.blueprint.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var file in blueprints)
        {
            var relative = Path.GetRelativePath(_dataDir, file).Replace('\\', '/');
            if (!relative.Contains("SteamBoiler", StringComparison.Ordinal))
            {
                continue;
            }

            ValidatedCount++;
            ValidateBlueprint(file, relative);
        }
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

            CheckBannedSpecs(root, relative, string.Empty);

            var boiler = TryObject(root, ContractSpecs.BoilerKey);
            if (boiler is null)
            {
                _report.Error("blueprint", relative + ": missing the frozen " + ContractSpecs.BoilerKey
                                            + " spec (class/alias not bound to the blueprint)");
                return;
            }

            CheckMechanicalOutput(root, boiler.Value, relative);
            CheckConsumedGoods(root, boiler.Value, relative);
            CheckTransputs(root, relative);
        }
    }

    private void CheckBannedSpecs(JsonElement element, string relative, string location)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var childLocation = location.Length == 0 ? property.Name : location + "." + property.Name;
                    if (BannedSpecs.Contains(property.Name, StringComparer.Ordinal))
                    {
                        _report.Error("blueprint", relative + ": forbidden spec '" + property.Name + "' at '"
                                                    + childLocation + "' (it fights the SEE output/consumption controller)");
                    }

                    CheckBannedSpecs(property.Value, relative, childLocation);
                }

                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    CheckBannedSpecs(item, relative, location + "[" + index + "]");
                    index++;
                }

                break;
        }
    }

    private void CheckMechanicalOutput(JsonElement root, JsonElement boiler, string relative)
    {
        if (!TryNumber(boiler, "Horsepower", out var horsepower))
        {
            _report.Error("blueprint", relative + ": " + ContractSpecs.BoilerKey + ".Horsepower must be a number");
            return;
        }

        var mechanical = TryObject(root, "MechanicalNodeSpec");
        if (mechanical is null)
        {
            _report.Error("blueprint", relative + ": missing MechanicalNodeSpec (the boiler has to feed the mechanical graph)");
            return;
        }

        if (!TryNumber(mechanical.Value, "PowerOutput", out var powerOutput))
        {
            _report.Error("blueprint", relative + ": MechanicalNodeSpec.PowerOutput must be a number");
            return;
        }

        if (powerOutput != horsepower)
        {
            _report.Error("blueprint", relative + ": MechanicalNodeSpec.PowerOutput = " + Show(powerOutput)
                                        + " but " + ContractSpecs.BoilerKey + ".Horsepower = " + Show(horsepower)
                                        + " (the mechanical output must match the rated horsepower)");
        }
    }

    private void CheckConsumedGoods(JsonElement root, JsonElement boiler, string relative)
    {
        if (!boiler.TryGetProperty("FuelGoodIds", out var fuelGoodIds) || fuelGoodIds.ValueKind != JsonValueKind.Array)
        {
            _report.Error("blueprint", relative + ": " + ContractSpecs.BoilerKey + ".FuelGoodIds must be an array");
            return;
        }

        var allowed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in fuelGoodIds.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(item.GetString()))
            {
                _report.Error("blueprint", relative + ": " + ContractSpecs.BoilerKey + ".FuelGoodIds entries must be strings");
                continue;
            }

            allowed.Add(item.GetString()!);
        }

        if (allowed.Count == 0)
        {
            _report.Error("blueprint", relative + ": " + ContractSpecs.BoilerKey + ".FuelGoodIds is empty");
        }

        var consumer = TryObject(root, "GoodConsumingBuildingSpec");
        if (consumer is null)
        {
            return;
        }

        if (!consumer.Value.TryGetProperty("ConsumedGoods", out var consumed)
            || consumed.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in consumed.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var good = String(item, "GoodId");
            if (good is null)
            {
                continue;
            }

            // Bug fix 2026-11-08: Water must not be a GoodConsuming good. The dedicated
            // SEEWaterInventoryInitializer inventory owns the tank; a Water entry here would create
            // a second water store that shares the fuel inventory and its hauling target.
            if (good == "Water")
            {
                _report.Error("blueprint", relative + ": GoodConsumingBuildingSpec must not consume 'Water'; "
                                            + "the dedicated water inventory owns the tank");
                continue;
            }

            if (!allowed.Contains(good))
            {
                _report.Error("blueprint", relative + ": GoodConsumingBuildingSpec consumes '" + good
                                            + "' which is not listed in " + ContractSpecs.BoilerKey
                                            + ".FuelGoodIds (" + string.Join(", ", allowed) + ")");
            }
        }
    }

    private void CheckTransputs(JsonElement root, string relative)
    {
        var transput = TryObject(root, "TransputProviderSpec");
        if (transput is null)
        {
            _report.Warn("blueprint", relative + ": no TransputProviderSpec (mechanical face declaration)");
            return;
        }

        if (!transput.Value.TryGetProperty("Transputs", out var transputs) || transputs.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        // Expected back-face slot: X = floor(Size.X / 2), Y = Size.Y - 1, Z = 0 (vanilla evidence:
        // SteamEngine.IronTeeth declares its back transput at (1, 2, 0) with Directions "Up" on its
        // 3x3 footprint; Directions3D's "Up" is the horizontal +Y face, so only "Top" is forbidden).
        var hasSlot = TryBackSlot(root, relative, out var slotX, out var slotY);
        var upCount = 0;
        foreach (var item in transputs.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var directions = String(item, "Directions");
            if (directions is null)
            {
                continue;
            }

            foreach (var token in directions.Split(','))
            {
                var direction = token.Trim();
                if (direction.Length == 0)
                {
                    continue;
                }

                if (BannedTransputDirections.Contains(direction, StringComparer.OrdinalIgnoreCase))
                {
                    _report.Error("blueprint", relative + ": TransputProviderSpec declares a '" + direction
                                                + "' transput; the vertical top face is forbidden (requirements §13.4)");
                    continue;
                }

                if (string.Equals(direction, "Up", StringComparison.OrdinalIgnoreCase))
                {
                    upCount++;
                    if (hasSlot)
                    {
                        CheckUpTransputSlot(item, relative, slotX, slotY);
                    }
                }
            }
        }

        if (upCount != 1)
        {
            _report.Error("blueprint", relative + ": TransputProviderSpec must declare exactly one 'Up' back-face transput "
                                        + "(vanilla SteamEngine.IronTeeth uses (1, 2, 0) Directions 'Up'); found " + upCount);
        }
    }

    /// <summary>Reads the expected back-face slot from BlockObjectSpec.Size (X width, Y depth, Z height).</summary>
    private bool TryBackSlot(JsonElement root, string relative, out double slotX, out double slotY)
    {
        slotX = 0;
        slotY = 0;
        var blockObject = TryObject(root, "BlockObjectSpec");
        var size = blockObject is null ? (JsonElement?)null : TryObject(blockObject.Value, "Size");
        if (size is null || !TryNumber(size.Value, "X", out var sizeX) || !TryNumber(size.Value, "Y", out var sizeY))
        {
            _report.Error("blueprint", relative
                                        + ": cannot verify the back-face 'Up' transput slot: BlockObjectSpec.Size.X/Y is missing");
            return false;
        }

        slotX = Math.Floor(sizeX / 2.0);
        slotY = sizeY - 1.0;
        return true;
    }

    private void CheckUpTransputSlot(JsonElement transput, string relative, double slotX, double slotY)
    {
        if (!transput.TryGetProperty("Coordinates", out var coordinates) || coordinates.ValueKind != JsonValueKind.Object
            || !TryNumber(coordinates, "X", out var x) || !TryNumber(coordinates, "Y", out var y)
            || !TryNumber(coordinates, "Z", out var z))
        {
            _report.Error("blueprint", relative + ": the 'Up' transput must carry Coordinates {X, Y, Z}");
            return;
        }

        if (Math.Abs(x - slotX) > 1e-9 || Math.Abs(y - slotY) > 1e-9 || Math.Abs(z) > 1e-9)
        {
            _report.Error("blueprint", relative + ": 'Up' transput at (" + Show(x) + ", " + Show(y) + ", " + Show(z)
                                        + ") but the vanilla back-face slot is (" + Show(slotX) + ", " + Show(slotY)
                                        + ", 0) computed from BlockObjectSpec.Size");
        }
    }

    private static JsonElement? TryObject(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : null;

    private static bool TryNumber(JsonElement parent, string name, out double value)
    {
        if (parent.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number
            && element.TryGetDouble(out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static string? String(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string Show(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
