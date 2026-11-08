using SEE.UI.Status;
using UnityEngine;
using UnityEngine.UIElements;

namespace SEE.UI.Panels
{
    /// <summary>
    /// Code-only styling for the boiler panel (requirements §15.1 P4, §15.6 A1).
    ///
    /// The mod ships no UXML/USS asset (a compiled mod assembly cannot reference the game's asset
    /// bundles), so every control that is not a plain label gets explicit inline colours and sizes
    /// here. All colours are static readonly values and no style is touched per frame
    /// (REQ-OPS-02); the status is encoded by colour AND by the badge swatch shape AND by the
    /// text, so a greyscale screenshot still distinguishes Critical from MaintenanceDue.
    /// </summary>
    public static class BoilerPanelStyles
    {
        private static readonly Color BarTrack = new Color(0.13f, 0.15f, 0.17f, 1f);
        private static readonly Color BarHigh = new Color(0.42f, 0.78f, 0.38f, 1f);
        private static readonly Color BarMedium = new Color(0.93f, 0.78f, 0.28f, 1f);
        private static readonly Color BarLow = new Color(0.90f, 0.55f, 0.22f, 1f);
        private static readonly Color BarCritical = new Color(0.86f, 0.29f, 0.23f, 1f);
        private static readonly Color ButtonBackground = new Color(0.24f, 0.42f, 0.62f, 1f);
        private static readonly Color ButtonBorder = new Color(0.16f, 0.30f, 0.46f, 1f);
        private static readonly Color ButtonDisabled = new Color(0.25f, 0.27f, 0.29f, 1f);
        private static readonly Color HintText = new Color(0.72f, 0.75f, 0.78f, 1f);
        private static readonly Color ErrorText = new Color(0.90f, 0.45f, 0.38f, 1f);
        private static readonly Color HeaderText = new Color(0.86f, 0.87f, 0.82f, 1f);
        private static readonly Color Neutral = new Color(0.86f, 0.87f, 0.82f, 1f);
        private static readonly Color Grey = new Color(0.65f, 0.67f, 0.70f, 1f);
        private static readonly Color Blue = new Color(0.35f, 0.63f, 0.95f, 1f);
        private static readonly Color Orange = new Color(0.93f, 0.60f, 0.24f, 1f);
        private static readonly Color Yellow = new Color(0.93f, 0.78f, 0.28f, 1f);
        private static readonly Color Red = new Color(0.86f, 0.29f, 0.23f, 1f);

        /// <summary>Creates one wrapping label row and adds it to the panel.</summary>
        /// <param name="parent">Panel root or section container.</param>
        /// <returns>The created label.</returns>
        public static Label AddRow(VisualElement parent)
        {
            var label = new Label();
            label.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(label);
            return label;
        }

        /// <summary>Creates a bold section header label.</summary>
        /// <param name="parent">Panel root.</param>
        /// <returns>The created header label.</returns>
        public static Label AddHeader(VisualElement parent)
        {
            Label label = AddRow(parent);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = HeaderText;
            label.style.marginTop = 4f;
            return label;
        }

        /// <summary>Styles a grey secondary hint line.</summary>
        /// <param name="label">Hint label.</param>
        public static void ApplyHint(Label label) => label.style.color = HintText;

        /// <summary>Styles a red error / blocked-action line.</summary>
        /// <param name="label">Error label.</param>
        public static void ApplyError(Label label) => label.style.color = ErrorText;

        /// <summary>Creates the square badge swatch shown in front of the status text (shape cue, A1).</summary>
        /// <param name="parent">Row container.</param>
        /// <returns>The swatch element; colour it with <see cref="SetBadge"/>.</returns>
        public static VisualElement CreateBadgeSwatch(VisualElement parent)
        {
            var swatch = new VisualElement();
            swatch.style.width = 10f;
            swatch.style.height = 10f;
            swatch.style.marginRight = 4f;
            swatch.style.marginTop = 3f;
            swatch.style.borderTopLeftRadius = 2f;
            swatch.style.borderTopRightRadius = 2f;
            swatch.style.borderBottomLeftRadius = 2f;
            swatch.style.borderBottomRightRadius = 2f;
            parent.Add(swatch);
            return swatch;
        }

        /// <summary>Applies the status colour to text and badge swatch.</summary>
        /// <param name="label">Status label.</param>
        /// <param name="swatch">Badge swatch; may be null.</param>
        /// <param name="band">Colour band from the status descriptor.</param>
        /// <param name="visible">False hides the swatch (Running shows no badge).</param>
        public static void SetBadge(Label label, VisualElement? swatch, StatusColorBand band, bool visible)
        {
            Color color = StatusColor(band);
            label.style.color = color;
            if (swatch is null)
            {
                return;
            }

            swatch.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            swatch.style.backgroundColor = color;
        }

