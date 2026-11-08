using System;
using SEE.Core.Config;
using SEE.Core.Constants;

namespace SEE.Core.Formulas
{
    /// <summary>
    /// Pure production formulas shared by gameplay, UI and the offline balance tool
    /// (requirements §9.2/§13.5, REQ-BAL-01..04). Every member is defensive: NaN, infinities and
    /// negative inputs degrade to 0 instead of propagating into the simulation (REQ-SAFE-01).
    /// </summary>
    public static partial class BoilerFormulas
    {
        /// <summary>Wear per loaded running hour: <c>50 / (intervalDays * 24)</c> (REQ-MAINT-02).</summary>
        /// <param name="intervalDays">Maintenance interval in running days (6/8/10/12).</param>
        /// <returns>Condition points lost per loaded running hour; 0 for an invalid interval.</returns>
        public static double WearPerRunningHour(double intervalDays)
        {
            if (double.IsNaN(intervalDays) || double.IsInfinity(intervalDays) || intervalDays <= 0.0)
            {
                return 0.0;
            }

            return SEEConst.WearPointsPerInterval / (intervalDays * SEEConst.HoursPerDay);
        }

        /// <summary>
        /// Wear per loaded running hour: <c>WearPerRunningHour(intervalDays) * LoadFactor(loadRatio)</c>
        /// (requirements §3.4/§9.2, REQ-MAINT-02/03). This is the single formula the gameplay tick
        /// and the maintenance ledger share.
        /// </summary>
        /// <param name="intervalDays">Maintenance interval in running days (6/8/10/12).</param>
        /// <param name="loadRatio">Actual output divided by rated horsepower, 0..1.</param>
        /// <returns>Condition points lost per loaded running hour; 0 for an invalid input.</returns>
        public static float WearPerHour(double intervalDays, float loadRatio)
        {
            double baseWear = WearPerRunningHour(intervalDays);
            if (baseWear <= 0.0)
            {
                return 0f;
            }

            return (float)(baseWear * LoadFactor(loadRatio));
        }

        /// <summary>Load coefficient: <c>0.4 + 0.6 * clamp(loadRatio, 0, 1)</c> (REQ-MAINT-02).</summary>
        /// <param name="loadRatio">Actual output divided by rated horsepower.</param>
        /// <returns>Coefficient in 0.4..1.0; NaN reads as 0.4 (no load).</returns>
        public static float LoadFactor(float loadRatio)
        {
            return SEEConst.LoadFloor + (SEEConst.LoadFactorRange * Clamp01(loadRatio));
        }

        /// <summary>Actual output: <c>ceil(rated * conditionMultiplier)</c> whole horsepower.</summary>
        /// <param name="rated">Rated horsepower of the blueprint (already includes the faction multiplier).</param>
        /// <param name="conditionMultiplier">Condition curve multiplier 0..1.</param>
        /// <returns>Whole horsepower, never negative; 0 when a factor is invalid.</returns>
        public static int OutputHorsepower(int rated, float conditionMultiplier)
        {
            if (rated <= 0 || float.IsNaN(conditionMultiplier) || conditionMultiplier <= 0f)
            {
                return 0;
            }

            // Cast through decimal so the float representation of the documented multipliers
            // (0.85f is 0.85000002…) cannot bump an exact product over the next whole horsepower.
            decimal raw = rated * (decimal)conditionMultiplier;
            double ceiling = Math.Ceiling((double)raw);
            return ceiling >= int.MaxValue ? int.MaxValue : (int)ceiling;
        }

        /// <summary>
        /// Proportional fuel consumption: <c>ratedFuel * clamp(actualOutput / ratedHorsepower, 0, 1)</c>
        /// (REQ-FUEL-06, fuelScaling="proportional").
        /// </summary>
        /// <param name="ratedFuelPerHour">Rated fuel items per game hour at full load.</param>
        /// <param name="actualOutput">Actual output in hp.</param>
        /// <param name="ratedHorsepower">Rated horsepower of the blueprint.</param>
        /// <returns>Fuel items per game hour, never negative.</returns>
        public static float FuelPerHourScaled(float ratedFuelPerHour, float actualOutput, int ratedHorsepower)
        {
            if (float.IsNaN(ratedFuelPerHour) || ratedFuelPerHour <= 0f
                || ratedHorsepower <= 0 || float.IsNaN(actualOutput) || actualOutput <= 0f)
            {
                return 0f;
            }

            return ratedFuelPerHour * Clamp01(actualOutput / ratedHorsepower);
        }

