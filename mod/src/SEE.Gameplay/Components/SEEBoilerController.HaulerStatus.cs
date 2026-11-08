using System;
using Timberborn.GameDistricts;
using Timberborn.Hauling;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Partial part: hides the vanilla "requires haulers" status on a worker-free boiler
    /// (gameplay fix 2026-11-08, player report: the localized "requires haulers" message shown on
    /// finished steam engines).
    ///
    /// Root cause (proven from game IL): every building whose blueprint has a
    /// <c>GoodConsumingBuildingSpec</c> receives the vanilla
    /// <c>GoodConsumingBuildingStatusInitializer</c>. Its <c>OnEnterFinishedState</c> calls
    /// <c>Timberborn.Hauling.NoHaulingPostStatus.Initialize(...)</c> whenever the entity has no
    /// <c>Workplace</c> component. A steam boiler is deliberately worker-free (no WorkplaceSpec; the
    /// engine is supply-agnostic and is fed by belts or by haulers into its public input
    /// inventories), so the initializer treats the boiler as a hauling-post-less consumer and shows
    /// the status <c>Status.Hauling.NoHaulingPost</c> even though neither haulers nor a hauling post
    /// are required for the engine to work.
    ///
    /// Vanilla provides a public opt-out, so no runtime code patching and no reflection is needed:
    /// <c>NoHaulingPostStatus.Disable()</c> removes the
    /// <c>HaulCandidate.InHaulingCenterRangeChanged</c> handler and deactivates the status toggle.
    /// The vanilla initializer runs its <c>OnEnterFinishedState</c> at finish time and listener
    /// ordering relative to this controller is unspecified, so the controller disables the status on
    /// the next settlement tick(s) instead of exactly once. The only remaining re-activation path is
    /// <c>DistrictBuilding.ReassignedDistrict</c>: <c>Disable()</c> keeps that handler subscribed and
    /// its <c>UpdateStatus()</c> activates the toggle again. The controller therefore subscribes to
    /// that event, re-arms the deferred suppression and disables the status again on the next
    /// settlement tick. The few-tick window covers a reassignment whose vanilla listener runs after
    /// ours in the same callback list, and it stays O(1): all work happens on activation and on the
    /// settlement tick, never per frame.
    ///
    /// Everything is exception-isolated (REQ-SAFE-02): a missing component or a throwing vanilla API
    /// degrades to "the status may stay visible", never to a broken boiler.
    /// </summary>
    public sealed partial class SEEBoilerController
    {
        /// <summary>Number of settlement steps the deferred re-suppression stays armed.</summary>
        private const int HaulerStatusSuppressionTicks = 3;

        /// <summary>Vanilla status component resolved from the entity; null while unavailable.</summary>
        private NoHaulingPostStatus? _noHaulingPostStatus;

        /// <summary>Remaining settlement steps that still call the vanilla <c>Disable()</c> opt-out.</summary>
        private int _haulerStatusSuppressionTicks;

        /// <summary>District building whose <c>ReassignedDistrict</c> event is currently subscribed.</summary>
        private DistrictBuilding? _haulerStatusDistrictBuilding;

        /// <summary>
        /// Resolves the vanilla status component once it exists on the entity and subscribes to the
        /// district reassignment event (once per entity). Called on activation and on every settlement
        /// tick until both are resolved; afterwards it is only two null checks.
        /// </summary>
        private void ResolveHaulerStatusSuppression()
        {
            if (_noHaulingPostStatus == null)
            {
                try
                {
                    if (HasComponent<NoHaulingPostStatus>())
                    {
                        _noHaulingPostStatus = GetComponent<NoHaulingPostStatus>();
                        // The vanilla initializer may run before or after this controller; arm the
                        // deferred disable so the status disappears on the next settlement even when
                        // Initialize() runs after activation.
                        _haulerStatusSuppressionTicks = HaulerStatusSuppressionTicks;
                    }
                }
                catch (Exception exception)
                {
                    LogWarn("hauling status component could not be resolved ("
                            + exception.GetType().Name + ")");
                }
            }

            if (_haulerStatusDistrictBuilding != null)
            {
                return;
            }

            try
            {
                DistrictBuilding? districtBuilding = _districtBuilding;
                if (districtBuilding == null && HasComponent<DistrictBuilding>())
                {
                    districtBuilding = GetComponent<DistrictBuilding>();
                }

                if (districtBuilding != null)
                {
                    districtBuilding.ReassignedDistrict += OnHaulerStatusDistrictReassigned;
                    _haulerStatusDistrictBuilding = districtBuilding;
                }
            }
            catch (Exception exception)
            {
                // A missed subscription can only make the warning re-appear after a district change;
                // never a reason to break activation.
                LogWarn("hauler status district subscription failed ("
                        + exception.GetType().Name + ")");
            }
        }

        /// <summary>
        /// Disables the vanilla status for the remaining deferred settlement steps; never throws.
        /// Called from the settlement tick after <see cref="ResolveHaulerStatusSuppression"/>.
        /// </summary>
        private void ApplyHaulerStatusSuppression()
        {
            if (_haulerStatusSuppressionTicks <= 0 || _noHaulingPostStatus == null)
            {
                return;
            }

            try
            {
                _noHaulingPostStatus.Disable();
            }
            catch (Exception exception)
            {
                LogWarn("hauling status could not be disabled (" + exception.GetType().Name + ")");
                _haulerStatusSuppressionTicks = 0;
                return;
            }

            _haulerStatusSuppressionTicks--;
        }

        /// <summary>
        /// Re-arms the deferred suppression after a district reassignment: the vanilla
        /// <c>NoHaulingPostStatus.UpdateStatus()</c> stays subscribed to <c>DistrictBuilding</c> after
        /// <c>Disable()</c> and may have re-activated the status toggle, so the next settlement
        /// disables it again.
        /// </summary>
        private void OnHaulerStatusDistrictReassigned(object? sender, EventArgs e)
        {
            if (_noHaulingPostStatus != null)
            {
                _haulerStatusSuppressionTicks = HaulerStatusSuppressionTicks;
            }
        }

        /// <summary>
        /// Drops the vanilla status reference and the district subscription when the building leaves
        /// the finished state (reverted, deleted or unloaded); never throws.
        /// </summary>
        private void ReleaseHaulerStatusSuppression()
        {
            DistrictBuilding? districtBuilding = _haulerStatusDistrictBuilding;
            _haulerStatusDistrictBuilding = null;
            if (districtBuilding != null)
            {
                try
                {
                    districtBuilding.ReassignedDistrict -= OnHaulerStatusDistrictReassigned;
                }
                catch (Exception exception)
                {
                    // Diagnostics only: a missed unsubscribe is harmless because the event and this
                    // controller share the entity lifetime.
                    LogWarn("hauler status unsubscribe failed (" + exception.GetType().Name + ")");
                }
            }

            _noHaulingPostStatus = null;
            _haulerStatusSuppressionTicks = 0;
        }
    }
}
