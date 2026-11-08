using System;
using System.Collections.Generic;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Json;
using SEE.Core.Logging;

namespace SEE.Core.Save
{
    /// <summary>
    /// Save schema v1 validation, migration chain and the JSON form of one boiler
    /// (requirements §14.1/§14.4/§14.5, REQ-SAVE-03/05).
    ///
    /// Loading is defensive on purpose: a single damaged field is repaired in place and reported
    /// once, it never fails the whole save. Unknown JSON properties are ignored (forward
    /// compatibility), a missing property keeps its documented default, and a schema version newer
    /// than <see cref="CurrentSchemaVersion"/> only logs a warning (read-only degradation is the
    /// caller's job; this codec never discards data).
    /// </summary>
    public static class SeeSaveCodec
    {
        /// <summary>Schema version this codec writes; v1 ships no migration step.</summary>
        public static int CurrentSchemaVersion => SEEVersion.SaveSchemaVersion;

        /// <summary>Migration chain; empty for schema v1 (requirements §14.4).</summary>
        public static IReadOnlyList<ISaveMigration> Migrations { get; } = Array.Empty<ISaveMigration>();

        /// <summary>Parses the JSON form of one boiler; null when the text is not a JSON object.</summary>
        /// <param name="json">JSON text produced by <see cref="Serialize"/> or an older version.</param>
        /// <param name="log">Diagnostics sink; null is tolerated.</param>
        /// <returns>The raw parsed state (not yet sanitized), or null when unparsable.</returns>
        public static BoilerSaveState? Parse(string json, ISeeLog? log)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Warn(log, SeeLogKeys.SaveCorrupt, "empty boiler save data");
                return null;
            }