        /// <summary>Fuel inventory goods: <c>ceil(fuelPerHour * fullInventoryWorkHours)</c> (REQ-FUEL-03).</summary>
        /// <param name="fuelPerHour">Rated fuel items per game hour.</param>
        /// <param name="fullInventoryWorkHours">Capacity expressed in full-load working hours (25).</param>
        /// <returns>Whole items; 0 for an invalid input.</returns>
        public static int FuelStockGoods(float fuelPerHour, float fullInventoryWorkHours)
        {
            if (float.IsNaN(fuelPerHour) || fuelPerHour <= 0f
                || float.IsNaN(fullInventoryWorkHours) || fullInventoryWorkHours <= 0f)
            {
                return 0;
            }

            double raw = fuelPerHour * (double)fullInventoryWorkHours;
            double ceiling = Math.Ceiling(raw);
            return ceiling >= int.MaxValue ? int.MaxValue : (int)ceiling;
        }

        /// <summary>Water tank goods: <c>round(waterPerHour * waterTankHours)</c> (REQ-FUEL-08).</summary>
        /// <param name="waterPerHour">Rated water items per game hour.</param>
        /// <param name="waterTankHours">Capacity expressed in full-load working hours (200).</param>
        /// <returns>Whole items; 0 for an invalid input.</returns>
        public static int WaterTankGoods(float waterPerHour, float waterTankHours)
        {
            if (float.IsNaN(waterPerHour) || waterPerHour <= 0f
                || float.IsNaN(waterTankHours) || waterTankHours <= 0f)
            {
                return 0;
            }

            double raw = waterPerHour * (double)waterTankHours;
            double rounded = Math.Round(raw, MidpointRounding.AwayFromZero);
            return rounded >= int.MaxValue ? int.MaxValue : (int)rounded;
        }

        /// <summary>
        /// Effective fuel consumption per hour under the configured scaling mode (requirements
        /// §3.4/§8.3, REQ-CORE-07, REQ-FUEL-06): <c>proportional</c> multiplies the rated rate by
        /// the clamped load, <c>vanilla</c> consumes the rated rate whenever the boiler carries any
        /// load. The spec value already contains the faction multiplier (PLAN §4.5 D-3), so this
        /// method never applies a faction factor again.
        /// </summary>
        /// <param name="ratedFuelPerHour">Rated fuel items per game hour at full load.</param>
        /// <param name="loadRatio">Network load ratio λ, 0..1.</param>
        /// <param name="scaling">Configured scaling mode.</param>
        /// <returns>Fuel items per game hour; 0 at zero load or for an invalid rate.</returns>
        public static float FuelPerHour(float ratedFuelPerHour, float loadRatio, FuelScalingMode scaling)
        {
            if (float.IsNaN(ratedFuelPerHour) || float.IsInfinity(ratedFuelPerHour) || ratedFuelPerHour <= 0f)
            {
                return 0f;
            }

            float load = Clamp01(loadRatio);
            if (load <= 0f)
            {
                return 0f;
            }

            return scaling == FuelScalingMode.Vanilla ? ratedFuelPerHour : ratedFuelPerHour * load;
        }

        /// <summary>
        /// Water consumption per hour (requirements §3.4/§8.3, REQ-CORE-07): always proportional to
        /// the load; only the fuel side honours <c>fuelScaling="vanilla"</c> (CONTRACT_SEE §8.3).
        /// </summary>
        /// <param name="ratedWaterPerHour">Rated water items per game hour at full load.</param>
        /// <param name="loadRatio">Network load ratio λ, 0..1.</param>
        /// <returns>Water items per game hour; 0 at zero load or for an invalid rate.</returns>
        public static float WaterPerHour(float ratedWaterPerHour, float loadRatio)
        {
            if (float.IsNaN(ratedWaterPerHour) || float.IsInfinity(ratedWaterPerHour) || ratedWaterPerHour <= 0f)
            {
                return 0f;
            }

            return ratedWaterPerHour * Clamp01(loadRatio);
        }

