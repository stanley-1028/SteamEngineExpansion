using System;
using SEE.Core.Save;

namespace SEE.Integration.Save
{
    /// <summary>
    /// Forward-only save migration bridge (requirements §14.4, REQ-SAVE-05). The chain itself lives
    /// in <see cref="SeeSaveNodeCodec"/> (v1 ships zero migrations, so every v1 document is already
    /// current). A newer schema is reported as a read-only load instead of being discarded; a failed
    /// step returns null and never throws into the persistence pipeline.
    /// </summary>
    public static class SaveMigrator
    {
        /// <summary>Current save schema version; the chain never moves past it.</summary>
        public static int CurrentSchemaVersion => SeeSaveNodeCodec.CurrentSchemaVersion;

        /// <summary>Migrates one serialized SEE node to the current schema version.</summary>
        /// <param name="fromVersion">Schema version of <paramref name="json"/>.</param>
        /// <param name="json">Serialized document text; null is tolerated.</param>
        /// <returns>The migration result; never throws.</returns>
        public static SaveMigrationResult Migrate(int fromVersion, string? json)
        {
            if (json == null)
            {
                return new SaveMigrationResult(false, null, false, "null-document");
            }

            if (fromVersion < 0)
            {
                return new SaveMigrationResult(false, json, false, "invalid-schema-version");
            }

            if (fromVersion > CurrentSchemaVersion)
            {
                // Known fields are kept, nothing is written back; the player is told to update.
                return new SaveMigrationResult(false, json, true, "newer-schema:read-only");
            }

            try
            {
                string? migrated = SeeSaveNodeCodec.Migrate(fromVersion, json);
                return migrated != null
                    ? new SaveMigrationResult(true, migrated, false, "ok")
                    : new SaveMigrationResult(false, json, true, "migration-failed:read-only");
            }
            catch (Exception exception)
            {
                // A migration must never throw into the game loop (requirements §14.4).
                return new SaveMigrationResult(false, json, true, "migration-exception:" + exception.GetType().Name);
            }
        }
    }
}
