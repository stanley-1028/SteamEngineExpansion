using Mono.Cecil;

namespace SEE.Tools.Validate;

/// <summary>
/// Static gates on the built shipping assemblies, read with Mono.Cecil from
/// <c>mod/src/&lt;Project&gt;/bin/Release/netstandard2.1/&lt;Project&gt;.dll</c>.
///
/// The assembly set is an explicit whitelist — Core / Gameplay / Integration / UI, never the test
/// runner and never an arbitrary folder name (the R-E "validator assemblies" bug: a folder scan
/// used to count tools/ApiIndex.dll and tests as shipping assemblies).
///
/// Checks:
/// <list type="bullet">
/// <item>all four DLLs exist, their AssemblyName is the project name, and the version is real;</item>
/// <item>no external mod / Harmony / NuGet assembly reference in any of them;</item>
/// <item>layering (REQ-ARCH-02): Core references no SEE.* and no game/Unity assembly at all;
/// Gameplay never references Integration/UI; UI never references Gameplay/Integration;
/// Integration never references UI;</item>
/// <item>the two frozen ComponentSpecs exist with their [SpecAlias] JSON keys and exactly the
/// CONTRACT field list (the blueprint validator resolves those aliases).</item>
/// </list>
/// </summary>
internal sealed class AssemblyChecks
{
    private static readonly string[] ShippingAssemblies = { "SEE.Core", "SEE.Gameplay", "SEE.Integration", "SEE.UI" };

    private static readonly string[] ExternalModAssemblies =
    {
        "TimberPipes", "PowerLines", "ConveyorBelt", "ConveyorBelts", "Harmony", "HarmonyLib",
    };

    private static readonly string[] NuGetAssemblies =
    {
        "Newtonsoft.Json", "Mono.Cecil", "NUnit", "nunit.framework", "xunit", "FluentAssertions", "Moq",
        "AutoFixture", "System.Text.Json",
    };

    private const string ComponentSpec = "Timberborn.BlueprintSystem.ComponentSpec";

    private readonly string _modRoot;
    private readonly string _managedDir;
    private readonly Report _report;
    private readonly GameMetadata _metadata;
    private readonly Dictionary<string, AssemblyDefinition> _assemblies = new(StringComparer.Ordinal);

    public AssemblyChecks(string modRoot, string managedDir, GameMetadata metadata, Report report)
    {
        _modRoot = modRoot;
        _managedDir = managedDir;
        _metadata = metadata;
        _report = report;
    }

    public int LoadedAssemblyCount => _assemblies.Count;

    public void Validate()
    {
        Discover();

        foreach (var name in ShippingAssemblies)
        {
            if (!_assemblies.ContainsKey(name))
            {
                continue;
            }

            ValidateIdentity(name, _assemblies[name]);
        }

        if (_assemblies.Count == ShippingAssemblies.Length)
        {
            ValidateLayering();
            ValidateExternalReferences();
            ValidateSpecs();
        }
    }

    private void Discover()
    {
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(_managedDir);

        foreach (var project in ShippingAssemblies)
        {
            var path = FindAssembly(project);
            if (path is null)
            {
                _report.Error("assemblies", "shipping assembly " + project + ".dll was not built at "
                                            + "src/" + project + "/bin/Release/netstandard2.1/ (run build.ps1 before validate)");
                continue;
            }

            try
            {
                _assemblies[project] = AssemblyDefinition.ReadAssembly(
                    path, new ReaderParameters { AssemblyResolver = resolver });
            }
            catch (BadImageFormatException ex)
            {
                _report.Error("assemblies", "could not read " + Path.GetFileName(path) + ": " + ex.Message);
            }
        }
    }

    private string? FindAssembly(string project)
    {
        foreach (var configuration in new[] { "Release", "Debug" })
        {
            var candidate = Path.Combine(_modRoot, "src", project, "bin", configuration, "netstandard2.1", project + ".dll");
            if (!File.Exists(candidate))
            {
                continue;
            }

            if (configuration == "Debug")
            {
                _report.Warn("assemblies", project + ".dll was found only in Debug; the Release gate expects a Release build");
            }

            return candidate;
        }

        return null;
    }

    private void ValidateIdentity(string project, AssemblyDefinition assembly)
    {
        var name = assembly.Name.Name;
        if (!string.Equals(name, project, StringComparison.Ordinal))
        {
            _report.Error("assemblies", project + ".dll declares assembly name '" + name + "'; expected '" + project + "'");
        }

        var version = assembly.Name.Version;
        if (version is null || (version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision <= 0))
        {
            _report.Error("assemblies", project + ".dll has no meaningful assembly version");
        }
    }

