using System.Globalization;
using System.Text.Json;

namespace SEE.Tools.Validate;

/// <summary>
/// Validates <c>data/SEE_Config.json</c> against the requirements §20 parameter table
/// (configSchemaVersion, the documented groups, every key's type and clamp range).
///
/// The clamp table is written down here instead of being parsed from Core: the config loader may
/// tolerate bad input (clamp + warn), but the shipped default file must already be inside the
/// supported ranges — a shipped value outside its range is a data bug, not a player choice.
/// Unknown keys also fail: the shipped file is generated from the requirements table, so a key
/// that is not in that table is either a typo or a leftover from the previous mod.
/// </summary>
internal sealed class ConfigValidator
{
    private const int ExpectedSchemaVersion = 1;

    private static readonly string[] KnownGroups =
    {
        "balance", "maintenance", "compat", "perf", "anim", "art", "ui",
    };

    private static readonly string[] RequiredGroups =
    {
        "balance", "maintenance", "compat", "perf", "anim",
    };

    /// <summary>The shipped file must document at least six groups (requirements §20 / §11.4).</summary>
    private const int MinimumGroupCount = 6;

    /// <summary>§4.1 authoritative science costs (base values; both factions share them).</summary>
    private static readonly Dictionary<string, int> ScienceCostBase = new(StringComparer.Ordinal)
    {
        ["Small.T1"] = 90,
        ["Medium.T1"] = 150,
        ["Large.T1"] = 225,
        ["Small.T2"] = 240,
        ["Medium.T2"] = 400,
        ["Large.T2"] = 600,
        ["Small.T3"] = 420,
        ["Medium.T3"] = 700,
        ["Large.T3"] = 1050,
        ["Small.T4"] = 900,
        ["Medium.T4"] = 1500,
        ["Large.T4"] = 2250,
    };

    private enum RuleKind
    {
        Number,
        Integer,
        Boolean,
        Enum,
        NumberArray,
    }

    private sealed record Rule(
        RuleKind Kind,
        double Min = double.MinValue,
        double Max = double.MaxValue,
        double AltMin = double.MinValue,
        double AltMax = double.MaxValue,
        string[]? Allowed = null,
        int ArrayLength = 0)
    {
        public bool HasAlternative => AltMin != double.MinValue && AltMax != double.MaxValue;
    }

    private static readonly Dictionary<string, Rule> Rules = BuildRules();

    private readonly Report _report;

    public ConfigValidator(Report report)
    {
        _report = report;
    }

