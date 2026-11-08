using System;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Logging;
using SEE.Core.Maintenance;
using Timberborn.Buildings;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.InventorySystem;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Partial part: the automatic/manual maintenance flow of one boiler (requirements §9.4/§9.5,
    /// REQ-MAINT-01..13, D-5). The pure decisions live in <see cref="MaintenanceLedger"/>; this part
    /// supplies the two environment facts it deliberately does not touch: the district inventory
    /// (materials) and the district road network (accessibility).
    ///
    /// Discipline: materials are checked first and only then withdrawn atomically ("先驗後扣"), a
    /// missing material or road keeps the output running and consumes nothing, and
    /// <c>maintenance.enabled=false</c> leaves no half-applied state (the ledger resets itself).
    /// </summary>
    public sealed partial class SEEBoilerController
    {
        private MaintenanceAction _lastMaintenanceAction = MaintenanceAction.None;

        /// <summary>Runs one maintenance step: timer, decision and the atomic material withdrawal.</summary>
        /// <param name="hours">Game hours elapsed since the previous step.</param>
        private void StepMaintenance(double hours)
        {
            if (++_materialSnapshotTicks >= MaterialSnapshotIntervalTicks)
            {
                _materialSnapshotTicks = 0;
                RefreshMaintenanceMaterialSnapshot();
            }
            if (_rules.Enabled && _ledger.Phase == MaintenancePhase.Maintaining && !_paused)
            {
                _maintenance.AdvanceMaintenance(hours);
            }

            bool manual = _maintenanceRequested;
            bool manualDue = manual && _rules.Enabled && _maintenance.IsManualAvailable;
            bool autoDue = _rules.Enabled && !_paused
                           && (_ledger.Phase == MaintenancePhase.MissingMaterials
                               || _ledger.Phase == MaintenancePhase.NoAccess
                               || (_rules.Auto && _ledger.AutoMaintenance
                                   && _ledger.Condition <= _rules.AutoThreshold));

            // The district inventory and the navmesh are queried only when a decision can actually
            // start maintenance (cheap path for the 99.9% of the steps that do nothing).
            bool hasMaterials = true;
            bool hasAccess = true;
            if (manualDue || autoDue)
            {
                hasMaterials = HasMaintenanceMaterials();
                if (hasMaterials)
                {
                    hasAccess = HasMaintenanceAccess();
                }
            }

            MaintenanceDecision decision = manualDue
                ? _maintenance.RequestManual(hasMaterials, hasAccess)
                : _maintenance.Evaluate(hasMaterials, hasAccess, _paused);
            if (manual)
            {
                _maintenanceRequested = false;
            }

            ApplyMaintenanceDecision(decision);
        }

        private void ApplyMaintenanceDecision(in MaintenanceDecision decision)
        {
            switch (decision.Action)
            {
                case MaintenanceAction.Start:
                    if (TryConsumeMaintenanceMaterials())
                    {
                        _maintenance.BeginMaintenance(_rules.DurationHours);
                        TryLog(SeeLogKeys.MaintenanceStarted, "maintenance started");
                    }
                    else
                    {
                        // The stock changed between the pre-check and the withdrawal: stay blocked.
                        TryLog(SeeLogKeys.MaintenanceBlocked, "maintenance blocked: materials vanished");
                    }

                    break;

                case MaintenanceAction.Completed:
                    _maintenance.CompleteMaintenance(CurrentDay());
                    TryLog(SeeLogKeys.MaintenanceFinished, "maintenance finished; condition restored");
                    break;

                case MaintenanceAction.BlockedByMaterials:
                    if (_lastMaintenanceAction != MaintenanceAction.BlockedByMaterials)
                    {
                        TryLog(SeeLogKeys.MaintenanceBlocked, "maintenance blocked: missing materials");
                    }

                    break;

                case MaintenanceAction.BlockedByAccess:
                    if (_lastMaintenanceAction != MaintenanceAction.BlockedByAccess)
                    {
                        TryLog(SeeLogKeys.MaintenanceBlocked, "maintenance blocked: no road for materials");
                    }

                    break;
            }

            _lastMaintenanceAction = decision.Action;
        }

        /// <summary>True when every required material is available as unreserved district stock.</summary>
        private bool HasMaintenanceMaterials()
        {
            System.Collections.Generic.IReadOnlyList<MaintenanceMaterial> materials = _rules.Materials;
            if (materials.Count == 0)
            {
                return true;
            }

            DistrictInventoryRegistry? registry = ResolveInventoryRegistry();
            if (registry == null)
            {
                return false;
            }

            try
            {
                for (int i = 0; i < materials.Count; i++)
                {
                    MaintenanceMaterial material = materials[i];
                    if (material.Amount <= 0)
                    {
                        continue;
                    }

                    if (CountUnreserved(registry, material.GoodId) < material.Amount)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                LogWarn("district inventory query failed (" + exception.GetType().Name + ")");
                return false;
            }
        }

        /// <summary>
        /// Withdraws the maintenance materials atomically from the district stock (never from the
        /// boiler's own fuel/water inventories, requirements §9.5). Returns false without touching
        /// the stock when one item cannot be covered.
        /// </summary>
        private bool TryConsumeMaintenanceMaterials()
        {
            System.Collections.Generic.IReadOnlyList<MaintenanceMaterial> materials = _rules.Materials;
            if (materials.Count == 0)
            {
                return true;
            }

            DistrictInventoryRegistry? registry = ResolveInventoryRegistry();
            if (registry == null || !HasMaintenanceMaterials())
            {
                return false;
            }

            try
            {
                for (int i = 0; i < materials.Count; i++)
                {
                    MaintenanceMaterial material = materials[i];
                    if (material.Amount <= 0)
                    {
                        continue;
                    }

                    int remaining = material.Amount;
                    foreach (Inventory candidate in registry.ActiveInventoriesWithStock(material.GoodId))
                    {
                        if (candidate == null || IsOwnInventory(candidate))
                        {
                            continue;
                        }

                        int take = candidate.UnreservedAmountInStock(material.GoodId);
                        if (take <= 0)
                        {
                            continue;
                        }

                        if (take > remaining)
                        {
                            take = remaining;
                        }

                        var good = new GoodAmount(material.GoodId, take);
                        if (!candidate.HasUnreservedStock(good))
                        {
                            continue;
                        }

                        candidate.TakeExported(good);
                        remaining -= take;
                        if (remaining <= 0)
                        {
                            break;
                        }
                    }

                    if (remaining > 0)
                    {
                        LogWarn("maintenance material stock changed during withdrawal");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                LogWarn("maintenance material withdrawal failed (" + exception.GetType().Name + ")");
                return false;
            }
        }

        /// <summary>True when the boiler's pickup point lies on the district road network.</summary>
        private bool HasMaintenanceAccess()
        {
            try
            {
                BuildingAccessible? buildingAccessible = _buildingAccessible;
                if (buildingAccessible == null)
                {
                    return false;
                }

                var accessible = buildingAccessible.Accessible;
                if (accessible == null)
                {
                    return false;
                }

                DistrictCenter? districtCenter = ResolveDistrictCenter();
                return districtCenter != null && districtCenter.AccessibleIsOnDistrictRoad(accessible);
            }
            catch (Exception exception)
            {
                LogWarn("accessibility query failed (" + exception.GetType().Name + ")");
                return false;
            }
        }

        private bool IsOwnInventory(Inventory candidate)
        {
            return ReferenceEquals(candidate, _fuelInventory) || ReferenceEquals(candidate, _waterInventory);
        }

        private int CountUnreserved(DistrictInventoryRegistry registry, string goodId)
        {
            int total = 0;
            foreach (Inventory candidate in registry.ActiveInventoriesWithStock(goodId))
            {
                if (candidate == null || IsOwnInventory(candidate))
                {
                    continue;
                }

                total += candidate.UnreservedAmountInStock(goodId);
            }

            return total;
        }

        private DistrictCenter? ResolveDistrictCenter()
        {
            DistrictBuilding? districtBuilding = _districtBuilding;
            if (districtBuilding == null)
            {
                return null;
            }

            DistrictCenter? center = districtBuilding.District;
            if (center == null)
            {
                center = districtBuilding.InstantDistrict;
            }

            return center;
        }

        private DistrictInventoryRegistry? ResolveInventoryRegistry()
        {
            try
            {
                DistrictCenter? center = ResolveDistrictCenter();
                return center != null ? center.GetComponent<DistrictInventoryRegistry>() : null;
            }
            catch (Exception exception)
            {
                LogWarn("district inventory registry unavailable (" + exception.GetType().Name + ")");
                return null;
            }
        }

        private void TryLog(string key, string message)
        {
            try
            {
                _runtime.Log.Info(key, message + "|" + _tier);
            }
            catch (Exception)
            {
                // Diagnostics only.
            }
        }
    }
}
