namespace SEE.Integration.Detection
{
    /// <summary>
    /// Result of one compatibility detection pass (requirements §10.6). Third-party types never
    /// appear in this public signature: only strings, so SEE.Integration stays free of compile-time
    /// references to external mod assemblies (REQ-COMPAT-08).
    /// <para>
    /// <see cref="Level"/> is the frozen three-state result. <see cref="Detected"/> is the diagnostic
    /// answer "the assembly is loaded" and is deliberately independent of <see cref="Level"/>: a
    /// <see cref="Availability.Degraded"/> module is detected but must be treated as absent by every
    /// adapter (REQ-COMPAT-10).
    /// </para>
    /// </summary>
    public readonly struct ExternalModStatus
    {
        /// <summary>Creates a status.</summary>
        /// <param name="modId">Stable mod id, for example "TimberPipes".</param>
        /// <param name="level">Detection result.</param>
        /// <param name="version">Best-effort assembly version, or null.</param>
        /// <param name="detail">Short machine-readable detail for the compat log, or null.</param>
        /// <param name="detected">True when the external assembly itself was seen.</param>
        /// <param name="versionNote">Human-readable note about version comparison, or null.</param>
        public ExternalModStatus(
            string modId,
            Availability level,
            string? version,
            string? detail,
            bool detected,
            string? versionNote)
        {
            ModId = modId;
            Level = level;
            Version = version;
            Detail = detail;
            Detected = detected;
            VersionNote = versionNote;
        }

        /// <summary>Stable mod id ("TimberPipes", "PowerLines", "ConveyorBelt").</summary>
        public string ModId { get; }

        /// <summary>Detection result.</summary>
        public Availability Level { get; }

        /// <summary>Best-effort assembly version string; null when unavailable.</summary>
        public string? Version { get; }

        /// <summary>Short machine-readable detail, for example "anchor-missing: PowerLines.Specs.PowerLineSpec".</summary>
        public string? Detail { get; }

        /// <summary>True when the external assembly was seen, even if <see cref="Level"/> is Degraded.</summary>
        public bool Detected { get; }

        /// <summary>
        /// Note about the version gate: "assembly-metadata-version" when the assembly version cannot
        /// be compared with the manifest minimum (all three shipped mods report 1.0.0+hash while
        /// their manifests say 11.x), or "below-minimum" when a same-major version failed the gate.
        /// Never null when the version gate was evaluated.
        /// </summary>
        public string? VersionNote { get; }

        /// <summary>True when the optional feature may be used (anchors and version gate passed).</summary>
        public bool IsAvailable => Level == Availability.Available;

        /// <summary>Builds an "absent" status with the given detail.</summary>
        /// <param name="modId">Stable mod id.</param>
        /// <param name="detail">Short detail for diagnostics.</param>
        /// <returns>The absent status.</returns>
        public static ExternalModStatus Absent(string modId, string detail) =>
            new ExternalModStatus(modId, Availability.Absent, null, detail, false, null);

        /// <summary>Builds a "degraded" status (assembly seen, anchors incomplete or version gate failed).</summary>
        /// <param name="modId">Stable mod id.</param>
        /// <param name="version">Detected version, if known.</param>
        /// <param name="detail">Short detail for diagnostics.</param>
        /// <param name="versionNote">Version-gate note, if any.</param>
        /// <returns>The degraded status.</returns>
        public static ExternalModStatus Degraded(string modId, string? version, string detail, string? versionNote = null) =>
            new ExternalModStatus(modId, Availability.Degraded, version, detail, true, versionNote);

        /// <summary>Builds an "available" status.</summary>
        /// <param name="modId">Stable mod id.</param>
        /// <param name="version">Detected version, if known.</param>
        /// <param name="versionNote">Version-gate note, if any.</param>
        /// <returns>The available status.</returns>
        public static ExternalModStatus Available(string modId, string? version, string? versionNote = null) =>
            new ExternalModStatus(modId, Availability.Available, version, "ok", true, versionNote);

        /// <summary>Builds a synthetic status used before the first scan ran.</summary>
        /// <param name="modId">Stable mod id.</param>
        /// <returns>An absent status with detail "not-scanned".</returns>
        public static ExternalModStatus NotScanned(string modId) => Absent(modId, "not-scanned");
    }
}
