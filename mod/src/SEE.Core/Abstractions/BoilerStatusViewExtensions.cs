using System;

namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Derived read-only helpers over <see cref="IBoilerStatusView"/> (requirements §3.5/§15.3,
    /// REQ-CORE-11, REQ-UI-01). They stay extension methods so the frozen interface keeps its seven
    /// per-entity-state members while the panel still gets "remaining hours" without duplicating
    /// the division in the UI.
    /// </summary>
    public static class BoilerStatusViewExtensions
    {
        /// <summary>
        /// Fuel remaining in game hours at the rated full-load rate; 0 when the boiler has no
        /// rated fuel consumption. The value is a buffer estimate, not a simulation.
        /// </summary>
        /// <param name="view">Read-only boiler view; must not be null.</param>
        /// <returns>Hours of fuel at the rated rate; 0 for a zero rate.</returns>
        public static float FuelRemainingHours(this IBoilerStatusView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            return view.FuelPerHour > 0f ? view.FuelInStock / view.FuelPerHour : 0f;
        }

        /// <summary>
        /// Water remaining in game hours at the rated full-load rate; 0 when the boiler has no
        /// rated water consumption.
        /// </summary>
        /// <param name="view">Read-only boiler view; must not be null.</param>
        /// <returns>Hours of water at the rated rate; 0 for a zero rate.</returns>
        public static float WaterRemainingHours(this IBoilerStatusView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            return view.WaterPerHour > 0f ? view.WaterInStock / view.WaterPerHour : 0f;
        }
    }
}
