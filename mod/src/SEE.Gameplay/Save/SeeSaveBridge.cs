using System;
using System.Collections.Generic;
using SEE.Core.Constants;
using SEE.Core.Logging;
using SEE.Core.Save;
using SEE.Gameplay.Components;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.WorldPersistence;

namespace SEE.Gameplay
{
    /// <summary>
    /// World-node index of the SEE save (requirements §14.1, D-6 hybrid decision / AD-11).
    ///
    /// The per-boiler mutable state itself lives in the per-entity <c>IPersistentEntity</c> component
    /// named "SEE" (see <see cref="Components.SEEBoilerController"/>); this singleton owns only the
    /// node that makes the save self-describing: schemaVersion, modVersion, checksum, the
    /// buildings[] index (entityId/tier/size/faction) and the compat block. The node is stored as
    /// the canonical Core codec document because the vanilla singleton persistence has no
    /// nested-object primitive; the primitive header keys mirror it for inspection.
    ///
    /// A missing or damaged node only loses the index and logs a warning — the entity components
    /// still restore every boiler (REQ-SAVE-03), so the index is diagnostics and migration aid,
    /// never the source of truth for per-boiler state.
    /// </summary>
    public sealed class SeeSaveBridge : ILoadableSingleton, ISaveableSingleton
    {
        private readonly SEERuntime _runtime;
        private readonly SEEBoilerRegistry _registry;
        private readonly ISingletonLoader _singletonLoader;

        private SeeSaveNode _loadedNode = SeeSaveNode.NewGame();
        private bool _readOnly;
        private readonly List<string> _compatDetected = new List<string>(4);
        private readonly List<string> _compatEnabled = new List<string>(4);
        private readonly List<string> _compatFailed = new List<string>(4);
        private bool _checksumMismatchReported;

        /// <summary>Creates the bridge; the singleton loader is supplied by the game container.</summary>
        /// <param name="runtime">Gameplay runtime (log).</param>
        /// <param name="registry">Registry of active boilers (index enumeration).</param>
        /// <param name="singletonLoader">Game world-node loader.</param>
        public SeeSaveBridge(
            SEERuntime runtime,
            SEEBoilerRegistry registry,
            ISingletonLoader singletonLoader)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _singletonLoader = singletonLoader ?? throw new ArgumentNullException(nameof(singletonLoader));
        }

        /// <summary>The last loaded or last saved node (diagnostics and P5 compat integration).</summary>
        public SeeSaveNode LoadedNode => _loadedNode;

        /// <summary>True when the loaded save was written by a newer schema and must not be rewritten.</summary>
        public bool ReadOnly => _readOnly;

        /// <summary>
        /// Records the compatibility snapshot written into the next save node (requirements §6.3).
        /// The integration layer owns detection; gameplay only persists its diagnostic snapshot, and
        /// the next detection pass remains authoritative (AD-5, REQ-SAVE-09).
        /// </summary>
        /// <param name="detected">Detected module short names; null is treated as empty.</param>
        /// <param name="enabled">Enabled adapter ids; null is treated as empty.</param>
        /// <param name="failed">Failure reason keys; null is treated as empty.</param>
        public void RecordCompatSnapshot(
            IReadOnlyList<string>? detected,
            IReadOnlyList<string>? enabled,
            IReadOnlyList<string>? failed)
        {
            CopyInto(_compatDetected, detected);
            CopyInto(_compatEnabled, enabled);
            CopyInto(_compatFailed, failed);
        }

        /// <inheritdoc />
        public void Save(ISingletonSaver singletonSaver)
        {
            try
            {
                if (_readOnly)
                {
                    _runtime.Log.Warn(
                        SeeLogKeys.SaveReadOnly,
                        "newer SEE save schema loaded; the world node is not rewritten");
                    return;
                }

                IObjectSaver? node = singletonSaver.GetSingleton(SeeSaveKeys.Node);
                if (node == null)
                {
                    return;
                }

                SeeSaveNode model = BuildNode();
                string document = SeeSaveNodeCodec.Serialize(model);
                node.Set(SeeSaveKeys.SchemaVersion, model.SchemaVersion);
                node.Set(SeeSaveKeys.ModVersion, model.ModVersion);
                node.Set(SeeSaveKeys.Checksum, SeeSaveNodeCodec.ComputeChecksum(document) ?? string.Empty);
                node.Set(SeeSaveKeys.Document, document);
                _loadedNode = model;
            }
            catch (Exception exception)
            {
                // One failed node write must never abort the world save (REQ-SAVE-01).
                LogError("SEE save node write failed; the previous data is kept", exception);
            }
        }

