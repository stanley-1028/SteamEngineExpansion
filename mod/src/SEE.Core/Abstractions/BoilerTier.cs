namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Technology tier of a boiler (requirements §3/§4.1). The numeric value is stable: blueprint
    /// data and the config array layout both use it as an index.
    /// </summary>
    public enum BoilerTier
    {
        /// <summary>Mk.I, the early-game wood tier.</summary>
        T1 = 0,

        /// <summary>Mk.II, the mainstay tier.</summary>
        T2 = 1,

        /// <summary>Mk.III, the cast-iron tier.</summary>
        T3 = 2,

        /// <summary>Mk.IV, the end-game precision tier.</summary>
        T4 = 3
    }
}
