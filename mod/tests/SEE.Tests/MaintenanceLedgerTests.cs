using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Maintenance;

namespace SEE.Tests
{
    /// <summary>
    /// Unit tests for <see cref="MaintenanceLedger"/> (requirements §9.2/§9.5/§9.8, D-7):
    /// wear accumulation, the 100 -> 50 one-interval invariant, the Begin/Advance/Complete cycle,
    /// save restore clamps and the automatic/manual decision matrix.
    /// </summary>
    internal static class MaintenanceLedgerTests
    {
        internal static void Run(TestRunner t)
        {
            DefaultsTests(t);
            WearTests(t);
            RestoreTests(t);
            DecideTests(t);
            ManualTests(t);
            CycleTests(t);
        }

        private static void DefaultsTests(TestRunner t)
        {
            t.Run("A fresh ledger is fully maintained", () =>
            {
                var ledger = new MaintenanceLedger();
                Assert.Near(100.0, ledger.Condition, 1e-6, "condition");
                Assert.Equal(MaintenancePhase.None, ledger.Phase, "phase");
                Assert.Near(0.0, ledger.RemainingHours, 1e-6, "remaining");
                Assert.Near(0.0, ledger.LastMaintenanceDay, 1e-6, "day");
                Assert.True(ledger.AutoMaintenance, "auto maintenance defaults on");
            });
        }

        private static void WearTests(TestRunner t)
        {
            t.Run("Full load wears exactly 50 points in one T2 interval", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.AccumulateWear(8 * 24, 8.0, 1.0f);
                Assert.Near(50.0, ledger.Condition, 1e-4, "100 -> 50 over 8 running days");
            });

            t.Run("Full load reaches the threshold in one interval for every tier", () =>
            {
                var intervals = new Dictionary<BoilerTier, double>
                {
                    [BoilerTier.T1] = 6,
                    [BoilerTier.T2] = 8,
                    [BoilerTier.T3] = 10,
                    [BoilerTier.T4] = 12,
                };

                foreach (var pair in intervals)
                {
                    var ledger = new MaintenanceLedger();
                    ledger.AccumulateWear(pair.Value * 24, pair.Value, 1.0f);
                    Assert.Near(50.0, ledger.Condition, 1e-4, pair.Key + " 100 -> 50");
                }
            });

            t.Run("Half load wears 0.7 of the full-load amount", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.AccumulateWear(96, 8.0, 0.5f);
                Assert.Near(82.5, ledger.Condition, 1e-4, "100 - 50*0.5*0.7");
            });

