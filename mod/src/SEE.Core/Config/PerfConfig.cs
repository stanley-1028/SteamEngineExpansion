using System;
using System.Collections.Generic;

namespace SEE.Core.Config
{
    /// <summary>
    /// Resolved <c>perf.*</c> configuration (requirements §9.4/§20.4): tick period, UI/badge
    /// refresh caps and LOD distances. Gameplay UI code reads these, never literal frame numbers.
    /// </summary>
    public sealed class PerfConfig
    {
        internal PerfConfig(
            float tickSeconds,
            float uiRefreshHz,
            float badgeRefreshHz,
            int maxBadgesVisible,
            IReadOnlyList<float> lodDistances)
        {
            TickSeconds = tickSeconds;
            UiRefreshHz = uiRefreshHz;
            BadgeRefreshHz = badgeRefreshHz;
            MaxBadgesVisible = maxBadgesVisible;
            LodDistances = lodDistances ?? throw new ArgumentNullException(nameof(lodDistances));
        }

        /// <summary>Boiler settlement period in seconds (1.0, range 0.5..2.0).</summary>
        public float TickSeconds { get; }

        /// <summary>Entity panel refresh cap in Hz (4, range 1..10).</summary>
        public float UiRefreshHz { get; }

        /// <summary>World badge refresh cap in Hz (1, range 0.5..2).</summary>
        public float BadgeRefreshHz { get; }

        /// <summary>Maximum simultaneously visible badges (64, range 16..256).</summary>
        public int MaxBadgesVisible { get; }

        /// <summary>LOD0→1 and LOD1→2 switch distances in meters (18 / 40).</summary>
        public IReadOnlyList<float> LodDistances { get; }
    }
}
