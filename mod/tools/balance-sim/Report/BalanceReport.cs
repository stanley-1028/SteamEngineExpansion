using SEE.Core.Abstractions;
using SEE.Tools.BalanceSim.Analysis;
using SEE.Tools.BalanceSim.Data;

namespace SEE.Tools.BalanceSim.Report;

/// <summary>
/// Prints the balance evidence sections (requirements §5): data summary, derived metrics, the four
/// §5.2 criteria, the §5.1 A/B table and the §4.4 faction quantization deviation. Every number
/// printed here is reproduced in docs/BALANCE_REPORT.md.
/// </summary>
internal static class BalanceReport
{
    /// <summary>Prints the loaded-data summary and cross-check gate.</summary>
    public static void PrintDataSummary(LoadedData data)
    {
        Fmt.Section("0. DATA SOURCE AND CROSS-CHECK");
        Fmt.Info("engines.json sha256", data.EnginesSha256);
        Fmt.Info("rows", data.Catalog.Engines.Count + " (3 sizes x 4 tiers)");
        Fmt.Info("derived variants", data.Derived.Rows.Count + " rows / "
            + (data.Derived.Rows.Count * 2) + " faction variants / blueprintCount "
            + data.Derived.BlueprintCount);
        Fmt.Info("P1 gates", (data.Derived.AllGatesPass ? "PASS 5/5" : "FAIL")
            + " (water tank autonomy, dedicated water tank, efficiency, capacities, forbidden spec scan)");
        Fmt.Info("formula source", "SEE.Core BoilerFormulas + ConditionCurve + BoilerStateMachine");
        Fmt.Info("workload", "30-day average, L=1.0, maintenance 2 h, intervals 6/8/10/12 days");
        Console.WriteLine("  cross-checks passed : " + data.CrossCheckCount
                          + " (see CatalogLoader; any mismatch aborts the run)");
    }

    /// <summary>Prints the §4.1/§4.3 derived metrics table plus the REQ-BLD-09 capacity check.</summary>
    public static void PrintDerivedTable(AnalysisResult analysis)
    {
        Fmt.Section("1. DERIVED METRICS AND CAPACITY CHECK (REQ-BLD-09)");
        Console.WriteLine(Fmt.Line(
            ("row", 10), ("hp", 5), ("cells", 5), ("hp/cell", 8), ("fuel/h", 7), ("water/h", 7),
            ("fuel/100hp", 9), ("water/100hp", 9), ("science/hp", 9), ("BCI", 5), ("BCI/hp", 8),
            ("fuelStock", 9), ("waterTank", 9), ("cap", 6)));
        foreach (RowAnalysis row in analysis.Rows)
        {
            bool capacity = row.RecalculatedFuelStock == row.Engine.FuelStock
                            && row.RecalculatedWaterTank == row.Engine.WaterTankGoods;
            Console.WriteLine(Fmt.Line(
                (row.Engine.Id, 10),
                (row.Engine.RatedHorsepower.ToString(), 5),
                (row.Cells.ToString(), 5),
                (Fmt.N(row.HpPerCell, 1), 8),
                (Fmt.N(row.Engine.FuelPerHour, 3), 7),
                (Fmt.N(row.Engine.WaterPerHour, 3), 7),
                (Fmt.N(row.FuelPer100Hp, 4), 9),
                (Fmt.N(row.WaterPer100Hp, 4), 9),
                (Fmt.N(row.SciencePerHp, 4), 9),
                (Fmt.N(row.BuildCostIndex, 0), 5),
                (Fmt.N(row.BciPerHp, 4), 8),
                (row.Engine.FuelStock + "/" + row.RecalculatedFuelStock, 9),
                (row.Engine.WaterTankGoods + "/" + row.RecalculatedWaterTank, 9),
                (Fmt.Verdict(capacity), 6)));
        }

        Console.WriteLine("  note: fuelStock = ceil(fuel/h x 25), waterTank = round(water/h x 200), both via SEE.Core.");
        Console.WriteLine("  REQ-BLD-09 spot value: Large.T1 waterTankGoods = 28 (round(0.14 x 200) = 28).");
    }

