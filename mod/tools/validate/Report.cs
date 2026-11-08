using System.Text;

namespace SEE.Tools.Validate;

/// <summary>One named gate group of the validator (name + number of problems it produced).</summary>
internal sealed record CheckResult(string Name, int Errors);

/// <summary>
/// Collects validation problems, prints one PASS/FAIL line per gate group and produces the final
/// exit code. Warnings do not change the exit code.
/// </summary>
internal sealed class Report
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();
    private readonly List<CheckResult> _checks = new();

    public int ErrorCount => _errors.Count;

    public int WarningCount => _warnings.Count;

    /// <summary>Number of gate groups that ran (each prints exactly one PASS/FAIL line).</summary>
    public int CheckCount => _checks.Count;

    public void Error(string category, string message) => _errors.Add("[" + category + "] " + message);

    public void Warn(string category, string message) => _warnings.Add("[" + category + "] " + message);

    /// <summary>
    /// Runs one named gate group and prints its PASS/FAIL line. Exceptions are turned into an error
    /// so a broken check cannot abort the remaining gates or escape as a crash.
    /// </summary>
    /// <param name="name">Stable gate name; build.ps1 matches these lines against release-gates.json.</param>
    /// <param name="action">The checks to run.</param>
    public void Check(string name, Action action)
    {
        var before = _errors.Count;
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Error("internal", name + " threw " + exception.GetType().Name + ": " + exception.Message);
        }

        var errors = _errors.Count - before;
        _checks.Add(new CheckResult(name, errors));
        Console.WriteLine((errors == 0 ? "PASS  " : "FAIL  ") + name
                          + (errors == 0 ? string.Empty : "  (" + errors + " error(s))"));
    }

    /// <summary>Prints the gate summary, the warnings/errors and the final verdict. Returns the exit code.</summary>
    public int Finish(TextWriter output)
    {
        output.WriteLine();
        output.WriteLine("Gate summary");

        var passed = 0;
        foreach (var check in _checks)
        {
            if (check.Errors == 0)
            {
                passed++;
            }

            output.WriteLine("  " + (check.Errors == 0 ? "PASS" : "FAIL") + "  " + check.Name
                             + (check.Errors == 0 ? string.Empty : "  (" + check.Errors + " error(s))"));
        }

        output.WriteLine();
        output.WriteLine("  " + passed + "/" + _checks.Count + " gate group(s) passed, "
                         + _warnings.Count + " warning(s), " + _errors.Count + " error(s)");
        output.WriteLine();

        if (_warnings.Count == 0)
        {
            output.WriteLine("WARN: none");
        }
        else
        {
            foreach (var warning in _warnings)
            {
                output.WriteLine("WARN: " + warning);
            }
        }

        output.WriteLine();

        if (_errors.Count == 0)
        {
            output.WriteLine("VALIDATION PASSED");
            return 0;
        }

        foreach (var error in _errors)
        {
            output.WriteLine("ERROR: " + error);
        }

        output.WriteLine();
        output.WriteLine("VALIDATION FAILED (" + _errors.Count + " error(s), " + passed + "/" + _checks.Count
                         + " gate group(s) passed)");
        return 1;
    }
}

/// <summary>Minimal RFC 4180 CSV reader, enough for the game's localization files.</summary>
internal static class Csv
{
    public static List<string[]> Parse(string text)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var index = 0;

        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            index = 1;
        }

        while (index < text.Length)
        {
            var ch = text[index];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        field.Append('"');
                        index += 2;
                        continue;
                    }

                    inQuotes = false;
                    index++;
                    continue;
                }

                field.Append(ch);
                index++;
                continue;
            }

            switch (ch)
            {
                case '"':
                    inQuotes = true;
                    index++;
                    break;

                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    index++;
                    break;

                case '\r':
                    index++;
                    break;

                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row.ToArray());
                    row.Clear();
                    index++;
                    break;

                default:
                    field.Append(ch);
                    index++;
                    break;
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row.ToArray());
        }

        return rows;
    }
}
