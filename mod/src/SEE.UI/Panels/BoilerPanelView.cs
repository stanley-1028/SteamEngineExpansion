using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.UI.Status;
using Timberborn.Localization;
using UnityEngine.UIElements;

namespace SEE.UI.Panels
{
    /// <summary>
    /// The UIElements tree of the boiler panel plus its one-way rendering path
    /// (requirements §15.1 P3/P4, §15.2, §15.4, REQ-UI-01/05/12).
    ///
    /// The tree is built exactly once in the constructor (never per frame, REQ-OPS-02). The six
    /// information blocks live inside one vanilla <see cref="Foldout"/>, so the SEE block is
    /// collapsible inside the regular building panel and keeps its collapsed state for the session
    /// (REQ-UI-12). <see cref="Render"/> only assigns text, colours, one bar width and two control
    /// values; the auto-maintenance toggle and the water-mode dropdown are updated with
    /// <c>SetValueWithoutNotify</c>, so a refresh can never re-trigger a command.
    /// </summary>
    public sealed class BoilerPanelView
    {
        private readonly Foldout _root;
        private readonly VisualElement _statusRow;
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
        private readonly Label _condition;
        private readonly VisualElement _conditionBar;
        private readonly VisualElement _conditionFill;
        private readonly Label _nextMaintenance;
        private readonly Label _materials;
        private readonly Toggle _autoMaintenance;
        private readonly Button _maintainButton;
        private readonly Label _waterModeHeader;
        private readonly DropdownField _waterMode;
        private readonly Label _waterModeUnavailable;
        private readonly Button _pauseButton;
        private readonly List<string> _modeLabels = new List<string>(4);

        private int _modeIndex;

