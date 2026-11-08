using System;
using SEE.Core.Config;
using SEE.Core.Formulas;

namespace SEE.Tests
{
    /// <summary>
    /// Unit tests for <see cref="BoilerFormulas"/> (requirements §9.2, §11.3, REQ-BAL-01..04):
    /// wear per running hour, load factor, whole-horsepower output, proportional fuel, and the two
    /// capacity derivations. Values trace to the §9.2 table and §11.3 derivation rules.
    /// </summary>
    internal static class BoilerFormulasTests
    {
        internal static void Run(TestRunner t)
        {
            WearTests(t);
            WearPerHourTests(t);
            LoadFactorTests(t);
            OutputTests(t);
            FuelTests(t);
            ScalingTests(t);
            OperationalLoadTests(t);
            FactionTests(t);
            AverageOutputTests(t);
            CapacityTests(t);
            ClampTests(t);
            SanitizeTests(t);
        }

        private static void WearTests(TestRunner t)
        {
            t.Run("WearPerRunningHour T1 (6 days) is 0.347 %/h", () =>
                Assert.Near(0.347, BoilerFormulas.WearPerRunningHour(6.0), 1e-3, "T1"));
            t.Run("WearPerRunningHour T2 (8 days) is 0.260 %/h", () =>
                Assert.Near(0.260, BoilerFormulas.WearPerRunningHour(8.0), 1e-3, "T2"));
            t.Run("WearPerRunningHour T3 (10 days) is 0.208 %/h", () =>
                Assert.Near(0.208, BoilerFormulas.WearPerRunningHour(10.0), 1e-3, "T3"));
            t.Run("WearPerRunningHour T4 (12 days) is 0.174 %/h", () =>
                Assert.Near(0.174, BoilerFormulas.WearPerRunningHour(12.0), 1e-3, "T4"));
            t.Run("WearPerRunningHour is exactly 50/(days*24)", () =>
                Assert.Near(50.0 / (8.0 * 24.0), BoilerFormulas.WearPerRunningHour(8.0), 1e-12, "exact T2"));
            t.Run("WearPerRunningHour zero interval is 0", () =>
                Assert.Near(0.0, BoilerFormulas.WearPerRunningHour(0.0), 1e-12, "0 days"));
            t.Run("WearPerRunningHour NaN interval is 0", () =>
                Assert.Near(0.0, BoilerFormulas.WearPerRunningHour(double.NaN), 1e-12, "NaN"));
            t.Run("WearPerRunningHour negative interval is 0", () =>
                Assert.Near(0.0, BoilerFormulas.WearPerRunningHour(-8.0), 1e-12, "-8"));
        }

        private static void WearPerHourTests(TestRunner t)
        {
            t.Run("WearPerHour T2 full load is 50/(8*24) x 1.0", () =>
                Assert.Near(50.0 / (8.0 * 24.0), BoilerFormulas.WearPerHour(8.0, 1.0f), 1e-6, "T2 full"));
            t.Run("WearPerHour T2 half load is 50/(8*24) x 0.7", () =>
                Assert.Near(50.0 / (8.0 * 24.0) * 0.7, BoilerFormulas.WearPerHour(8.0, 0.5f), 1e-6, "T2 half"));
            t.Run("WearPerHour T4 full load matches the interval", () =>
                Assert.Near(50.0 / (12.0 * 24.0), BoilerFormulas.WearPerHour(12.0, 1.0f), 1e-6, "T4 full"));
            t.Run("WearPerHour idle load uses the 0.4 floor", () =>
                Assert.Near(50.0 / (8.0 * 24.0) * 0.4, BoilerFormulas.WearPerHour(8.0, 0f), 1e-6, "idle"));
            t.Run("WearPerHour invalid interval is 0", () =>
                Assert.Near(0.0, BoilerFormulas.WearPerHour(0.0, 1f), 1e-12, "zero interval"));
            t.Run("WearPerHour NaN interval is 0", () =>
                Assert.Near(0.0, BoilerFormulas.WearPerHour(double.NaN, 1f), 1e-12, "NaN interval"));
            t.Run("WearPerHour NaN load reads as the floor", () =>
                Assert.Near(50.0 / (8.0 * 24.0) * 0.4, BoilerFormulas.WearPerHour(8.0, float.NaN), 1e-6, "NaN load"));
        }

