using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Balance;
using SEE.Core.Formulas;

namespace SEE.Tests
{
    /// <summary>
    /// Tests for <see cref="TierDefinition"/>/<see cref="TierCatalog"/> (requirements §4.1,
    /// REQ-CORE-04) and the 30-day average output table of requirements §5.5 (REQ-BAL-05).
    ///
    /// The fixture below is the authoritative 12-row table copied from requirements §4.1 /
    /// SHARED_BRIEF §3. It lives in the test project on purpose: the production code must never
    /// hard-code balance values (CONTRACT_SEE §10 rule 6), and this fixture is what a future
    /// generator would feed from <c>engines.derived.json</c>.
    /// </summary>
    internal static class TierCatalogTests
    {
        internal static void Run(TestRunner t)
        {
            CatalogShapeTests(t);
            CatalogValidationTests(t);
            RequirementTableTests(t);
            ThirtyDayAverageTableTests(t);
        }

        private static void CatalogShapeTests(TestRunner t)
        {
            t.Run("The injected fixture builds a complete 12-entry catalog", () =>
            {
                var catalog = BuildCatalog();
                Assert.Equal(12, catalog.Count, "count");
                Assert.Equal(TierCatalog.ExpectedCount, catalog.Count, "expected count");
                Assert.Equal(12, catalog.Definitions.Count, "definitions");
            });

            t.Run("Every (Size, Tier) combination resolves to its definition", () =>
            {
                var catalog = BuildCatalog();
                foreach (BoilerSize size in Enum.GetValues(typeof(BoilerSize)))
                {
                    foreach (BoilerTier tier in Enum.GetValues(typeof(BoilerTier)))
                    {
                        Assert.True(catalog.TryGet(size, tier, out TierDefinition definition),
                            "missing " + size + "/" + tier);
                        Assert.Equal(size, definition.Size, "size of " + size + "/" + tier);
                        Assert.Equal(tier, definition.Tier, "tier of " + size + "/" + tier);
                    }
                }
            });

            t.Run("Get returns the Medium.T2 row used by the balance examples", () =>
            {
                var definition = BuildCatalog().Get(BoilerSize.Medium, BoilerTier.T2);
                Assert.Equal(460, definition.BaseHorsepower, "base horsepower");
                Assert.Near(0.12, definition.BaseFuelPerHour, 1e-6, "base fuel");
                Assert.Near(0.32, definition.WaterPerHour, 1e-6, "water");
                Assert.Equal(3, definition.FuelStockGoods, "fuel stock");
                Assert.Equal(64, definition.WaterTankGoods, "water tank");
                Assert.Near(8.0, definition.MaintenanceIntervalDays, 1e-6, "interval");
            });

            t.Run("An unknown combination is reported as missing", () =>
            {
                var catalog = BuildCatalog();
                Assert.False(catalog.TryGet(BoilerSize.Large, BoilerTier.T4, out _) == false, "T4 exists");
                Assert.True(catalog.TryGet(BoilerSize.Large, (BoilerTier)99, out _) == false, "undefined tier");
            });

            t.Run("TemplateName matches the frozen naming contract", () =>
            {
                var definition = BuildCatalog().Get(BoilerSize.Medium, BoilerTier.T2);
                Assert.Equal("SteamBoiler.Medium.T2.Folktails", definition.TemplateName(BoilerFaction.Folktails), "FT");
                Assert.Equal("SteamBoiler.Medium.T2.IronTeeth", definition.TemplateName(BoilerFaction.IronTeeth), "IT");
            });
        }

        private static void CatalogValidationTests(TestRunner t)
        {
            t.Run("Null definitions are rejected", () =>
                Assert.Throws<ArgumentNullException>(() => new TierCatalog(null!), "null catalog"));

            t.Run("An incomplete family is rejected", () =>
            {
                var rows = new List<TierDefinition>(BuildCatalog().Definitions);
                Assert.Throws<ArgumentException>(() => new TierCatalog(rows.GetRange(0, 11)), "11 rows");
            });

            t.Run("A duplicated (Size, Tier) row is rejected", () =>
            {
                var rows = new List<TierDefinition>(BuildCatalog().Definitions);
                rows[11] = BuildDefinition(BoilerSize.Small, BoilerTier.T1, 130, 0.04f, 0.10f, 1, 20, 6);
                Assert.Throws<ArgumentException>(() => new TierCatalog(rows), "duplicate replaces the last row");
            });

            t.Run("A non-positive horsepower is rejected", () =>
                Assert.Throws<ArgumentException>(
                    () => new TierCatalog(Replace(BuildCatalog(), BoilerSize.Small, BoilerTier.T1,
                        BuildDefinition(BoilerSize.Small, BoilerTier.T1, 0, 0.04f, 0.10f, 1, 20, 6))),
                    "zero hp"));

            t.Run("A NaN fuel rate is rejected", () =>
                Assert.Throws<ArgumentException>(
                    () => new TierCatalog(Replace(BuildCatalog(), BoilerSize.Small, BoilerTier.T1,
                        BuildDefinition(BoilerSize.Small, BoilerTier.T1, 130, float.NaN, 0.10f, 1, 20, 6))),
                    "NaN fuel"));

            t.Run("A negative water tank is rejected", () =>
                Assert.Throws<ArgumentException>(
                    () => new TierCatalog(Replace(BuildCatalog(), BoilerSize.Small, BoilerTier.T1,
                        BuildDefinition(BoilerSize.Small, BoilerTier.T1, 130, 0.04f, 0.10f, 1, -1, 6))),
                    "negative tank"));
        }