        /// <inheritdoc />
        public void Load()
        {
            try
            {
                _readOnly = false;
                _checksumMismatchReported = false;
                IObjectLoader? node = null;
                if (!_singletonLoader.TryGetSingleton(SeeSaveKeys.Node, out node) || node == null)
                {
                    // No SEE node: a new world or a save written before SEE existed, not damage.
                    _loadedNode = SeeSaveNode.NewGame();
                    return;
                }

                string document = node.Has(SeeSaveKeys.Document) ? node.Get(SeeSaveKeys.Document) : string.Empty;
                if (string.IsNullOrEmpty(document))
                {
                    // Header-only node (pre-document builds): salvage the version diagnostics.
                    _loadedNode = SeeSaveNode.NewGame();
                    if (node.Has(SeeSaveKeys.SchemaVersion))
                    {
                        int version = node.Get(SeeSaveKeys.SchemaVersion);
                        _loadedNode.SchemaVersion = version;
                        if (version > SEEVersion.SaveSchemaVersion)
                        {
                            _readOnly = true;
                            _runtime.Log.Warn(SeeLogKeys.SaveReadOnly, "SEE save node schema is newer; read-only");
                        }
                    }

                    return;
                }

                SeeSaveLoadResult result = SeeSaveNodeCodec.Load(document!, _runtime.RawLog);
                SeeSaveNode loaded = result.Node;
                _readOnly = result.ReadOnly;
                string? checksum = SeeSaveNodeCodec.ComputeChecksum(document!);
                if (checksum != null && !string.Equals(checksum, loaded.Checksum, StringComparison.OrdinalIgnoreCase))
                {
                    if (!_checksumMismatchReported)
                    {
                        _checksumMismatchReported = true;
                        _runtime.Log.Warn(
                            SeeLogKeys.SaveCorrupt,
                            "SEE save node checksum mismatch; the index is rebuilt from the live world");
                    }
                }

                _loadedNode = loaded;
                CopyInto(_compatDetected, loaded.Compat.Detected);
                CopyInto(_compatEnabled, loaded.Compat.Enabled);
                CopyInto(_compatFailed, loaded.Compat.Failed);
            }
            catch (Exception exception)
            {
                // A damaged node only loses the index; the world keeps loading (REQ-SAVE-03).
                _loadedNode = SeeSaveNode.NewGame();
                LogError("SEE save node load failed; safe defaults are kept", exception);
            }
        }

        private SeeSaveNode BuildNode()
        {
            var node = new SeeSaveNode
            {
                SchemaVersion = SEEVersion.SaveSchemaVersion,
                ModVersion = SEEVersion.ModVersion
            };

            IReadOnlyList<SEEBoilerController> controllers = _registry.Controllers;
            for (int i = 0; i < controllers.Count; i++)
            {
                SEEBoilerController controller = controllers[i];
                if (controller == null || !controller.IsFinished)
                {
                    continue;
                }

                node.Buildings.Add(new SeeSaveBuildingIndex
                {
                    EntityId = controller.EntityId.ToString("D"),
                    Tier = controller.Tier,
                    Size = controller.Size,
                    Faction = controller.Faction
                });
            }

            CopyInto(node.Compat.Detected, _compatDetected);
            CopyInto(node.Compat.Enabled, _compatEnabled);
            CopyInto(node.Compat.Failed, _compatFailed);
            return node;
        }

        private static void CopyInto(List<string> target, IReadOnlyList<string>? source)
        {
            target.Clear();
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                string? value = source[i];
                if (!string.IsNullOrEmpty(value))
                {
                    target.Add(value!);
                }
            }
        }

        private void LogError(string message, Exception exception)
        {
            try
            {
                _runtime.Log.Error(SeeLogKeys.SaveError, message + " (" + exception.GetType().Name + ")");
            }
            catch (Exception)
            {
                // Diagnostics only (REQ-SAFE-02).
            }
        }
    }
}
