using System;
using System.Globalization;

namespace SEE.UI.Panels
{
    /// <summary>
    /// Culture-invariant, NaN-safe formatting for the panel (requirements §15.2, §15.5 L2).
    ///
    /// The panel inserts numbers into localized templates as pre-formatted strings, exactly like
    /// the sister mods do; every helper therefore returns a string that a player can read instead
    /// of ever leaking "NaN", "Infinity" or a negative count. Values that cannot be displayed (for
    /// example a material list the read model does not expose) become <see cref="Unknown"/>.
    /// </summary>
    public static class PanelNumbers
    {
        /// <summary>Locale-neutral placeholder shown when a value is unavailable.</summary>
        public const string Unknown = "\u2014";

        /// <summary>One decimal place, invariant culture, "-" for non-finite or negative values.</summary>
        /// <param name="value">Raw measurement.</param>
        /// <returns>For example "27.3".</returns>
        public static string OneDecimal(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                return Unknown;
            }

            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        /// <summary>Two decimal places, invariant culture, "-" for non-finite or negative values.</summary>
        /// <param name="value">Raw measurement.</param>
        /// <returns>For example "0.11".</returns>
        public static string TwoDecimals(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                return Unknown;
            }

            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        /// <summary>Whole number; negative or non-finite values become 0.</summary>
        /// <param name="value">Raw count.</param>
        /// <returns>For example "64".</returns>
        public static string Whole(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "0";
            }

            double clamped = value < 0.0 ? 0.0 : value;
            return ((long)Math.Round(clamped, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Condition in 0..100 rounded to a whole number, as the panel prints it.</summary>
        /// <param name="condition">Raw condition.</param>
        /// <returns>0..100.</returns>
        public static int ConditionPercent(float condition)
        {
            if (float.IsNaN(condition) || float.IsInfinity(condition))
            {
                return 0;
            }

            float clamped = condition < 0f ? 0f : (condition > 100f ? 100f : condition);
            return (int)Math.Round(clamped, MidpointRounding.AwayFromZero);
        }

        /// <summary>Whole percent of a 0..1 fraction.</summary>
        /// <param name="fraction">Ratio; clamped to 0..1.</param>
        /// <returns>0..100.</returns>
        public static int PercentPoint(float fraction)
        {
            if (float.IsNaN(fraction) || float.IsInfinity(fraction))
            {
                return 0;
            }

            float clamped = fraction < 0f ? 0f : (fraction > 1f ? 1f : fraction);
            return (int)Math.Round(clamped * 100f, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Water cargo in cubic metres, the unit requirements §15.2 asks for: 1 m³ = 5 water
        /// (research 02 §2.3 A, flagged V-07). The conversion is display-only.
        /// </summary>
        /// <param name="waterGoods">Water cargo in goods.</param>
        /// <returns>For example "12.8".</returns>
        public static string CubicMetres(int waterGoods)
        {
            if (waterGoods <= 0)
            {
                return "0.0";
            }

            return (waterGoods / 5f).ToString("0.0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// How many beavers' drinking water this boiler consumes per game day
        /// (requirements §15.2/§7.3: one beaver drinks 2.12 water per day).
        /// </summary>
        /// <param name="waterPerHour">Rated water consumption in goods per game hour.</param>
        /// <param name="beaverWaterPerDay">Daily intake of one beaver; values &lt;= 0 return 0.</param>
        /// <returns>For example 3.6.</returns>
        public static float BeaverEquivalent(float waterPerHour, float beaverWaterPerDay)
        {
            if (float.IsNaN(waterPerHour) || float.IsInfinity(waterPerHour) || waterPerHour < 0f
                || float.IsNaN(beaverWaterPerDay) || float.IsInfinity(beaverWaterPerDay) || beaverWaterPerDay <= 0f)
            {
                return 0f;
            }

            return waterPerHour * 24f / beaverWaterPerDay;
        }

        /// <summary>Hours a stock of goods lasts at the rated rate; 0 when the rate is not positive.</summary>
        /// <param name="stock">Goods in stock.</param>
        /// <param name="perHour">Rated consumption per game hour.</param>
        /// <returns>Remaining hours, or 0.</returns>
        public static float RemainingHours(int stock, float perHour)
        {
            if (stock <= 0 || float.IsNaN(perHour) || float.IsInfinity(perHour) || perHour <= 0f)
            {
                return 0f;
            }

            return stock / perHour;
        }

        /// <summary>True when the remaining hours are a low-supply warning (positive but below the threshold).</summary>
        /// <param name="remainingHours">Remaining hours from <see cref="RemainingHours"/>.</param>
        /// <param name="thresholdHours">Configured <c>ui.lowSupplyHours</c>.</param>
        /// <returns>True when the warning row should be shown.</returns>
        public static bool IsLowSupply(float remainingHours, float thresholdHours) =>
            remainingHours > 0f && remainingHours < thresholdHours;

        /// <summary>Clamps a condition value into 0..100 for the progress bar.</summary>
        /// <param name="condition">Raw condition.</param>
        /// <returns>0..100.</returns>
        public static float ConditionBarValue(float condition)
        {
            if (float.IsNaN(condition) || float.IsInfinity(condition))
            {
                return 0f;
            }

            return condition < 0f ? 0f : (condition > 100f ? 100f : condition);
        }
    }
}
