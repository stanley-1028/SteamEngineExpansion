using System;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Formulas;

namespace SEE.Core.Maintenance
{
    /// <summary>
    /// Pure wear and maintenance state machine for one boiler (requirements §9.2/§9.5/§9.7/§9.8).
    /// It owns the condition, the per-boiler auto flag, the persisted maintenance phase, the
    /// downtime timer and the last maintenance day; it never touches a game object, so the whole
    /// flow is deterministic and unit-testable (REQ-MAINT-01..07).
    ///
    /// Invariants:
    /// <list type="bullet">
    /// <item>condition is always finite and inside 0..100;</item>
    /// <item>remaining hours are always inside the closed interval [0, 2] and are 0 unless the
    /// phase is <see cref="MaintenancePhase.Maintaining"/> (D-7);</item>
    /// <item>wear only accumulates while the boiler carries a load; an idle boiler never wears;</item>
    /// <item>disabling maintenance resets to a fully maintained state instead of leaving a half
    /// applied one (REQ-MAINT-13).</item>
    /// </list>
    /// </summary>
    public sealed class MaintenanceLedger
    {
        private float _condition = SEEConst.MaxCondition;
        private float _remainingHours;
        private float _lastMaintenanceDay;

        /// <summary>Current condition, 0..100 (REQ-MAINT-01).</summary>
        public float Condition => _condition;

        /// <summary>Per-boiler automatic-maintenance flag; persisted in the save file (REQ-MAINT-05).</summary>
        public bool AutoMaintenance { get; set; } = true;

        /// <summary>Current maintenance sub-state (persisted, requirements §9.8).</summary>
        public MaintenancePhase Phase { get; private set; } = MaintenancePhase.None;

        /// <summary>Remaining downtime in game hours; 0 unless <see cref="Phase"/> is Maintaining.</summary>
        public float RemainingHours => _remainingHours;

        /// <summary>Game day of the last completed maintenance; 0 when never maintained.</summary>
        public float LastMaintenanceDay => _lastMaintenanceDay;

        /// <summary>Current rules; the controller refreshes them whenever the spec/config changes.</summary>
        public MaintenanceRules Rules { get; set; } = MaintenanceRules.Default;

        /// <summary>
        /// Restores the ledger from save data (REQ-MAINT-11). Invalid values fall back to safe
        /// defaults instead of throwing; downtime is only kept while actually maintaining.
        /// </summary>
        /// <param name="condition">Stored condition, normally 0..100; NaN becomes 100.</param>
        /// <param name="autoMaintenance">Stored automatic-maintenance flag.</param>
        /// <param name="phase">Stored maintenance phase; undefined values become None.</param>
        /// <param name="remainingHours">Stored remaining downtime in game hours.</param>
        /// <param name="lastMaintenanceDay">Stored game day of the last completed maintenance.</param>
        public void Reset(
            float condition,
            bool autoMaintenance,
            MaintenancePhase phase,
            float remainingHours,
            float lastMaintenanceDay)
        {
            _condition = SanitizeCondition(condition);
            AutoMaintenance = autoMaintenance;
            Phase = IsDefined(phase) ? phase : MaintenancePhase.None;
            _remainingHours = Phase == MaintenancePhase.Maintaining ? SanitizeRemaining(remainingHours) : 0f;
            _lastMaintenanceDay = SanitizeDay(lastMaintenanceDay);
        }

        /// <summary>
        /// Applies wear for loaded running hours only (REQ-MAINT-01): the caller passes the actual
        /// load ratio, and a zero/NaN load is a no-op. The formula is
        /// <c>50 / (intervalDays * 24) * hours * (0.4 + 0.6 * load)</c> (requirements §9.2).
        /// </summary>
        /// <param name="hours">Game hours spent loaded since the previous call.</param>
        /// <param name="intervalDays">Configured maintenance interval in running days.</param>
        /// <param name="loadRatio">Actual output divided by rated horsepower, 0..1.</param>
        public void AccumulateWear(double hours, double intervalDays, float loadRatio)
        {
            if (Phase == MaintenancePhase.Maintaining)
            {
                return;
            }

            if (double.IsNaN(hours) || double.IsInfinity(hours) || hours <= 0.0)
            {
                return;
            }

            float load = BoilerFormulas.Clamp01(loadRatio);
            if (load <= 0f)
            {
                return;
            }

            double wear = BoilerFormulas.WearPerRunningHour(intervalDays) * hours * BoilerFormulas.LoadFactor(load);
            if (double.IsNaN(wear) || double.IsInfinity(wear) || wear <= 0.0)
            {
                return;
            }

            _condition = (float)BalanceWear(_condition - wear);
        }

