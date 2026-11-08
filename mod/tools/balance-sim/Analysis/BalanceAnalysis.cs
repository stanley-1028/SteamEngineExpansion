using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Formulas;
using SEE.Tools.BalanceSim.Data;

namespace SEE.Tools.BalanceSim.Analysis;

/// <summary>Every derived number and verdict for one of the twelve boiler rows.</summary>
internal sealed class RowAnalysis
{
    /// <summary>Authoritative row from engines.json.</summary>
    public EngineRow Engine { get; init; } = new();

    /// <summary>P1 derived row (faction variants).</summary>
    public DerivedRow Derived { get; init; } = new();

    /// <summary>Frozen §5.1 counter proposal.</summary>
    public BalanceReference Reference { get; init; } = new();

    /// <summary>Footprint cells X*Y.</summary>
    public double Cells { get; init; }

    /// <summary>Horsepower per footprint cell (requirements §4.3).</summary>
    public double HpPerCell { get; init; }

    /// <summary>BuildCostIndex = Log*1 + Plank*2 + MetalBlock*4 + Gear*6 (§4.3).</summary>
    public double BuildCostIndex { get; init; }

    /// <summary>ScienceCost / rated horsepower (§4.3).</summary>
    public double SciencePerHp { get; init; }

    /// <summary>BuildCostIndex / rated horsepower (§4.3).</summary>
    public double BciPerHp { get; init; }

    /// <summary>Back-calculated fuel rate per 100 hp from the table row (§5.2 measurement).</summary>
    public double FuelPer100Hp { get; init; }

    /// <summary>Back-calculated water rate per 100 hp from the table row (§5.2 measurement).</summary>
    public double WaterPer100Hp { get; init; }

    /// <summary>Tier derived fuel rate per 100 hp (0.032/0.025/0.021/0.016).</summary>
    public double TierFuelRate { get; init; }

    /// <summary>Tier derived water rate per 100 hp (0.080/0.070/0.040/0.018).</summary>
    public double TierWaterRate { get; init; }

    /// <summary>Back-calculation deviation from the tier fuel rate, in percent (rounding only).</summary>
    public double FuelRoundingDeltaPct { get; init; }

    /// <summary>Back-calculation deviation from the tier water rate, in percent (rounding only).</summary>
    public double WaterRoundingDeltaPct { get; init; }

    /// <summary>Average condition coefficient over one §5.5 maintenance cycle at L = 1.0.</summary>
    public double AverageCoefficient { get; init; }

    /// <summary>30-day average effective output at L = 1.0 (horsepower).</summary>
    public double AverageOutput { get; init; }

    /// <summary>Reference horsepower x 0.95 (Criterion 3 threshold).</summary>
    public double Threshold95 { get; init; }

    /// <summary>Headroom over the Criterion 3 threshold, in percent.</summary>
    public double MarginPct { get; init; }

    /// <summary>Maintenance downtime fraction of the 30-day window (§5.5, REQ-BAL-10).</summary>
    public double DowntimeFraction { get; init; }

    /// <summary>Maintenance events per 30 game days (2 h each).</summary>
    public double MaintenanceEvents30Days { get; init; }

    /// <summary>Folktails fuel per 100 hp (blueprint actual values, §4.4 quantization).</summary>
    public double FtFuelPer100Hp { get; init; }

    /// <summary>Iron Teeth fuel per 100 hp (blueprint actual values, §4.4 quantization).</summary>
    public double ItFuelPer100Hp { get; init; }

    /// <summary>Iron Teeth relative fuel penalty per hp versus Folktails, in percent.</summary>
    public double FactionFuelDeltaPct { get; init; }

    /// <summary>Criterion 1 verdict (full load reaches the reference minus 5%).</summary>
    public bool Pass1 { get; init; }

    /// <summary>Criterion 2 fuel verdict for this row (vanilla cap plus rounding allowance).</summary>
    public bool Pass2Fuel { get; init; }

    /// <summary>Criterion 2 water verdict for this row.</summary>
    public bool Pass2Water { get; init; }

    /// <summary>Criterion 2 T1 fuel-cap verdict (T1 x 1.3 x T2 derived rate).</summary>
    public bool Pass2T1Cap { get; init; }

    /// <summary>Criterion 2 aggregate verdict for this row (T1 only checks the fuel cap).</summary>
    public bool Pass2 => Pass2Fuel && Pass2Water;

    /// <summary>Criterion 3 verdict (30-day average reaches reference x 0.95).</summary>
    public bool Pass3 { get; init; }

    /// <summary>Relative output versus the §5.1 counter proposal.</summary>
    public double RelativeOutput => Engine.RatedHorsepower / Reference.ReferenceHorsepower;