        /// <summary>Maps a semantic band to the panel palette.</summary>
        /// <param name="band">Colour band.</param>
        /// <returns>The inline colour.</returns>
        public static Color StatusColor(StatusColorBand band)
        {
            switch (band)
            {
                case StatusColorBand.Grey:
                    return Grey;
                case StatusColorBand.Blue:
                    return Blue;
                case StatusColorBand.Orange:
                    return Orange;
                case StatusColorBand.Yellow:
                    return Yellow;
                case StatusColorBand.Red:
                    return Red;
                default:
                    return Neutral;
            }
        }

        /// <summary>Styles a plain UIElements button so it stays readable without a themed USS asset.</summary>
        /// <param name="button">Button to style.</param>
        public static void ApplyButton(Button button)
        {
            button.style.height = 22f;
            button.style.marginTop = 2f;
            button.style.marginBottom = 2f;
            button.style.paddingLeft = 8f;
            button.style.paddingRight = 8f;
            button.style.backgroundColor = ButtonBackground;
            button.style.color = Color.white;
            button.style.borderTopWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderTopColor = ButtonBorder;
            button.style.borderBottomColor = ButtonBorder;
            button.style.borderLeftColor = ButtonBorder;
            button.style.borderRightColor = ButtonBorder;
            button.style.borderTopLeftRadius = 3f;
            button.style.borderTopRightRadius = 3f;
            button.style.borderBottomLeftRadius = 3f;
            button.style.borderBottomRightRadius = 3f;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        /// <summary>Gives a UIElements toggle a predictable height inside the panel column.</summary>
        /// <param name="toggle">Toggle to style.</param>
        public static void ApplyToggle(Toggle toggle)
        {
            toggle.style.height = 20f;
            toggle.style.marginTop = 2f;
            toggle.style.marginBottom = 2f;
        }

        /// <summary>Gives the water-supply dropdown a predictable height and width.</summary>
        /// <param name="dropdown">Dropdown to style.</param>
        public static void ApplyDropdown(DropdownField dropdown)
        {
            dropdown.style.height = 22f;
            dropdown.style.marginTop = 2f;
            dropdown.style.marginBottom = 2f;
            dropdown.style.width = 150f;
        }

        /// <summary>Enables or disables a button and dims it while disabled (fail feedback, REQ-UI-06).</summary>
        /// <param name="button">Button to update.</param>
        /// <param name="enabled">Whether the command may be issued.</param>
        public static void SetButtonEnabled(Button button, bool enabled)
        {
            button.SetEnabled(enabled);
            button.style.backgroundColor = enabled ? ButtonBackground : ButtonDisabled;
        }

        /// <summary>Creates the condition progress bar and returns its fill element.</summary>
        /// <param name="fill">The element whose width is the condition.</param>
        /// <returns>The bar track, ready to be added to the panel.</returns>
        public static VisualElement CreateProgressBar(out VisualElement fill)
        {
            var track = new VisualElement();
            track.style.height = 8f;
            track.style.marginTop = 2f;
            track.style.marginBottom = 4f;
            track.style.backgroundColor = BarTrack;
            track.style.borderTopLeftRadius = 2f;
            track.style.borderTopRightRadius = 2f;
            track.style.borderBottomLeftRadius = 2f;
            track.style.borderBottomRightRadius = 2f;

            fill = new VisualElement();
            fill.style.height = Length.Percent(100f);
            fill.style.width = Length.Percent(0f);
            fill.style.backgroundColor = BarHigh;
            track.Add(fill);
            return track;
        }

        /// <summary>Applies a 0..100 condition value to the bar fill (width + colour band).</summary>
        /// <param name="fill">Bar fill element from <see cref="CreateProgressBar"/>.</param>
        /// <param name="percent">Condition in percent.</param>
        public static void SetProgress(VisualElement fill, float percent)
        {
            double clamped = double.IsNaN(percent) || double.IsInfinity(percent)
                ? 0.0
                : Mathf.Clamp(percent, 0f, 100f);
            fill.style.width = Length.Percent((float)clamped);
            fill.style.backgroundColor = BarColor(clamped);
        }

        private static Color BarColor(double percent)
        {
            if (percent >= 75.0)
            {
                return BarHigh;
            }

            if (percent >= 50.0)
            {
                return BarMedium;
            }

            return percent >= 25.0 ? BarLow : BarCritical;
        }
    }
}
