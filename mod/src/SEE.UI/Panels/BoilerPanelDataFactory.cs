using System;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.UI.Status;
using Timberborn.Localization;

namespace SEE.UI.Panels
{
    /// <summary>
    /// Localizes the pure <see cref="BoilerPanelModel"/> into the strings the UIElements view
    /// assigns (CONTRACT_SEE §5.4). All <c>loc.T</c> calls happen here and only here, so the view
    /// never touches the localization service while a frame is being drawn.
    ///
    /// The factory adds no localization key of its own: every key comes from the model, which in
    /// turn only uses <see cref="SEE.Core.Constants.SeeLocKeys"/> (REQ-UI-08).
    /// </summary>
    public static class BoilerPanelDataFactory
    {
        /// <summary>Builds the localized payload of one selected boiler.</summary>
        /// <param name="model">Pure model from <see cref="BoilerPanelModelBuilder"/>; null hides the panel.</param>
        /// <param name="loc">Game localization service; null hides the panel (defensive).</param>
        /// <returns>The display payload; <see cref="BoilerPanelData.HasData"/> is false when hidden.</returns>
        public static BoilerPanelData Create(BoilerPanelModel? model, ILoc? loc)
        {
            var data = new BoilerPanelData();
            if (model == null || loc == null || !model.HasData)
            {
                return data;
            }

            data.HasData = true;
            data.Title = loc.T(model.TitleKey);
            data.TitleTooltip = loc.T(model.TitleTooltipKey);
            data.Status = LocalizeOne(model.StatusKey, model.StatusArgument, loc);
            data.SubStatus = string.IsNullOrEmpty(model.SubStatusKey) ? string.Empty : loc.T(model.SubStatusKey);
            data.FixHint = string.IsNullOrEmpty(model.FixHintKey) ? string.Empty : loc.T(model.FixHintKey);
            data.Hints = LocalizeHints(model.Hints, loc);
            data.StatusColor = model.StatusColor;
            data.BadgeVisible = model.BadgeVisible;
            data.PowerHeader = loc.T(model.PowerHeaderKey);
            data.Output = loc.T(model.OutputKey, model.OutputActual, model.OutputRated);
            data.Network = loc.T(model.NetworkKey, model.NetworkSupply, model.NetworkDemand);
            data.Efficiency = loc.T(model.EfficiencyKey, model.EfficiencyText);
            data.FuelHeader = loc.T(model.FuelHeaderKey);
            data.FuelDetail = model.FuelDetail;
            data.WaterHeader = loc.T(model.WaterHeaderKey);
            data.WaterDetail = model.WaterDetail;
            data.WaterBeaver = string.IsNullOrEmpty(model.WaterBeaverKey) || string.IsNullOrEmpty(model.WaterBeaverText)
                ? string.Empty
                : loc.T(model.WaterBeaverKey, model.WaterBeaverText);
            data.Condition = loc.T(model.ConditionKey, model.ConditionText);
            data.ConditionPercent = model.ConditionPercent;
            data.NextMaintenance = loc.T(model.NextMaintenanceKey, model.NextMaintenanceDays);
            data.Materials = LocalizeMaterials(model, loc);
            data.WaterModeHeader = loc.T(model.WaterModeHeaderKey);
            data.WaterModeValue = loc.T(model.WaterModeKey);
            data.WaterModeUnavailable = string.IsNullOrEmpty(model.WaterModeUnavailableKey)
                ? string.Empty
                : loc.T(model.WaterModeUnavailableKey, loc.T(model.WaterModeUnavailableArgumentKey));
            data.PauseLabel = loc.T(model.PauseKey);
            data.MaintainLabel = loc.T(model.MaintainKey);
            data.AutoLabel = loc.T(model.AutoKey);
            data.MaintenanceVisible = model.MaintenanceVisible;
            data.CanMaintain = model.CanMaintain;
            data.AutoMaintenance = model.AutoMaintenance;
            data.Paused = model.Paused;
            data.WaterModeIndex = model.WaterModeIndex;
            data.WaterModeLabels = BuildWaterModeLabels(loc);
            return data;
        }

        private static string[] BuildWaterModeLabels(ILoc loc)
        {
            return new[]
            {
                loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Auto)),
                loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Tank)),
                loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Valve)),
                loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Hauler))
            };
        }

        /// <summary>
        /// P4B (G5): localizes the material list. Good names come from the vanilla
        /// <c>Good.&lt;id&gt;.DisplayName</c> keys (the mod never re-declares vanilla good names);
        /// a missing vanilla translation falls back to the good id, never to a raw localization key.
        /// </summary>
        private static string LocalizeMaterials(BoilerPanelModel model, ILoc loc)
        {
            if (model.MaterialLines.Length == 0)
            {
                return loc.T(model.MaterialsKey, model.MaterialsText, model.MissingMaterialsText);
            }

            var parts = new string[model.MaterialLines.Length];
            for (int i = 0; i < model.MaterialLines.Length; i++)
            {
                PanelMaterialLine line = model.MaterialLines[i];
                string nameKey = SeeLocKeys.GoodDisplayName + line.GoodId + SeeLocKeys.GoodDisplayNameSuffix;
                string name = loc.T(nameKey);
                if (string.IsNullOrEmpty(name) || string.Equals(name, nameKey, StringComparison.Ordinal))
                {
                    name = line.GoodId;
                }

                parts[i] = loc.T(
                    SeeLocKeys.UiPanelMaterialEntry,
                    name,
                    PanelNumbers.Whole(line.Have),
                    PanelNumbers.Whole(line.Need));
            }

            return loc.T(model.MaterialsKey, string.Join(", ", parts), model.MissingMaterialsText);
        }

        private static string LocalizeOne(string key, string argument, ILoc loc)
        {
            return string.IsNullOrEmpty(argument) ? loc.T(key) : loc.T(key, argument);
        }

        private static string[] LocalizeHints(PanelHint[] hints, ILoc loc)
        {
            if (hints == null || hints.Length == 0)
            {
                return Array.Empty<string>();
            }

            var texts = new string[hints.Length];
            for (int i = 0; i < hints.Length; i++)
            {
                texts[i] = LocalizeOne(hints[i].LocKey, hints[i].Argument, loc);
            }

            return texts;
        }
    }
}
