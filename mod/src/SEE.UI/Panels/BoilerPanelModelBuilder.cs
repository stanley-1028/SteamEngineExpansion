using System;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.UI.Status;

namespace SEE.UI.Panels
{
    /// <summary>
    /// Pure mapping from the read-only <see cref="IBoilerStatusView"/> to the panel payload
    /// (requirements §15.2/§15.3/§15.4, REQ-UI-01/02/03/04/06). This class is the "single source"
    /// the UIElements view consumes; it has no engine dependency, so SEE.Tests asserts every row,
    /// every placeholder and the whole status priority table offline (CONTRACT_SEE §5.2).
    ///
    /// The builder never throws: a corrupt view (NaN condition, negative stock, unknown enum)
    /// degrades to a safe value instead of leaking into the entity panel (REQ-SAFE-01).
    /// </summary>
    public static class BoilerPanelModelBuilder
    {
        /// <summary>Builds the panel model of one selected boiler.</summary>
        /// <param name="view">Read-only view from the gameplay registry; null hides the panel.</param>
        /// <param name="options">Resolved panel configuration; null falls back to the shipped defaults.</param>
        /// <returns>The display-ready model; <see cref="BoilerPanelModel.HasData"/> is false for unfinished boilers.</returns>
        public static BoilerPanelModel Build(IBoilerStatusView? view, BoilerPanelOptions? options)
        {
            var model = new BoilerPanelModel();
            if (view == null)
            {
                return model;
            }

            if (!view.IsFinished)
            {
                return model;
            }

            options = options ?? BoilerPanelOptions.Default;
            model.HasData = true;
            BuildHeader(model, view, options);
            BuildPower(model, view);
            BuildFuel(model, view);
            BuildWater(model, view, options);
            BuildMaintenance(model, view, options);
            BuildActions(model, view, options);
            return model;
        }

        private static void BuildHeader(BoilerPanelModel model, IBoilerStatusView view, BoilerPanelOptions options)
        {
            model.TitleKey = StatusDescriptors.DisplayNameLocKey(view.Size, view.Tier);
            model.TitleTooltipKey = SeeLocKeys.Description;
            model.StatusKey = StatusDescriptors.StatusLocKey(view.Status);
            model.StatusArgument = StatusDescriptors.StatusHasArgument(view.Status)
                ? PanelNumbers.OneDecimal(view.MaintenanceRemainingHours)
                : string.Empty;
            model.StatusColor = StatusDescriptors.StatusColor(view.Status);
            model.BadgeVisible = BoilerBadgePolicy.IsVisible(view.Status, options.ShowIdleBadge);
            model.BadgePriority = BoilerBadgePolicy.Priority(view.Status);
            model.SubStatusKey = options.MaintenanceEnabled && StatusDescriptors.IsMaintenanceReason(view.MaintenancePhase)
                ? StatusDescriptors.MaintenanceLocKey(view.MaintenancePhase)
                : string.Empty;
            model.Hints = BuildHints(view, options);
            model.FixHintKey = FixHint(view, options);
        }

        private static PanelHint[] BuildHints(IBoilerStatusView view, BoilerPanelOptions options)
        {
            var hints = new PanelHint[BoilerPanelModel.MaxHints];
            int count = 0;
            float fuelHours = PanelNumbers.RemainingHours(view.FuelInStock, view.FuelPerHour);
            if (view.Status != BoilerStatus.NoFuel && PanelNumbers.IsLowSupply(fuelHours, options.LowSupplyHours))
            {
                hints[count++] = new PanelHint(SeeLocKeys.StatusLowFuel, PanelNumbers.OneDecimal(fuelHours));
            }

            float waterHours = PanelNumbers.RemainingHours(view.WaterInStock, view.WaterPerHour);
            if (count < BoilerPanelModel.MaxHints
                && view.Status != BoilerStatus.NoWater
                && PanelNumbers.IsLowSupply(waterHours, options.LowSupplyHours))
            {
                hints[count++] = new PanelHint(SeeLocKeys.StatusLowWater, PanelNumbers.OneDecimal(waterHours));
            }

            if (count == 0)
            {
                return Array.Empty<PanelHint>();
            }

            var result = new PanelHint[count];
            Array.Copy(hints, result, count);
            return result;
        }

