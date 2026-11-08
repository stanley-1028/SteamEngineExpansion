using System;
using System.Collections.Generic;
using System.Text;

namespace SEE.Integration.Detection
{
    /// <summary>
    /// Pure session-wide detection engine (requirements §10.6, REQ-COMPAT-09): runs every probe at
    /// most once, caches the result (including <see cref="Availability.Degraded"/>), enforces the
    /// total budget and applies the version gate. No game types and no reflection live here, so the
    /// offline tests can drive the whole matrix with an injected fake probe (CONTRACT_SEE §1.4).
    /// </summary>
    public sealed class CompatDetectionSession
    {
        /// <summary>Detail key of a probe that had to be skipped because the budget was spent.</summary>
        public const string BudgetExceededDetail = "budget-exceeded";

        /// <summary>Detail key of a probe that threw and was contained.</summary>
        public const string ProbeExceptionDetail = "probe-exception";

        private readonly ICompatProbe _probe;
        private readonly Dictionary<string, ExternalModStatus> _cache =
            new Dictionary<string, ExternalModStatus>(StringComparer.Ordinal);
        private readonly List<ExternalModStatus> _order = new List<ExternalModStatus>(4);
        private readonly Func<double> _clock;
        private double _budgetSeconds;
        private double _startedAt;
        private bool _scanned;

        /// <summary>Creates the session.</summary>
        /// <param name="probe">Probe implementation (real AppDomain probe in game, fake in tests).</param>
        /// <param name="budgetSeconds">Total scan budget in seconds; values &lt;= 0 disable the budget.</param>
        /// <param name="clock">Monotonic clock in seconds; defaults to a zero clock (tests) unless set.</param>
        public CompatDetectionSession(ICompatProbe probe, double budgetSeconds, Func<double>? clock = null)
        {
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
            _budgetSeconds = NormalizeBudget(budgetSeconds);
            _clock = clock ?? ZeroClock;
        }

        /// <summary>True once the single startup scan ran; cached results are authoritative.</summary>
        public bool Scanned => _scanned;

        /// <summary>Number of probe calls performed (identity check for the single-scan test).</summary>
        public int ProbeCallCount { get; private set; }

        /// <summary>Replaces the budget (the game applies the configured value before the first scan).</summary>
        /// <param name="budgetSeconds">Total scan budget in seconds.</param>
        public void SetBudget(double budgetSeconds)
        {
            _budgetSeconds = NormalizeBudget(budgetSeconds);
        }

        /// <summary>Detects one mod, returning the cached result on every later call.</summary>
        /// <param name="modId">Stable mod id.</param>
        /// <returns>The status; never null.</returns>
        public ExternalModStatus Detect(string modId)
        {
            if (string.IsNullOrEmpty(modId))
            {
                return ExternalModStatus.Absent(string.Empty, "unknown-mod-id");
            }

            if (_cache.TryGetValue(modId, out ExternalModStatus cached))
            {
                return cached;
            }

            CompatProbeDefinition? definition = CompatAnchors.ById(modId);
            if (definition == null)
            {
                return ExternalModStatus.Absent(modId, "unknown-mod-id");
            }

            if (!_scanned)
            {
                StartScan();
            }

            ExternalModStatus status = ProbeMod(definition);
            _cache[modId] = status;
            return status;
        }

        /// <summary>Detects the three optional mods in the fixed order and returns all statuses.</summary>
        /// <returns>One status per optional mod; never null.</returns>
        public IReadOnlyList<ExternalModStatus> DetectAll()
        {
            if (_order.Count != CompatAnchors.All.Length)
            {
                _order.Clear();
                for (int i = 0; i < CompatAnchors.All.Length; i++)
                {
                    _order.Add(Detect(CompatAnchors.All[i].ModId));
                }
            }

            return _order;
        }

