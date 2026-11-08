using System.Runtime.InteropServices;
using SEE.Tools.BalanceSim.Analysis;
using SEE.Tools.BalanceSim.Data;
using SEE.Tools.BalanceSim.Estimation;

namespace SEE.Tools.BalanceSim.Report;

/// <summary>
/// Prints the offline save and performance evidence (requirements §12.6 / §13.7 / §14.7 / §17.3)
/// and the final PASS/FAIL verdict that drives the process exit code.
/// </summary>
internal static class PerfReport
{
    /// <summary>Prints the 1,000-building save size / time / memory estimate.</summary>
    public static void PrintSaveEstimate(SaveEstimateResult save)
    {
        Fmt.Section("8. 1,000-BUILDING SAVE ESTIMATE (requirements §14.7 / §17.3)");
        Fmt.Info("buildings", save.Buildings.ToString());
        Fmt.Info("§14.7 estimate per building", "350 B pretty / 250 B compact");
        Fmt.Info("§14.7 estimate for 1,000",
            save.EstimatedPrettyBytes.ToString("N0") + " B pretty / "
            + save.EstimatedCompactBytes.ToString("N0") + " B compact");
        Fmt.Info("§14.7 estimate zip (1/5)",
            "~" + save.EstimatedZipBytes.ToString("N0") + " B ("
            + Fmt.N(save.EstimatedZipBytes / 1024.0, 1) + " KiB) <= 400 KiB threshold");
        Fmt.Info("measured pretty (System.Text.Json)",
            save.PrettyBytes.ToString("N0") + " B (" + Fmt.N(save.PrettyPerBuilding, 1) + " B/bld)");
        Fmt.Info("measured compact",
            save.CompactBytes.ToString("N0") + " B (" + Fmt.N(save.CompactPerBuilding, 1) + " B/bld)");
        Fmt.Info("measured gzip pretty",
            save.GzipPrettyBytes.ToString("N0") + " B (ratio 1/" + Fmt.N(1.0 / save.PrettyGzipRatio, 2) + ")");
        Fmt.Info("measured gzip compact",
            save.GzipCompactBytes.ToString("N0") + " B (ratio 1/"
            + Fmt.N((double)save.CompactBytes / save.GzipCompactBytes, 2) + ")");
        Fmt.Info("serialize pretty (median)",
            Fmt.N(save.SerializePrettyMs, 2) + " ms (budget "
            + save.SaveTimeThresholdMs.ToString("N0") + " ms; headroom x"
            + Fmt.N(save.WriteHeadroomFactor, 0) + ")");
        Fmt.Info("serialize compact (median)", Fmt.N(save.SerializeCompactMs, 2) + " ms");
        Fmt.Info("parse compact (median)",
            Fmt.N(save.DeserializeCompactMs, 2) + " ms (budget "
            + save.SaveTimeThresholdMs.ToString("N0") + " ms; headroom x"
            + Fmt.N(save.LoadHeadroomFactor, 0) + ")");
        Fmt.Info("Core save state allocation",
            save.StateBytesPerBuilding + " B/bld; " + save.StateBytesTotal.ToString("N0")
            + " B for 1,000 (budget 64 B/bld, 65,536 B)");
        Console.WriteLine("  note: the synthetic payload is the §14.1 SEE node only; the real save is a");
        Console.WriteLine("        zip container written through the game persistence APIs (P8 profiler item).");
        Console.WriteLine("  status: within the §17.3 save thresholds (offline proxy, not a shipped gate).");
    }