    /// <summary>Prints Criterion 1 (full-load output vs the §5.1 counter proposal).</summary>
    public static void PrintCriterion1(AnalysisResult analysis)
    {
        Fmt.Section("2. CRITERION 1: FULL-LOAD OUTPUT >= REFERENCE (tolerance -5%)");
        Console.WriteLine(Fmt.Line(
            ("row", 10), ("rated", 6), ("reference", 20), ("ref hp", 7), ("delta%", 8),
            ("floor95", 8), ("verdict", 8)));
        foreach (RowAnalysis row in analysis.Rows)
        {
            Console.WriteLine(Fmt.Line(
                (row.Engine.Id, 10),
                (row.Engine.RatedHorsepower.ToString(), 6),
                (row.Reference.Label, 20),
                (Fmt.N(row.Reference.ReferenceHorsepower, 0), 7),
                (Fmt.Pct(row.ReferenceDeltaPct), 8),
                (Fmt.N(row.Reference.ReferenceHorsepower * References.Criterion1Floor, 1), 8),
                (Fmt.Verdict(row.Pass1), 8)));
        }

        PrintCriterionLine(analysis, 1);
    }

    /// <summary>Prints Criterion 2 (fuel/water per 100 hp plus the T1 fuel cap).</summary>
    public static void PrintCriterion2(AnalysisResult analysis)
    {
        Fmt.Section("3. CRITERION 2: FUEL <= 0.025 / WATER <= 0.075 PER 100 HP (+-8% rounding)");
        Console.WriteLine("  fuel (Log/h per 100 hp):");
        Console.WriteLine(Fmt.Line(
            ("row", 10), ("tier", 4), ("hp", 5), ("fuel/h", 7), ("per100hp", 9), ("tierRate", 8),
            ("delta%", 8), ("rounding", 9), ("rowCap", 7), ("verdict", 8)));
        foreach (RowAnalysis row in analysis.Rows)
        {
            bool rounding = Math.Abs(row.FuelRoundingDeltaPct) <= References.RoundingTolerance * 100.0 + 1e-9;
            bool rowCap = row.Engine.Tier == BoilerTier.T1
                          || row.FuelPer100Hp
                          <= References.VanillaFuelPer100Hp * (1.0 + References.RoundingTolerance) + 1e-12;
            Console.WriteLine(Fmt.Line(
                (row.Engine.Id, 10),
                (row.Engine.Tier.ToString(), 4),
                (row.Engine.RatedHorsepower.ToString(), 5),
                (Fmt.N(row.Engine.FuelPerHour, 3), 7),
                (Fmt.N(row.FuelPer100Hp, 4), 9),
                (Fmt.N(row.TierFuelRate, 3), 8),
                (Fmt.Pct(row.FuelRoundingDeltaPct), 8),
                (rounding ? "YES" : "NO", 9),
                (row.Engine.Tier == BoilerTier.T1 ? "n/a" : (rowCap ? "OK" : "OVER"), 7),
                (Fmt.Verdict(row.Pass2Fuel), 8)));
        }

        Console.WriteLine("  water (Water/h per 100 hp):");
        Console.WriteLine(Fmt.Line(
            ("row", 10), ("tier", 4), ("hp", 5), ("water/h", 7), ("per100hp", 9), ("tierRate", 8),
            ("delta%", 8), ("rounding", 9), ("rowCap", 7), ("verdict", 8)));
        foreach (RowAnalysis row in analysis.Rows)
        {
            bool rounding = Math.Abs(row.WaterRoundingDeltaPct) <= References.RoundingTolerance * 100.0 + 1e-9;
            bool rowCap = row.Engine.Tier == BoilerTier.T1
                          || row.WaterPer100Hp
                          <= References.VanillaWaterPer100Hp * (1.0 + References.RoundingTolerance) + 1e-12;
            Console.WriteLine(Fmt.Line(
                (row.Engine.Id, 10),
                (row.Engine.Tier.ToString(), 4),
                (row.Engine.RatedHorsepower.ToString(), 5),
                (Fmt.N(row.Engine.WaterPerHour, 3), 7),
                (Fmt.N(row.WaterPer100Hp, 4), 9),
                (Fmt.N(row.TierWaterRate, 3), 8),
                (Fmt.Pct(row.WaterRoundingDeltaPct), 8),
                (rounding ? "YES" : "NO", 9),
                (row.Engine.Tier == BoilerTier.T1 ? "n/a" : (rowCap ? "OK" : "OVER"), 7),
                (Fmt.Verdict(row.Pass2Water), 8)));
        }

        double t1Rate = analysis.Rows[0].TierFuelRate;
        Console.WriteLine("  T1 fuel cap: derived " + Fmt.N(t1Rate, 3) + " <= 1.3 x T2 "
                          + Fmt.N((double)analysis.T1FuelCap / References.T1FuelCapFactor, 3)
                          + " = " + Fmt.N(analysis.T1FuelCap, 4)
                          + "  margin " + Fmt.Pct((analysis.T1FuelCap / t1Rate - 1.0) * 100.0)
                          + "  -> " + Fmt.Verdict(analysis.Rows[0].Pass2T1Cap));
        RowAnalysis smallT2 = analysis.Rows.First(row => row.Engine.Id == "Small.T2");
        Console.WriteLine("  sensitivity: Small.T2 fuel 0.07 back-calculates to "
                          + Fmt.N(smallT2.FuelPer100Hp, 4) + " ("
                          + Fmt.Pct(smallT2.FuelRoundingDeltaPct)
                          + " vs the 0.025 derived rate); the +-8% rounding allowance is the §5.2 upper bound.");
        PrintCriterionLine(analysis, 2);
    }