        private static void LoadFactorTests(TestRunner t)
        {
            t.Run("LoadFactor idle is 0.4", () => Assert.Near(0.4, BoilerFormulas.LoadFactor(0f), 1e-6, "0"));
            t.Run("LoadFactor half load is 0.7", () => Assert.Near(0.7, BoilerFormulas.LoadFactor(0.5f), 1e-6, "0.5"));
            t.Run("LoadFactor full load is 1.0", () => Assert.Near(1.0, BoilerFormulas.LoadFactor(1f), 1e-6, "1"));
            t.Run("LoadFactor negative clamps to 0.4", () => Assert.Near(0.4, BoilerFormulas.LoadFactor(-3f), 1e-6, "-3"));
            t.Run("LoadFactor above one clamps to 1.0", () => Assert.Near(1.0, BoilerFormulas.LoadFactor(7f), 1e-6, "7"));
            t.Run("LoadFactor NaN is 0.4", () => Assert.Near(0.4, BoilerFormulas.LoadFactor(float.NaN), 1e-6, "NaN"));
            t.Run("LoadFactor formula at 0.25 is 0.55", () => Assert.Near(0.55, BoilerFormulas.LoadFactor(0.25f), 1e-6, "0.25"));
        }

        private static void OutputTests(TestRunner t)
        {
            t.Run("OutputHorsepower full condition keeps rated", () =>
                Assert.Equal(460, BoilerFormulas.OutputHorsepower(460, 1.00f), "full"));
            t.Run("OutputHorsepower rounds up (460 x 0.93 -> 428)", () =>
                Assert.Equal(428, BoilerFormulas.OutputHorsepower(460, 0.93f), "0.93"));
            t.Run("OutputHorsepower rounds up (130 x 0.85 -> 111)", () =>
                Assert.Equal(111, BoilerFormulas.OutputHorsepower(130, 0.85f), "0.85"));
            t.Run("OutputHorsepower rounds up (460 x 0.40 -> 184)", () =>
                Assert.Equal(184, BoilerFormulas.OutputHorsepower(460, 0.40f), "0.40 band"));
            t.Run("OutputHorsepower zero multiplier is 0", () =>
                Assert.Equal(0, BoilerFormulas.OutputHorsepower(460, 0f), "0"));
            t.Run("OutputHorsepower invalid input is 0", () =>
                Assert.Equal(0, BoilerFormulas.OutputHorsepower(0, 1f), "rated 0"));
            t.Run("OutputHorsepower NaN multiplier is 0", () =>
                Assert.Equal(0, BoilerFormulas.OutputHorsepower(460, float.NaN), "NaN"));
        }

        private static void FuelTests(TestRunner t)
        {
            t.Run("FuelPerHourScaled full load is rated", () =>
                Assert.Near(0.12, BoilerFormulas.FuelPerHourScaled(0.12f, 460f, 460), 1e-6, "full"));
            t.Run("FuelPerHourScaled half load is half", () =>
                Assert.Near(0.06, BoilerFormulas.FuelPerHourScaled(0.12f, 230f, 460), 1e-6, "half"));
            t.Run("FuelPerHourScaled over-rated output clamps to rated", () =>
                Assert.Near(0.12, BoilerFormulas.FuelPerHourScaled(0.12f, 900f, 460), 1e-6, "over"));
            t.Run("FuelPerHourScaled zero output is zero", () =>
                Assert.Near(0.0, BoilerFormulas.FuelPerHourScaled(0.12f, 0f, 460), 1e-6, "zero output"));
            t.Run("FuelPerHourScaled zero rated horsepower is zero", () =>
                Assert.Near(0.0, BoilerFormulas.FuelPerHourScaled(0.12f, 100f, 0), 1e-6, "zero rated"));
            t.Run("FuelPerHourScaled NaN input is zero", () =>
                Assert.Near(0.0, BoilerFormulas.FuelPerHourScaled(float.NaN, 100f, 460), 1e-6, "NaN"));
        }

