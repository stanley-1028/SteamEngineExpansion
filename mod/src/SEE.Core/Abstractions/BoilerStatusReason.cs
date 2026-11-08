namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Machine-readable cause of the current <see cref="BoilerStatus"/> (requirements §3.3/§3.5,
    /// REQ-CORE-11). It exists so the UI can attribute "why is output 0?" without parsing strings:
    /// every blocked state maps to exactly one reason, and the four producing states share the
    /// <see cref="ConditionCritical"/>/<see cref="ConditionWorn"/>/<see cref="MaintenanceDue"/>/
    /// <see cref="Loaded"/> causes.
    /// </summary>
    public enum BoilerStatusReason
    {
        /// <summary>The player paused this boiler.</summary>
        PlayerPaused = 0,

        /// <summary>Maintenance downtime is in progress.</summary>
        MaintenanceRunning = 1,

        /// <summary>The fuel inventory is empty.</summary>
        FuelEmpty = 2,

        /// <summary>The water tank is empty.</summary>
        WaterEmpty = 3,

        /// <summary>Condition reached 0.</summary>
        ConditionZero = 4,

        /// <summary>The network demand is zero, so the boiler is idle.</summary>
        NoNetworkDemand = 5,

        /// <summary>Condition is in the critical band while loaded.</summary>
        ConditionCritical = 6,

        /// <summary>Condition is in the worn band while loaded.</summary>
        ConditionWorn = 7,

        /// <summary>Condition is in the maintenance-due band while loaded.</summary>
        MaintenanceDue = 8,

        /// <summary>The boiler runs normally under load.</summary>
        Loaded = 9
    }
}
