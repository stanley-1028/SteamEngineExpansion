using System.Diagnostics;
using SEE.Core.Abstractions;
using SEE.Core.Config;
using SEE.Core.Formulas;
using SEE.Core.State;

namespace SEE.Tools.BalanceSim.Estimation;

/// <summary>One micro-benchmark sample: a Core formula and its measured cost.</summary>
internal sealed class FormulaBench
{
    /// <summary>Method display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Measured nanoseconds per call (indicative only; offline desktop JIT).</summary>
    public double NsPerOp { get; init; }
}

/// <summary>Offline performance estimate for the §13.7 per-building tick budget.</summary>
internal sealed class PerfEstimateResult
{
    /// <summary>Formula micro-benchmarks (1,000,000 calls each).</summary>
    public IReadOnlyList<FormulaBench> FormulaBenches { get; init; } = Array.Empty<FormulaBench>();

    /// <summary>Cost of the synthetic one-building tick bundle, in nanoseconds.</summary>
    public double TickBundleNsPerOp { get; init; }

    /// <summary>Number of simulated buildings per batch (1,000).</summary>
    public int Buildings { get; init; }

    /// <summary>Number of measured 1,000-building batches.</summary>
    public int MeasuredBatches { get; init; }

    /// <summary>Number of warm-up batches.</summary>
    public int WarmupBatches { get; init; }

    /// <summary>Mean wall clock of one 1,000-building tick, in microseconds.</summary>
    public double BatchMeanUs { get; init; }

    /// <summary>Median (p50) wall clock of one 1,000-building tick, in microseconds.</summary>
    public double BatchP50Us { get; init; }

    /// <summary>p95 wall clock of one 1,000-building tick, in microseconds.</summary>
    public double BatchP95Us { get; init; }

    /// <summary>p99 wall clock of one 1,000-building tick, in microseconds.</summary>
    public double BatchP99Us { get; init; }

    /// <summary>Worst measured 1,000-building tick, in microseconds.</summary>
    public double BatchMaxUs { get; init; }

    /// <summary>Mean per-building tick cost, in microseconds.</summary>
    public double PerBuildingMeanUs => BatchMeanUs / Buildings;

    /// <summary>§17.3 p95 threshold for the whole 1,000-building tick, in milliseconds.</summary>
    public double BudgetP95Ms => 3.0;

    /// <summary>§17.3 p99 threshold for the whole 1,000-building tick, in milliseconds.</summary>
    public double BudgetP99Ms => 6.0;

    /// <summary>§13.7 per-building average budget, in microseconds.</summary>
    public double BudgetPerBuildingUs => 3.0;

    /// <summary>p95 headroom factor versus the 3 ms threshold.</summary>
    public double P95HeadroomFactor => BudgetP95Ms * 1000.0 / Math.Max(BatchP95Us, 1e-6);

    /// <summary>p99 headroom factor versus the 6 ms threshold.</summary>
    public double P99HeadroomFactor => BudgetP99Ms * 1000.0 / Math.Max(BatchP99Us, 1e-6);

    /// <summary>Per-building headroom factor versus the 3 us average budget.</summary>
    public double PerBuildingHeadroomFactor => BudgetPerBuildingUs / Math.Max(PerBuildingMeanUs, 1e-6);
}

/// <summary>
/// Drives the shared SEE.Core formulas in a 1,000-building / 1-second-tick shape and reports
/// nanosecond and p95/p99 numbers (requirements §13.7, §12.6, §17.3).
///
/// This is an OFFLINE PROXY: desktop .NET 9 JIT on the developer machine, no Unity runtime, no
/// game systems, no MMC profiler marker. It can only prove that the pure Core arithmetic is far
/// inside the budget; the authoritative 1,000-building p95/p99 measurement is a P8 human gate.
/// </summary>
internal static class PerfEstimate
{
    private const int Buildings = 1000;
    private const int BenchIterations = 1_000_000;
    private const int WarmupIterations = 100_000;
    private const int WarmupBatches = 200;
    private const int MeasuredBatches = 10_000;

