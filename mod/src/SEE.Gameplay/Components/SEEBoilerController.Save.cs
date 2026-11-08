using System;
using SEE.Core.Abstractions;
using SEE.Core.Constants;
using SEE.Core.Logging;
using SEE.Core.Maintenance;
using SEE.Core.Save;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Partial part: the per-entity save of one boiler (requirements §9.8/§14.1, D-6 hybrid save).
    ///
    /// The game already serializes components transactionally as part of the world save, so SEE
    /// writes one component named "SEE" on its own boiler entity: seven fields, no tier/size/faction
    /// (rebuilt from the template) and no checksum (the world-node index owns it). A missing
    /// component means "a new boiler"; a broken one resets only that boiler (REQ-SAVE-03).
    ///
    /// Reads are guarded by <c>Has</c> because <c>IObjectLoader.Get</c> throws when a key is absent
    /// (verified game API fact); the surrogate state then goes through the pure
    /// <see cref="SeeSaveCodec.Sanitize"/> so clamping/NaN/range rules stay identical to the
    /// JSON test path.
    /// </summary>
    public sealed partial class SEEBoilerController
    {
        /// <inheritdoc />
        public void Save(IEntitySaver entitySaver)
        {
            if (!_awake)
            {
                return;
            }

            try
            {
                IObjectSaver? component = entitySaver.GetComponent(SeeSaveKeys.Component);
                if (component == null)
                {
                    return;
                }

                component.Set(SeeSaveKeys.Condition, _ledger.Condition);
                component.Set(SeeSaveKeys.AutoMaintenance, _ledger.AutoMaintenance);
                component.Set(SeeSaveKeys.MaintenanceState, _ledger.Phase.ToString());
                component.Set(SeeSaveKeys.MaintenanceRemainingHours, _ledger.RemainingHours);
                component.Set(SeeSaveKeys.LastMaintenanceDay, _ledger.LastMaintenanceDay);
                component.Set(SeeSaveKeys.WaterSupplyMode, _waterSupplyMode.ToString().ToLowerInvariant());
                component.Set(SeeSaveKeys.PowerLineAttached, _powerLineAttached);
            }
            catch (Exception exception)
            {
                // One boiler must never abort the world save (REQ-SAVE-01, REQ-SAFE-02/13).
                LogError("boiler save failed; the previous data is kept", exception);
            }
        }

        /// <inheritdoc />
        public void Load(IEntityLoader entityLoader)
        {
            if (!_awake)
            {
                return;
            }

            try
            {
                IObjectLoader? component = null;
                if (!entityLoader.TryGetComponent(SeeSaveKeys.Component, out component) || component == null)
                {
                    // Fresh boiler or a save written before SEE existed: "component missing" is not damage.
                    _saveLoaded = true;
                    return;
                }

                BoilerSaveState raw = ReadSavedState(component);
                BoilerSaveState safe = SeeSaveCodec.Sanitize(raw, CurrentDay(), _runtime.RawLog);
                ApplySavedState(safe);
                _saveLoaded = true;
            }
            catch (Exception exception)
            {
                // Keep the safe defaults created in Awake; the boiler stays playable (REQ-SAVE-03).
                _saveLoaded = true;
                LogError("boiler load failed; safe defaults kept", exception);
            }
        }

        /// <summary>Builds the persisted snapshot of this boiler (11 fields, D-8/D-6).</summary>
        /// <returns>The current state; never null.</returns>
        internal BoilerSaveState CreateSaveState()
        {
            return new BoilerSaveState
            {
                EntityId = _entityId.ToString("D"),
                Tier = _tier,
                Size = _size,
                Faction = _faction,
                Condition = _ledger.Condition,
                AutoMaintenance = _ledger.AutoMaintenance,
                MaintenancePhase = _ledger.Phase,
                MaintenanceRemainingHours = _ledger.RemainingHours,
                LastMaintenanceDay = _ledger.LastMaintenanceDay,
                WaterSupplyMode = _waterSupplyMode,
                PowerLineAttached = _powerLineAttached
            };
        }

        private BoilerSaveState ReadSavedState(IObjectLoader loader)
        {
            bool degraded = false;
            var state = new BoilerSaveState
            {
                Condition = ReadFloat(loader, SeeSaveKeys.Condition, SEEConst.MaxCondition, ref degraded),
                AutoMaintenance = ReadBool(loader, SeeSaveKeys.AutoMaintenance, _autoMaintenanceDefault, ref degraded),
                MaintenancePhase = ReadMaintenancePhase(loader, ref degraded),
                MaintenanceRemainingHours = ReadFloat(loader, SeeSaveKeys.MaintenanceRemainingHours, 0f, ref degraded),
                LastMaintenanceDay = ReadFloat(loader, SeeSaveKeys.LastMaintenanceDay, 0f, ref degraded),
                WaterSupplyMode = ReadWaterSupplyMode(loader, ref degraded),
                PowerLineAttached = ReadBool(loader, SeeSaveKeys.PowerLineAttached, false, ref degraded)
            };

            if (degraded)
            {
                try
                {
                    _runtime.Log.Warn(
                        SeeLogKeys.SaveCorrupt,
                        "boiler save had unreadable fields; documented defaults used for them|"
                        + _tier + "|" + _entityId.ToString("N"));
                }
                catch (Exception)
                {
                    // Diagnostics only.
                }
            }

            return state;
        }

        private void ApplySavedState(BoilerSaveState state)
        {
            _ledger.Reset(
                state.Condition,
                state.AutoMaintenance,
                state.MaintenancePhase,
                state.MaintenanceRemainingHours,
                state.LastMaintenanceDay);
            _waterSupplyMode = state.WaterSupplyMode;
            _powerLineAttached = state.PowerLineAttached;
            // REQ-SAVE-12: the mechanical-node write-back (output multiplier, water mode) must wait
            // until the vanilla graph exists; the first tick after load performs it.
            _applyAfterLoad = true;
        }

        private static float ReadFloat(IObjectLoader loader, PropertyKey<float> key, float fallback, ref bool degraded)
        {
            try
            {
                return loader.Has(key) ? loader.Get(key) : fallback;
            }
            catch (Exception)
            {
                degraded = true;
                return fallback;
            }
        }

        private static bool ReadBool(IObjectLoader loader, PropertyKey<bool> key, bool fallback, ref bool degraded)
        {
            try
            {
                return loader.Has(key) ? loader.Get(key) : fallback;
            }
            catch (Exception)
            {
                degraded = true;
                return fallback;
            }
        }

        private static string ReadString(IObjectLoader loader, PropertyKey<string> key, string fallback, ref bool degraded)
        {
            try
            {
                return loader.Has(key) ? loader.Get(key) ?? fallback : fallback;
            }
            catch (Exception)
            {
                degraded = true;
                return fallback;
            }
        }

        private static MaintenancePhase ReadMaintenancePhase(IObjectLoader loader, ref bool degraded)
        {
            string text = ReadString(loader, SeeSaveKeys.MaintenanceState, string.Empty, ref degraded);
            if (!string.IsNullOrEmpty(text)
                && Enum.TryParse(text, true, out MaintenancePhase phase)
                && Enum.IsDefined(typeof(MaintenancePhase), phase))
            {
                return phase;
            }

            return MaintenancePhase.None;
        }

        private static WaterSupplyMode ReadWaterSupplyMode(IObjectLoader loader, ref bool degraded)
        {
            string text = ReadString(loader, SeeSaveKeys.WaterSupplyMode, string.Empty, ref degraded);
            if (!string.IsNullOrEmpty(text)
                && Enum.TryParse(text, true, out WaterSupplyMode mode)
                && Enum.IsDefined(typeof(WaterSupplyMode), mode))
            {
                return mode;
            }

            return WaterSupplyMode.Auto;
        }
    }
}
