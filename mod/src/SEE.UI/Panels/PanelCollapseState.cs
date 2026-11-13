namespace SEE.UI.Panels
{
    /// <summary>
    /// The collapse state of the SEE block inside the entity panel (REQ-UI-12).
    ///
    /// Deliberately a tiny pure-logic type: the UIElements view owns one instance for its whole
    /// lifetime, so the player's expanded/collapsed choice survives every <c>Render</c> and the
    /// panel being closed and reopened, and it is never written to a save file. Keeping the rule
    /// here also makes "expanded by default" testable offline, without a game runtime.
    /// </summary>
    public sealed class PanelCollapseState
    {
        /// <summary>True while the block body is visible. New blocks start expanded.</summary>
        public bool IsExpanded { get; private set; } = true;

        /// <summary>Flips the state.</summary>
        /// <returns>The new value of <see cref="IsExpanded"/>.</returns>
        public bool Toggle()
        {
            IsExpanded = !IsExpanded;
            return IsExpanded;
        }
    }
}
