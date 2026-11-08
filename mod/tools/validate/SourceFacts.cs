using System.Text.RegularExpressions;

namespace SEE.Tools.Validate;

/// <summary>
/// Static "source facts" gate over <c>mod/src</c>, <c>mod/tools</c> and <c>mod/tests</c>:
///
/// <list type="bullet">
/// <item>no leftovers from the previous mod's business (its family names, product identifiers and
/// file names) — the fork must be semantically clean, not just renamed;</item>
/// <item>no compile-time reference to a third-party mod DLL: csproj HintPaths must not point at
/// Timber Pipes / Power Lines / Conveyor Belts / Harmony, and the C# code (comments and string
/// literals stripped) must not mention <c>BuildingPipeSpec</c>/<c>PowerLineSpec</c>; string
/// type names used for runtime detection stay legal;</item>
/// <item>no Harmony compile-time dependency (no <c>HarmonyLib</c> identifier outside strings);</item>
/// <item>zero NuGet packages in the csproj files (offline build rule);</item>
/// <item>file-size limits: 400 lines per C# file, 600 for spec definitions (§11.5);</item>
/// <item>stub exceptions are reported as a warning for the lead to review (not a hard failure,
/// because the P0 skeleton may still carry TODO placeholders).</item>
/// </list>
///
/// This check reports through <see cref="Report"/>, so any violation produces VALIDATION FAILED.
/// </summary>
internal sealed class SourceFacts
{
    private const int MaxSourceFileLines = 400;
    private const int MaxSpecFileLines = 600;

    private static readonly string[] Roots = { "src", "tools", "tests" };

    private static readonly string[] ForbiddenNameFragments =
    {
        "M" + "WP",
        "Mechanical" + "Water" + "Pu" + "mp",
        "Pu" + "mp",
    };

    private static readonly string[] ExternalModAssemblyNames =
    {
        "TimberPipes", "PowerLines", "ConveyorBelt", "ConveyorBelts", "Harmony",
    };

    // Assembled at runtime so this file does not contain the forbidden identifiers by accident.
    private static readonly string[] ForbiddenSpecIdentifiers =
    {
        "Building" + "PipeSpec",
        "PowerLine" + "Spec",
    };

    private static readonly Regex HarmonyIdentifier =
        new(@"\bHarmony(Lib)?\b", RegexOptions.Compiled);

    // Assembled at runtime so this file does not report itself when scanned.
    private static readonly string NotImplementedSyntax = "NotImplemented" + "Exception";

    private readonly string _modRoot;
    private readonly Report _report;

    private SourceFacts(string modRoot, Report report)
    {
        _modRoot = modRoot;
        _report = report;
    }

    /// <summary>Number of source files (cs / csproj / ps1) inspected.</summary>
    public int ScannedFileCount { get; private set; }

    /// <summary>Total C# lines inspected.</summary>
    public int SourceLineCount { get; private set; }

    /// <summary>Files that still contain a stub exception.</summary>
    public int NotImplementedFiles { get; private set; }

    public static SourceFacts Load(string modRoot, Report report)
    {
        var facts = new SourceFacts(modRoot, report);
        facts.Scan();
        return facts;
    }

