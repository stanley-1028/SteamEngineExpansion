using System;

namespace SEE.Integration.Adapters
{
    /// <summary>
    /// Capability interface of the Power Lines adapter (CONTRACT_SEE §2.4, requirements §10.4).
    /// The injection itself is done by <see cref="PowerLineInjectionProvider"/>; this adapter only
    /// reports whether it is available. A failed injection leaves the building on the vanilla
    /// mechanical node (MUST) and never touches the save (REQ-COMPAT-11/REQ-POWER-16).
    /// </summary>
    public interface IPowerLineAdapter : IExternalAdapter
    {
        /// <summary>True when runtime <c>PowerLineSpec</c> injection is active for SEE blueprints.</summary>
        bool InjectionAvailable { get; }

        /// <summary>
        /// Reports whether the building is attached to the Power Lines network. With blueprint-level
        /// injection the connection points exist for every boiler while injection is active, so this
        /// is the per-session answer rather than a per-entity mutation (REQ-COMPAT-11).
        /// </summary>
        /// <param name="entityId">Boiler entity id (diagnostics).</param>
        /// <returns>True when injection is active.</returns>
        bool TryAttach(Guid entityId);

        /// <summary>True when the building currently exposes injected connection points.</summary>
        /// <param name="entityId">Boiler entity id (diagnostics).</param>
        /// <returns>True when injection is active.</returns>
        bool IsAttached(Guid entityId);
    }
}
