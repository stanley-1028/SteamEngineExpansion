using SEE.Core.Abstractions;

namespace SEE.Core.Constants
{
    /// <summary>
    /// All 63 localization keys SEE ships (requirements §15.5, D-2; AC-16 requires at least 54, so
    /// the P4B card adds 9 keys to the frozen 54-key baseline). They are frozen strings: data
    /// files, the UI and the validator all reference these constants instead of raw literals, so a
    /// rename can never silently leave a raw key on screen (REQ-UI-05).
    /// </summary>
    public static class SeeLocKeys
    {
        /// <summary>Prefix shared by every boiler display-name key.</summary>
        public const string BuildingPrefix = "Building.SEE.SteamBoiler.";

        /// <summary>Display name of one size/tier combination, for example "...Small.T1.DisplayName".</summary>
        /// <param name="size">Boiler size token.</param>
        /// <param name="tier">Boiler tier token.</param>
        /// <returns>The frozen localization key.</returns>
        public static string DisplayName(BoilerSize size, BoilerTier tier) =>
            BuildingPrefix + size.ToString() + "." + tier.ToString() + ".DisplayName";

        /// <summary>Description key of the boiler building.</summary>
        public const string Description = "Building.SEE.SteamBoiler.Description";

        /// <summary>Flavor-text key of one tier, for example "Building.SEE.SteamBoiler.Flavor.T1".</summary>
        /// <param name="tier">Boiler tier token.</param>
        /// <returns>The frozen localization key.</returns>
        public static string Flavor(BoilerTier tier) => BuildingPrefix + "Flavor." + tier.ToString();

        /// <summary>Running state.</summary>
        public const string StatusRunning = "Status.SEE.Running";

        /// <summary>Idle state (no network demand).</summary>
        public const string StatusIdle = "Status.SEE.Idle";

        /// <summary>Player paused this boiler.</summary>
        public const string StatusPaused = "Status.SEE.Paused";

        /// <summary>Fuel inventory is empty.</summary>
        public const string StatusNoFuel = "Status.SEE.NoFuel";

        /// <summary>Water tank is empty.</summary>
        public const string StatusNoWater = "Status.SEE.NoWater";

        /// <summary>Condition reached 0.</summary>
        public const string StatusNeedsMaintenance = "Status.SEE.NeedsMaintenance";

        /// <summary>Condition in the critical band (1..20).</summary>
        public const string StatusCritical = "Status.SEE.Critical";

        /// <summary>Condition in the worn band (21..40).</summary>
        public const string StatusWorn = "Status.SEE.Worn";

        /// <summary>Condition in the maintenance-due band (41..60).</summary>
        public const string StatusMaintenanceDue = "Status.SEE.MaintenanceDue";

        /// <summary>Maintenance downtime in progress; placeholder {0} is the remaining time.</summary>
        public const string StatusMaintaining = "Status.SEE.Maintaining";

        /// <summary>Automatic maintenance is due but the district lacks materials.</summary>
        public const string StatusMissingMaintenanceMaterials = "Status.SEE.MissingMaintenanceMaterials";

        /// <summary>Automatic maintenance is due but no road reaches the building.</summary>
        public const string StatusNoAccessForMaintenance = "Status.SEE.NoAccessForMaintenance";

        /// <summary>Fuel remaining is below the low-supply threshold; placeholder {0} is hours.</summary>
        public const string StatusLowFuel = "Status.SEE.LowFuel";

        /// <summary>Water remaining is below the low-supply threshold; placeholder {0} is hours.</summary>
        public const string StatusLowWater = "Status.SEE.LowWater";

        /// <summary>Water is supplied by the Timber Pipes network.</summary>
        public const string StatusPipeSupply = "Status.SEE.PipeSupply";

        /// <summary>Fuel is supplied by a conveyor belt.</summary>
        public const string StatusBeltSupply = "Status.SEE.BeltSupply";

        /// <summary>The conveyor network has no power.</summary>
        public const string StatusBeltNoPower = "Status.SEE.BeltNoPower";

        /// <summary>The pipe network is contaminated.</summary>
        public const string StatusPipeContaminated = "Status.SEE.PipeContaminated";

        /// <summary>Water supply mode: tank (Timber Pipes).</summary>
        public const string StatusWaterModeTank = "Status.SEE.WaterMode.Tank";

        /// <summary>Water supply mode: one-way valve.</summary>
        public const string StatusWaterModeValve = "Status.SEE.WaterMode.Valve";

        /// <summary>Water supply mode: haulers.</summary>
        public const string StatusWaterModeHauler = "Status.SEE.WaterMode.Hauler";

        /// <summary>Water supply mode: automatic resolution.</summary>
        public const string StatusWaterModeAuto = "Status.SEE.WaterMode.Auto";

