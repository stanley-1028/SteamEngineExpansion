namespace SEE.Integration.Save
{
    /// <summary>
    /// Outcome of one <see cref="SaveMigrator.Migrate"/> call (requirements §14.4).
    /// </summary>
    public readonly struct SaveMigrationResult
    {
        /// <summary>Creates a result.</summary>
        /// <param name="success">True when the document is at the current schema version.</param>
        /// <param name="json">Migrated (or original) JSON text; null when unreadable.</param>
        /// <param name="readOnly">True when the document must not be written back (newer schema).</param>
        /// <param name="detail">Short machine-readable reason for diagnostics.</param>
        public SaveMigrationResult(bool success, string? json, bool readOnly, string detail)
        {
            Success = success;
            Json = json;
            ReadOnly = readOnly;
            Detail = detail;
        }

        /// <summary>True when the document is at the current schema version.</summary>
        public bool Success { get; }

        /// <summary>Migrated (or original) JSON text; null when unreadable.</summary>
        public string? Json { get; }

        /// <summary>True when the document must not be written back (newer schema: read-only load).</summary>
        public bool ReadOnly { get; }

        /// <summary>Short machine-readable reason for diagnostics.</summary>
        public string Detail { get; }
    }
}
