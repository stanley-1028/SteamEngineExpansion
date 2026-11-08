namespace SEE.Core.Config
{
    /// <summary>
    /// Whether fuel supply relies on conveyor belts when they are available
    /// (requirements §9.3, <c>compat.beltFuelMode</c>).
    /// </summary>
    public enum BeltFuelMode
    {
        /// <summary>Use conveyor belts when the mod is detected, otherwise haulers.</summary>
        Auto = 0,

        /// <summary>Always supply fuel with haulers, even when conveyor belts exist.</summary>
        Haulers = 1
    }
}
