using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Save;
using SEE.Integration.Adapters;
using SEE.Integration.Detection;
using SEE.Integration.Save;

namespace SEE.Tests
{
    /// <summary>
    /// The twelve compatibility scenarios of requirements §10.8 (REQ-COMPAT-18) plus the detection
    /// edge cases of §10.6. Every case runs against an injected fake probe / fake detector, so no
    /// real TimberPipes / PowerLines / ConveyorBelt DLL is loaded offline (CONTRACT_SEE §1.4).
    /// </summary>
    internal static class CompatScenarioTests
    {
        internal static void Run(TestRunner t)
        {
            ScenarioTests(t);
            DetectionSessionTests(t);
            AdapterLifecycleTests(t);
        }

        private static void ScenarioTests(TestRunner t)
        {
            t.Run("Scenario 1: no external mods -> hauler water, hauler fuel, vanilla shaft", () =>
            {
                CompatRig rig = new CompatRig().Initialize();
                Assert.Equal(AdapterState.Absent, rig.Pipes.State, "TP adapter");
                Assert.Equal(AdapterState.Absent, rig.Power.State, "PL adapter");
                Assert.Equal(AdapterState.Absent, rig.Belts.State, "CB adapter");
                Assert.Equal(WaterSupplyMode.Hauler, rig.Pipes.SuggestedMode, "water mode");
                Assert.False(rig.Power.InjectionAvailable, "no cable injection");
                Assert.False(rig.Belts.FuelSupplyAvailable, "no belt UI");
                Assert.False(rig.Pipes.TankModeAvailable, "no tank capability");
            });

            t.Run("Scenario 2: Timber Pipes only -> hauler (v0.1.1 has no liquid tank)", () =>
            {
                CompatRig rig = new CompatRig(timberPipes: Statuses.Available(CompatAnchors.TimberPipes)).Initialize();
                Assert.Equal(AdapterState.Enabled, rig.Pipes.State, "TP enabled");
                Assert.Equal(WaterSupplyMode.Hauler, rig.Pipes.SuggestedMode, "auto resolves to hauler");
                Assert.Equal(WaterSupplyPolicy.AutoHaulerNoLiquidTankReason, rig.Pipes.SuggestedModeReason, "reason");
                Assert.False(rig.Pipes.TankModeAvailable, "tank not offered before P8 evidence");
                Assert.Equal(WaterSupplyMode.Hauler, rig.Pipes.Resolve(WaterSupplyMode.Auto).Effective, "auto");
                Assert.Equal(AdapterState.Absent, rig.Power.State, "PL absent");
                Assert.Equal(AdapterState.Absent, rig.Belts.State, "CB absent");
            });

            t.Run("Scenario 3: Conveyor Belts only -> belts feed the vanilla inventory", () =>
            {
                CompatRig rig = new CompatRig(conveyorBelts: Statuses.Available(CompatAnchors.ConveyorBelt)).Initialize();
                Assert.Equal(AdapterState.Enabled, rig.Belts.State, "CB enabled");
                Assert.True(rig.Belts.FuelSupplyAvailable, "belt fuel row");
                Assert.True(rig.Belts.BeltFuelPreferred, "auto belt preference");
                Assert.True(rig.Belts.BeltPowerWarningAvailable, "belt power warning");
                Assert.Equal(WaterSupplyMode.Hauler, rig.Pipes.SuggestedMode, "water hauler");
                Assert.False(rig.Power.InjectionAvailable, "no PL");
            });

            t.Run("Scenario 4: Power Lines only -> runtime PowerLineSpec injection", () =>
            {
                CompatRig rig = new CompatRig(powerLines: Statuses.Available(CompatAnchors.PowerLines)).Initialize();
                Assert.Equal(AdapterState.Enabled, rig.Power.State, "PL enabled");
                Assert.True(rig.Power.InjectionAvailable, "injection available");
                Assert.True(rig.Power.TryAttach(Guid.NewGuid()), "attach reports active");
                Assert.True(rig.Power.IsAttached(Guid.NewGuid()), "attached reports active");
                Assert.Equal(WaterSupplyMode.Hauler, rig.Pipes.SuggestedMode, "water hauler");
                Assert.Equal(AdapterState.Absent, rig.Belts.State, "CB absent");
            });

            t.Run("Scenario 5: Timber Pipes + Conveyor Belts -> hauler water, fuel belts", () =>
            {
                CompatRig rig = new CompatRig(
                    timberPipes: Statuses.Available(CompatAnchors.TimberPipes),
                    conveyorBelts: Statuses.Available(CompatAnchors.ConveyorBelt)).Initialize();
                Assert.Equal(WaterSupplyMode.Hauler, rig.Pipes.SuggestedMode, "water");
                Assert.True(rig.Belts.FuelSupplyAvailable, "fuel belt");
                Assert.False(rig.Power.InjectionAvailable, "no cable");
            });

            t.Run("Scenario 6: Timber Pipes + Power Lines -> hauler water, cable points", () =>
            {
                CompatRig rig = new CompatRig(
                    timberPipes: Statuses.Available(CompatAnchors.TimberPipes),
                    powerLines: Statuses.Available(CompatAnchors.PowerLines)).Initialize();
                Assert.Equal(WaterSupplyMode.Hauler, rig.Pipes.SuggestedMode, "water");
                Assert.True(rig.Power.InjectionAvailable, "cable");
                Assert.False(rig.Belts.FuelSupplyAvailable, "no belt");
            });

            t.Run("Scenario 7: Conveyor Belts + Power Lines -> belts and cable, hauler water", () =>
            {
                CompatRig rig = new CompatRig(
                    powerLines: Statuses.Available(CompatAnchors.PowerLines),
                    conveyorBelts: Statuses.Available(CompatAnchors.ConveyorBelt)).Initialize();
                Assert.True(rig.Power.InjectionAvailable, "cable");
                Assert.True(rig.Belts.FuelSupplyAvailable, "belt");
                Assert.Equal(WaterSupplyMode.Hauler, rig.Pipes.SuggestedMode, "water");
            });

            t.Run("Scenario 8: all three mods -> every enhancement independent", () =>
            {
                CompatRig rig = new CompatRig(
                    timberPipes: Statuses.Available(CompatAnchors.TimberPipes),
                    powerLines: Statuses.Available(CompatAnchors.PowerLines),
                    conveyorBelts: Statuses.Available(CompatAnchors.ConveyorBelt)).Initialize();
                Assert.Equal(AdapterState.Enabled, rig.Pipes.State, "TP");
                Assert.Equal(AdapterState.Enabled, rig.Power.State, "PL");
                Assert.Equal(AdapterState.Enabled, rig.Belts.State, "CB");
                Assert.Equal(WaterSupplyMode.Hauler, rig.Pipes.SuggestedMode, "water");
                Assert.True(rig.Power.InjectionAvailable, "cable");
                Assert.True(rig.Belts.FuelSupplyAvailable, "belt");
                SeeCompatSnapshot snapshot = CompatSnapshotBuilder.Build(
                    new[] { rig.Pipes.Status, rig.Power.Status, rig.Belts.Status },
                    new IExternalAdapter[] { rig.Pipes, rig.Power, rig.Belts });
                Assert.Equal(3, snapshot.Detected.Count, "detected count");
                Assert.Equal(3, snapshot.Enabled.Count, "enabled count");
                Assert.Equal(0, snapshot.Failed.Count, "failed count");
            });

            t.Run("Scenario 9: legacy Opolame Power Lines is reported but never integrated", () =>
            {
                FakeCompatDetector detector = new FakeCompatDetector()
                    .WithLegacy(Statuses.Available(CompatAnchors.LegacyPowerLines));
                TimberPipesAdapter pipes = new TimberPipesAdapter(detector);
                PowerLinesAdapter power = new PowerLinesAdapter(detector, new FixedCompatOptions());
                pipes.Initialize();
                power.Initialize();
                Assert.True(detector.DetectLegacyPowerLines().Detected, "legacy detected");
                Assert.Equal(AdapterState.Absent, power.State, "legacy PL never enables the adapter");
                Assert.False(power.InjectionAvailable, "legacy PL never injects");
                Assert.Equal(WaterSupplyMode.Hauler, pipes.SuggestedMode, "water hauler");
            });

            t.Run("Scenario 10: missing TP/CB prerequisites -> absent and auto-degraded", () =>
            {
                CompatRig rig = new CompatRig().Initialize();
                Assert.Equal(AdapterState.Absent, rig.Pipes.State, "TP absent");
                Assert.Equal(AdapterState.Absent, rig.Belts.State, "CB absent");
                Assert.Equal(WaterSupplyPolicy.AutoHaulerNoPipesReason,
                    rig.Pipes.Resolve(WaterSupplyMode.Auto).ReasonKey, "reason");
                Assert.Equal(AdapterState.Absent, rig.Power.State, "PL absent");
            });

            t.Run("Scenario 11: mod present but anchors missing -> Degraded treated as absent", () =>
            {
                CompatRig rig = new CompatRig(
                    timberPipes: Statuses.Degraded(CompatAnchors.TimberPipes, "anchor-missing: TimberPipes.Specs.BuildingPipeSpec"),
                    powerLines: Statuses.Degraded(CompatAnchors.PowerLines, "anchor-missing: PowerLines.Specs.PowerLineSpec"),
                    conveyorBelts: Statuses.Degraded(CompatAnchors.ConveyorBelt, "anchor-missing: ConveyorBelt.Specs.BeltSystemSpec"))
                    .Initialize();
                Assert.Equal(AdapterState.Failed, rig.Pipes.State, "TP failed");
                Assert.Equal(AdapterState.Failed, rig.Power.State, "PL failed");
                Assert.Equal(AdapterState.Failed, rig.Belts.State, "CB failed");
                Assert.Equal(WaterSupplyMode.Hauler, rig.Pipes.SuggestedMode, "water hauler");
                Assert.False(rig.Power.InjectionAvailable, "no injection");
                Assert.NotNull(rig.Pipes.FailureKey, "TP failure key");
                Assert.NotNull(rig.Power.FailureKey, "PL failure key");
                SeeCompatSnapshot snapshot = CompatSnapshotBuilder.Build(
                    new[] { rig.Pipes.Status, rig.Power.Status, rig.Belts.Status },
                    new IExternalAdapter[] { rig.Pipes, rig.Power, rig.Belts });
                Assert.Equal(3, snapshot.Detected.Count, "degraded modules are still detected");
                Assert.Equal(0, snapshot.Enabled.Count, "nothing enabled");
                Assert.Equal(3, snapshot.Failed.Count, "three failure keys");
            });

            t.Run("Scenario 12: save with mods, load without them -> intents stay, run degrades", () =>
            {
                CompatRig rig = new CompatRig().Initialize();
                WaterSupplyDecision tankIntent = rig.Pipes.Resolve(WaterSupplyMode.Tank);
                Assert.Equal(WaterSupplyMode.Tank, tankIntent.Intent, "intent preserved");
                Assert.Equal(WaterSupplyMode.Hauler, tankIntent.Effective, "tank falls back to hauler");
                Assert.True(tankIntent.NeedsPlayerNotice, "player is told");
                Assert.Equal(WaterSupplyPolicy.IntentTankUnavailableReason, tankIntent.ReasonKey, "tank reason");

                WaterSupplyDecision valveIntent = rig.Pipes.Resolve(WaterSupplyMode.Valve);
                Assert.Equal(WaterSupplyMode.Hauler, valveIntent.Effective, "valve falls back to hauler");
                Assert.Equal(WaterSupplyPolicy.IntentValveUnavailableReason, valveIntent.ReasonKey, "valve reason");

                SeeCompatSnapshot snapshot = CompatSnapshotBuilder.Build(
                    new[]
                    {
                        Statuses.Absent(CompatAnchors.TimberPipes),
                        Statuses.Absent(CompatAnchors.PowerLines),
                        Statuses.Absent(CompatAnchors.ConveyorBelt)
                    },
                    new IExternalAdapter[] { rig.Pipes, rig.Power, rig.Belts });
                Assert.Equal(0, snapshot.Detected.Count, "detected reset");
                Assert.Equal(0, snapshot.Enabled.Count, "enabled reset");
                Assert.Equal(0, snapshot.Failed.Count, "failed reset");

                // Reverse case: the save was made without TP, then TP gets installed.
                CompatRig installed = new CompatRig(
                    timberPipes: Statuses.Available(CompatAnchors.TimberPipes)).Initialize();
                Assert.Equal(WaterSupplyMode.Hauler,
                    installed.Pipes.Resolve(WaterSupplyMode.Auto).Effective, "auto re-resolves on load");
            });
        }