        private static void BuildPower(BoilerPanelModel model, IBoilerStatusView view)
        {
            model.PowerHeaderKey = SeeLocKeys.UiPanelPower;
            model.OutputKey = SeeLocKeys.UiPanelOutput;
            model.OutputActual = PanelNumbers.Whole(view.ActualOutput);
            model.OutputRated = PanelNumbers.Whole(view.RatedHorsepower);
            model.NetworkKey = SeeLocKeys.UiPanelNetwork;
            model.NetworkSupply = PanelNumbers.Whole(view.NetworkSupplyHp);
            model.NetworkDemand = PanelNumbers.Whole(view.NetworkDemandHp);
            // P4B (REQ-UI-02): the network efficiency the contract already exposed.
            model.EfficiencyKey = SeeLocKeys.UiPanelEfficiency;
            model.EfficiencyText = PanelNumbers.Whole(PanelNumbers.PercentPoint(view.NetworkEfficiency));
        }

        private static void BuildFuel(BoilerPanelModel model, IBoilerStatusView view)
        {
            float hours = PanelNumbers.RemainingHours(view.FuelInStock, view.FuelPerHour);
            model.FuelHeaderKey = SeeLocKeys.UiPanelFuel;
            model.FuelDetail = PanelNumbers.Whole(view.FuelInStock)
                               + " / " + PanelNumbers.Whole(view.FuelCapacity)
                               + "   " + PanelNumbers.TwoDecimals(view.FuelPerHour) + "/h"
                               + "   " + PanelNumbers.OneDecimal(hours) + " h";
        }

        private static void BuildWater(BoilerPanelModel model, IBoilerStatusView view, BoilerPanelOptions options)
        {
            float hours = PanelNumbers.RemainingHours(view.WaterInStock, view.WaterPerHour);
            model.WaterHeaderKey = SeeLocKeys.UiPanelWater;
            model.WaterDetail = PanelNumbers.Whole(view.WaterInStock)
                                + " / " + PanelNumbers.Whole(view.WaterCapacity)
                                + "   " + PanelNumbers.CubicMetres(view.WaterInStock) + " m\u00b3"
                                + "   " + PanelNumbers.TwoDecimals(view.WaterPerHour) + "/h"
                                + "   " + PanelNumbers.OneDecimal(hours) + " h";
            // P4B (G1): the read view owns the conversion source; the UI falls back to its own
            // display constant only when the view cannot provide a usable value.
            float equivalent = view.WaterBeaverEquivalent;
            if (float.IsNaN(equivalent) || float.IsInfinity(equivalent) || equivalent <= 0f)
            {
                equivalent = PanelNumbers.BeaverEquivalent(view.WaterPerHour, options.BeaverWaterPerDay);
            }

            model.WaterBeaverKey = string.IsNullOrEmpty(options.WaterBeaverEquivalentLocKey)
                ? SeeLocKeys.UiPanelWaterBeaverEquivalent
                : options.WaterBeaverEquivalentLocKey;
            model.WaterBeaverText = PanelNumbers.OneDecimal(equivalent);
        }

        private static void BuildMaintenance(BoilerPanelModel model, IBoilerStatusView view, BoilerPanelOptions options)
        {
            int percent = PanelNumbers.ConditionPercent(view.Condition);
            model.ConditionPercent = percent;
            model.ConditionText = PanelNumbers.Whole(percent);
            model.ConditionKey = SeeLocKeys.UiPanelCondition;

            // P4B (G5): the read view now owns the next-maintenance day and the material list.
            model.NextMaintenanceKey = SeeLocKeys.UiPanelNextMaintenanceDay;
            float nextDay = view.NextMaintenanceDay;
            model.NextMaintenanceDays = !float.IsNaN(nextDay) && !float.IsInfinity(nextDay) && nextDay > 0f
                ? PanelNumbers.OneDecimal(nextDay)
                : PanelNumbers.Unknown;

            BuildMaterials(model, view);

            model.MaintenanceVisible = options.MaintenanceEnabled;
            model.CanMaintain = percent < 100
                                && view.MaintenancePhase != MaintenancePhase.Maintaining
                                && !StatusDescriptors.IsMaintenanceReason(view.MaintenancePhase);
            model.AutoMaintenance = view.AutoMaintenance;
            model.Paused = view.IsPaused;
            model.AutoKey = SeeLocKeys.UiActionAutoMaintenance;
        }

