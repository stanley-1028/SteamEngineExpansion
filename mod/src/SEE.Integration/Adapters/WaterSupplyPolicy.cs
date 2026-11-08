using SEE.Core.Abstractions;

namespace SEE.Integration.Adapters
{
    /// <summary>
    /// One resolved water-supply decision (requirements §7.7/§10.3.1): the stored intent, the mode
    /// that actually runs and the reason surfaced to the player. The intent is never modified, so a
    /// save cannot flip-flop between modes (REQ-SAVE-08).
    /// </summary>
    public sealed class WaterSupplyDecision
    {
        /// <summary>Creates a decision.</summary>
        /// <param name="intent">Stored player intent.</param>
        /// <param name="effective">Mode that runs now.</param>
        /// <param name="reasonKey">Stable reason key for the panel/log.</param>
        /// <param name="intentHonored">True when the intent mode is exactly the effective mode.</param>
        public WaterSupplyDecision(WaterSupplyMode intent, WaterSupplyMode effective, string reasonKey, bool intentHonored)
        {
            Intent = intent;
            Effective = effective;
            ReasonKey = reasonKey;
            IntentHonored = intentHonored;
        }

        /// <summary>Stored player intent (persisted per entity by the gameplay controller).</summary>
        public WaterSupplyMode Intent { get; }

        /// <summary>Mode that runs now; never <see cref="WaterSupplyMode.Auto"/>.</summary>
        public WaterSupplyMode Effective { get; }

        /// <summary>Stable reason key explaining the effective mode.</summary>
        public string ReasonKey { get; }

        /// <summary>True when the stored intent could be honoured exactly.</summary>
        public bool IntentHonored { get; }

        /// <summary>True when the player must be told that the requested mode is unavailable (REQ-COMPAT-06).</summary>
        public bool NeedsPlayerNotice => !IntentHonored;

        /// <summary>True when the effective mode is the last-resort hauler path.</summary>
        public bool IsFallback => Effective == WaterSupplyMode.Hauler;
    }

    /// <summary>
    /// Pure water-supply policy of the Timber Pipes adapter (requirements §7.7/§10.3.1,
    /// REQ-COMPAT-06, REQ-FUEL-15). It only decides the mode: SEE never injects external blueprints
    /// and never replaces the vanilla liquid <c>StockpileSpec</c>, so every mode stays playable.
    /// <para>
    /// The <c>tank</c> mode is gated on the two pending manual gates (V-01 auto-fill and V-TP2
    /// volume accuracy). Until P8 confirms them, detection of Timber Pipes resolves <c>auto</c> to
    /// <c>valve</c>, never <c>tank</c> — this is the PLAN §8 risk-2 mitigation and part of the
    /// requirement's own degradation order.
    /// </para>
    /// </summary>
    public static class WaterSupplyPolicy
    {
        /// <summary>Reason key: auto resolved to tank (both tank gates verified).</summary>
        public const string AutoTankReason = "auto-tank";

        /// <summary>Reason key: auto resolved to valve because tank gates are unverified.</summary>
        public const string AutoValveReason = "auto-valve:tp-evidence-pending";

        /// <summary>Reason key: auto resolved to hauler because Timber Pipes is absent.</summary>
        public const string AutoHaulerNoPipesReason = "auto-hauler:no-timber-pipes";

        /// <summary>Reason key: auto resolved to hauler because v0.1.1 has no liquid water stockpile.</summary>
        public const string AutoHaulerNoLiquidTankReason = "auto-hauler:no-liquid-tank-v011";

        /// <summary>Reason key: auto resolved to hauler because detection degraded.</summary>
        public const string AutoHaulerDegradedReason = "auto-hauler:integration-degraded";

        /// <summary>Reason key: a player-requested tank mode is unavailable and the run falls back to hauler.</summary>
        public const string IntentTankUnavailableReason = "intent-tank-unavailable:fallback-hauler";

        /// <summary>Reason key: a player-requested valve mode is unavailable and the run falls back to hauler.</summary>
        public const string IntentValveUnavailableReason = "intent-valve-unavailable:fallback-hauler";

        /// <summary>Reason key: a player-requested hauler mode.</summary>
        public const string IntentHaulerReason = "intent-hauler";

        /// <summary>Reason key: a verified tank mode (V-01/V-TP2 passed).</summary>
        public const string IntentTankReason = "intent-tank";

        /// <summary>Reason key: valve mode (Timber Pipes available, tank evidence not passed).</summary>
        public const string IntentValveReason = "intent-valve";

        /// <summary>
        /// V-01/V-TP1 status: a vanilla liquid Stockpile on a custom building is auto-filled by the
        /// Timber Pipes network. Not manually verified yet (P8 gate), so <c>tank</c> is not offered.
        /// </summary>
        public const bool TankAutoFillVerified = false;

        /// <summary>
        /// V-TP2 status: the PipeTank volume calculation including fuel goods is within tolerance.
        /// The offline mix-error gate passes (≤ 10.5%), but the runtime calculation is unverified.
        /// </summary>
        public const bool TankMixVolumeVerified = false;

        /// <summary>True when both tank-mode manual gates passed; drives the UI capability flag.</summary>
        public static bool TankModeAvailable => TankAutoFillVerified && TankMixVolumeVerified;

        /// <summary>Resolves the effective mode for one boiler (or the global default intent).</summary>
        /// <param name="intent">Stored intent; <see cref="WaterSupplyMode.Auto"/> when unset.</param>
        /// <param name="timberPipesAvailable">True when the Timber Pipes assembly and anchors resolved.</param>
        /// <param name="timberPipesDegraded">True when the assembly exists but an anchor/version failed.</param>
        /// <returns>The decision; never null.</returns>
        public static WaterSupplyDecision Resolve(
            WaterSupplyMode intent,
            bool timberPipesAvailable,
            bool timberPipesDegraded)
        {
            if (intent == WaterSupplyMode.Hauler)
            {
                return new WaterSupplyDecision(intent, WaterSupplyMode.Hauler, IntentHaulerReason, true);
            }

            if (intent == WaterSupplyMode.Tank)
            {
                if (timberPipesAvailable && TankModeAvailable)
                {
                    return new WaterSupplyDecision(intent, WaterSupplyMode.Tank, IntentTankReason, true);
                }

                // Player asked for tank but it cannot run: show why and run haulers (never silent).
                return new WaterSupplyDecision(
                    intent, WaterSupplyMode.Hauler, IntentTankUnavailableReason, false);
            }

            if (intent == WaterSupplyMode.Valve)
            {
                // A valve only fills a liquid Stockpile; the SEE water tank is a code-created public
                // input Inventory (2026-11-08 fix), so the mode degrades to haulers until v0.2 offers
                // a pipe-facing tank.
                return new WaterSupplyDecision(
                    intent, WaterSupplyMode.Hauler, IntentValveUnavailableReason, false);
            }

            // Auto: the water tank is a dedicated public input Inventory, so haulers are the only
            // working mode. Tank/valve return in v0.2 together with a pipe-facing tank.
            if (!timberPipesAvailable)
            {
                string reason = timberPipesDegraded ? AutoHaulerDegradedReason : AutoHaulerNoPipesReason;
                return new WaterSupplyDecision(WaterSupplyMode.Auto, WaterSupplyMode.Hauler, reason, true);
            }

            return new WaterSupplyDecision(
                WaterSupplyMode.Auto, WaterSupplyMode.Hauler, AutoHaulerNoLiquidTankReason, true);
        }
    }
}
