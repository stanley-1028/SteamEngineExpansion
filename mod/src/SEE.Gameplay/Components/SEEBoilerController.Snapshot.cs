using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Formulas;
using Timberborn.InventorySystem;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Partial part: the read-only snapshot values P4B added to <see cref="IBoilerStatusView"/>
    /// (requirements §15.2/§15.4, REQ-UI-02/04).
    ///
    /// Every member here is display-only: the values are derived from already cached fields, the
    /// maintenance ledger or a slow (10 s) district-inventory snapshot, so a 4 Hz panel refresh
    /// neither touches a game component nor allocates. None of these fields feeds back into the
    /// fuel/water/wear/power settlement, so the P3 gameplay behaviour is unchanged.
    /// </summary>
    public sealed partial class SEEBoilerController
    {
        /// <summary>
        /// Refresh cadence of the panel-only material snapshot, in settlement ticks (1 tick = 1 s).
        /// The maintenance decision still performs its own live check.
        /// </summary>
        private const int MaterialSnapshotIntervalTicks = 10;

        private readonly List<string> _fuelGoods = new List<string>(2);
        private MaintenanceMaterialStatus[] _materialStatuses = Array.Empty<MaintenanceMaterialStatus>();
        private int _missingMaterialCount;
        private int _materialSnapshotTicks;
        private float _nextMaintenanceDay;
        private float _waterBeaverEquivalent;

        // P5 owns the detection wiring; until then the water-mode slots report "available".
        private WaterSupplyAvailability _waterSupplyAvailability = WaterSupplyAvailability.Available;
        private bool _pipeSupplyActive = false;
        private bool _beltSupplyActive = false;
        private bool _beltHasPower = true;

        /// <summary>
        /// Display-only "≈N beavers' daily drinking water" conversion of the rated water rate
        /// (requirements §15.2/§7.3). Shared with the panel through the read view; no gameplay
        /// value depends on it.
        /// </summary>
        /// <param name="waterPerHour">Rated water consumption in goods per game hour.</param>
        /// <returns>Beaver equivalents; 0 when the rate is unusable.</returns>
        private static float ComputeWaterBeaverEquivalent(float waterPerHour)
        {
            if (waterPerHour <= 0f || float.IsNaN(waterPerHour) || float.IsInfinity(waterPerHour))
            {
                return 0f;
            }

            float perDay = waterPerHour * (float)SEEConst.HoursPerDay / SEEConst.BeaverWaterPerDay;
            return float.IsNaN(perDay) || float.IsInfinity(perDay) ? 0f : perDay;
        }

        /// <summary>
        /// Display estimate of the game day the next maintenance is expected: current day plus the
        /// frozen wear formula at full load (requirements §9.2). It never feeds back into gameplay;
        /// the authoritative countdown is the maintenance ledger.
        /// </summary>
        /// <returns>Absolute game day, or 0 when maintenance is disabled/unknown.</returns>
        private float ComputeNextMaintenanceDay()
        {
            if (!_rules.Enabled || _rules.IntervalDays <= 0f)
            {
                return 0f;
            }

            double days = BoilerFormulas.DaysUntilMaintenance(_ledger.Condition, _rules.IntervalDays, 1f);
            if (double.IsNaN(days) || double.IsInfinity(days) || days < 0.0)
            {
                return 0f;
            }

            float day = CurrentDay() + (float)days;
            return float.IsNaN(day) || float.IsInfinity(day) || day < 0f ? 0f : day;
        }

        /// <summary>
        /// Rebuilds the panel-only material snapshot (requirements §15.2/REQ-UI-04): one row per
        /// required material with the district stock last observed. It never changes a gameplay
        /// decision (those still run their own live check) and is refreshed only every
        /// <see cref="MaterialSnapshotIntervalTicks"/> settlement ticks, so the read view stays a
        /// cached-value contract and the district inventory is not queried at panel refresh rate.
        /// </summary>
        private void RefreshMaintenanceMaterialSnapshot()
        {
            IReadOnlyList<MaintenanceMaterial> materials = _rules.Materials;
            if (!_rules.Enabled || materials.Count == 0)
            {
                _materialStatuses = Array.Empty<MaintenanceMaterialStatus>();
                _missingMaterialCount = 0;
                return;
            }

            try
            {
                DistrictInventoryRegistry? registry = ResolveInventoryRegistry();
                var statuses = new MaintenanceMaterialStatus[materials.Count];
                int missing = 0;
                for (int i = 0; i < materials.Count; i++)
                {
                    MaintenanceMaterial material = materials[i];
                    int have = registry != null ? CountUnreserved(registry, material.GoodId) : 0;
                    statuses[i] = new MaintenanceMaterialStatus(material.GoodId, material.Amount, have);
                    if (have < material.Amount)
                    {
                        missing++;
                    }
                }

                _materialStatuses = statuses;
                _missingMaterialCount = missing;
            }
            catch (Exception exception)
            {
                LogWarn("maintenance material snapshot failed (" + exception.GetType().Name + ")");
                _materialStatuses = Array.Empty<MaintenanceMaterialStatus>();
                _missingMaterialCount = 0;
            }
        }
    }
}