        private static void DetectionSessionTests(TestRunner t)
        {
            t.Run("Detection: one scan caches all three results", () =>
            {
                var probe = new FakeCompatProbe()
                    .Available(CompatAnchors.TimberPipes)
                    .Available(CompatAnchors.PowerLines)
                    .Available(CompatAnchors.ConveyorBelt);
                var session = new CompatDetectionSession(probe, 2.0);
                session.DetectAll();
                session.DetectAll();
                session.Detect(CompatAnchors.TimberPipes);
                Assert.Equal(3, probe.Calls, "probe calls");
                Assert.True(session.Scanned, "scanned");
            });

            t.Run("Detection: unknown mod id is absent and never cached", () =>
            {
                var session = new CompatDetectionSession(new FakeCompatProbe(), 2.0);
                ExternalModStatus status = session.Detect("NoSuchMod");
                Assert.Equal(Availability.Absent, status.Level, "level");
                Assert.Equal("unknown-mod-id", status.Detail, "detail");
            });

            t.Run("Detection: probe exception degrades, never throws", () =>
            {
                var probe = new FakeCompatProbe { OnProbe = _ => throw new InvalidOperationException("boom") };
                var session = new CompatDetectionSession(probe, 2.0);
                ExternalModStatus status = session.Detect(CompatAnchors.TimberPipes);
                Assert.Equal(Availability.Degraded, status.Level, "level");
                Assert.Contains(status.Detail!, "probe-exception:InvalidOperationException", "detail");
            });

            t.Run("Detection: budget exceeded degrades the remaining probes", () =>
            {
                double clock = 0.0;
                var probe = new FakeCompatProbe()
                    .Available(CompatAnchors.TimberPipes)
                    .Available(CompatAnchors.PowerLines);
                var session = new CompatDetectionSession(probe, 2.0, () => clock);
                session.Detect(CompatAnchors.TimberPipes);
                clock = 5.0;
                ExternalModStatus later = session.Detect(CompatAnchors.PowerLines);
                Assert.Equal(Availability.Degraded, later.Level, "level");
                Assert.Equal(CompatDetectionSession.BudgetExceededDetail, later.Detail, "detail");
            });

            t.Run("Detection: same-major version below the manifest minimum degrades", () =>
            {
                var probe = new FakeCompatProbe().Available(CompatAnchors.TimberPipes, "11.3.0");
                var session = new CompatDetectionSession(probe, 2.0);
                ExternalModStatus status = session.Detect(CompatAnchors.TimberPipes);
                Assert.Equal(Availability.Degraded, status.Level, "level");
                Assert.Contains(status.Detail!, "version-below-minimum", "detail");
                Assert.Equal(CompatVersionPolicy.BelowMinimumNote, status.VersionNote, "note");
            });

            t.Run("Detection: different assembly version scheme is a note, never a block", () =>
            {
                var probe = new FakeCompatProbe().Available(CompatAnchors.PowerLines, "1.0.0+c73f20e");
                var session = new CompatDetectionSession(probe, 2.0);
                ExternalModStatus status = session.Detect(CompatAnchors.PowerLines);
                Assert.Equal(Availability.Available, status.Level, "level");
                Assert.Equal(CompatVersionPolicy.AssemblyMetadataVersionNote, status.VersionNote, "note");
            });

            t.Run("Detection: legacy Power Lines stays out of the three-mod set", () =>
            {
                var probe = new FakeCompatProbe().Available(CompatAnchors.LegacyPowerLines);
                var session = new CompatDetectionSession(probe, 2.0);
                IReadOnlyList<ExternalModStatus> all = session.DetectAll();
                Assert.Equal(3, all.Count, "three-mod set");
                Assert.True(session.DetectLegacyPowerLines().Detected, "legacy detected");
            });

            t.Run("Detection: manual reset rescans (one cache per scan)", () =>
            {
                var probe = new FakeCompatProbe().Available(CompatAnchors.TimberPipes);
                var session = new CompatDetectionSession(probe, 2.0);
                session.Detect(CompatAnchors.TimberPipes);
                session.Reset();
                session.Detect(CompatAnchors.TimberPipes);
                Assert.Equal(2, probe.Calls, "probe calls");
            });
        }

