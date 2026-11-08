using SEE.Core.Abstractions;
using SEE.Core.Constants;

namespace SEE.UI.Status
{
    /// <summary>
    /// Semantic colour band of one boiler status (requirements §15.3). The enum carries no
    /// engine colour on purpose: everything below this layer is pure data, so the mapping can be
    /// asserted by the offline test runner, and the UIElements layer is the only place that turns
    /// a band into a <c>Color</c>.
    /// </summary>
    public enum StatusColorBand
    {
        /// <summary>Default panel text colour (Running).</summary>
        Neutral = 0,

        /// <summary>Grey: the boiler was paused by the player.</summary>
        Grey = 1,

        /// <summary>Blue: missing water.</summary>
        Blue = 2,

        /// <summary>Orange: fuel, worn parts, blocked maintenance.</summary>
        Orange = 3,

        /// <summary>Yellow: maintenance due / maintenance running.</summary>
        Yellow = 4,

        /// <summary>Red: needs maintenance / critical condition.</summary>
        Red = 5
    }

    /// <summary>
    /// The single mapping from machine-readable state to player-visible localization key and
    /// colour band (requirements §15.3, REQ-UI-03). The priority order itself lives in
    /// <see cref="BoilerStatus"/> and the state machine; the panel only looks the winner up here,
    /// so there is no second "which state wins" implementation in the UI.
    ///
    /// Every string returned here is one of the frozen loc keys in
    /// <see cref="SeeLocKeys"/>; the class therefore never invents a key (REQ-UI-08).
    /// </summary>
    public static class StatusDescriptors
    {
        /// <summary>Localization key of the one main status badge.</summary>
        /// <param name="status">Highest-priority state resolved by gameplay.</param>
        /// <returns>A <c>Status.SEE.*</c> key.</returns>
        public static string StatusLocKey(BoilerStatus status)
        {
            switch (status)
            {
                case BoilerStatus.Paused:
                    return SeeLocKeys.StatusPaused;
                case BoilerStatus.Maintaining:
                    return SeeLocKeys.StatusMaintaining;
                case BoilerStatus.NeedsMaintenance:
                    return SeeLocKeys.StatusNeedsMaintenance;
                case BoilerStatus.NoFuel:
                    return SeeLocKeys.StatusNoFuel;
                case BoilerStatus.NoWater:
                    return SeeLocKeys.StatusNoWater;
                case BoilerStatus.Idle:
                    return SeeLocKeys.StatusIdle;
                case BoilerStatus.Critical:
                    return SeeLocKeys.StatusCritical;
                case BoilerStatus.Worn:
                    return SeeLocKeys.StatusWorn;
                case BoilerStatus.MaintenanceDue:
                    return SeeLocKeys.StatusMaintenanceDue;
                default:
                    return SeeLocKeys.StatusRunning;
            }
        }

        /// <summary>True when the status text carries the <c>{0}</c> remaining-time placeholder.</summary>
        /// <param name="status">Highest-priority state.</param>
        /// <returns>True only for <see cref="BoilerStatus.Maintaining"/>.</returns>
        public static bool StatusHasArgument(BoilerStatus status) => status == BoilerStatus.Maintaining;

        /// <summary>Colour band required by the status table in requirements §15.3.</summary>
        /// <param name="status">Highest-priority state.</param>
        /// <returns>The band the panel renders the status row in.</returns>
        public static StatusColorBand StatusColor(BoilerStatus status)
        {
            switch (status)
            {
                case BoilerStatus.Paused:
                    return StatusColorBand.Grey;
                case BoilerStatus.NoFuel:
                    return StatusColorBand.Orange;
                case BoilerStatus.NoWater:
                    return StatusColorBand.Blue;
                case BoilerStatus.NeedsMaintenance:
                case BoilerStatus.Critical:
                    return StatusColorBand.Red;
                case BoilerStatus.Worn:
                    return StatusColorBand.Orange;
                case BoilerStatus.MaintenanceDue:
                case BoilerStatus.Maintaining:
                    return StatusColorBand.Yellow;
                case BoilerStatus.Idle:
                    return StatusColorBand.Grey;
                default:
                    return StatusColorBand.Neutral;
            }
        }