        /// <summary>
        /// Decides this tick's maintenance action and updates <see cref="Phase"/> accordingly
        /// (requirements §9.5). Automatic maintenance triggers at or below the rules threshold while
        /// the per-boiler flag is on; a blocked request stays pending until the material/road
        /// problem is fixed, and a paused boiler never starts a new maintenance.
        /// </summary>
        /// <param name="hasMaterials">Whether the district currently holds every required material.</param>
        /// <param name="hasAccess">Whether a road reaches the boiler's pickup point.</param>
        /// <param name="paused">True while the player paused this boiler.</param>
        /// <returns>The action for the gameplay layer plus the resulting phase.</returns>
        public MaintenanceDecision Decide(bool hasMaterials, bool hasAccess, bool paused)
        {
            if (!Rules.Enabled)
            {
                DisableMaintenance();
                return new MaintenanceDecision(MaintenanceAction.None, Phase);
            }

            if (Phase == MaintenancePhase.Maintaining)
            {
                MaintenanceAction action = _remainingHours <= 0f
                    ? MaintenanceAction.Completed
                    : MaintenanceAction.None;
                return new MaintenanceDecision(action, Phase);
            }

            if (paused)
            {
                return new MaintenanceDecision(MaintenanceAction.None, Phase);
            }

            bool pending = Phase == MaintenancePhase.MissingMaterials || Phase == MaintenancePhase.NoAccess;
            float threshold = SanitizeThreshold(Rules.AutoThreshold);
            bool wanted = pending
                || (Rules.Auto && AutoMaintenance && _condition <= threshold);
            if (!wanted)
            {
                Phase = MaintenancePhase.None;
                return new MaintenanceDecision(MaintenanceAction.None, Phase);
            }

            return StartOrBlock(hasMaterials, hasAccess);
        }

        /// <summary>
        /// True when a manual maintenance request may start right now (requirements §9.6, C-5): the
        /// system is enabled and the boiler is not already fully maintained. A condition above
        /// <see cref="MaintenanceRules.ManualWarnAbove"/> stays allowed; it only produces the
        /// <see cref="IsManualBenefitLow"/> hint.
        /// </summary>
        public bool IsManualMaintenanceAvailable => Rules.Enabled && _condition < SEEConst.MaxCondition;

        /// <summary>
        /// True when the condition is above the manual-warn threshold, so the UI shows "still in
        /// good condition, maintenance benefit is low" — a hint only, never a disable (REQ-MAINT-05,
        /// config <c>maintenance.manualWarnAbove</c> default 90).
        /// </summary>
        public bool IsManualBenefitLow =>
            IsManualMaintenanceAvailable && _condition > SanitizeThreshold(Rules.ManualWarnAbove);

        /// <summary>
        /// Starts a player-requested maintenance (requirements §9.6). At condition 100 the request
        /// is a no-op (the only disabled case, C-5); blocked materials/road keep the request pending
        /// and consume nothing (REQ-MAINT-11/12). The caller performs the atomic district
        /// withdrawal only when <see cref="MaintenanceAction.Start"/> is returned.
        /// </summary>
        /// <param name="hasMaterials">Whether the district currently holds every required material.</param>
        /// <param name="hasAccess">Whether a road reaches the boiler's pickup point.</param>
        /// <returns>The action for the gameplay layer plus the resulting phase.</returns>
        public MaintenanceDecision RequestManual(bool hasMaterials, bool hasAccess)
        {
            if (!Rules.Enabled)
            {
                DisableMaintenance();
                return new MaintenanceDecision(MaintenanceAction.None, Phase);
            }

            if (Phase == MaintenancePhase.Maintaining || _condition >= SEEConst.MaxCondition)
            {
                return new MaintenanceDecision(MaintenanceAction.None, Phase);
            }

            return StartOrBlock(hasMaterials, hasAccess);
        }

