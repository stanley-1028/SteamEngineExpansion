using System.Text.RegularExpressions;
using Mono.Cecil;

namespace SEE.Tools.Validate;

/// <summary>
/// SEE data validator. Usage (from the project root):
///
///   dotnet run --project mod\tools\validate -c Release -m:1 --
///       mod\data --managed "&lt;game&gt;\Timberborn_Data\Managed" --mod mod
///
/// Exit codes: 0 = data/ is valid and the report ends with VALIDATION PASSED, 1 = problems,
/// 2 = cannot even start (bad paths).
///
/// Gate groups (each prints one PASS/FAIL line; build.ps1 matches them against
/// tools/release/release-gates.json): manifest, engines.json, blueprint spec schema, the 24 x 12
/// authoritative-field table, fuel/water capacity derivation, boiler blueprint rules, the 63-key
/// localization set (54-key requirements baseline + 9 P4B additions), config schema and clamp
/// ranges, the golden hash, the forbidden static scan,
/// the assembly whitelist/layering/spec shape, and the source facts.
///
/// Development helper: <c>--fixture-add-reference &lt;in.dll&gt; &lt;out.dll&gt; &lt;assembly&gt;</c>
/// rewrites a copy of an assembly with one extra assembly reference. It exists only so
/// tools/validate/negative-tests.ps1 can prove that the layering gate rejects a UI assembly that
/// references SEE.Gameplay; build.ps1 never calls it.
/// </summary>
internal static class Program
{
    internal const string FixtureAddReferenceValue = "--fixture-add-reference";

    /// <summary>Explicit shipping whitelist (R-E: never a folder scan, never the test runner).</summary>
    private static readonly string[] ShippingAssemblies =
    {
        "SEE.Core", "SEE.Gameplay", "SEE.Integration", "SEE.UI",
    };

