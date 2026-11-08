using SEE.Core.Abstractions;
using SEE.Core.Formulas;
using SEE.Core.State;

namespace SEE.Tests
{
    /// <summary>
    /// Unit tests for <see cref="BoilerStateMachine"/> (requirements §3.3/§9.7/§13.5, AD-12): the
    /// frozen priority Paused &gt; Maintaining &gt; NoFuel &gt; NoWater &gt; NeedsMaintenance &gt;
    /// Critical &gt; Worn &gt; MaintenanceDue &gt; Idle &gt; Running, plus the full
    /// <see cref="BoilerStateResolution"/> surface (badge, reason, multiplier, load).
    /// </summary>
    internal static class StateMachineTests
    {
        internal static void Run(TestRunner t)
        {
            PriorityTests(t);
            PriorityMatrixTests(t);
            ConditionBandTests(t);
            ProducesTests(t);
            ResolutionTests(t);
            BadgeAndReasonTests(t);
            EnumOrderTests(t);
        }

        private static void PriorityTests(TestRunner t)
        {
            t.Run("Paused wins over NoFuel", () =>
                Assert.Equal(BoilerStatus.Paused, Evaluate(paused: true, hasFuel: false), "paused + no fuel"));

            t.Run("Paused wins over an active maintenance", () =>
                Assert.Equal(BoilerStatus.Paused,
                    Evaluate(paused: true, phase: MaintenancePhase.Maintaining, condition: 0f), "paused + maintaining"));

            t.Run("Maintaining wins over NoFuel", () =>
                Assert.Equal(BoilerStatus.Maintaining,
                    Evaluate(phase: MaintenancePhase.Maintaining, hasFuel: false), "maintaining + no fuel"));

            t.Run("NoFuel wins over NoWater", () =>
                Assert.Equal(BoilerStatus.NoFuel, Evaluate(hasFuel: false, hasWater: false), "no fuel + no water"));

            t.Run("NoFuel wins over NeedsMaintenance", () =>
                Assert.Equal(BoilerStatus.NoFuel, Evaluate(condition: 0f, hasFuel: false), "condition 0 + no fuel"));

            t.Run("NoWater wins over NeedsMaintenance", () =>
                Assert.Equal(BoilerStatus.NoWater, Evaluate(condition: 0f, hasWater: false), "condition 0 + no water"));

            t.Run("NeedsMaintenance at condition 0 with fuel and water", () =>
                Assert.Equal(BoilerStatus.NeedsMaintenance, Evaluate(condition: 0f), "condition 0"));

            t.Run("Idle wins over the condition bands when there is no demand", () =>
                Assert.Equal(BoilerStatus.Idle, Evaluate(condition: 15f, load: 0f), "condition 15 + no demand"));

            t.Run("Critical band applies when there is demand", () =>
                Assert.Equal(BoilerStatus.Critical, Evaluate(condition: 15f, load: 1f), "condition 15 + demand"));

            t.Run("Normal boiler under demand is Running", () =>
                Assert.Equal(BoilerStatus.Running, Evaluate(condition: 100f, load: 1f), "normal"));

            t.Run("NaN demand reads as idle", () =>
                Assert.Equal(BoilerStatus.Idle, Evaluate(load: float.NaN), "NaN load"));

            // 2026-11-08 hotfix-3: at night the network demand is 0, but the gameplay controller
            // passes the standby-floored operational load, so a supplied boiler stays Running.
            t.Run("Standby load keeps a supplied boiler Running when the demand is 0", () =>
                Assert.Equal(
                    BoilerStatus.Running,
                    Evaluate(condition: 100f, load: BoilerFormulas.OperationalLoadRatio(0f, 0.10f)),
                    "night standby"));

            t.Run("NaN condition reads as NeedsMaintenance", () =>
                Assert.Equal(BoilerStatus.NeedsMaintenance, Evaluate(condition: float.NaN), "NaN condition"));
        }

