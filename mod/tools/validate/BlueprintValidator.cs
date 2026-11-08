using System.Text.Json;
using Mono.Cecil;

namespace SEE.Tools.Validate;

/// <summary>
/// Validates every <c>.blueprint.json</c> document against the metadata of the installed game plus
/// the mod's own built assemblies:
///
/// <list type="bullet">
/// <item>every top-level key is either a real game <c>ComponentSpec</c> subclass (or one of its
/// <c>[SpecAlias]</c> names), one of the frozen SEE specs (<c>SEE.SEEBoilerSpec</c>,
/// <c>SEE.SEEMaintenanceSpec</c> — resolved from the built SEE.Gameplay metadata), or the
/// reserved <c>Children</c> block;</item>
/// <item>every field inside a spec exists on that spec type and has the right JSON shape;</item>
/// <item>array item types are validated recursively, enum values are real enum member names, and
/// modifier suffixes (<c>#append</c>, <c>#remove</c>, <c>#replace</c>) are resolved;</item>
/// <item>every localization key referenced by the JSON is collected for the CSV cross-check.</item>
/// </list>
///
/// When SEE.Gameplay has not been built yet the two custom spec keys are validated against the
/// frozen CONTRACT field lists and only a warning is emitted; the build pipeline always builds
/// before it validates, so the shipped gate runs against real metadata.
/// </summary>
internal sealed class BlueprintValidator
{
    private static readonly HashSet<string> ReservedChildKeys =
        new(StringComparer.Ordinal) { "Children", "BlueprintPath", "Modification" };

    private static readonly HashSet<string> UnityVectorKeys =
        new(StringComparer.OrdinalIgnoreCase) { "X", "Y", "Z", "W" };

    private readonly GameMetadata _metadata;
    private readonly Report _report;
    private readonly IReadOnlyDictionary<string, string[]> _contractSpecs;
    private readonly SortedSet<string> _usedLocKeys = new(StringComparer.Ordinal);
    private readonly HashSet<string> _missingContractSpecsReported = new(StringComparer.Ordinal);

    public BlueprintValidator(
        GameMetadata metadata,
        Report report,
        IReadOnlyDictionary<string, string[]> contractSpecs)
    {
        _metadata = metadata;
        _report = report;
        _contractSpecs = contractSpecs;
    }

    public IReadOnlyCollection<string> UsedLocKeys => _usedLocKeys;