        /// <summary>Builds the panel once and wires the four commands (CONTRACT_SEE §5.2 D).</summary>
        /// <param name="loc">Localization service for the static control labels.</param>
        /// <param name="pauseToggled">Invoked by the pause/resume button.</param>
        /// <param name="maintainRequested">Invoked by the maintain-now button.</param>
        /// <param name="autoMaintenanceChanged">Invoked with the new auto-maintenance value.</param>
        /// <param name="waterModeChanged">Invoked with the new zero-based water-supply mode index.</param>
        public BoilerPanelView(
            ILoc loc,
            Action pauseToggled,
            Action maintainRequested,
            Action<bool> autoMaintenanceChanged,
            Action<int> waterModeChanged)
        {
            if (loc is null)
            {
                throw new ArgumentNullException(nameof(loc));
            }

            // Expanded by default: a collapsed Foldout made the whole SEE panel look missing
            // (user report 2026-11-08: only a "Small Steam Engine Mk.I" chevron was visible).
            _root = new Foldout { value = true };
            _root.style.marginTop = 4f;
            _root.style.display = DisplayStyle.None;

            _statusRow = new VisualElement();
            _statusRow.style.flexDirection = FlexDirection.Row;
            _root.Add(_statusRow);
            _badgeSwatch = BoilerPanelStyles.CreateBadgeSwatch(_statusRow);
            _status = BoilerPanelStyles.AddRow(_statusRow);

            _subStatus = BoilerPanelStyles.AddRow(_root);
            BoilerPanelStyles.ApplyError(_subStatus);
            _fixHint = BoilerPanelStyles.AddRow(_root);
            BoilerPanelStyles.ApplyHint(_fixHint);
            _hint1 = BoilerPanelStyles.AddRow(_root);
            BoilerPanelStyles.ApplyHint(_hint1);
            _hint2 = BoilerPanelStyles.AddRow(_root);
            BoilerPanelStyles.ApplyHint(_hint2);

            _powerHeader = BoilerPanelStyles.AddHeader(_root);
            _output = BoilerPanelStyles.AddRow(_root);
            _network = BoilerPanelStyles.AddRow(_root);
            _efficiency = BoilerPanelStyles.AddRow(_root);

            _fuelHeader = BoilerPanelStyles.AddHeader(_root);
            _fuelDetail = BoilerPanelStyles.AddRow(_root);

            _waterHeader = BoilerPanelStyles.AddHeader(_root);
            _waterDetail = BoilerPanelStyles.AddRow(_root);
            _waterBeaver = BoilerPanelStyles.AddRow(_root);
            BoilerPanelStyles.ApplyHint(_waterBeaver);

            _condition = BoilerPanelStyles.AddRow(_root);
            _conditionBar = BoilerPanelStyles.CreateProgressBar(out _conditionFill);
            _root.Add(_conditionBar);
            _nextMaintenance = BoilerPanelStyles.AddRow(_root);
            _materials = BoilerPanelStyles.AddRow(_root);

            _autoMaintenance = new Toggle(loc.T(SEE.Core.Constants.SeeLocKeys.UiActionAutoMaintenance));
            BoilerPanelStyles.ApplyToggle(_autoMaintenance);
            _autoMaintenance.RegisterValueChangedCallback(evt => autoMaintenanceChanged(evt.newValue));
            _root.Add(_autoMaintenance);

            _maintainButton = new Button(maintainRequested)
            {
                text = loc.T(SEE.Core.Constants.SeeLocKeys.UiActionMaintainNow)
            };
            BoilerPanelStyles.ApplyButton(_maintainButton);
            _root.Add(_maintainButton);

            _waterModeHeader = BoilerPanelStyles.AddRow(_root);
            _waterModeHeader.style.marginTop = 4f;

            var choices = new List<string>(4)
            {
                loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Auto)),
                loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Tank)),
                loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Valve)),
                loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Hauler))
            };
            _modeLabels.AddRange(choices);
            _waterMode = new DropdownField(choices, 0, value => value, value => value);
            BoilerPanelStyles.ApplyDropdown(_waterMode);
            _waterMode.RegisterValueChangedCallback(evt => OnModePicked(evt.newValue, waterModeChanged));
            _root.Add(_waterMode);

            _waterModeUnavailable = BoilerPanelStyles.AddRow(_root);
            BoilerPanelStyles.ApplyError(_waterModeUnavailable);

            _pauseButton = new Button(pauseToggled)
            {
                text = loc.T(SEE.Core.Constants.SeeLocKeys.UiActionPause)
            };
            BoilerPanelStyles.ApplyButton(_pauseButton);
            _root.Add(_pauseButton);
        }

        /// <summary>Root element handed to the entity panel system.</summary>
        public VisualElement Root => _root;

        /// <summary>Shows or hides the whole fragment (other entity types add no rows).</summary>
        /// <param name="visible">True to show the boiler block.</param>
        public void SetVisible(bool visible) =>
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        /// <summary>Applies one localized payload to the existing elements.</summary>
        /// <param name="data">Payload from <see cref="BoilerPanelDataFactory"/>.</param>
        public void Render(BoilerPanelData data)
        {
            if (!data.HasData)
            {
                SetVisible(false);
                return;
            }

            _root.text = string.IsNullOrEmpty(data.Status)
                ? data.Title
                : data.Title + " · " + data.Status;
            _root.tooltip = data.TitleTooltip;
            SetText(_status, data.Status);
            BoilerPanelStyles.SetBadge(_status, _badgeSwatch, data.StatusColor, data.BadgeVisible);

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
            BoilerPanelStyles.SetProgress(_conditionFill, data.ConditionPercent);
            SetText(_nextMaintenance, data.NextMaintenance);
            SetText(_materials, data.Materials);
            SetMaintenanceVisible(data.MaintenanceVisible);

            _autoMaintenance.SetValueWithoutNotify(data.AutoMaintenance);
            _maintainButton.text = data.MaintainLabel;
            BoilerPanelStyles.SetButtonEnabled(_maintainButton, data.CanMaintain);
            _pauseButton.text = data.PauseLabel;

            SetText(_waterModeHeader, data.WaterModeHeader);
            UpdateModeChoices(data);
            SetOptional(_waterModeUnavailable, data.WaterModeUnavailable);
            SetVisible(true);
        }

        private void UpdateModeChoices(BoilerPanelData data)
        {
            if (data.WaterModeLabels.Length != _modeLabels.Count)
            {
                return;
            }

            bool changed = false;
            for (int i = 0; i < _modeLabels.Count; i++)
            {
                if (!string.Equals(_modeLabels[i], data.WaterModeLabels[i], StringComparison.Ordinal))
                {
                    _modeLabels[i] = data.WaterModeLabels[i];
                    changed = true;
                }
            }

            if (changed)
            {
                _waterMode.choices = new List<string>(_modeLabels);
            }

            int index = data.WaterModeIndex;
            if (index < 0 || index >= _modeLabels.Count)
            {
                index = 0;
            }

            _modeIndex = index;
            _waterMode.SetValueWithoutNotify(_modeLabels[index]);
        }

        private void OnModePicked(string label, Action<int> waterModeChanged)
        {
            if (label is null)
            {
                return;
            }

            for (int i = 0; i < _modeLabels.Count; i++)
            {
                if (string.Equals(_modeLabels[i], label, StringComparison.Ordinal))
                {
                    if (i != _modeIndex)
                    {
                        _modeIndex = i;
                        waterModeChanged(i);
                    }

                    return;
                }
            }
        }

        private void SetMaintenanceVisible(bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _condition.style.display = display;
            _conditionBar.style.display = display;
            _nextMaintenance.style.display = display;
            _materials.style.display = display;
            _autoMaintenance.style.display = display;
            _maintainButton.style.display = display;
        }

        private static void SetText(Label label, string text)
        {
            if (!string.Equals(label.text, text, StringComparison.Ordinal))
            {
                label.text = text;
            }
        }

        private static void SetText(Foldout root, string text)
        {
            if (!string.Equals(root.text, text, StringComparison.Ordinal))
            {
                root.text = text;
            }
        }

        private static void SetOptional(Label label, string text)
        {
            SetText(label, text);
            label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
