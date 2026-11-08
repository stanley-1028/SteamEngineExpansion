namespace SEE.Integration.Detection
{
    /// <summary>
    /// Compatibility detector contract (requirements §10.6). Implementations cache the result of
    /// one load (including <see cref="Availability.Degraded"/>) and never throw: a failed probe
    /// only means the optional feature is unavailable (REQ-COMPAT-01/09/10).
    /// </summary>
    public interface ICompatDetector
    {
        /// <summary>Returns the cached status of one optional mod.</summary>
        /// <param name="modId">Stable mod id, for example "TimberPipes".</param>
        /// <returns>The detection result; <see cref="Availability.Absent"/> for unknown ids.</returns>
        ExternalModStatus Detect(string modId);

        /// <summary>Returns the three optional-mod statuses in the frozen order.</summary>
        /// <returns>One status per optional mod; never null.</returns>
        System.Collections.Generic.IReadOnlyList<ExternalModStatus> DetectAll();

        /// <summary>Detects the unsupported legacy Power Lines build (report only, never integrated).</summary>
        /// <returns>Available when the legacy assembly is loaded.</returns>
        ExternalModStatus DetectLegacyPowerLines();

        /// <summary>Clears the session cache so the next query rescans (manual re-detect).</summary>
        void Refresh();
    }
}
