using System;
using SEE.Integration.Detection;

namespace SEE.Integration.Adapters
{
    /// <summary>
    /// Power Lines soft-dependency adapter (requirements §10.2/§10.4, REQ-COMPAT-05/11).
    ///
    /// The MUST path always works through the vanilla <c>MechanicalNodeSpec</c> and transputs. When
    /// Power Lines is detected and <c>compat.enablePowerLineInjection</c> is on, the adapter reports
    /// active and <see cref="PowerLineInjectionProvider"/> merges a <c>PowerLineSpec</c> JSON
    /// fragment into the SEE boiler blueprints at deserialization time (no Harmony, no blueprint file
    /// edit). A disabled or failed integration keeps the building on the MUST path and never touches
    /// the save (REQ-POWER-16).
    /// </summary>
    public sealed class PowerLinesAdapter : ExternalAdapterBase, IPowerLineAdapter
    {
        private readonly ICompatOptionsSource _options;

        /// <summary>Creates the adapter; detection and late-bound options are injected.</summary>
        /// <param name="detector">Session compatibility detector.</param>
        /// <param name="options">Late-bound <c>compat.*</c> options.</param>
        public PowerLinesAdapter(ICompatDetector detector, ICompatOptionsSource options)
            : base(detector, CompatAnchors.PowerLinesAdapterId, CompatAnchors.PowerLines)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>True when runtime <c>PowerLineSpec</c> injection is active for SEE blueprints.</summary>
        public bool InjectionAvailable => State == AdapterState.Enabled;

        /// <summary>True when the injected line is expected to participate in the mechanical graph.</summary>
        public bool LineConnectionAvailable => InjectionAvailable;

        /// <inheritdoc />
        public bool TryAttach(Guid entityId)
        {
            return InjectionAvailable;
        }

        /// <inheritdoc />
        public bool IsAttached(Guid entityId)
        {
            return InjectionAvailable;
        }

        /// <inheritdoc />
        protected override void OnAvailable()
        {
            if (_options.Current.EnablePowerLineInjection)
            {
                Enable("injection-enabled");
            }
            else
            {
                SetDetected("config-disabled:enablePowerLineInjection");
            }
        }
    }
}