    private void ValidateLayering()
    {
        ValidateReferences("SEE.Core", references =>
            references.Where(reference => reference.StartsWith("SEE.", StringComparison.Ordinal))
                .Select(reference => "SEE.Core must not reference the SEE assembly '" + reference + "'")
                .Concat(references
                    .Where(reference => reference.StartsWith("Timberborn.", StringComparison.Ordinal)
                                        || reference.StartsWith("UnityEngine", StringComparison.Ordinal)
                                        || reference.StartsWith("Bindito.", StringComparison.Ordinal))
                    .Select(reference => "SEE.Core must stay game-independent, found reference '" + reference + "'")));

        ValidateReferences("SEE.Gameplay", references =>
        {
            var problems = new List<string>();
            if (references.Contains("SEE.Integration", StringComparer.Ordinal))
            {
                problems.Add("SEE.Gameplay must not reference SEE.Integration (layering: Gameplay -> Core only)");
            }

            if (references.Contains("SEE.UI", StringComparer.Ordinal))
            {
                problems.Add("SEE.Gameplay must not reference SEE.UI (layering: Gameplay -> Core only)");
            }

            return problems;
        });

        ValidateReferences("SEE.Integration", references =>
            references.Contains("SEE.UI", StringComparer.Ordinal)
                ? new[] { "SEE.Integration must not reference SEE.UI (layering: Integration -> Core/Gameplay only)" }
                : Array.Empty<string>());

        ValidateReferences("SEE.UI", references =>
        {
            var problems = new List<string>();
            if (references.Contains("SEE.Gameplay", StringComparer.Ordinal))
            {
                problems.Add("SEE.UI must not reference SEE.Gameplay (UI reads SEE.Core contracts only)");
            }

            if (references.Contains("SEE.Integration", StringComparer.Ordinal))
            {
                problems.Add("SEE.UI must not reference SEE.Integration (UI reads SEE.Core contracts only)");
            }

            if (!references.Contains("SEE.Core", StringComparer.Ordinal))
            {
                _report.Warn("assemblies", "SEE.UI does not reference SEE.Core; check the UI project layout");
            }

            return problems;
        });
    }

