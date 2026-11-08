namespace SEE.Integration.Adapters
{
    /// <summary>
    /// Lifecycle state of one optional-mod adapter (requirements §10.6/§10.7, REQ-COMPAT-17).
    /// <para>
    /// <see cref="Absent"/> and <see cref="Failed"/> are inert (no events, no UI writes);
    /// <see cref="Detected"/> means the mod is usable but the adapter is intentionally not enabled
    /// (for example Power Lines injection turned off in the config); <see cref="Enabled"/> is the
    /// only state in which an adapter may write anything. Degraded detection maps to
    /// <see cref="Failed"/> so it is recorded in the save compat block instead of being silently
    /// half-integrated (REQ-COMPAT-10).
    /// </para>
    /// </summary>
    public enum AdapterState
    {
        /// <summary>The optional mod is not loaded.</summary>
        Absent = 0,

        /// <summary>The mod was detected but the adapter is deliberately not enabled.</summary>
        Detected = 1,

        /// <summary>The adapter is initialized and active.</summary>
        Enabled = 2,

        /// <summary>Detection degraded or initialization failed; treated as not installed.</summary>
        Failed = 3
    }
}
