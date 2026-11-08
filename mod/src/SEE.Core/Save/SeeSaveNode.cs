using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Constants;

namespace SEE.Core.Save
{
    /// <summary>
    /// One index row of the world save node (CONTRACT_SEE §6.1/§6.2(a), D-8/AD-11): the entity id
    /// as a GUID string plus tier, size and faction. The per-boiler mutable state lives in the
    /// per-entity component (see <see cref="BoilerSaveState"/>); together they form the eleven
    /// requirement-§14.1 fields.
    ///
    /// Unknown JSON properties are preserved in <see cref="UnknownFields"/> so a newer or older
    /// field set survives the round trip (REQ-SAVE-02).
    /// </summary>
    public sealed class SeeSaveBuildingIndex
    {
        /// <summary>Entity id in <see cref="Guid.ToString(string)"/> "D" format; empty when detached.</summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>Technology tier of the blueprint (T1..T4).</summary>
        public BoilerTier Tier { get; set; } = BoilerTier.T1;

        /// <summary>Footprint class of the blueprint.</summary>
        public BoilerSize Size { get; set; } = BoilerSize.Small;

        /// <summary>Faction of the blueprint (diagnostics only; it never changes loading).</summary>
        public BoilerFaction Faction { get; set; } = BoilerFaction.Folktails;

        /// <summary>Unknown JSON properties of this entry, preserved verbatim on write-back.</summary>
        public Dictionary<string, object?> UnknownFields { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Diagnostic snapshot of the compatibility layer stored in the save node (CONTRACT_SEE §6.3).
    /// The three arrays are snapshots, never promises: the next detection pass is authoritative.
    /// Only module short names and adapter ids may be stored (REQ-COMPAT-13).
    /// </summary>
    public sealed class SeeCompatSnapshot
    {
        /// <summary>Detected external module short names (for example "TimberPipes").</summary>
        public List<string> Detected { get; } = new List<string>();

        /// <summary>Adapter ids that were actually enabled (for example "SEE.Adapter.TimberPipes").</summary>
        public List<string> Enabled { get; } = new List<string>();

        /// <summary>Failure reason keys of the integration attempts (keys only, never stacks).</summary>
        public List<string> Failed { get; } = new List<string>();
    }

    /// <summary>
    /// Typed view of the world save node <c>SEE</c> (CONTRACT_SEE §6.1; D-6 hybrid form): schema
    /// version, mod version, checksum, the four-field building index and the compat snapshot.
    /// Unknown top-level JSON properties are preserved in <see cref="UnknownFields"/>.
    /// </summary>
    public sealed class SeeSaveNode
    {
        /// <summary>Schema version this node was read or written with (shared with the entities).</summary>
        public int SchemaVersion { get; set; } = SEEVersion.SaveSchemaVersion;

        /// <summary>Mod version written for diagnostics (SemVer).</summary>
        public string ModVersion { get; set; } = SEEVersion.ModVersion;

        /// <summary>Checksum of the node without this field, <c>sha256:&lt;hex&gt;</c>.</summary>
        public string Checksum { get; set; } = string.Empty;

        /// <summary>Building index rows (entityId/tier/size/faction), one per boiler.</summary>
        public List<SeeSaveBuildingIndex> Buildings { get; } = new List<SeeSaveBuildingIndex>();

        /// <summary>Diagnostic compatibility snapshot (§6.3).</summary>
        public SeeCompatSnapshot Compat { get; set; } = new SeeCompatSnapshot();

        /// <summary>Unknown top-level JSON properties, preserved verbatim on write-back.</summary>
        public Dictionary<string, object?> UnknownFields { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

        /// <summary>Creates the empty node of a new world or a world saved before SEE existed.</summary>
        /// <returns>A node with the current schema/mod version, no buildings and no compat data.</returns>
        public static SeeSaveNode NewGame()
        {
            return new SeeSaveNode();
        }
    }
}