        /// <summary>True when this state owns a visible status badge (Running shows none).</summary>
        /// <param name="status">Highest-priority state.</param>
        /// <returns>False only for <see cref="BoilerStatus.Running"/>.</returns>
        public static bool HasBadge(BoilerStatus status) => status != BoilerStatus.Running;

        /// <summary>
        /// Badge priority as displayed, high value first. The declaration order of
        /// <see cref="BoilerStatus"/> IS the frozen priority, so this is a pure cast.
        /// </summary>
        /// <param name="status">Highest-priority state.</param>
        /// <returns>0 (Paused, highest priority) through 9 (Running).</returns>
        public static int BadgePriority(BoilerStatus status) => (int)status;

        /// <summary>Localization key of the maintenance sub-state line.</summary>
        /// <param name="phase">Persisted maintenance phase.</param>
        /// <returns>A <c>Status.SEE.*</c> key; empty for <see cref="MaintenancePhase.None"/>.</returns>
        public static string MaintenanceLocKey(MaintenancePhase phase)
        {
            switch (phase)
            {
                case MaintenancePhase.Maintaining:
                    return SeeLocKeys.StatusMaintaining;
                case MaintenancePhase.MissingMaterials:
                    return SeeLocKeys.StatusMissingMaintenanceMaterials;
                case MaintenancePhase.NoAccess:
                    return SeeLocKeys.StatusNoAccessForMaintenance;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// True when the phase is a reason line rather than the "maintenance running" state
        /// (the latter is already the main status via <see cref="BoilerStatus.Maintaining"/>).
        /// </summary>
        /// <param name="phase">Persisted maintenance phase.</param>
        /// <returns>True for MissingMaterials / NoAccess.</returns>
        public static bool IsMaintenanceReason(MaintenancePhase phase) =>
            phase == MaintenancePhase.MissingMaterials || phase == MaintenancePhase.NoAccess;

        /// <summary>Localization key of the water supply mode row.</summary>
        /// <param name="mode">Player intent for the water supply mode.</param>
        /// <returns>A <c>Status.SEE.WaterMode.*</c> key.</returns>
        public static string WaterSupplyModeLocKey(WaterSupplyMode mode)
        {
            switch (mode)
            {
                case WaterSupplyMode.Tank:
                    return SeeLocKeys.StatusWaterModeTank;
                case WaterSupplyMode.Valve:
                    return SeeLocKeys.StatusWaterModeValve;
                case WaterSupplyMode.Hauler:
                    return SeeLocKeys.StatusWaterModeHauler;
                default:
                    return SeeLocKeys.StatusWaterModeAuto;
            }
        }

        /// <summary>Localization key of a boiler display name (size x tier).</summary>
        /// <param name="size">Boiler size.</param>
        /// <param name="tier">Boiler tier.</param>
        /// <returns>A <c>Building.SEE.SteamBoiler.*</c> key.</returns>
        public static string DisplayNameLocKey(BoilerSize size, BoilerTier tier) =>
            SeeLocKeys.DisplayName(size, tier);

        /// <summary>One of the four frozen water-supply mode intents, in display order.</summary>
        /// <param name="index">0..3.</param>
        /// <returns><see cref="WaterSupplyMode.Auto"/> for out-of-range indexes.</returns>
        public static WaterSupplyMode WaterSupplyModeAt(int index)
        {
            switch (index)
            {
                case 1:
                    return WaterSupplyMode.Tank;
                case 2:
                    return WaterSupplyMode.Valve;
                case 3:
                    return WaterSupplyMode.Hauler;
                default:
                    return WaterSupplyMode.Auto;
            }
        }

        /// <summary>Display index (0..3) of a water-supply mode, matching <see cref="WaterSupplyModeAt"/>.</summary>
        /// <param name="mode">Water-supply mode intent.</param>
        /// <returns>0..3.</returns>
        public static int WaterSupplyModeIndex(WaterSupplyMode mode)
        {
            int value = (int)mode;
            return value < 0 || value > 3 ? 0 : value;
        }
    }
}
