using SEE.Core.Logging;
using SEE.Integration.Detection;

namespace SEE.Integration.Patching
{
    /// <summary>
    /// Optional runtime patch host (requirements §10.4/§10.7, REQ-COMPAT-11/12).
    ///
    /// <b>v1 never opens this host.</b> The injection of <c>PowerLineSpec</c> into SEE blueprints is
    /// proven feasible without Harmony through the public
    /// <c>IBlueprintModifierProvider</c> extension point (see
    /// <see cref="PowerLineInjectionModifier"/> and <c>docs/API_FACTS_SEE.md</c> §7.3), so the
    /// patching library is neither referenced nor loaded. The host keeps the single-attempt,
    /// fail-closed, never-retried lifecycle required for the day a patching path is ever needed:
    /// <list type="bullet">
    /// <item>closed unless <c>compat.enablePowerLineInjection</c> is on;</item>
    /// <item>closed when Power Lines is absent or degraded;</item>
    /// <item>closed when a non-Harmony path is proven (the v1 state);</item>
    /// <item>one attempt per session, never retried, never throwing.</item>
    /// </list>
    /// It deliberately contains no compile-time reference to any patching library: a future host
    /// would load it reflectively inside this class only (REQ-COMPAT-12).
    /// </summary>
    public sealed class OptionalPatchHost
    {
        /// <summary>Reason while no attempt was made yet.</summary>
        public const string NotAttemptedReason = "closed:not-attempted";

        /// <summary>Reason when the config disabled optional patches.</summary>
        public const string ConfigDisabledReason = "closed:config-disabled";

        /// <summary>Reason when Power Lines is not available.</summary>
        public const string PowerLinesAbsentReason = "closed:power-lines-absent";

        /// <summary>Reason when a non-Harmony injection path is proven (v1).</summary>
        public const string NotRequiredReason = "closed:non-harmony-injection-proven";

        /// <summary>Reason when no verified injection path exists.</summary>
        public const string UnavailableReason = "closed:no-verified-injection-path";

        private bool _attempted;

        /// <summary>True only after a successful optional patch initialization; always false in v1.</summary>
        public bool Enabled { get; private set; }

        /// <summary>True once <see cref="TryEnable"/> was called (single-attempt flag).</summary>
        public bool Attempted => _attempted;

        /// <summary>Machine-readable host state for diagnostics and the compat log.</summary>
        public string State { get; private set; } = NotAttemptedReason;

        /// <summary>
        /// Attempts to open the host once (requirements §10.7: single attempt, never retried).
        /// Returns false whenever the host stays closed, which is always the case in v1.
        /// </summary>
        /// <param name="detector">Session detector; null is treated as "Power Lines absent".</param>
        /// <param name="configEnabled">Value of <c>compat.enablePowerLineInjection</c>.</param>
        /// <param name="nonHarmonyInjectionProven">True when blueprint-modifier injection was proven (v1).</param>
        /// <returns>False in every v1 path.</returns>
        public bool TryEnable(ICompatDetector? detector, bool configEnabled, bool nonHarmonyInjectionProven)
        {
            if (_attempted)
            {
                return Enabled;
            }

            _attempted = true;
            try
            {
                if (!configEnabled)
                {
                    State = ConfigDisabledReason;
                    return false;
                }

                if (detector == null || !detector.Detect(CompatAnchors.PowerLines).IsAvailable)
                {
                    State = PowerLinesAbsentReason;
                    return false;
                }

                if (nonHarmonyInjectionProven)
                {
                    State = NotRequiredReason;
                    return false;
                }

                State = UnavailableReason;
                return false;
            }
            catch (System.Exception exception)
            {
                State = "closed:init-failed:" + exception.GetType().Name;
                Enabled = false;
                return false;
            }
        }

        /// <summary>Closes the host. Symmetric with <see cref="TryEnable"/> and never throws.</summary>
        public void Disable()
        {
            Enabled = false;
            State = NotAttemptedReason;
        }

        /// <summary>Writes the current host state through the throttled log (diagnostics only).</summary>
        /// <param name="log">Session throttled log; null is tolerated.</param>
        public void Report(ThrottledLog? log)
        {
            try
            {
                log?.Compat(SeeLogKeys.CompatPatch, State);
            }
            catch (System.Exception)
            {
                // Diagnostics only.
            }
        }
    }
}