            try
            {
                if (MiniJson.Parse(json) is not IDictionary<string, object?> root)
                {
                    Warn(log, SeeLogKeys.SaveCorrupt, "boiler save data is not a JSON object");
                    return null;
                }

                int schemaVersion = MiniJson.GetInt(root, "schemaVersion", CurrentSchemaVersion);
                if (schemaVersion > CurrentSchemaVersion)
                {
                    Warn(log, SeeLogKeys.SaveError,
                        "save schema " + schemaVersion + " is newer than supported " + CurrentSchemaVersion);
                }

                var state = new BoilerSaveState
                {
                    EntityId = MiniJson.GetString(root, "entityId", string.Empty),
                    Tier = ParseEnum(MiniJson.GetString(root, "tier", string.Empty), BoilerTier.T1),
                    Size = ParseEnum(MiniJson.GetString(root, "size", string.Empty), BoilerSize.Small),
                    Faction = ParseEnum(MiniJson.GetString(root, "faction", string.Empty), BoilerFaction.Folktails),
                    Condition = (float)MiniJson.GetNumber(root, "condition", SEEConst.MaxCondition),
                    AutoMaintenance = MiniJson.GetBool(root, "autoMaintenance", true),
                    MaintenancePhase = ParseEnum(MiniJson.GetString(root, "maintenanceState", string.Empty), MaintenancePhase.None),
                    MaintenanceRemainingHours = (float)MiniJson.GetNumber(root, "maintenanceRemainingHours", 0.0),
                    LastMaintenanceDay = (float)MiniJson.GetNumber(root, "lastMaintenanceDay", 0.0),
                    WaterSupplyMode = ParseEnum(MiniJson.GetString(root, "waterSupplyMode", string.Empty), WaterSupplyMode.Auto),
                    PowerLineAttached = MiniJson.GetBool(root, "powerLineAttached", false)
                };
                return state;
            }
            catch (Exception exception)
            {
                Warn(log, SeeLogKeys.SaveCorrupt, "unparsable boiler save data (" + exception.GetType().Name + ")");
                return null;
            }
        }

        /// <summary>
        /// Validates and clamps one boiler's state (requirements §14.5, D-7). A null input becomes
        /// the documented new-boiler defaults; every invalid value degrades to a documented value
        /// and is reported once, never throwing (REQ-SAFE-01).
        /// </summary>
        /// <param name="raw">Parsed state; null means "no data stored for this boiler".</param>
        /// <param name="currentDay">Current game day, used to clamp the last-maintenance field.</param>
        /// <param name="log">Diagnostics sink; null is tolerated.</param>
        /// <returns>A valid state at the current schema version.</returns>
        public static BoilerSaveState Sanitize(BoilerSaveState? raw, float currentDay, ISeeLog? log)
        {
            if (raw == null)
            {
                Warn(log, SeeLogKeys.SaveCorrupt, "no boiler data; using safe defaults");
                return BoilerSaveState.NewGame();
            }

            var state = new BoilerSaveState
            {
                EntityId = SanitizeEntityId(raw.EntityId, log),
                Tier = IsDefined(raw.Tier) ? raw.Tier : BoilerTier.T1,
                Size = IsDefined(raw.Size) ? raw.Size : BoilerSize.Small,
                Faction = IsDefined(raw.Faction) ? raw.Faction : BoilerFaction.Folktails,
                AutoMaintenance = raw.AutoMaintenance,
                WaterSupplyMode = IsDefined(raw.WaterSupplyMode) ? raw.WaterSupplyMode : WaterSupplyMode.Auto,
                PowerLineAttached = raw.PowerLineAttached
            };

            bool conditionValid = !float.IsNaN(raw.Condition) && !float.IsInfinity(raw.Condition);
            state.Condition = SanitizeCondition(raw.Condition, log);
            if (!conditionValid)
            {
                // CONTRACT_SEE §6.2: a corrupt condition resets the boiler and turns automatic
                // maintenance off so the damaged record cannot silently start a maintenance cycle.
                state.AutoMaintenance = false;
            }
            state.MaintenancePhase = IsDefined(raw.MaintenancePhase) ? raw.MaintenancePhase : MaintenancePhase.None;
            if (state.MaintenancePhase == MaintenancePhase.Maintaining)
            {
                state.MaintenanceRemainingHours = Clamp(
                    raw.MaintenanceRemainingHours,
                    0f,
                    SEEConst.MaxMaintenanceRemainingHours,
                    SEEConst.MaxMaintenanceRemainingHours);
            }

            float day = SanitizeDay(currentDay);
            state.LastMaintenanceDay = Clamp(raw.LastMaintenanceDay, 0f, day, 0f);
            return state;
        }

        /// <summary>Serializes one boiler to compact JSON at the current schema version.</summary>
        /// <param name="state">State to write; must not be null.</param>
        /// <returns>JSON text, including the schema version and the entity id string.</returns>
        public static string Serialize(BoilerSaveState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var root = new Dictionary<string, object?>
            {
                ["schemaVersion"] = CurrentSchemaVersion,
                ["entityId"] = state.EntityId,
                ["tier"] = state.Tier.ToString(),
                ["size"] = state.Size.ToString(),
                ["faction"] = state.Faction.ToString(),
                ["condition"] = state.Condition,
                ["autoMaintenance"] = state.AutoMaintenance,
                ["maintenanceState"] = state.MaintenancePhase.ToString(),
                ["maintenanceRemainingHours"] = state.MaintenanceRemainingHours,
                ["lastMaintenanceDay"] = state.LastMaintenanceDay,
                ["waterSupplyMode"] = state.WaterSupplyMode.ToString().ToLowerInvariant(),
                ["powerLineAttached"] = state.PowerLineAttached
            };
            return MiniJson.Write(root);
        }

        /// <summary>
        /// Applies the migration chain from <paramref name="fromVersion"/> to the current schema.
        /// v1 is already current, so it returns the text unchanged; a newer version returns null
        /// (the caller degrades to a read-only load). A failed step returns null and never throws.
        /// </summary>
        /// <param name="fromVersion">Schema version of <paramref name="json"/>.</param>
        /// <param name="json">Serialized document to migrate.</param>
        /// <returns>Migrated text, or null when the version cannot be migrated.</returns>
        public static string? Migrate(int fromVersion, string json)
        {
            if (json == null)
            {
                return null;
            }

            if (fromVersion > CurrentSchemaVersion)
            {
                return null;
            }

            string current = json;
            int version = fromVersion;
            for (int i = 0; i < Migrations.Count; i++)
            {
                ISaveMigration step = Migrations[i];
                if (step.FromVersion != version)
                {
                    continue;
                }

                try
                {
                    string? migrated = step.Migrate(current);
                    if (migrated == null)
                    {
                        return null;
                    }

                    current = migrated;
                    version = step.ToVersion;
                }
                catch (Exception)
                {
                    // A migration must never throw into the game loop (requirements §14.4).
                    return null;
                }
            }

            return version == CurrentSchemaVersion ? current : null;
        }

        private static string SanitizeEntityId(string? entityId, ISeeLog? log)
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                return string.Empty;
            }

            if (Guid.TryParse(entityId, out Guid parsed))
            {
                return parsed.ToString("D");
            }

            Warn(log, SeeLogKeys.SaveCorrupt, "entityId is not a GUID; the record is detached");
            return string.Empty;
        }

        private static float SanitizeCondition(float condition, ISeeLog? log)
        {
            if (float.IsNaN(condition) || float.IsInfinity(condition))
            {
                Warn(log, SeeLogKeys.SaveCorrupt, "condition was not finite; reset to 100");
                return SEEConst.MaxCondition;
            }

            return Clamp(condition, 0f, SEEConst.MaxCondition, SEEConst.MaxCondition);
        }

        private static float SanitizeDay(float day)
        {
            if (float.IsNaN(day) || float.IsInfinity(day) || day < 0f)
            {
                return 0f;
            }

            return day;
        }

        private static float Clamp(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                value = fallback;
            }

            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static T ParseEnum<T>(string? text, T fallback) where T : struct
        {
            if (!string.IsNullOrWhiteSpace(text)
                && Enum.TryParse(text, true, out T parsed)
                && IsDefined(parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static bool IsDefined<T>(T value) where T : struct
        {
            return Enum.IsDefined(typeof(T), value);
        }

        private static bool IsDefined(MaintenancePhase phase)
        {
            return phase == MaintenancePhase.None
                || phase == MaintenancePhase.Maintaining
                || phase == MaintenancePhase.MissingMaterials
                || phase == MaintenancePhase.NoAccess;
        }

        private static bool IsDefined(BoilerTier tier) => tier == BoilerTier.T1 || tier == BoilerTier.T2 || tier == BoilerTier.T3 || tier == BoilerTier.T4;

        private static bool IsDefined(BoilerSize size) => size == BoilerSize.Small || size == BoilerSize.Medium || size == BoilerSize.Large;

        private static bool IsDefined(BoilerFaction faction) => faction == BoilerFaction.Folktails || faction == BoilerFaction.IronTeeth;

        private static bool IsDefined(WaterSupplyMode mode) => mode == WaterSupplyMode.Auto || mode == WaterSupplyMode.Tank || mode == WaterSupplyMode.Valve || mode == WaterSupplyMode.Hauler;

        private static void Warn(ISeeLog? log, string key, string message)
        {
            try
            {
                log?.Warn(key, message);
            }
            catch (Exception)
            {
                // Diagnostics only: a broken log sink must not break save loading.
            }
        }
    }
}