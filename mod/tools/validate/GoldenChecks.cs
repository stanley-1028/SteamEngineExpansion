using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SEE.Tools.Validate;

/// <summary>
/// Golden-hash gate (contract §4.10 step 6): the 24 checked-in blueprints must be byte-identical
/// to the golden table <c>tests/SEE.Tests/Golden/blueprints.sha256</c>.
///
/// The generator's <c>--check</c> mode re-renders the whole output in memory and compares it with
/// what is on disk; this gate is the independent second opinion: it only re-hashes the shipped
/// bytes and compares them with the checked-in table, so a stale golden table or a hand-edited
/// blueprint fails even when the generator is not run.
/// </summary>
internal sealed class GoldenChecks
{
    /// <summary>Number of blueprint entries the golden table must contain (12 x 2 factions).</summary>
    public const int ExpectedCount = 24;

    private const string RelativeGoldenPath = "tests/SEE.Tests/Golden/blueprints.sha256";
    private const string BlueprintFolder = "Buildings/Power/SteamBoiler";

    private static readonly Regex LinePattern = new("^([0-9a-f]{64})  (.+)$", RegexOptions.Compiled);

    private readonly string _modRoot;
    private readonly string _dataDir;
    private readonly Report _report;

    public GoldenChecks(string modRoot, string dataDir, Report report)
    {
        _modRoot = modRoot;
        _dataDir = dataDir;
        _report = report;
    }

    /// <summary>Number of golden entries whose hash matched.</summary>
    public int EntriesChecked { get; private set; }

    public void Validate()
    {
        var goldenPath = Path.Combine(_modRoot, RelativeGoldenPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(goldenPath))
        {
            _report.Error("golden", RelativeGoldenPath + " is missing; the generated blueprints have no golden baseline");
            return;
        }

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = File.ReadAllLines(goldenPath);
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }

            var match = LinePattern.Match(line);
            if (!match.Success)
            {
                _report.Error("golden", RelativeGoldenPath + " line " + (index + 1)
                                             + " is not '<sha256>  <path>' (found '" + line + "')");
                continue;
            }

            var path = match.Groups[2].Value.Replace('\\', '/');
            if (!entries.TryAdd(path, match.Groups[1].Value))
            {
                _report.Error("golden", RelativeGoldenPath + " lists '" + path + "' twice");
            }
        }

        if (entries.Count != ExpectedCount)
        {
            _report.Error("golden", RelativeGoldenPath + " has " + entries.Count + " entries; the contract requires exactly "
                                         + ExpectedCount + " generated blueprints");
        }

        foreach (var pair in entries)
        {
            var file = Path.Combine(_modRoot, pair.Key.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(file))
            {
                _report.Error("golden", pair.Key + " is listed in the golden table but missing on disk");
                continue;
            }

            using var stream = File.OpenRead(file);
            var actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!string.Equals(actual, pair.Value, StringComparison.Ordinal))
            {
                _report.Error("golden", pair.Key + " hashes to " + actual + " but the golden table records " + pair.Value
                                             + " (the blueprint was edited by hand or the golden table is stale)");
                continue;
            }

            EntriesChecked++;
        }

        var folder = Path.Combine(_dataDir, BlueprintFolder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(folder))
        {
            _report.Error("golden", "data/" + BlueprintFolder + " is missing");
            return;
        }

        foreach (var file in Directory.GetFiles(folder, "*.blueprint.json", SearchOption.TopDirectoryOnly))
        {
            var relative = "data/" + BlueprintFolder + "/" + Path.GetFileName(file);
            if (!entries.ContainsKey(relative))
            {
                _report.Error("golden", relative + " is not listed in the golden table (extra or renamed blueprint)");
            }
        }
    }
}