        /// <summary>Panel header: power section.</summary>
        public const string UiPanelPower = "UI.SEE.Panel.Power";

        /// <summary>Panel row: network supply / demand. Placeholders {0} supply, {1} demand.</summary>
        public const string UiPanelNetwork = "UI.SEE.Panel.Network";

        /// <summary>Panel row: actual / rated horsepower. Placeholders {0} actual, {1} rated.</summary>
        public const string UiPanelOutput = "UI.SEE.Panel.Output";

        /// <summary>Panel header: fuel section.</summary>
        public const string UiPanelFuel = "UI.SEE.Panel.Fuel";

        /// <summary>Panel header: water section.</summary>
        public const string UiPanelWater = "UI.SEE.Panel.Water";

        /// <summary>Panel row: condition. Placeholder {0} is the 0..100 value.</summary>
        public const string UiPanelCondition = "UI.SEE.Panel.Condition";

        /// <summary>Panel row: next maintenance countdown. Placeholder {0} is game days.</summary>
        public const string UiPanelNextMaintenance = "UI.SEE.Panel.NextMaintenance";

        /// <summary>Panel row: maintenance material list. Placeholder {0} is the list.</summary>
        public const string UiPanelMaterials = "UI.SEE.Panel.Materials";

        /// <summary>Panel row: water supply mode.</summary>
        public const string UiPanelWaterSupplyMode = "UI.SEE.Panel.WaterSupplyMode";

        // ---------------------------------------------------------------- P4B additions (9 keys)

        /// <summary>Panel row: rated water use as "≈N beavers' daily drinking water"; {0} is N.</summary>
        public const string UiPanelWaterBeaverEquivalent = "UI.SEE.Panel.WaterBeaverEquivalent";

        /// <summary>Panel row: network power efficiency percentage; {0} is 0..100.</summary>
        public const string UiPanelEfficiency = "UI.SEE.Panel.Efficiency";

        /// <summary>Panel row: maintenance material list plus missing count; {0} list, {1} missing.</summary>
        public const string UiPanelMaterialsDetail = "UI.SEE.Panel.MaterialsDetail";

        /// <summary>Panel row: one maintenance material as name + have/need; {0} name, {1} have, {2} need.</summary>
        public const string UiPanelMaterialEntry = "UI.SEE.Panel.MaterialEntry";

        /// <summary>Panel row: absolute game day of the next maintenance; {0} is the day.</summary>
        public const string UiPanelNextMaintenanceDay = "UI.SEE.Panel.NextMaintenanceDay";

        /// <summary>Repair hint when the boiler has no fuel (requirements §15.4/REQ-UI-04).</summary>
        public const string UiHintFixNoFuel = "UI.SEE.Hint.FixNoFuel";

        /// <summary>Repair hint when the boiler has no water (requirements §15.4/REQ-UI-04).</summary>
        public const string UiHintFixNoWater = "UI.SEE.Hint.FixNoWater";

        /// <summary>Repair hint when the boiler needs maintenance or maintenance is blocked.</summary>
        public const string UiHintFixNeedsMaintenance = "UI.SEE.Hint.FixNeedsMaintenance";

        /// <summary>Panel row: the selected water-supply mode cannot run; {0} is the mode name.</summary>
        public const string UiWaterModeUnavailable = "UI.SEE.WaterModeUnavailable";

        /// <summary>Pause button.</summary>
        public const string UiActionPause = "UI.SEE.Action.Pause";

        /// <summary>Resume button.</summary>
        public const string UiActionResume = "UI.SEE.Action.Resume";

        /// <summary>Maintain-now button.</summary>
        public const string UiActionMaintainNow = "UI.SEE.Action.MaintainNow";

        /// <summary>Auto-maintenance toggle.</summary>
        public const string UiActionAutoMaintenance = "UI.SEE.Action.AutoMaintenance";

        /// <summary>Manual maintenance error. Placeholder {0} is the missing material list.</summary>
        public const string UiErrorMaterialsMissing = "UI.SEE.Error.MaterialsMissing";

        /// <summary>Conveyor network error.</summary>
        public const string UiErrorBeltNoPower = "UI.SEE.Error.BeltNoPower";

        /// <summary>Vanilla key of a good's display name: <c>"Good." + goodId + ".DisplayName"</c>.</summary>
        public const string GoodDisplayName = "Good.";

        /// <summary>Vanilla suffix of a good display-name key.</summary>
        public const string GoodDisplayNameSuffix = ".DisplayName";

        /// <summary>Vanilla key of the mechanical "needs power" hint reused by the panel.</summary>
        public const string VanillaHintNeedsPower = "Status.Mechanical.NoPower";
    }
}
