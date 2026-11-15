namespace SEE.Tools.Validate;

/// <summary>
/// Cross-checks the shipped localization files (REQ-UI-04/05/08, CONTRACT loc section):
///
/// <list type="bullet">
/// <item><c>enUS.csv</c> and <c>zhTW.csv</c> must exist and have the <c>ID,Text,Comment</c> header;</item>
/// <item>both files must contain exactly the canonical key set (<see cref="LocKeys"/>, 63 keys =
/// the 54-key requirements baseline + 9 P4B additions) — a missing key, a stale key from the
/// previous mod or an extra ad-hoc key fails validation;</item>
/// <item>texts must be non-empty, must not echo the raw key, must keep the same <c>{0}</c>/<c>{1}</c>
/// placeholder set in every language, and zhTW must not contain simplified characters;</item>
/// <item>keys forbidden by the requirements (<c>SteamEngine</c>/<c>SteamPlant</c> fragments) are rejected;</item>
/// <item>every localization key referenced by a blueprint must exist (collection from the blueprint pass);</item>
/// <item><c>zhCN.csv</c> is optional; when present it is held to the same key set and placeholder rules.</item>
/// </list>
/// </summary>
internal sealed class Localizations
{
    private const string RequiredLanguage = "enUS";
    private const string TraditionalLanguage = "zhTW";
    private const string OptionalSimplifiedLanguage = "zhCN";

    /// <summary>
    /// Simplified-only characters that may not appear in the Traditional Chinese file. The list
    /// is curated from the requirement vocabulary (steam engine panel strings) and contains only
    /// characters whose simplified form is not used in Traditional Chinese, to avoid false
    /// positives on shared characters (e.g. 型 / 料 / 力 / 停 are legal in zhTW).
    /// </summary>
    private static readonly HashSet<char> SimplifiedOnly = new(
        ("这个们时间开关发动设备产业务维护状态压阀门铁齿炉烧热输带网电线检测无数据传类错" +
         "误运转暂继续补给与为会说话问题实现计画优变应该显隐权限链储读写删选择档库车号轮轴" +
         "灯费启闭资").ToCharArray());

    private readonly Report _report;
    private readonly Dictionary<string, Dictionary<string, string>> _texts = new(StringComparer.Ordinal);

    public Localizations(Report report)
    {
        _report = report;
    }

    public void Validate(string dataDir, IReadOnlyCollection<string> usedLocKeys)
    {
        var directory = Path.Combine(dataDir, "Localizations");
        if (!Directory.Exists(directory))
        {
            _report.Error("localization", "data/Localizations is missing");
            return;
        }

        foreach (var language in new[] { RequiredLanguage, TraditionalLanguage })
        {
            var path = Path.Combine(directory, language + ".csv");
            if (!File.Exists(path))
            {
                _report.Error("localization", "Localizations/" + language + ".csv is missing (required)");
                continue;
            }

            Load(language, path);
        }

        var optionalPath = Path.Combine(directory, OptionalSimplifiedLanguage + ".csv");
        if (File.Exists(optionalPath))
        {
            Load(OptionalSimplifiedLanguage, optionalPath);
        }

        if (_texts.Count < 2)
        {
            return;
        }

        if (LocKeys.ExpectedCount < 54)
        {
            _report.Error("localization", "internal: canonical key set has " + LocKeys.ExpectedCount
                                          + " keys, the requirements baseline is at least 54");
        }

        var canonical = new HashSet<string>(LocKeys.All, StringComparer.Ordinal);

        foreach (var pair in _texts)
        {
            var language = pair.Key;
            var texts = pair.Value;

            if (texts.Count != LocKeys.ExpectedCount)
            {
                _report.Error("localization", "Localizations/" + language + ".csv has " + texts.Count
                                              + " keys; the canonical set mandates exactly " + LocKeys.ExpectedCount);
            }

            foreach (var key in canonical)
            {
                if (!texts.ContainsKey(key))
                {
                    _report.Error("localization", "Localizations/" + language + ".csv is missing key '" + key + "'");
                }
            }

            foreach (var key in texts.Keys)
            {
                if (!canonical.Contains(key))
                {
                    _report.Error("localization", "'" + key + "' in " + language
                                                  + ".csv is not one of the " + LocKeys.ExpectedCount
                                                  + " canonical SEE keys (stale or ad-hoc key)");
                }

                foreach (var fragment in LocKeys.ForbiddenFragments)
                {
                    if (key.Contains(fragment, StringComparison.Ordinal))
                    {
                        _report.Error("localization", "'" + key + "' in " + language + ".csv uses the forbidden fragment '"
                                                      + fragment + "'");
                    }
                }
            }

            foreach (var key in usedLocKeys)
            {
                if (!texts.ContainsKey(key))
                {
                    _report.Error("localization", "a blueprint references '" + key + "' but " + language
                                                  + ".csv has no such key");
                }
            }
        }

        // Placeholder / emptiness / raw-key checks: the first loaded language is the reference set.
        foreach (var key in LocKeys.All)
        {
            var present = _texts.Where(pair => pair.Value.ContainsKey(key)).ToList();
            if (present.Count == 0)
            {
                continue;
            }

            var reference = Placeholders(present[0].Value[key]);
            foreach (var pair in present)
            {
                var language = pair.Key;
                var text = pair.Value[key];

                if (text.Length == 0)
                {
                    _report.Error("localization", "'" + key + "' has an empty " + language + " text");
                    continue;
                }

                if (string.Equals(text, key, StringComparison.Ordinal))
                {
                    _report.Error("localization", "'" + key + "' still shows the raw key in " + language + ".csv (REQ-UI-05)");
                }

                var placeholders = Placeholders(text);
                if (!placeholders.SequenceEqual(reference))
                {
                    _report.Error("localization", "'" + key + "' placeholder mismatch: " + present[0].Key
                                                  + " has {" + string.Join(",", reference) + "} but " + language
                                                  + " has {" + string.Join(",", placeholders) + "}");
                }
            }
        }

        CheckTraditionalCharacters();
    }

