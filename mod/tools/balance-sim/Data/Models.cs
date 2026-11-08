using SEE.Core.Abstractions;

namespace SEE.Tools.BalanceSim.Data;

/// <summary>One good amount of a build or maintenance cost (requirements §4.1).</summary>
internal sealed class GoodAmount
{
    /// <summary>Good id (Log / Plank / MetalBlock / Gear).</summary>
    public string Good { get; init; } = string.Empty;

    /// <summary>Whole item count.</summary>
    public int Amount { get; init; }
}

/// <summary>Faction multipliers from <c>engines.json</c> (requirements §4.4).</summary>
internal sealed class FactionRates
{
    /// <summary>Faction name as written in the data file.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Output multiplier (FT 1.00 / IT 1.05).</summary>
    public double OutputMultiplier { get; init; }

    /// <summary>Fuel multiplier (FT 0.90 / IT 1.00).</summary>
    public double FuelMultiplier { get; init; }
}

/// <summary>One authoritative row of <c>data/engines.json</c> (requirements §4.1).</summary>
internal sealed class EngineRow
{
    /// <summary>Footprint class (Small / Medium / Large).</summary>
    public BoilerSize Size { get; init; }

    /// <summary>Technology tier (T1..T4).</summary>
    public BoilerTier Tier { get; init; }

    /// <summary>Footprint X (blocks).</summary>
    public int FootprintX { get; init; }

    /// <summary>Footprint Y (blocks).</summary>
    public int FootprintY { get; init; }

    /// <summary>Height Z (blocks).</summary>
    public int Height { get; init; }

    /// <summary>Rated horsepower (base value before the faction multiplier).</summary>
    public int RatedHorsepower { get; init; }

    /// <summary>ScienceCost of the blueprint.</summary>
    public int ScienceCost { get; init; }

    /// <summary>Build cost goods.</summary>
    public IReadOnlyList<GoodAmount> BuildCost { get; init; } = Array.Empty<GoodAmount>();

    /// <summary>Fuel rate in Log/h, stored as decimal to avoid binary rounding drift.</summary>
    public decimal FuelPerHourDecimal { get; init; }

    /// <summary>Water rate in Water/h, stored as decimal to avoid binary rounding drift.</summary>
    public decimal WaterPerHourDecimal { get; init; }

    /// <summary>Declared fuel inventory goods (authoritative table value).</summary>
    public int FuelStock { get; init; }

    /// <summary>Declared water tank goods (authoritative table value).</summary>
    public int WaterTankGoods { get; init; }

    /// <summary>Full-load working hours of the fuel inventory (25).</summary>
    public int FullInventoryWorkHours { get; init; }

    /// <summary>Maintenance interval in running days (6/8/10/12).</summary>
    public int MaintenanceIntervalDays { get; init; }

    /// <summary>Maintenance material goods.</summary>
    public IReadOnlyList<GoodAmount> MaintenanceMaterials { get; init; } = Array.Empty<GoodAmount>();

    /// <summary>Fuel rate as a double for the shared SEE.Core formulas.</summary>
    public double FuelPerHour => (double)FuelPerHourDecimal;

    /// <summary>Water rate as a double for the shared SEE.Core formulas.</summary>
    public double WaterPerHour => (double)WaterPerHourDecimal;

    /// <summary>Stable display id, for example <c>Medium.T2</c>.</summary>
    public string Id => Size + "." + Tier;
}

/// <summary>Parsed <c>data/engines.json</c>; the single source of the 12 authoritative rows.</summary>
internal sealed class EngineCatalog
{
    /// <summary>Build-time data version of the file.</summary>
    public int DataVersion { get; init; }

    /// <summary>Fuel inventory capacity in full-load working hours (25).</summary>
    public double FullInventoryWorkHours { get; init; }

    /// <summary>Water tank capacity in full-load working hours (200).</summary>
    public double WaterTankHours { get; init; }

    /// <summary>Fuel rate per 100 hp per hour, indexed by <see cref="BoilerTier"/>.</summary>
    public decimal[] TierFuelPer100Hp { get; init; } = Array.Empty<decimal>();

    /// <summary>Water rate per 100 hp per hour, indexed by <see cref="BoilerTier"/>.</summary>
    public decimal[] TierWaterPer100Hp { get; init; } = Array.Empty<decimal>();

    /// <summary>Folktails multipliers.</summary>
    public FactionRates Folktails { get; init; } = new();

    /// <summary>Iron Teeth multipliers.</summary>
    public FactionRates IronTeeth { get; init; } = new();

    /// <summary>The 12 engine rows in file order.</summary>
    public IReadOnlyList<EngineRow> Engines { get; init; } = Array.Empty<EngineRow>();