    /// <summary>Runs the micro-benchmarks and the 1,000-building tick proxy.</summary>
    public static PerfEstimateResult Run()
    {
        var benches = new List<FormulaBench>
        {
            new() { Name = "ConditionCurve.Multiplier", NsPerOp = BenchConditionCurve() },
            new() { Name = "BoilerFormulas.FuelPerHour", NsPerOp = BenchFuel() },
            new() { Name = "BoilerFormulas.WaterPerHour", NsPerOp = BenchWater() },
            new() { Name = "BoilerFormulas.WearPerHour", NsPerOp = BenchWear() },
            new() { Name = "BoilerFormulas.OutputHorsepower", NsPerOp = BenchOutput() },
            new() { Name = "BoilerStateMachine.Evaluate", NsPerOp = BenchEvaluate() },
        };

        double tickNs = BenchTickBundle();
        (double mean, double p50, double p95, double p99, double max) = BenchBatches();
        return new PerfEstimateResult
        {
            FormulaBenches = benches,
            TickBundleNsPerOp = tickNs,
            Buildings = Buildings,
            MeasuredBatches = MeasuredBatches,
            WarmupBatches = WarmupBatches,
            BatchMeanUs = mean,
            BatchP50Us = p50,
            BatchP95Us = p95,
            BatchP99Us = p99,
            BatchMaxUs = max,
        };
    }

    private static double BenchConditionCurve()
    {
        float sink = 0f;
        for (int index = 0; index < WarmupIterations; index++)
        {
            sink += ConditionCurve.Multiplier(40f + index % 61);
        }

        var stopwatch = Stopwatch.StartNew();
        for (int index = 0; index < BenchIterations; index++)
        {
            sink += ConditionCurve.Multiplier(40f + index % 61);
        }

        stopwatch.Stop();
        GC.KeepAlive(sink);
        return Nanoseconds(stopwatch, BenchIterations);
    }

    private static double BenchFuel()
    {
        float sink = 0f;
        for (int index = 0; index < WarmupIterations; index++)
        {
            sink += BoilerFormulas.FuelPerHour(0.12f, (index & 63) * 0.015f, FuelScalingMode.Proportional);
        }

        var stopwatch = Stopwatch.StartNew();
        for (int index = 0; index < BenchIterations; index++)
        {
            sink += BoilerFormulas.FuelPerHour(0.12f, (index & 63) * 0.015f, FuelScalingMode.Proportional);
        }

        stopwatch.Stop();
        GC.KeepAlive(sink);
        return Nanoseconds(stopwatch, BenchIterations);
    }

    private static double BenchWater()
    {
        float sink = 0f;
        for (int index = 0; index < WarmupIterations; index++)
        {
            sink += BoilerFormulas.WaterPerHour(0.32f, (index & 63) * 0.015f);
        }

        var stopwatch = Stopwatch.StartNew();
        for (int index = 0; index < BenchIterations; index++)
        {
            sink += BoilerFormulas.WaterPerHour(0.32f, (index & 63) * 0.015f);
        }

        stopwatch.Stop();
        GC.KeepAlive(sink);
        return Nanoseconds(stopwatch, BenchIterations);
    }

    private static double BenchWear()
    {
        float sink = 0f;
        for (int index = 0; index < WarmupIterations; index++)
        {
            sink += BoilerFormulas.WearPerHour(8.0, (index & 63) * 0.015f);
        }

        var stopwatch = Stopwatch.StartNew();
        for (int index = 0; index < BenchIterations; index++)
        {
            sink += BoilerFormulas.WearPerHour(8.0, (index & 63) * 0.015f);
        }

        stopwatch.Stop();
        GC.KeepAlive(sink);
        return Nanoseconds(stopwatch, BenchIterations);
    }

    private static double BenchOutput()
    {
        int sink = 0;
        for (int index = 0; index < WarmupIterations; index++)
        {
            sink += BoilerFormulas.OutputHorsepower(460, 0.5f + (index & 63) * 0.0075f);
        }

        var stopwatch = Stopwatch.StartNew();
        for (int index = 0; index < BenchIterations; index++)
        {
            sink += BoilerFormulas.OutputHorsepower(460, 0.5f + (index & 63) * 0.0075f);
        }

        stopwatch.Stop();
        GC.KeepAlive(sink);
        return Nanoseconds(stopwatch, BenchIterations);
    }

    private static double BenchEvaluate()
    {
        int sink = 0;
        for (int index = 0; index < WarmupIterations; index++)
        {
            var input = new BoilerStateInput(
                false, MaintenancePhase.None, 40f + index % 61, true, true, (index & 63) * 0.015f);
            sink += (int)BoilerStateMachine.Evaluate(in input);
        }

        var stopwatch = Stopwatch.StartNew();
        for (int index = 0; index < BenchIterations; index++)
        {
            var input = new BoilerStateInput(
                false, MaintenancePhase.None, 40f + index % 61, true, true, (index & 63) * 0.015f);
            sink += (int)BoilerStateMachine.Evaluate(in input);
        }

        stopwatch.Stop();
        GC.KeepAlive(sink);
        return Nanoseconds(stopwatch, BenchIterations);
    }

