using SEE.Core.Abstractions;
using SEE.Integration.Detection;

namespace SEE.Integration.Adapters
{
    /// <summary>
    /// Timber Pipes soft-dependency adapter (requirements §10.2/§10.3, REQ-COMPAT-02/06).
    ///
    /// There is no blueprint injection: the boiler already declares a vanilla liquid
    /// <c>StockpileSpec</c>, so a pipe network (tank mode) or a one-way valve (valve mode) can fill
    /// it, and haulers always work (hauler mode). The adapter only owns detection and the water-mode
    /// policy; it never references the Timber Pipes assembly and never turns the pipe mod into a hard
    /// dependency (REQ-COMPAT-08).
    /// </summary>
    public sealed class TimberPipesAdapter : ExternalAdapterBase, IWaterSupplyAdapter
    {
        /// <summary>Creates the adapter; detection is injected so one scan serves all adapters.</summary>
        /// <param name="detector">Session compatibility detector.</param>
        public TimberPipesAdapter(ICompatDetector detector)
            : base(detector, CompatAnchors.TimberPipesAdapterId, CompatAnchors.TimberPipes)
        {
        }

        /// <summary>True when the tank mode may be offered (both V-01/V-TP2 manual gates verified).</summary>
        public bool TankModeAvailable => State == AdapterState.Enabled && WaterSupplyPolicy.TankModeAvailable;

        /// <summary>True when the valve mode may be offered.</summary>
        public bool ValveModeAvailable => State == AdapterState.Enabled;

        /// <summary>Mode the automatic resolution currently selects.</summary>
        public WaterSupplyMode SuggestedMode => Resolve(WaterSupplyMode.Auto).Effective;

        /// <summary>Reason key of <see cref="SuggestedMode"/>.</summary>
        public string SuggestedModeReason => Resolve(WaterSupplyMode.Auto).ReasonKey;

        /// <summary>True when the panel may show the m³ conversion row (capacity display only).</summary>
        public bool VolumeConversionAvailable => State == AdapterState.Enabled;

        /// <inheritdoc />
        public WaterSupplyDecision Resolve(WaterSupplyMode intent)
        {
            bool available = State == AdapterState.Enabled;
            bool degraded = State == AdapterState.Failed || (Status.Detected && !Status.IsAvailable);
            return WaterSupplyPolicy.Resolve(intent, available, degraded);
        }

        /// <inheritdoc />
        protected override void OnAvailable()
        {
            Enable("timber-pipes-detected");
        }
    }
}
