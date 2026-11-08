using SEE.Core.Abstractions;

namespace SEE.Core.Config
{
    /// <summary>
    /// Resolved <c>compat.*</c> configuration (requirements §9.3/§10.6/§10.9): detection budget,
    /// optional Power Lines injection, belt fuel mode, water-supply intent and log throttling.
    /// </summary>
    public sealed class CompatConfig
    {
        internal CompatConfig(
            WaterSupplyMode waterSupplyMode,
            float detectionBudgetSeconds,
            bool enablePowerLineInjection,
            BeltFuelMode beltFuelMode,
            float logThrottleSeconds,
            bool pipePollutionWarn)
        {
            WaterSupplyMode = waterSupplyMode;
            DetectionBudgetSeconds = detectionBudgetSeconds;
            EnablePowerLineInjection = enablePowerLineInjection;
            BeltFuelMode = beltFuelMode;
            LogThrottleSeconds = logThrottleSeconds;
            PipePollutionWarn = pipePollutionWarn;
        }

        /// <summary>Player intent for the water supply mode (auto by default).</summary>
        public WaterSupplyMode WaterSupplyMode { get; }

        /// <summary>Total budget of one compatibility scan, in seconds (2, range 1..5).</summary>
        public float DetectionBudgetSeconds { get; }

        /// <summary>Whether the Power Lines adapter may inject a connection point (default on, fails safe).</summary>
        public bool EnablePowerLineInjection { get; }

        /// <summary>Whether conveyor belts are preferred for fuel delivery (auto by default).</summary>
        public BeltFuelMode BeltFuelMode { get; }

        /// <summary>Per-key log throttle window in seconds (30, range 10..120).</summary>
        public float LogThrottleSeconds { get; }

        /// <summary>Whether the panel shows the pipe-contamination hint.</summary>
        public bool PipePollutionWarn { get; }
    }
}
