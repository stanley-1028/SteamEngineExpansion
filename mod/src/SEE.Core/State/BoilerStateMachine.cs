using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Formulas;

namespace SEE.Core.State
{
    /// <summary>
    /// Pure, deterministic status resolver for one boiler (requirements §3.3/§15.3, Lead ruling
    /// AD-12; §9.7 pseudocode is superseded where it conflicts with the main-state table).
    ///
    /// The evaluation order is frozen and must not be rewritten, because it defines which single
    /// badge the player sees:
    /// <code>
    /// 1. IsPaused                              -> Paused
    /// 2. MaintenancePhase == Maintaining       -> Maintaining        (level-0 override)
    /// 3. !HasFuel                              -> NoFuel
    /// 4. !HasWater                             -> NoWater
    /// 5. Condition &lt;= 0                        -> NeedsMaintenance
    /// 6. load = clamp(demand, 0, 1); load &lt;= 0 -> Idle               (only applicable at λ &lt;= 0)
    /// 7. condition band: &lt;= 20 Critical, &lt;= 40 Worn, &lt;= 60 MaintenanceDue, else Running
    ///    (these four only apply while λ &gt; 0)
    /// </code>
    /// The nine main states exclude <see cref="BoilerStatus.Maintaining"/>; that state is derived
    /// from <see cref="MaintenancePhase.Maintaining"/> and only overrides NoFuel/NoWater/
    /// NeedsMaintenance. Behaviour gate: the six blocking states (Paused, Maintaining, NoFuel,
    /// NoWater, NeedsMaintenance, Idle) produce 0, consume nothing and wear nothing; the four
    /// remaining states produce <c>ceil(rated × conditionMultiplier)</c> and wear while loaded
    /// (requirements §9.1/§9.7).
    /// </summary>
    public static class BoilerStateMachine
    {
        /// <summary>Resolves the single highest-priority state of one boiler.</summary>
        /// <param name="input">Cached observations for this tick.</param>
        /// <returns>The winning state; never throws and never returns an undefined value.</returns>
        public static BoilerStatus Evaluate(in BoilerStateInput input)
        {
            if (input.IsPaused)
            {
                return BoilerStatus.Paused;
            }

            if (input.MaintenancePhase == MaintenancePhase.Maintaining)
            {
                return BoilerStatus.Maintaining;
            }

            if (!input.HasFuel)
            {
                return BoilerStatus.NoFuel;
            }

            if (!input.HasWater)
            {
                return BoilerStatus.NoWater;
            }

            if (float.IsNaN(input.Condition) || input.Condition <= 0f)
            {
                return BoilerStatus.NeedsMaintenance;
            }

            float load = input.NetworkDemandRatio;
            if (float.IsNaN(load) || load <= 0f)
            {
                // No network demand: the wear bands are not applicable (they need λ > 0), so the
                // boiler is Idle even at a low condition.
                return BoilerStatus.Idle;
            }

            return ResolveConditionState(input.Condition);
        }

        /// <summary>Maps a positive condition to the wear band state (requirements §9.3).</summary>
        /// <param name="condition">Condition greater than 0.</param>
        /// <returns>Critical, Worn, MaintenanceDue or Running.</returns>
        public static BoilerStatus ResolveConditionState(float condition)
        {
            if (condition <= 20f)
            {
                return BoilerStatus.Critical;
            }

            if (condition <= 40f)
            {
                return BoilerStatus.Worn;
            }

            return condition <= 60f ? BoilerStatus.MaintenanceDue : BoilerStatus.Running;
        }

        /// <summary>
        /// True when the state still produces power (and therefore consumes fuel/water and wears):
        /// Running plus the three condition warning bands. Idle, Paused, NoFuel, NoWater,
        /// NeedsMaintenance and Maintaining produce nothing (AD-12 behaviour gate).
        /// </summary>
        /// <param name="status">State resolved by <see cref="Evaluate"/>.</param>
        /// <returns>True for Running/Critical/Worn/MaintenanceDue.</returns>
        public static bool Produces(BoilerStatus status)
        {
            return status == BoilerStatus.Running
                || status == BoilerStatus.Critical
                || status == BoilerStatus.Worn
                || status == BoilerStatus.MaintenanceDue;
        }