        private static void ScalingTests(TestRunner t)
        {
            t.Run("FuelPerHour proportional at load 1 is the rated rate", () =>
                Assert.Near(0.12, BoilerFormulas.FuelPerHour(0.12f, 1f, FuelScalingMode.Proportional), 1e-6, "full"));
            t.Run("FuelPerHour proportional at load 0.5 is half", () =>
                Assert.Near(0.06, BoilerFormulas.FuelPerHour(0.12f, 0.5f, FuelScalingMode.Proportional), 1e-6, "half"));
            t.Run("FuelPerHour proportional at load 0 is zero", () =>
                Assert.Near(0.0, BoilerFormulas.FuelPerHour(0.12f, 0f, FuelScalingMode.Proportional), 1e-6, "zero"));
            t.Run("FuelPerHour vanilla consumes the full rate at any positive load", () =>
            {
                Assert.Near(0.12, BoilerFormulas.FuelPerHour(0.12f, 0.5f, FuelScalingMode.Vanilla), 1e-6, "half load");
                Assert.Near(0.12, BoilerFormulas.FuelPerHour(0.12f, 1f, FuelScalingMode.Vanilla), 1e-6, "full load");
            });
            t.Run("FuelPerHour vanilla still stops at zero load", () =>
                Assert.Near(0.0, BoilerFormulas.FuelPerHour(0.12f, 0f, FuelScalingMode.Vanilla), 1e-6, "idle"));
            t.Run("FuelPerHour clamps an over-one load", () =>
                Assert.Near(0.12, BoilerFormulas.FuelPerHour(0.12f, 9f, FuelScalingMode.Proportional), 1e-6, "clamped"));
            t.Run("FuelPerHour invalid rate is zero", () =>
            {
                Assert.Near(0.0, BoilerFormulas.FuelPerHour(float.NaN, 1f, FuelScalingMode.Proportional), 1e-6, "NaN");
                Assert.Near(0.0, BoilerFormulas.FuelPerHour(-1f, 1f, FuelScalingMode.Proportional), 1e-6, "negative");
            });
            t.Run("WaterPerHour proportional at load 0.5 is half", () =>
                Assert.Near(0.16, BoilerFormulas.WaterPerHour(0.32f, 0.5f), 1e-6, "half"));
            t.Run("WaterPerHour zero load and invalid rate are zero", () =>
            {
                Assert.Near(0.0, BoilerFormulas.WaterPerHour(0.32f, 0f), 1e-6, "zero load");
                Assert.Near(0.0, BoilerFormulas.WaterPerHour(float.NaN, 1f), 1e-6, "NaN rate");
            });
        }

        /// <summary>
        /// 2026-11-08 hotfix-3: the operational load floor that keeps a supplied boiler running when
        /// the network demand is 0 at night (REQ-CORE-08 revision).
        /// </summary>
        private static void OperationalLoadTests(TestRunner t)
        {
            t.Run("OperationalLoadRatio keeps the demand when above standby", () =>
                Assert.Near(0.60, BoilerFormulas.OperationalLoadRatio(0.60f, 0.10f), 1e-6, "daytime"));
            t.Run("OperationalLoadRatio floors a below-standby demand", () =>
                Assert.Near(0.10, BoilerFormulas.OperationalLoadRatio(0.05f, 0.10f), 1e-6, "low demand"));
            t.Run("OperationalLoadRatio keeps a supplied boiler running at night", () =>
                Assert.Near(0.10, BoilerFormulas.OperationalLoadRatio(0f, 0.10f), 1e-6, "zero demand"));
            t.Run("OperationalLoadRatio 0 standby restores the old idle behaviour", () =>
            {
                Assert.Near(0.0, BoilerFormulas.OperationalLoadRatio(0f, 0f), 1e-6, "both zero");
                Assert.Near(0.42, BoilerFormulas.OperationalLoadRatio(0.42f, 0f), 1e-6, "demand only");
            });
            t.Run("OperationalLoadRatio clamps both operands into 0..1", () =>
            {
                Assert.Near(1.0, BoilerFormulas.OperationalLoadRatio(9f, 0.10f), 1e-6, "demand high");
                Assert.Near(1.0, BoilerFormulas.OperationalLoadRatio(0f, 9f), 1e-6, "standby high");
                Assert.Near(0.0, BoilerFormulas.OperationalLoadRatio(-2f, -1f), 1e-6, "negative");
            });
            t.Run("OperationalLoadRatio degrades NaN operands to the other side", () =>
            {
                Assert.Near(0.10, BoilerFormulas.OperationalLoadRatio(float.NaN, 0.10f), 1e-6, "NaN demand");
                Assert.Near(0.42, BoilerFormulas.OperationalLoadRatio(0.42f, float.NaN), 1e-6, "NaN standby");
                Assert.Near(0.0, BoilerFormulas.OperationalLoadRatio(float.NaN, float.NaN), 1e-6, "both NaN");
            });
        }

