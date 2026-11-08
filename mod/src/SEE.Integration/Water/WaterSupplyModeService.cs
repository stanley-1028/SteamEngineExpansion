using System;
using SEE.Core.Abstractions;
using SEE.Core.Logging;
using SEE.Gameplay;
using SEE.Integration.Adapters;
using SEE.Integration.Detection;
using Timberborn.SingletonSystem;

namespace SEE.Integration.Water
{
    /// <summary>
    /// Resolves the effective water-supply mode of one boiler from the stored intent plus the
    /// current Timber Pipes detection (requirements §7.7/§10.3.1, REQ-COMPAT-06, REQ-FUEL-15).
    ///
    /// The persisted value stays the player intent (P3 owns the per-entity field); this service
    /// never writes it back, so a save cannot flip-flop. A player intent that cannot run is reported
    /// with a reason and falls back to haulers — never a silent failure.
    /// </summary>
    public sealed class WaterSupplyModeService : IPostLoadableSingleton
    {
        private readonly ICompatDetector _detector;
        private readonly IBoilerQuery _query;
        private readonly ICompatOptionsSource _options;
        private readonly SEERuntime _runtime;

        /// <summary>Creates the service.</summary>
        /// <param name="detector">Session compatibility detector.</param>
        /// <param name="query">Read-only boiler query (UI contract).</param>
        /// <param name="options">Late-bound <c>compat.*</c> options.</param>
        /// <param name="runtime">Gameplay runtime for the shared throttled log.</param>
        public WaterSupplyModeService(
            ICompatDetector detector,
            IBoilerQuery query,
            ICompatOptionsSource options,
            SEERuntime runtime)
        {
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        /// <summary>Global default intent from <c>compat.waterSupplyMode</c>.</summary>
        public WaterSupplyMode ConfiguredDefault => _options.Current.WaterSupplyMode;

        /// <summary>Mode the automatic resolution currently selects (never <see cref="WaterSupplyMode.Auto"/>).</summary>
        public WaterSupplyMode SuggestedMode => Resolve(WaterSupplyMode.Auto).Effective;

        /// <summary>Resolves a stored intent against the current detection.</summary>
        /// <param name="intent">Stored intent; never modified.</param>
        /// <returns>The decision; never null.</returns>
        public WaterSupplyDecision Resolve(WaterSupplyMode intent)
        {
            ExternalModStatus timberPipes = _detector.Detect(CompatAnchors.TimberPipes);
            bool available = timberPipes.IsAvailable;
            bool degraded = timberPipes.Detected && !timberPipes.IsAvailable;
            return WaterSupplyPolicy.Resolve(intent, available, degraded);
        }

        /// <summary>Resolves the effective mode of one boiler from its stored intent.</summary>
        /// <param name="entityId">Boiler entity id.</param>
        /// <returns>The decision; the configured default/auto when the boiler is unknown.</returns>
        public WaterSupplyDecision Resolve(Guid entityId)
        {
            try
            {
                if (_query.TryGetView(entityId, out IBoilerStatusView? view) && view != null)
                {
                    return Resolve(view.WaterSupplyMode);
                }
            }
            catch (Exception exception)
            {
                TryWarn("water mode query failed (" + exception.GetType().Name + "); auto resolution used");
            }

            return Resolve(ConfiguredDefault);
        }

        /// <summary>Post-load hook: logs the global resolution once so the player can see why.</summary>
        public void PostLoad()
        {
            try
            {
                WaterSupplyDecision decision = Resolve(ConfiguredDefault);
                _runtime.Log.Compat(
                    SeeLogKeys.CompatScan,
                    "water supply: configured=" + ConfiguredDefault + " effective=" + decision.Effective
                    + " reason=" + decision.ReasonKey);
            }
            catch (Exception)
            {
                // Diagnostics only (REQ-SAFE-01).
            }
        }

        private void TryWarn(string message)
        {
            try
            {
                _runtime.Log.Warn(SeeLogKeys.CompatAdapterError, message);
            }
            catch (Exception)
            {
                // Diagnostics only.
            }
        }
    }
}