        /// <summary>
        /// True when the boiler carries load (λ &gt; 0): wear and fuel/water consumption are only
        /// allowed in a producing state with a positive load.
        /// </summary>
        /// <param name="status">State resolved by <see cref="Evaluate"/>.</param>
        /// <param name="loadRatio">Network demand ratio 0..1.</param>
        /// <returns>True when the state produces and the load is positive.</returns>
        public static bool IsLoaded(BoilerStatus status, float loadRatio)
        {
            return Produces(status) && !float.IsNaN(loadRatio) && loadRatio > 0f;
        }

        /// <summary>
        /// Resolves one boiler into the full UI contract: main state, badge key, cause, effective
        /// output multiplier and network load λ (requirements §3.3/§15.3, REQ-CORE-06/11).
        /// </summary>
        /// <param name="input">Cached observations for this tick.</param>
        /// <returns>The resolution; never throws.</returns>
        public static BoilerStateResolution Resolve(in BoilerStateInput input)
        {
            BoilerStatus status = Evaluate(in input);
            float load = BoilerFormulas.Clamp01(input.NetworkDemandRatio);
            float outputMultiplier = 0f;
            if (Produces(status))
            {
                float conditionMultiplier = ConditionCurve.Multiplier(input.Condition);
                outputMultiplier = BoilerFormulas.Clamp01(load * conditionMultiplier);
            }

            return new BoilerStateResolution(status, BadgeKey(status), ReasonFor(status), outputMultiplier, load);
        }

        /// <summary>Frozen <c>Status.SEE.*</c> badge localization key of one state (CONTRACT_SEE §5.2).</summary>
        /// <param name="status">Resolved state.</param>
        /// <returns>The badge key; <c>Status.SEE.Running</c> for an undefined enum value.</returns>
        public static string BadgeKey(BoilerStatus status)
        {
            switch (status)
            {
                case BoilerStatus.Paused: return SeeLocKeys.StatusPaused;
                case BoilerStatus.Maintaining: return SeeLocKeys.StatusMaintaining;
                case BoilerStatus.NoFuel: return SeeLocKeys.StatusNoFuel;
                case BoilerStatus.NoWater: return SeeLocKeys.StatusNoWater;
                case BoilerStatus.NeedsMaintenance: return SeeLocKeys.StatusNeedsMaintenance;
                case BoilerStatus.Critical: return SeeLocKeys.StatusCritical;
                case BoilerStatus.Worn: return SeeLocKeys.StatusWorn;
                case BoilerStatus.MaintenanceDue: return SeeLocKeys.StatusMaintenanceDue;
                case BoilerStatus.Idle: return SeeLocKeys.StatusIdle;
                default: return SeeLocKeys.StatusRunning;
            }
        }

        /// <summary>Machine-readable cause of one state (REQ-CORE-11).</summary>
        /// <param name="status">Resolved state.</param>
        /// <returns>The cause; <see cref="BoilerStatusReason.Loaded"/> for an undefined enum value.</returns>
        public static BoilerStatusReason ReasonFor(BoilerStatus status)
        {
            switch (status)
            {
                case BoilerStatus.Paused: return BoilerStatusReason.PlayerPaused;
                case BoilerStatus.Maintaining: return BoilerStatusReason.MaintenanceRunning;
                case BoilerStatus.NoFuel: return BoilerStatusReason.FuelEmpty;
                case BoilerStatus.NoWater: return BoilerStatusReason.WaterEmpty;
                case BoilerStatus.NeedsMaintenance: return BoilerStatusReason.ConditionZero;
                case BoilerStatus.Critical: return BoilerStatusReason.ConditionCritical;
                case BoilerStatus.Worn: return BoilerStatusReason.ConditionWorn;
                case BoilerStatus.MaintenanceDue: return BoilerStatusReason.MaintenanceDue;
                case BoilerStatus.Idle: return BoilerStatusReason.NoNetworkDemand;
                default: return BoilerStatusReason.Loaded;
            }
        }
    }
}
