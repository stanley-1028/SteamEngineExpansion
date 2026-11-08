using System;
using SEE.Core.Abstractions;
using SEE.Core.Maintenance;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Maintenance flow of one boiler (requirements §9.5/§9.6, REQ-MAINT-01..07): a thin gameplay
    /// wrapper around the pure Core <see cref="MaintenanceLedger"/>. Keeping it separate means the
    /// state machine stays unit-tested without the game and the controller only supplies the two
    /// environment facts (district materials, road access) plus the material withdrawal.
    /// </summary>
    public sealed class SEEMaintenanceController
    {
        private readonly MaintenanceLedger _ledger;

        /// <summary>Creates the maintenance flow for one boiler.</summary>
        /// <param name="ledger">Pure wear/maintenance ledger owned by the boiler controller.</param>
        public SEEMaintenanceController(MaintenanceLedger ledger)
        {
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        }

        /// <summary>Current maintenance phase (persisted in the per-entity save).</summary>
        public MaintenancePhase Phase => _ledger.Phase;

        /// <summary>Remaining downtime in game hours; 0 unless maintaining.</summary>
        public float RemainingHours => _ledger.RemainingHours;

        /// <summary>Condition 0..100.</summary>
        public float Condition => _ledger.Condition;

        /// <summary>True when the manual-maintenance command may start a cycle (condition &lt; 100).</summary>
        public bool IsManualAvailable => _ledger.IsManualMaintenanceAvailable;

        /// <summary>True when condition is still healthy: manual maintenance only shows the low-benefit hint.</summary>
        public bool IsManualBenefitLow => _ledger.IsManualBenefitLow;

        /// <summary>
        /// Evaluates one automatic maintenance decision. The caller supplies the environment facts
        /// so the pure ledger never touches the district inventory or the road network itself.
        /// </summary>
        /// <param name="hasMaterials">Whether the district holds every required material.</param>
        /// <param name="hasAccess">Whether a road reaches the boiler.</param>
        /// <param name="paused">True while the player paused this boiler.</param>
        /// <returns>The requested action plus the resulting phase.</returns>
        public MaintenanceDecision Evaluate(bool hasMaterials, bool hasAccess, bool paused)
        {
            return _ledger.Decide(hasMaterials, hasAccess, paused);
        }

        /// <summary>Evaluates a player-requested maintenance (requirements §9.6, C-5).</summary>
        /// <param name="hasMaterials">Whether the district holds every required material.</param>
        /// <param name="hasAccess">Whether a road reaches the boiler.</param>
        /// <returns>The requested action plus the resulting phase.</returns>
        public MaintenanceDecision RequestManual(bool hasMaterials, bool hasAccess)
        {
            return _ledger.RequestManual(hasMaterials, hasAccess);
        }

        /// <summary>Enters the downtime phase after the caller consumed the materials.</summary>
        /// <param name="durationHours">Configured downtime in game hours.</param>
        public void BeginMaintenance(float durationHours)
        {
            _ledger.BeginMaintenance(durationHours);
        }

        /// <summary>Advances the downtime timer.</summary>
        /// <param name="hours">Elapsed game hours.</param>
        public void AdvanceMaintenance(double hours)
        {
            _ledger.AdvanceMaintenance(hours);
        }

        /// <summary>Finishes maintenance and records the current game day.</summary>
        /// <param name="currentDay">Current game day.</param>
        public void CompleteMaintenance(float currentDay)
        {
            _ledger.CompleteMaintenance(currentDay);
        }
    }
}