    /// <summary>Criterion 1 deviation versus the reference, in percent.</summary>
    public double ReferenceDeltaPct => (Engine.RatedHorsepower / Reference.ReferenceHorsepower - 1.0) * 100.0;

    /// <summary>Recomputed fuel stock from the shared Core formula (REQ-BLD-09).</summary>
    public int RecalculatedFuelStock { get; init; }

    /// <summary>Recomputed water tank from the shared Core formula (REQ-BLD-09).</summary>
    public int RecalculatedWaterTank { get; init; }
}

/// <summary>Scale-effect check of one tier (requirements §5.2 Criterion 4).</summary>
internal sealed class TierScaleCheck
{
    /// <summary>Technology tier under test.</summary>
    public BoilerTier Tier { get; init; }

    /// <summary>Research per hp for Small / Medium / Large.</summary>
    public double[] SciencePerHp { get; init; } = Array.Empty<double>();

    /// <summary>BuildCostIndex per hp for Small / Medium / Large.</summary>
    public double[] BciPerHp { get; init; } = Array.Empty<double>();

    /// <summary>Research per hp strictly decreases Small -> Medium -> Large.</summary>
    public bool ScienceMonotonic { get; init; }

    /// <summary>BuildCostIndex per hp strictly decreases Small -> Medium -> Large.</summary>
    public bool BciMonotonic { get; init; }

    /// <summary>All three rows stay at or above the 40% Small.T1 floor.</summary>
    public bool FloorPass { get; init; }

    /// <summary>Tier verdict.</summary>
    public bool Pass => ScienceMonotonic && BciMonotonic && FloorPass;
}

/// <summary>Complete analysis result: per-row metrics, four criterion verdicts and A/B evidence.</summary>
internal sealed class AnalysisResult
{
    /// <summary>The twelve rows in requirements §4.1 order.</summary>
    public IReadOnlyList<RowAnalysis> Rows { get; init; } = Array.Empty<RowAnalysis>();

    /// <summary>Scale checks for all four tiers.</summary>
    public IReadOnlyList<TierScaleCheck> ScaleChecks { get; init; } = Array.Empty<TierScaleCheck>();

    /// <summary>Research/hp floor = 40% of Small.T1 (§5.2 Criterion 4).</summary>
    public double ScienceFloor { get; init; }

    /// <summary>BuildCostIndex/hp floor = 40% of Small.T1 (§5.2 Criterion 4).</summary>
    public double BciFloor { get; init; }

    /// <summary>Vanilla rate per 100 hp used by Criterion 2 (0.025).</summary>
    public double VanillaFuelRate { get; init; }

    /// <summary>Vanilla water rate per 100 hp used by Criterion 2 (0.075).</summary>
    public double VanillaWaterRate { get; init; }

    /// <summary>T1 fuel cap = 1.3 x the T2 derived rate (0.0325).</summary>
    public double T1FuelCap { get; init; }

    /// <summary>Criterion 1 verdict for the whole family.</summary>
    public bool Criterion1 { get; init; }

    /// <summary>Criterion 2 verdict for the whole family.</summary>
    public bool Criterion2 { get; init; }

    /// <summary>Criterion 3 verdict for the whole family.</summary>
    public bool Criterion3 { get; init; }

    /// <summary>Criterion 4 verdict for the whole family.</summary>
    public bool Criterion4 { get; init; }

    /// <summary>Vanilla SteamEngine fuel efficiency advantage of Large.T4 (0.025 / 0.016).</summary>
    public double LargeT4FuelAdvantage { get; init; }

    /// <summary>Large.T4 block volume (4 x 4 x 4).</summary>
    public int LargeT4Blocks { get; init; }

    /// <summary>3.75 vanilla SteamEngines block volume, floored to whole blocks (101).</summary>
    public int LargeT4ReferenceBlocks { get; init; }

    /// <summary>Measured faction quantization range minimum (percent).</summary>
    public double FactionDeltaMinPct { get; init; }

    /// <summary>Measured faction quantization range maximum (percent).</summary>
    public double FactionDeltaMaxPct { get; init; }

    /// <summary>True when all four criteria pass.</summary>
    public bool AllPass => Criterion1 && Criterion2 && Criterion3 && Criterion4;

    /// <summary>Number of rows whose Criterion 3 margin is positive.</summary>
    public int RowsPassing => Rows.Count(row => row.Pass1 && row.Pass2 && row.Pass3);
}

