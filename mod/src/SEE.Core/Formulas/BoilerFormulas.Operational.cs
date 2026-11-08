
namespace SEE.Core.Formulas
{
    /// <summary>
    /// Partial part: the operational (running) load of a boiler. Split out of
    /// <c>BoilerFormulas.cs</c> to keep every source file under the 400-line gate (§11.5).
    /// </summary>
    public static partial class BoilerFormulas
    {
        /// <summary>
        /// Load ratio the boiler actually runs at:
        /// <c>max(clamp01(demandRatio), clamp01(standbyRatio))</c> (2026-11-08 hotfix-3,
        /// REQ-CORE-08 revision). The game disables the power consumption of worker buildings
        /// outside working hours, so <c>graph.PowerDemand</c> drops to 0 at night; taking the maximum
        /// with the configured standby ratio keeps the boiler running at a low load instead of going
        /// <see cref="SEE.Core.Abstractions.BoilerStatus.Idle"/>. A standby ratio of 0 restores the
        /// original idle-at-zero-demand behaviour.
        /// </summary>
        /// <param name="demandRatio">Raw network demand ratio λ in 0..1 (0 when nothing demands power).</param>
        /// <param name="standbyRatio">Configured <c>balance.standbyLoadRatio</c> in 0..1.</param>
        /// <returns>A ratio in 0..1; NaN/invalid inputs degrade to the other operand or 0.</returns>
        public static float OperationalLoadRatio(float demandRatio, float standbyRatio)
        {
            float demand = Clamp01(demandRatio);
            float standby = Clamp01(standbyRatio);
            return demand >= standby ? demand : standby;
        }
    }
}