        private static void ConditionBandTests(TestRunner t)
        {
            t.Run("Condition 61-100 is Running", () =>
            {
                Assert.Equal(BoilerStatus.Running, Evaluate(condition: 100f), "100");
                Assert.Equal(BoilerStatus.Running, Evaluate(condition: 61f), "61");
            });

            t.Run("Condition 41-60 is MaintenanceDue", () =>
            {
                Assert.Equal(BoilerStatus.MaintenanceDue, Evaluate(condition: 60f), "60");
                Assert.Equal(BoilerStatus.MaintenanceDue, Evaluate(condition: 41f), "41");
            });

            t.Run("Condition 21-40 is Worn", () =>
            {
                Assert.Equal(BoilerStatus.Worn, Evaluate(condition: 40f), "40");
                Assert.Equal(BoilerStatus.Worn, Evaluate(condition: 21f), "21");
            });

            t.Run("Condition 1-20 is Critical", () =>
            {
                Assert.Equal(BoilerStatus.Critical, Evaluate(condition: 20f), "20");
                Assert.Equal(BoilerStatus.Critical, Evaluate(condition: 1f), "1");
            });

            t.Run("ResolveConditionState maps the band boundaries", () =>
            {
                Assert.Equal(BoilerStatus.Running, BoilerStateMachine.ResolveConditionState(100f), "100");
                Assert.Equal(BoilerStatus.MaintenanceDue, BoilerStateMachine.ResolveConditionState(60f), "60");
                Assert.Equal(BoilerStatus.Worn, BoilerStateMachine.ResolveConditionState(40f), "40");
                Assert.Equal(BoilerStatus.Critical, BoilerStateMachine.ResolveConditionState(20f), "20");
            });
        }

        private static void ProducesTests(TestRunner t)
        {
            t.Run("Running and the three condition bands produce power", () =>
            {
                Assert.True(BoilerStateMachine.Produces(BoilerStatus.Running), "Running");
                Assert.True(BoilerStateMachine.Produces(BoilerStatus.MaintenanceDue), "MaintenanceDue");
                Assert.True(BoilerStateMachine.Produces(BoilerStatus.Worn), "Worn");
                Assert.True(BoilerStateMachine.Produces(BoilerStatus.Critical), "Critical");
            });

            t.Run("All blocked states produce nothing", () =>
            {
                Assert.False(BoilerStateMachine.Produces(BoilerStatus.NeedsMaintenance), "NeedsMaintenance");
                Assert.False(BoilerStateMachine.Produces(BoilerStatus.NoFuel), "NoFuel");
                Assert.False(BoilerStateMachine.Produces(BoilerStatus.NoWater), "NoWater");
                Assert.False(BoilerStateMachine.Produces(BoilerStatus.Idle), "Idle");
                Assert.False(BoilerStateMachine.Produces(BoilerStatus.Paused), "Paused");
                Assert.False(BoilerStateMachine.Produces(BoilerStatus.Maintaining), "Maintaining");
            });
        }

        private static void EnumOrderTests(TestRunner t)
        {
            t.Run("BoilerStatus declaration order is the AD-12 display priority order", () =>
            {
                Assert.True((int)BoilerStatus.Paused < (int)BoilerStatus.Maintaining, "Paused first");
                Assert.True((int)BoilerStatus.Maintaining < (int)BoilerStatus.NoFuel, "Maintaining second");
                Assert.True((int)BoilerStatus.NoFuel < (int)BoilerStatus.NoWater, "NoFuel third");
                Assert.True((int)BoilerStatus.NoWater < (int)BoilerStatus.NeedsMaintenance, "NoWater fourth");
                Assert.True((int)BoilerStatus.NeedsMaintenance < (int)BoilerStatus.Critical, "NeedsMaintenance fifth");
                Assert.True((int)BoilerStatus.Critical < (int)BoilerStatus.Worn, "Critical sixth");
                Assert.True((int)BoilerStatus.Worn < (int)BoilerStatus.MaintenanceDue, "Worn seventh");
                Assert.True((int)BoilerStatus.MaintenanceDue < (int)BoilerStatus.Idle, "MaintenanceDue eighth");
                Assert.True((int)BoilerStatus.Idle < (int)BoilerStatus.Running, "Idle ninth");
            });
        }

        private static void PriorityMatrixTests(TestRunner t)
        {
            // REQ-CORE-06: at least eight combinations from CONTRACT_SEE §8.2 AD-12 item 4, each
            // asserted through the full Resolve() surface (state + badge).
            t.Run("Priority matrix 1: paused + no fuel -> Paused", () =>
                AssertResolved(BoilerStatus.Paused, "Status.SEE.Paused", paused: true, hasFuel: false));

            t.Run("Priority matrix 2: no fuel + no water -> NoFuel", () =>
                AssertResolved(BoilerStatus.NoFuel, "Status.SEE.NoFuel", hasFuel: false, hasWater: false));

            t.Run("Priority matrix 3: no water + needs maintenance -> NoWater", () =>
                AssertResolved(BoilerStatus.NoWater, "Status.SEE.NoWater", condition: 0f, hasWater: false));

            t.Run("Priority matrix 4: condition 15 + no demand -> Idle", () =>
                AssertResolved(BoilerStatus.Idle, "Status.SEE.Idle", condition: 15f, load: 0f));

            t.Run("Priority matrix 5: condition 15 + demand -> Critical", () =>
                AssertResolved(BoilerStatus.Critical, "Status.SEE.Critical", condition: 15f, load: 1f));

            t.Run("Priority matrix 6: normal boiler -> Running", () =>
                AssertResolved(BoilerStatus.Running, "Status.SEE.Running", condition: 100f, load: 1f));

            t.Run("Priority matrix 7: maintaining + no fuel -> Maintaining", () =>
                AssertResolved(BoilerStatus.Maintaining, "Status.SEE.Maintaining",
                    phase: MaintenancePhase.Maintaining, hasFuel: false));

            t.Run("Priority matrix 8: condition 0 + no fuel -> NoFuel; refuelled -> NeedsMaintenance", () =>
            {
                AssertResolved(BoilerStatus.NoFuel, "Status.SEE.NoFuel", condition: 0f, hasFuel: false);
                AssertResolved(BoilerStatus.NeedsMaintenance, "Status.SEE.NeedsMaintenance", condition: 0f);
            });
        }

