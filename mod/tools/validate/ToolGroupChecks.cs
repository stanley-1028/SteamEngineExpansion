using System.Text.Json;

namespace SEE.Tools.Validate;

/// <summary>
/// P10 gate: the dedicated SEE bottom-bar building group ("Steam Engines").
///
/// It checks the three things the data package must ship so the collapsible building menu appears:
///
/// <list type="bullet">
/// <item><c>data/BlockObjectToolGroups/BlockObjectToolGroup.SteamEngines.blueprint.json</c> exists
/// and is exactly the vanilla <c>BlockObjectToolGroupSpec</c> shape with the frozen values
/// (Id/Order/NameLocKey/Icon/FallbackGroup);</item>
/// <item>its sprite exists as <c>data/ToolGroupIcons/SteamEngines.png</c> plus the
/// <c>.png.meta.json</c> sidecar with <c>isSprite=true</c> (the game's <c>ModSpriteConverter</c>
/// loads the mod PNG only through that sidecar);</item>
/// <item>all 24 boiler blueprints point their <c>PlaceableBlockObjectSpec.ToolGroupId</c> at the new
/// group (the button is created only when the group has members).</item>
/// </list>
///
/// The group blueprint is placed in its own folder, not in a TemplateCollection: the game's
/// <c>ModSystemFileProvider&lt;BlueprintAsset&gt;</c> converts every mod file whose name ends with
/// <c>.blueprint.json</c> and feeds the spec service, so the group spec loads like any vanilla one.
/// </summary>
internal sealed class ToolGroupChecks
{
    private const int ExpectedBoilerCount = 24;
    private const int ExpectedSpecFieldCount = 5;

    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private readonly string _dataDir;
    private readonly Report _report;

    public ToolGroupChecks(string dataDir, Report report)
    {
        _dataDir = dataDir;
        _report = report;
    }

    /// <summary>Number of boiler blueprints that declare the new group id (24 when complete).</summary>
    public int MembersVerified { get; private set; }

    public void Validate()
    {
        ValidateBlueprint();
        ValidateIcon();
        ValidateMembers();
    }

