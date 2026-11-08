namespace SEE.Integration.Adapters
{
    /// <summary>
    /// Capability interface of the Conveyor Belts adapter (CONTRACT_SEE §2.4, requirements §7.6).
    /// Fuel delivery works without this adapter because the vanilla fuel inventory accepts
    /// <c>Log</c>/<c>Plank</c>; the adapter only enables the belt-supply UI hints (REQ-FUEL-16).
    /// </summary>
    public interface IFuelSupplyAdapter : IExternalAdapter
    {
        /// <summary>True when the panel may report belt fuel supply.</summary>
        bool FuelSupplyAvailable { get; }

        /// <summary>True when belts should be preferred over haulers for the fuel hint.</summary>
        bool BeltFuelPreferred { get; }

        /// <summary>True when the belt-no-power warning may be shown (REQ-FUEL-18).</summary>
        bool BeltPowerWarningAvailable { get; }
    }
}
