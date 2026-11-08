using System;
using SEE.Core.Formulas;

namespace SEE.Tests
{
    /// <summary>
    /// Boundary tests for <see cref="ConditionCurve"/> (requirements §9.3): each condition value
    /// belongs to exactly one band — 61–100 → 1.00, 41–60 → 0.93, 21–40 → 0.85, 1–20 → 0.40, 0 → 0.
    /// </summary>
    internal static class ConditionCurveTests
    {
        internal static void Run(TestRunner t)
        {
            BoundaryTests(t);
            BandTests(t);
            DefensiveTests(t);
        }

        private static void BoundaryTests(TestRunner t)
        {
            t.Run("ConditionCurve 100 is 1.00", () => Near(1.00, 100f, "100"));
            t.Run("ConditionCurve 61 is 1.00", () => Near(1.00, 61f, "61"));
            t.Run("ConditionCurve 60 is 0.93", () => Near(0.93, 60f, "60"));
            t.Run("ConditionCurve 41 is 0.93", () => Near(0.93, 41f, "41"));
            t.Run("ConditionCurve 40 is 0.85", () => Near(0.85, 40f, "40"));
            t.Run("ConditionCurve 21 is 0.85", () => Near(0.85, 21f, "21"));
            t.Run("ConditionCurve 20 is 0.40", () => Near(0.40, 20f, "20"));
            t.Run("ConditionCurve 1 is 0.40", () => Near(0.40, 1f, "1"));
            t.Run("ConditionCurve 0 is 0", () => Near(0.00, 0f, "0"));
            t.Run("ConditionCurve above 100 clamps to 1.00", () => Near(1.00, 250f, "250"));
            t.Run("ConditionMultiplier is the card-named alias of Multiplier", () =>
            {
                float[] samples = { 0f, 1f, 20f, 21f, 40f, 41f, 60f, 61f, 100f };
                foreach (float condition in samples)
                {
                    Assert.Near(
                        ConditionCurve.Multiplier(condition),
                        ConditionCurve.ConditionMultiplier(condition),
                        1e-9,
                        "alias at " + condition);
                }
            });
        }

        private static void BandTests(TestRunner t)
        {
            t.Run("Every sampled condition maps to one of the five documented multipliers", () =>
            {
                float[] allowed = { 0f, 0.40f, 0.85f, 0.93f, 1.00f };
                for (var condition = 0f; condition <= 100f; condition += 1f)
                {
                    var value = ConditionCurve.Multiplier(condition);
                    var matches = false;
                    foreach (var candidate in allowed)
                    {
                        if (Math.Abs(value - candidate) < 1e-6f)
                        {
                            matches = true;
                            break;
                        }
                    }

                    Assert.True(matches, "multiplier at condition " + condition + " must be a documented value, was " + value);
                }
            });

            t.Run("Multiplier never decreases as condition rises", () =>
            {
                var previous = 0f;
                for (var condition = 0f; condition <= 100f; condition += 0.5f)
                {
                    var value = ConditionCurve.Multiplier(condition);
                    Assert.True(value >= previous,
                        "multiplier must be monotonic: condition " + condition + " -> " + value + " after " + previous);
                    previous = value;
                }
            });
        }

        private static void DefensiveTests(TestRunner t)
        {
            t.Run("ConditionCurve NaN is 0", () => Near(0.0, float.NaN, "NaN"));
            t.Run("ConditionCurve negative is 0", () => Near(0.0, -5f, "-5"));
            t.Run("ConditionCurve tiny positive is 0.40", () => Near(0.40, 0.001f, "epsilon"));
        }

        private static void Near(double expected, float condition, string label) =>
            Assert.Near(expected, ConditionCurve.Multiplier(condition), 1e-6, "condition " + label);
    }
}
