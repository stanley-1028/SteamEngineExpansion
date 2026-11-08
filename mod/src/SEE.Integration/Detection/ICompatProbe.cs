namespace SEE.Integration.Detection
{
    /// <summary>
    /// Injection seam of the three-stage compatibility detection (requirements §10.6,
    /// REQ-COMPAT-09). The shipped implementation (<see cref="AppDomainCompatProbe"/>) reads the
    /// loaded assembly list; the offline tests substitute a fake probe and therefore never load the
    /// Timber Pipes / Power Lines / Conveyor Belts assemblies (CONTRACT_SEE §1.4).
    /// </summary>
    public interface ICompatProbe
    {
        /// <summary>Probes one optional mod exactly once; never throws.</summary>
        /// <param name="modId">Stable mod id from <see cref="CompatProbeCatalog"/>.</param>
        /// <returns>The raw probe result; unknown ids report an absent assembly.</returns>
        CompatProbeResult Probe(string modId);
    }
}