    /// <summary>Prints Criterion 3 (30-day average effective output).</summary>
    public static void PrintCriterion3(AnalysisResult analysis)
    {
        Fmt.Section("4. CRITERION 3: 30-DAY AVERAGE >= REFERENCE x 0.95 (L=1.0)");
        Console.WriteLine(Fmt.Line(
            ("row", 10), ("interval", 8), ("avgCoeff", 9), ("avgOutput", 10), ("reference", 19),
            ("threshold", 9), ("margin%", 9), ("downtime", 8), ("maint/30d", 9), ("verdict", 8)));
        foreach (RowAnalysis row in analysis.Rows)
        {
            Console.WriteLine(Fmt.Line(
                (row.Engine.Id, 10),
                (row.Engine.MaintenanceIntervalDays + " d", 8),
                (Fmt.N(row.AverageCoefficient, 4), 9),
                (Fmt.N(row.AverageOutput, 1), 10),
                (row.Reference.Label, 19),
                (Fmt.N(row.Threshold95, 1), 9),
                (Fmt.Pct(row.MarginPct), 9),
                (Fmt.Pct(row.DowntimeFraction * 100.0), 8),
                (Fmt.N(row.MaintenanceEvents30Days, 2), 9),
                (Fmt.Verdict(row.Pass3), 8)));
        }

        Console.WriteLine("  derivation (requirements §5.5, SEE.Core BoilerFormulas.AverageConditionCoefficient):");
        Console.WriteLine("    100->61 band = 39 x 24 x I / (50 x f), 61->50 band = 11 x 24 x I / (50 x f)");
        Console.WriteLine("    cycle = both bands + 2 h maintenance; avg = (band1 x 1.00 + band2 x 0.93) / cycle");
        Console.WriteLine("    f = 0.4 + 0.6 x L = 1.0 at L = 1.0; 30-day average = rated x avg x L");
        RowAnalysis largeT4 = analysis.Rows[^1];
        Console.WriteLine("  sensitive row: Large.T4 margin " + Fmt.Pct(largeT4.MarginPct)
                          + " (average " + Fmt.N(largeT4.AverageOutput, 2) + " vs threshold "
                          + Fmt.N(largeT4.Threshold95, 1)
                          + "); faster wear or longer downtime that pushes the coefficient below 0.95 would"
                          + " FAIL it, and a shorter maintenance interval erodes the margin.");
        PrintCriterionLine(analysis, 3);
    }

    /// <summary>Prints Criterion 4 (scale effect: research/hp and BCI/hp decrease with size).</summary>
    public static void PrintCriterion4(AnalysisResult analysis)
    {
        Fmt.Section("5. CRITERION 4: SCALE EFFECT (research/hp and BCI/hp decrease with size, >=40% Small.T1)");
        Console.WriteLine(Fmt.Line(("tier", 6), ("science/hp Small->Medium->Large", 34),
            ("mono", 6), ("BCI/hp Small->Medium->Large", 30), ("mono", 6), ("floor", 6), ("verdict", 8)));
        foreach (TierScaleCheck check in analysis.ScaleChecks)
        {
            Console.WriteLine(Fmt.Line(
                (check.Tier.ToString(), 6),
                (Fmt.N(check.SciencePerHp[0], 4) + " -> " + Fmt.N(check.SciencePerHp[1], 4)
                 + " -> " + Fmt.N(check.SciencePerHp[2], 4), 34),
                (check.ScienceMonotonic ? "YES" : "NO", 6),
                (Fmt.N(check.BciPerHp[0], 4) + " -> " + Fmt.N(check.BciPerHp[1], 4)
                 + " -> " + Fmt.N(check.BciPerHp[2], 4), 30),
                (check.BciMonotonic ? "YES" : "NO", 6),
                (check.FloorPass ? "YES" : "NO", 6),
                (Fmt.Verdict(check.Pass), 8)));
        }

        RowAnalysis minScience = analysis.Rows.OrderBy(row => row.SciencePerHp / analysis.ScienceFloor).First();
        RowAnalysis minBci = analysis.Rows.OrderBy(row => row.BciPerHp / analysis.BciFloor).First();
        Console.WriteLine("  floor = 40% of Small.T1: research/hp >= " + Fmt.N(analysis.ScienceFloor, 4)
                          + ", BCI/hp >= " + Fmt.N(analysis.BciFloor, 4));
        Console.WriteLine("  minimum headroom: research/hp x" + Fmt.N(minScience.SciencePerHp / analysis.ScienceFloor, 2)
                          + " (" + minScience.Engine.Id + "), BCI/hp x"
                          + Fmt.N(minBci.BciPerHp / analysis.BciFloor, 2) + " (" + minBci.Engine.Id + ")");
        PrintCriterionLine(analysis, 4);
    }

