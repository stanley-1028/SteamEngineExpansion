using System;
using SEE.Core.Config;
using SEE.Integration.Detection;

namespace SEE.Integration.Adapters
{
    /// <summary>
    /// Conveyor Belts v2 soft-dependency adapter (requirements §10.2/§7.6, REQ-COMPAT-03).
    ///
    /// Fuel delivery always works through the vanilla fuel inventory (haulers), and the belt mod
    /// attaches its own target to any building with inventories, so there is nothing to inject. The
    /// adapter only enables the panel hints: "fuel supplied by conveyor" and the belt-no-power
    /// warning. It never references the Conveyor Belts assembly and never assumes a belt network exists.
    /// </summary>
    public sealed class ConveyorBeltAdapter : ExternalAdapterBase, IFuelSupplyAdapter
    {
        private readonly ICompatOptionsSource _options;

        /// <summary>Creates the adapter; detection and late-bound options are injected.</summary>
        /// <param name="detector">Session compatibility detector.</param>
        /// <param name="options">Late-bound <c>compat.*</c> options.</param>
        public ConveyorBeltAdapter(ICompatDetector detector, ICompatOptionsSource options)
            : base(detector, CompatAnchors.ConveyorBeltAdapterId, CompatAnchors.ConveyorBelt)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>True when the panel may report belt fuel supply.</summary>
        public bool FuelSupplyAvailable => State == AdapterState.Enabled;

        /// <summary>True when belts should be preferred over haulers for the fuel hint.</summary>
        public bool BeltFuelPreferred => State == AdapterState.Enabled && _options.Current.BeltFuelMode == BeltFuelMode.Auto;

        /// <summary>True when the belt-no-power warning may be shown.</summary>
        public bool BeltPowerWarningAvailable => State == AdapterState.Enabled;

        /// <inheritdoc />
        protected override void OnAvailable()
        {
            Enable("conveyor-belts-detected");
        }
    }
}
