using SEE.Core.Abstractions;

namespace SEE.Tools.BalanceSim.Data;

/// <summary>How one boiler row is compared with the vanilla power buildings (requirements §5.1).</summary>
internal sealed class BalanceReference
{
    /// <summary>Footprint class of the boiler row.</summary>
    public BoilerSize Size { get; init; }

    /// <summary>Technology tier of the boiler row.</summary>
    public BoilerTier Tier { get; init; }

    /// <summary>Human-readable counter proposal, for example <c>1x WaterWheel</c>.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Rated horsepower of the counter proposal (sum of the vanilla buildings).</summary>
    public double ReferenceHorsepower { get; init; }

    /// <summary>Display id, for example <c>Medium.T2</c>.</summary>
    public string Id => Size + "." + Tier;
}

/// <summary>
/// The frozen §5.1 A/B table plus the §5.2 criterion constants. These are requirement-derived
/// comparison anchors, not gameplay values: the shipped mod never reads them (balance values live
/// only in <c>data/engines.json</c>); this offline evidence tool owns them so the report can be
/// reproduced without a hand-kept spreadsheet.
/// </summary>
internal static class References
{
    /// <summary>Vanilla SteamEngine fuel rate per 100 hp per hour (requirements §5.4).</summary>
    public const double VanillaFuelPer100Hp = 0.025;

    /// <summary>Vanilla SteamEngine water rate per 100 hp per hour (requirements §5.4).</summary>
    public const double VanillaWaterPer100Hp = 0.075;

    /// <summary>Per-row 2-decimal rounding allowance of the §5.2 Criterion 2 measurement (8%).</summary>
    public const double RoundingTolerance = 0.08;

    /// <summary>Criterion 1: a row meeting at least 95% of the reference output passes.</summary>
    public const double Criterion1Floor = 0.95;

    /// <summary>Criterion 3: the 30-day average must reach 95% of the reference output.</summary>
    public const double Criterion3Floor = 0.95;

    /// <summary>Criterion 4: the scale floor is 40% of the Small.T1 metric.</summary>
    public const double ScaleFloorFactor = 0.40;

    /// <summary>T1 fuel cap factor relative to the T2 derived rate (requirements §5.2).</summary>
    public const double T1FuelCapFactor = 1.3;

    /// <summary>Average load ratio L used by the §5.5 conservative simulation (full load).</summary>
    public const float ConservativeLoad = 1.0f;

    /// <summary>BuildCostIndex weights: Log x1 + Plank x2 + MetalBlock x4 + Gear x6 (§4.3).</summary>
    public static readonly IReadOnlyDictionary<string, int> BuildCostWeights =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Log"] = 1,
            ["Plank"] = 2,
            ["MetalBlock"] = 4,
            ["Gear"] = 6,
        };

    /// <summary>Expected tier fuel rates per 100 hp (requirements §4.1 / §5.4).</summary>
    public static readonly decimal[] ExpectedFuelRatesPer100Hp = { 0.032m, 0.025m, 0.021m, 0.016m };

    /// <summary>Expected tier water rates per 100 hp (requirements §4.1 / §5.4).</summary>
    public static readonly decimal[] ExpectedWaterRatesPer100Hp = { 0.080m, 0.070m, 0.040m, 0.018m };

    /// <summary>Expected maintenance intervals in running days (requirements §5.5 / §20.2).</summary>
    public static readonly int[] ExpectedMaintenanceIntervals = { 6, 8, 10, 12 };

    /// <summary>Vanilla power buildings that act as comparison anchors (requirements §5.1 / §2.3).</summary>
    public static readonly (string Name, double Horsepower)[] VanillaPowerBuildings =
    {
        ("PowerWheel", 50),
        ("WaterWheel", 270),
        ("WindTurbine", 150),
        ("SteamEngine", 400),
        ("LargeWaterWheel", 540),
        ("LargeWindTurbine", 300),
    };

    /// <summary>The twelve §5.1 counter proposals, keyed by (size, tier) and in table order.</summary>
    public static readonly BalanceReference[] Rows =
    {
        Row(BoilerSize.Small, BoilerTier.T1, "1x PowerWheel", 50),
        Row(BoilerSize.Medium, BoilerTier.T1, "2x PowerWheel", 100),
        Row(BoilerSize.Large, BoilerTier.T1, "1x WaterWheel", 270),
        Row(BoilerSize.Small, BoilerTier.T2, "1x WindTurbine", 150),
        Row(BoilerSize.Medium, BoilerTier.T2, "1x SteamEngine", 400),
        Row(BoilerSize.Large, BoilerTier.T2, "1x LargeWaterWheel", 540),
        Row(BoilerSize.Small, BoilerTier.T3, "1x LargeWindTurbine", 300),
        Row(BoilerSize.Medium, BoilerTier.T3, "1x SteamEngine", 400),
        Row(BoilerSize.Large, BoilerTier.T3, "2x SteamEngine", 800),
        Row(BoilerSize.Small, BoilerTier.T4, "1x SteamEngine", 400),
        Row(BoilerSize.Medium, BoilerTier.T4, "2x SteamEngine", 800),
        Row(BoilerSize.Large, BoilerTier.T4, "3.75x SteamEngine", 1500),
    };

    /// <summary>Returns the frozen reference row for one boiler.</summary>
    public static BalanceReference For(BoilerSize size, BoilerTier tier)
    {
        foreach (BalanceReference reference in Rows)
        {
            if (reference.Size == size && reference.Tier == tier)
            {
                return reference;
            }
        }

        throw new BalanceSimException("no A/B reference for " + size + "/" + tier);
    }

    private static BalanceReference Row(BoilerSize size, BoilerTier tier, string label, double horsepower)
    {
        return new BalanceReference
        {
            Size = size,
            Tier = tier,
            Label = label,
            ReferenceHorsepower = horsepower,
        };
    }
}

/// <summary>Raised when the input data or the workspace layout is not what the tool expects.</summary>
internal sealed class BalanceSimException : Exception
{
    /// <summary>Creates the exception with a human-readable message.</summary>
    public BalanceSimException(string message)
        : base(message)
    {
    }
}
