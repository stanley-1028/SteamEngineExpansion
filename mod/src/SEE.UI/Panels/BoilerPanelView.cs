using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.UI.Status;
using Timberborn.CoreUI;
using Timberborn.DropdownSystem;
using Timberborn.Localization;
using UnityEngine.UIElements;

namespace SEE.UI.Panels
{
    /// <summary>
    /// The UIElements tree of the boiler panel plus its one-way rendering path
    /// (requirements §15.1 P3/P4, §15.2, §15.4, REQ-UI-01/05/12).
    ///
    /// The visual language is the shipped game's own and nothing else: the SEE block is a native
    /// collapsible header row (the vanilla clickable entity-name button) followed by vanilla
    /// nine-slice sub-panel cards (<see cref="NineSliceVisualElement"/> with
    /// <c>entity-sub-panel bg-sub-box--green</c>) whose rows use <c>entity-panel__text</c> /
    /// <c>entity-panel__sub-text</c> / <c>entity-panel__heading-text</c> and the native red/yellow
    /// severity classes, a native <see cref="Timberborn.CoreUI.ProgressBar"/>, a native
    /// <c>game-toggle</c>, native nine-slice buttons taken from a vanilla UXML template and the
    /// game's own <see cref="Timberborn.DropdownSystem.Dropdown"/> for the water-supply mode. No
    /// inline colour, font size or fixed size is set, except the A1 status swatch (the one
    /// documented shape/colour cue).
    ///
    /// The tree is built exactly once in the constructor (never per frame, REQ-OPS-02).
    /// <see cref="Render"/> only assigns text, class membership, display visibility, one progress
    /// value and two control values; the auto-maintenance toggle is updated with
    /// <c>SetValueWithoutNotify</c> and the water-mode dropdown through its provider plus
    /// <c>UpdateSelectedValue</c>, so a refresh can never re-trigger a command. The collapsed flag
    /// is kept in <see cref="PanelCollapseState"/> for the lifetime of the view and is never reset
    /// by <see cref="Render"/> (REQ-UI-12).
    /// </summary>
    public sealed partial class BoilerPanelView
    {
        /// <summary>Header glyph while the block body is expanded.</summary>
        private const string ExpandedChevron = "▾";

        /// <summary>Header glyph while the block body is collapsed.</summary>
        private const string CollapsedChevron = "▸";

        private readonly VisualElementLoader _loader;
        private readonly DropdownItemsSetter _dropdownItemsSetter;
        private readonly PanelCollapseState _collapse = new PanelCollapseState();
        private readonly List<string> _modeLabels = new List<string>(4);

        private readonly VisualElement _root;
        private readonly Button _header;
        private readonly Label _headerChevron;
        private readonly Label _headerTitle;
        private readonly VisualElement _body;

        private readonly VisualElement _badgeSwatch;
        private readonly Label _status;
        private readonly Label _subStatus;
        private readonly Label _fixHint;
        private readonly Label _hint1;
        private readonly Label _hint2;

        private readonly Label _powerHeader;
        private readonly Label _output;
        private readonly Label _network;
        private readonly Label _efficiency;
        private readonly Label _fuelHeader;
        private readonly Label _fuelDetail;
        private readonly Label _waterHeader;
        private readonly Label _waterDetail;
        private readonly Label _waterBeaver;

        private readonly NineSliceVisualElement _maintenanceSection;
        private readonly Label _condition;
        private readonly global::Timberborn.CoreUI.ProgressBar _conditionBar;
        private readonly Label _nextMaintenance;
        private readonly Label _materials;
        private readonly Toggle _autoMaintenance;
        private readonly Button _maintainButton;

        private readonly Label _waterModeHeader;
        private readonly Label _waterModeUnavailable;
        private readonly Button _pauseButton;

        private Dropdown? _waterMode;
        private DropdownField? _waterModeFallback;
        /// <summary>Last visibility applied by <see cref="SetVisible"/>; false before the first call.</summary>
        private bool _visible;
        private WaterModeDropdownProvider? _provider;

        private int _modeIndex;

