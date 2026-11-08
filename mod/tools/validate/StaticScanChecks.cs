using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SEE.Tools.Validate;

/// <summary>
/// Static red-line scan (contract §10, requirements §12/§16): the things that must never appear in
/// the shipped code or data, regardless of whether the feature "works".
///
/// <list type="bullet">
/// <item>no per-frame work in a Unity <c>Update</c>/<c>LateUpdate</c>/<c>FixedUpdate</c> method:
/// no <c>FindObjects</c>/full-map enumeration, no string interpolation and no config read
/// (REQ-SAFE-07, REQ-OPS-05/07); gameplay/Integration code must use the tick system instead of
/// <c>Update</c> at all;</item>
/// <item>no <c>unsafe</c>, P/Invoke, <c>Process.Start</c>, network client or background thread
/// (REQ-NFR-15, REQ-SAFE-15);</item>
/// <item>no compile-time Harmony reference outside <c>SEE.Integration</c> (contract §10 item 1,
/// REQ-ARCH-03) — the current v1 state is zero occurrences anywhere;</item>
/// <item>no hard-coded balance values outside the whitelist: a file that carries three or more of
/// the authoritative <c>engines.json</c> numbers (horsepower, science cost, fuel/water rate, build
/// cost) looks like a copied balance table (REQ-AFR-09/REQ-IMPL-05, contract §10 item 6);</item>
/// <item>no <c>SteamEngine</c>/<c>SteamPlant</c> as a TemplateName or localization key, and every
/// TemplateName in every shipped data document is one of the 24 contract names (contract §2.7/§5.5).</item>
/// </list>
/// </summary>
internal sealed class StaticScanChecks
{
    private static readonly string[] PerFrameMethods = { "Update", "LateUpdate", "FixedUpdate" };

    private static readonly string[] PerFrameIdentifiers =
    {
        "FindObjects", "FindFirstObjectByType", "FindAnyObjectByType", "FindObjectsOfTypeAll",
        "GetComponentsInChildren", "GetComponentsInParent", "EntityRegistry", ".Entities", "GetAll<",
    };

    private static readonly string[] PerFrameInterpolation = { "$\"", "string.Format", "string.Concat" };

    private static readonly string[] PerFrameConfig = { "SeeConfig", "SEE_Config", "Config." };

    private static readonly string[] SafetyIdentifiers =
    {
        "unsafe", "DllImport", "extern ", "Process.Start", "System.Net", "HttpClient", "WebClient",
        "WebRequest", "UnityWebRequest", "new Thread", "ThreadPool.", "Task.Run",
    };

    private static readonly string[] BalanceWhitelist =
    {
        "src/SEE.Gameplay/Specs/",
        "src/SEE.Core/Constants/",
        "src/SEE.Core/Config/",
        "src/SEE.Core/Balance/",
        "src/SEE.Core/Formulas/",
        "src/SEE.Core/Maintenance/",
        "src/SEE.Core/State/",
        "src/SEE.Core/Save/",
    };

    private static readonly string[] LocKeyFields =
    {
        "DisplayNameLocKey", "DescriptionLocKey", "FlavorDescriptionLocKey",
    };

    private static readonly string[] ForbiddenTemplateFragments = { "SteamEngine", "SteamPlant", "TWSteamEngine" };

    private static readonly Regex HarmonyIdentifier = new(@"\bHarmony(Lib)?\b", RegexOptions.Compiled);

    private static readonly Regex TemplateNamePattern = new(
        "^SteamBoiler\\.(Small|Medium|Large)\\.(T1|T2|T3|T4)\\.(Folktails|IronTeeth)$", RegexOptions.Compiled);