        /// <summary>
        /// Enters the downtime phase. The caller must consume the materials first (REQ-MAINT-04);
        /// repeated calls while already maintaining keep the running timer.
        /// </summary>
        /// <param name="durationHours">Configured downtime in game hours (clamped to the save contract).</param>
        public void BeginMaintenance(float durationHours)
        {
            if (!Rules.Enabled || Phase == MaintenancePhase.Maintaining)
            {
                return;
            }

            Phase = MaintenancePhase.Maintaining;
            _remainingHours = SanitizeRemaining(durationHours);
        }

        /// <summary>Advances the downtime timer; never below zero (REQ-MAINT-04).</summary>
        /// <param name="hours">Elapsed game hours since the previous advance.</param>
        public void AdvanceMaintenance(double hours)
        {
            if (Phase != MaintenancePhase.Maintaining)
            {
                return;
            }

            if (double.IsNaN(hours) || double.IsInfinity(hours) || hours <= 0.0)
            {
                return;
            }

            double remaining = _remainingHours - hours;
            _remainingHours = remaining <= 0.0 ? 0f : (remaining > SEEConst.MaxMaintenanceRemainingHours
                ? SEEConst.MaxMaintenanceRemainingHours
                : (float)remaining);
        }

        /// <summary>Finishes maintenance: condition returns to 100 and the phase to None (REQ-MAINT-04).</summary>
        public void CompleteMaintenance()
        {
            _condition = SEEConst.MaxCondition;
            Phase = MaintenancePhase.None;
            _remainingHours = 0f;
        }

        /// <summary>Finishes maintenance and records the current game day (requirements §9.8).</summary>
        /// <param name="currentDay">Current game day, used for the last-maintenance field.</param>
        public void CompleteMaintenance(float currentDay)
        {
            CompleteMaintenance();
            _lastMaintenanceDay = SanitizeDay(currentDay);
        }

        /// <summary>REQ-MAINT-13: disabling maintenance must not leave a half-applied state.</summary>
        private void DisableMaintenance()
        {
            _condition = SEEConst.MaxCondition;
            Phase = MaintenancePhase.None;
            _remainingHours = 0f;
        }

        /// <summary>Starts maintenance, or reports why the materials/road are missing.</summary>
        private MaintenanceDecision StartOrBlock(bool hasMaterials, bool hasAccess)
        {
            if (!hasMaterials)
            {
                Phase = MaintenancePhase.MissingMaterials;
                return new MaintenanceDecision(MaintenanceAction.BlockedByMaterials, Phase);
            }

            if (!hasAccess)
            {
                Phase = MaintenancePhase.NoAccess;
                return new MaintenanceDecision(MaintenanceAction.BlockedByAccess, Phase);
            }

            Phase = MaintenancePhase.None;
            return new MaintenanceDecision(MaintenanceAction.Start, Phase);
        }

        private static float SanitizeCondition(float condition)
        {
            if (float.IsNaN(condition))
            {
                return SEEConst.MaxCondition;
            }

            if (condition < 0f)
            {
                return 0f;
            }

            return condition > SEEConst.MaxCondition ? SEEConst.MaxCondition : condition;
        }

        private static float SanitizeRemaining(float remainingHours)
        {
            if (float.IsNaN(remainingHours) || remainingHours < 0f)
            {
                return 0f;
            }

            return remainingHours > SEEConst.MaxMaintenanceRemainingHours
                ? SEEConst.MaxMaintenanceRemainingHours
                : remainingHours;
        }

        private static float SanitizeThreshold(float threshold)
        {
            if (float.IsNaN(threshold) || threshold < 0f)
            {
                return SEEConst.DefaultMaintenanceThreshold;
            }

            return threshold > SEEConst.MaxCondition ? SEEConst.MaxCondition : threshold;
        }

        private static float SanitizeDay(float day)
        {
            return float.IsNaN(day) || float.IsInfinity(day) || day < 0f ? 0f : day;
        }

        private static double BalanceWear(double condition)
        {
            if (double.IsNaN(condition) || condition < 0.0)
            {
                return 0.0;
            }

            return condition > SEEConst.MaxCondition ? SEEConst.MaxCondition : condition;
        }

        private static bool IsDefined(MaintenancePhase phase)
        {
            return phase == MaintenancePhase.None
                || phase == MaintenancePhase.Maintaining
                || phase == MaintenancePhase.MissingMaterials
                || phase == MaintenancePhase.NoAccess;
        }
    }
}