    private static int Main(string[] args)
    {
        if (Array.IndexOf(args, FixtureAddReferenceValue) >= 0)
        {
            return FixtureTools.AddAssemblyReference(args);
        }

        string? dataArgument = null;
        string? managedArgument = null;
        string? modArgument = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help" or "-h":
                    PrintUsage();
                    return 0;
                case "--managed":
                    managedArgument = i + 1 < args.Length ? args[++i] : null;
                    break;
                case "--mod":
                    modArgument = i + 1 < args.Length ? args[++i] : null;
                    break;
                case "--":
                    break;
                default:
                    if (args[i].StartsWith('-'))
                    {
                        break;
                    }

                    if (dataArgument is null)
                    {
                        dataArgument = args[i];
                    }
                    else
                    {
                        Console.Error.WriteLine("unexpected argument: " + args[i]);
                        PrintUsage();
                        return 2;
                    }

                    break;
            }
        }

        var dataDir = Path.GetFullPath(dataArgument ?? "data");
        if (!Directory.Exists(dataDir))
        {
            Console.Error.WriteLine("data directory not found: " + dataDir);
            return 2;
        }

        var modRoot = Path.GetFullPath(modArgument ?? GuessModRoot(dataDir));
        var managedDir = ResolveManagedDir(modRoot, managedArgument);
        if (managedDir is null || !Directory.Exists(managedDir))
        {
            Console.Error.WriteLine(
                "could not find the Timberborn managed assemblies. Pass --managed <game>\\Timberborn_Data\\Managed "
                + "or set TIMBERBORN_MANAGED_DIR. Tried: " + (managedDir ?? "(none)"));
            return 2;
        }

        var report = new Report();
        var modAssemblies = DiscoverShippingAssemblies(modRoot, report);
        var metadata = GameMetadata.Load(managedDir, modAssemblies, report);
        var game = GameBlueprintCatalog.Load(managedDir, report);

        var blueprintFiles = Directory
            .GetFiles(dataDir, "*.blueprint.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Console.WriteLine("SEE data validator");
        Console.WriteLine("  data dir        : " + dataDir);
        Console.WriteLine("  mod root        : " + modRoot);
        Console.WriteLine("  managed dir     : " + managedDir);
        Console.WriteLine("  game assemblies : " + metadata.GameAssemblyCount);
        Console.WriteLine("  spec types      : " + metadata.SpecTypeCount + " (ComponentSpec subclasses + aliases)");
        Console.WriteLine("  mod assemblies  : " + metadata.ModAssemblyCount
                          + (modAssemblies.Count > 0
                              ? " (" + string.Join(", ", modAssemblies.Select(Path.GetFileName)) + ")"
                              : " (shipping assemblies not built yet)"));
        Console.WriteLine("  vanilla models  : " + (game.Available
            ? game.BlueprintCount + " blueprints indexed from Blueprints.zip"
            : "NOT INDEXED (model path checks downgraded to warnings)"));
        Console.WriteLine("  localizations   : " + LocKeys.ExpectedCount + " canonical keys required");
        Console.WriteLine("  blueprint files : " + blueprintFiles.Count);
        Console.WriteLine();

        // ---- data package checks -------------------------------------------------------------
        report.Check("manifest.json", () =>
            new ManifestChecks(dataDir, report).Validate());

        var engines = new EnginesChecks(dataDir, report);
        report.Check("engines.json (12 rows)", engines.Validate);
        Console.WriteLine("      " + engines.EngineCount + " rows, dataVersion " + engines.DataVersion);

        var blueprintValidator = new BlueprintValidator(metadata, report, ContractSpecs.Fallback);
        report.Check("blueprint spec schema", () =>
        {
            foreach (var file in blueprintFiles)
            {
                var relative = Path.GetRelativePath(dataDir, file).Replace('\\', '/');
                blueprintValidator.ValidateFile(file, relative);
            }
        });
        Console.WriteLine("      " + blueprintFiles.Count + " documents (24 boilers + 2 template collections) validated "
                          + "against the game and SEE spec metadata");

        var table = new BlueprintTableChecks(dataDir, report, game);
        report.Check("blueprint table (24 x 12 authoritative fields)", table.Validate);
        Console.WriteLine("      " + table.FilesValidated + "/24 files x " + BlueprintTableChecks.AuthoritativeFieldCount
                          + "/" + BlueprintTableChecks.AuthoritativeFieldCount + " fields, " + table.Mismatches
                          + " mismatch(es) against engines.json + engines.derived.json");

        report.Check("fuel/water capacity derivation", () =>
            new FuelCapacityChecks(dataDir, report).Validate());

        report.Check("boiler blueprint rules (AD-9 / AD-5)", () =>
            new BoilerBlueprintChecks(dataDir, report).Validate());

        report.Check("localization (63 keys >= 54, enUS/zhTW/zhCN)", () =>
            new Localizations(report).Validate(dataDir, blueprintValidator.UsedLocKeys));

        report.Check("config schema and clamp", () =>
            new ConfigValidator(report).Validate(dataDir));

        var golden = new GoldenChecks(modRoot, dataDir, report);
        report.Check("golden hash (24 blueprints)", golden.Validate);
        Console.WriteLine("      " + golden.EntriesChecked + "/" + GoldenChecks.ExpectedCount
                          + " golden entries verified against tests/SEE.Tests/Golden/blueprints.sha256");

        var scan = new StaticScanChecks(modRoot, dataDir, report);
        report.Check("forbidden static scan", () =>
        {
            scan.ValidateSource();
            scan.ValidateData();
        });
        Console.WriteLine("      " + scan.SourceFilesScanned + " source files, " + scan.DataDocumentsScanned
                          + " data documents, " + scan.TemplateNamesSeen + " TemplateName declarations, "
                          + scan.Hits + " red-line hit(s)");

        // ---- assembly checks (shipping whitelist only) ---------------------------------------
        report.Check("assembly whitelist and layering", () =>
            new AssemblyChecks(modRoot, managedDir, metadata, report).Validate());

        SourceFacts? facts = null;
        report.Check("source facts", () =>
            facts = SourceFacts.Load(modRoot, report));
        if (facts is not null)
        {
            Console.WriteLine("      " + facts.ScannedFileCount + " source files, " + facts.SourceLineCount
                              + " C# lines, " + facts.NotImplementedFiles + " stub exception file(s)");
        }

        return report.Finish(Console.Out);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("usage: SeeValidate [dataDir] [--managed <Timberborn_Data\\Managed>] [--mod <modRoot>]");
        Console.WriteLine("       SeeValidate " + FixtureAddReferenceValue + " <in.dll> <out.dll> <assemblyName>");
        Console.WriteLine();
        Console.WriteLine("Validates the shippable mod data folder against the installed Timberborn metadata;");
        Console.WriteLine("defaults: dataDir = 'data', modRoot = parent of dataDir.");
    }

    private static string GuessModRoot(string dataDir)
    {
        var parent = Directory.GetParent(dataDir);
        if (parent is not null && string.Equals(Path.GetFileName(dataDir), "data", StringComparison.OrdinalIgnoreCase))
        {
            return parent.FullName;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string? ResolveManagedDir(string modRoot, string? explicitManagedDir)
    {
        if (!string.IsNullOrWhiteSpace(explicitManagedDir))
        {
            return Path.GetFullPath(explicitManagedDir);
        }

        var fromEnvironment = Environment.GetEnvironmentVariable("TIMBERBORN_MANAGED_DIR");
        if (!string.IsNullOrWhiteSpace(fromEnvironment) && Directory.Exists(fromEnvironment))
        {
            return Path.GetFullPath(fromEnvironment);
        }

        var propsPath = Path.Combine(modRoot, "Directory.Build.props");
        if (File.Exists(propsPath))
        {
            var match = Regex.Match(File.ReadAllText(propsPath),
                @"<TimberbornDir[^>]*>([^<]+)</TimberbornDir>", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var candidate = Path.Combine(match.Groups[1].Value.Trim(), "Timberborn_Data", "Managed");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam", "steamapps", "common", "Timberborn", "Timberborn_Data", "Managed");
    }

    /// <summary>Shipping assemblies from the explicit whitelist; the test runner is never included (R-E).</summary>
    private static List<string> DiscoverShippingAssemblies(string modRoot, Report report)
    {
        var assemblies = new List<string>();
        foreach (var project in ShippingAssemblies)
        {
            var found = false;
            foreach (var configuration in new[] { "Release", "Debug" })
            {
                var candidate = Path.Combine(modRoot, "src", project, "bin", configuration, "netstandard2.1",
                    project + ".dll");
                if (!File.Exists(candidate))
                {
                    continue;
                }

                if (configuration == "Debug")
                {
                    report.Warn("assemblies", project + ".dll was found only in Debug; the Release gate expects Release");
                }

                assemblies.Add(candidate);
                found = true;
                break;
            }

            if (!found)
            {
                report.Warn("assemblies", project + ".dll is not built yet; spec aliases resolve from the CONTRACT "
                                           + "fallback list only");
            }
        }

        return assemblies;
    }
}

/// <summary>
/// Development-only fixture helper used by <c>tools/validate/negative-tests.ps1</c>: writes a copy
/// of an assembly with one extra assembly reference, so the layering gate can be tested against a
/// real "SEE.UI references SEE.Gameplay" violation instead of a hand-edited JSON file.
/// </summary>
internal static class FixtureTools
{
    public static int AddAssemblyReference(string[] args)
    {
        var index = Array.IndexOf(args, Program.FixtureAddReferenceValue);
        if (index < 0 || index + 3 >= args.Length)
        {
            Console.Error.WriteLine("usage: SeeValidate " + Program.FixtureAddReferenceValue
                                    + " <in.dll> <out.dll> <assemblyName>");
            return 2;
        }

        var input = Path.GetFullPath(args[index + 1]);
        var output = Path.GetFullPath(args[index + 2]);
        var assemblyName = args[index + 3];

        if (!File.Exists(input))
        {
            Console.Error.WriteLine("input assembly not found: " + input);
            return 2;
        }

        try
        {
            using var definition = AssemblyDefinition.ReadAssembly(input,
                new ReaderParameters { ReadingMode = ReadingMode.Immediate, InMemory = true });
            if (!definition.MainModule.AssemblyReferences.Any(reference =>
                    string.Equals(reference.Name, assemblyName, StringComparison.Ordinal)))
            {
                definition.MainModule.AssemblyReferences.Add(
                    new AssemblyNameReference(assemblyName, new Version(1, 0, 0, 0)));
            }

            var directory = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            definition.Write(output);
            Console.WriteLine("fixture: wrote " + output + " with the synthetic reference " + assemblyName);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("fixture failed: " + exception.GetType().Name + ": " + exception.Message);
            return 1;
        }
    }
}
