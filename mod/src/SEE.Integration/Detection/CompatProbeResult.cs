namespace SEE.Integration.Detection
{
    /// <summary>
    /// Raw outcome of probing one optional mod. Kept free of game and third-party types so a fake
    /// probe can be injected by the offline tests without loading any real DLL
    /// (CONTRACT_SEE §1.4, REQ-COMPAT-09/10).
    /// </summary>
    public readonly struct CompatProbeResult
    {
        private CompatProbeResult(
            bool assemblyPresent,
            string? assemblyVersion,
            string? anchorMissing,
            string? failure)
        {
            AssemblyPresent = assemblyPresent;
            AssemblyVersion = assemblyVersion;
            AnchorMissing = anchorMissing;
            Failure = failure;
        }

        /// <summary>True when an assembly with the expected simple name was found.</summary>
        public bool AssemblyPresent { get; }

        /// <summary>Best-effort version text read from the matched assembly; null when unknown.</summary>
        public string? AssemblyVersion { get; }

        /// <summary>Full name of the first missing anchor type; null when every anchor resolved.</summary>
        public string? AnchorMissing { get; }

        /// <summary>Non-null when the probe itself threw or the assembly list was unavailable.</summary>
        public string? Failure { get; }

        /// <summary>True when the probe completed and every expected anchor resolved.</summary>
        public bool AnchorsResolved => AssemblyPresent && AnchorMissing == null && Failure == null;

        /// <summary>The assembly (or its dependencies) is not loaded.</summary>
        /// <returns>An absent result.</returns>
        public static CompatProbeResult AssemblyNotFound() => new CompatProbeResult(false, null, null, null);

        /// <summary>The assembly matched and every anchor resolved.</summary>
        /// <param name="version">Detected version, if known.</param>
        /// <returns>An available result.</returns>
        public static CompatProbeResult AssemblyFound(string? version) => new CompatProbeResult(true, version, null, null);

        /// <summary>The assembly matched but one anchor type is missing.</summary>
        /// <param name="version">Detected version, if known.</param>
        /// <param name="anchorType">Full name of the missing type.</param>
        /// <returns>A degraded result.</returns>
        public static CompatProbeResult AnchorNotFound(string? version, string anchorType) =>
            new CompatProbeResult(true, version, anchorType, null);

        /// <summary>The probe failed (reflection exception, unreadable assembly list).</summary>
        /// <param name="failure">Short failure key.</param>
        /// <returns>A failed result.</returns>
        public static CompatProbeResult ProbeFailed(string failure) => new CompatProbeResult(false, null, null, failure);
    }
}