    /// <summary>zhTW must stay Traditional Chinese (PLAN section 7 item 7).</summary>
    private void CheckTraditionalCharacters()
    {
        if (!_texts.TryGetValue(TraditionalLanguage, out var texts))
        {
            return;
        }

        foreach (var pair in texts)
        {
            foreach (var ch in pair.Value)
            {
                if (!SimplifiedOnly.Contains(ch))
                {
                    continue;
                }

                _report.Error("localization", "zhTW '" + pair.Key + "' contains simplified character '" + ch
                                              + "' (zhTW must stay Traditional Chinese)");
                break;
            }
        }
    }

    private void Load(string language, string path)
    {
        var rows = Csv.Parse(File.ReadAllText(path));
        if (rows.Count == 0)
        {
            _report.Error("localization", "Localizations/" + language + ".csv is empty");
            return;
        }

        var header = rows[0];
        if (header.Length < 3 || header[0].Trim() != "ID" || header[1].Trim() != "Text" || header[2].Trim() != "Comment")
        {
            _report.Error("localization", "Localizations/" + language
                                          + ".csv header must be 'ID,Text,Comment' (found '"
                                          + string.Join(",", header) + "')");
        }

        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Length == 0 || (row.Length == 1 && row[0].Trim().Length == 0))
            {
                continue;
            }

            if (row.Length != 3)
            {
                _report.Error("localization", "Localizations/" + language + ".csv line " + (i + 1) + " has "
                                              + row.Length + " CSV fields; the game requires exactly 3 (ID,Text,Comment)");
                continue;
            }

            var id = row[0].Trim();
            if (id.Length == 0)
            {
                _report.Error("localization", "Localizations/" + language + ".csv line " + (i + 1) + " has an empty ID");
                continue;
            }

            if (!texts.TryAdd(id, row[1]))
            {
                _report.Error("localization", "duplicate key '" + id + "' in Localizations/" + language
                                              + ".csv line " + (i + 1));
            }
        }

        _texts[language] = texts;
    }

    private static List<int> Placeholders(string text)
    {
        var result = new List<int>();
        var index = 0;
        while (index < text.Length)
        {
            var start = text.IndexOf('{', index);
            if (start < 0)
            {
                break;
            }

            var end = text.IndexOf('}', start + 1);
            if (end < 0)
            {
                break;
            }

            var token = text.Substring(start + 1, end - start - 1);
            if (int.TryParse(token, out var number))
            {
                result.Add(number);
            }

            index = end + 1;
        }

        result.Sort();
        return result;
    }
}
