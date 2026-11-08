namespace SEE.Integration.Detection
{
    /// <summary>
    /// Three-state result of one compatibility probe (requirements §10.6). Any path that fails to
    /// resolve an anchor degrades the optional feature to <see cref="Degraded"/>; it never throws
    /// and never turns the optional mod into a hard dependency (REQ-COMPAT-01).
    /// </summary>
    public enum Availability
    {
        /// <summary>The optional mod is not loaded (or its assembly is not present).</summary>
        Absent = 0,

        /// <summary>The assembly exists but at least one anchor type is missing; treated as absent.</summary>
        Degraded = 1,

        /// <summary>The assembly and every expected anchor type resolved.</summary>
        Available = 2
    }
}
