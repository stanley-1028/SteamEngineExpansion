using System;
using SEE.Core.Abstractions;
using SEE.Core.Logging;
using SEE.Core.Save;

namespace SEE.Tests
{
    /// <summary>
    /// Unit tests for <see cref="SeeSaveCodec"/> and <see cref="BoilerSaveState"/> (requirements
    /// §14.1/§14.4/§14.5, D-7/D-8): v1 round-trip, tolerant restore of NaN/out-of-range/unknown
    /// enum/missing fields, and the schema-version handling (newer schema degrades read-only).
    /// </summary>
    internal static class SaveCodecTests
    {
        internal static void Run(TestRunner t)
        {
            RoundtripTests(t);
            DefaultsTests(t);
            SanitizeTests(t);
            SchemaTests(t);
        }

        private static void RoundtripTests(TestRunner t)
        {
            t.Run("A full v1 state round-trips through Serialize/Parse/Sanitize", () =>
            {
                var state = new BoilerSaveState
                {
                    EntityId = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e").ToString("D"),
                    Tier = BoilerTier.T3,
                    Size = BoilerSize.Large,
                    Faction = BoilerFaction.IronTeeth,
                    Condition = 42.5f,
                    AutoMaintenance = false,
                    MaintenancePhase = MaintenancePhase.Maintaining,
                    MaintenanceRemainingHours = 1.5f,
                    LastMaintenanceDay = 7.25f,
                    WaterSupplyMode = WaterSupplyMode.Valve,
                    PowerLineAttached = true,
                };

                var json = SeeSaveCodec.Serialize(state);
                var parsed = SeeSaveCodec.Parse(json, null);
                Assert.NotNull(parsed, "parse");
                var sanitized = SeeSaveCodec.Sanitize(parsed, currentDay: 10f, log: null);

                Assert.Equal(state.EntityId, sanitized.EntityId, "entity id");
                Assert.Equal(state.Tier, sanitized.Tier, "tier");
                Assert.Equal(state.Size, sanitized.Size, "size");
                Assert.Equal(state.Faction, sanitized.Faction, "faction");
                Assert.Near(state.Condition, sanitized.Condition, 1e-6, "condition");
                Assert.Equal(state.AutoMaintenance, sanitized.AutoMaintenance, "auto flag");
                Assert.Equal(state.MaintenancePhase, sanitized.MaintenancePhase, "phase");
                Assert.Near(state.MaintenanceRemainingHours, sanitized.MaintenanceRemainingHours, 1e-6, "remaining");
                Assert.Near(state.LastMaintenanceDay, sanitized.LastMaintenanceDay, 1e-6, "last day");
                Assert.Equal(state.WaterSupplyMode, sanitized.WaterSupplyMode, "water mode");
                Assert.Equal(state.PowerLineAttached, sanitized.PowerLineAttached, "power line");
            });

            t.Run("The serialized document carries schemaVersion 1 and the entity id", () =>
            {
                var json = SeeSaveCodec.Serialize(BoilerSaveState.NewGame());
                Assert.Contains(json, "\"schemaVersion\":1", "schema version");
                Assert.Contains(json, "entityId", "entity id key");
                Assert.Equal(1, SeeSaveCodec.CurrentSchemaVersion, "codec version");
            });

            t.Run("Unknown JSON properties are ignored on load", () =>
            {
                var parsed = SeeSaveCodec.Parse(
                    "{\"schemaVersion\":1,\"condition\":55,\"futureField\":{\"nested\":true}}", null);
                Assert.NotNull(parsed, "parse");
                Assert.Near(55.0, parsed!.Condition, 1e-6, "condition survives");
            });
        }

