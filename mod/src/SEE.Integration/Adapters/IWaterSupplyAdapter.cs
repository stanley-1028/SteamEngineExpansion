using SEE.Core.Abstractions;
using SEE.Integration.Detection;

namespace SEE.Integration.Adapters
{
    /// <summary>
    /// Capability interface of the Timber Pipes adapter (CONTRACT_SEE §2.4, requirements §10.3.3).
    /// It exposes detection, the suggested water mode and the effective-mode resolution. SEE never
    /// injects external blueprints and never replaces the vanilla liquid <c>StockpileSpec</c>
    /// (REQ-COMPAT-02/07); the adapter only decides what the panel may show and which mode runs.
    /// </summary>
    public interface IWaterSupplyAdapter : IExternalAdapter
    {
        /// <summary>True when the tank mode may be offered (both V-01/V-TP2 manual gates verified).</summary>
        bool TankModeAvailable { get; }

        /// <summary>True when the valve mode may be offered (Timber Pipes available).</summary>
        bool ValveModeAvailable { get; }

        /// <summary>Mode the automatic resolution currently selects (never <see cref="WaterSupplyMode.Auto"/>).</summary>
        WaterSupplyMode SuggestedMode { get; }

        /// <summary>Reason key of <see cref="SuggestedMode"/>.</summary>
        string SuggestedModeReason { get; }

        /// <summary>Resolves the effective mode for one stored intent (requirements §7.7).</summary>
        /// <param name="intent">Stored intent; never modified by this call.</param>
        /// <returns>The decision; never null.</returns>
        WaterSupplyDecision Resolve(WaterSupplyMode intent);
    }
}
