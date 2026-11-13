using System;
using SEE.UI.Status;
using Timberborn.CoreUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace SEE.UI.Panels
{
    /// <summary>
    /// Native controls of the boiler panel (buttons, toggle, progress bar, collapsible header).
    /// The second half of <see cref="BoilerPanelNativeStyles"/>; partial on purpose so no source
    /// file exceeds the contract 400-line limit (CONTRACT_SEE §11.5).
    /// </summary>
    public static partial class BoilerPanelNativeStyles
    {
        // ----- controls ----------------------------------------------------------------------

        /// <summary>Applies the native body-text class to a control that is not a plain label.</summary>
        /// <param name="element">Control (for example the fallback dropdown field).</param>
        public static void ApplyNativeTextClass(VisualElement element) => element.AddToClassList(TextClass);

        /// <summary>Applies the native <c>game-dropdown</c> class (also used by the fallback field).</summary>
        /// <param name="element">Dropdown element.</param>
        public static void ApplyNativeDropdownClass(VisualElement element) => element.AddToClassList(DropdownClass);

        /// <summary>Creates the native centred button row.</summary>
        /// <param name="parent">Element that receives the wrapper.</param>
        /// <returns>The created wrapper.</returns>
        public static VisualElement CreateButtonWrapper(VisualElement parent)
        {
            var wrapper = new VisualElement();
            wrapper.AddToClassList(ButtonWrapperClass);
            parent.Add(wrapper);
            return wrapper;
        }

        /// <summary>
        /// Creates a real native nine-slice fragment button. The button is taken from a vanilla
        /// UXML template so the game's own skins, hover/press states and click sound are reused;
        /// if the template ever stops providing one the panel falls back to a code-created native
        /// nine-slice button, and finally to a plain UIElements button, so it can never break.
        /// </summary>
        /// <param name="wrapper">Button row from <see cref="CreateButtonWrapper"/>.</param>
        /// <param name="loader">Game service that loads the vanilla UXML templates.</param>
        /// <param name="text">Initial (localized) label.</param>
        /// <param name="green">True for the native green skin, false for the native red skin.</param>
        /// <param name="clicked">Command invoked on click.</param>
        /// <returns>The created button; relabel and re-tone it on every render.</returns>
        public static Button CreateFragmentButton(VisualElement wrapper, VisualElementLoader loader, string text,
            bool green, Action clicked)
        {
            Button button = TakeTemplateButton(loader) ?? CreateFallbackButton();
            ApplyButtonTone(button, green);
            button.text = text;
            button.clicked += clicked;
            wrapper.Add(button);
            return button;
        }

        /// <summary>Switches a native fragment button between the green and red skins.</summary>
        /// <param name="button">Button created by <see cref="CreateFragmentButton"/>.</param>
        /// <param name="green">True for the green skin, false for the red skin.</param>
        public static void ApplyButtonTone(Button button, bool green)
        {
            button.EnableInClassList(ButtonPositiveClass, green);
            button.EnableInClassList(ButtonDangerClass, !green);
        }

        /// <summary>Creates a native game toggle with a localized label.</summary>
        /// <param name="parent">Element that receives the toggle.</param>
        /// <param name="label">Already localized toggle label.</param>
        /// <returns>The created toggle.</returns>
        public static Toggle CreateToggle(VisualElement parent, string label)
        {
            var toggle = new Toggle(label);
            toggle.AddToClassList(ToggleClass);
            toggle.AddToClassList(TextClass);
            toggle.AddToClassList(PanelToggleClass);
            parent.Add(toggle);
            return toggle;
        }

        /// <summary>
        /// Creates the native progress bar whose label overlays the fill (the vanilla
        /// DeteriorableFragment/DemolishableFragment pattern).
        /// </summary>
        /// <param name="parent">Element that receives the bar.</param>
        /// <param name="label">Label placed inside the bar; the view assigns its text.</param>
        /// <returns>The created bar; call <see cref="SetProgress"/> to update it.</returns>
        public static global::Timberborn.CoreUI.ProgressBar CreateProgressBar(VisualElement parent, out Label label)
        {
            var bar = new global::Timberborn.CoreUI.ProgressBar();
            bar.AddToClassList(ProgressBarClass);
            bar.AddToClassList(ProgressBarGreenClass);

            label = new Label();
            label.AddToClassList(TextClass);
            bar.Add(label);

            parent.Add(bar);
            return bar;
        }

        /// <summary>Applies a 0..100 condition value to a native progress bar (green above half).</summary>
        /// <param name="bar">Bar from <see cref="CreateProgressBar"/>.</param>
        /// <param name="percent">Condition in percent.</param>
        public static void SetProgress(global::Timberborn.CoreUI.ProgressBar bar, double percent)
        {
            double clamped = double.IsNaN(percent) || double.IsInfinity(percent)
                ? 0.0
                : Math.Max(0.0, Math.Min(100.0, percent));
            bar.SetProgress((float)(clamped / 100.0));

            bool healthy = clamped >= 50.0;
            bar.EnableInClassList(ProgressBarGreenClass, healthy);
            bar.EnableInClassList(ProgressBarDangerClass, !healthy);
        }

        // ----- collapsible header ------------------------------------------------------------

        /// <summary>Creates the clickable collapse header row (vanilla entity-name button pattern).</summary>
        /// <param name="parent">Element that receives the header.</param>
        /// <param name="clicked">Invoked when the player clicks the header.</param>
        /// <returns>The header button; put the chevron and title labels inside it.</returns>
        public static Button CreateCollapseHeader(VisualElement parent, Action clicked)
        {
            var header = new Button(clicked);
            header.AddToClassList(HeaderButtonClass);
            parent.Add(header);
            return header;
        }

        /// <summary>Creates the chevron label shown in front of the collapse header title.</summary>
        /// <param name="header">Header button from <see cref="CreateCollapseHeader"/>.</param>
        /// <returns>The chevron label; the view owns its ▾/▸ text.</returns>
        public static Label CreateCollapseChevron(VisualElement header)
        {
            var label = new Label();
            label.AddToClassList(TextClass);
            header.Add(label);
            return label;
        }

        /// <summary>
        /// Creates the collapse header title (native bold heading inside the clickable name row);
        /// the vanilla hover rule turns it yellow, like the entity name.
        /// </summary>
        /// <param name="header">Header button from <see cref="CreateCollapseHeader"/>.</param>
        /// <returns>The title label; the view assigns the building name.</returns>
        public static Label CreateCollapseTitle(VisualElement header)
        {
            var label = new Label();
            label.AddToClassList(HeaderTitleClass);
            label.AddToClassList(HeadingTextClass);
            label.AddToClassList(BoldClass);
            header.Add(label);
            return label;
        }

        // ----- internals ---------------------------------------------------------------------

        /// <summary>Maps a semantic band to the A1 swatch colour.</summary>
        /// <param name="band">Colour band.</param>
        /// <returns>The inline swatch colour (never used for text).</returns>
        private static Color StatusColor(StatusColorBand band)
        {
            switch (band)
            {
                case StatusColorBand.Grey:
                    return new Color(0.65f, 0.67f, 0.70f, 1f);
                case StatusColorBand.Blue:
                    return new Color(0.35f, 0.63f, 0.95f, 1f);
                case StatusColorBand.Orange:
                    return new Color(0.93f, 0.60f, 0.24f, 1f);
                case StatusColorBand.Yellow:
                    return new Color(0.93f, 0.78f, 0.28f, 1f);
                case StatusColorBand.Red:
                    return new Color(0.86f, 0.29f, 0.23f, 1f);
                default:
                    return new Color(0.86f, 0.87f, 0.82f, 1f);
            }
        }

        /// <summary>Takes the native nine-slice button out of the vanilla fragment template.</summary>
        /// <param name="loader">Game service that loads the vanilla UXML templates.</param>
        /// <returns>The native button, or null when the template no longer provides one.</returns>
        private static Button? TakeTemplateButton(VisualElementLoader loader)
        {
            try
            {
                VisualElement template = loader.LoadVisualElement(ButtonTemplateName);
                Button? button = template.Q<Button>(null, ButtonClass);
                if (button != null)
                {
                    button.RemoveFromHierarchy();
                    return button;
                }
            }
            catch (Exception)
            {
                // A missing or renamed vanilla template must never break the panel.
            }

            return null;
        }

        /// <summary>
        /// Creates a plain UIElements button as the last resort when the vanilla template cannot be
        /// loaded. The game's concrete nine-slice button type is internal in this build, so the
        /// native look can only come from the template; this keeps the command reachable.
        /// </summary>
        /// <returns>A button that still carries the native fragment-button classes.</returns>
        private static Button CreateFallbackButton()
        {
            var button = new Button();
            button.AddToClassList(ButtonClass);
            button.AddToClassList(TextClass);
            return button;
        }
    }
}
