using SEE.Core.Abstractions;

namespace SEE.Core.Maintenance
{
    /// <summary>
    /// Outcome of <see cref="MaintenanceLedger.Decide"/>: the requested action plus the resulting
    /// maintenance phase (requirements §9.5).
    /// </summary>
    public readonly struct MaintenanceDecision
    {
        /// <summary>Creates a decision.</summary>
        /// <param name="action">What the caller must do next.</param>
        /// <param name="phase">Maintenance phase after the decision.</param>
        public MaintenanceDecision(MaintenanceAction action, MaintenancePhase phase)
        {
            Action = action;
            Phase = phase;
        }

        /// <summary>What the caller must do next.</summary>
        public MaintenanceAction Action { get; }

        /// <summary>Maintenance phase after the decision.</summary>
        public MaintenancePhase Phase { get; }
    }
}