        private static void RequirementTableTests(TestRunner t)
        {
            t.Run("Every row keeps the requirement §4.1 base horsepower", () =>
            {
                var catalog = BuildCatalog();
                Assert.Equal(130, catalog.Get(BoilerSize.Small, BoilerTier.T1).BaseHorsepower, "Small T1");
                Assert.Equal(230, catalog.Get(BoilerSize.Medium, BoilerTier.T1).BaseHorsepower, "Medium T1");
                Assert.Equal(360, catalog.Get(BoilerSize.Large, BoilerTier.T1).BaseHorsepower, "Large T1");
                Assert.Equal(260, catalog.Get(BoilerSize.Small, BoilerTier.T2).BaseHorsepower, "Small T2");
                Assert.Equal(460, catalog.Get(BoilerSize.Medium, BoilerTier.T2).BaseHorsepower, "Medium T2");
                Assert.Equal(720, catalog.Get(BoilerSize.Large, BoilerTier.T2).BaseHorsepower, "Large T2");
                Assert.Equal(380, catalog.Get(BoilerSize.Small, BoilerTier.T3).BaseHorsepower, "Small T3");
                Assert.Equal(680, catalog.Get(BoilerSize.Medium, BoilerTier.T3).BaseHorsepower, "Medium T3");
                Assert.Equal(1060, catalog.Get(BoilerSize.Large, BoilerTier.T3).BaseHorsepower, "Large T3");
                Assert.Equal(520, catalog.Get(BoilerSize.Small, BoilerTier.T4).BaseHorsepower, "Small T4");
                Assert.Equal(920, catalog.Get(BoilerSize.Medium, BoilerTier.T4).BaseHorsepower, "Medium T4");
                Assert.Equal(1500, catalog.Get(BoilerSize.Large, BoilerTier.T4).BaseHorsepower, "Large T4");
            });

            t.Run("Fuel stock and water tank equal the §4.1 derivations", () =>
            {
                foreach (TierDefinition definition in BuildCatalog().Definitions)
                {
                    Assert.Equal(
                        BoilerFormulas.FuelStockGoods(definition.BaseFuelPerHour, 25f),
                        definition.FuelStockGoods,
                        "fuel stock of " + definition.TemplateName(BoilerFaction.Folktails));
                    Assert.Equal(
                        BoilerFormulas.WaterTankGoods(definition.WaterPerHour, 200f),
                        definition.WaterTankGoods,
                        "water tank of " + definition.TemplateName(BoilerFaction.Folktails));
                }
            });
        }