    private void ValidateReferences(string project, Func<List<string>, IEnumerable<string>> checker)
    {
        if (!_assemblies.TryGetValue(project, out var assembly))
        {
            return;
        }

        var references = assembly.MainModule.AssemblyReferences
            .Select(reference => reference.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        foreach (var problem in checker(references))
        {
            _report.Error("assemblies", problem);
        }
    }

    private void ValidateExternalReferences()
    {
        foreach (var pair in _assemblies)
        {
            var references = pair.Value.MainModule.AssemblyReferences.Select(reference => reference.Name).ToList();
            foreach (var reference in references)
            {
                foreach (var external in ExternalModAssemblies)
                {
                    if (reference.StartsWith(external, StringComparison.OrdinalIgnoreCase))
                    {
                        _report.Error("assemblies", pair.Key + " must not reference the external assembly '" + reference
                                                    + "' at compile time (external mods are detected at runtime only)");
                    }
                }

                foreach (var nuget in NuGetAssemblies)
                {
                    if (string.Equals(reference, nuget, StringComparison.Ordinal)
                        || reference.StartsWith(nuget + ".", StringComparison.Ordinal))
                    {
                        _report.Error("assemblies", pair.Key + " must not reference the NuGet assembly '" + reference
                                                    + "' (zero NuGet dependencies)");
                    }
                }
            }
        }
    }

    private void ValidateSpecs()
    {
        var gameplay = _assemblies["SEE.Gameplay"];
        var types = GameMetadata.EnumerateTypes(gameplay).ToList();
        ValidateSpec(
            types,
            ContractSpecs.BoilerTypeName,
            ContractSpecs.BoilerKey,
            ContractSpecs.BoilerFields,
            new Dictionary<string, PrimitiveKind>(StringComparer.Ordinal)
            {
                ["Tier"] = PrimitiveKind.String,
                ["Size"] = PrimitiveKind.String,
                ["Faction"] = PrimitiveKind.String,
                ["Horsepower"] = PrimitiveKind.Number,
                ["FuelPerHour"] = PrimitiveKind.Number,
                ["WaterPerHour"] = PrimitiveKind.Number,
                ["FuelCapacityGoods"] = PrimitiveKind.Number,
                ["WaterCapacityGoods"] = PrimitiveKind.Number,
                ["FuelScaling"] = PrimitiveKind.String,
                ["AutoMaintenanceDefault"] = PrimitiveKind.Boolean,
            },
            new[] { "FuelGoodIds", "FuelValues" });

        ValidateSpec(
            types,
            ContractSpecs.MaintenanceTypeName,
            ContractSpecs.MaintenanceKey,
            ContractSpecs.MaintenanceFields,
            new Dictionary<string, PrimitiveKind>(StringComparer.Ordinal)
            {
                ["Enabled"] = PrimitiveKind.Boolean,
                ["IntervalDays"] = PrimitiveKind.Number,
                ["LoadFloor"] = PrimitiveKind.Number,
                ["LoadFactorRange"] = PrimitiveKind.Number,
                ["TriggerThreshold"] = PrimitiveKind.Number,
                ["DurationHours"] = PrimitiveKind.Number,
            },
            new[] { "Materials" });
    }

    private void ValidateSpec(
        List<TypeDefinition> types,
        string typeName,
        string alias,
        string[] expectedFields,
        IReadOnlyDictionary<string, PrimitiveKind> primitiveFields,
        IReadOnlyList<string> collectionFields)
    {
        var spec = types.FirstOrDefault(type => string.Equals(type.Name, typeName, StringComparison.Ordinal));
        if (spec is null)
        {
            _report.Error("assemblies", "SEE.Gameplay.dll does not declare the frozen ComponentSpec '" + typeName
                                        + "' (the blueprint JSON top-level key '" + alias + "' resolves through its [SpecAlias])");
            return;
        }

        if (!spec.IsPublic || spec.IsAbstract || spec.IsInterface)
        {
            _report.Error("assemblies", spec.FullName + " must be a public concrete class");
        }

        if (!_metadata.IsAssignableTo(spec, ComponentSpec))
        {
            _report.Error("assemblies", spec.FullName + " must derive from " + ComponentSpec);
        }

        var aliases = GameMetadata.ReadAliases(spec);
        if (!aliases.Contains(alias, StringComparer.Ordinal))
        {
            _report.Error("assemblies", spec.FullName + " must carry [SpecAlias(\"" + alias + "\")]");
        }

        var serialized = _metadata.GetSerializedMemberNames(spec);
        var expected = expectedFields.OrderBy(name => name, StringComparer.Ordinal).ToList();
        if (!serialized.SequenceEqual(expected, StringComparer.Ordinal))
        {
            var missing = expected.Except(serialized, StringComparer.Ordinal).ToList();
            var extra = serialized.Except(expected, StringComparer.Ordinal).ToList();
            _report.Error("assemblies", spec.FullName + " [Serialize] members must equal the frozen CONTRACT list"
                                        + (missing.Count > 0 ? "; missing: " + string.Join(", ", missing) : string.Empty)
                                        + (extra.Count > 0 ? "; unexpected: " + string.Join(", ", extra) : string.Empty));
        }

        var members = _metadata.GetMembers(spec);
        foreach (var pair in primitiveFields)
        {
            if (!members.TryGetValue(pair.Key, out var member))
            {
                continue;
            }

            if (!GameMetadata.TryGetPrimitiveKind(member.Type, out var kind) || kind != pair.Value)
            {
                _report.Error("assemblies", spec.FullName + "." + pair.Key + " must be " + pair.Value
                                            + ", found " + member.Type.FullName);
            }
        }

        foreach (var field in collectionFields)
        {
            if (!members.TryGetValue(field, out var member))
            {
                continue;
            }

            if (!GameMetadata.IsCollection(member.Type, out _))
            {
                _report.Error("assemblies", spec.FullName + "." + field + " must be a collection, found "
                                            + member.Type.FullName);
            }
        }

        // SEEMaintenanceSpec.Materials is the vanilla GoodAmountSpec ({Id, Amount}); the blueprint
        // data writes those two keys, so the element shape is part of the frozen contract.
        if (string.Equals(typeName, ContractSpecs.MaintenanceTypeName, StringComparison.Ordinal)
            && members.TryGetValue("Materials", out var materials)
            && GameMetadata.IsCollection(materials.Type, out var elementType))
        {
            var element = _metadata.Resolve(elementType);
            if (element is null)
            {
                _report.Warn("assemblies", "could not resolve the Materials element type '" + elementType.FullName
                                           + "'; expected the vanilla GoodAmountSpec with Id/Amount");
            }
            else
            {
                var elementMembers = _metadata.GetMembers(element);
                if (elementMembers.Count > 0
                    && (!elementMembers.ContainsKey("Id") || !elementMembers.ContainsKey("Amount")))
                {
                    _report.Error("assemblies", "Materials element " + element.FullName
                                                + " must expose the vanilla GoodAmountSpec members Id and Amount");
                }
            }
        }
    }
}
