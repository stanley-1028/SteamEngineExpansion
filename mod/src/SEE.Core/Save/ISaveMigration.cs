namespace SEE.Core.Save
{
    /// <summary>
    /// One step of the save migration chain (requirements §14.4). v1 ships zero migrations; a
    /// future schema version adds one implementation per step. Migrations only move forward, are
    /// idempotent and must never throw: a failed step returns null and the caller degrades to a
    /// read-only load instead of discarding data.
    /// </summary>
    public interface ISaveMigration
    {
        /// <summary>Schema version this step reads.</summary>
        int FromVersion { get; }

        /// <summary>Schema version this step produces; always <see cref="FromVersion"/> + 1.</summary>
        int ToVersion { get; }

        /// <summary>Migrates one serialized document; returns null when the step cannot be applied.</summary>
        /// <param name="json">JSON text of the document at <see cref="FromVersion"/>.</param>
        /// <returns>JSON text at <see cref="ToVersion"/>, or null on failure.</returns>
        string? Migrate(string json);
    }
}
