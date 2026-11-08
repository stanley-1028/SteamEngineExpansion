using System;
using SEE.Core.Abstractions;
using SEE.UI.Status;
using Timberborn.BaseComponentSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.EntitySystem;
using Timberborn.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace SEE.UI.Panels
{
    /// <summary>
    /// Entity panel fragment of the steam boiler (requirements §15.2/§15.3, REQ-UI-01/02/03/07).
    ///
    /// The whole fragment is authored in code with UIElements and needs no UXML/USS asset or
    /// prefab; it is added to the vanilla panel through the game's own
    /// <see cref="EntityPanelModule"/> multi-binding (see <see cref="BoilerPanelModuleProvider"/>).
    ///
    /// Behaviour:
    /// <list type="bullet">
    /// <item>invisible for every entity without a readable boiler view, so other buildings get no
    /// extra rows;</item>
    /// <item>reads only <see cref="IBoilerQuery"/> / <see cref="IBoilerStatusView"/>, never a
    /// gameplay type (REQ-ARCH-03);</item>
    /// <item>re-queries at most <c>perf.uiRefreshHz</c> times per second, and only rebuilds the
    /// localized payload when the value fingerprint changed (dirty marker, REQ-OPS-02);</item>
    /// <item>does no work at all while the panel is closed: <see cref="ClearFragment"/> drops the
    /// entity and <see cref="UpdateFragment"/> returns before any query (REQ-OPS-04);</item>
    /// <item>buttons send this entity's id only and go through <see cref="IBoilerLogic"/>
    /// (REQ-UI-07);</item>
    /// <item>never throws: a failing query hides the fragment instead of breaking the panel.</item>
    /// </list>
    /// </summary>
    public sealed class BoilerPanelFragment : IEntityPanelFragment
    {
        private readonly IBoilerQuery _query;
        private readonly IBoilerLogic _logic;
        private readonly ILoc _loc;
        private readonly BoilerPanelOptions _options;
        private readonly float _refreshIntervalSeconds;

        private BoilerPanelView? _view;
        private BaseComponent? _entity;
        private Guid _entityId;
        private long _fingerprint;
        private bool _hasFingerprint;
        private bool _forceRender;
        private bool _paused;
        private float _nextRefreshTime;
        private int _snapshotReads;
        private int _renders;

        /// <summary>Creates the fragment; Bindito injects the query, command and localization services.</summary>
        /// <param name="query">Read-only boiler query registered by the gameplay assembly.</param>
        /// <param name="logic">Per-boiler command surface registered by the gameplay assembly.</param>
        /// <param name="loc">Localization service.</param>
        public BoilerPanelFragment(IBoilerQuery query, IBoilerLogic logic, ILoc loc)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _logic = logic ?? throw new ArgumentNullException(nameof(logic));
            _loc = loc ?? throw new ArgumentNullException(nameof(loc));
            _options = SeeUiConfigSource.Options;
            _refreshIntervalSeconds = BoilerPanelRefreshPolicy.IntervalSeconds(_options.UiRefreshHz);
        }

        /// <summary>Number of snapshot reads since the panel was created (perf diagnostics, REQ-OPS-01).</summary>
        public int SnapshotReads => _snapshotReads;

        /// <summary>Number of localized payload rebuilds since the panel was created (dirty-marker diagnostics).</summary>
        public int Renders => _renders;

        /// <inheritdoc />
        public VisualElement InitializeFragment()
        {
            _view = new BoilerPanelView(_loc, OnPauseToggled, OnMaintainRequested, OnAutoMaintenanceChanged,
                OnWaterModeChanged);
            return _view.Root;
        }

        /// <inheritdoc />
        public void ShowFragment(BaseComponent entity)
        {
            _entity = entity;
            _entityId = ResolveEntityId(entity);
            _hasFingerprint = false;
            _forceRender = true;
            _nextRefreshTime = 0f;
            Refresh(forceRender: true);
        }

        /// <inheritdoc />
        public void UpdateFragment()
        {
            // The panel system calls this every frame. A closed panel (ClearFragment dropped the
            // entity) returns here before any query, formatting or allocation (REQ-OPS-04).
            if (_view is null || !BoilerPanelRefreshPolicy.ShouldQuery(_entity != null, _entityId != Guid.Empty))
            {
                return;
            }

            float now = Time.unscaledTime;
            if (!BoilerPanelRefreshPolicy.IsDue(now, _nextRefreshTime))
            {
                return;
            }

            _nextRefreshTime = BoilerPanelRefreshPolicy.NextAllowedTime(now, _refreshIntervalSeconds);
            Refresh(forceRender: _forceRender);
        }

        /// <inheritdoc />
        public void ClearFragment()
        {
            _entity = null;
            _entityId = Guid.Empty;
            _hasFingerprint = false;
            _forceRender = false;
            _view?.SetVisible(false);
        }

        private void OnPauseToggled()
        {
            if (_entityId == Guid.Empty)
            {
                return;
            }

            _logic.SetPaused(_entityId, !_paused);
            RequestQuickResync();
        }

        private void OnMaintainRequested()
        {
            if (_entityId == Guid.Empty)
            {
                return;
            }

            _logic.RequestMaintenanceNow(_entityId);
            RequestQuickResync();
        }

        private void OnAutoMaintenanceChanged(bool enabled)
        {
            if (_entityId == Guid.Empty)
            {
                return;
            }

            _logic.SetAutoMaintenance(_entityId, enabled);
            RequestQuickResync();
        }

        private void OnWaterModeChanged(int index)
        {
            if (_entityId == Guid.Empty)
            {
                return;
            }

            _logic.SetWaterSupplyMode(_entityId, StatusDescriptors.WaterSupplyModeAt(index));
            RequestQuickResync();
        }

        /// <summary>Lets the next frame re-read the view so a click is reflected immediately.</summary>
        private void RequestQuickResync()
        {
            _forceRender = true;
            _nextRefreshTime = 0f;
        }

        private void Refresh(bool forceRender)
        {
            BoilerPanelView? view = _view;
            if (view == null)
            {
                return;
            }

            if (_entity is null || _entityId == Guid.Empty)
            {
                Hide(view);
                return;
            }

            IBoilerStatusView? status;
            try
            {
                _snapshotReads++;
                if (!_query.TryGetView(_entityId, out status) || status is null || !status.IsFinished)
                {
                    Hide(view);
                    return;
                }
            }
            catch (Exception)
            {
                // A broken boiler or adapter must never break the entity panel (REQ-SAFE-01).
                Hide(view);
                return;
            }

            long fingerprint = BoilerPanelFingerprint.Compute(status, _options);
            if (!forceRender && _hasFingerprint && fingerprint == _fingerprint)
            {
                return;
            }

            _fingerprint = fingerprint;
            _hasFingerprint = true;
            _forceRender = false;
            _paused = status.IsPaused;
            BoilerPanelModel model = BoilerPanelModelBuilder.Build(status, _options);
            BoilerPanelData data = BoilerPanelDataFactory.Create(model, _loc);
            if (!data.HasData)
            {
                Hide(view);
                return;
            }

            _renders++;
            view.Render(data);
        }

        private void Hide(BoilerPanelView view)
        {
            _hasFingerprint = false;
            _forceRender = false;
            view.SetVisible(false);
        }

        private static Guid ResolveEntityId(BaseComponent? entity)
        {
            if (entity is null)
            {
                return Guid.Empty;
            }

            var components = entity.AllComponents;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] is EntityComponent entityComponent)
                {
                    return entityComponent.EntityId;
                }
            }

            return Guid.Empty;
        }
    }
}
