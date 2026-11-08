using System;
using SEE.Core.Abstractions;
using SEE.Integration.Adapters;

namespace SEE.Tests
{
    /// <summary>
    /// Decision table of the water-supply policy (requirements §7.7/§10.3.1, REQ-COMPAT-06,
    /// REQ-FUEL-15): manual intent wins, an unavailable intent degrades to haulers with a visible
    /// reason, and <c>auto</c> follows the documented order. The tank gates stay closed until P8
    /// verifies V-01/V-TP2, so detection alone never advertises tank mode.
    /// </summary>
    internal static class WaterSupplyPolicyTests
    {
        internal static void Run(TestRunner t)
        {
            AutoResolutionTests(t);
            IntentTests(t);
            FieldTests(t);
        }

        private static void AutoResolutionTests(TestRunner t)
        {
            t.Run("Water auto: no Timber Pipes -> hauler", () =>
            {
                WaterSupplyDecision decision = WaterSupplyPolicy.Resolve(WaterSupplyMode.Auto, false, false);
                Assert.Equal(WaterSupplyMode.Hauler, decision.Effective, "effective");
                Assert.Equal(WaterSupplyPolicy.AutoHaulerNoPipesReason, decision.ReasonKey, "reason");
                Assert.True(decision.IntentHonored, "auto is always honoured");
            });

            t.Run("Water auto: degraded Timber Pipes -> hauler with its own reason", () =>
            {
                WaterSupplyDecision decision = WaterSupplyPolicy.Resolve(WaterSupplyMode.Auto, false, true);
                Assert.Equal(WaterSupplyMode.Hauler, decision.Effective, "effective");
                Assert.Equal(WaterSupplyPolicy.AutoHaulerDegradedReason, decision.ReasonKey, "reason");
            });

            t.Run("Water auto: Timber Pipes available but v0.1.1 has no liquid tank -> hauler", () =>
            {
                WaterSupplyDecision decision = WaterSupplyPolicy.Resolve(WaterSupplyMode.Auto, true, false);
                Assert.Equal(WaterSupplyMode.Hauler, decision.Effective, "effective");
                Assert.Equal(WaterSupplyPolicy.AutoHaulerNoLiquidTankReason, decision.ReasonKey, "reason");
            });
        }

        private static void IntentTests(TestRunner t)
        {
            t.Run("Water intent: tank without Timber Pipes falls back to hauler and is visible", () =>
            {
                WaterSupplyDecision decision = WaterSupplyPolicy.Resolve(WaterSupplyMode.Tank, false, false);
                Assert.Equal(WaterSupplyMode.Tank, decision.Intent, "intent preserved");
                Assert.Equal(WaterSupplyMode.Hauler, decision.Effective, "effective");
                Assert.True(decision.NeedsPlayerNotice, "player notice");
                Assert.Equal(WaterSupplyPolicy.IntentTankUnavailableReason, decision.ReasonKey, "reason");
            });

            t.Run("Water intent: valve without Timber Pipes falls back to hauler and is visible", () =>
            {
                WaterSupplyDecision decision = WaterSupplyPolicy.Resolve(WaterSupplyMode.Valve, false, true);
                Assert.Equal(WaterSupplyMode.Hauler, decision.Effective, "effective");
                Assert.True(decision.NeedsPlayerNotice, "player notice");
                Assert.Equal(WaterSupplyPolicy.IntentValveUnavailableReason, decision.ReasonKey, "reason");
            });

            t.Run("Water intent: valve with Timber Pipes available still falls back to hauler (v0.1.1)", () =>
            {
                WaterSupplyDecision decision = WaterSupplyPolicy.Resolve(WaterSupplyMode.Valve, true, false);
                Assert.Equal(WaterSupplyMode.Hauler, decision.Effective, "effective");
                Assert.True(decision.NeedsPlayerNotice, "player notice");
                Assert.Equal(WaterSupplyPolicy.IntentValveUnavailableReason, decision.ReasonKey, "reason");
            });

            t.Run("Water intent: hauler is always honoured", () =>
            {
                Assert.Equal(WaterSupplyMode.Hauler,
                    WaterSupplyPolicy.Resolve(WaterSupplyMode.Hauler, true, false).Effective, "with TP");
                Assert.Equal(WaterSupplyMode.Hauler,
                    WaterSupplyPolicy.Resolve(WaterSupplyMode.Hauler, false, false).Effective, "without TP");
            });

            t.Run("Water intent: tank cannot be selected while the P8 evidence gate stays closed", () =>
            {
                WaterSupplyDecision decision = WaterSupplyPolicy.Resolve(WaterSupplyMode.Tank, true, false);
                Assert.Equal(WaterSupplyMode.Hauler, decision.Effective, "tank gated");
                Assert.False(WaterSupplyPolicy.TankModeAvailable, "V-01/V-TP2 gate");
            });
        }

        private static void FieldTests(TestRunner t)
        {
            t.Run("Water decision: fallback flag mirrors the hauler endpoint", () =>
            {
                Assert.True(WaterSupplyPolicy.Resolve(WaterSupplyMode.Auto, false, false).IsFallback, "fallback");
                Assert.True(WaterSupplyPolicy.Resolve(WaterSupplyMode.Valve, true, false).IsFallback,
                    "v0.1.1 valve falls back to hauler");
            });

            t.Run("Water decision: reason keys are stable machine-readable identifiers", () =>
            {
                string[] reasons =
                {
                    WaterSupplyPolicy.AutoTankReason,
                    WaterSupplyPolicy.AutoValveReason,
                    WaterSupplyPolicy.AutoHaulerNoPipesReason,
                    WaterSupplyPolicy.AutoHaulerDegradedReason,
                    WaterSupplyPolicy.AutoHaulerNoLiquidTankReason,
                    WaterSupplyPolicy.IntentTankUnavailableReason,
                    WaterSupplyPolicy.IntentValveUnavailableReason,
                    WaterSupplyPolicy.IntentHaulerReason,
                    WaterSupplyPolicy.IntentTankReason,
                    WaterSupplyPolicy.IntentValveReason
                };
                foreach (string reason in reasons)
                {
                    Assert.True(reason.Length > 0, "non-empty reason");
                    Assert.False(reason.Contains(" "), "no spaces in reason key: " + reason);
                }
            });
        }
    }
}