    private void Scan()
    {
        foreach (var root in Roots)
        {
            var directory = Path.Combine(_modRoot, root);
            if (!Directory.Exists(directory))
            {
                _report.Error("source", "source root '" + root + "' is missing under the mod root");
                continue;
            }

            foreach (var file in EnumerateSourceFiles(directory))
            {
                ScannedFileCount++;
                var relative = Relative(file);

                CheckName(relative);

                var extension = Path.GetExtension(file);
                if (string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    CheckProjectFile(relative, File.ReadAllText(file));
                    continue;
                }

                if (!string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                var lineCount = CountLines(text);
                if (root == "src")
                {
                    SourceLineCount += lineCount;
                }

                CheckBannedWords(relative, text);
                var codeOnly = StripCommentsAndLiterals(text);
                CheckHardReferences(relative, codeOnly);
                CheckNotImplemented(relative, text);

                if (root == "src")
                {
                    CheckFileSize(relative, file, lineCount);
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateSourceFiles(string directory)
    {
        var extensions = new[] { ".cs", ".csproj", ".ps1" };
        return Directory
            .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !HasDirectorySegment(path, "bin") && !HasDirectorySegment(path, "obj"))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasDirectorySegment(string path, string segment)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => string.Equals(part, segment, StringComparison.OrdinalIgnoreCase));
    }

    private void CheckName(string relative)
    {
        var fileName = Path.GetFileName(relative);
        foreach (var fragment in ForbiddenNameFragments)
        {
            if (fileName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                _report.Error("source", relative + ": the file name still contains the previous mod's business word '"
                                         + fragment + "'");
                return;
            }
        }
    }

    private void CheckBannedWords(string relative, string text)
    {
        var lines = text.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            foreach (var fragment in ForbiddenNameFragments)
            {
                if (lines[index].Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    _report.Error("source", relative + ":" + (index + 1)
                                             + ": bans the previous mod's business word '" + fragment + "'");
                }
            }
        }
    }

    /// <summary>Removes comments, string literals and char literals so identifier checks see code only.</summary>
    internal static string StripCommentsAndLiterals(string text)
    {
        var result = new char[text.Length];
        var inString = false;
        var inChar = false;
        var inLineComment = false;
        var inBlockComment = false;
        var inVerbatimString = false;
        var index = 0;

        while (index < text.Length)
        {
            var ch = text[index];
            var next = index + 1 < text.Length ? text[index + 1] : '\0';

            if (inLineComment)
            {
                if (ch == '\n')
                {
                    inLineComment = false;
                    result[index] = '\n';
                }
                else
                {
                    result[index] = ' ';
                }

                index++;
                continue;
            }

            if (inBlockComment)
            {
                if (ch == '*' && next == '/')
                {
                    inBlockComment = false;
                    result[index] = ' ';
                    result[index + 1] = ' ';
                    index += 2;
                    continue;
                }

                result[index] = ch == '\n' ? '\n' : ' ';
                index++;
                continue;
            }

            if (inString)
            {
                if (inVerbatimString)
                {
                    if (ch == '"' && next == '"')
                    {
                        result[index] = ' ';
                        result[index + 1] = ' ';
                        index += 2;
                        continue;
                    }

                    if (ch == '"')
                    {
                        inString = false;
                    }
                }
                else
                {
                    if (ch == '\\')
                    {
                        result[index] = ' ';
                        if (index + 1 < text.Length)
                        {
                            result[index + 1] = ' ';
                        }

                        index += 2;
                        continue;
                    }

                    if (ch == '"')
                    {
                        inString = false;
                    }
                }

                result[index] = ch == '\n' ? '\n' : ' ';
                index++;
                continue;
            }

            if (inChar)
            {
                if (ch == '\\')
                {
                    result[index] = ' ';
                    if (index + 1 < text.Length)
                    {
                        result[index + 1] = ' ';
                    }

                    index += 2;
                    continue;
                }

                if (ch == '\'')
                {
                    inChar = false;
                }

                result[index] = ' ';
                index++;
                continue;
            }

            if (ch == '/' && next == '/')
            {
                inLineComment = true;
                result[index] = ' ';
                result[index + 1] = ' ';
                index += 2;
                continue;
            }

            if (ch == '/' && next == '*')
            {
                inBlockComment = true;
                result[index] = ' ';
                result[index + 1] = ' ';
                index += 2;
                continue;
            }

            if (ch == '@' && next == '"')
            {
                inString = true;
                inVerbatimString = true;
                result[index] = ' ';
                result[index + 1] = ' ';
                index += 2;
                continue;
            }

            if (ch == '"')
            {
                inString = true;
                result[index] = ' ';
                index++;
                continue;
            }

            if (ch == '\'')
            {
                inChar = true;
                result[index] = ' ';
                index++;
                continue;
            }

            result[index] = ch;
            index++;
        }

        return new string(result);
    }

    private void CheckHardReferences(string relative, string codeOnly)
    {
        foreach (var identifier in ForbiddenSpecIdentifiers)
        {
            if (codeOnly.Contains(identifier, StringComparison.Ordinal))
            {
                _report.Error("source", relative + ": hard-codes the previous mod's blueprint type '" + identifier
                                         + "'; external-mod spec names may only appear as runtime strings");
            }
        }

        if (HarmonyIdentifier.IsMatch(codeOnly))
        {
            _report.Error("source", relative + ": compile-time Harmony reference found; P0 must not depend on Harmony "
                                     + "(runtime string detection only)");
        }
    }

    private void CheckProjectFile(string relative, string text)
    {
        // XML comments are documentation; strip them before looking for real references so a
        // "we do not depend on Harmony" comment does not trip the gate.
        var stripped = Regex.Replace(text, "<!--.*?-->", string.Empty, RegexOptions.Singleline);

        foreach (var assembly in ExternalModAssemblyNames)
        {
            if (stripped.Contains(assembly, StringComparison.OrdinalIgnoreCase))
            {
                _report.Error("source", relative + ": references the external/Harmony assembly '" + assembly
                                         + "' at compile time (external mods must stay runtime-detected)");
            }
        }

        if (Regex.IsMatch(stripped, "<PackageReference\\b", RegexOptions.IgnoreCase))
        {
            _report.Error("source", relative + ": NuGet PackageReference is forbidden (offline build; Mono.Cecil uses "
                                     + "the local cache via HintPath)");
        }
    }

    private void CheckNotImplemented(string relative, string text)
    {
        if (!text.Contains(NotImplementedSyntax, StringComparison.Ordinal))
        {
            return;
        }

        NotImplementedFiles++;
        _report.Warn("source", relative + " still throws " + NotImplementedSyntax
                               + "; a Release gate must not ship stubs");
    }

    private void CheckFileSize(string relative, string path, int lineCount)
    {
        var isSpec = Path.GetFileName(path).EndsWith("Spec.cs", StringComparison.OrdinalIgnoreCase)
                     || HasDirectorySegment(path, "Specs");
        var limit = isSpec ? MaxSpecFileLines : MaxSourceFileLines;
        if (lineCount > limit)
        {
            _report.Error("source", relative + " has " + lineCount + " lines; the limit is " + limit
                                     + (isSpec ? " for spec files" : string.Empty) + " (§11.5)");
        }
    }

    private static int CountLines(string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        var count = 1;
        foreach (var ch in text)
        {
            if (ch == '\n')
            {
                count++;
            }
        }

        if (text[^1] == '\n')
        {
            count--;
        }

        return count;
    }

    private string Relative(string path) =>
        Path.GetRelativePath(_modRoot, path).Replace('\\', '/');
}
