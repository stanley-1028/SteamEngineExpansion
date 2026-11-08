using System;
using SEE.Core.Abstractions;
using SEE.Core.Config;

namespace SEE.Integration.Adapters
{
    /// <summary>
    /// Immutable snapshot of the <c>compat.*</c> options the adapters need (requirements §7.6/§10.3).
    /// Pure data so the offline tests can construct every combination without a game container.
    /// </summary>
    public sealed class CompatAdapterOptions
    {
        /// <summary>Creates the options.</summary>
        /// <param name="enablePowerLineInjection"><c>compat.enablePowerLineInjection</c>.</param>
        /// <param name="waterSupplyMode"><c>compat.waterSupplyMode</c> global default intent.</param>
        /// <param name="beltFuelMode"><c>compat.beltFuelMode</c>.</param>
        /// <param name="logThrottleSeconds"><c>compat.logThrottleSeconds</c>.</param>
        public CompatAdapterOptions(
            bool enablePowerLineInjection,
            WaterSupplyMode waterSupplyMode,
            BeltFuelMode beltFuelMode,
            double logThrottleSeconds)
        {
            EnablePowerLineInjection = enablePowerLineInjection;
            WaterSupplyMode = waterSupplyMode;
            BeltFuelMode = beltFuelMode;
            LogThrottleSeconds = logThrottleSeconds;
        }

        /// <summary>Whether Power Lines runtime injection may be attempted.</summary>
        public bool EnablePowerLineInjection { get; }

        /// <summary>Global default water-supply intent (per-boiler intent wins).</summary>
        public WaterSupplyMode WaterSupplyMode { get; }

        /// <summary>Belt fuel preference from the config.</summary>
        public BeltFuelMode BeltFuelMode { get; }

        /// <summary>Session log throttle window in seconds.</summary>
        public double LogThrottleSeconds { get; }

        /// <summary>Compiled defaults matching CONTRACT_SEE §7.6.</summary>
        public static CompatAdapterOptions Default { get; } = new CompatAdapterOptions(
            enablePowerLineInjection: true,
            waterSupplyMode: WaterSupplyMode.Auto,
            beltFuelMode: BeltFuelMode.Auto,
            logThrottleSeconds: SEE.Core.Constants.SEEConst.LogThrottleSeconds);

        /// <summary>Projects the resolved config onto the adapter snapshot.</summary>
        /// <param name="config">Resolved configuration; null yields <see cref="Default"/>.</param>
        /// <returns>A new snapshot.</returns>
        public static CompatAdapterOptions FromConfig(SeeConfig? config)
        {
            if (config == null)
            {
                return Default;
            }

            try
            {
                CompatConfig compat = config.Compat;
                return new CompatAdapterOptions(
                    compat.EnablePowerLineInjection,
                    compat.WaterSupplyMode,
                    compat.BeltFuelMode,
                    compat.LogThrottleSeconds);
            }
            catch (Exception)
            {
                return Default;
            }
        }
    }

    /// <summary>
    /// Late-bound access to the current <see cref="CompatAdapterOptions"/>. The config is loaded
    /// before the game container exists, but <c>SEERuntime.Load</c> copies it later; adapters read
    /// through this seam so they always see the session value instead of a construction-time copy.
    /// </summary>
    public interface ICompatOptionsSource
    {
        /// <summary>Current option snapshot; never null.</summary>
        CompatAdapterOptions Current { get; }
    }
}
