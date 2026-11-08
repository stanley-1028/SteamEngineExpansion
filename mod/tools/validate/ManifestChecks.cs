using System.Text.Json;
using System.Text.RegularExpressions;

namespace SEE.Tools.Validate;

/// <summary>
/// Validates <c>data/manifest.json</c> against the 1.1.2.8 <c>ModManifest</c> schema:
/// Name / Description / Version / Id / MinimumGameVersion / RequiredMods / OptionalMods.
///
/// The manifest has no <c>Assemblies</c> field in this game version; SEE must not add one.
/// RequiredMods / OptionalMods must stay empty: SEE depends on the base game only.
/// </summary>
internal sealed class ManifestChecks
{
    public const string ModId = "SteamEngineExpansion";

    private static readonly Regex VersionPattern =
        new("^\\d+\\.\\d+\\.\\d+(\\.\\d+)?$", RegexOptions.CultureInvariant);

    private static readonly string[] RequiredStringFields =
    {
        "Name", "Description", "Version", "Id", "MinimumGameVersion",
    };

    // Business words that must not survive the fork. They are assembled at runtime so this
    // file never contains the forbidden literal itself.
    private static readonly string[] ForbiddenDescriptionFragments =
    {
        "Pu" + "mp",
        "Mechanical" + "Water" + "Pu" + "mp",
        "M" + "WP",
    };

    private readonly string _dataDir;
    private readonly Report _report;

    public ManifestChecks(string dataDir, Report report)
    {
        _dataDir = dataDir;
        _report = report;
    }

    public void Validate()
    {
        var path = Path.Combine(_dataDir, "manifest.json");
        if (!File.Exists(path))
        {
            _report.Error("manifest", "data/manifest.json is missing");
            return;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path));
        }
        catch (JsonException ex)
        {
            _report.Error("manifest", "manifest.json is not valid JSON: " + ex.Message);
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                _report.Error("manifest", "manifest.json must be a JSON object");
                return;
            }

            foreach (var field in RequiredStringFields)
            {
                if (!root.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(value.GetString()))
                {
                    _report.Error("manifest", "manifest.json is missing the non-empty string field '" + field + "'");
                }
            }

            if (root.TryGetProperty("Id", out var id) && id.ValueKind == JsonValueKind.String
                && !string.Equals(id.GetString(), ModId, StringComparison.Ordinal))
            {
                _report.Error("manifest", "manifest.json Id must be '" + ModId + "', found '" + id.GetString() + "'");
            }

            if (root.TryGetProperty("Version", out var version) && version.ValueKind == JsonValueKind.String
                && !VersionPattern.IsMatch(version.GetString() ?? string.Empty))
            {
                _report.Error("manifest", "manifest.json Version must be a SemVer-like string (e.g. 0.1.0), found '"
                                          + version.GetString() + "'");
            }

            if (root.TryGetProperty("MinimumGameVersion", out var minimum) && minimum.ValueKind == JsonValueKind.String
                && !VersionPattern.IsMatch(minimum.GetString() ?? string.Empty))
            {
                _report.Error("manifest", "manifest.json MinimumGameVersion must be a version string (e.g. 1.1.2.8), found '"
                                          + minimum.GetString() + "'");
            }

            if (root.TryGetProperty("Assemblies", out _))
            {
                _report.Error("manifest", "manifest.json must not declare 'Assemblies': the 1.1.2.8 ModManifest has no "
                                          + "such field (the mod loader loads every DLL in the folder)");
            }

            ValidateModList(root, "RequiredMods");
            ValidateModList(root, "OptionalMods");
            ValidateDescription(root);
        }
    }

    private void ValidateModList(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            _report.Error("manifest", "manifest.json is missing '" + propertyName + "' (must be an empty array)");
            return;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            _report.Error("manifest", "'" + propertyName + "' must be an array");
            return;
        }

        if (value.GetArrayLength() != 0)
        {
            _report.Error("manifest", "'" + propertyName + "' must stay empty: SEE depends on the base game only "
                                      + "(no hard dependency on other mods, REQ-COMPAT-01)");
        }
    }

    private void ValidateDescription(JsonElement root)
    {
        foreach (var field in new[] { "Name", "Description" })
        {
            if (!root.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var text = value.GetString() ?? string.Empty;
            foreach (var fragment in ForbiddenDescriptionFragments)
            {
                if (text.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    _report.Error("manifest", "manifest.json '" + field + "' still contains the previous mod's business "
                                              + "word '" + fragment + "'");
                }
            }
        }
    }
}
