using System;

namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Per-boiler command surface addressed by entity id (requirements §11.1/REQ-UI-07, D-8).
    /// SEE.UI calls it through <see cref="IBoilerLogic"/>; the split exists so a future automation or
    /// scenario layer can depend on the command contract without the panel-facing alias.
    ///
    /// Every method addresses exactly one boiler by its <see cref="Guid"/> entity id; a command never
    /// affects another boiler and returns false when the id is unknown or the boiler is not active.
    /// </summary>
    public interface IBoilerCommands
    {
        /// <summary>Pauses or resumes one boiler (REQ-UI-07).</summary>
        /// <param name="entityId">Entity id of the boiler.</param>
        /// <param name="paused">True to pause, false to resume.</param>
        /// <returns>True when the boiler was found and updated.</returns>
        bool SetPaused(Guid entityId, bool paused);

        /// <summary>Requests an immediate manual maintenance (REQ-MAINT-05).</summary>
        /// <param name="entityId">Entity id of the boiler.</param>
        /// <returns>True when the request was recorded; materials/path are re-checked by the tick.</returns>
        bool RequestMaintenanceNow(Guid entityId);

        /// <summary>Enables or disables automatic maintenance for one boiler (REQ-MAINT-05).</summary>
        /// <param name="entityId">Entity id of the boiler.</param>
        /// <param name="enabled">New value of the per-boiler flag.</param>
        /// <returns>True when the boiler was found and updated.</returns>
        bool SetAutoMaintenance(Guid entityId, bool enabled);

        /// <summary>Stores the player's water-supply intent for one boiler (REQ-FUEL-11).</summary>
        /// <param name="entityId">Entity id of the boiler.</param>
        /// <param name="mode">Requested supply mode.</param>
        /// <returns>True when the boiler was found and updated.</returns>
        bool SetWaterSupplyMode(Guid entityId, WaterSupplyMode mode);
    }
}
