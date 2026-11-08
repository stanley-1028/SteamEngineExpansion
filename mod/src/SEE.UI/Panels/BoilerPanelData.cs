using SEE.UI.Status;

namespace SEE.UI.Panels
{
    /// <summary>
    /// Fully localized, display-ready payload of the boiler panel (requirements §15.2).
    /// <see cref="BoilerPanelDataFactory"/> produces it from the pure <see cref="BoilerPanelModel"/>;
    /// the UIElements view only assigns these strings, so no localization or formatting ever
    /// happens inside <c>UpdateFragment</c> unless the dirty fingerprint changed (REQ-OPS-02).
    /// </summary>
    public sealed class BoilerPanelData
    {
        /// <summary>False when the entity is not a finished boiler; the fragment hides its root.</summary>
        public bool HasData { get; set; }

        /// <summary>Localized display name (title of the collapsible block).</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Localized tooltip of the title block.</summary>
        public string TitleTooltip { get; set; } = string.Empty;

        /// <summary>Localized highest-priority status text.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Localized maintenance reason line (missing materials / no access); empty when none.</summary>
        public string SubStatus { get; set; } = string.Empty;

        /// <summary>Localized repair guidance (P4B, REQ-UI-04); empty when no fix is needed.</summary>
        public string FixHint { get; set; } = string.Empty;

        /// <summary>Localized secondary hints, at most two.</summary>
        public string[] Hints { get; set; } = System.Array.Empty<string>();

        /// <summary>Colour band of the status row and badge.</summary>
        public StatusColorBand StatusColor { get; set; }

        /// <summary>True when the status badge is shown (Running never shows one).</summary>
        public bool BadgeVisible { get; set; }

        /// <summary>Localized "Power" section header.</summary>
        public string PowerHeader { get; set; } = string.Empty;

        /// <summary>Localized actual / rated output row.</summary>
        public string Output { get; set; } = string.Empty;

        /// <summary>Localized network supply / demand row.</summary>
        public string Network { get; set; } = string.Empty;

        /// <summary>Localized network-efficiency row (P4B, REQ-UI-02).</summary>
        public string Efficiency { get; set; } = string.Empty;

        /// <summary>Localized "Fuel" section header.</summary>
        public string FuelHeader { get; set; } = string.Empty;

        /// <summary>Fuel stock / capacity, rate and remaining hours.</summary>
        public string FuelDetail { get; set; } = string.Empty;

        /// <summary>Localized "Water" section header.</summary>
        public string WaterHeader { get; set; } = string.Empty;

        /// <summary>Water stock / capacity, m³, rate and remaining hours.</summary>
        public string WaterDetail { get; set; } = string.Empty;

        /// <summary>Localized beaver-drinking equivalent; empty while the key is not frozen.</summary>
        public string WaterBeaver { get; set; } = string.Empty;

        /// <summary>Localized condition row.</summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>Condition in 0..100 for the progress bar.</summary>
        public int ConditionPercent { get; set; }

        /// <summary>Localized next-maintenance countdown row.</summary>
        public string NextMaintenance { get; set; } = string.Empty;

        /// <summary>Localized maintenance materials row.</summary>
        public string Materials { get; set; } = string.Empty;

        /// <summary>Localized water supply mode header.</summary>
        public string WaterModeHeader { get; set; } = string.Empty;

        /// <summary>Localized current water supply mode.</summary>
        public string WaterModeValue { get; set; } = string.Empty;

        /// <summary>Localized "selected water mode unavailable" warning (P4B); empty when available.</summary>
        public string WaterModeUnavailable { get; set; } = string.Empty;

        /// <summary>Localized pause / resume button label.</summary>
        public string PauseLabel { get; set; } = string.Empty;

        /// <summary>Localized maintain-now button label.</summary>
        public string MaintainLabel { get; set; } = string.Empty;

        /// <summary>Localized auto-maintenance toggle label.</summary>
        public string AutoLabel { get; set; } = string.Empty;

        /// <summary>True when the whole maintenance block is shown (config switch).</summary>
        public bool MaintenanceVisible { get; set; }

        /// <summary>True when the maintain-now button is enabled.</summary>
        public bool CanMaintain { get; set; }

        /// <summary>Auto-maintenance toggle value.</summary>
        public bool AutoMaintenance { get; set; }

        /// <summary>True when the boiler is paused.</summary>
        public bool Paused { get; set; }

        /// <summary>Zero-based display index of the water supply mode (0..3).</summary>
        public int WaterModeIndex { get; set; }

        /// <summary>Localized labels of the four water supply modes, in display order.</summary>
        public string[] WaterModeLabels { get; set; } = System.Array.Empty<string>();
    }
}
