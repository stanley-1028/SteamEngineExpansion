using System;
using SEE.Core.Abstractions;
using SEE.Core.Logging;
using SEE.Core.Save;
using SEE.Gameplay;
using SEE.Integration.Adapters;
using SEE.Integration.Detection;
using SEE.Integration.Patching;
using Timberborn.SingletonSystem;

namespace SEE.Integration.Save
{
    /// <summary>
    /// Wires the compatibility result into the world save node and the panel-facing water mode
    /// (requirements §14.6/§10.9, REQ-COMPAT-13/14, REQ-SAVE-08).
    ///
    /// Runs in the post-load phase so <see cref="SeeSaveBridge.Load"/> (load phase) has already read
    /// the saved node; the current detection then overwrites the diagnostic <c>compat</c> arrays
    /// before any save can happen. Removing/adding an optional mod therefore always rewrites the
    /// block from live detection, never from stale saved data.
    /// </summary>
    public sealed class CompatSaveIntegrator : IPostLoadableSingleton
    {
        private readonly ICompatDetector _detector;
        private readonly TimberPipesAdapter _timberPipes;
        private readonly PowerLinesAdapter _powerLines;
        private readonly ConveyorBeltAdapter _conveyorBelts;
        private readonly SeeSaveBridge _saveBridge;
        private readonly OptionalPatchHost _patchHost;
        private readonly ICompatOptionsSource _options;
        private readonly SEERuntime _runtime;
        private bool _initialized;

        /// <summary>Creates the integrator with every adapter and the save bridge.</summary>
        /// <param name="detector">Session compatibility detector.</param>
        /// <param name="timberPipes">Timber Pipes adapter.</param>
        /// <param name="powerLines">Power Lines adapter.</param>
        /// <param name="conveyorBelts">Conveyor Belts adapter.</param>
        /// <param name="saveBridge">Gameplay world-node save bridge.</param>
        /// <param name="patchHost">Optional patch host (stays closed in v1).</param>
        /// <param name="options">Late-bound <c>compat.*</c> options.</param>
        /// <param name="runtime">Gameplay runtime for the shared throttled log.</param>
        public CompatSaveIntegrator(
            ICompatDetector detector,
            TimberPipesAdapter timberPipes,
            PowerLinesAdapter powerLines,
            ConveyorBeltAdapter conveyorBelts,
            SeeSaveBridge saveBridge,
            OptionalPatchHost patchHost,
            ICompatOptionsSource options,
            SEERuntime runtime)
        {
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
            _timberPipes = timberPipes ?? throw new ArgumentNullException(nameof(timberPipes));
            _powerLines = powerLines ?? throw new ArgumentNullException(nameof(powerLines));
            _conveyorBelts = conveyorBelts ?? throw new ArgumentNullException(nameof(conveyorBelts));
            _saveBridge = saveBridge ?? throw new ArgumentNullException(nameof(saveBridge));
            _patchHost = patchHost ?? throw new ArgumentNullException(nameof(patchHost));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        /// <summary>True once a detection pass was published.</summary>
        public bool Initialized => _initialized;

        /// <summary>True when the unsupported legacy Power Lines assembly was seen (report only).</summary>
        public bool LegacyPowerLinesDetected { get; private set; }

        /// <summary>Suggested water mode from the current detection (never Auto).</summary>
        public WaterSupplyMode SuggestedWaterMode => _timberPipes.SuggestedMode;

        /// <summary>Summary line of the last publish (diagnostics).</summary>
        public string LastSummary { get; private set; } = "not-initialized";

        /// <summary>Post-load hook: detects, initializes the adapters and publishes the snapshot.</summary>
        public void PostLoad()
        {
            Refresh();
        }

        /// <summary>
        /// Runs detection, initializes every adapter once, refreshes the save snapshot and attempts
        /// the (closed-by-design) optional patch host. Never throws.
        /// </summary>
        public void Refresh()
        {
            try
            {
                _detector.DetectAll();
                _timberPipes.Initialize();
                _powerLines.Initialize();
                _conveyorBelts.Initialize();

                var adapters = new IExternalAdapter[] { _timberPipes, _powerLines, _conveyorBelts };
                SeeCompatSnapshot snapshot = CompatSnapshotBuilder.Build(
                    new[] { _timberPipes.Status, _powerLines.Status, _conveyorBelts.Status }, adapters);
                _saveBridge.RecordCompatSnapshot(snapshot.Detected, snapshot.Enabled, snapshot.Failed);

                ExternalModStatus legacy = _detector.DetectLegacyPowerLines();
                LegacyPowerLinesDetected = legacy.Detected;

                CompatAdapterOptions options = _options.Current;
                _patchHost.TryEnable(_detector, options.EnablePowerLineInjection, nonHarmonyInjectionProven: true);

                _initialized = true;
                LastSummary = Summarize(adapters, legacy);
                LogOnce();
            }
            catch (Exception exception)
            {
                LastSummary = "publish-failed:" + exception.GetType().Name;
            }
        }

        private string Summarize(IExternalAdapter[] adapters, ExternalModStatus legacy)
        {
            var builder = new System.Text.StringBuilder(96);
            builder.Append("compat publish:");
            for (int i = 0; i < adapters.Length; i++)
            {
                builder.Append(' ');
                builder.Append(adapters[i].ModId);
                builder.Append('=');
                builder.Append(adapters[i].State);
            }

            if (legacy.Detected)
            {
                builder.Append(" legacy-power-lines=detected(report-only)");
            }

            builder.Append(" water=");
            builder.Append(_timberPipes.SuggestedMode);
            builder.Append(" patch-host=");
            builder.Append(_patchHost.State);
            return builder.ToString();
        }

        private void LogOnce()
        {
            try
            {
                _runtime.Log.Compat(SeeLogKeys.CompatScan, LastSummary);
            }
            catch (Exception)
            {
                // Diagnostics only (REQ-SAFE-01).
            }
        }
    }
}