        private static void FactionTests(TestRunner t)
        {
            t.Run("FactionOutputHorsepower IT Medium.T2 rounds up to 483", () =>
                Assert.Equal(483, BoilerFormulas.FactionOutputHorsepower(460, 1.05f), "460 x 1.05"));
            t.Run("FactionOutputHorsepower IT Medium.T1 rounds 241.5 up to 242", () =>
                Assert.Equal(242, BoilerFormulas.FactionOutputHorsepower(230, 1.05f), "230 x 1.05"));
            t.Run("FactionOutputHorsepower FT keeps the base value", () =>
                Assert.Equal(130, BoilerFormulas.FactionOutputHorsepower(130, 1.00f), "130 x 1.00"));
            t.Run("FactionOutputHorsepower invalid input is zero", () =>
            {
                Assert.Equal(0, BoilerFormulas.FactionOutputHorsepower(0, 1.05f), "zero base");
                Assert.Equal(0, BoilerFormulas.FactionOutputHorsepower(460, float.NaN), "NaN coefficient");
            });
            t.Run("FactionFuelPerHour FT Medium.T2 rounds 0.108 to 0.11", () =>
                Assert.Near(0.11, BoilerFormulas.FactionFuelPerHour(0.12f, 0.90f), 1e-6, "0.12 x 0.90"));
            t.Run("FactionFuelPerHour IT keeps the base rate", () =>
                Assert.Near(0.12, BoilerFormulas.FactionFuelPerHour(0.12f, 1.00f), 1e-6, "0.12 x 1.00"));
            t.Run("FactionFuelPerHour FT Small.T1 collapses to 0.04", () =>
                Assert.Near(0.04, BoilerFormulas.FactionFuelPerHour(0.04f, 0.90f), 1e-6, "0.036 -> 0.04"));
            t.Run("FactionFuelPerHour invalid input is zero", () =>
                Assert.Near(0.0, BoilerFormulas.FactionFuelPerHour(0f, 0.90f), 1e-6, "zero base"));
        }

        private static void AverageOutputTests(TestRunner t)
        {
            t.Run("AverageConditionCoefficient T1 (6 days) is 0.971", () =>
                Assert.Near(0.971, BoilerFormulas.AverageConditionCoefficient(6.0, 1f), 6e-4, "T1"));
            t.Run("AverageConditionCoefficient T2 (8 days) is 0.974", () =>
                Assert.Near(0.974, BoilerFormulas.AverageConditionCoefficient(8.0, 1f), 6e-4, "T2"));
            t.Run("AverageConditionCoefficient T3 (10 days) is 0.976", () =>
                Assert.Near(0.976, BoilerFormulas.AverageConditionCoefficient(10.0, 1f), 6e-4, "T3"));
            t.Run("AverageConditionCoefficient T4 (12 days) is 0.978", () =>
                Assert.Near(0.978, BoilerFormulas.AverageConditionCoefficient(12.0, 1f), 6e-4, "T4"));
            t.Run("Partial load raises the coefficient but lowers the output", () =>
            {
                double full = BoilerFormulas.AverageConditionCoefficient(8.0, 1f);
                double half = BoilerFormulas.AverageConditionCoefficient(8.0, 0.5f);
                Assert.True(half > full, "lower load wears less per running hour");
                Assert.True(
                    BoilerFormulas.ThirtyDayAverageOutput(460, 8.0, 0.5f) < BoilerFormulas.ThirtyDayAverageOutput(460, 8.0, 1f),
                    "lower load still produces less absolute output");
            });
            t.Run("ThirtyDayAverageOutput Medium.T2 at full load is 448.2 hp", () =>
                Assert.Near(448.2, BoilerFormulas.ThirtyDayAverageOutput(460, 8.0, 1f), 0.1, "Medium T2"));
            t.Run("ThirtyDayAverageOutput Large.T4 at full load is 1466.7 hp", () =>
                Assert.Near(1466.7, BoilerFormulas.ThirtyDayAverageOutput(1500, 12.0, 1f), 0.1, "Large T4"));
            t.Run("MaintenanceDowntimeFraction is 1.37% for T1 and 0.69% for T4", () =>
            {
                Assert.Near(0.0137, BoilerFormulas.MaintenanceDowntimeFraction(6.0, 1f), 1e-4, "T1");
                Assert.Near(0.0069, BoilerFormulas.MaintenanceDowntimeFraction(12.0, 1f), 1e-4, "T4");
            });
            t.Run("A non-positive interval models disabled maintenance", () =>
            {
                Assert.Near(1.0, BoilerFormulas.AverageConditionCoefficient(0.0, 1f), 1e-12, "coefficient");
                Assert.Near(0.0, BoilerFormulas.MaintenanceDowntimeFraction(0.0, 1f), 1e-12, "downtime");
                Assert.Near(460.0, BoilerFormulas.ThirtyDayAverageOutput(460, 0.0, 1f), 1e-9, "output");
            });
        }

