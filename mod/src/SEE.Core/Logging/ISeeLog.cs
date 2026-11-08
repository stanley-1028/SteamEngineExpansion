using System;

namespace SEE.Core.Logging
{
    /// <summary>
    /// Throttled logger abstraction so Core never references UnityEngine (REQ-OPS-03,
    /// REQ-ARCH-03). The four methods map to the four stable log categories
    /// <c>SEE/Info</c>, <c>SEE/Warn</c>, <c>SEE/Error</c> and <c>SEE/Compat</c>.
    /// </summary>
    public interface ISeeLog
    {
        /// <summary>Informational message.</summary>
        /// <param name="key">Stable throttle key, see <see cref="SeeLogKeys"/>.</param>
        /// <param name="message">Message text.</param>
        void Info(string key, string message);

        /// <summary>Warning that needs attention but is recoverable.</summary>
        /// <param name="key">Stable throttle key.</param>
        /// <param name="message">Message text.</param>
        void Warn(string key, string message);

        /// <summary>Error; the system degraded but kept running.</summary>
        /// <param name="key">Stable throttle key.</param>
        /// <param name="message">Message text.</param>
        void Error(string key, string message);

        /// <summary>Compatibility-detection message.</summary>
        /// <param name="key">Stable throttle key.</param>
        /// <param name="message">Message text.</param>
        void Compat(string key, string message);
    }
}