        private static void DefaultsTests(TestRunner t)
        {
            t.Run("NewGame is fully maintained", () =>
            {
                var state = BoilerSaveState.NewGame();
                Assert.Near(100.0, state.Condition, 1e-6, "condition");
                Assert.True(state.AutoMaintenance, "auto maintenance");
                Assert.Equal(MaintenancePhase.None, state.MaintenancePhase, "phase");
                Assert.Near(0.0, state.MaintenanceRemainingHours, 1e-6, "remaining");
                Assert.Equal(WaterSupplyMode.Auto, state.WaterSupplyMode, "water mode");
            });

            t.Run("Parsing an empty object keeps the documented defaults", () =>
            {
                var parsed = SeeSaveCodec.Parse("{}", null);
                Assert.NotNull(parsed, "parse");
                Assert.Near(100.0, parsed!.Condition, 1e-6, "condition");
                Assert.Equal(BoilerTier.T1, parsed.Tier, "tier");
                Assert.Equal(BoilerSize.Small, parsed.Size, "size");
                Assert.Equal(BoilerFaction.Folktails, parsed.Faction, "faction");
            });

            t.Run("Unparsable data returns null instead of throwing", () =>
            {
                Assert.True(SeeSaveCodec.Parse(null!, null) == null, "null text");
                Assert.True(SeeSaveCodec.Parse(string.Empty, null) == null, "empty text");
                Assert.True(SeeSaveCodec.Parse("not json", null) == null, "garbage");
                Assert.True(SeeSaveCodec.Parse("[1,2]", null) == null, "array is not a boiler record");
            });

            t.Run("Sanitize(null) returns the safe new-boiler state", () =>
            {
                var state = SeeSaveCodec.Sanitize(null, 0f, null);
                Assert.Near(100.0, state.Condition, 1e-6, "condition");
                Assert.Equal(MaintenancePhase.None, state.MaintenancePhase, "phase");
            });
        }

