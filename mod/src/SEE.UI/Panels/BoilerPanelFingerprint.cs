using System;
using SEE.Core.Abstractions;
using SEE.UI.Status;

namespace SEE.UI.Panels
{
    /// <summary>
    /// Cheap value fingerprint of everything the panel prints (requirements §15.7, REQ-OPS-02).
    /// The fragment re-reads the view at most <c>perf.uiRefreshHz</c> times per second and only
    /// rebuilds localized strings when this value changed; a fingerprint collision merely costs one
    /// skipped refresh, never a wrong value.
    ///
    /// Every quantity is rounded exactly like the formatter rounds it (condition/percent whole,
    /// rates and hours one or two decimals), otherwise a visible change could hide inside the
    /// fingerprint's coarser grid.
    /// </summary>
    public static class BoilerPanelFingerprint
    {
        /// <summary>Computes the fingerprint of one view at the current display precision.</summary>
        /// <param name="view">Read-only view; null yields 0.</param>
        /// <param name="options">Panel options (the beaver row participates when enabled).</param>
        /// <returns>A 64-bit value that changes whenever a rendered value changes.</returns>
        public static long Compute(IBoilerStatusView? view, BoilerPanelOptions? options)
        {
            if (view == null)
            {
                return 0L;
            }

            unchecked
            {
                long hash = 17L;
                hash = hash * 31 + (int)view.Status;
                hash = hash * 31 + (int)view.MaintenancePhase;
                hash = hash * 31 + (int)view.Tier;
                hash = hash * 31 + (int)view.Size;
                hash = hash * 31 + (view.IsFinished ? 1 : 0);
                hash = hash * 31 + (view.IsPaused ? 1 : 0);
                hash = hash * 31 + view.RatedHorsepower;
                hash = hash * 31 + view.ActualOutput;
                hash = hash * 31 + PanelNumbers.ConditionPercent(view.Condition);
                hash = hash * 31 + view.NetworkSupplyHp;
                hash = hash * 31 + view.NetworkDemandHp;
                hash = hash * 31 + PanelNumbers.PercentPoint(view.NetworkEfficiency);
                hash = hash * 31 + view.FuelInStock;
                hash = hash * 31 + view.FuelCapacity;
                hash = hash * 31 + view.WaterInStock;
                hash = hash * 31 + view.WaterCapacity;
                hash = hash * 31 + RoundTo(view.FuelPerHour, 100.0);
                hash = hash * 31 + RoundTo(view.WaterPerHour, 100.0);
                hash = hash * 31 + RoundTo(view.MaintenanceRemainingHours, 10.0);
                hash = hash * 31 + (view.AutoMaintenance ? 1 : 0);
                hash = hash * 31 + (view.PowerLineAttached ? 1 : 0);
                hash = hash * 31 + StatusDescriptors.WaterSupplyModeIndex(view.WaterSupplyMode);

                // P4B rendered rows: efficiency, beaver equivalent, next-maintenance day, material
                // list and the water-mode availability warning.
                hash = hash * 31 + RoundTo(view.WaterBeaverEquivalent, 10.0);
                hash = hash * 31 + RoundTo(view.NextMaintenanceDay, 10.0);
                hash = hash * 31 + view.MissingMaterialCount;
                hash = hash * 31 + (int)view.WaterSupplyAvailability;
                hash = hash * 31 + (view.PipeSupplyActive ? 1 : 0);
                hash = hash * 31 + (view.BeltSupplyActive ? 1 : 0);
                hash = hash * 31 + (view.BeltHasPower ? 1 : 0);
                System.Collections.Generic.IReadOnlyList<MaintenanceMaterialStatus> materials =
                    view.MaintenanceMaterials;
                if (materials != null)
                {
                    hash = hash * 31 + materials.Count;
                    for (int i = 0; i < materials.Count; i++)
                    {
                        MaintenanceMaterialStatus material = materials[i];
                        hash = hash * 31 + StableHash(material.GoodId);
                        hash = hash * 31 + material.InStock;
                        hash = hash * 31 + material.Required;
                    }
                }

                if (options != null && !string.IsNullOrEmpty(options.WaterBeaverEquivalentLocKey))
                {
                    hash = hash * 31 + RoundTo(
                        PanelNumbers.BeaverEquivalent(view.WaterPerHour, options.BeaverWaterPerDay), 10.0);
                }

                return hash;
            }
        }

        /// <summary>Stable per-process hash of a good id (string.GetHashCode is randomized per run).</summary>
        private static long StableHash(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0L;
            }

            unchecked
            {
                long hash = 17L;
                for (int i = 0; i < text.Length; i++)
                {
                    hash = hash * 31 + text[i];
                }

                return hash;
            }
        }

        private static long RoundTo(float value, double scale)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0L;
            }

            return (long)Math.Round(value * scale, MidpointRounding.AwayFromZero);
        }
    }
}
