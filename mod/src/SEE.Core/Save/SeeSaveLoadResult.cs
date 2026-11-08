using System;
using SEE.Core.Constants;
using SEE.Core.Logging;

namespace SEE.Core.Save
{
    /// <summary>
    /// Outcome of <see cref="SeeSaveNodeCodec.Load"/> (requirements §14.2/§14.5): the repaired node
    /// plus the degradation flags the caller needs. <see cref="ReadOnly"/> means a newer schema was
    /// found and the node must not be written back; <see cref="Damaged"/> means at least one repair
    /// happened while loading (the data was kept, not rejected).
    /// </summary>
    public readonly struct SeeSaveLoadResult
    {
        /// <summary>Creates one load result.</summary>
        /// <param name="node">Parsed and repaired node; never null.</param>
        /// <param name="readOnly">True when the save schema is newer than supported.</param>
        /// <param name="damaged">True when at least one repair was applied.</param>
        /// <param name="repairs">Number of repairs applied.</param>
        internal SeeSaveLoadResult(SeeSaveNode node, bool readOnly, bool damaged, int repairs)
        {
            Node = node;
            ReadOnly = readOnly;
            Damaged = damaged;
            Repairs = repairs;
        }

        /// <summary>Parsed and repaired node; never null.</summary>
        public SeeSaveNode Node { get; }

        /// <summary>True when the save schema is newer than supported (read-only degradation).</summary>
        public bool ReadOnly { get; }

        /// <summary>True when at least one repair was applied to keep the node loadable.</summary>
        public bool Damaged { get; }

        /// <summary>Number of repairs applied while loading.</summary>
        public int Repairs { get; }

        /// <summary>
        /// Logs the degradation once with the appropriate <c>SEE/Warn</c> key (REQ-SAFE-02).
        /// Never throws when the log sink itself is broken.
        /// </summary>
        /// <param name="log">Diagnostics sink; null is tolerated.</param>
        public void Report(ISeeLog? log)
        {
            try
            {
                if (ReadOnly)
                {
                    log?.Warn(SeeLogKeys.SaveReadOnly, "SEE save schema is newer than supported; read-only load");
                }

                if (Damaged)
                {
                    log?.Warn(SeeLogKeys.SaveCorrupt, "SEE save node repaired (" + Repairs + " repairs)");
                }
            }
            catch (Exception)
            {
                // Diagnostics only: a broken log sink must not break loading (REQ-SAFE-02).
            }
        }
    }
}