    private void ValidateBlueprint()
    {
        var path = Path.Combine(_dataDir,
            ContractSpecs.ToolGroupBlueprintRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            _report.Error("toolgroup", "data/" + ContractSpecs.ToolGroupBlueprintRelativePath
                                        + " is missing; the bottom-bar group has no spec and the 24 boilers would "
                                        + "fall back to a log warning instead of a button");
            return;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path));
        }
        catch (JsonException exception)
        {
            _report.Error("toolgroup", "data/" + ContractSpecs.ToolGroupBlueprintRelativePath
                                        + " is not valid JSON: " + exception.Message);
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                _report.Error("toolgroup", "the tool-group blueprint root must be a JSON object");
                return;
            }

            foreach (var property in root.EnumerateObject())
            {
                if (!string.Equals(property.Name, "BlockObjectToolGroupSpec", StringComparison.Ordinal))
                {
                    _report.Error("toolgroup", "the tool-group blueprint must only contain "
                                                + "'BlockObjectToolGroupSpec' (found '" + property.Name + "')");
                }
            }

            if (!root.TryGetProperty("BlockObjectToolGroupSpec", out var spec)
                || spec.ValueKind != JsonValueKind.Object)
            {
                _report.Error("toolgroup", "BlockObjectToolGroupSpec is missing or is not an object");
                return;
            }

            var fields = 0;
            foreach (var property in spec.EnumerateObject())
            {
                fields++;
            }

            if (fields != ExpectedSpecFieldCount)
            {
                _report.Error("toolgroup", "BlockObjectToolGroupSpec has " + fields + " field(s); the vanilla shape is "
                                            + ExpectedSpecFieldCount
                                            + " (Id, Order, NameLocKey, Icon, FallbackGroup)");
            }

            ExpectText("Id", spec, ContractSpecs.ToolGroupId);
            ExpectInt("Order", spec, ContractSpecs.ToolGroupOrder);
            ExpectText("NameLocKey", spec, ContractSpecs.ToolGroupNameLocKey);
            ExpectText("Icon", spec, ContractSpecs.ToolGroupIconPath);
            ExpectBool("FallbackGroup", spec, false);

            var icon = Text(spec, "Icon") ?? string.Empty;
            if (icon.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                _report.Error("toolgroup", "BlockObjectToolGroupSpec.Icon must not carry a file extension ('" + icon + "')");
            }
        }
    }

    private void ValidateIcon()
    {
        var pngPath = Path.Combine(_dataDir,
            ContractSpecs.ToolGroupIconRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(pngPath))
        {
            _report.Error("toolgroup", "data/" + ContractSpecs.ToolGroupIconRelativePath
                                        + " is missing; the bottom-bar group button would have no sprite");
        }
        else
        {
            var header = new byte[PngSignature.Length];
            using (var stream = File.OpenRead(pngPath))
            {
                var read = stream.Read(header, 0, header.Length);
                if (read != header.Length || !header.AsSpan().SequenceEqual(PngSignature))
                {
                    _report.Error("toolgroup", "data/" + ContractSpecs.ToolGroupIconRelativePath
                                                + " is not a PNG file (bad signature)");
                }
            }
        }

        var metaPath = Path.Combine(_dataDir,
            ContractSpecs.ToolGroupIconMetaRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(metaPath))
        {
            _report.Error("toolgroup", "data/" + ContractSpecs.ToolGroupIconMetaRelativePath
                                        + " is missing; the game only converts a mod PNG when its sidecar says isSprite");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(metaPath));
            if (!document.RootElement.TryGetProperty("isSprite", out var isSprite)
                || isSprite.ValueKind != JsonValueKind.True)
            {
                _report.Error("toolgroup", "data/" + ContractSpecs.ToolGroupIconMetaRelativePath
                                            + " must contain {\"isSprite\": true}");
            }
        }
        catch (JsonException exception)
        {
            _report.Error("toolgroup", "data/" + ContractSpecs.ToolGroupIconMetaRelativePath
                                        + " is not valid JSON: " + exception.Message);
        }
    }

    private void ValidateMembers()
    {
        var folder = Path.Combine(_dataDir, "Buildings", "Power", "SteamBoiler");
        if (!Directory.Exists(folder))
        {
            _report.Error("toolgroup", "data/Buildings/Power/SteamBoiler is missing; the group has no members to show");
            return;
        }

        var files = Directory.GetFiles(folder, "*.blueprint.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (files.Count != ExpectedBoilerCount)
        {
            _report.Error("toolgroup", "expected " + ExpectedBoilerCount + " boiler blueprints, found " + files.Count);
        }

        foreach (var file in files)
        {
            var relative = "data/Buildings/Power/SteamBoiler/" + Path.GetFileName(file);
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(File.ReadAllText(file));
            }
            catch (JsonException exception)
            {
                _report.Error("toolgroup", relative + ": invalid JSON: " + exception.Message);
                continue;
            }

            using (document)
            {
                var group = GroupId(document.RootElement);
                if (string.Equals(group, ContractSpecs.ToolGroupId, StringComparison.Ordinal))
                {
                    MembersVerified++;
                    continue;
                }

                _report.Error("toolgroup", relative + ": PlaceableBlockObjectSpec.ToolGroupId = " + (group ?? "<missing>")
                                            + " but the dedicated SEE group requires '" + ContractSpecs.ToolGroupId + "'");
            }
        }

        if (MembersVerified != ExpectedBoilerCount)
        {
            _report.Error("toolgroup", "only " + MembersVerified + "/" + ExpectedBoilerCount
                                        + " boiler blueprints declare the '" + ContractSpecs.ToolGroupId + "' group");
        }
    }

    private static string? GroupId(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("PlaceableBlockObjectSpec", out var placeable)
            || placeable.ValueKind != JsonValueKind.Object
            || !placeable.TryGetProperty("ToolGroupId", out var group)
            || group.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return group.GetString();
    }

    private static string? Text(JsonElement parent, string name)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private void ExpectText(string field, JsonElement spec, string expected)
    {
        var actual = Text(spec, field);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            _report.Error("toolgroup", "BlockObjectToolGroupSpec." + field + " = " + (actual ?? "<missing>")
                                        + " but the frozen design requires '" + expected + "'");
        }
    }

    private void ExpectInt(string field, JsonElement spec, int expected)
    {
        if (spec.ValueKind != JsonValueKind.Object || !spec.TryGetProperty(field, out var value)
            || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var actual))
        {
            _report.Error("toolgroup", "BlockObjectToolGroupSpec." + field + " is missing or is not an integer");
            return;
        }

        if (actual != expected)
        {
            _report.Error("toolgroup", "BlockObjectToolGroupSpec." + field + " = " + actual
                                        + " but the frozen design requires " + expected);
        }
    }

    private void ExpectBool(string field, JsonElement spec, bool expected)
    {
        if (spec.ValueKind != JsonValueKind.Object || !spec.TryGetProperty(field, out var value)
            || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            _report.Error("toolgroup", "BlockObjectToolGroupSpec." + field + " is missing or is not a boolean");
            return;
        }

        var actual = value.ValueKind == JsonValueKind.True;
        if (actual != expected)
        {
            _report.Error("toolgroup", "BlockObjectToolGroupSpec." + field + " = "
                                        + (actual ? "true" : "false") + " but the frozen design requires "
                                        + (expected ? "true" : "false"));
        }
    }
}