    /// <summary>Finds a row by size and tier (null when absent).</summary>
    public EngineRow? Find(BoilerSize size, BoilerTier tier)
    {
        foreach (EngineRow row in Engines)
        {
            if (row.Size == size && row.Tier == tier)
            {
                return row;
            }
        }

        return null;
    }
}

/// <summary>One P1 gate result copied from <c>engines.derived.json</c>.</summary>
internal sealed class DerivedGate
{
    /// <summary>Gate number 1..5.</summary>
    public int Id { get; init; }

    /// <summary>Gate name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gate verdict.</summary>
    public bool Pass { get; init; }

    /// <summary>Gate detail text.</summary>
    public string Detail { get; init; } = string.Empty;
}

/// <summary>One faction-specific blueprint variant from <c>engines.derived.json</c>.</summary>
internal sealed class FactionVariant
{
    /// <summary>Faction of the variant.</summary>
    public BoilerFaction Faction { get; init; }

    /// <summary>Derived whole horsepower (base x faction output, rounded up).</summary>
    public int Horsepower { get; init; }

    /// <summary>Derived fuel rate in Log/h (2 decimals).</summary>
    public decimal FuelPerHourDecimal { get; init; }

    /// <summary>Water rate in Water/h (unchanged from the base row).</summary>
    public decimal WaterPerHourDecimal { get; init; }

    /// <summary>Declared fuel inventory goods (base table value, D-6).</summary>
    public int FuelStock { get; init; }

    /// <summary>Declared water tank goods (base table value).</summary>
    public int WaterTankGoods { get; init; }

    /// <summary>Frozen TemplateName of the variant.</summary>
    public string TemplateName { get; init; } = string.Empty;

    /// <summary>Derived fuel rate as a double.</summary>
    public double FuelPerHour => (double)FuelPerHourDecimal;
}

/// <summary>One derived row (12 rows; each carries both faction variants).</summary>
internal sealed class DerivedRow
{
    /// <summary>Footprint class.</summary>
    public BoilerSize Size { get; init; }

    /// <summary>Technology tier.</summary>
    public BoilerTier Tier { get; init; }

    /// <summary>Base rated horsepower (must equal engines.json).</summary>
    public int RatedHorsepower { get; init; }

    /// <summary>Faction variants keyed by faction.</summary>
    public IReadOnlyDictionary<BoilerFaction, FactionVariant> Variants { get; init; } =
        new Dictionary<BoilerFaction, FactionVariant>();

    /// <summary>Stable display id, for example <c>Medium.T2</c>.</summary>
    public string Id => Size + "." + Tier;
}

/// <summary>Parsed <c>data/engines.derived.json</c> (P1 product, generated; never edited by hand).</summary>
internal sealed class DerivedCatalog
{
    /// <summary>SHA-256 of the engines.json the file was generated from.</summary>
    public string SourceSha256 { get; init; } = string.Empty;

    /// <summary>Declared blueprint count (24 = 12 x 2 factions).</summary>
    public int BlueprintCount { get; init; }

    /// <summary>Declared authoritative field count (12).</summary>
    public int FieldsPerFile { get; init; }

    /// <summary>The 12 derived rows in file order.</summary>
    public IReadOnlyList<DerivedRow> Rows { get; init; } = Array.Empty<DerivedRow>();

    /// <summary>The five P1 gates.</summary>
    public IReadOnlyList<DerivedGate> Gates { get; init; } = Array.Empty<DerivedGate>();

    /// <summary>True when all five P1 gates report PASS.</summary>
    public bool AllGatesPass
    {
        get
        {
            if (Gates.Count != 5)
            {
                return false;
            }

            foreach (DerivedGate gate in Gates)
            {
                if (!gate.Pass)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>Finds a derived row by size and tier (null when absent).</summary>
    public DerivedRow? Find(BoilerSize size, BoilerTier tier)
    {
        foreach (DerivedRow row in Rows)
        {
            if (row.Size == size && row.Tier == tier)
            {
                return row;
            }
        }

        return null;
    }
}

/// <summary>Everything the analyses need, loaded and cross-checked once at start-up.</summary>
internal sealed class LoadedData
{
    /// <summary>Authoritative 12-row catalog.</summary>
    public EngineCatalog Catalog { get; init; } = new();

    /// <summary>P1 derived table.</summary>
    public DerivedCatalog Derived { get; init; } = new();

    /// <summary>SHA-256 of engines.json (equal to the derived source hash).</summary>
    public string EnginesSha256 { get; init; } = string.Empty;

    /// <summary>Number of data cross-checks that passed.</summary>
    public int CrossCheckCount { get; init; }
}
