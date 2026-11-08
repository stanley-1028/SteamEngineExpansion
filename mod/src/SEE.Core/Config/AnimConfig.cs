namespace SEE.Core.Config
{
    /// <summary>
    /// Resolved <c>anim.*</c> configuration (requirements §9.5/§16.9). Animation is a pure visual
    /// layer: these values never change output, fuel, water or maintenance numbers.
    /// </summary>
    public sealed class AnimConfig
    {
        internal AnimConfig(
            bool enable,
            float updateHz,
            float lod0MaxDistance,
            float lod1MaxDistance,
            float vfxEmissionScale)
        {
            Enable = enable;
            UpdateHz = updateHz;
            Lod0MaxDistance = lod0MaxDistance;
            Lod1MaxDistance = lod1MaxDistance;
            VfxEmissionScale = vfxEmissionScale;
        }

        /// <summary>Master switch; false freezes the model with no VFX or transform offset.</summary>
        public bool Enable { get; }

        /// <summary>LOD0 phase update rate in Hz (30, range 15..60).</summary>
        public float UpdateHz { get; }

        /// <summary>Full-animation distance in meters (18, range 10..30).</summary>
        public float Lod0MaxDistance { get; }

        /// <summary>Flywheel/crankshaft-only distance in meters (40, range 25..60).</summary>
        public float Lod1MaxDistance { get; }

        /// <summary>Global smoke/steam emission multiplier (1.0, range 0..2).</summary>
        public float VfxEmissionScale { get; }
    }
}
