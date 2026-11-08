using SEE.Integration.Detection;

namespace SEE.Integration.Adapters
{
    /// <summary>
    /// Contract every optional-mod adapter implements (requirements §10.1/§10.3). Third-party types
    /// never appear in this signature; an adapter is created only after its mod was detected, and a
    /// failed initialization permanently closes it without affecting the core gameplay
    /// (REQ-COMPAT-03/07/17).
    /// </summary>
    public interface IExternalAdapter
    {
        /// <summary>Stable adapter id written into the save compat block, for example "SEE.Adapter.TimberPipes".</summary>
        string AdapterId { get; }

        /// <summary>Stable mod id the adapter belongs to, for example "TimberPipes".</summary>
        string ModId { get; }

        /// <summary>Current lifecycle state; only <see cref="AdapterState.Enabled"/> may write UI/save data.</summary>
        AdapterState State { get; }

        /// <summary>True when the mod is available and this adapter initialized successfully.</summary>
        bool IsAvailable { get; }

        /// <summary>Latest detection status; refreshed only on world load or manual re-detect, never per tick.</summary>
        ExternalModStatus Status { get; }

        /// <summary>Short machine-readable diagnostic explaining the state.</summary>
        string Diagnostic { get; }

        /// <summary>Failure key written into the save compat block; null unless <see cref="State"/> is Failed.</summary>
        string? FailureKey { get; }

        /// <summary>Initializes the adapter once. Never throws; failure closes the adapter.</summary>
        void Initialize();

        /// <summary>Releases the adapter. Must be symmetric with <see cref="Initialize"/> and never throws.</summary>
        void Shutdown();
    }
}