    /// <summary>Prints the §5.1 A/B table, the vanilla anchors and the Large.T4 positioning evidence.</summary>
    public static void PrintAbComparison(AnalysisResult analysis)
    {
        Fmt.Section("6. A/B COMPARISON (requirements §5.1)");
        Console.WriteLine("  vanilla power buildings (rated horsepower, §5.1 anchors):");
        foreach ((string name, double horsepower) in References.VanillaPowerBuildings)
        {
            Console.WriteLine("    " + Fmt.L(name, 18) + Fmt.R(horsepower, 6, 0));
        }

        Console.WriteLine();
        Console.WriteLine(Fmt.Line(
            ("row", 10), ("rated", 6), ("counter proposal", 20), ("ref hp", 7), ("relative", 9),
            ("delta%", 8), ("criterion 1", 12)));
        foreach (RowAnalysis row in analysis.Rows)
        {
            Console.WriteLine(Fmt.Line(
                (row.Engine.Id, 10),
                (row.Engine.RatedHorsepower.ToString(), 6),
                (row.Reference.Label, 20),
                (Fmt.N(row.Reference.ReferenceHorsepower, 0), 7),
                (Fmt.N(row.RelativeOutput, 2) + "x", 9),
                (Fmt.Pct(row.ReferenceDeltaPct), 8),
                (Fmt.Verdict(row.Pass1), 12)));
        }

        RowAnalysis largeT4 = analysis.Rows[^1];
        Console.WriteLine("  Large.T4 positioning:");
        Console.WriteLine("    relative output        : " + Fmt.N(largeT4.RelativeOutput, 2)
                          + "x (1500 hp / 3.75 x SteamEngine 1500 hp)");
        Console.WriteLine("    fuel advantage         : " + Fmt.N(analysis.LargeT4FuelAdvantage, 2)
                          + "x (vanilla 0.025 / Large.T4 " + Fmt.N(largeT4.TierFuelRate, 3)
                          + " per 100 hp; REQ-BAL-13)");
        Console.WriteLine("    block volume           : " + analysis.LargeT4Blocks
                          + " vs " + analysis.LargeT4ReferenceBlocks
                          + " (3.75 x 27 blocks, floored; REQ-BAL-13)");
        Console.WriteLine("    maintenance margin     : " + Fmt.Pct(largeT4.MarginPct)
                          + " (30-day average vs reference x 0.95)");
    }