        private static void ResolutionTests(TestRunner t)
        {
            t.Run("Resolve running at full load is multiplier 1.0 and rated output", () =>
            {
                var resolution = Resolve(condition: 80f, load: 1f);
                Assert.Equal(BoilerStatus.Running, resolution.Status, "status");
                Assert.Near(1.0, resolution.OutputMultiplier, 1e-6, "multiplier");
                Assert.Near(1.0, resolution.LoadRatio, 1e-6, "load");
                Assert.Equal(460, resolution.OutputHorsepower(460), "output");
            });

            t.Run("Resolve running at half load is multiplier 0.5", () =>
            {
                var resolution = Resolve(condition: 80f, load: 0.5f);
                Assert.Near(0.5, resolution.OutputMultiplier, 1e-6, "multiplier");
                Assert.Near(0.5, resolution.LoadRatio, 1e-6, "load");
                Assert.Equal(230, resolution.OutputHorsepower(460), "output");
            });

            t.Run("Resolve applies the condition curve to the multiplier", () =>
            {
                var worn = Resolve(condition: 30f, load: 1f);
                Assert.Equal(BoilerStatus.Worn, worn.Status, "worn status");
                Assert.Near(0.85, worn.OutputMultiplier, 1e-6, "worn multiplier");
                Assert.Equal(391, worn.OutputHorsepower(460), "worn output");

                var due = Resolve(condition: 45f, load: 1f);
                Assert.Equal(BoilerStatus.MaintenanceDue, due.Status, "due status");
                Assert.Near(0.93, due.OutputMultiplier, 1e-6, "due multiplier");
                Assert.Equal(428, due.OutputHorsepower(460), "due output");

                var critical = Resolve(condition: 10f, load: 1f);
                Assert.Equal(BoilerStatus.Critical, critical.Status, "critical status");
                Assert.Near(0.40, critical.OutputMultiplier, 1e-6, "critical multiplier");
                Assert.Equal(184, critical.OutputHorsepower(460), "critical output");
            });

            t.Run("Resolve blocked states have multiplier 0 and zero output", () =>
            {
                var idle = Resolve(condition: 80f, load: 0f);
                Assert.Equal(BoilerStatus.Idle, idle.Status, "idle");
                Assert.Near(0.0, idle.OutputMultiplier, 1e-6, "idle multiplier");
                Assert.Equal(0, idle.OutputHorsepower(460), "idle output");

                var noFuel = Resolve(condition: 80f, load: 1f, hasFuel: false);
                Assert.Near(0.0, noFuel.OutputMultiplier, 1e-6, "no fuel multiplier");
                Assert.Equal(0, noFuel.OutputHorsepower(460), "no fuel output");

                var needs = Resolve(condition: 0f, load: 1f);
                Assert.Near(0.0, needs.OutputMultiplier, 1e-6, "needs maintenance multiplier");
            });

            t.Run("Resolve clamps NaN demand to load 0", () =>
            {
                var resolution = Resolve(condition: 80f, load: float.NaN);
                Assert.Equal(BoilerStatus.Idle, resolution.Status, "status");
                Assert.Near(0.0, resolution.LoadRatio, 1e-6, "load");
            });
        }