        private static void AdapterLifecycleTests(TestRunner t)
        {
            t.Run("Adapter lifecycle: initialize is single-shot and shutdown is symmetric", () =>
            {
                CompatRig rig = new CompatRig(
                    timberPipes: Statuses.Available(CompatAnchors.TimberPipes)).Initialize();
                rig.Pipes.Initialize();
                Assert.Equal(AdapterState.Enabled, rig.Pipes.State, "still enabled");
                rig.Pipes.Shutdown();
                Assert.Equal(AdapterState.Absent, rig.Pipes.State, "closed");
                Assert.False(rig.Pipes.IsAvailable, "not available");
            });

            t.Run("Adapter lifecycle: Power Lines injection follows compat.enablePowerLineInjection", () =>
            {
                CompatRig disabled = new CompatRig(
                    powerLines: Statuses.Available(CompatAnchors.PowerLines),
                    enablePowerLineInjection: false).Initialize();
                Assert.Equal(AdapterState.Detected, disabled.Power.State, "detected but disabled");
                Assert.False(disabled.Power.InjectionAvailable, "no injection");
                Assert.Equal("config-disabled:enablePowerLineInjection", disabled.Power.Diagnostic, "reason");

                CompatRig enabled = new CompatRig(
                    powerLines: Statuses.Available(CompatAnchors.PowerLines)).Initialize();
                Assert.Equal(AdapterState.Enabled, enabled.Power.State, "enabled");
            });

            t.Run("Adapter lifecycle: a broken detector never escapes as an exception", () =>
            {
                var pipes = new TimberPipesAdapter(new ThrowingDetector());
                pipes.Initialize();
                Assert.Equal(AdapterState.Failed, pipes.State, "failed");
                Assert.NotNull(pipes.FailureKey, "failure key");
            });
        }

        /// <summary>Detector that always throws, to prove the adapter failure boundary.</summary>
        private sealed class ThrowingDetector : ICompatDetector
        {
            public ExternalModStatus Detect(string modId) => throw new InvalidOperationException("boom");

            public IReadOnlyList<ExternalModStatus> DetectAll() => throw new InvalidOperationException("boom");

            public ExternalModStatus DetectLegacyPowerLines() => throw new InvalidOperationException("boom");

            public void Refresh()
            {
            }
        }
    }
}