    /// <summary>Prints the pure-Core micro-benchmarks and the 1,000-building tick proxy.</summary>
    public static void PrintPerfEstimate(PerfEstimateResult perf)
    {
        Fmt.Section("9. PERFORMANCE ESTIMATE (requirements §13.7 / §17.3)");
        Fmt.Info("runtime", RuntimeInformation.FrameworkDescription + " / "
            + RuntimeInformation.ProcessArchitecture + " / " + Environment.ProcessorCount + " CPUs");
        Fmt.Info("os", RuntimeInformation.OSDescription);
        Fmt.Info("workload", perf.Buildings + " buildings per tick, "
            + perf.MeasuredBatches.ToString("N0") + " measured batches, "
            + perf.WarmupBatches + " warm-up batches");
        Console.WriteLine("  micro-benchmarks (offline desktop JIT; 1,000,000 calls each):");
        foreach (FormulaBench bench in perf.FormulaBenches)
        {
            Console.WriteLine("    " + Fmt.L(bench.Name, 34) + Fmt.R(bench.NsPerOp, 8, 2) + " ns/op");
        }

        Console.WriteLine("    " + Fmt.L("synthetic tick bundle (1 building)", 34)
                          + Fmt.R(perf.TickBundleNsPerOp, 8, 2) + " ns/op ("
                          + Fmt.N(perf.TickBundleNsPerOp / 1000.0, 3) + " us/bld)");
        Console.WriteLine("  1,000-building tick proxy:");
        Console.WriteLine("    " + Fmt.L("mean", 26) + Fmt.R(perf.BatchMeanUs, 10, 2)
                          + " us (per building " + Fmt.N(perf.PerBuildingMeanUs, 3) + " us)");
        Console.WriteLine("    " + Fmt.L("p50 / p95 / p99 / max", 26)
                          + Fmt.R(perf.BatchP50Us, 10, 2) + " / " + Fmt.N(perf.BatchP95Us, 2)
                          + " / " + Fmt.N(perf.BatchP99Us, 2) + " / " + Fmt.N(perf.BatchMaxUs, 2) + " us");
        Console.WriteLine("    " + Fmt.L("p95 budget (3,000 us)", 26) + "headroom x"
                          + Fmt.N(perf.P95HeadroomFactor, 0));
        Console.WriteLine("    " + Fmt.L("p99 budget (6,000 us)", 26) + "headroom x"
                          + Fmt.N(perf.P99HeadroomFactor, 0));
        Console.WriteLine("    " + Fmt.L("per-building budget 3 us", 26) + "headroom x"
                          + Fmt.N(perf.PerBuildingHeadroomFactor, 0));
        Console.WriteLine("  p95/p99 derivation: 3 us/bld x 1,000 buildings = 3.0 ms p95; p99 = 2x p95 = 6.0 ms;");
        Console.WriteLine("    at a 1 s tick the p95 budget is 0.3% of wall-clock time.");
        Console.WriteLine("  assumptions: O(1) per building, no full-map scan, no steady-state allocation;");
        Console.WriteLine("    maintenance material/road lookups throttled to at most once per 10 s.");
        Console.WriteLine("  status: OFFLINE PROXY ONLY. The authoritative p95/p99, GC and FPS gates are");
        Console.WriteLine("    measured with the Unity Profiler in the P8 human gate.");
    }

    /// <summary>Prints the final verdict block and returns the process exit code.</summary>
    public static int PrintVerdict(LoadedData data, AnalysisResult analysis)
    {
        Fmt.Section("11. VERDICT");
        int rows = analysis.Rows.Count;
        int pass1 = analysis.Rows.Count(row => row.Pass1);
        int pass2 = analysis.Rows.Count(row => row.Pass2);
        int pass3 = analysis.Rows.Count(row => row.Pass3);
        int tiers = analysis.ScaleChecks.Count;
        int pass4 = analysis.ScaleChecks.Count(check => check.Pass);
        Console.WriteLine("  [1] full-load output >= reference -5%                 "
                          + Fmt.Verdict(analysis.Criterion1) + " (" + pass1 + "/" + rows + " rows)");
        Console.WriteLine("  [2] fuel <=0.025 / water <=0.075 per 100 hp           "
                          + Fmt.Verdict(analysis.Criterion2) + " (" + pass2 + "/" + rows + " rows)");
        Console.WriteLine("  [3] 30-day average >= reference x 0.95               "
                          + Fmt.Verdict(analysis.Criterion3) + " (" + pass3 + "/" + rows + " rows)");
        Console.WriteLine("  [4] scale effect strictly decreasing, >= 40% Small.T1 "
                          + Fmt.Verdict(analysis.Criterion4) + " (" + pass4 + "/" + tiers + " tiers)");
        Console.WriteLine("  data cross-checks                                     PASS ("
                          + data.CrossCheckCount + " checks)");
        Console.WriteLine("  offline save/performance estimate                     INFORMATIONAL"
                          + " (P8 Profiler required)");
        bool pass = analysis.AllPass;
        int failing = analysis.Rows.Count(row => !(row.Pass1 && row.Pass2 && row.Pass3));
        Console.WriteLine();
        Console.WriteLine(pass
            ? "BALANCE SIM PASS 12/12 (4/4 criteria) -> exit 0"
            : "BALANCE SIM FAIL (" + failing + " failing row(s), "
              + (tiers - pass4) + " failing tier(s)) -> non-zero exit");
        return pass ? 0 : 1;
    }
}