    public void ValidateFile(string filePath, string relativePath)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(filePath));
        }
        catch (JsonException ex)
        {
            _report.Error("blueprint", relativePath + ": invalid JSON: " + ex.Message);
            return;
        }

        using (document)
        {
            CollectLocKeys(document.RootElement);
            ValidateBlueprint(document.RootElement, relativePath, string.Empty, nested: false);
        }
    }

    private void ValidateBlueprint(JsonElement blueprint, string relativePath, string location, bool nested)
    {
        if (blueprint.ValueKind != JsonValueKind.Object)
        {
            _report.Error("blueprint", At(relativePath, location) + ": a blueprint must be a JSON object");
            return;
        }

        foreach (var property in blueprint.EnumerateObject())
        {
            var name = property.Name;

            if (name == "Children")
            {
                ValidateChildren(property.Value, relativePath, Join(location, "Children"));
                continue;
            }

            if (ReservedChildKeys.Contains(name))
            {
                if (!nested)
                {
                    _report.Error("blueprint", At(relativePath, location) + ": '" + name + "' is only valid in a child blueprint");
                }
                else if (name == "BlueprintPath")
                {
                    if (property.Value.ValueKind != JsonValueKind.String)
                    {
                        _report.Error("blueprint", At(relativePath, location) + ".BlueprintPath must be a string");
                    }
                }
                else if (name == "Modification")
                {
                    ValidateBlueprint(property.Value, relativePath, Join(location, "Modification"), nested: true);
                }

                continue;
            }

            if (_metadata.IsKnownSpecName(name))
            {
                ValidateSpec(name, property.Value, relativePath, Join(location, name));
                continue;
            }

            if (_contractSpecs.TryGetValue(name, out var contractFields))
            {
                ValidateContractOnlySpec(name, contractFields, property.Value, relativePath, Join(location, name));
                continue;
            }

            _report.Error("spec", At(relativePath, location) + ": unknown spec '" + name
                                  + "' (not a ComponentSpec subclass in the installed game, not a [SpecAlias] of a built "
                                  + "SEE assembly, and not one of the frozen SEE contract specs "
                                  + string.Join(" / ", _contractSpecs.Keys) + ")");
        }
    }

    private void ValidateChildren(JsonElement children, string relativePath, string location)
    {
        if (children.ValueKind != JsonValueKind.Object)
        {
            _report.Error("blueprint", At(relativePath, location) + " must be an object");
            return;
        }

        foreach (var child in children.EnumerateObject())
        {
            ValidateBlueprint(child.Value, relativePath, Join(location, child.Name), nested: true);
        }
    }

    private void ValidateSpec(string specName, JsonElement value, string relativePath, string location)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            _report.Error("spec", At(relativePath, location) + " must be an object");
            return;
        }

        if (!_metadata.TryGetSpec(specName, out var definition))
        {
            return;
        }

        var members = _metadata.GetMembers(definition);

        foreach (var property in value.EnumerateObject())
        {
            var baseName = StripModifier(property.Name);

            if (!members.TryGetValue(baseName, out var member))
            {
                _report.Error("spec", At(relativePath, location) + ": '" + property.Name
                                      + "' is not a serialized member of " + specName);
                continue;
            }

            ValidateValue(member.Type, property.Value, relativePath, Join(location, property.Name), depth: 0);
        }
    }

    /// <summary>
    /// Fallback validation for the frozen SEE specs before SEE.Gameplay is built: field names are
    /// checked against the CONTRACT list instead of live metadata.
    /// </summary>
    private void ValidateContractOnlySpec(
        string specName,
        string[] contractFields,
        JsonElement value,
        string relativePath,
        string location)
    {
        if (_missingContractSpecsReported.Add(specName))
        {
            _report.Warn("assemblies",
                "SEE.Gameplay is not built yet: '" + specName
                + "' has no metadata, so its field types are checked against the frozen CONTRACT list only "
                + "(rebuild SEE.Gameplay and re-run for full metadata validation)");
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            _report.Error("spec", At(relativePath, location) + " must be an object");
            return;
        }

        foreach (var property in value.EnumerateObject())
        {
            if (!contractFields.Contains(property.Name, StringComparer.Ordinal))
            {
                _report.Error("spec", At(relativePath, location) + ": '" + property.Name
                                      + "' is not one of the frozen CONTRACT fields of " + specName);
            }
        }

        foreach (var expected in contractFields)
        {
            if (!value.TryGetProperty(expected, out _))
            {
                _report.Error("spec", At(relativePath, location) + ": missing frozen CONTRACT field '" + expected + "'");
            }
        }
    }

    private void ValidateValue(TypeReference type, JsonElement value, string relativePath, string location, int depth)
    {
        if (depth > 16 || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return;
        }

        if (GameMetadata.IsCollection(type, out var elementType))
        {
            if (value.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                ValidateValue(elementType, item, relativePath, location + "[" + index + "]", depth + 1);
                index++;
            }

            return;
        }

        if (GameMetadata.TryGetPrimitiveKind(type, out var kind))
        {
            if (kind == PrimitiveKind.Boolean)
            {
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    _report.Error("spec", At(relativePath, location) + ": expected a boolean but found " + value.ValueKind);
                }

                return;
            }

            var expected = kind switch
            {
                PrimitiveKind.String => JsonValueKind.String,
                PrimitiveKind.Number => JsonValueKind.Number,
                _ => JsonValueKind.Undefined,
            };

            if (expected != JsonValueKind.Undefined && value.ValueKind != expected)
            {
                _report.Error("spec", At(relativePath, location) + ": expected " + expected
                                      + " but found " + value.ValueKind + " (" + type.Name + ")");
            }

            return;
        }

        if (GameMetadata.IsUnityVector(type))
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (var axis in value.EnumerateObject())
            {
                if (!UnityVectorKeys.Contains(axis.Name))
                {
                    _report.Error("spec", At(relativePath, location) + ": unexpected vector component '" + axis.Name + "'");
                }
                else if (axis.Value.ValueKind != JsonValueKind.Number)
                {
                    _report.Error("spec", At(relativePath, location) + "." + axis.Name + ": expected a number");
                }
            }

            return;
        }

        // Game asset references (AssetRef<TimbermeshAsset>, AssetRef<Sprite>, ...) are opaque
        // value objects; their generic argument is not a serializable shape to recurse into.
        if (type is GenericInstanceType)
        {
            return;
        }

        var definition = _metadata.Resolve(type);
        if (definition is null)
        {
            _report.Warn("spec", At(relativePath, location) + ": could not resolve type '" + type.FullName
                                  + "' for deep validation");
            return;
        }

        if (definition.IsEnum)
        {
            ValidateEnum(definition, value, relativePath, location);
            return;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var members = _metadata.GetMembers(definition);
        if (members.Count == 0)
        {
            return;
        }

        foreach (var property in value.EnumerateObject())
        {
            var baseName = StripModifier(property.Name);
            if (!members.TryGetValue(baseName, out var member))
            {
                _report.Error("spec", At(relativePath, location) + ": '" + property.Name
                                      + "' is not a serialized member of " + definition.Name);
                continue;
            }

            ValidateValue(member.Type, property.Value, relativePath, Join(location, property.Name), depth + 1);
        }
    }

    private void ValidateEnum(TypeDefinition definition, JsonElement value, string relativePath, string location)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            _report.Error("spec", At(relativePath, location) + ": expected an enum name or number");
            return;
        }

        var names = _metadata.ReadEnumNames(definition);
        var text = value.GetString() ?? string.Empty;

        foreach (var token in text.Split(','))
        {
            var name = token.Trim();
            if (name.Length == 0)
            {
                continue;
            }

            var known = names.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
            if (!known)
            {
                _report.Error("spec", At(relativePath, location) + ": '" + name
                                      + "' is not a member of enum " + definition.FullName);
            }
        }
    }

    private static string StripModifier(string name)
    {
        var hash = name.IndexOf('#');
        return hash < 0 ? name : name[..hash];
    }

    private static string Join(string location, string segment) =>
        location.Length == 0 ? segment : location + "." + segment;

    private static string At(string relativePath, string location) =>
        location.Length == 0 ? relativePath : relativePath + ": " + location;

    private void CollectLocKeys(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString() ?? string.Empty;
                        if (value.Length > 0
                            && property.Name.EndsWith("LocKey", StringComparison.Ordinal)
                            && !ReservedChildKeys.Contains(property.Name))
                        {
                            _usedLocKeys.Add(value);
                        }
                    }

                    CollectLocKeys(property.Value);
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectLocKeys(item);
                }

                break;
        }
    }
}
