namespace SEE.Tools.Validate;

/// <summary>
/// The authoritative localization key set of SEE: 64 keys (12 display names + 5 text keys +
/// 22 status keys + 24 panel/action keys + 1 bottom-bar tool-group label). The 54-key requirements
/// §15.5 baseline is preserved; the 9 P4B additions (numbered 50-58 below) are the P4B card's exact
/// texts; the tool-group key labels the dedicated "Steam Engines" building group.
///
/// The canonical list lives here (not in Core) on purpose: the validator must be able to catch
/// a Core-side key rename as a data problem instead of silently following it. A shipped CSV must
/// contain exactly these keys: no missing key, no extra leftover key.
/// </summary>
internal static class LocKeys
{
    /// <summary>The four size tokens in table order.</summary>
    public static readonly string[] Sizes = { "Small", "Medium", "Large" };

    /// <summary>The four tier tokens.</summary>
    public static readonly string[] Tiers = { "T1", "T2", "T3", "T4" };

    /// <summary>A: 12 display names, one per size x tier.</summary>
    public static readonly string[] DisplayNames = BuildDisplayNames();

    /// <summary>B: 5 description / flavor keys.</summary>
    public static readonly string[] Texts =
    {
        "Building.SEE.SteamBoiler.Description",
        "Building.SEE.SteamBoiler.Flavor.T1",
        "Building.SEE.SteamBoiler.Flavor.T2",
        "Building.SEE.SteamBoiler.Flavor.T3",
        "Building.SEE.SteamBoiler.Flavor.T4",
    };

    /// <summary>C: 22 status keys.</summary>
    public static readonly string[] Statuses =
    {
        "Status.SEE.Running",
        "Status.SEE.Idle",
        "Status.SEE.Paused",
        "Status.SEE.NoFuel",
        "Status.SEE.NoWater",
        "Status.SEE.NeedsMaintenance",
        "Status.SEE.Critical",
        "Status.SEE.Worn",
        "Status.SEE.MaintenanceDue",
        "Status.SEE.Maintaining",
        "Status.SEE.MissingMaintenanceMaterials",
        "Status.SEE.NoAccessForMaintenance",
        "Status.SEE.LowFuel",
        "Status.SEE.LowWater",
        "Status.SEE.PipeSupply",
        "Status.SEE.BeltSupply",
        "Status.SEE.BeltNoPower",
        "Status.SEE.PipeContaminated",
        "Status.SEE.WaterMode.Tank",
        "Status.SEE.WaterMode.Valve",
        "Status.SEE.WaterMode.Hauler",
        "Status.SEE.WaterMode.Auto",
    };

    /// <summary>D: 24 panel / action keys (15 requirements §15.5 baseline + 9 P4B additions).</summary>
    public static readonly string[] PanelAndActions =
    {
        "UI.SEE.Panel.Power",
        "UI.SEE.Panel.Network",
        "UI.SEE.Panel.Output",
        "UI.SEE.Panel.Fuel",
        "UI.SEE.Panel.Water",
        "UI.SEE.Panel.Condition",
        "UI.SEE.Panel.NextMaintenance",
        "UI.SEE.Panel.Materials",
        "UI.SEE.Panel.WaterSupplyMode",
        "UI.SEE.Action.Pause",
        "UI.SEE.Action.Resume",
        "UI.SEE.Action.MaintainNow",
        "UI.SEE.Action.AutoMaintenance",
        "UI.SEE.Error.MaterialsMissing",
        "UI.SEE.Error.BeltNoPower",
        "UI.SEE.Panel.WaterBeaverEquivalent",
        "UI.SEE.Panel.Efficiency",
        "UI.SEE.Panel.MaterialsDetail",
        "UI.SEE.Panel.MaterialEntry",
        "UI.SEE.Panel.NextMaintenanceDay",
        "UI.SEE.Hint.FixNoFuel",
        "UI.SEE.Hint.FixNoWater",
        "UI.SEE.Hint.FixNeedsMaintenance",
        "UI.SEE.WaterModeUnavailable",
    };

    /// <summary>Label of the dedicated SEE bottom-bar building group.</summary>
    public const string SteamEnginesToolGroup = "UI.SEE.ToolGroup.SteamEngines";

    /// <summary>E: 1 bottom-bar building-group label (the collapsible "Steam Engines" menu).</summary>
    public static readonly string[] ToolGroups =
    {
        SteamEnginesToolGroup,
    };

    /// <summary>All 64 keys (ordinal, de-duplicated).</summary>
    public static readonly IReadOnlyList<string> All = BuildAll();

    public static readonly int ExpectedCount = All.Count;

    /// <summary>
    /// Fragments the requirements forbid in a localization key (the previous mod's namespaces). The
    /// one intentional exception is <see cref="SteamEnginesToolGroup"/>: it is the SEE bottom-bar
    /// group label, lives in the UI.SEE.* namespace and can never overwrite a vanilla key (vanilla
    /// group labels are ToolGroups.*), so the localization check skips that key.
    /// </summary>
    public static readonly string[] ForbiddenFragments = { "SteamEngine", "Building.SteamEngine.", "SteamPlant" };

    private static string[] BuildDisplayNames()
    {
        var keys = new List<string>(12);
        foreach (var tier in Tiers)
        {
            foreach (var size in Sizes)
            {
                keys.Add("Building.SEE.SteamBoiler." + size + "." + tier + ".DisplayName");
            }
        }

        return keys.ToArray();
    }

    private static IReadOnlyList<string> BuildAll()
    {
        var all = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var key in DisplayNames)
        {
            all.Add(key);
        }

        foreach (var key in Texts)
        {
            all.Add(key);
        }

        foreach (var key in Statuses)
        {
            all.Add(key);
        }

        foreach (var key in PanelAndActions)
        {
            all.Add(key);
        }

        foreach (var key in ToolGroups)
        {
            all.Add(key);
        }

        return all.ToList();
    }
}
