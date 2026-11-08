using System.Globalization;

namespace SEE.Tools.GenerateBlueprints;

/// <summary>
/// SEE blueprint generator entry point.
///
/// Seven steps (requirements 11.3 / CONTRACT_SEE 4.10): load and validate engines.json, re-derive
/// every rate and capacity, expand both factions, build the 24 blueprints, run the five gates, emit
/// the derived table and the golden hash, then either write or verify (<c>--check</c>) every byte.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (GeneratorException exception)
        {
            Console.Error.WriteLine("GENERATE FAILED");
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("GENERATE FAILED (unexpected)");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        var options = Options.Parse(args);
        if (options.Help)
        {
            PrintUsage();
            return 0;
        }

        var modRoot = FindModRoot(options.ModRoot);
        var dataDir = Path.Combine(modRoot, "data");
        var enginesPath = Path.Combine(dataDir, "engines.json");
        var catalog = CatalogLoader.Load(enginesPath);
        var rows = Derivation.Expand(catalog);
        var variants = Derivation.Variants(rows);

        var trees = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var variant in variants)
        {
            var tree = BlueprintFactory.Build(variant, catalog);
            trees[variant.GeneratedRelativePath] = tree;
            texts[variant.GeneratedRelativePath] = BlueprintFactory.Serialize(tree);
        }

        var gates = Gates.Run(catalog, rows, texts);
        var verification = Verification.Verify(variants, trees);

        var files = new List<GeneratedFile>(79);
        var blueprintFiles = new List<GeneratedFile>(24);
        foreach (var variant in variants)
        {
            var file = new GeneratedFile(variant.GeneratedRelativePath, Outputs.Utf8(texts[variant.GeneratedRelativePath]));
            files.Add(file);
            blueprintFiles.Add(file);
        }

        foreach (var faction in catalog.Factions)
        {
            var factionVariants = variants
                .Where(variant => string.Equals(variant.Faction.Name, faction.Name, StringComparison.Ordinal))
                .ToList();
            var collection = Outputs.TemplateCollection("Buildings." + faction.Name, factionVariants);
            files.Add(new GeneratedFile(
                "data/TemplateCollections/TemplateCollection.Buildings." + faction.Name + ".blueprint.json",
                Outputs.Utf8(JsonText.Serialize(collection))));
        }

        var locRows = Localization.Build(catalog, Path.Combine(dataDir, "loc_keys.json"));
        files.Add(new GeneratedFile("data/Localizations/enUS.csv", Outputs.Utf8(Localization.ToCsv(locRows, "enUS"))));
        files.Add(new GeneratedFile("data/Localizations/zhTW.csv", Outputs.Utf8(Localization.ToCsv(locRows, "zhTW"))));
        files.Add(new GeneratedFile("data/Localizations/zhCN.csv", Outputs.Utf8(Localization.ToCsv(locRows, "zhCN"))));

        foreach (var variant in variants)
        {
            var sizeIndex = Array.IndexOf(CatalogLoader.Sizes, variant.Engine.Size);
            var tierIndex = Array.IndexOf(CatalogLoader.Tiers, variant.Engine.Tier);
            var ironTeeth = string.Equals(variant.Faction.Name, "IronTeeth", StringComparison.Ordinal);
            // P9 ingest hook: a rendered sprite under <mod root>/art/icons/ wins over the placeholder.
            var artIconPath = Path.Combine(modRoot, "art", "icons", variant.IconName + ".png");
            var icon = Icons.Render(variant, sizeIndex, tierIndex, ironTeeth, artIconPath);
            files.Add(new GeneratedFile("data/Icons/" + variant.IconName + ".png", icon));
            files.Add(new GeneratedFile("data/Icons/" + variant.IconName + ".png.meta.json", Icons.MetaJson()));
        }

        var sourceSha256 = Outputs.Sha256(File.ReadAllBytes(enginesPath));
        var derived = Outputs.DerivedJson(catalog, rows, gates, sourceSha256, verification);
        files.Add(new GeneratedFile("data/engines.derived.json", Outputs.Utf8(JsonText.Serialize(derived))));
        files.Add(new GeneratedFile(Outputs.GoldenPath, Outputs.Utf8(Outputs.GoldenHashes(blueprintFiles))));
        files.Add(new GeneratedFile("data/Buildings/Power/SteamBoiler/README.md", Outputs.Utf8(Outputs.BlueprintReadme())));

        PrintReport(modRoot, catalog, rows, gates, verification, locRows.Count, files.Count);

        if (options.Check)
        {
            return Check(modRoot, files, blueprintFiles);
        }

        WriteAll(modRoot, files, blueprintFiles);
        Console.WriteLine("WROTE " + files.Count + " files; GENERATE PASS");
        return 0;
    }

    private static void PrintReport(
        string modRoot,
        Catalog catalog,
        IReadOnlyList<DerivedRow> rows,
        IReadOnlyList<GateResult> gates,
        VerificationResult verification,
        int locCount,
        int fileCount)
    {
        Console.WriteLine("SEE blueprint generator");
        Console.WriteLine("  mod root     : " + modRoot);
        Console.WriteLine("  engines.json : " + catalog.Engines.Count + " rows (dataVersion " + catalog.DataVersion + ")");
        Console.WriteLine("  factions     : " + string.Join(", ", catalog.Factions.Select(faction =>
            faction.Name + " output x" + Show(faction.OutputMultiplier) + " fuel x" + Show(faction.FuelMultiplier))));
        Console.WriteLine("  tool orders  : " + catalog.ToolOrders.Count + " entries, range 110-142");
        Console.WriteLine("  blueprints   : " + (rows.Count * catalog.Factions.Count) + " (12 size/tier x 2 factions)");
        Console.WriteLine("  gates        : " + gates.Count(gate => gate.Pass) + "/" + gates.Count + " PASS");
        foreach (var gate in gates)
        {
            Console.WriteLine("    [" + gate.Id + "] " + (gate.Pass ? "PASS" : "FAIL") + " " + gate.Name + ": " + gate.Detail);
        }

        Console.WriteLine("  fields       : " + verification.Files + "/" + verification.Files + " files x "
                          + verification.FieldsPerFile + "/" + verification.FieldsPerFile
                          + " authoritative fields PASS (" + verification.ExtraChecks + " extra checks)");
        Console.WriteLine("  localization : " + locCount + " keys x 3 languages (enUS, zhTW, zhCN)");
        Console.WriteLine("  icons        : " + (rows.Count * catalog.Factions.Count) + " png + "
                          + (rows.Count * catalog.Factions.Count) + " meta");
        Console.WriteLine("  golden hash  : " + (rows.Count * catalog.Factions.Count) + " entries");
        Console.WriteLine("  files        : " + fileCount);
    }

    private static int Check(string modRoot, IReadOnlyList<GeneratedFile> files, IReadOnlyList<GeneratedFile> blueprintFiles)
    {
        var differences = new List<string>();
        foreach (var file in files)
        {
            var path = AbsolutePath(modRoot, file.RelativePath);
            if (!File.Exists(path))
            {
                differences.Add("missing: " + file.RelativePath);
                continue;
            }

            var actual = File.ReadAllBytes(path);
            if (actual.AsSpan().SequenceEqual(file.Bytes))
            {
                continue;
            }

            if (actual.Length != file.Bytes.Length)
            {
                differences.Add("size differs: " + file.RelativePath + " (disk " + actual.Length
                                + " bytes, expected " + file.Bytes.Length + " bytes)");
                continue;
            }

            var index = FirstDifference(actual, file.Bytes);
            differences.Add("content differs: " + file.RelativePath + " at byte " + index);
        }

        var expected = new HashSet<string>(blueprintFiles.Select(file => file.RelativePath), StringComparer.Ordinal);
        var blueprintDirectory = Path.Combine(modRoot, "data", "Buildings", "Power", "SteamBoiler");
        if (Directory.Exists(blueprintDirectory))
        {
            foreach (var path in Directory.GetFiles(blueprintDirectory, "*.blueprint.json"))
            {
                var relative = Path.GetRelativePath(modRoot, path).Replace('\\', '/');
                if (!expected.Contains(relative))
                {
                    differences.Add("unexpected blueprint file: " + relative);
                }
            }
        }

        if (differences.Count > 0)
        {
            Console.Error.WriteLine("CHECK FAILED (" + differences.Count + " difference(s)):");
            foreach (var difference in differences.Take(40))
            {
                Console.Error.WriteLine("  - " + difference);
            }

            return 1;
        }

        Console.WriteLine("CHECK PASS (0 differences, " + files.Count + " files, 24 blueprints)");
        return 0;
    }

    private static void WriteAll(string modRoot, IReadOnlyList<GeneratedFile> files, IReadOnlyList<GeneratedFile> blueprintFiles)
    {
        var expected = new HashSet<string>(blueprintFiles.Select(file => file.RelativePath), StringComparer.Ordinal);
        var expectedIcons = new HashSet<string>(files
            .Where(file => file.RelativePath.StartsWith("data/Icons/", StringComparison.Ordinal))
            .Select(file => file.RelativePath), StringComparer.Ordinal);

        var blueprintDirectory = Path.Combine(modRoot, "data", "Buildings", "Power", "SteamBoiler");
        if (Directory.Exists(blueprintDirectory))
        {
            foreach (var path in Directory.GetFiles(blueprintDirectory, "*.blueprint.json"))
            {
                var relative = Path.GetRelativePath(modRoot, path).Replace('\\', '/');
                if (!expected.Contains(relative))
                {
                    File.Delete(path);
                    Console.WriteLine("  removed stale file: " + relative);
                }
            }
        }

        var iconDirectory = Path.Combine(modRoot, "data", "Icons");
        if (Directory.Exists(iconDirectory))
        {
            foreach (var path in Directory.GetFiles(iconDirectory))
            {
                var relative = Path.GetRelativePath(modRoot, path).Replace('\\', '/');
                if (Path.GetFileName(path).StartsWith("SteamBoiler", StringComparison.Ordinal)
                    && !expectedIcons.Contains(relative))
                {
                    File.Delete(path);
                    Console.WriteLine("  removed stale file: " + relative);
                }
            }
        }

        foreach (var file in files)
        {
            var path = AbsolutePath(modRoot, file.RelativePath);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, file.Bytes);
        }
    }

    private static string AbsolutePath(string modRoot, string relativePath) =>
        Path.Combine(modRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static int FirstDifference(byte[] actual, byte[] expected)
    {
        var limit = Math.Min(actual.Length, expected.Length);
        for (var index = 0; index < limit; index++)
        {
            if (actual[index] != expected[index])
            {
                return index;
            }
        }

        return limit;
    }

    private static string FindModRoot(string? explicitRoot)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            candidates.Add(Path.GetFullPath(explicitRoot));
        }

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            candidates.AddRange(Ancestors(start));
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(Path.Combine(candidate, "data", "engines.json"))
                && Directory.Exists(Path.Combine(candidate, "tools")))
            {
                return candidate;
            }
        }

        throw new GeneratorException("could not locate the mod root (a folder with data/engines.json); "
                                     + "pass --mod <modRoot>");
    }

    private static IEnumerable<string> Ancestors(string start)
    {
        DirectoryInfo? directory;
        try
        {
            directory = new DirectoryInfo(Path.GetFullPath(start));
        }
        catch (Exception)
        {
            yield break;
        }

        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    private static string Show(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static void PrintUsage()
    {
        Console.WriteLine("usage: SeeGenerateBlueprints [--check] [--mod <modRoot>]");
        Console.WriteLine();
        Console.WriteLine("  (no flag)  regenerate the 24 blueprints, template collections, localizations,");
        Console.WriteLine("             icons, data/engines.derived.json and the golden hash file");
        Console.WriteLine("  --check    rebuild everything in memory and fail on the first byte difference");
        Console.WriteLine("  --mod      explicit mod root (default: walk up from the current directory)");
    }

    private sealed record Options(bool Check, bool Help, string? ModRoot)
    {
        public static Options Parse(string[] args)
        {
            var check = false;
            var help = false;
            string? modRoot = null;
            for (var index = 0; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--check":
                        check = true;
                        break;
                    case "--help" or "-h":
                        help = true;
                        break;
                    case "--mod":
                        if (index + 1 >= args.Length)
                        {
                            throw new GeneratorException("--mod requires a folder argument");
                        }

                        modRoot = args[++index];
                        break;
                    case "--":
                    case "--nologo":
                        break;
                    default:
                        // `dotnet run` forwards unparsed MSBuild switches (for example -m:1, which
                        // the repository build rule requires) to the application; they are not ours.
                        if (args[index].StartsWith("-m:", StringComparison.Ordinal)
                            || args[index].StartsWith("-p:", StringComparison.Ordinal)
                            || args[index].StartsWith("-v:", StringComparison.Ordinal)
                            || args[index] == "-v")
                        {
                            break;
                        }

                        throw new GeneratorException("unknown argument '" + args[index] + "' (see --help)");
                }
            }

            return new Options(check, help, modRoot);
        }
    }
}
