namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Footprint class of a boiler (requirements §4.1). The numeric value is stable because the
    /// art/config tables are indexed by it.
    /// </summary>
    public enum BoilerSize
    {
        /// <summary>2x2 footprint.</summary>
        Small = 0,

        /// <summary>3x3 footprint.</summary>
        Medium = 1,

        /// <summary>4x4 footprint.</summary>
        Large = 2
    }
}