        /// <summary>
        /// P4B (G5): material list plus the number of missing kinds. A view that exposes no material
        /// list degrades to the explicit unknown glyph rather than showing an empty or raw-key row.
        /// </summary>
        private static void BuildMaterials(BoilerPanelModel model, IBoilerStatusView view)
        {
            model.MaterialsKey = SeeLocKeys.UiPanelMaterialsDetail;
            System.Collections.Generic.IReadOnlyList<MaintenanceMaterialStatus> materials =
                view.MaintenanceMaterials;
            if (materials == null || materials.Count == 0)
            {
                model.MaterialsText = PanelNumbers.Unknown;
                model.MissingMaterialsText = PanelNumbers.Unknown;
                model.MaterialLines = Array.Empty<PanelMaterialLine>();
                return;
            }

            var lines = new PanelMaterialLine[materials.Count];
            var parts = new string[materials.Count];
            int missing = 0;
            for (int i = 0; i < materials.Count; i++)
            {
                MaintenanceMaterialStatus material = materials[i];
                lines[i] = new PanelMaterialLine(material.GoodId, material.InStock, material.Required);
                parts[i] = material.GoodId + " " + PanelNumbers.Whole(material.InStock)
                           + "/" + PanelNumbers.Whole(material.Required);
                if (material.Missing > 0)
                {
                    missing++;
                }
            }

            model.MaterialLines = lines;
            model.MaterialsText = string.Join(", ", parts);
            int reportedMissing = view.MissingMaterialCount;
            model.MissingMaterialsText = PanelNumbers.Whole(reportedMissing >= 0 ? reportedMissing : missing);
        }

        private static void BuildActions(BoilerPanelModel model, IBoilerStatusView view, BoilerPanelOptions options)
        {
            model.PauseKey = view.IsPaused ? SeeLocKeys.UiActionResume : SeeLocKeys.UiActionPause;
            model.MaintainKey = SeeLocKeys.UiActionMaintainNow;
            model.WaterModeHeaderKey = SeeLocKeys.UiPanelWaterSupplyMode;
            model.WaterMode = view.WaterSupplyMode;
            model.WaterModeIndex = StatusDescriptors.WaterSupplyModeIndex(view.WaterSupplyMode);
            model.WaterModeKey = StatusDescriptors.WaterSupplyModeLocKey(view.WaterSupplyMode);
            if (view.WaterSupplyAvailability != WaterSupplyAvailability.Available)
            {
                // P4B contract slot (G7 wiring stays with P5): explain why the mode cannot run.
                model.WaterModeUnavailableKey = SeeLocKeys.UiWaterModeUnavailable;
                model.WaterModeUnavailableArgumentKey = model.WaterModeKey;
            }
        }

        /// <summary>
        /// P4B (G4/REQ-UI-04): one repair-guidance line chosen from the highest-priority status.
        /// Fuel and water problems win over maintenance because the boiler cannot produce without
        /// them; the maintenance line also covers a blocked automatic maintenance reason.
        /// </summary>
        private static string FixHint(IBoilerStatusView view, BoilerPanelOptions options)
        {
            switch (view.Status)
            {
                case BoilerStatus.NoFuel:
                    return SeeLocKeys.UiHintFixNoFuel;
                case BoilerStatus.NoWater:
                    return SeeLocKeys.UiHintFixNoWater;
                case BoilerStatus.NeedsMaintenance:
                case BoilerStatus.MaintenanceDue:
                case BoilerStatus.Critical:
                    return options.MaintenanceEnabled ? SeeLocKeys.UiHintFixNeedsMaintenance : string.Empty;
                default:
                    return options.MaintenanceEnabled && StatusDescriptors.IsMaintenanceReason(view.MaintenancePhase)
                        ? SeeLocKeys.UiHintFixNeedsMaintenance
                        : string.Empty;
            }
        }
    }
}