        private static void ThirtyDayAverageTableTests(TestRunner t)
        {
            // Requirements §5.5 table: rated hp, maintenance interval and the reference horsepower
            // of the vanilla comparison. Each row is one named test so the 12-row table is visible
            // in the PASS output even when the runner only prints failures.
            var table = new (BoilerSize Size, BoilerTier Tier, int Hp, double Interval, int Reference,
                double Coefficient, double AverageOutput)[]
            {
                (BoilerSize.Small, BoilerTier.T1, 130, 6, 50, 0.971, 126.2),
                (BoilerSize.Medium, BoilerTier.T1, 230, 6, 100, 0.971, 223.4),
                (BoilerSize.Large, BoilerTier.T1, 360, 6, 270, 0.971, 349.6),
                (BoilerSize.Small, BoilerTier.T2, 260, 8, 150, 0.974, 253.4),
                (BoilerSize.Medium, BoilerTier.T2, 460, 8, 400, 0.974, 448.2),
                (BoilerSize.Large, BoilerTier.T2, 720, 8, 540, 0.974, 701.6),
                (BoilerSize.Small, BoilerTier.T3, 380, 10, 300, 0.976, 371.1),
                (BoilerSize.Medium, BoilerTier.T3, 680, 10, 400, 0.976, 664.0),
                (BoilerSize.Large, BoilerTier.T3, 1060, 10, 800, 0.976, 1035.1),
                (BoilerSize.Small, BoilerTier.T4, 520, 12, 400, 0.978, 508.5),
                (BoilerSize.Medium, BoilerTier.T4, 920, 12, 800, 0.978, 899.6),
                (BoilerSize.Large, BoilerTier.T4, 1500, 12, 1500, 0.978, 1466.7),
            };

            foreach (var row in table)
            {
                TierDefinition definition = BuildCatalog().Get(row.Size, row.Tier);
                string label = row.Size + " " + row.Tier;
                t.Run("30-day average output " + label + " matches the §5.5 table", () =>
                {
                    Assert.Equal(row.Hp, definition.BaseHorsepower, label + " hp");
                    Assert.Near(row.Interval, definition.MaintenanceIntervalDays, 1e-6, label + " interval");

                    double coefficient = BoilerFormulas.AverageConditionCoefficient(row.Interval, 1f);
                    Assert.Near(row.Coefficient, coefficient, 6e-4, label + " coefficient");

                    double output = BoilerFormulas.ThirtyDayAverageOutput(row.Hp, row.Interval, 1f);
                    Assert.Near(row.AverageOutput, output, 0.1, label + " average output");

                    // REQ-BAL-05: the 30-day average must stay above reference x 0.95.
                    Assert.True(output >= row.Reference * 0.95, label + " stays above reference x 0.95");
                });
            }

            t.Run("Only Large.T4 has less than 5% margin over the reference", () =>
            {
                double largeT4 = BoilerFormulas.ThirtyDayAverageOutput(1500, 12.0, 1f);
                double margin = (largeT4 / 1500.0) - 0.95;
                Assert.Near(0.0278, margin, 0.002, "≈+2.9% margin documented by requirements §5.5");
            });
        }

        internal static TierCatalog BuildCatalog()
        {
            return new TierCatalog(new[]
            {
                BuildDefinition(BoilerSize.Small, BoilerTier.T1, 130, 0.04f, 0.10f, 1, 20, 6),
                BuildDefinition(BoilerSize.Medium, BoilerTier.T1, 230, 0.07f, 0.18f, 2, 36, 6),
                BuildDefinition(BoilerSize.Large, BoilerTier.T1, 360, 0.12f, 0.29f, 3, 58, 6),
                BuildDefinition(BoilerSize.Small, BoilerTier.T2, 260, 0.07f, 0.18f, 2, 36, 8),
                BuildDefinition(BoilerSize.Medium, BoilerTier.T2, 460, 0.12f, 0.32f, 3, 64, 8),
                BuildDefinition(BoilerSize.Large, BoilerTier.T2, 720, 0.18f, 0.50f, 5, 100, 8),
                BuildDefinition(BoilerSize.Small, BoilerTier.T3, 380, 0.08f, 0.15f, 2, 30, 10),
                BuildDefinition(BoilerSize.Medium, BoilerTier.T3, 680, 0.14f, 0.27f, 4, 54, 10),
                BuildDefinition(BoilerSize.Large, BoilerTier.T3, 1060, 0.22f, 0.42f, 6, 84, 10),
                BuildDefinition(BoilerSize.Small, BoilerTier.T4, 520, 0.08f, 0.09f, 2, 18, 12),
                BuildDefinition(BoilerSize.Medium, BoilerTier.T4, 920, 0.15f, 0.17f, 4, 34, 12),
                BuildDefinition(BoilerSize.Large, BoilerTier.T4, 1500, 0.24f, 0.27f, 6, 54, 12),
            });
        }

        private static TierDefinition BuildDefinition(
            BoilerSize size,
            BoilerTier tier,
            int horsepower,
            float fuelPerHour,
            float waterPerHour,
            int fuelStock,
            int waterTank,
            float intervalDays)
        {
            return new TierDefinition
            {
                Size = size,
                Tier = tier,
                BaseHorsepower = horsepower,
                BaseFuelPerHour = fuelPerHour,
                WaterPerHour = waterPerHour,
                FuelStockGoods = fuelStock,
                WaterTankGoods = waterTank,
                MaintenanceIntervalDays = intervalDays
            };
        }

        private static IReadOnlyList<TierDefinition> Replace(
            TierCatalog catalog,
            BoilerSize size,
            BoilerTier tier,
            TierDefinition replacement)
        {
            var rows = new List<TierDefinition>(catalog.Definitions);
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Size == size && rows[i].Tier == tier)
                {
                    rows[i] = replacement;
                }
            }

            return rows;
        }
    }
}
