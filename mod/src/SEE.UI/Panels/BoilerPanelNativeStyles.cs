using System;
using SEE.UI.Status;
using Timberborn.CoreUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace SEE.UI.Panels
{
    /// <summary>
    /// The vanilla building blocks the boiler panel is composed of (REQ-UI-01/05/12).
    ///
    /// A compiled mod assembly cannot reference the game's UXML/USS asset bundles, so the panel
    /// never ships a stylesheet of its own. Instead every element uses the exact controls and USS
    /// class names the shipped vanilla entity-panel fragments use (<c>entity-sub-panel</c> +
    /// <c>bg-sub-box--green</c> nine-slice cards, <c>entity-panel__text</c>/<c>__sub-text</c>/
    /// <c>__heading-text</c> rows, <c>text--bold</c>, <c>game-text--red</c>/<c>text--yellow</c>
    /// severity colours, <c>entity-panel__button-wrapper</c> + real native
    /// <c>entity-fragment__button</c> nine-slice buttons, <c>game-toggle</c> toggles,
    /// <c>progress-bar</c> bars and <c>game-dropdown</c>). The game attaches CoreStyle,
    /// CommonStyle, EntityPanelCommonStyle, EntityPanelGameStyle and GameStyle to the entity-panel
    /// hierarchy, so these classes are styled exactly like the vanilla fragments.
    ///
    /// No inline colour, font size or fixed size is ever set here. The single exception is the
    /// status badge swatch (requirement A1): it is a shape/colour cue that must be distinguishable
    /// in a greyscale screenshot, so its inline <c>backgroundColor</c> is intentional and is the
    /// only inline colour in the whole assembly (see <see cref="CreateBadgeSwatch"/>).
    /// </summary>
    public static partial class BoilerPanelNativeStyles
    {
        /// <summary>
        /// Vanilla entity-panel template that contains exactly one <c>entity-fragment__button</c>
        /// (a real native nine-slice button with the game's hover/press skins and click sound).
        /// Used as the button factory because building a nine-slice button by hand would have to
        /// recreate the game's custom-style background resolution.
        /// </summary>
        private const string ButtonTemplateName = "Game/EntityPanel/DemolishableFragment";

        /// <summary>Vanilla card padding class (EntityPanelCommonStyle.uss).</summary>
        private const string SectionClass = "entity-sub-panel";

        /// <summary>Green nine-slice sub-panel frame every normal vanilla fragment uses.</summary>
        private const string SectionGreenClass = "bg-sub-box--green";

        /// <summary>Vanilla row container (EntityPanelCommonStyle.uss).</summary>
        private const string RowClass = "entity-panel__row";

        /// <summary>Native body text (13 px, wrapping).</summary>
        private const string TextClass = "entity-panel__text";

        /// <summary>Native secondary text (12 px, wrapping) for hints.</summary>
        private const string SubTextClass = "entity-panel__sub-text";

        /// <summary>Native section heading text (14 px, wrapping).</summary>
        private const string HeadingTextClass = "entity-panel__heading-text";

        /// <summary>Native bold modifier (CoreStyle.uss).</summary>
        private const string BoldClass = "text--bold";

        /// <summary>
        /// Native 13 px grey text used as the status base. Deliberately not
        /// <c>entity-panel__text</c>: EntityPanelCommonStyle is attached after CommonStyle and would
        /// override the native red/yellow severity colour classes.
        /// </summary>
        private const string StatusBaseClass = "game-text-normal";

        /// <summary>Native red severity text class (CommonStyle.uss).</summary>
        private const string DangerTextClass = "game-text--red";

        /// <summary>Native yellow warning text class (CommonStyle.uss).</summary>
        private const string WarningTextClass = "text--yellow";

        /// <summary>Native row that centres its buttons (EntityPanelCommonStyle.uss).</summary>
        private const string ButtonWrapperClass = "entity-panel__button-wrapper";

        /// <summary>Native fragment button base class.</summary>
        private const string ButtonClass = "entity-fragment__button";

        /// <summary>Native red nine-slice button skin.</summary>
        private const string ButtonDangerClass = "entity-fragment__button--red";

        /// <summary>Native green nine-slice button skin.</summary>
        private const string ButtonPositiveClass = "entity-fragment__button--green";

        /// <summary>Native toggle class used by every entity-panel toggle.</summary>
        private const string ToggleClass = "game-toggle";

        /// <summary>Native entity-panel toggle spacing class.</summary>
        private const string PanelToggleClass = "entity-panel__toggle";

        /// <summary>Native progress-bar frame class.</summary>
        private const string ProgressBarClass = "progress-bar";

        /// <summary>Native green progress-bar fill variant (GameStyle.uss).</summary>
        private const string ProgressBarGreenClass = "progress-bar--green";

        /// <summary>Native red progress-bar fill variant (GameStyle.uss).</summary>
        private const string ProgressBarDangerClass = "progress-bar--red";

        /// <summary>
        /// Native clickable entity-name row (EntityPanel.uxml <c>EntityName</c>): a plain button
        /// whose child labels carry the real text, so the collapse header inherits the vanilla
        /// name-row behaviour, including the yellow hover colour.
        /// </summary>
        private const string HeaderButtonClass = "entity-panel__name-wrapper";

        /// <summary>Native entity-name text class that participates in the header hover rule.</summary>
        private const string HeaderTitleClass = "entity-panel__name";

        /// <summary>Native dropdown spacing class every vanilla entity-panel dropdown carries.</summary>
        private const string DropdownClass = "game-dropdown";

        // ----- cards and rows ----------------------------------------------------------------

        /// <summary>Creates a native nine-slice sub-panel card and adds it to the panel.</summary>
        /// <param name="parent">Element that receives the card.</param>
        /// <returns>An empty card with the native green frame and padding.</returns>
        public static NineSliceVisualElement CreateSection(VisualElement parent)
        {
            var section = new NineSliceVisualElement();
            section.AddToClassList(SectionClass);
            section.AddToClassList(SectionGreenClass);
            parent.Add(section);
            return section;
        }

        /// <summary>Creates a native horizontal row container.</summary>
        /// <param name="parent">Element that receives the row.</param>
        /// <returns>The created row.</returns>
        public static VisualElement CreateRow(VisualElement parent)
        {
            var row = new VisualElement();
            row.AddToClassList(RowClass);
            parent.Add(row);
            return row;
        }

        /// <summary>Creates one native body-text row.</summary>
        /// <param name="parent">Element that receives the label.</param>
        /// <returns>The created label.</returns>
        public static Label CreateTextRow(VisualElement parent)
        {
            var label = new Label();
            label.AddToClassList(TextClass);
            parent.Add(label);
            return label;
        }

        /// <summary>Creates one native secondary-text row (hint).</summary>
        /// <param name="parent">Element that receives the label.</param>
        /// <returns>The created label.</returns>
        public static Label CreateSubTextRow(VisualElement parent)
        {
            var label = new Label();
            label.AddToClassList(SubTextClass);
            parent.Add(label);
            return label;
        }

        /// <summary>Creates one native bold section-heading row.</summary>
        /// <param name="parent">Element that receives the label.</param>
        /// <returns>The created label.</returns>
        public static Label CreateSectionHeader(VisualElement parent)
        {
            var label = new Label();
            label.AddToClassList(HeadingTextClass);
            label.AddToClassList(BoldClass);
            parent.Add(label);
            return label;
        }

        /// <summary>Creates the native status label (severity colour is applied by <see cref="SetBadge"/>).</summary>
        /// <param name="parent">Element that receives the label.</param>
        /// <returns>The created status label.</returns>
        public static Label CreateStatusText(VisualElement parent)
        {
            var label = new Label();
            label.AddToClassList(StatusBaseClass);
            parent.Add(label);
            return label;
        }

        /// <summary>Creates a native red error/blocked-action row.</summary>
        /// <param name="parent">Element that receives the label.</param>
        /// <returns>The created label.</returns>
        public static Label CreateErrorText(VisualElement parent)
        {
            var label = new Label();
            label.AddToClassList(StatusBaseClass);
            label.AddToClassList(DangerTextClass);
            parent.Add(label);
            return label;
        }

        // ----- status badge (A1) -------------------------------------------------------------

        /// <summary>
        /// Creates the square status swatch shown in front of the status text (requirement A1).
        /// This is the one place in SEE.UI that sets an inline colour: the swatch must stay
        /// readable as a shape/colour cue in a greyscale screenshot, which no vanilla USS class
        /// can express for the seven semantic bands.
        /// </summary>
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

        /// <summary>
        /// Applies the native severity text class and the A1 swatch for one status band.
        /// Only class membership and the swatch colour change; never a text colour or font size.
        /// </summary>
        /// <param name="label">Status label from <see cref="CreateStatusText"/>.</param>
        /// <param name="swatch">Swatch from <see cref="CreateBadgeSwatch"/>.</param>
        /// <param name="band">Colour band of the highest-priority status.</param>
        /// <param name="swatchVisible">False hides the swatch (Running shows no badge).</param>
        public static void SetBadge(Label label, VisualElement swatch, StatusColorBand band, bool swatchVisible)
        {
            bool danger = band == StatusColorBand.Red;
            bool warning = !danger && (band == StatusColorBand.Orange || band == StatusColorBand.Yellow);
            label.EnableInClassList(DangerTextClass, danger);
            label.EnableInClassList(WarningTextClass, warning);

            SetVisible(swatch, swatchVisible);
            // A1: the shape/colour badge is the intentional inline-colour exception (see the class
            // summary); everything else is encoded through vanilla USS classes.
            swatch.style.backgroundColor = StatusColor(band);
        }

        /// <summary>Shows or hides an element the same way the vanilla fragments do.</summary>
        /// <param name="element">Element to toggle.</param>
        /// <param name="visible">True to display it.</param>
        public static void SetVisible(VisualElement element, bool visible) => element.ToggleDisplayStyle(visible);

    }
}
