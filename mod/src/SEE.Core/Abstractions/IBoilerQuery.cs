using System;

namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Read-only query service the UI assembly consumes without referencing gameplay
    /// (REQ-ARCH-03). Implemented by the gameplay registry.
    /// </summary>
    public interface IBoilerQuery
    {
        /// <summary>Tries to read the status view of the boiler on the given entity.</summary>
        /// <param name="entityId">Entity id of the boiler (<c>EntityComponent.EntityId</c>).</param>
        /// <param name="view">Status view when the entity carries a live boiler controller.</param>
        /// <returns>True when the boiler was found and its view could be built.</returns>
        bool TryGetView(Guid entityId, out IBoilerStatusView? view);
    }
}
