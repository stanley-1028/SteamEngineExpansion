namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Why the water-supply mode the player selected cannot run right now (requirements §15.2,
    /// REQ-UI-07). The panel prints one explanatory line instead of silently running a different
    /// mode, so an unavailable mode is never a silent failure (REQ-COMPAT-06).
    /// <para>
    /// The value is a plain Core enum: SEE.UI reads it through <see cref="IBoilerStatusView"/> and
    /// never references the integration assembly that actually detects the external mods.
    /// </para>
    /// </summary>
    public enum WaterSupplyAvailability
    {
        /// <summary>The selected mode can run (or only the automatic/hauler fallback is needed).</summary>
        Available = 0,

        /// <summary>The tank/valve mode needs Timber Pipes, which is not installed or failed detection.</summary>
        TimberPipesMissing = 1,

        /// <summary>The mode exists in the contract but the current boiler/network cannot honour it.</summary>
        ModeNotApplicable = 2,

        /// <summary>The pipe network is detected but reports contaminated water.</summary>
        PipeContaminated = 3
    }
}
