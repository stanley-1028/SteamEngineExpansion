using SEE.Tools.BalanceSim.Analysis;
using SEE.Tools.BalanceSim.Data;
using SEE.Tools.BalanceSim.Estimation;
using SEE.Tools.BalanceSim.Report;

namespace SEE.Tools.BalanceSim;

/// <summary>
/// SEE P7 entry point: loads the two authoritative data files, drives the shared SEE.Core
/// formulas, and prints the §5.5 30-day simulation, the four §5.2 criteria, the §5.1 A/B table and
/// the offline save/performance estimate.
///
/// The tool is read-only with respect to the workspace: exit code 0 means all four criteria pass;
/// any FAIL (or a data cross-check mismatch) means non-zero.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            Options options = Options.Parse(args);
            if (options.Help)
            {
                PrintUsage();
                return 0;
            }

            string modRoot = FindModRoot(options.ModRoot);
            Console.WriteLine("SEE P7 offline balance + performance simulator");
            Fmt.Info("mod root", modRoot);
            Fmt.Info("scope", "offline evidence only; on-device Profiler / 30-day run is the P8 human gate");

            LoadedData data = CatalogLoader.Load(modRoot);
            BalanceReport.PrintDataSummary(data);
            AnalysisResult analysis = BalanceAnalysis.Run(data);
            BalanceReport.PrintDerivedTable(analysis);
            BalanceReport.PrintCriterion1(analysis);
            BalanceReport.PrintCriterion2(analysis);
            BalanceReport.PrintCriterion3(analysis);
            BalanceReport.PrintCriterion4(analysis);
            BalanceReport.PrintAbComparison(analysis);
            BalanceReport.PrintFactionQuantization(analysis);

            SaveEstimateResult save = SaveEstimate.Run();
            PerfEstimateResult perf = PerfEstimate.Run();
            PerfReport.PrintSaveEstimate(save);
            PerfReport.PrintPerfEstimate(perf);
            BalanceReport.PrintMatrix(analysis);
            return PerfReport.PrintVerdict(data, analysis);
        }
        catch (BalanceSimException exception)
        {
            Console.Error.WriteLine("BALANCE SIM FAILED");
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("BALANCE SIM FAILED (unexpected)");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static string FindModRoot(string? explicitRoot)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            candidates.Add(Path.GetFullPath(explicitRoot));
        }

        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            candidates.AddRange(Ancestors(start));
        }

        foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(Path.Combine(candidate, "data", "engines.json"))
                && File.Exists(Path.Combine(candidate, "data", "engines.derived.json")))
            {
                return candidate;
            }
        }

        throw new BalanceSimException(
            "could not locate the mod root (a folder with data/engines.json and "
            + "data/engines.derived.json); pass --mod <modRoot>");
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

    private static void PrintUsage()
    {
        Console.WriteLine("usage: SeeBalanceSim [--mod <modRoot>] [--help]");
        Console.WriteLine();
        Console.WriteLine("  (no flag)  run the 12-row balance simulation, the four §5.2 criteria, the §5.1");
        Console.WriteLine("             A/B table and the offline 1,000-building save/performance estimate");
        Console.WriteLine("  --mod      explicit mod root (default: walk up from the current directory)");
        Console.WriteLine("  --help     show this text");
        Console.WriteLine();
        Console.WriteLine("  Exit code 0 = all criteria PASS; any FAIL = non-zero.");
    }

    private sealed class Options
    {
        public bool Help { get; init; }

        public string? ModRoot { get; init; }

        public static Options Parse(string[] args)
        {
            bool help = false;
            string? modRoot = null;
            for (int index = 0; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--help" or "-h":
                        help = true;
                        break;
                    case "--mod":
                        if (index + 1 >= args.Length)
                        {
                            throw new BalanceSimException("--mod requires a folder argument");
                        }

                        modRoot = args[++index];
                        break;
                    case "--":
                    case "--nologo":
                        break;
                    default:
                        // `dotnet run` forwards unparsed MSBuild switches (for example -m:1, which the
                        // repository build rule requires) to the application; they are not ours.
                        if (args[index].StartsWith("-m:", StringComparison.Ordinal)
                            || args[index].StartsWith("-p:", StringComparison.Ordinal)
                            || args[index].StartsWith("-v:", StringComparison.Ordinal)
                            || args[index] == "-v")
                        {
                            break;
                        }

                        throw new BalanceSimException("unknown argument '" + args[index] + "' (see --help)");
                }
            }

            return new Options { Help = help, ModRoot = modRoot };
        }
    }
}
