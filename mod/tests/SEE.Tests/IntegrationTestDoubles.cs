using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Config;
using SEE.Integration.Adapters;
using SEE.Integration.Detection;

namespace SEE.Tests
{
    /// <summary>
    /// Injected fake probe of the compatibility detection (CONTRACT_SEE §1.4, REQ-COMPAT-09). It
    /// never loads a real external DLL: the tests decide presence, anchors, version and failures per
    /// mod id, so the whole 12-scenario matrix runs offline.
    /// </summary>
    internal sealed class FakeCompatProbe : ICompatProbe
    {
        private readonly Dictionary<string, CompatProbeResult> _results =
            new Dictionary<string, CompatProbeResult>(StringComparer.Ordinal);

        /// <summary>Number of probe invocations (single-cache assertions).</summary>
        internal int Calls { get; private set; }

        /// <summary>Optional callback to simulate a throwing probe.</summary>
        internal Action<string>? OnProbe { get; set; }

        /// <summary>Registers a raw probe result for one mod id.</summary>
        internal FakeCompatProbe With(string modId, CompatProbeResult result)
        {
            _results[modId] = result;
            return this;
        }

        /// <summary>Registers an available result with an optional detected version.</summary>
        internal FakeCompatProbe Available(string modId, string? version = null) =>
            With(modId, CompatProbeResult.AssemblyFound(version));

        /// <summary>Registers an assembly-only result with a missing anchor.</summary>
        internal FakeCompatProbe AnchorMissing(string modId, string anchor) =>
            With(modId, CompatProbeResult.AnchorNotFound(null, anchor));

        /// <inheritdoc />
        public CompatProbeResult Probe(string modId)
        {
            Calls++;
            OnProbe?.Invoke(modId);
            return _results.TryGetValue(modId, out CompatProbeResult result)
                ? result
                : CompatProbeResult.AssemblyNotFound();
        }
    }

    /// <summary>Fake detector used by the adapter matrix tests; returns the canned statuses.</summary>
    internal sealed class FakeCompatDetector : ICompatDetector
    {
        private readonly Dictionary<string, ExternalModStatus> _statuses =
            new Dictionary<string, ExternalModStatus>(StringComparer.Ordinal);
        private ExternalModStatus _legacy = ExternalModStatus.Absent(CompatAnchors.LegacyPowerLines, "assembly-missing");

        /// <summary>Creates a detector with the three statuses (null means assembly-missing).</summary>
        internal FakeCompatDetector(
            ExternalModStatus? timberPipes = null,
            ExternalModStatus? powerLines = null,
            ExternalModStatus? conveyorBelts = null)
        {
            _statuses[CompatAnchors.TimberPipes] = timberPipes ?? Statuses.Absent(CompatAnchors.TimberPipes);
            _statuses[CompatAnchors.PowerLines] = powerLines ?? Statuses.Absent(CompatAnchors.PowerLines);
            _statuses[CompatAnchors.ConveyorBelt] = conveyorBelts ?? Statuses.Absent(CompatAnchors.ConveyorBelt);
        }

        /// <summary>Sets the legacy detection result.</summary>
        internal FakeCompatDetector WithLegacy(ExternalModStatus legacy)
        {
            _legacy = legacy;
            return this;
        }

        /// <inheritdoc />
        public ExternalModStatus Detect(string modId)
        {
            return _statuses.TryGetValue(modId, out ExternalModStatus status)
                ? status
                : Statuses.Absent(modId);
        }

        /// <inheritdoc />
        public IReadOnlyList<ExternalModStatus> DetectAll()
        {
            return new[]
            {
                Detect(CompatAnchors.TimberPipes),
                Detect(CompatAnchors.PowerLines),
                Detect(CompatAnchors.ConveyorBelt)
            };
        }

        /// <inheritdoc />
        public ExternalModStatus DetectLegacyPowerLines() => _legacy;

        /// <inheritdoc />
        public void Refresh()
        {
        }
    }

    /// <summary>Fixed option source for the adapter matrix.</summary>
    internal sealed class FixedCompatOptions : ICompatOptionsSource
    {
        internal FixedCompatOptions(CompatAdapterOptions? options = null)
        {
            Current = options ?? CompatAdapterOptions.Default;
        }

        /// <inheritdoc />
        public CompatAdapterOptions Current { get; set; }
    }

    /// <summary>Small factory helpers for the detection statuses used by the tests.</summary>
    internal static class Statuses
    {
        internal static ExternalModStatus Available(string modId, string? version = null) =>
            ExternalModStatus.Available(modId, version);

        internal static ExternalModStatus Degraded(string modId, string detail) =>
            ExternalModStatus.Degraded(modId, null, detail);

        internal static ExternalModStatus Absent(string modId) =>
            ExternalModStatus.Absent(modId, "assembly-missing");
    }

    /// <summary>One fully wired adapter set for one compatibility scenario.</summary>
    internal sealed class CompatRig
    {
        internal CompatRig(
            ExternalModStatus? timberPipes = null,
            ExternalModStatus? powerLines = null,
            ExternalModStatus? conveyorBelts = null,
            bool enablePowerLineInjection = true,
            WaterSupplyMode defaultWaterMode = WaterSupplyMode.Auto,
            BeltFuelMode beltFuelMode = BeltFuelMode.Auto)
        {
            Detector = new FakeCompatDetector(timberPipes, powerLines, conveyorBelts);
            Options = new FixedCompatOptions(new CompatAdapterOptions(
                enablePowerLineInjection, defaultWaterMode, beltFuelMode, 30.0));
            Pipes = new TimberPipesAdapter(Detector);
            Power = new PowerLinesAdapter(Detector, Options);
            Belts = new ConveyorBeltAdapter(Detector, Options);
        }

        internal FakeCompatDetector Detector { get; }

        internal FixedCompatOptions Options { get; }

        internal TimberPipesAdapter Pipes { get; }

        internal PowerLinesAdapter Power { get; }

        internal ConveyorBeltAdapter Belts { get; }

        /// <summary>Initializes all three adapters (idempotent).</summary>
        internal CompatRig Initialize()
        {
            Pipes.Initialize();
            Power.Initialize();
            Belts.Initialize();
            return this;
        }
    }
}