        /// <summary>Builds the panel once and wires the four commands (CONTRACT_SEE §5.2 D).</summary>
        /// <param name="loc">Localization service for the static control labels.</param>
        /// <param name="visualElementLoader">Game service that loads the vanilla UXML templates (native buttons).</param>
        /// <param name="dropdownListDrawer">Game singleton that opens/closes the vanilla dropdown list.</param>
        /// <param name="dropdownItemsSetter">Game singleton that fills a vanilla dropdown from a provider.</param>
        /// <param name="pauseToggled">Invoked by the pause/resume button.</param>
        /// <param name="maintainRequested">Invoked by the maintain-now button.</param>
        /// <param name="autoMaintenanceChanged">Invoked with the new auto-maintenance value.</param>
        /// <param name="waterModeChanged">Invoked with the new zero-based water-supply mode index.</param>
        public BoilerPanelView(
            ILoc loc,
            VisualElementLoader visualElementLoader,
            DropdownListDrawer dropdownListDrawer,
            DropdownItemsSetter dropdownItemsSetter,
            Action pauseToggled,
            Action maintainRequested,
            Action<bool> autoMaintenanceChanged,
            Action<int> waterModeChanged)
        {
            if (loc is null)
            {
                throw new ArgumentNullException(nameof(loc));
            }

            _loader = visualElementLoader ?? throw new ArgumentNullException(nameof(visualElementLoader));
            _dropdownItemsSetter = dropdownItemsSetter ?? throw new ArgumentNullException(nameof(dropdownItemsSetter));
            if (dropdownListDrawer is null)
            {
                throw new ArgumentNullException(nameof(dropdownListDrawer));
            }

            // Native collapsible block: the vanilla clickable entity-name row (Button) with the
            // chevron and the display name as child labels. It starts expanded (a collapsed block
            // once made the whole SEE panel look missing), and the state is kept for the session.
            _root = new VisualElement();
            BoilerPanelNativeStyles.SetVisible(_root, false);

            _header = BoilerPanelNativeStyles.CreateCollapseHeader(_root, ToggleCollapsed);
            _headerChevron = BoilerPanelNativeStyles.CreateCollapseChevron(_header);
            _headerChevron.text = ExpandedChevron;
            _headerTitle = BoilerPanelNativeStyles.CreateCollapseTitle(_header);

            _body = new VisualElement();
            _root.Add(_body);

            // Status card: the A1 swatch + native status text, reason line, fix hint and hints.
            NineSliceVisualElement statusSection = BoilerPanelNativeStyles.CreateSection(_body);
            VisualElement statusRow = BoilerPanelNativeStyles.CreateRow(statusSection);
            _badgeSwatch = BoilerPanelNativeStyles.CreateBadgeSwatch(statusRow);
            _status = BoilerPanelNativeStyles.CreateStatusText(statusRow);
            _subStatus = BoilerPanelNativeStyles.CreateErrorText(statusSection);
            _fixHint = BoilerPanelNativeStyles.CreateSubTextRow(statusSection);
            _hint1 = BoilerPanelNativeStyles.CreateSubTextRow(statusSection);
            _hint2 = BoilerPanelNativeStyles.CreateSubTextRow(statusSection);

            // Production card: power, fuel and water blocks in one native card.
            NineSliceVisualElement productionSection = BoilerPanelNativeStyles.CreateSection(_body);
            _powerHeader = BoilerPanelNativeStyles.CreateSectionHeader(productionSection);
            _output = BoilerPanelNativeStyles.CreateTextRow(productionSection);
            _network = BoilerPanelNativeStyles.CreateTextRow(productionSection);
            _efficiency = BoilerPanelNativeStyles.CreateTextRow(productionSection);
            _fuelHeader = BoilerPanelNativeStyles.CreateSectionHeader(productionSection);
            _fuelDetail = BoilerPanelNativeStyles.CreateTextRow(productionSection);
            _waterHeader = BoilerPanelNativeStyles.CreateSectionHeader(productionSection);
            _waterDetail = BoilerPanelNativeStyles.CreateTextRow(productionSection);
            _waterBeaver = BoilerPanelNativeStyles.CreateSubTextRow(productionSection);

            // Maintenance card: condition bar (DeteriorableFragment pattern), countdown/material
            // rows, the native toggle and the native maintain button. Hidden as a whole when the
            // maintenance system is disabled.
            _maintenanceSection = BoilerPanelNativeStyles.CreateSection(_body);
            _conditionBar = BoilerPanelNativeStyles.CreateProgressBar(_maintenanceSection, out Label condition);
            _condition = condition;
            _nextMaintenance = BoilerPanelNativeStyles.CreateTextRow(_maintenanceSection);
            _materials = BoilerPanelNativeStyles.CreateTextRow(_maintenanceSection);
            _autoMaintenance = BoilerPanelNativeStyles.CreateToggle(_maintenanceSection,
                loc.T(SeeLocKeys.UiActionAutoMaintenance));
            _autoMaintenance.RegisterValueChangedCallback(evt => autoMaintenanceChanged(evt.newValue));
            _maintainButton = BoilerPanelNativeStyles.CreateFragmentButton(
                BoilerPanelNativeStyles.CreateButtonWrapper(_maintenanceSection), _loader,
                loc.T(SeeLocKeys.UiActionMaintainNow), green: true, maintainRequested);

            // Controls card: water-supply mode and the pause/resume button.
            NineSliceVisualElement controlsSection = BoilerPanelNativeStyles.CreateSection(_body);
            _waterModeHeader = BoilerPanelNativeStyles.CreateTextRow(controlsSection);
            CreateWaterModeControl(loc, dropdownListDrawer, controlsSection, waterModeChanged);
            _waterModeUnavailable = BoilerPanelNativeStyles.CreateErrorText(controlsSection);
            _pauseButton = BoilerPanelNativeStyles.CreateFragmentButton(
                BoilerPanelNativeStyles.CreateButtonWrapper(controlsSection), _loader,
                loc.T(SeeLocKeys.UiActionPause), green: false, pauseToggled);
        }

