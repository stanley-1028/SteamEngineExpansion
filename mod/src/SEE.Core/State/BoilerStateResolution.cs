using SEE.Core.Abstractions;
using SEE.Core.Formulas;

namespace SEE.Core.State
{
    /// <summary>
    /// Full resolution of one boiler for one tick (requirements §3.3/§15.3, REQ-CORE-06/11): the
    /// winning main state, its frozen badge localization key (<c>Status.SEE.*</c>), the machine-
    /// readable cause, the effective output multiplier and the network load λ.
    ///
    /// <see cref="OutputMultiplier"/> is the multiplier applied to the rated horsepower:
    /// <c>clamp01(λ × ConditionCurve)</c> for the four producing states and 0 for every blocked
    /// state (CONTRACT_SEE §8.4). <see cref="LoadRatio"/> is λ itself, independently clamped.
    /// </summary>
    public readonly struct BoilerStateResolution
    {
        /// <summary>Creates one resolution.</summary>
        /// <param name="status">Winning main (or level-0 override) state.</param>
        /// <param name="badgeLocKey">Frozen <c>Status.SEE.*</c> badge key.</param>
        /// <param name="reason">Machine-readable cause of the state.</param>
        /// <param name="outputMultiplier">Effective multiplier applied to the rated horsepower.</param>
        /// <param name="loadRatio">Network load ratio λ, 0..1.</param>
        public BoilerStateResolution(
            BoilerStatus status,
            string badgeLocKey,
            BoilerStatusReason reason,
            float outputMultiplier,
            float loadRatio)
        {
            Status = status;
            BadgeLocKey = badgeLocKey;
            Reason = reason;
            OutputMultiplier = outputMultiplier;
            LoadRatio = loadRatio;
        }

        /// <summary>Winning main state (nine main states plus the <c>Maintaining</c> override).</summary>
        public BoilerStatus Status { get; }

        /// <summary>Frozen badge localization key of the state.</summary>
        public string BadgeLocKey { get; }

        /// <summary>Machine-readable cause of the state.</summary>
        public BoilerStatusReason Reason { get; }

        /// <summary>Effective multiplier applied to the rated horsepower (0 when blocked).</summary>
        public float OutputMultiplier { get; }

        /// <summary>Network load ratio λ, 0..1.</summary>
        public float LoadRatio { get; }

        /// <summary>True when this state produces power (and therefore consumes and wears).</summary>
        public bool Produces => BoilerStateMachine.Produces(Status);

        /// <summary>
        /// Actual output in whole horsepower for a rated value (already includes the faction
        /// coefficient), rounded up once at the end (REQ-POWER-13).
        /// </summary>
        /// <param name="ratedHorsepower">Rated horsepower of the blueprint.</param>
        /// <returns>Whole horsepower; 0 for a blocked state.</returns>
        public int OutputHorsepower(int ratedHorsepower)
        {
            return BoilerFormulas.OutputHorsepower(ratedHorsepower, OutputMultiplier);
        }
    }
}
