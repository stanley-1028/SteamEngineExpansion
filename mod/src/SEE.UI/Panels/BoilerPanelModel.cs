using SEE.Core.Abstractions;
using SEE.UI.Status;

namespace SEE.UI.Panels
{
    /// <summary>One secondary warning line (requirements §15.3: at most two are displayed).</summary>
    public readonly struct PanelHint
    {
        /// <summary>Creates a hint.</summary>
        /// <param name="locKey">Frozen localization key.</param>
        /// <param name="argument">Pre-formatted <c>{0}</c> argument; empty when the key has none.</param>
        public PanelHint(string locKey, string argument)
        {
            LocKey = locKey;
            Argument = argument;
        }

        /// <summary>Frozen localization key of the hint.</summary>
        public string LocKey { get; }

        /// <summary>Pre-formatted placeholder value; empty when the key has no placeholder.</summary>
        public string Argument { get; }
    }

    /// <summary>
    /// One maintenance material line of the panel (requirements §15.2, REQ-UI-04): the vanilla good
    /// id plus the have/need counts. The UIElements path localizes the good name through the
    /// vanilla <c>Good.&lt;id&gt;.DisplayName</c> key; the pure model keeps the id so it stays
    /// offline-testable.
    /// </summary>
    public readonly struct PanelMaterialLine
    {
        /// <summary>Creates one material line.</summary>
        /// <param name="goodId">Vanilla good id, for example <c>Log</c>.</param>
        /// <param name="have">Whole items currently available.</param>
        /// <param name="need">Whole items the next maintenance cycle requires.</param>
        public PanelMaterialLine(string goodId, int have, int need)
        {
            GoodId = goodId ?? string.Empty;
            Have = have > 0 ? have : 0;
            Need = need > 0 ? need : 0;
        }

        /// <summary>Vanilla good id.</summary>
        public string GoodId { get; }

        /// <summary>Whole items currently available in the district.</summary>
        public int Have { get; }

        /// <summary>Whole items required for the next maintenance cycle.</summary>
        public int Need { get; }

        /// <summary>True when the district currently covers this material.</summary>
        public bool IsSatisfied => Have >= Need;
    }

    /// <summary>
    /// Display-ready boiler panel payload built exclusively from <see cref="IBoilerStatusView"/>
    /// and <see cref="BoilerPanelOptions"/> (requirements §15.2, REQ-UI-01/02/03/04/06).
    ///
    /// Every text is either a frozen localization key plus pre-formatted arguments, or a numeric
    /// string. The object holds no game component, no mutable state and no delegate, so the
    /// offline test runner can assert the whole panel contract (CONTRACT_SEE §5.2) without the
    /// game - and the UIElements view only has to assign the values.
    /// </summary>
    public sealed class BoilerPanelModel
    {
        /// <summary>False when the selected entity is not a finished boiler; the fragment hides.</summary>
        public bool HasData { get; set; }

        /// <summary>Display-name key of this size/tier combination.</summary>
        public string TitleKey { get; set; } = string.Empty;

        /// <summary>Tooltip key of the title row (the shared description).</summary>
        public string TitleTooltipKey { get; set; } = string.Empty;

        /// <summary>Localization key of the main status badge.</summary>
        public string StatusKey { get; set; } = string.Empty;

        /// <summary>Pre-formatted argument of the status key; empty when the key has no placeholder.</summary>
        public string StatusArgument { get; set; } = string.Empty;

        /// <summary>Colour band of the status row.</summary>
        public StatusColorBand StatusColor { get; set; }

        /// <summary>True when the status owns a visible badge.</summary>
        public bool BadgeVisible { get; set; }

        /// <summary>Badge priority; lower value wins.</summary>
        public int BadgePriority { get; set; }

        /// <summary>Reason line for a blocked maintenance (missing materials / no access); empty otherwise.</summary>
        public string SubStatusKey { get; set; } = string.Empty;

        /// <summary>Secondary hints, at most two (<see cref="MaxHints"/>).</summary>
        public PanelHint[] Hints { get; set; } = System.Array.Empty<PanelHint>();

        /// <summary>Section header key of the power block.</summary>
        public string PowerHeaderKey { get; set; } = string.Empty;

        /// <summary>Output row key; arguments are <see cref="OutputActual"/> / <see cref="OutputRated"/>.</summary>
        public string OutputKey { get; set; } = string.Empty;

        /// <summary>Actual output in whole hp.</summary>
        public string OutputActual { get; set; } = string.Empty;

        /// <summary>Rated output in whole hp.</summary>
        public string OutputRated { get; set; } = string.Empty;

        /// <summary>Network row key; arguments are <see cref="NetworkSupply"/> / <see cref="NetworkDemand"/>.</summary>
        public string NetworkKey { get; set; } = string.Empty;

        /// <summary>Network supply in whole hp.</summary>
        public string NetworkSupply { get; set; } = string.Empty;

        /// <summary>Network demand in whole hp.</summary>
        public string NetworkDemand { get; set; } = string.Empty;

