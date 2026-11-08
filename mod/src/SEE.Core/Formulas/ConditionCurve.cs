namespace SEE.Core.Formulas
{
    /// <summary>
    /// Condition to output multiplier curve (requirements §9.3/§13.5). Bands are closed on their
    /// lower bound: 100/61 → 1.00, 60..41 → 0.93, 40..21 → 0.85, 20..1 → 0.40, 0 → 0.
    /// Each value belongs to exactly one band, with no overlaps and no gaps.
    /// </summary>
    public static class ConditionCurve
    {
        /// <summary>Returns the output multiplier for a condition value (card name for <see cref="Multiplier"/>).</summary>
        /// <param name="condition">Condition, normally 0..100; values above 100 clamp to 1.00.</param>
        /// <returns>0, 0.40, 0.85, 0.93 or 1.00.</returns>
        public static float ConditionMultiplier(float condition) => Multiplier(condition);

        /// <summary>Returns the output multiplier for a condition value.</summary>
        /// <param name="condition">Condition, normally 0..100; values above 100 clamp to 1.00.</param>
        /// <returns>0, 0.40, 0.85, 0.93 or 1.00.</returns>
        public static float Multiplier(float condition)
        {
            if (float.IsNaN(condition) || condition <= 0f)
            {
                return 0f;
            }

            if (condition > 60f)
            {
                return 1.00f;
            }

            if (condition > 40f)
            {
                return 0.93f;
            }

            return condition > 20f ? 0.85f : 0.40f;
        }
    }
}