    /// <summary>Prints the §4.4 faction fuel quantization deviation (2-decimal blueprint values).</summary>
    public static void PrintFactionQuantization(AnalysisResult analysis)
    {
        Fmt.Section("7. FACTION QUANTIZATION DEVIATION (requirements §4.4)");
        Console.WriteLine(Fmt.Line(
            ("row", 10), ("FT hp", 6), ("FT fuel", 8), ("FT/100hp", 9), ("IT hp", 6),
            ("IT fuel", 8), ("IT/100hp", 9), ("IT vs FT", 9)));
        foreach (RowAnalysis row in analysis.Rows)
        {
            FactionVariant ft = row.Derived.Variants[BoilerFaction.Folktails];
            FactionVariant it = row.Derived.Variants[BoilerFaction.IronTeeth];
            Console.WriteLine(Fmt.Line(
                (row.Engine.Id, 10),
                (ft.Horsepower.ToString(), 6),
                (Fmt.N((double)ft.FuelPerHourDecimal, 2), 8),
                (Fmt.N(row.FtFuelPer100Hp, 4), 9),
                (it.Horsepower.ToString(), 6),
                (Fmt.N((double)it.FuelPerHourDecimal, 2), 8),
                (Fmt.N(row.ItFuelPer100Hp, 4), 9),
                (Fmt.Pct(row.FactionFuelDeltaPct), 9)));
        }

        RowAnalysis documentedMin = analysis.Rows.First(row => row.Engine.Id == "Medium.T4");
        RowAnalysis documentedMax = analysis.Rows.First(row => row.Engine.Id == "Medium.T1");
        RowAnalysis widest = analysis.Rows.OrderByDescending(row => row.FactionFuelDeltaPct).First();
        RowAnalysis lowest = analysis.Rows.OrderBy(row => row.FactionFuelDeltaPct).First();
        Console.WriteLine("  documented range (§4.4)      : +2.0% .. +10.9% per 100 hp; endpoints reproduce as "
                          + Fmt.Pct(documentedMin.FactionFuelDeltaPct) + " (Medium.T4) and "
                          + Fmt.Pct(documentedMax.FactionFuelDeltaPct) + " (Medium.T1)");
        Console.WriteLine("  recomputed range (12 rows)   : " + Fmt.Pct(analysis.FactionDeltaMinPct) + " .. "
                          + Fmt.Pct(analysis.FactionDeltaMaxPct)
                          + " (lowest " + lowest.Engine.Id + ", widest " + widest.Engine.Id + ")");
        Console.WriteLine("  known deviation: Small.T1 fuel is collapsed by 2-decimal rounding"
                          + " (factionFuelCollapsed); Small.T2 is +11.1% because 0.07 x 0.90 rounds to"
                          + " 0.06. Requirement §4.4 quotes 2.0%..10.9%; P7 is read-only on data and"
                          + " reports the discrepancy to the Lead instead of editing values.");
    }

    /// <summary>Prints the consolidated 12-row PASS/FAIL matrix for the four criteria.</summary>
    public static void PrintMatrix(AnalysisResult analysis)
    {
        Fmt.Section("10. 12-ROW PASS/FAIL MATRIX");
        Console.WriteLine(Fmt.Line(
            ("row", 10), ("criterion 1", 12), ("criterion 2", 12), ("criterion 3", 12),
            ("criterion 4", 12), ("overall", 8)));
        int passing = 0;
        foreach (RowAnalysis row in analysis.Rows)
        {
            bool pass4 = analysis.ScaleChecks.First(check => check.Tier == row.Engine.Tier).Pass;
            bool overall = row.Pass1 && row.Pass2 && row.Pass3 && pass4;
            passing += overall ? 1 : 0;
            Console.WriteLine(Fmt.Line(
                (row.Engine.Id, 10),
                (Fmt.Verdict(row.Pass1), 12),
                (Fmt.Verdict(row.Pass2), 12),
                (Fmt.Verdict(row.Pass3), 12),
                (Fmt.Verdict(pass4), 12),
                (Fmt.Verdict(overall), 8)));
        }

        Console.WriteLine("  rows: " + passing + "/" + analysis.Rows.Count + " PASS; tier scale checks: "
                          + analysis.ScaleChecks.Count(check => check.Pass) + "/" + analysis.ScaleChecks.Count
                          + " tiers PASS");
    }

    private static void PrintCriterionLine(AnalysisResult analysis, int criterion)
    {
        switch (criterion)
        {
            case 1:
                Console.WriteLine("  Criterion 1: " + Fmt.Verdict(analysis.Criterion1) + " ("
                                  + analysis.Rows.Count(row => row.Pass1) + "/" + analysis.Rows.Count + " rows)");
                break;
            case 2:
                Console.WriteLine("  Criterion 2: " + Fmt.Verdict(analysis.Criterion2) + " ("
                                  + analysis.Rows.Count(row => row.Pass2) + "/" + analysis.Rows.Count + " rows)");
                break;
            case 3:
                Console.WriteLine("  Criterion 3: " + Fmt.Verdict(analysis.Criterion3) + " ("
                                  + analysis.Rows.Count(row => row.Pass3) + "/" + analysis.Rows.Count + " rows)");
                break;
            default:
                Console.WriteLine("  Criterion 4: " + Fmt.Verdict(analysis.Criterion4) + " ("
                                  + analysis.ScaleChecks.Count(check => check.Pass) + "/"
                                  + analysis.ScaleChecks.Count + " tiers)");
                break;
        }
    }
}
