using System;
using System.Collections.Generic;

namespace SEE.Core.Config
{
    /// <summary>
    /// Resolved <c>art.*</c> configuration (requirements §9.6/§16.5): triangle and texture budgets
    /// used by the art tooling and the LOD validator, never by the simulation.
    /// </summary>
    public sealed class ArtConfig
    {
        internal ArtConfig(
            IReadOnlyList<int> trisLod0,
            float lod1Ratio,
            float lod2Ratio,
            int textureSizeSmall,
            int textureSizeLarge)
        {
            TrisLod0 = trisLod0 ?? throw new ArgumentNullException(nameof(trisLod0));
            Lod1Ratio = lod1Ratio;
            Lod2Ratio = lod2Ratio;
            TextureSizeSmall = textureSizeSmall;
            TextureSizeLarge = textureSizeLarge;
        }

        /// <summary>LOD0 triangle budget indexed by <see cref="Abstractions.BoilerSize"/>.</summary>
        public IReadOnlyList<int> TrisLod0 { get; }

        /// <summary>LOD1 triangle ratio (0.50, range 0.30..0.60).</summary>
        public float Lod1Ratio { get; }

        /// <summary>LOD2 triangle ratio (0.15, range 0.05..0.20).</summary>
        public float Lod2Ratio { get; }

        /// <summary>Small-size texture edge in pixels (512, range 256..1024).</summary>
        public int TextureSizeSmall { get; }

        /// <summary>Medium/large texture edge in pixels (1024, range 512..2048).</summary>
        public int TextureSizeLarge { get; }
    }
}