        /// <summary>Root element handed to the entity panel system.</summary>
        public VisualElement Root => _root;

        /// <summary>True while the SEE block body is expanded (survives renders, never saved).</summary>
        public bool IsExpanded => _collapse.IsExpanded;

        /// <summary>Shows or hides the whole fragment (other entity types add no rows).</summary>
        /// <param name="visible">True to show the boiler block.</param>
        public void SetVisible(bool visible)
        {
            BoilerPanelNativeStyles.SetVisible(_root, visible);

            // 2026-11-14 hotfix-7 (player report "the crop selector closes all the time"):
            // the vanilla Dropdown.Hide() closes the DRAWER SINGLETON, i.e. every open dropdown
            // popup in the game - not just ours. BoilerPanelFragment hides this block on every
            // entity-panel refresh whenever the selected entity is not a boiler, so an
            // unconditional Hide() here closed other panels' dropdowns too (measured: an HDBF
            // crop selector closed ~0.2 s after opening while any non-boiler was selected).
            // Only close OUR popup on a real visible -> hidden transition of this block.
            if (_visible && !visible)
            {
                _waterMode?.Hide();
            }

            _visible = visible;
        }

        /// <summary>Applies one localized payload to the existing elements.</summary>
        /// <param name="data">Payload from <see cref="BoilerPanelDataFactory"/>.</param>
        public void Render(BoilerPanelData data)
        {
            if (!data.HasData)
            {
                SetVisible(false);
                return;
            }

            SetText(_headerTitle, data.Title);
            _header.tooltip = data.TitleTooltip;

            SetText(_status, data.Status);
            BoilerPanelNativeStyles.SetBadge(_status, _badgeSwatch, data.StatusColor, data.BadgeVisible);

            SetOptional(_subStatus, data.SubStatus);
            SetOptional(_fixHint, data.FixHint);
            SetOptional(_hint1, data.Hints.Length > 0 ? data.Hints[0] : string.Empty);
            SetOptional(_hint2, data.Hints.Length > 1 ? data.Hints[1] : string.Empty);

            SetText(_powerHeader, data.PowerHeader);
            SetText(_output, data.Output);
            SetText(_network, data.Network);
            SetText(_efficiency, data.Efficiency);
            SetText(_fuelHeader, data.FuelHeader);
            SetText(_fuelDetail, data.FuelDetail);
            SetText(_waterHeader, data.WaterHeader);
            SetText(_waterDetail, data.WaterDetail);
            SetOptional(_waterBeaver, data.WaterBeaver);

            SetText(_condition, data.Condition);
            BoilerPanelNativeStyles.SetProgress(_conditionBar, data.ConditionPercent);
            SetText(_nextMaintenance, data.NextMaintenance);
            SetText(_materials, data.Materials);
            SetMaintenanceVisible(data.MaintenanceVisible);

            _autoMaintenance.SetValueWithoutNotify(data.AutoMaintenance);
            SetText(_maintainButton, data.MaintainLabel);
            _maintainButton.SetEnabled(data.CanMaintain);
            BoilerPanelNativeStyles.ApplyButtonTone(_maintainButton, data.CanMaintain);

            SetText(_pauseButton, data.PauseLabel);
            BoilerPanelNativeStyles.ApplyButtonTone(_pauseButton, data.Paused);

            SetText(_waterModeHeader, data.WaterModeHeader);
            UpdateModeChoices(data);
            SetOptional(_waterModeUnavailable, data.WaterModeUnavailable);
            SetVisible(true);
        }

        private void SetMaintenanceVisible(bool visible) =>
            BoilerPanelNativeStyles.SetVisible(_maintenanceSection, visible);

        private static void SetText(Label label, string text)
        {
            if (!string.Equals(label.text, text, StringComparison.Ordinal))
            {
                label.text = text;
            }
        }

        private static void SetText(Button button, string text)
        {
            if (!string.Equals(button.text, text, StringComparison.Ordinal))
            {
                button.text = text;
            }
        }

        private static void SetOptional(Label label, string text)
        {
            SetText(label, text);
            BoilerPanelNativeStyles.SetVisible(label, !string.IsNullOrEmpty(text));
        }

    }
}