        /// <summary>
        /// Faction output derivation (requirements §4.4/REQ-BLD-10): the base horsepower multiplied
        /// by the faction output coefficient and rounded <b>up</b> to whole horsepower.
        /// </summary>
        /// <param name="baseHorsepower">Base (pre-faction) horsepower of the blueprint.</param>
        /// <param name="factionOutputMultiplier">Faction output coefficient (FT 1.00, IT 1.05).</param>
        /// <returns>Whole horsepower, never negative; 0 for an invalid input.</returns>
        public static int FactionOutputHorsepower(int baseHorsepower, float factionOutputMultiplier)
        {
            if (baseHorsepower <= 0 || float.IsNaN(factionOutputMultiplier)
                || float.IsInfinity(factionOutputMultiplier) || factionOutputMultiplier <= 0f)
            {
                return 0;
            }

            // Same decimal detour as OutputHorsepower: 1.05f must not turn 483 into 484.
            decimal raw = baseHorsepower * (decimal)factionOutputMultiplier;
            double ceiling = Math.Ceiling((double)raw);
            return ceiling >= int.MaxValue ? int.MaxValue : (int)ceiling;
        }

        /// <summary>
        /// Faction fuel derivation (requirements §4.4/REQ-BLD-11): the base fuel rate multiplied by
        /// the faction fuel coefficient and rounded to two decimals.
        /// </summary>
        /// <param name="baseFuelPerHour">Base (pre-faction) fuel rate.</param>
        /// <param name="factionFuelMultiplier">Faction fuel coefficient (FT 0.90, IT 1.00).</param>
        /// <returns>Fuel items per game hour rounded to two decimals; 0 for an invalid input.</returns>
        public static float FactionFuelPerHour(float baseFuelPerHour, float factionFuelMultiplier)
        {
            if (float.IsNaN(baseFuelPerHour) || float.IsInfinity(baseFuelPerHour) || baseFuelPerHour <= 0f
                || float.IsNaN(factionFuelMultiplier) || float.IsInfinity(factionFuelMultiplier)
                || factionFuelMultiplier <= 0f)
            {
                return 0f;
            }

            double raw = baseFuelPerHour * (double)factionFuelMultiplier;
            return (float)Math.Round(raw, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Average output coefficient over one maintenance cycle (requirements §5.5, REQ-BAL-05).
        ///
        /// The cycle wears from condition 100 down to the automatic-maintenance threshold 50 in two
        /// segments (100→61 at ×1.00, 61→50 at ×0.93), then adds the maintenance downtime. A
        /// non-positive or non-finite interval means "maintenance disabled": no wear cycle, so the
        /// coefficient is 1.0.
        /// </summary>
        /// <param name="intervalDays">Maintenance interval in running days (6/8/10/12).</param>
        /// <param name="averageLoad">Average load ratio L in 0..1 (1.0 is the conservative case).</param>
        /// <returns>Average output multiplier in 0.93..1.0 (or 1.0 when maintenance is disabled).</returns>
        public static double AverageConditionCoefficient(double intervalDays, float averageLoad)
        {
            if (!IsFinitePositive(intervalDays))
            {
                return 1.0;
            }

            double load = Clamp01(averageLoad);
            double loadFactor = SEEConst.LoadFloor + (SEEConst.LoadFactorRange * load);
            double wearPerHour = SEEConst.WearPointsPerInterval
                                 / (intervalDays * SEEConst.HoursPerDay)
                                 * loadFactor;
            if (wearPerHour <= 0.0)
            {
                return 1.0;
            }

            double fullBandHours = (SEEConst.MaxCondition - SEEConst.FullBandMin) / wearPerHour;
            double dueBandHours = (SEEConst.FullBandMin - SEEConst.AutoThresholdCondition) / wearPerHour;
            double cycleHours = fullBandHours + dueBandHours + SEEConst.MaintenanceDurationHours;
            return ((fullBandHours * 1.00) + (dueBandHours * 0.93)) / cycleHours;
        }

        /// <summary>
        /// 30-day average effective output including maintenance downtime and the condition decay
        /// (requirements §5.5, REQ-BAL-05): <c>rated * averageCoefficient * L</c>.
        /// </summary>
        /// <param name="ratedHorsepower">Rated horsepower (already includes the faction multiplier).</param>
        /// <param name="intervalDays">Maintenance interval in running days.</param>
        /// <param name="averageLoad">Average load ratio L in 0..1.</param>
        /// <returns>Average effective horsepower; 0 for an invalid rated value.</returns>
        public static double ThirtyDayAverageOutput(int ratedHorsepower, double intervalDays, float averageLoad)
        {
            if (ratedHorsepower <= 0)
            {
                return 0.0;
            }

            return ratedHorsepower * AverageConditionCoefficient(intervalDays, averageLoad) * Clamp01(averageLoad);
        }

        /// <summary>
        /// Share of the 30-day window spent in maintenance downtime (REQ-BAL-10: must stay ≤3%).
        /// </summary>
        /// <param name="intervalDays">Maintenance interval in running days.</param>
        /// <param name="averageLoad">Average load ratio L in 0..1.</param>
        /// <returns>Downtime fraction 0..1; 0 when maintenance is disabled.</returns>
        public static double MaintenanceDowntimeFraction(double intervalDays, float averageLoad)
        {
            if (!IsFinitePositive(intervalDays))
            {
                return 0.0;
            }

            double load = Clamp01(averageLoad);
            double loadFactor = SEEConst.LoadFloor + (SEEConst.LoadFactorRange * load);
            double wearPerHour = SEEConst.WearPointsPerInterval
                                 / (intervalDays * SEEConst.HoursPerDay)
                                 * loadFactor;
            if (wearPerHour <= 0.0)
            {
                return 0.0;
            }

            double fullBandHours = (SEEConst.MaxCondition - SEEConst.FullBandMin) / wearPerHour;
            double dueBandHours = (SEEConst.FullBandMin - SEEConst.AutoThresholdCondition) / wearPerHour;
            double cycleHours = fullBandHours + dueBandHours + SEEConst.MaintenanceDurationHours;
            return SEEConst.MaintenanceDurationHours / cycleHours;
        }

        /// <summary>
        /// Estimated game days until the automatic-maintenance threshold is reached at the given
        /// average load (requirements §9.5, panel row "next maintenance"). A fully maintained
        /// boiler at full load returns exactly one maintenance interval; an idle boiler never wears
        /// and returns <see cref="double.PositiveInfinity"/>.
        /// </summary>
        /// <param name="condition">Current condition 0..100.</param>
        /// <param name="intervalDays">Maintenance interval in running days.</param>
        /// <param name="averageLoad">Average load ratio 0..1.</param>
        /// <param name="threshold">Condition at which maintenance becomes due (default 50).</param>
        /// <returns>Days until the threshold; 0 when already due or invalid; +Infinity when no wear.</returns>
        public static double DaysUntilMaintenance(
            float condition,
            double intervalDays,
            float averageLoad,
            float threshold = SEEConst.AutoThreshold)
        {
            if (float.IsNaN(condition) || condition <= 0f)
            {
                return 0.0;
            }

            if (!IsFinitePositive(intervalDays))
            {
                // No maintenance interval = no wear cycle: the boiler never becomes due.
                return double.PositiveInfinity;
            }

            float target = threshold;
            if (float.IsNaN(target) || target < 0f)
            {
                target = SEEConst.AutoThreshold;
            }

            if (condition <= target)
            {
                return 0.0;
            }

            float load = Clamp01(averageLoad);
            if (load <= 0f)
            {
                // An idle boiler never wears (requirements §3.3/§9.2), so it never becomes due.
                return double.PositiveInfinity;
            }

            double wearPerHour = WearPerRunningHour(intervalDays) * LoadFactor(load);
            if (wearPerHour <= 0.0)
            {
                return double.PositiveInfinity;
            }

            return (condition - target) / wearPerHour / SEEConst.HoursPerDay;
        }

        /// <summary>
        /// Defensive sanitizer used before a formula consumes a raw value (REQ-SAFE-01): NaN,
        /// infinities and negative numbers degrade to 0.
        /// </summary>
        /// <param name="value">Raw value.</param>
        /// <returns>The value when finite and non-negative, otherwise 0.</returns>
        public static double Sanitize(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) || value < 0.0 ? 0.0 : value;
        }

        /// <summary>Float overload of <see cref="Sanitize(double)"/>.</summary>
        /// <param name="value">Raw value.</param>
        /// <returns>The value when finite and non-negative, otherwise 0.</returns>
        public static float Sanitize(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) || value < 0f ? 0f : value;
        }

        /// <summary>Clamps a ratio into 0..1; NaN and infinities degrade to the nearest bound.</summary>
        /// <param name="value">Value to clamp.</param>
        /// <returns>A finite value in 0..1.</returns>
        public static float Clamp01(float value)
        {
            if (float.IsNaN(value))
            {
                return 0f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private static bool IsFinitePositive(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
        }
    }
}
