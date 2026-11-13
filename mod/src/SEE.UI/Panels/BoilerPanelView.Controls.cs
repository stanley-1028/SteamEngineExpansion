using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.UI.Status;
using Timberborn.DropdownSystem;
using Timberborn.Localization;
using UnityEngine.UIElements;

namespace SEE.UI.Panels
{
    /// <summary>
    /// Water-supply mode control (game <see cref="Dropdown"/> + provider + fallback) and the
    /// native collapse toggle of the boiler panel. The second half of
    /// <see cref="BoilerPanelView"/>; partial on purpose so no source file exceeds the contract
    /// 400-line limit (CONTRACT_SEE §11.5).
    /// </summary>
    public sealed partial class BoilerPanelView
    {
        /// <summary>
        /// Builds the water-supply mode control: the game's own <see cref="Dropdown"/> driven by an
        /// <see cref="IDropdownProvider"/>. Any construction failure (missing asset, renamed API)
        /// falls back to a plain <see cref="DropdownField"/> with the same native text class so the
        /// panel never breaks.
        /// </summary>
        private void CreateWaterModeControl(ILoc loc, DropdownListDrawer dropdownListDrawer,
            VisualElement parent, Action<int> waterModeChanged)
        {
            _modeLabels.Add(loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Auto)));
            _modeLabels.Add(loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Tank)));
            _modeLabels.Add(loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Valve)));
            _modeLabels.Add(loc.T(StatusDescriptors.WaterSupplyModeLocKey(WaterSupplyMode.Hauler)));

            try
            {
                var dropdown = new Dropdown();
                dropdown.Initialize(dropdownListDrawer);
                BoilerPanelNativeStyles.ApplyNativeDropdownClass(dropdown);
                // The localized header is the separate native label above the control, so the
                // template's own label is hidden instead of being localized from a loc key.
                dropdown.Localize(loc);

                _provider = new WaterModeDropdownProvider(_modeLabels);
                _dropdownItemsSetter.SetItems(dropdown, _provider);
                dropdown.ValueChanged += (sender, args) => OnWaterModePicked(waterModeChanged);
                _waterMode = dropdown;
                parent.Add(dropdown);
            }
            catch (Exception)
            {
                _waterMode = null;
                var fallback = new DropdownField(new List<string>(_modeLabels), 0, value => value, value => value);
                BoilerPanelNativeStyles.ApplyNativeTextClass(fallback);
                fallback.RegisterValueChangedCallback(evt => OnFallbackModePicked(evt.newValue, waterModeChanged));
                _waterModeFallback = fallback;
                parent.Add(fallback);
            }
        }

        private void ToggleCollapsed()
        {
            bool expanded = _collapse.Toggle();
            _headerChevron.text = expanded ? ExpandedChevron : CollapsedChevron;
            BoilerPanelNativeStyles.SetVisible(_body, expanded);
        }

        private void OnWaterModePicked(Action<int> waterModeChanged)
        {
            int index = _provider?.Index ?? _modeIndex;
            if (index == _modeIndex)
            {
                return;
            }

            _modeIndex = index;
            waterModeChanged(index);
        }

        private void OnFallbackModePicked(string label, Action<int> waterModeChanged)
        {
            if (label is null)
            {
                return;
            }

            int index = _modeLabels.IndexOf(label);
            if (index < 0 || index == _modeIndex)
            {
                return;
            }

            _modeIndex = index;
            waterModeChanged(index);
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

            int index = data.WaterModeIndex;
            if (index < 0 || index >= _modeLabels.Count)
            {
                index = 0;
            }

            _modeIndex = index;

            if (_provider != null && _waterMode != null)
            {
                _provider.SetIndex(index);
                if (changed)
                {
                    // Rebuild the cached item labels (Dropdown caches them after the first open)
                    // then show the current value; SetItems itself does not raise ValueChanged.
                    _dropdownItemsSetter.SetItems(_waterMode, _provider);
                }
                else
                {
                    _waterMode.UpdateSelectedValue();
                }
            }
            else if (_waterModeFallback != null)
            {
                if (changed)
                {
                    _waterModeFallback.choices = new List<string>(_modeLabels);
                }

                _waterModeFallback.SetValueWithoutNotify(_modeLabels[index]);
            }
        }

        /// <summary>
        /// The four localized water-supply mode labels, with the selected index. The provider is
        /// created after <see cref="_modeLabels"/> is filled and shares that list, so a language
        /// change updates the items without a second data source (REQ-UI-08: no new keys).
        /// </summary>
        private sealed class WaterModeDropdownProvider : IDropdownProvider
        {
            private readonly List<string> _labels;
            private int _index;

            internal WaterModeDropdownProvider(List<string> labels)
            {
                _labels = labels;
            }

            /// <summary>Selected zero-based index.</summary>
            internal int Index => _index;

            /// <inheritdoc />
            public IReadOnlyList<string> Items => _labels;

            /// <inheritdoc />
            public string GetValue() => _labels[_index];

            /// <inheritdoc />
            public void SetValue(string value)
            {
                for (int i = 0; i < _labels.Count; i++)
                {
                    if (string.Equals(_labels[i], value, StringComparison.Ordinal))
                    {
                        _index = i;
                        return;
                    }
                }
            }

            /// <summary>Selects one index after clamping into the four frozen modes.</summary>
            /// <param name="index">Zero-based index; out-of-range leaves the selection alone.</param>
            internal void SetIndex(int index)
            {
                if (index >= 0 && index < _labels.Count)
                {
                    _index = index;
                }
            }
        }
    }
}