    /// <summary>One synthetic boiler state; the shape mirrors the cached fields a real tick reads.</summary>
    private struct BuildingState
    {
        public int RatedHorsepower;
        public double RatedFuel;
        public double RatedWater;
        public double IntervalDays;
        public float Condition;
        public float Load;
        public bool HasFuel;
        public bool HasWater;
        public MaintenancePhase Phase;
        public double Accumulator;
        public double OutputTotal;
    }

    /// <summary>Applies one synthetic tick to one building using the shared Core formulas.</summary>
    private static void TickOnce(ref BuildingState state)
    {
        var input = new BoilerStateInput(
            false, state.Phase, state.Condition, state.HasFuel, state.HasWater, state.Load);
        BoilerStatus status = BoilerStateMachine.Evaluate(in input);
        float conditionMultiplier = ConditionCurve.Multiplier(state.Condition);
        float load = BoilerFormulas.Clamp01(state.Load);
        int output = BoilerFormulas.OutputHorsepower(state.RatedHorsepower, load * conditionMultiplier);
        float fuel = BoilerFormulas.FuelPerHour((float)state.RatedFuel, load, FuelScalingMode.Proportional);
        float water = BoilerFormulas.WaterPerHour((float)state.RatedWater, load);
        float wear = BoilerFormulas.WearPerHour(state.IntervalDays, load);
        state.Condition -= wear;
        if (state.Condition <= 50.0f)
        {
            // Automatic-maintenance reset; the 2 game hours of downtime are a small share of the
            // cycle and are modelled in the 30-day balance analysis, not in this cost proxy.
            state.Condition = 100.0f;
        }

        state.OutputTotal += output;
        state.Accumulator += fuel + water + (int)status * 0.001;
    }

    private static BuildingState[] CreateStates()
    {
        var states = new BuildingState[Buildings];
        for (int index = 0; index < Buildings; index++)
        {
            states[index] = new BuildingState
            {
                RatedHorsepower = 130 + index % 12 * 120,
                RatedFuel = 0.04 + index % 12 * 0.02,
                RatedWater = 0.10 + index % 12 * 0.03,
                IntervalDays = 6 + index % 4 * 2,
                Condition = 61.0f + index % 39,
                Load = 1.0f,
                HasFuel = true,
                HasWater = true,
                Phase = MaintenancePhase.None,
            };
        }

        return states;
    }

    private static double BenchTickBundle()
    {
        BuildingState state = CreateStates()[0];
        for (int index = 0; index < WarmupIterations; index++)
        {
            TickOnce(ref state);
        }

        var stopwatch = Stopwatch.StartNew();
        for (int index = 0; index < BenchIterations; index++)
        {
            TickOnce(ref state);
        }

        stopwatch.Stop();
        GC.KeepAlive(state.Accumulator);
        return Nanoseconds(stopwatch, BenchIterations);
    }

    private static (double Mean, double P50, double P95, double P99, double Max) BenchBatches()
    {
        BuildingState[] states = CreateStates();
        for (int batch = 0; batch < WarmupBatches; batch++)
        {
            TickBatch(states);
        }

        var samples = new double[MeasuredBatches];
        for (int batch = 0; batch < MeasuredBatches; batch++)
        {
            var stopwatch = Stopwatch.StartNew();
            TickBatch(states);
            stopwatch.Stop();
            samples[batch] = stopwatch.Elapsed.TotalMilliseconds * 1000.0;
        }

        GC.KeepAlive(states);
        Array.Sort(samples);
        double mean = 0.0;
        foreach (double sample in samples)
        {
            mean += sample;
        }

        return (mean / samples.Length, Percentile(samples, 0.50), Percentile(samples, 0.95),
            Percentile(samples, 0.99), samples[^1]);
    }

    private static void TickBatch(BuildingState[] states)
    {
        for (int index = 0; index < states.Length; index++)
        {
            TickOnce(ref states[index]);
        }
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        int rank = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        rank = Math.Clamp(rank, 0, sorted.Length - 1);
        return sorted[rank];
    }

    private static double Nanoseconds(Stopwatch stopwatch, int iterations) =>
        stopwatch.Elapsed.TotalMilliseconds * 1_000_000.0 / iterations;
}