/// <summary>
/// Computes the four §5.2 balance criteria from the loaded data, always through the shared
/// SEE.Core formulas (requirements §11.7: tools and gameplay must not compute two formula sets).
/// </summary>
internal static class BalanceAnalysis
{
    /// <summary>Runs every criterion over the loaded catalog.</summary>
    public static AnalysisResult Run(LoadedData data)
    {
        EngineCatalog catalog = data.Catalog;
        var rows = new List<RowAnalysis>(catalog.Engines.Count);
        double t1FuelCap = References.T1FuelCapFactor * (double)catalog.TierFuelPer100Hp[(int)BoilerTier.T2];
        foreach (EngineRow engine in catalog.Engines)
        {
            rows.Add(AnalyzeRow(engine, catalog, data.Derived, t1FuelCap));
        }

        double scienceFloor = References.ScaleFloorFactor * rows[0].SciencePerHp;
        double bciFloor = References.ScaleFloorFactor * rows[0].BciPerHp;
        var scaleChecks = new List<TierScaleCheck>(4);
        foreach (BoilerTier tier in new[] { BoilerTier.T1, BoilerTier.T2, BoilerTier.T3, BoilerTier.T4 })
        {
            scaleChecks.Add(CheckTier(tier, rows, scienceFloor, bciFloor));
        }

        bool criterion1 = rows.All(row => row.Pass1);
        bool criterion2 = rows.All(row => row.Pass2);
        bool criterion3 = rows.All(row => row.Pass3);
        bool criterion4 = scaleChecks.All(check => check.Pass);
        RowAnalysis largeT4 = rows[^1];
        var factionDeltas = rows.Select(row => row.FactionFuelDeltaPct).ToArray();
        return new AnalysisResult
        {
            Rows = rows,
            ScaleChecks = scaleChecks,
            ScienceFloor = scienceFloor,
            BciFloor = bciFloor,
            VanillaFuelRate = References.VanillaFuelPer100Hp,
            VanillaWaterRate = References.VanillaWaterPer100Hp,
            T1FuelCap = t1FuelCap,
            Criterion1 = criterion1,
            Criterion2 = criterion2,
            Criterion3 = criterion3,
            Criterion4 = criterion4,
            LargeT4FuelAdvantage = References.VanillaFuelPer100Hp / largeT4.TierFuelRate,
            LargeT4Blocks = largeT4.Engine.FootprintX * largeT4.Engine.FootprintY * largeT4.Engine.Height,
            LargeT4ReferenceBlocks = (int)Math.Floor(largeT4.Reference.ReferenceHorsepower / 400.0 * 27.0),
            FactionDeltaMinPct = factionDeltas.Min(),
            FactionDeltaMaxPct = factionDeltas.Max(),
        };
    }