        /// <summary>
        /// Detects the unsupported legacy Power Lines build. It has no adapter and is never
        /// integrated or patched; the caller only reports it to the player (REQ-COMPAT-16).
        /// </summary>
        /// <returns>Available when the legacy assembly is loaded, otherwise absent.</returns>
        public ExternalModStatus DetectLegacyPowerLines()
        {
            return Detect(CompatAnchors.LegacyPowerLines);
        }

        /// <summary>Clears the cache for the manual "re-detect" action (requirements §10.6).</summary>
        public void Reset()
        {
            _cache.Clear();
            _order.Clear();
            _scanned = false;
            ProbeCallCount = 0;
        }

        /// <summary>One-line diagnostic summary of every cached status.</summary>
        /// <returns>Deterministic summary text.</returns>
        public string BuildSummary()
        {
            var builder = new StringBuilder(128);
            builder.Append("compat scan:");
            for (int i = 0; i < CompatAnchors.All.Length; i++)
            {
                string modId = CompatAnchors.All[i].ModId;
                if (!_cache.TryGetValue(modId, out ExternalModStatus status))
                {
                    continue;
                }

                builder.Append(' ');
                builder.Append(modId);
                builder.Append('=');
                builder.Append(status.Level);
                if (!string.IsNullOrEmpty(status.Detail) && status.Level != Availability.Available)
                {
                    builder.Append('(');
                    builder.Append(status.Detail);
                    builder.Append(')');
                }

                if (!string.IsNullOrEmpty(status.Version))
                {
                    builder.Append('@');
                    builder.Append(status.Version);
                }
            }

            return builder.ToString();
        }

        private void StartScan()
        {
            _scanned = true;
            try
            {
                _startedAt = _clock();
            }
            catch (Exception)
            {
                _startedAt = 0.0;
            }
        }

        private ExternalModStatus ProbeMod(CompatProbeDefinition definition)
        {
            CompatProbeResult result;
            try
            {
                ProbeCallCount++;
                result = _probe.Probe(definition.ModId);
            }
            catch (Exception exception)
            {
                return ExternalModStatus.Degraded(
                    definition.ModId, null, ProbeExceptionDetail + ":" + exception.GetType().Name);
            }

            if (result.Failure != null)
            {
                return ExternalModStatus.Degraded(definition.ModId, null, result.Failure);
            }

            if (!result.AssemblyPresent)
            {
                return ExternalModStatus.Absent(definition.ModId, "assembly-missing");
            }

            string? version = result.AssemblyVersion;
            if (result.AnchorMissing != null)
            {
                return ExternalModStatus.Degraded(
                    definition.ModId, version, "anchor-missing: " + result.AnchorMissing);
            }

            if (BudgetExceeded())
            {
                return ExternalModStatus.Degraded(definition.ModId, version, BudgetExceededDetail);
            }

            VersionGateOutcome gate = CompatVersionPolicy.Evaluate(version, definition.MinimumVersion);
            if (gate == VersionGateOutcome.BelowMinimum)
            {
                return ExternalModStatus.Degraded(
                    definition.ModId,
                    version,
                    "version-below-minimum: " + definition.MinimumVersion,
                    CompatVersionPolicy.BelowMinimumNote);
            }

            string? note = gate == VersionGateOutcome.NotComparable && !string.IsNullOrEmpty(version)
                ? CompatVersionPolicy.AssemblyMetadataVersionNote
                : null;
            return ExternalModStatus.Available(definition.ModId, version, note);
        }

        private bool BudgetExceeded()
        {
            if (_budgetSeconds <= 0.0)
            {
                return false;
            }

            try
            {
                return _clock() - _startedAt > _budgetSeconds;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static double NormalizeBudget(double budgetSeconds)
        {
            return double.IsNaN(budgetSeconds) || budgetSeconds < 0.0 ? 0.0 : budgetSeconds;
        }

        private static double ZeroClock()
        {
            return 0.0;
        }
    }
}