        private static void BadgeAndReasonTests(TestRunner t)
        {
            t.Run("Every state maps to its frozen Status.SEE badge key", () =>
            {
                Assert.Equal("Status.SEE.Paused", BoilerStateMachine.BadgeKey(BoilerStatus.Paused), "paused");
                Assert.Equal("Status.SEE.Maintaining", BoilerStateMachine.BadgeKey(BoilerStatus.Maintaining), "maintaining");
                Assert.Equal("Status.SEE.NoFuel", BoilerStateMachine.BadgeKey(BoilerStatus.NoFuel), "no fuel");
                Assert.Equal("Status.SEE.NoWater", BoilerStateMachine.BadgeKey(BoilerStatus.NoWater), "no water");
                Assert.Equal("Status.SEE.NeedsMaintenance", BoilerStateMachine.BadgeKey(BoilerStatus.NeedsMaintenance), "needs");
                Assert.Equal("Status.SEE.Critical", BoilerStateMachine.BadgeKey(BoilerStatus.Critical), "critical");
                Assert.Equal("Status.SEE.Worn", BoilerStateMachine.BadgeKey(BoilerStatus.Worn), "worn");
                Assert.Equal("Status.SEE.MaintenanceDue", BoilerStateMachine.BadgeKey(BoilerStatus.MaintenanceDue), "due");
                Assert.Equal("Status.SEE.Idle", BoilerStateMachine.BadgeKey(BoilerStatus.Idle), "idle");
                Assert.Equal("Status.SEE.Running", BoilerStateMachine.BadgeKey(BoilerStatus.Running), "running");
            });

            t.Run("Every state maps to exactly one machine-readable reason", () =>
            {
                Assert.Equal(BoilerStatusReason.PlayerPaused, BoilerStateMachine.ReasonFor(BoilerStatus.Paused), "paused");
                Assert.Equal(BoilerStatusReason.MaintenanceRunning, BoilerStateMachine.ReasonFor(BoilerStatus.Maintaining), "maintaining");
                Assert.Equal(BoilerStatusReason.FuelEmpty, BoilerStateMachine.ReasonFor(BoilerStatus.NoFuel), "no fuel");
                Assert.Equal(BoilerStatusReason.WaterEmpty, BoilerStateMachine.ReasonFor(BoilerStatus.NoWater), "no water");
                Assert.Equal(BoilerStatusReason.ConditionZero, BoilerStateMachine.ReasonFor(BoilerStatus.NeedsMaintenance), "needs");
                Assert.Equal(BoilerStatusReason.ConditionCritical, BoilerStateMachine.ReasonFor(BoilerStatus.Critical), "critical");
                Assert.Equal(BoilerStatusReason.ConditionWorn, BoilerStateMachine.ReasonFor(BoilerStatus.Worn), "worn");
                Assert.Equal(BoilerStatusReason.MaintenanceDue, BoilerStateMachine.ReasonFor(BoilerStatus.MaintenanceDue), "due");
                Assert.Equal(BoilerStatusReason.NoNetworkDemand, BoilerStateMachine.ReasonFor(BoilerStatus.Idle), "idle");
                Assert.Equal(BoilerStatusReason.Loaded, BoilerStateMachine.ReasonFor(BoilerStatus.Running), "running");
            });

            t.Run("Badge keys all use the Status.SEE namespace", () =>
            {
                foreach (BoilerStatus status in System.Enum.GetValues(typeof(BoilerStatus)))
                {
                    Assert.Contains(BoilerStateMachine.BadgeKey(status), "Status.SEE.", status.ToString());
                }
            });
        }

        private static void AssertResolved(
            BoilerStatus expected,
            string expectedBadge,
            bool paused = false,
            MaintenancePhase phase = MaintenancePhase.None,
            float condition = 100f,
            bool hasFuel = true,
            bool hasWater = true,
            float load = 1f)
        {
            var resolution = Resolve(paused, phase, condition, hasFuel, hasWater, load);
            Assert.Equal(expected, resolution.Status, "status");
            Assert.Equal(expectedBadge, resolution.BadgeLocKey, "badge");
        }

        private static BoilerStateResolution Resolve(
            bool paused = false,
            MaintenancePhase phase = MaintenancePhase.None,
            float condition = 100f,
            bool hasFuel = true,
            bool hasWater = true,
            float load = 1f)
        {
            var input = new BoilerStateInput(
                isPaused: paused,
                maintenancePhase: phase,
                condition: condition,
                hasFuel: hasFuel,
                hasWater: hasWater,
                networkDemandRatio: load);
            return BoilerStateMachine.Resolve(in input);
        }

        private static BoilerStatus Evaluate(
            bool paused = false,
            MaintenancePhase phase = MaintenancePhase.None,
            float condition = 100f,
            bool hasFuel = true,
            bool hasWater = true,
            float load = 1f)
        {
            var input = new BoilerStateInput(
                isPaused: paused,
                maintenancePhase: phase,
                condition: condition,
                hasFuel: hasFuel,
                hasWater: hasWater,
                networkDemandRatio: load);
            return BoilerStateMachine.Evaluate(in input);
        }
    }
}
