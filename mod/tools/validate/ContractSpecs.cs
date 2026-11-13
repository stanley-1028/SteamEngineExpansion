namespace SEE.Tools.Validate;

/// <summary>
/// The frozen P0 blueprint contract (CONTRACT_SEE, decision D-11): the two custom
/// <c>ComponentSpec</c> classes and their serialized field lists.
///
/// The validator uses this list in two places: as the blueprint-validator fallback when
/// SEE.Gameplay has not been built yet, and as the expected shape in the assembly gate that
/// inspects the built assembly with Mono.Cecil. The built metadata stays authoritative; a drift
/// between src and this list is a validation error.
/// </summary>
internal static class ContractSpecs
{
    /// <summary>Blueprint JSON top-level key of the boiler spec (= its [SpecAlias]).</summary>
    public const string BoilerKey = "SEE.SEEBoilerSpec";

    /// <summary>Blueprint JSON top-level key of the maintenance spec (= its [SpecAlias]).</summary>
    public const string MaintenanceKey = "SEE.SEEMaintenanceSpec";

    /// <summary>Simple type name of the boiler spec in SEE.Gameplay.</summary>
    public const string BoilerTypeName = "SEEBoilerSpec";

    /// <summary>Simple type name of the maintenance spec in SEE.Gameplay.</summary>
    public const string MaintenanceTypeName = "SEEMaintenanceSpec";

    /// <summary>
    /// Id of the dedicated SEE bottom-bar building group ("Steam Engines") every boiler declares as
    /// <c>PlaceableBlockObjectSpec.ToolGroupId</c>. The group spec is the vanilla
    /// <c>BlockObjectToolGroupSpec</c> (no nested group exists in 1.1.2.8); order 72 seats it between
    /// the vanilla Power group (70) and Science (80).
    /// </summary>
    public const string ToolGroupId = "SteamEngines";

    /// <summary>Bottom-bar order of the SEE building group.</summary>
    public const int ToolGroupOrder = 72;

    /// <summary>Localization key of the group's bottom-bar label.</summary>
    public const string ToolGroupNameLocKey = "UI.SEE.ToolGroup.SteamEngines";

    /// <summary>Group sprite reference (mod-relative, no file extension).</summary>
    public const string ToolGroupIconPath = "ToolGroupIcons/SteamEngines";

    /// <summary>Data-relative path of the generated group blueprint.</summary>
    public const string ToolGroupBlueprintRelativePath =
        "BlockObjectToolGroups/BlockObjectToolGroup." + ToolGroupId + ".blueprint.json";

    /// <summary>Data-relative path of the generated group icon.</summary>
    public const string ToolGroupIconRelativePath = ToolGroupIconPath + ".png";

    /// <summary>Data-relative path of the icon's sprite sidecar.</summary>
    public const string ToolGroupIconMetaRelativePath = ToolGroupIconRelativePath + ".meta.json";

    public static readonly string[] BoilerFields =
    {
        "Tier", "Size", "Faction", "Horsepower", "FuelPerHour", "WaterPerHour", "FuelCapacityGoods",
        "WaterCapacityGoods", "FuelGoodIds", "FuelValues", "FuelScaling", "AutoMaintenanceDefault",
    };

    public static readonly string[] MaintenanceFields =
    {
        "Enabled", "IntervalDays", "LoadFloor", "LoadFactorRange", "TriggerThreshold", "DurationHours",
        "Materials",
    };

    /// <summary>Fallback field lists when SEE.Gameplay is not built (key = blueprint top-level key).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Fallback =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [BoilerKey] = BoilerFields,
            [MaintenanceKey] = MaintenanceFields,
        };
}
