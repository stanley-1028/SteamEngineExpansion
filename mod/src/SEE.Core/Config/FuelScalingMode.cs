namespace SEE.Core.Config
{
    /// <summary>
    /// Fuel consumption model of a boiler (requirements §13.4, <c>balance.fuelScaling</c>).
    /// </summary>
    public enum FuelScalingMode
    {
        /// <summary>Nominal fuel rate scaled by the actual load ratio (the shipped default).</summary>
        Proportional = 0,

        /// <summary>Vanilla behaviour: consume at the nominal rate whenever there is demand.</summary>
        Vanilla = 1
    }
}
