using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Config;
using SEE.Core.Constants;
using SEE.Core.Logging;
using SEE.Core.Maintenance;
using SEE.Gameplay.Logging;
using Timberborn.SingletonSystem;

namespace SEE.Gameplay
{
    /// <summary>
    /// Game-context singleton exposing the startup-resolved configuration and the session
    /// throttled log to every gameplay component (REQ-NFR-04/09).
    ///
    /// One singleton owns the single <see cref="ThrottledLog"/> so every component shares the same
    /// per-key 30 s windows (REQ-OPS-03). <see cref="Load"/> is the game's singleton lifecycle hook
    /// (API verified by the earlier SEE investigation): the config itself was parsed once in
    /// <see cref="SeeModStarter"/> before the DI container existed, so no tick ever touches the
    /// filesystem.
    /// </summary>
    public sealed class SEERuntime : ILoadableSingleton
    {
        private SeeConfig _config = SeeConfig.Default;
        private ThrottledLog _log;
        private ISeeLog _rawLog;

        /// <summary>Creates the runtime; the real config arrives in <see cref="Load"/>.</summary>
        public SEERuntime()
        {
            _rawLog = new UnitySeeLog();
            _log = new ThrottledLog(_rawLog, SEEConst.LogThrottleSeconds);
        }

        /// <summary>Unthrottled sink used only for one-shot save/config diagnostics.</summary>
        public ISeeLog RawLog => _rawLog;

        /// <summary>Validated, immutable configuration for this session.</summary>
        public SeeConfig Config => _config;

        /// <summary>Session-wide throttled log shared by all boilers.</summary>
        public ThrottledLog Log => _log;

        /// <summary>Builds the maintenance rules of one boiler from the session config.</summary>
        /// <param name="tier">Tier id of the boiler.</param>
        /// <param name="autoEnabled">Whether automatic maintenance is enabled for this boiler.</param>
        /// <param name="materials">Materials declared by the blueprint maintenance spec.</param>
        /// <returns>Immutable rules for the ledger; the safe default on any failure.</returns>
        public MaintenanceRules RulesFor(
            BoilerTier tier,
            bool autoEnabled,
            IReadOnlyList<MaintenanceMaterial> materials)
        {
            try
            {
                return _config.RulesFor(tier, autoEnabled, materials);
            }
            catch (Exception)
            {
                // A corrupt config must not stop boilers from running (REQ-SAFE-01).
                return MaintenanceRules.Default;
            }
        }

        /// <inheritdoc />
        public void Load()
        {
            try
            {
                _config = SeeStartup.Config;
                _log = SeeStartup.Log;
                _rawLog = SeeStartup.RawLog;
                _log.Info(
                    SeeLogKeys.Startup,
                    "SEE " + SEEVersion.ModVersion + " ready; maintenance="
                    + (_config.Maintenance.Enabled ? "on" : "off")
                    + "; fuelScaling=" + _config.Balance.FuelScaling);
            }
            catch (Exception exception)
            {
                // Configuration problems degrade to compiled defaults, never break loading.
                _config = SeeConfig.Default;
                try
                {
                    _log.Error(
                        SeeLogKeys.Startup,
                        "runtime initialisation failed (" + exception.GetType().Name + "); compiled defaults are used");
                }
                catch (Exception)
                {
                    // Diagnostics only.
                }
            }
        }
    }
}