        private static void CapacityTests(TestRunner t)
        {
            t.Run("FuelStockGoods(0.12, 25) is 3", () => Assert.Equal(3, BoilerFormulas.FuelStockGoods(0.12f, 25f), "0.12"));
            t.Run("FuelStockGoods(0.10, 25) is 3", () => Assert.Equal(3, BoilerFormulas.FuelStockGoods(0.10f, 25f), "0.10"));
            t.Run("FuelStockGoods(0.04, 25) is 1", () => Assert.Equal(1, BoilerFormulas.FuelStockGoods(0.04f, 25f), "0.04"));
            t.Run("FuelStockGoods(0.18, 25) is 5", () => Assert.Equal(5, BoilerFormulas.FuelStockGoods(0.18f, 25f), "0.18"));
            t.Run("FuelStockGoods invalid is 0", () => Assert.Equal(0, BoilerFormulas.FuelStockGoods(0f, 25f), "zero rate"));
            t.Run("WaterTankGoods(0.32, 200) is 64", () => Assert.Equal(64, BoilerFormulas.WaterTankGoods(0.32f, 200f), "0.32"));
            t.Run("WaterTankGoods(0.29, 200) is 58", () => Assert.Equal(58, BoilerFormulas.WaterTankGoods(0.29f, 200f), "0.29"));
            t.Run("WaterTankGoods(0.10, 200) is 20", () => Assert.Equal(20, BoilerFormulas.WaterTankGoods(0.10f, 200f), "0.10"));
            t.Run("WaterTankGoods(0.09, 200) is 18", () => Assert.Equal(18, BoilerFormulas.WaterTankGoods(0.09f, 200f), "0.09"));
            t.Run("WaterTankGoods(0.27, 200) is 54", () => Assert.Equal(54, BoilerFormulas.WaterTankGoods(0.27f, 200f), "0.27"));
            t.Run("WaterTankGoods invalid is 0", () => Assert.Equal(0, BoilerFormulas.WaterTankGoods(0f, 200f), "zero rate"));
        }

        private static void ClampTests(TestRunner t)
        {
            t.Run("Clamp01 negative is 0", () => Assert.Near(0.0, BoilerFormulas.Clamp01(-2f), 1e-6, "-2"));
            t.Run("Clamp01 in range is unchanged", () => Assert.Near(0.42, BoilerFormulas.Clamp01(0.42f), 1e-6, "0.42"));
            t.Run("Clamp01 above one is 1", () => Assert.Near(1.0, BoilerFormulas.Clamp01(42f), 1e-6, "42"));
            t.Run("Clamp01 NaN is 0", () => Assert.Near(0.0, BoilerFormulas.Clamp01(float.NaN), 1e-6, "NaN"));
            t.Run("Clamp01 positive infinity is 1", () =>
                Assert.Near(1.0, BoilerFormulas.Clamp01(float.PositiveInfinity), 1e-6, "+Inf"));
        }

        private static void SanitizeTests(TestRunner t)
        {
            t.Run("Sanitize keeps finite non-negative values", () =>
            {
                Assert.Near(2.5, BoilerFormulas.Sanitize(2.5), 1e-12, "double");
                Assert.Near(0.5, BoilerFormulas.Sanitize(0.5f), 1e-6, "float");
            });
            t.Run("Sanitize degrades NaN, infinities and negatives to zero", () =>
            {
                Assert.Near(0.0, BoilerFormulas.Sanitize(double.NaN), 1e-12, "NaN");
                Assert.Near(0.0, BoilerFormulas.Sanitize(double.PositiveInfinity), 1e-12, "+Inf");
                Assert.Near(0.0, BoilerFormulas.Sanitize(double.NegativeInfinity), 1e-12, "-Inf");
                Assert.Near(0.0, BoilerFormulas.Sanitize(-3.0), 1e-12, "negative");
                Assert.Near(0.0, BoilerFormulas.Sanitize(float.NaN), 1e-6, "float NaN");
                Assert.Near(0.0, BoilerFormulas.Sanitize(-1f), 1e-6, "float negative");
            });
        }
    }
}
