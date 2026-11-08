using System;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Formulas;
using SEE.Core.Logging;
using SEE.Core.State;
using Timberborn.MechanicalSystem;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Partial part: the guarded settlement step of one boiler (CONTRACT_SEE §8.1). The only entry
    /// point is <see cref="TickBoilers"/>, called once per settlement batch by
    /// <c>BoilerTickSystem</c>/<c>SEEBoilerRegistry</c>, so the whole boiler family has one O(1)
    /// path per boiler and never a per-frame one.
    ///
    /// Order inside one step (requirements §13.5, Lead ruling AD-12):
    /// <list type="number">
    /// <item>resolve the deferred post-load state (REQ-SAVE-12);</item>
    /// <item>refresh the two vanilla inventories and run the maintenance state machine;</item>
    /// <item>compute the network load λ with the anti-feedback formula (D-1), resolve the state and
    /// write <c>node.SetOutputMultiplier</c> (case A);</item>
    /// <item>withdraw fuel/water and accumulate wear only while loaded (AD-12 behaviour gate);</item>
    /// <item>publish the cached read view.</item>
    /// </list>
    /// </summary>
    public sealed partial class SEEBoilerController
    {
        /// <summary>Runs one settlement step; never throws into the game loop (REQ-SAFE-02/13).</summary>
        /// <param name="gameHours">Game hours elapsed since the previous step.</param>
        /// <param name="networkRatedHp">Σ rated hp of the SEE boilers on the same mechanical graph.</param>
        /// <param name="networkActualHp">Σ actual output of those boilers before this step.</param>
        internal void TickBoilers(double gameHours, int networkRatedHp, int networkActualHp)
        {
            try
            {
                if (!_activated)
                {
                    return;
                }

                if (double.IsNaN(gameHours) || double.IsInfinity(gameHours) || gameHours <= 0.0)
                {
                    return;
                }

                TryResolveComponents();
                EnsureVanillaConsumptionPaused();
                EnableWaterInventory();
                WarnIfWaterTankMissing();
                if (_applyAfterLoad)
                {
                    ApplyDeferredAfterLoad();
                }

                RefreshStock();
                StepMaintenance(gameHours);

                if (!_componentsResolved)
                {
                    // Dedicated inventories not ready yet: stay idle and retry next settlement.
                    WriteMultiplier(0f);
                    _loadRatio = 0f;
                    _actualOutput = 0;
                    UpdateViewState();
                    return;
                }

                ResolvePowerAndState(networkRatedHp, networkActualHp);
                bool loaded = BoilerStateMachine.IsLoaded(_status, _loadRatio) && _actualOutput > 0;
                if (loaded)
                {
                    SettleSupplies(gameHours);
                }
                else
                {
                    ClampCarries();
                }

                RefreshStock();
                if (BoilerStateMachine.Produces(_status) && (!_hasFuel || !_hasWater))
                {
                    // The step ran out of supplies: the requirement turns this into NoFuel/NoWater
                    // output 0 immediately (requirements §13.5 step 7).
                    WriteMultiplier(0f);
                }

                UpdateViewState();
            }
            catch (Exception exception)
            {
                // A single damaged boiler must not stop the other boilers or the game loop
                // (REQ-SAFE-02); the throttled log bounds the noise.
                LogError("boiler tick failed; keeping the previous state", exception);
            }
        }

        /// <summary>
        /// One-shot diagnostic (2026-11-08 hauling fix): the dedicated water tank inventory is
        /// created by the boiler template module, so a missing one means the decorator wiring is
        /// broken. The boiler keeps running (NoWater state) and this warning names the cause once.
        /// </summary>
        private void WarnIfWaterTankMissing()
        {
            if (_componentsResolved && _waterInventory == null && !_waterTankWarningLogged)
            {
                _waterTankWarningLogged = true;
                LogWarn("water tank inventory was not created; boiler will report NoWater");
            }
        }

        /// <summary>Re-applies the non-persisted runtime state after a load (REQ-SAVE-12).</summary>
        private void ApplyDeferredAfterLoad()
        {
            _applyAfterLoad = false;
            _nodeMultiplier = float.NaN;
            EnsureVanillaConsumptionPaused();
            EnableWaterInventory();
        }

        /// <summary>
        /// Resolves the network load λ and the boiler state, then writes the node multiplier.
        ///
        /// D-1 anti-feedback formula (AD-9):
        /// <c>λ = clamp01(PowerDemand / max(1, S_other + ΣN_SEE))</c> with
        /// <c>S_other = max(0, PowerSupply - Σ actual of the SEE boilers on this graph)</c>.
        /// The denominator uses rated power, so our own output reduction can never raise λ again.
        ///
        /// 2026-11-08 hotfix-3: the final load is floored at <c>balance.standbyLoadRatio</c>.
        /// Outside working hours the worker buildings call
        /// <c>MechanicalBuilding.SetConsumptionDisabled(true)</c>, so <c>PowerDemand</c> becomes 0 and
        /// the raw λ would stop every supplied boiler (Idle). The standby floor keeps it running at
        /// a low load instead, so the engine never stops while fuel and water remain. A detached
        /// node (no graph) keeps the raw λ, because running without a grid would only burn supplies.
        /// </summary>
        private void ResolvePowerAndState(int networkRatedHp, int networkActualHp)
        {
            int demand = 0;
            int supply = 0;
            bool hasGraph = false;
            MechanicalNode? node = _mechanicalNode;
            if (node != null)
            {
                try
                {
                    MechanicalGraph? graph = node.Graph;
                    if (graph != null)
                    {
                        hasGraph = true;
                        demand = graph.PowerDemand;
                        supply = graph.PowerSupply;
                        _networkEfficiency = graph.PowerEfficiency;
                    }
                    else
                    {
                        _networkEfficiency = 0f;
                    }
                }
                catch (Exception)
                {
                    demand = 0;
                    supply = 0;
                    _networkEfficiency = 0f;
                }
            }
            else
            {
                _networkEfficiency = 0f;
            }

            _networkSupplyHp = supply;
            _networkDemandHp = demand;

            float demandLambda = 0f;
            if (demand > 0)
            {
                int otherSupply = supply - networkActualHp;
                if (otherSupply < 0)
                {
                    otherSupply = 0;
                }

                int denominator = otherSupply + networkRatedHp;
                if (denominator < 1)
                {
                    denominator = 1;
                }

                demandLambda = BoilerFormulas.Clamp01((float)demand / denominator);
            }

            // 2026-11-08 hotfix-3: never stop a supplied boiler just because the network demands
            // nothing (night). The configured standby ratio is the minimum running load.
            float lambda = hasGraph
                ? BoilerFormulas.OperationalLoadRatio(demandLambda, _standbyLoadRatio)
                : demandLambda;

            _loadRatio = lambda;
            var input = new BoilerStateInput(
                isPaused: _paused,
                maintenancePhase: _ledger.Phase,
                condition: _ledger.Condition,
                hasFuel: _hasFuel,
                hasWater: _hasWater,
                networkDemandRatio: lambda);
            _status = BoilerStateMachine.Evaluate(in input);
            _conditionMultiplier = ConditionCurve.Multiplier(_ledger.Condition);

            float multiplier = BoilerStateMachine.Produces(_status)
                ? BoilerFormulas.Clamp01(lambda * _conditionMultiplier)
                : 0f;
            WriteMultiplier(multiplier);
            _actualOutput = multiplier > 0f
                ? BoilerFormulas.OutputHorsepower(_ratedHorsepower, multiplier)
                : 0;
        }

        /// <summary>
        /// Writes the node multiplier only when it changes (case A, D-1). The game setter updates the
        /// node actuals immediately and does no work when the value is unchanged.
        /// </summary>
        private void WriteMultiplier(float multiplier)
        {
            MechanicalNode? node = _mechanicalNode;
            if (node == null)
            {
                _nodeMultiplier = 0f;
                return;
            }

            float clamped = multiplier > 1f ? 1f : (multiplier < 0f ? 0f : multiplier);
            if (clamped == _nodeMultiplier)
            {
                return;
            }

            try
            {
                node.SetOutputMultiplier(clamped);
                _nodeMultiplier = clamped;
            }
            catch (Exception exception)
            {
                LogWarn("output multiplier write failed (" + exception.GetType().Name + ")");
            }
        }
    }
}