    private static readonly Regex BalanceAssignment = new(
        @"\b(?:Horsepower|FuelPerHour|WaterPerHour|FuelCapacityGoods|WaterCapacityGoods|ScienceCost|PowerOutput|"
        + @"FuelStock|WaterTankGoods|BuildCost)\s*[=:]\s*(?<value>-?\d+(?:\.\d+)?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const int HardCodedTableThreshold = 3;
    private const int HardCodedTableMaxGap = 80;

    private readonly string _modRoot;
    private readonly string _dataDir;
    private readonly Report _report;
    private readonly Dictionary<string, string> _templateNameFiles = new(StringComparer.Ordinal);
    private HashSet<string>? _authoritativeNumbers;
    private HashSet<string>? _tableNumbers;

    public StaticScanChecks(string modRoot, string dataDir, Report report)
    {
        _modRoot = modRoot;
        _dataDir = dataDir;
        _report = report;
    }

    /// <summary>Number of C# source files inspected.</summary>
    public int SourceFilesScanned { get; private set; }

    /// <summary>Number of data documents inspected.</summary>
    public int DataDocumentsScanned { get; private set; }

    /// <summary>Number of TemplateName declarations seen in data.</summary>
    public int TemplateNamesSeen { get; private set; }

    /// <summary>Number of red-line hits (each one is also an error).</summary>
    public int Hits { get; private set; }

    public void ValidateSource()
    {
        LoadBalanceTokens();
        var src = Path.Combine(_modRoot, "src");
        if (!Directory.Exists(src))
        {
            _report.Error("forbidden", "src root is missing under the mod root");
            return;
        }

        foreach (var file in Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
                     .Where(path => !HasSegment(path, "bin") && !HasSegment(path, "obj"))
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            SourceFilesScanned++;
            var relative = Path.GetRelativePath(_modRoot, file).Replace('\\', '/');
            var text = File.ReadAllText(file);
            var codeOnly = SourceFacts.StripCommentsAndLiterals(text);

            CheckPerFrame(relative, text, codeOnly);
            CheckSafety(relative, codeOnly);
            CheckHarmony(relative, codeOnly);
            CheckHardCodedBalance(relative, codeOnly, text);
        }
    }

    public void ValidateData()
    {
        if (!Directory.Exists(_dataDir))
        {
            _report.Error("forbidden", "data directory is missing");
            return;
        }

        foreach (var file in Directory.EnumerateFiles(_dataDir, "*.blueprint.json", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            DataDocumentsScanned++;
            var relative = "data/" + Path.GetRelativePath(_dataDir, file).Replace('\\', '/');

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(File.ReadAllText(file));
            }
            catch (JsonException exception)
            {
                _report.Error("forbidden", relative + ": invalid JSON: " + exception.Message);
                continue;
            }

            using (document)
            {
                Walk(document.RootElement, relative);
            }
        }
    }

    private void CheckPerFrame(string relative, string text, string codeOnly)
    {
        foreach (var method in PerFrameMethods)
        {
            var pattern = @"\b" + method + @"\s*\(\s*\)\s*\{";
            foreach (Match match in Regex.Matches(codeOnly, pattern))
            {
                var body = ExtractBody(text, match.Index + match.Length - 1);
                if (body is null)
                {
                    continue;
                }

                if ((relative.StartsWith("src/SEE.Gameplay/", StringComparison.Ordinal)
                     || relative.StartsWith("src/SEE.Integration/", StringComparison.Ordinal)))
                {
                    Hit(relative, method + " runs gameplay/Integration code per frame; use the 1 s tick system "
                                          + "(TickableComponent) instead (REQ-SAFE-07)");
                }

                foreach (var identifier in PerFrameIdentifiers)
                {
                    if (body.Contains(identifier, StringComparison.Ordinal))
                    {
                        Hit(relative, method + " contains '" + identifier
                                              + "' — every-frame full scan/`FindObjects` is forbidden (REQ-SAFE-07)");
                    }
                }

                foreach (var token in PerFrameInterpolation)
                {
                    if (body.Contains(token, StringComparison.Ordinal))
                    {
                        Hit(relative, method + " contains '" + token
                                              + "' — per-frame string formatting/configuration is forbidden (REQ-OPS-08)");
                    }
                }

                foreach (var token in PerFrameConfig)
                {
                    if (body.Contains(token, StringComparison.Ordinal))
                    {
                        Hit(relative, method + " reads the configuration ('" + token
                                              + "') every frame; cache it outside the frame loop (REQ-OPS-08)");
                    }
                }
            }
        }
    }

    private void CheckSafety(string relative, string codeOnly)
    {
        foreach (var identifier in SafetyIdentifiers)
        {
            if (codeOnly.Contains(identifier, StringComparison.Ordinal))
            {
                Hit(relative, "contains '" + identifier
                              + "' — unsafe/P-Invoke/process/network/background-thread is forbidden (REQ-NFR-15, REQ-SAFE-15)");
            }
        }
    }

    private void CheckHarmony(string relative, string codeOnly)
    {
        if (!HarmonyIdentifier.IsMatch(codeOnly))
        {
            return;
        }

        if (!relative.StartsWith("src/SEE.Integration/", StringComparison.Ordinal))
        {
            Hit(relative, "contains a compile-time Harmony reference outside SEE.Integration "
                          + "(contract §10 item 1, REQ-ARCH-03)");
            return;
        }

        _report.Warn("forbidden", relative + " mentions Harmony in code; v1 must keep the optional PL "
                                             + "injection on the non-Harmony path (config-controlled, default off)");
    }

    private void CheckHardCodedBalance(string relative, string codeOnly, string text)
    {
        if (_authoritativeNumbers is null || _tableNumbers is null || _authoritativeNumbers.Count == 0)
        {
            return;
        }

        if (BalanceWhitelist.Any(prefix => relative.StartsWith(prefix, StringComparison.Ordinal))
            || relative.Contains("Benchmark", StringComparison.OrdinalIgnoreCase)
            || text.Contains("BenchmarkStep", StringComparison.Ordinal))
        {
            return;
        }

        // 1. An assignment that copies an authoritative number into code, e.g. `Horsepower = 460`.
        foreach (Match match in BalanceAssignment.Matches(codeOnly))
        {
            var literal = NormalizeToken(match.Groups["value"].Value);
            if (literal is not null && _authoritativeNumbers.Contains(literal))
            {
                Hit(relative, "assigns the balance literal " + literal + " in code ('" + match.Value.Trim()
                              + "'); balance numbers may only come from engines.json / the spec / config / SEEConst "
                              + "(REQ-AFR-09, contract §10 item 6)");
            }
        }

        // 2. A compact table of three or more horsepower/science values is a copied balance table even
        //    without a field name next to it.
        var table = new List<KeyValuePair<int, string>>();
        foreach (var token in _tableNumbers)
        {
            var match = Regex.Match(codeOnly, @"(?<![\w.])" + Regex.Escape(token) + @"(?![\d])");
            if (match.Success)
            {
                table.Add(new KeyValuePair<int, string>(match.Index, token));
            }
        }

        table.Sort((left, right) => left.Key.CompareTo(right.Key));
        var run = new List<string>();
        var previous = -1;
        foreach (var pair in table)
        {
            if (previous >= 0 && pair.Key - previous > HardCodedTableMaxGap)
            {
                run.Clear();
            }

            run.Add(pair.Value);
            previous = pair.Key;

            if (run.Count >= HardCodedTableThreshold)
            {
                Hit(relative, "contains a copied balance table (" + string.Join(", ", run)
                              + "); the twelve rows must be loaded from engines.json (REQ-NFR-09, contract §10 item 6)");
                break;
            }
        }
    }

    private void Walk(JsonElement element, string relative)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, "TemplateName", StringComparison.Ordinal)
                        && property.Value.ValueKind == JsonValueKind.String)
                    {
                        CheckTemplateName(relative, property.Value.GetString() ?? string.Empty);
                    }
                    else if (LocKeyFields.Contains(property.Name, StringComparer.Ordinal)
                             && property.Value.ValueKind == JsonValueKind.String)
                    {
                        CheckLocKey(relative, property.Name, property.Value.GetString() ?? string.Empty);
                    }

                    Walk(property.Value, relative);
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Walk(item, relative);
                }

                break;
        }
    }

    private void CheckTemplateName(string relative, string templateName)
    {
        TemplateNamesSeen++;
        foreach (var fragment in ForbiddenTemplateFragments)
        {
            if (templateName.Contains(fragment, StringComparison.Ordinal))
            {
                Hit(relative, "uses the forbidden template/loc fragment '" + fragment + "' in TemplateName '"
                              + templateName + "' (contract §2.7)");
            }
        }

        if (!TemplateNamePattern.IsMatch(templateName))
        {
            Hit(relative, "TemplateName '" + templateName
                          + "' does not follow SteamBoiler.<Size>.<Tier>.<Faction> (contract §2.7)");
        }

        if (_templateNameFiles.TryGetValue(templateName, out var otherFile) && !string.Equals(otherFile, relative, StringComparison.Ordinal))
        {
            Hit(relative, "declares TemplateName '" + templateName + "' which is already declared by " + otherFile
                          + " (template names must be unique)");
        }
        else
        {
            _templateNameFiles[templateName] = relative;
        }
    }

    private void CheckLocKey(string relative, string field, string key)
    {
        foreach (var fragment in ForbiddenTemplateFragments)
        {
            if (key.Contains(fragment, StringComparison.Ordinal))
            {
                Hit(relative, field + " '" + key + "' uses the forbidden fragment '" + fragment + "' (contract §5.5)");
            }
        }

        if (!key.StartsWith("Building.SEE.", StringComparison.Ordinal)
            && !key.StartsWith("Status.SEE.", StringComparison.Ordinal)
            && !key.StartsWith("UI.SEE.", StringComparison.Ordinal))
        {
            Hit(relative, field + " '" + key + "' is not in the allowed Building.SEE.* / Status.SEE.* / UI.SEE.* "
                                  + "namespace (contract §5.5)");
        }
    }

    private void LoadBalanceTokens()
    {
        _authoritativeNumbers = null;
        _tableNumbers = null;

        var path = Path.Combine(_dataDir, "engines.json");
        if (!File.Exists(path))
        {
            _report.Warn("forbidden", "engines.json is missing; the hard-coded-balance scan is skipped");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var authoritative = new HashSet<string>(StringComparer.Ordinal);
            var table = new HashSet<string>(StringComparer.Ordinal);

            if (root.TryGetProperty("engines", out var engines) && engines.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in engines.EnumerateArray())
                {
                    AddNumber(authoritative, entry, "ratedHorsepower", integer: true);
                    AddNumber(table, entry, "ratedHorsepower", integer: true);
                    AddNumber(authoritative, entry, "scienceCost", integer: true);
                    AddNumber(table, entry, "scienceCost", integer: true);
                    AddNumber(authoritative, entry, "fuelPerHour", integer: false);
                    AddNumber(authoritative, entry, "waterPerHour", integer: false);

                    if (entry.TryGetProperty("buildCost", out var costs) && costs.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var cost in costs.EnumerateArray())
                        {
                            if (cost.TryGetProperty("amount", out var amount) && amount.TryGetInt32(out var value)
                                && value >= 45)
                            {
                                authoritative.Add(value.ToString(CultureInfo.InvariantCulture));
                            }
                        }
                    }
                }
            }

            _authoritativeNumbers = authoritative;
            _tableNumbers = table;
        }
        catch (JsonException exception)
        {
            _report.Warn("forbidden", "engines.json is not valid JSON; the hard-coded-balance scan is skipped: "
                                      + exception.Message);
        }
    }

    private static void AddNumber(HashSet<string> tokens, JsonElement entry, string name, bool integer)
    {
        if (!entry.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return;
        }

        if (integer)
        {
            if (value.TryGetInt32(out var number) && number >= 45)
            {
                tokens.Add(number.ToString(CultureInfo.InvariantCulture));
            }

            return;
        }

        if (value.TryGetDecimal(out var decimalValue))
        {
            tokens.Add(decimalValue.ToString("0.00", CultureInfo.InvariantCulture));
        }
    }

    private static string? NormalizeToken(string literal)
    {
        if (!decimal.TryParse(literal, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        return value == decimal.Truncate(value) && !literal.Contains('.', StringComparison.Ordinal)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private void Hit(string relative, string message)
    {
        Hits++;
        _report.Error("forbidden", relative + ": " + message);
    }

    private static string? ExtractBody(string text, int braceIndex)
    {
        if (braceIndex < 0 || braceIndex >= text.Length || text[braceIndex] != '{')
        {
            return null;
        }

        var depth = 0;
        for (var index = braceIndex; index < text.Length; index++)
        {
            var ch = text[index];
            if (ch == '{')
            {
                depth++;
            }
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return text.Substring(braceIndex, index - braceIndex + 1);
                }
            }
        }

        return null;
    }

    private static bool HasSegment(string path, string segment)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => string.Equals(part, segment, StringComparison.OrdinalIgnoreCase));
    }
}
