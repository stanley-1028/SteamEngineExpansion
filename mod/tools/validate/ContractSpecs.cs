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