    public void Validate(string dataDir)
    {
        var path = Path.Combine(dataDir, "SEE_Config.json");
        if (!File.Exists(path))
        {
            _report.Error("config", "SEE_Config.json is missing (the mod ships a default config file)");
            return;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path));
        }
        catch (JsonException ex)
        {
            _report.Error("config", "SEE_Config.json is not valid JSON: " + ex.Message);
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                _report.Error("config", "SEE_Config.json must be a JSON object");
                return;
            }

            ValidateSchemaVersion(root);
            var groups = ValidateGroups(root);

            var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var pair in groups)
            {
                Flatten(pair.Key, pair.Value, values);
            }

            foreach (var pair in values)
            {
                ValidateValue(pair.Key, pair.Value);
            }
        }
    }

    private void ValidateSchemaVersion(JsonElement root)
    {
        if (!root.TryGetProperty("configSchemaVersion", out var version))
        {
            _report.Error("config", "SEE_Config.json is missing configSchemaVersion");
            return;
        }

        if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number))
        {
            _report.Error("config", "configSchemaVersion must be an integer");
            return;
        }

        if (number != ExpectedSchemaVersion)
        {
            _report.Error("config", "configSchemaVersion must be " + ExpectedSchemaVersion + ", found " + number);
        }
    }

    private List<KeyValuePair<string, JsonElement>> ValidateGroups(JsonElement root)
    {
        var groups = new List<KeyValuePair<string, JsonElement>>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, "configSchemaVersion", StringComparison.Ordinal))
            {
                continue;
            }

            if (!KnownGroups.Contains(property.Name, StringComparer.Ordinal))
            {
                _report.Error("config", "unknown top-level config group '" + property.Name
                                        + "' (expected one of: " + string.Join(", ", KnownGroups) + ")");
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                _report.Error("config", "config group '" + property.Name + "' must be a JSON object");
                continue;
            }

            seen.Add(property.Name);
            groups.Add(new KeyValuePair<string, JsonElement>(property.Name, property.Value));
        }

        foreach (var group in RequiredGroups)
        {
            if (!seen.Contains(group))
            {
                _report.Error("config", "required config group '" + group + "' is missing");
            }
        }

        if (seen.Count < MinimumGroupCount)
        {
            _report.Error("config", "SEE_Config.json documents " + seen.Count + " groups; the requirements list "
                                    + MinimumGroupCount + " (balance/maintenance/compat/perf/anim/art/ui)");
        }

        return groups;
    }

    /// <summary>Flattens the nested config tree into dotted paths ("balance.faction.outputMultiplier.Folktails").</summary>
    private static void Flatten(string prefix, JsonElement element, Dictionary<string, JsonElement> values)
    {
        foreach (var property in element.EnumerateObject())
        {
            var path = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                Flatten(path, property.Value, values);
            }
            else
            {
                values[path] = property.Value;
            }
        }
    }

    private void ValidateValue(string path, JsonElement value)
    {
        if (ScienceCostBase.TryGetScienceCost(path, out var baseCost))
        {
            ValidateScienceCost(path, value, baseCost);
            return;
        }

        if (!Rules.TryGetValue(path, out var rule))
        {
            _report.Error("config", "unknown config key '" + path
                                    + "' (not part of the requirements §20 parameter table)");
            return;
        }

        switch (rule.Kind)
        {
            case RuleKind.Boolean:
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    _report.Error("config", "'" + path + "' must be a boolean, found " + value.ValueKind);
                }

                break;

            case RuleKind.Enum:
                if (value.ValueKind != JsonValueKind.String)
                {
                    _report.Error("config", "'" + path + "' must be a string enum, found " + value.ValueKind);
                    break;
                }

                var text = value.GetString() ?? string.Empty;
                if (rule.Allowed is not null && !rule.Allowed.Contains(text, StringComparer.Ordinal))
                {
                    _report.Error("config", "'" + path + "' has unsupported value '" + text
                                            + "' (allowed: " + string.Join(" / ", rule.Allowed) + ")");
                }

                break;

            case RuleKind.Integer:
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var integer))
                {
                    _report.Error("config", "'" + path + "' must be an integer, found " + value.ValueKind);
                    break;
                }

                if (integer < rule.Min || integer > rule.Max)
                {
                    _report.Error("config", "'" + path + "' = " + integer + " is outside the supported range ["
                                            + Format(rule.Min) + ", " + Format(rule.Max) + "]");
                }

                break;

            case RuleKind.Number:
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number))
                {
                    _report.Error("config", "'" + path + "' must be a number, found " + value.ValueKind);
                    break;
                }

                CheckRange(path, number, rule);
                break;

            case RuleKind.NumberArray:
                if (value.ValueKind != JsonValueKind.Array)
                {
                    _report.Error("config", "'" + path + "' must be an array of numbers, found " + value.ValueKind);
                    break;
                }

                var items = value.EnumerateArray().ToList();
                if (rule.ArrayLength > 0 && items.Count != rule.ArrayLength)
                {
                    _report.Error("config", "'" + path + "' must have exactly " + rule.ArrayLength
                                            + " entries, found " + items.Count);
                }

                var index = 0;
                foreach (var item in items)
                {
                    if (item.ValueKind != JsonValueKind.Number || !item.TryGetDouble(out var itemNumber))
                    {
                        _report.Error("config", "'" + path + "[" + index + "]' must be a number");
                    }
                    else if (itemNumber < rule.Min || itemNumber > rule.Max)
                    {
                        _report.Error("config", "'" + path + "[" + index + "]' = " + Format(itemNumber)
                                                + " is outside the supported range [" + Format(rule.Min) + ", "
                                                + Format(rule.Max) + "]");
                    }

                    index++;
                }

                break;
        }
    }

    private void CheckRange(string path, double number, Rule rule)
    {
        var inPrimary = number >= rule.Min && number <= rule.Max;
        var inAlternative = rule.HasAlternative && number >= rule.AltMin && number <= rule.AltMax;
        if (inPrimary || inAlternative)
        {
            return;
        }

        var range = "[" + Format(rule.Min) + ", " + Format(rule.Max) + "]";
        if (rule.HasAlternative)
        {
            range += " or [" + Format(rule.AltMin) + ", " + Format(rule.AltMax) + "]";
        }

        _report.Error("config", "'" + path + "' = " + Format(number) + " is outside the supported range " + range);
    }

    private void ValidateScienceCost(string path, JsonElement value, int baseCost)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
        {
            _report.Error("config", "'" + path + "' must be an integer, found " + value.ValueKind);
            return;
        }

        // REQ: each per-file science cost may be tuned +-20% around the §4.1 table value, clamped to
        // the tech band; T4 may never exceed 2250.
        var min = (int)Math.Floor(baseCost * 0.8);
        var max = (int)Math.Ceiling(baseCost * 1.2);
        if (path.EndsWith(".T4", StringComparison.Ordinal))
        {
            max = Math.Min(max, 2250);
        }

        if (number < min || number > max)
        {
            _report.Error("config", "'" + path + "' = " + number + " is outside the supported +-20% band ["
                                    + min + ", " + max + "] around the §4.1 value " + baseCost);
        }
    }

    private static string Format(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private static Dictionary<string, Rule> BuildRules()
    {
        var rules = new Dictionary<string, Rule>(StringComparer.Ordinal);
        var number = new Func<double, double, Rule>((min, max) => new Rule(RuleKind.Number, min, max));
        var integer = new Func<double, double, Rule>((min, max) => new Rule(RuleKind.Integer, min, max));
        var boolean = new Rule(RuleKind.Boolean);

        foreach (var tier in new[] { "T1", "T2", "T3", "T4" })
        {
            rules["balance.fuelRatePer100hp." + tier] = number(0.010, 0.040);
            rules["balance.waterRatePer100hp." + tier] = number(0.010, 0.100);
            rules["maintenance.intervalDays." + tier] = integer(4, 20);
            rules["anim.baseRevPerSec." + tier] = tier switch
            {
                "T1" => number(0.6, 2.0),
                "T2" => number(1.0, 3.0),
                "T3" => number(1.5, 4.0),
                _ => number(2.0, 6.0),
            };
        }

        foreach (var faction in new[] { "Folktails", "IronTeeth" })
        {
            rules["balance.faction.outputMultiplier." + faction] = number(0.95, 1.10);
            rules["balance.faction.fuelMultiplier." + faction] = number(0.80, 1.10);
            rules["anim.factionSpeed." + faction] = number(0.80, 1.20);
        }

        foreach (var size in new[] { "Small", "Medium", "Large" })
        {
            rules["anim.sizeSpeed." + size] = number(0.70, 1.10);
            rules["art.trisLod0." + size] = size switch
            {
                "Small" => integer(2450, 4550),
                "Medium" => integer(4200, 7800),
                _ => integer(6300, 11700),
            };
            rules["art.textureSize." + size] = size switch
            {
                "Small" => integer(256, 1024),
                _ => integer(512, 2048),
            };
        }

        // balance (§20.1)
        rules["balance.fullInventoryWorkHours"] = integer(10, 50);
        rules["balance.waterTankHours"] = integer(100, 300);
        rules["balance.fuelScaling"] = new Rule(RuleKind.Enum, Allowed: new[] { "proportional", "vanilla" });
        // 2026-11-08 hotfix-3: minimum running load kept when the network demand is 0 (night).
        rules["balance.standbyLoadRatio"] = number(0, 1);
        rules["balance.buildCostMultiplier"] = number(0.80, 1.20);

        // maintenance (§20.2)
        rules["maintenance.enabled"] = boolean;
        rules["maintenance.loadFactorBase"] = number(0.2, 0.8);
        rules["maintenance.autoThreshold"] = number(20, 80);
        rules["maintenance.manualWarnAbove"] = number(50, 100);
        rules["maintenance.durationHours"] = number(1, 8);
        rules["maintenance.manualMaterialsEnabled"] = boolean;

        // compat (§20.3)
        rules["compat.waterSupplyMode"] = new Rule(RuleKind.Enum, Allowed: new[] { "auto", "tank", "valve", "hauler" });
        rules["compat.detectionBudgetSeconds"] = number(1, 5);
        rules["compat.enablePowerLineInjection"] = boolean;
        rules["compat.beltFuelMode"] = new Rule(RuleKind.Enum, Allowed: new[] { "auto", "haulers" });
        rules["compat.logThrottleSeconds"] = number(10, 120);
        rules["compat.pipePollutionWarn"] = boolean;

        // perf (§20.4)
        rules["perf.tickSeconds"] = number(0.5, 2.0);
        rules["perf.uiRefreshHz"] = number(1, 10);
        rules["perf.badgeRefreshHz"] = number(0.5, 2);
        rules["perf.maxBadgesVisible"] = integer(16, 256);
        rules["perf.lodDistances"] = new Rule(RuleKind.NumberArray, 10, 80, ArrayLength: 2);

        // anim (§20.5)
        rules["anim.enable"] = boolean;
        rules["anim.updateHz"] = number(15, 60);
        rules["anim.lod0MaxDistance"] = number(10, 30);
        rules["anim.lod1MaxDistance"] = number(25, 60);
        rules["anim.vfxEmissionScale"] = number(0, 2.0);
        rules["anim.startSeconds"] = number(1.0, 2.5);
        rules["anim.stopSeconds"] = number(2.0, 5.0);
        rules["anim.pistonPhaseDeg"] = number(60, 120);
        rules["anim.jitterDeg"] = new Rule(RuleKind.NumberArray, 0, 8, ArrayLength: 4);
        rules["anim.maxParticlesPerBuilding"] = integer(40, 300);
        rules["anim.exhaustPulseSeconds"] = number(0.1, 0.5);

        // art (§20.6): the two LOD ratios accept both percent (50) and fraction (0.5) notation.
        rules["art.lod1Ratio"] = new Rule(RuleKind.Number, 0.30, 0.60, 30, 60);
        rules["art.lod2Ratio"] = new Rule(RuleKind.Number, 0.05, 0.20, 5, 20);
        rules["art.smokeAttachmentCount"] = integer(1, 4);
        rules["art.chimneyOverhangTiles"] = integer(0, 1);

        // ui (§20.4 tail)
        rules["ui.lowSupplyHours"] = number(2, 24);
        rules["ui.showIdleBadge"] = boolean;

        return rules;
    }
}

internal static class ScienceCostPathExtensions
{
    /// <summary>Maps "balance.scienceCost.&lt;Size&gt;.&lt;Tier&gt;" to the §4.1 base science cost.</summary>
    public static bool TryGetScienceCost(this Dictionary<string, int> table, string path, out int baseCost)
    {
        const string prefix = "balance.scienceCost.";
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            baseCost = 0;
            return false;
        }

        baseCost = 0;
        return table.TryGetValue(path[prefix.Length..], out baseCost);
    }
}
