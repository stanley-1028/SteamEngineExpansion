using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using SEE.Core.Abstractions;
using SEE.Core.Save;
using SEE.Tools.BalanceSim.Data;

namespace SEE.Tools.BalanceSim.Estimation;

/// <summary>Measured + estimated save footprint of 1,000 boilers (requirements §14.7 / §17.3).</summary>
internal sealed class SaveEstimateResult
{
    /// <summary>Number of simulated buildings (1,000).</summary>
    public int Buildings { get; init; }

    /// <summary>Pretty-print bytes of the synthetic §14.1 SEE node.</summary>
    public int PrettyBytes { get; init; }

    /// <summary>Compact bytes of the synthetic §14.1 SEE node.</summary>
    public int CompactBytes { get; init; }

    /// <summary>Gzip bytes of the pretty payload (estimated save-container size).</summary>
    public int GzipPrettyBytes { get; init; }

    /// <summary>Gzip bytes of the compact payload.</summary>
    public int GzipCompactBytes { get; init; }

    /// <summary>Pretty bytes per building.</summary>
    public double PrettyPerBuilding => (double)PrettyBytes / Buildings;

    /// <summary>Compact bytes per building.</summary>
    public double CompactPerBuilding => (double)CompactBytes / Buildings;

    /// <summary>Gzip ratio of the pretty payload (compressed / raw).</summary>
    public double PrettyGzipRatio => (double)GzipPrettyBytes / PrettyBytes;

    /// <summary>Median wall-clock time to serialize the pretty payload, in milliseconds.</summary>
    public double SerializePrettyMs { get; init; }

    /// <summary>Median wall-clock time to serialize the compact payload, in milliseconds.</summary>
    public double SerializeCompactMs { get; init; }

    /// <summary>Median wall-clock time to parse the compact payload back, in milliseconds.</summary>
    public double DeserializeCompactMs { get; init; }

    /// <summary>Measured managed allocation of one <see cref="BoilerSaveState"/>, in bytes.</summary>
    public int StateBytesPerBuilding { get; init; }

    /// <summary>Measured managed allocation for 1,000 states, in bytes.</summary>
    public long StateBytesTotal { get; init; }

    /// <summary>§14.7 estimate: 350 bytes per building (pretty print).</summary>
    public long EstimatedPrettyBytes => 350L * Buildings;

    /// <summary>§14.7 estimate: 250 bytes per building (compact).</summary>
    public long EstimatedCompactBytes => 250L * Buildings;

    /// <summary>§14.7 estimate: zip container is about one fifth of the pretty payload.</summary>
    public long EstimatedZipBytes => EstimatedPrettyBytes / 5;

    /// <summary>§14.7 save-container threshold: 400 KB after zip (§17.3).</summary>
    public long SaveFileThresholdBytes => 400L * 1024;

    /// <summary>§17.3 save/load time threshold: 2,000 ms each.</summary>
    public double SaveTimeThresholdMs => 2000.0;

    /// <summary>Headroom factor of the measured serialization time versus the 2 s budget.</summary>
    public double WriteHeadroomFactor => SaveTimeThresholdMs / Math.Max(SerializePrettyMs, 1e-6);

    /// <summary>Headroom factor of the measured parse time versus the 2 s budget.</summary>
    public double LoadHeadroomFactor => SaveTimeThresholdMs / Math.Max(DeserializeCompactMs, 1e-6);
}

/// <summary>
/// Produces the §14.7 / §17.3 save evidence: a representative 1,000-building <c>SEE</c> node,
/// its pretty/compact/gzip size, median serialize/parse wall-clock times, and the measured cost of
/// the Core save snapshot. Offline <c>System.Text.Json</c> measurement only; the real game save is
/// a zip container written through the game persistence APIs and is a P8 profiler item.
/// </summary>
internal static class SaveEstimate
{
    private const int DefaultBuildings = 1000;
    private const int TimingIterations = 11;

    /// <summary>Runs the save estimate for 1,000 buildings.</summary>
    public static SaveEstimateResult Run()
    {
        object document = BuildDocument(DefaultBuildings);
        byte[] pretty = JsonSerializer.SerializeToUtf8Bytes(
            document, new JsonSerializerOptions { WriteIndented = true });
        byte[] compact = JsonSerializer.SerializeToUtf8Bytes(
            document, new JsonSerializerOptions { WriteIndented = false });
        byte[] gzipPretty = Gzip(pretty);
        byte[] gzipCompact = Gzip(compact);

        double serializePretty = MedianMs(() => JsonSerializer.SerializeToUtf8Bytes(
            document, new JsonSerializerOptions { WriteIndented = true }));
        double serializeCompact = MedianMs(() => JsonSerializer.SerializeToUtf8Bytes(
            document, new JsonSerializerOptions { WriteIndented = false }));
        double deserializeCompact = MedianMs(() => CountBuildings(compact));
        int stateBytes = MeasureStateBytes(DefaultBuildings);

        return new SaveEstimateResult
        {
            Buildings = DefaultBuildings,
            PrettyBytes = pretty.Length,
            CompactBytes = compact.Length,
            GzipPrettyBytes = gzipPretty.Length,
            GzipCompactBytes = gzipCompact.Length,
            SerializePrettyMs = serializePretty,
            SerializeCompactMs = serializeCompact,
            DeserializeCompactMs = deserializeCompact,
            StateBytesPerBuilding = stateBytes,
            StateBytesTotal = (long)stateBytes * DefaultBuildings,
        };
    }