            t.Run("Idle boiler never wears", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.AccumulateWear(500, 8.0, 0f);
                Assert.Near(100.0, ledger.Condition, 1e-6, "load 0");
            });

            t.Run("Invalid wear arguments are no-ops", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.AccumulateWear(0, 8.0, 1f);
                ledger.AccumulateWear(-5, 8.0, 1f);
                ledger.AccumulateWear(double.NaN, 8.0, 1f);
                Assert.Near(100.0, ledger.Condition, 1e-6, "no-ops");
            });

            t.Run("Condition never goes below zero", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.AccumulateWear(2000, 8.0, 1f);
                Assert.Near(0.0, ledger.Condition, 1e-6, "clamped at 0");
            });

            t.Run("Wear only accumulates while not maintaining", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(50f, true, MaintenancePhase.Maintaining, 2f, 0f);
                ledger.AccumulateWear(24, 8.0, 1f);
                Assert.Near(50.0, ledger.Condition, 1e-6, "maintaining freezes wear");
            });
        }

        private static void RestoreTests(TestRunner t)
        {
            t.Run("Reset restores the persisted fields", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(80f, false, MaintenancePhase.Maintaining, 5f, 3.5f);
                Assert.Near(80.0, ledger.Condition, 1e-6, "condition");
                Assert.False(ledger.AutoMaintenance, "auto flag");
                Assert.Equal(MaintenancePhase.Maintaining, ledger.Phase, "phase");
                Assert.Near(2.0, ledger.RemainingHours, 1e-6, "remaining clamped to the [0,2] contract");
                Assert.Near(3.5, ledger.LastMaintenanceDay, 1e-6, "last maintenance day");
            });

            t.Run("Remaining hours are 0 when not maintaining", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(90f, true, MaintenancePhase.None, 1.5f, 0f);
                Assert.Near(0.0, ledger.RemainingHours, 1e-6, "None -> 0");
            });

            t.Run("Reset clamps NaN/out-of-range condition", () =>
            {
                var high = new MaintenanceLedger();
                high.Reset(250f, true, MaintenancePhase.None, 0f, 0f);
                Assert.Near(100.0, high.Condition, 1e-6, "250 -> 100");

                var low = new MaintenanceLedger();
                low.Reset(-20f, true, MaintenancePhase.None, 0f, 0f);
                Assert.Near(0.0, low.Condition, 1e-6, "-20 -> 0");

                var nan = new MaintenanceLedger();
                nan.Reset(float.NaN, true, MaintenancePhase.None, 0f, 0f);
                Assert.Near(100.0, nan.Condition, 1e-6, "NaN -> 100");
            });
        }

        private static void DecideTests(TestRunner t)
        {
            t.Run("Healthy boiler above the threshold needs nothing", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(80f, true, MaintenancePhase.None, 0f, 0f);
                var decision = ledger.Decide(hasMaterials: true, hasAccess: true, paused: false);
                Assert.Equal(MaintenanceAction.None, decision.Action, "action");
            });

            t.Run("Condition at the threshold starts automatic maintenance", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(50f, true, MaintenancePhase.None, 0f, 0f);
                var decision = ledger.Decide(hasMaterials: true, hasAccess: true, paused: false);
                Assert.Equal(MaintenanceAction.Start, decision.Action, "action");
                Assert.Equal(MaintenancePhase.None, decision.Phase, "phase");
            });

            t.Run("Missing materials block but do not drop the request", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(50f, true, MaintenancePhase.None, 0f, 0f);
                var blocked = ledger.Decide(hasMaterials: false, hasAccess: true, paused: false);
                Assert.Equal(MaintenanceAction.BlockedByMaterials, blocked.Action, "action");
                Assert.Equal(MaintenancePhase.MissingMaterials, blocked.Phase, "phase");

                var retry = ledger.Decide(hasMaterials: true, hasAccess: true, paused: false);
                Assert.Equal(MaintenanceAction.Start, retry.Action, "pending request resumes");
            });

            t.Run("Missing access blocks but does not drop the request", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(50f, true, MaintenancePhase.None, 0f, 0f);
                var blocked = ledger.Decide(hasMaterials: true, hasAccess: false, paused: false);
                Assert.Equal(MaintenanceAction.BlockedByAccess, blocked.Action, "action");
                Assert.Equal(MaintenancePhase.NoAccess, blocked.Phase, "phase");
            });

            t.Run("Paused boiler never starts maintenance", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(10f, true, MaintenancePhase.None, 0f, 0f);
                var decision = ledger.Decide(hasMaterials: true, hasAccess: true, paused: true);
                Assert.Equal(MaintenanceAction.None, decision.Action, "action");
            });

            t.Run("Auto flag off requires a manual request", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(10f, false, MaintenancePhase.None, 0f, 0f);
                var decision = ledger.Decide(hasMaterials: true, hasAccess: true, paused: false);
                Assert.Equal(MaintenanceAction.None, decision.Action, "action");
            });

            t.Run("Disabling the maintenance system leaves a fully maintained boiler", () =>
            {
                var ledger = new MaintenanceLedger
                {
                    Rules = new MaintenanceRules(
                        enabled: false,
                        auto: true,
                        autoThreshold: 50f,
                        durationHours: 2f,
                        intervalDays: 8f,
                        materials: Array.Empty<MaintenanceMaterial>()),
                };
                ledger.Reset(12f, true, MaintenancePhase.MissingMaterials, 0f, 0f);
                var decision = ledger.Decide(hasMaterials: true, hasAccess: true, paused: false);
                Assert.Equal(MaintenanceAction.None, decision.Action, "action");
                Assert.Near(100.0, ledger.Condition, 1e-6, "condition resets");
                Assert.Equal(MaintenancePhase.None, ledger.Phase, "phase resets");
            });
        }

        private static void ManualTests(TestRunner t)
        {
            t.Run("Manual maintenance is available at condition 99 but not at 100", () =>
            {
                var dirty = new MaintenanceLedger();
                dirty.Reset(99f, true, MaintenancePhase.None, 0f, 0f);
                Assert.True(dirty.IsManualMaintenanceAvailable, "99 is below 100");

                var clean = new MaintenanceLedger();
                Assert.False(clean.IsManualMaintenanceAvailable, "100 is fully maintained");
            });

            t.Run("Manual maintenance starts below 100 even while auto is off", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(80f, false, MaintenancePhase.None, 0f, 0f);
                var decision = ledger.RequestManual(hasMaterials: true, hasAccess: true);
                Assert.Equal(MaintenanceAction.Start, decision.Action, "action");
            });

            t.Run("A manual request above the warn threshold is allowed but reports low benefit", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(95f, true, MaintenancePhase.None, 0f, 0f);
                Assert.True(ledger.IsManualBenefitLow, "95 > 90 shows the hint");
                var decision = ledger.RequestManual(hasMaterials: true, hasAccess: true);
                Assert.Equal(MaintenanceAction.Start, decision.Action, "the hint never disables the button (C-5)");
            });

            t.Run("A manual request at or below the warn threshold reports no low-benefit hint", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(90f, true, MaintenancePhase.None, 0f, 0f);
                Assert.False(ledger.IsManualBenefitLow, "90 is not above 90");
                ledger.Reset(50f, true, MaintenancePhase.None, 0f, 0f);
                Assert.False(ledger.IsManualBenefitLow, "50");
            });

            t.Run("A manual request at condition 100 is a no-op", () =>
            {
                var ledger = new MaintenanceLedger();
                var decision = ledger.RequestManual(hasMaterials: true, hasAccess: true);
                Assert.Equal(MaintenanceAction.None, decision.Action, "action");
                Assert.Equal(MaintenancePhase.None, decision.Phase, "phase");
            });

            t.Run("Missing materials block a manual request without consuming anything", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(70f, false, MaintenancePhase.None, 0f, 0f);
                var blocked = ledger.RequestManual(hasMaterials: false, hasAccess: true);
                Assert.Equal(MaintenanceAction.BlockedByMaterials, blocked.Action, "action");
                Assert.Equal(MaintenancePhase.MissingMaterials, blocked.Phase, "phase");
                Assert.Near(70.0, ledger.Condition, 1e-6, "condition untouched");
                Assert.Near(0.0, ledger.RemainingHours, 1e-6, "no downtime started");

                var retry = ledger.RequestManual(hasMaterials: true, hasAccess: true);
                Assert.Equal(MaintenanceAction.Start, retry.Action, "the pending request resumes");
            });

            t.Run("Missing road blocks a manual request without consuming anything", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(70f, false, MaintenancePhase.None, 0f, 0f);
                var blocked = ledger.RequestManual(hasMaterials: true, hasAccess: false);
                Assert.Equal(MaintenanceAction.BlockedByAccess, blocked.Action, "action");
                Assert.Equal(MaintenancePhase.NoAccess, blocked.Phase, "phase");
                Assert.Near(70.0, ledger.Condition, 1e-6, "condition untouched");
            });

            t.Run("A disabled maintenance system ignores manual requests and resets to 100", () =>
            {
                var ledger = new MaintenanceLedger
                {
                    Rules = new MaintenanceRules(
                        enabled: false,
                        auto: true,
                        autoThreshold: 50f,
                        durationHours: 2f,
                        intervalDays: 8f,
                        materials: Array.Empty<MaintenanceMaterial>()),
                };
                ledger.Reset(40f, true, MaintenancePhase.MissingMaterials, 0f, 0f);
                var decision = ledger.RequestManual(hasMaterials: true, hasAccess: true);
                Assert.Equal(MaintenanceAction.None, decision.Action, "action");
                Assert.False(ledger.IsManualMaintenanceAvailable, "unavailable when disabled");
                Assert.Near(100.0, ledger.Condition, 1e-6, "condition resets (REQ-MAINT-17)");
                Assert.Equal(MaintenancePhase.None, ledger.Phase, "phase resets");
            });

            t.Run("Manual maintenance completes through the 2-hour cycle back to 100", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(60f, false, MaintenancePhase.None, 0f, 0f);
                Assert.Equal(MaintenanceAction.Start, ledger.RequestManual(true, true).Action, "start");
                ledger.BeginMaintenance(2f);
                ledger.AdvanceMaintenance(2.0);
                Assert.Equal(MaintenanceAction.Completed, ledger.Decide(true, true, false).Action, "completed");
                ledger.CompleteMaintenance(3f);
                Assert.Near(100.0, ledger.Condition, 1e-6, "condition");
                Assert.Near(3.0, ledger.LastMaintenanceDay, 1e-6, "last maintenance day");
            });
        }

        private static void CycleTests(TestRunner t)
        {
            t.Run("Begin/Advance/Complete runs the 2-hour downtime", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(50f, true, MaintenancePhase.None, 0f, 0f);
                ledger.BeginMaintenance(2f);
                Assert.Equal(MaintenancePhase.Maintaining, ledger.Phase, "phase");
                Assert.Near(2.0, ledger.RemainingHours, 1e-6, "timer");

                ledger.AdvanceMaintenance(0.5);
                Assert.Near(1.5, ledger.RemainingHours, 1e-6, "half hour");

                ledger.AdvanceMaintenance(10);
                Assert.Near(0.0, ledger.RemainingHours, 1e-6, "never negative");

                ledger.CompleteMaintenance();
                Assert.Near(100.0, ledger.Condition, 1e-6, "condition restored");
                Assert.Equal(MaintenancePhase.None, ledger.Phase, "phase");
                Assert.Near(0.0, ledger.RemainingHours, 1e-6, "timer cleared");
            });

            t.Run("CompleteMaintenance(day) records the maintenance day", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(40f, true, MaintenancePhase.Maintaining, 0.5f, 0f);
                ledger.CompleteMaintenance(12.5f);
                Assert.Near(12.5, ledger.LastMaintenanceDay, 1e-6, "last day");
                Assert.Near(100.0, ledger.Condition, 1e-6, "condition");
            });

            t.Run("AdvanceMaintenance outside the maintaining phase is a no-op", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.AdvanceMaintenance(1.0);
                Assert.Near(0.0, ledger.RemainingHours, 1e-6, "remaining");
                Assert.Equal(MaintenancePhase.None, ledger.Phase, "phase");
            });

            t.Run("Maintaining phases report Completed at timer zero", () =>
            {
                var ledger = new MaintenanceLedger();
                ledger.Reset(50f, true, MaintenancePhase.Maintaining, 0f, 0f);
                var decision = ledger.Decide(hasMaterials: true, hasAccess: true, paused: false);
                Assert.Equal(MaintenanceAction.Completed, decision.Action, "action");
            });
        }
    }
}