        /// <summary>Network-efficiency row key (P4B); argument is <see cref="EfficiencyText"/>.</summary>
        public string EfficiencyKey { get; set; } = string.Empty;

        /// <summary>Network efficiency in whole percent, 0..100 (P4B, REQ-UI-02).</summary>
        public string EfficiencyText { get; set; } = string.Empty;

        /// <summary>Repair-guidance key for NoFuel/NoWater/NeedsMaintenance (P4B, REQ-UI-04).</summary>
        public string FixHintKey { get; set; } = string.Empty;

        /// <summary>Section header key of the fuel block.</summary>
        public string FuelHeaderKey { get; set; } = string.Empty;

        /// <summary>Fuel detail line: stock / capacity, rated rate and remaining hours.</summary>
        public string FuelDetail { get; set; } = string.Empty;

        /// <summary>Section header key of the water block.</summary>
        public string WaterHeaderKey { get; set; } = string.Empty;

        /// <summary>Water detail line: stock / capacity, m³, rated rate and remaining hours.</summary>
        public string WaterDetail { get; set; } = string.Empty;

        /// <summary>Computed beaver-drinking equivalent of the rated water consumption; empty when hidden.</summary>
        public string WaterBeaverText { get; set; } = string.Empty;

        /// <summary>
        /// Frozen localization key of the beaver-equivalent line. P4B froze
        /// <c>UI.SEE.Panel.WaterBeaverEquivalent</c>; an empty key still hides the row (tests).
        /// </summary>
        public string WaterBeaverKey { get; set; } = string.Empty;

        /// <summary>Condition row key; argument is <see cref="ConditionText"/>.</summary>
        public string ConditionKey { get; set; } = string.Empty;

        /// <summary>Condition in 0..100 as a whole number.</summary>
        public string ConditionText { get; set; } = string.Empty;

        /// <summary>Condition in 0..100 for the progress bar.</summary>
        public int ConditionPercent { get; set; }

        /// <summary>Next-maintenance row key (P4B: absolute day); argument is <see cref="NextMaintenanceDays"/>.</summary>
        public string NextMaintenanceKey { get; set; } = string.Empty;

        /// <summary>Absolute game day of the next maintenance, one decimal; the unknown glyph when unavailable.</summary>
        public string NextMaintenanceDays { get; set; } = string.Empty;

        /// <summary>Materials row key (P4B: list + missing count); arguments are the two texts below.</summary>
        public string MaterialsKey { get; set; } = string.Empty;

        /// <summary>
        /// Pure, good-id based rendering of the material list ("Log 3/8, Plank 1/12"). The UIElements
        /// path replaces the good ids with their vanilla display names; the model keeps this form so
        /// the offline test can assert the joined list without a localization service.
        /// </summary>
        public string MaterialsText { get; set; } = string.Empty;

        /// <summary>Maintenance material lines, one per required material (P4B, REQ-UI-04).</summary>
        public PanelMaterialLine[] MaterialLines { get; set; } = System.Array.Empty<PanelMaterialLine>();

        /// <summary>How many material kinds are short, or the unknown glyph when not readable.</summary>
        public string MissingMaterialsText { get; set; } = string.Empty;

        /// <summary>Water supply mode header key.</summary>
        public string WaterModeHeaderKey { get; set; } = string.Empty;

        /// <summary>Water supply mode value key (one of the four <c>Status.SEE.WaterMode.*</c> keys).</summary>
        public string WaterModeKey { get; set; } = string.Empty;

        /// <summary>Player-intent water supply mode.</summary>
        public WaterSupplyMode WaterMode { get; set; }

        /// <summary>Zero-based display index of <see cref="WaterMode"/> (0..3).</summary>
        public int WaterModeIndex { get; set; }

        /// <summary>Unavailable-mode warning key (P4B); empty when the selected mode can run.</summary>
        public string WaterModeUnavailableKey { get; set; } = string.Empty;

        /// <summary>Frozen mode-name key used as the warning argument (a <c>Status.SEE.WaterMode.*</c> key).</summary>
        public string WaterModeUnavailableArgumentKey { get; set; } = string.Empty;

        /// <summary>Pause/resume button label key for the next click.</summary>
        public string PauseKey { get; set; } = string.Empty;

        /// <summary>Maintain-now button label key.</summary>
        public string MaintainKey { get; set; } = string.Empty;

        /// <summary>Auto-maintenance toggle label key.</summary>
        public string AutoKey { get; set; } = string.Empty;

        /// <summary>True when the whole maintenance block is shown (config switch, REQ-MAINT-17).</summary>
        public bool MaintenanceVisible { get; set; }

        /// <summary>True when the maintain-now button may be clicked (REQ-UI-06).</summary>
        public bool CanMaintain { get; set; }

        /// <summary>True when the boiler is paused.</summary>
        public bool Paused { get; set; }

        /// <summary>True when automatic maintenance is enabled for this boiler.</summary>
        public bool AutoMaintenance { get; set; }

        /// <summary>Maximum number of secondary hints the panel shows at once (§15.3).</summary>
        public const int MaxHints = 2;
    }
}
