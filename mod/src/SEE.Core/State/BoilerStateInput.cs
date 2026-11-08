using SEE.Core.Abstractions;

namespace SEE.Core.State
{
    /// <summary>
    /// Everything <see cref="BoilerStateMachine"/> needs to know about one boiler for one tick.
    /// Gameplay fills it from cached component references; no game type leaks into Core
    /// (REQ-ARCH-03).
    /// </summary>
    public readonly struct BoilerStateInput
    {
        /// <summary>Creates the input for one evaluation.</summary>
        /// <param name="isPaused">True when the player paused this boiler only.</param>
        /// <param name="maintenancePhase">Persisted maintenance sub-state.</param>
        /// <param name="condition">Current condition 0..100.</param>
        /// <param name="hasFuel">True when the fuel inventory holds at least one item.</param>
        /// <param name="hasWater">True when the water tank holds at least one item.</param>
        /// <param name="networkDemandRatio">Network demand ratio 0..1 (load).</param>
        public BoilerStateInput(
            bool isPaused,
            MaintenancePhase maintenancePhase,
            float condition,
            bool hasFuel,
            bool hasWater,
            float networkDemandRatio)
        {
            IsPaused = isPaused;
            MaintenancePhase = maintenancePhase;
            Condition = condition;
            HasFuel = hasFuel;
            HasWater = hasWater;
            NetworkDemandRatio = networkDemandRatio;
        }

        /// <summary>True when the player paused this boiler only.</summary>
        public bool IsPaused { get; }

        /// <summary>Persisted maintenance sub-state.</summary>
        public MaintenancePhase MaintenancePhase { get; }

        /// <summary>Current condition 0..100.</summary>
        public float Condition { get; }

        /// <summary>True when the fuel inventory holds at least one item.</summary>
        public bool HasFuel { get; }

        /// <summary>True when the water tank holds at least one item.</summary>
        public bool HasWater { get; }

        /// <summary>Network demand ratio 0..1 (load); NaN reads as 0.</summary>
        public float NetworkDemandRatio { get; }
    }
}
