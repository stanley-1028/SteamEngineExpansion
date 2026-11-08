namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Highest-priority machine-readable state of one boiler (requirements §3.3/§15.3, Lead ruling
    /// AD-12, 2026-11-06).
    ///
    /// The declaration order IS the authoritative display priority (low value wins):
    /// <c>Paused &gt; Maintaining &gt; NoFuel &gt; NoWater &gt; NeedsMaintenance &gt; Critical &gt;
    /// Worn &gt; MaintenanceDue &gt; Idle &gt; Running</c>.
    ///
    /// Two applicability rules resolve the overlap between "no demand" and the wear bands
    /// (requirements §9.7): the condition bands <see cref="Critical"/>/<see cref="Worn"/>/
    /// <see cref="MaintenanceDue"/>/<see cref="Running"/> only apply while the boiler carries load
    /// (λ &gt; 0), and <see cref="Idle"/> only applies when the network demand is zero (λ &lt;= 0).
    ///
    /// <see cref="Maintaining"/> is the "level 0 override": it overrides NoFuel/NoWater/
    /// NeedsMaintenance but not Paused; it is not one of the nine main states and is stored
    /// separately as a <see cref="MaintenancePhase"/>.
    /// </summary>
    public enum BoilerStatus
    {
        /// <summary>Player paused this boiler; output and consumption stop, wear freezes.</summary>
        Paused = 0,

        /// <summary>Maintenance downtime in progress (level 0 override, wins over NoFuel/NoWater).</summary>
        Maintaining = 1,

        /// <summary>Fuel inventory is empty; output 0.</summary>
        NoFuel = 2,

        /// <summary>Water tank is empty; output 0.</summary>
        NoWater = 3,

        /// <summary>Condition reached 0; output 0 until a maintenance completes.</summary>
        NeedsMaintenance = 4,

        /// <summary>Condition 1..20 while loaded; output x0.40.</summary>
        Critical = 5,

        /// <summary>Condition 21..40 while loaded; output x0.85.</summary>
        Worn = 6,

        /// <summary>Condition 41..60 while loaded; output x0.93.</summary>
        MaintenanceDue = 7,

        /// <summary>Network demand is 0 (λ &lt;= 0); output 0, no fuel/water consumed, no wear.</summary>
        Idle = 8,

        /// <summary>Condition 61..100 and loaded; output scaled by load.</summary>
        Running = 9
    }
}