        private static void SanitizeTests(TestRunner t)
        {
            t.Run("Out-of-range condition is clamped into 0..100", () =>
            {
                var high = SeeSaveCodec.Sanitize(new BoilerSaveState { Condition = 250f }, 0f, null);
                Assert.Near(100.0, high.Condition, 1e-6, "250 -> 100");

                var low = SeeSaveCodec.Sanitize(new BoilerSaveState { Condition = -20f }, 0f, null);
                Assert.Near(0.0, low.Condition, 1e-6, "-20 -> 0");
            });

            t.Run("NaN condition resets to 100 and turns auto maintenance off", () =>
            {
                var log = new RecordingLog();
                var state = SeeSaveCodec.Sanitize(
                    new BoilerSaveState { Condition = float.NaN, AutoMaintenance = true }, 0f, log);
                Assert.Near(100.0, state.Condition, 1e-6, "NaN -> 100");
                Assert.False(state.AutoMaintenance, "corrupt condition must force a manual maintenance (CONTRACT 6.2)");
                Assert.True(log.Entries.Count > 0, "warned");
            });

            t.Run("Maintenance remaining hours clamp to the closed [0,2] interval", () =>
            {
                var high = SeeSaveCodec.Sanitize(
                    new BoilerSaveState { MaintenancePhase = MaintenancePhase.Maintaining, MaintenanceRemainingHours = 5f },
                    0f, null);
                Assert.Near(2.0, high.MaintenanceRemainingHours, 1e-6, "5 -> 2");

                var low = SeeSaveCodec.Sanitize(
                    new BoilerSaveState { MaintenancePhase = MaintenancePhase.Maintaining, MaintenanceRemainingHours = -1f },
                    0f, null);
                Assert.Near(0.0, low.MaintenanceRemainingHours, 1e-6, "-1 -> 0");

                var none = SeeSaveCodec.Sanitize(
                    new BoilerSaveState { MaintenancePhase = MaintenancePhase.None, MaintenanceRemainingHours = 1.5f },
                    0f, null);
                Assert.Near(0.0, none.MaintenanceRemainingHours, 1e-6, "None -> 0");
            });

            t.Run("Last maintenance day is clamped to the current day", () =>
            {
                var state = SeeSaveCodec.Sanitize(new BoilerSaveState { LastMaintenanceDay = 100f }, 10f, null);
                Assert.Near(10.0, state.LastMaintenanceDay, 1e-6, "future day clamped");

                var negative = SeeSaveCodec.Sanitize(new BoilerSaveState { LastMaintenanceDay = -3f }, 10f, null);
                Assert.Near(0.0, negative.LastMaintenanceDay, 1e-6, "negative day clamped");

                var nan = SeeSaveCodec.Sanitize(new BoilerSaveState { LastMaintenanceDay = float.NaN }, 10f, null);
                Assert.Near(0.0, nan.LastMaintenanceDay, 1e-6, "NaN day clamped");
            });

            t.Run("An invalid entity id is detached with a warning", () =>
            {
                var log = new RecordingLog();
                var state = SeeSaveCodec.Sanitize(new BoilerSaveState { EntityId = "not-a-guid" }, 0f, log);
                Assert.Equal(string.Empty, state.EntityId, "detached");
                Assert.True(log.Entries.Count > 0, "warned");
            });

            t.Run("An upper-case GUID normalizes to the D format", () =>
            {
                var guid = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");
                var state = SeeSaveCodec.Sanitize(
                    new BoilerSaveState { EntityId = guid.ToString("D").ToUpperInvariant() }, 0f, null);
                Assert.Equal(guid.ToString("D"), state.EntityId, "normalized");
            });

            t.Run("Unknown enum names fall back to the documented defaults", () =>
            {
                var parsed = SeeSaveCodec.Parse(
                    "{\"schemaVersion\":1,\"tier\":\"T9\",\"size\":\"Gigantic\",\"faction\":\"Beavers\","
                    + "\"maintenanceState\":\"Broken\",\"waterSupplyMode\":\"pipe\"}", null);
                Assert.NotNull(parsed, "parse");
                var state = SeeSaveCodec.Sanitize(parsed, 0f, null);
                Assert.Equal(BoilerTier.T1, state.Tier, "tier");
                Assert.Equal(BoilerSize.Small, state.Size, "size");
                Assert.Equal(BoilerFaction.Folktails, state.Faction, "faction");
                Assert.Equal(MaintenancePhase.None, state.MaintenancePhase, "phase");
                Assert.Equal(WaterSupplyMode.Auto, state.WaterSupplyMode, "water mode");
            });

            t.Run("A missing field keeps its documented default", () =>
            {
                var parsed = SeeSaveCodec.Parse("{\"schemaVersion\":1,\"condition\":50}", null);
                Assert.NotNull(parsed, "parse");
                Assert.Near(50.0, parsed!.Condition, 1e-6, "provided");
                Assert.True(parsed.AutoMaintenance, "default auto flag");
                Assert.Near(0.0, parsed.MaintenanceRemainingHours, 1e-6, "default remaining");
            });
        }

        private static void SchemaTests(TestRunner t)
        {
            t.Run("A newer schema version is reported as a warning", () =>
            {
                var log = new RecordingLog();
                var parsed = SeeSaveCodec.Parse("{\"schemaVersion\":99,\"condition\":80}", log);
                Assert.NotNull(parsed, "data is not discarded");
                var warned = false;
                foreach (var entry in log.Entries)
                {
                    if (entry.Contains("99", StringComparison.Ordinal))
                    {
                        warned = true;
                        break;
                    }
                }

                Assert.True(warned, "newer schema reported");
            });

            t.Run("A newer schema cannot be migrated (read-only degradation)", () =>
            {
                Assert.True(SeeSaveCodec.Migrate(99, "{\"schemaVersion\":99}") == null, "newer -> null");
                Assert.True(SeeSaveCodec.Migrate(1, null!) == null, "null text -> null");
            });

            t.Run("Migrating v1 to v1 returns the text unchanged", () =>
            {
                var json = SeeSaveCodec.Serialize(BoilerSaveState.NewGame());
                Assert.Equal(json, SeeSaveCodec.Migrate(1, json), "v1 is current");
            });

            t.Run("There is no migration step before v1", () =>
                Assert.Equal(0, SeeSaveCodec.Migrations.Count, "v1 migration chain"));
        }
    }
}