    /// <summary>Builds the synthetic §14.1-shaped SEE save node.</summary>
    private static object BuildDocument(int buildings)
    {
        var entries = new List<object>(buildings);
        for (int index = 0; index < buildings; index++)
        {
            entries.Add(new Dictionary<string, object?>
            {
                ["entityId"] = SampleGuid(index),
                ["tier"] = "T" + (index % 4 + 1),
                ["size"] = (index % 3) switch { 0 => "Small", 1 => "Medium", _ => "Large" },
                ["faction"] = index % 2 == 0 ? "Folktails" : "IronTeeth",
                ["condition"] = Math.Round(50.0 + index % 50, 1),
                ["autoMaintenance"] = true,
                ["maintenanceState"] = index % 97 == 0 ? "Maintaining" : "None",
                ["maintenanceRemainingHours"] = index % 97 == 0 ? 1.5 : 0.0,
                ["lastMaintenanceDay"] = Math.Round(index % 1200 / 10.0, 1),
                ["waterSupplyMode"] = "Tank",
                ["powerLineAttached"] = index % 3 == 0,
            });
        }

        return new Dictionary<string, object?>
        {
            ["SEE"] = new Dictionary<string, object?>
            {
                ["schemaVersion"] = 1,
                ["modVersion"] = "0.1.0",
                ["checksum"] = "sha256:" + new string('a', 64),
                ["buildings"] = entries,
                ["compat"] = new Dictionary<string, object?>
                {
                    ["detected"] = new[] { "TimberPipes" },
                    ["enabled"] = new[] { "SEE.Adapter.TimberPipes" },
                    ["failed"] = Array.Empty<string>(),
                },
            },
        };
    }

    private static string SampleGuid(int index)
    {
        Span<byte> seed = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(seed, index);
        byte[] hash = SHA256.HashData(seed);
        return new Guid(hash.AsSpan(0, 16)).ToString("D");
    }

    private static int CountBuildings(byte[] compact)
    {
        using JsonDocument document = JsonDocument.Parse(compact);
        return document.RootElement.GetProperty("SEE").GetProperty("buildings").GetArrayLength();
    }

    private static double MedianMs(Func<byte[]> action)
    {
        // Warm-up exercises the serializer path before the timed samples.
        for (int index = 0; index < 2; index++)
        {
            GC.KeepAlive(action());
        }

        var samples = new double[TimingIterations];
        for (int index = 0; index < TimingIterations; index++)
        {
            var stopwatch = Stopwatch.StartNew();
            byte[] bytes = action();
            stopwatch.Stop();
            samples[index] = stopwatch.Elapsed.TotalMilliseconds;
            GC.KeepAlive(bytes);
        }

        Array.Sort(samples);
        return samples[TimingIterations / 2];
    }

    private static double MedianMs(Func<int> action)
    {
        for (int index = 0; index < 2; index++)
        {
            GC.KeepAlive(action());
        }

        var samples = new double[TimingIterations];
        for (int index = 0; index < TimingIterations; index++)
        {
            var stopwatch = Stopwatch.StartNew();
            int result = action();
            stopwatch.Stop();
            samples[index] = stopwatch.Elapsed.TotalMilliseconds;
            GC.KeepAlive(result);
        }

        Array.Sort(samples);
        return samples[TimingIterations / 2];
    }

    private static byte[] Gzip(byte[] raw)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(raw, 0, raw.Length);
        }

        return output.ToArray();
    }

    /// <summary>Measures the managed allocation of one Core save snapshot by allocation delta.</summary>
    private static int MeasureStateBytes(int count)
    {
        var sink = new BoilerSaveState[count];
        for (int index = 0; index < count; index++)
        {
            sink[index] = BoilerSaveState.NewGame();
        }

        GC.KeepAlive(sink);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < count; index++)
        {
            sink[index] = BoilerSaveState.NewGame();
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        GC.KeepAlive(sink);
        return (int)((after - before) / count);
    }
}
