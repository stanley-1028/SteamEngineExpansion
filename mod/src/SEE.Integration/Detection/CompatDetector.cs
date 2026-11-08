using System;
using System.Collections.Generic;
using System.Diagnostics;
using SEE.Core.Constants;
using SEE.Core.Logging;
using SEE.Gameplay;
using Timberborn.SingletonSystem;

namespace SEE.Integration.Detection
{
    /// <summary>
    /// Three-stage compatibility detector (requirements §10.6, REQ-COMPAT-09/10). The pure engine is
    /// <see cref="CompatDetectionSession"/>; this singleton only supplies the real
    /// <see cref="AppDomainCompatProbe"/>, the configured budget and the one throttled
    /// <c>SEE/Compat</c> summary line. One world load runs the scan exactly once and caches every
    /// result, including <see cref="Availability.Degraded"/>.
    /// </summary>
    public sealed class CompatDetector : ICompatDetector, ILoadableSingleton
    {
        private readonly SEERuntime _runtime;
        private readonly CompatDetectionSession _session;
        private bool _summaryLogged;

        /// <summary>Creates the detector with the real AppDomain probe and a monotonic clock.</summary>
        /// <param name="runtime">Gameplay runtime (config and the shared throttled log).</param>
        public CompatDetector(SEERuntime runtime)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _session = new CompatDetectionSession(new AppDomainCompatProbe(), SEEConst.DetectBudgetSeconds, MonotonicSeconds);
        }

        /// <summary>True once the single startup scan finished (cached results are authoritative).</summary>
        public bool Scanned => _session.Scanned;

        /// <summary>Runs the single startup scan and writes one throttled <c>SEE/Compat</c> summary.</summary>
        public void Load()
        {
            try
            {
                _session.SetBudget(ConfiguredBudget());
                DetectAll();
            }
            catch (Exception exception)
            {
                // Last line of defence: detection failure only degrades optional features.
                TryLog("compatibility scan threw " + exception.GetType().Name + "; optional adapters disabled");
            }
        }

        /// <inheritdoc />
        public ExternalModStatus Detect(string modId)
        {
            try
            {
                return _session.Detect(modId);
            }
            catch (Exception exception)
            {
                return ExternalModStatus.Degraded(
                    modId ?? string.Empty, null, CompatDetectionSession.ProbeExceptionDetail + ":" + exception.GetType().Name);
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<ExternalModStatus> DetectAll()
        {
            try
            {
                IReadOnlyList<ExternalModStatus> statuses = _session.DetectAll();
                if (!_summaryLogged)
                {
                    _summaryLogged = true;
                    TryLog(_session.BuildSummary());
                }

                return statuses;
            }
            catch (Exception exception)
            {
                TryLog("compatibility scan threw " + exception.GetType().Name);
                return new[]
                {
                    ExternalModStatus.Absent(CompatAnchors.TimberPipes, "scan-failed"),
                    ExternalModStatus.Absent(CompatAnchors.PowerLines, "scan-failed"),
                    ExternalModStatus.Absent(CompatAnchors.ConveyorBelt, "scan-failed")
                };
            }
        }

        /// <inheritdoc />
        public ExternalModStatus DetectLegacyPowerLines()
        {
            try
            {
                return _session.DetectLegacyPowerLines();
            }
            catch (Exception exception)
            {
                return ExternalModStatus.Absent(
                    CompatAnchors.LegacyPowerLines, "scan-failed:" + exception.GetType().Name);
            }
        }

        /// <inheritdoc />
        public void Refresh()
        {
            try
            {
                _session.Reset();
                _summaryLogged = false;
            }
            catch (Exception)
            {
                // Manual re-detect is a convenience; it must never fail the caller.
            }
        }

        private double ConfiguredBudget()
        {
            try
            {
                return _runtime.Config.Compat.DetectionBudgetSeconds;
            }
            catch (Exception)
            {
                return SEEConst.DetectBudgetSeconds;
            }
        }

        private void TryLog(string message)
        {
            try
            {
                _runtime.Log.Compat(SeeLogKeys.CompatScan, message);
            }
            catch (Exception)
            {
                // Diagnostics only: logging must not break the mod (REQ-SAFE-01).
            }
        }

        private static double MonotonicSeconds()
        {
            return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        }
    }
}