    private static RowAnalysis AnalyzeRow(
        EngineRow engine, EngineCatalog catalog, DerivedCatalog derived, double t1FuelCap)
    {
        DerivedRow derivedRow = derived.Find(engine.Size, engine.Tier)
                               ?? throw new BalanceSimException("derived row missing for " + engine.Id);
        BalanceReference reference = References.For(engine.Size, engine.Tier);
        int tier = (int)engine.Tier;
        double tierFuelRate = (double)catalog.TierFuelPer100Hp[tier];
        double tierWaterRate = (double)catalog.TierWaterPer100Hp[tier];
        double fuelPer100 = engine.FuelPerHour / engine.RatedHorsepower * 100.0;
        double waterPer100 = engine.WaterPerHour / engine.RatedHorsepower * 100.0;
        double fuelDelta = (fuelPer100 / tierFuelRate - 1.0) * 100.0;
        double waterDelta = (waterPer100 / tierWaterRate - 1.0) * 100.0;
        double coefficient = BoilerFormulas.AverageConditionCoefficient(
            engine.MaintenanceIntervalDays, References.ConservativeLoad);
        double averageOutput = BoilerFormulas.ThirtyDayAverageOutput(
            engine.RatedHorsepower, engine.MaintenanceIntervalDays, References.ConservativeLoad);
        double threshold = reference.ReferenceHorsepower * References.Criterion3Floor;
        double downtime = BoilerFormulas.MaintenanceDowntimeFraction(
            engine.MaintenanceIntervalDays, References.ConservativeLoad);

        double ftFuelPer100 = derivedRow.Variants[BoilerFaction.Folktails].FuelPerHour
                              / derivedRow.Variants[BoilerFaction.Folktails].Horsepower * 100.0;
        double itFuelPer100 = derivedRow.Variants[BoilerFaction.IronTeeth].FuelPerHour
                              / derivedRow.Variants[BoilerFaction.IronTeeth].Horsepower * 100.0;

        bool roundingFuel = Math.Abs(fuelDelta) <= References.RoundingTolerance * 100.0 + 1e-9;
        bool roundingWater = Math.Abs(waterDelta) <= References.RoundingTolerance * 100.0 + 1e-9;
        bool tierFuelCap = engine.Tier == BoilerTier.T1
            ? tierFuelRate <= t1FuelCap + 1e-12
            : tierFuelRate <= References.VanillaFuelPer100Hp + 1e-12;
        bool tierWaterCap = engine.Tier == BoilerTier.T1
            || tierWaterRate <= References.VanillaWaterPer100Hp + 1e-12;
        bool rowFuelCap = engine.Tier == BoilerTier.T1
            || fuelPer100 <= References.VanillaFuelPer100Hp * (1.0 + References.RoundingTolerance) + 1e-12;
        bool rowWaterCap = engine.Tier == BoilerTier.T1
            || waterPer100 <= References.VanillaWaterPer100Hp * (1.0 + References.RoundingTolerance) + 1e-12;

        return new RowAnalysis
        {
            Engine = engine,
            Derived = derivedRow,
            Reference = reference,
            Cells = engine.FootprintX * engine.FootprintY,
            HpPerCell = engine.RatedHorsepower / (double)(engine.FootprintX * engine.FootprintY),
            BuildCostIndex = BuildCostIndex(engine.BuildCost),
            SciencePerHp = engine.ScienceCost / (double)engine.RatedHorsepower,
            BciPerHp = BuildCostIndex(engine.BuildCost) / engine.RatedHorsepower,
            FuelPer100Hp = fuelPer100,
            WaterPer100Hp = waterPer100,
            TierFuelRate = tierFuelRate,
            TierWaterRate = tierWaterRate,
            FuelRoundingDeltaPct = fuelDelta,
            WaterRoundingDeltaPct = waterDelta,
            AverageCoefficient = coefficient,
            AverageOutput = averageOutput,
            Threshold95 = threshold,
            MarginPct = (averageOutput / threshold - 1.0) * 100.0,
            DowntimeFraction = downtime,
            MaintenanceEvents30Days = downtime * SEEConst.ThirtyDayHours / SEEConst.MaintenanceDurationHours,
            FtFuelPer100Hp = ftFuelPer100,
            ItFuelPer100Hp = itFuelPer100,
            FactionFuelDeltaPct = (itFuelPer100 / ftFuelPer100 - 1.0) * 100.0,
            Pass1 = engine.RatedHorsepower >= reference.ReferenceHorsepower * References.Criterion1Floor - 1e-9,
            Pass2Fuel = tierFuelCap && rowFuelCap && roundingFuel,
            Pass2Water = tierWaterCap && rowWaterCap && roundingWater,
            Pass2T1Cap = engine.Tier != BoilerTier.T1 || tierFuelCap,
            Pass3 = averageOutput >= threshold - 1e-9,
            RecalculatedFuelStock = BoilerFormulas.FuelStockGoods(
                (float)engine.FuelPerHour, (float)catalog.FullInventoryWorkHours),
            RecalculatedWaterTank = BoilerFormulas.WaterTankGoods(
                (float)engine.WaterPerHour, (float)catalog.WaterTankHours),
        };
    }

    private static TierScaleCheck CheckTier(
        BoilerTier tier, IReadOnlyList<RowAnalysis> rows, double scienceFloor, double bciFloor)
    {
        var tierRows = rows.Where(row => row.Engine.Tier == tier).OrderBy(row => row.Engine.Size).ToArray();
        double[] science = tierRows.Select(row => row.SciencePerHp).ToArray();
        double[] bci = tierRows.Select(row => row.BciPerHp).ToArray();
        bool floor = tierRows.All(row =>
            row.SciencePerHp >= scienceFloor - 1e-12 && row.BciPerHp >= bciFloor - 1e-12);
        return new TierScaleCheck
        {
            Tier = tier,
            SciencePerHp = science,
            BciPerHp = bci,
            ScienceMonotonic = IsStrictlyDecreasing(science),
            BciMonotonic = IsStrictlyDecreasing(bci),
            FloorPass = floor,
        };
    }

    private static bool IsStrictlyDecreasing(IReadOnlyList<double> values)
    {
        for (int index = 1; index < values.Count; index++)
        {
            if (!(values[index] < values[index - 1]))
            {
                return false;
            }
        }

        return values.Count > 1;
    }

    /// <summary>BuildCostIndex of one row (requirements §4.3 weights).</summary>
    private static double BuildCostIndex(IReadOnlyList<GoodAmount> cost)
    {
        double index = 0.0;
        foreach (GoodAmount item in cost)
        {
            if (!References.BuildCostWeights.TryGetValue(item.Good, out int weight))
            {
                throw new BalanceSimException("unknown build cost good '" + item.Good + "'");
            }

            index += weight * item.Amount;
        }

        return index;
    }
}
