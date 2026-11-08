namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Player intent for how this boiler receives water (requirements §7.7/§10.3). The stored
    /// value is the intent; the effective mode is resolved at runtime from the detection result
    /// and is never written back, so a save never flip-flops between modes.
    /// </summary>
    public enum WaterSupplyMode
    {
        /// <summary>Resolve automatically: tank when Timber Pipes works, otherwise valve/hauler.</summary>
        Auto = 0,

        /// <summary>Water supplied by the Timber Pipes network directly into the tank.</summary>
        Tank = 1,

        /// <summary>Water supplied by a player-placed one-way valve pointing at the boiler.</summary>
        Valve = 2,

        /// <summary>Water supplied by haulers from the district stock (always available fallback).</summary>
        Hauler = 3
    }
}
