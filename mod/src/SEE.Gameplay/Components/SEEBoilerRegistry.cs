using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Logging;
using Timberborn.MechanicalSystem;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Gameplay-side registration surface the controller uses itself. Kept separate from the frozen
    /// Core interfaces so the controller never depends on the UI contract for lifecycle
    /// bookkeeping (REQ-ARCH-03).
    /// </summary>
    public interface ISEEBoilerRegistry
    {
        /// <summary>Registers a finished boiler under its entity id.</summary>
        /// <param name="entityId">Entity id from <c>EntityComponent</c>.</param>
        /// <param name="controller">Controller to register.</param>
        void Register(Guid entityId, SEEBoilerController controller);

        /// <summary>Removes a boiler (deactivation/deletion).</summary>
        /// <param name="entityId">Entity id to remove.</param>
        void Unregister(Guid entityId);
    }

    /// <summary>
    /// Session singleton that answers the UI surfaces and drives the settlement batch:
    /// <see cref="IBoilerQuery"/> (read views), <see cref="IBoilerLogic"/> (per-boiler commands) and
    /// <see cref="ISEEBoilerRegistry"/> (lifecycle). One instance implements all three and is bound
    /// with <c>ToExisting</c>, so the UI query and the UI commands observe the same registry.
    ///
    /// One settlement pass is <c>O(n)</c> for the network aggregation plus <c>O(n)</c> for the
    /// per-boiler steps; there is no per-frame work, no LINQ and no allocation in steady state
    /// (SHARED_BRIEF C-1, REQ-SAFE-08). The aggregation exists because the D-1 λ formula needs the
    /// rated power and the previous output of every SEE boiler on the same mechanical graph.
    /// </summary>
    public sealed class SEEBoilerRegistry : IBoilerQuery, IBoilerLogic, ISEEBoilerRegistry
    {
        private sealed class GraphAggregate
        {
            internal MechanicalGraph? Graph;
            internal int RatedHp;
            internal int ActualHp;

            internal void Reset()
            {
                Graph = null;
                RatedHp = 0;
                ActualHp = 0;
            }
        }

        private readonly Dictionary<Guid, SEEBoilerController> _byId =
            new Dictionary<Guid, SEEBoilerController>(64);

        private readonly List<SEEBoilerController> _tickOrder = new List<SEEBoilerController>(64);
        private readonly List<GraphAggregate> _aggregates = new List<GraphAggregate>(4);
        private int _aggregateCount;
        private readonly SEERuntime _runtime;

        /// <summary>Creates the registry; the runtime supplies the shared throttled log.</summary>
        /// <param name="runtime">Gameplay runtime singleton.</param>
        public SEEBoilerRegistry(SEERuntime runtime)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        /// <summary>Number of currently active boilers; diagnostics and save export only.</summary>
        public int RegisteredCount => _tickOrder.Count;

        /// <summary>Number of settlement batches run (perf audit counter, O(1) per batch).</summary>
        internal long SettlementCount { get; private set; }

        /// <summary>Number of boilers settled by the last batch (perf audit counter).</summary>
        internal int LastSettlementBoilers { get; private set; }

        /// <summary>Stable registration-ordered controller list used by the save bridge.</summary>
        internal IReadOnlyList<SEEBoilerController> Controllers => _tickOrder;

        /// <inheritdoc />
        public void Register(Guid entityId, SEEBoilerController controller)
        {
            if (controller == null || entityId == Guid.Empty)
            {
                TryLogWarn("boiler registration skipped: empty entity id");
                return;
            }

            if (!_byId.ContainsKey(entityId))
            {
                _tickOrder.Add(controller);
            }

            _byId[entityId] = controller;
        }

        /// <inheritdoc />
        public void Unregister(Guid entityId)
        {
            if (entityId == Guid.Empty)
            {
                return;
            }

            if (_byId.TryGetValue(entityId, out SEEBoilerController? controller) && controller != null)
            {
                _byId.Remove(entityId);
                _tickOrder.Remove(controller);
            }
        }

        // ------------------------------------------------------------------ IBoilerQuery

        /// <inheritdoc />
        public bool TryGetView(Guid entityId, out IBoilerStatusView? view)
        {
            view = null;
            if (!TryFind(entityId, out SEEBoilerController? controller) || controller == null)
            {
                return false;
            }

            try
            {
                view = controller;
                return true;
            }
            catch (Exception exception)
            {
                // A broken boiler must only blank its own panel, never throw into the UI loop.
                TryLogError("boiler view failed; panel hides this boiler", exception);
                view = null;
                return false;
            }
        }

        // ------------------------------------------------------------------ IBoilerLogic

        /// <inheritdoc />
        public bool SetPaused(Guid entityId, bool paused)
        {
            return TryFind(entityId, out SEEBoilerController? controller)
                && controller != null
                && controller.SetPaused(entityId, paused);
        }

        /// <inheritdoc />
        public bool RequestMaintenanceNow(Guid entityId)
        {
            return TryFind(entityId, out SEEBoilerController? controller)
                && controller != null
                && controller.RequestMaintenanceNow(entityId);
        }

        /// <inheritdoc />
        public bool SetAutoMaintenance(Guid entityId, bool enabled)
        {
            return TryFind(entityId, out SEEBoilerController? controller)
                && controller != null
                && controller.SetAutoMaintenance(entityId, enabled);
        }

        /// <inheritdoc />
        public bool SetWaterSupplyMode(Guid entityId, WaterSupplyMode mode)
        {
            return TryFind(entityId, out SEEBoilerController? controller)
                && controller != null
                && controller.SetWaterSupplyMode(entityId, mode);
        }

        // ------------------------------------------------------------------ tick / save support

        /// <summary>
        /// Runs one settlement batch: first aggregates the rated/actual power of the SEE boilers per
        /// mechanical graph, then settles every registered boiler in stable registration order with
        /// the aggregate of its own graph. Every controller call is isolated (REQ-SAFE-02).
        /// </summary>
        /// <param name="gameHours">Game hours elapsed since the previous settlement.</param>
        internal void TickAll(double gameHours)
        {
            int count = _tickOrder.Count;
            if (count == 0)
            {
                return;
            }

            SettlementCount++;
            LastSettlementBoilers = count;
            PrepareAggregates();
            for (int i = 0; i < count; i++)
            {
                SEEBoilerController controller = _tickOrder[i];
                if (controller == null)
                {
                    continue;
                }

                MechanicalGraph? graph = controller.CurrentGraph;
                int ratedHp = 0;
                int actualHp = 0;
                int aggregate = FindAggregate(graph);
                if (aggregate >= 0)
                {
                    GraphAggregate entry = _aggregates[aggregate];
                    ratedHp = entry.RatedHp;
                    actualHp = entry.ActualHp;
                }

                try
                {
                    controller.TickBoilers(gameHours, ratedHp, actualHp);
                }
                catch (Exception exception)
                {
                    // The controller already isolates itself; this is the batch-level belt and braces.
                    TryLogError("boiler settlement failed", exception);
                }
            }
        }

        /// <summary>
        /// Rebuilds the per-graph aggregation of the previous settlement. Reuses its entry objects,
        /// so a steady-state batch allocates nothing.
        /// </summary>
        private void PrepareAggregates()
        {
            for (int i = 0; i < _aggregateCount; i++)
            {
                _aggregates[i].Reset();
            }

            _aggregateCount = 0;
            for (int i = 0; i < _tickOrder.Count; i++)
            {
                SEEBoilerController controller = _tickOrder[i];
                if (controller == null || !controller.IsActive)
                {
                    continue;
                }

                MechanicalGraph? graph = controller.CurrentGraph;
                int index = FindAggregate(graph);
                if (index < 0)
                {
                    index = _aggregateCount;
                    EnsureAggregate(index);
                    _aggregates[index].Reset();
                    _aggregates[index].Graph = graph;
                    _aggregateCount++;
                }

                _aggregates[index].RatedHp += controller.RatedOutput;
                _aggregates[index].ActualHp += controller.SettledOutput;
            }
        }

        private void EnsureAggregate(int index)
        {
            while (_aggregates.Count <= index)
            {
                _aggregates.Add(new GraphAggregate());
            }
        }

        private int FindAggregate(MechanicalGraph? graph)
        {
            for (int i = 0; i < _aggregateCount; i++)
            {
                if (ReferenceEquals(_aggregates[i].Graph, graph))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool TryFind(Guid entityId, out SEEBoilerController? controller)
        {
            if (entityId != Guid.Empty
                && _byId.TryGetValue(entityId, out SEEBoilerController? found)
                && found != null)
            {
                controller = found;
                return true;
            }

            controller = null;
            return false;
        }

        private void TryLogWarn(string message)
        {
            try
            {
                _runtime.Log.Warn(SeeLogKeys.QueryError, message);
            }
            catch (Exception)
            {
                // Diagnostics only.
            }
        }

        private void TryLogError(string message, Exception exception)
        {
            try
            {
                _runtime.Log.Error(SeeLogKeys.QueryError, message + " (" + exception.GetType().Name + ")");
            }
            catch (Exception)
            {
                // Diagnostics only.
            }
        }
    }
}
