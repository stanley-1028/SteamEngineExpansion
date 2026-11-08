using System;
using SEE.Integration.Detection;

namespace SEE.Integration.Adapters
{
    /// <summary>
    /// Shared skeleton of the three optional-mod adapters. It holds the immutable identity, the
    /// cached detection status and the symmetric initialize/shutdown lifecycle; the concrete
    /// adapters add only the capability hooks (water supply, power-line injection, belt fuel
    /// supply). Initialization is single-shot: a failure permanently closes the adapter and is never
    /// retried in the same session (requirements §10.7, REQ-COMPAT-17).
    /// </summary>
    public abstract class ExternalAdapterBase : IExternalAdapter
    {
        private readonly ICompatDetector _detector;
        private bool _initialized;

        /// <summary>Creates the adapter identity.</summary>
        /// <param name="detector">Session compatibility detector.</param>
        /// <param name="adapterId">Stable adapter id.</param>
        /// <param name="modId">Stable mod id.</param>
        protected ExternalAdapterBase(ICompatDetector detector, string adapterId, string modId)
        {
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
            AdapterId = adapterId;
            ModId = modId;
            Status = ExternalModStatus.NotScanned(modId);
            Diagnostic = "not-initialized";
        }

        /// <inheritdoc />
        public string AdapterId { get; }

        /// <inheritdoc />
        public string ModId { get; }

        /// <inheritdoc />
        public AdapterState State { get; private set; } = AdapterState.Absent;

        /// <inheritdoc />
        public bool IsAvailable => State == AdapterState.Enabled;

        /// <inheritdoc />
        public ExternalModStatus Status { get; private set; }

        /// <inheritdoc />
        public string Diagnostic { get; private set; }

        /// <inheritdoc />
        public string? FailureKey { get; private set; }

        /// <inheritdoc />
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            try
            {
                Status = _detector.Detect(ModId);
                if (Status.IsAvailable)
                {
                    OnAvailable();
                    if (State == AdapterState.Absent || State == AdapterState.Failed)
                    {
                        Enable("ok");
                    }
                }
                else if (Status.Detected)
                {
                    Fail("degraded:" + (Status.Detail ?? "unknown"));
                }
                else
                {
                    State = AdapterState.Absent;
                    Diagnostic = "assembly-missing";
                }
            }
            catch (Exception exception)
            {
                // A failed optional adapter only loses convenience (REQ-COMPAT-03/07).
                Fail("adapter-init-failed:" + exception.GetType().Name);
            }
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!_initialized)
            {
                return;
            }

            _initialized = false;
            try
            {
                OnShutdown();
            }
            catch (Exception)
            {
                // A failed shutdown must not affect the rest of the mod (requirements §10.7).
            }

            State = AdapterState.Absent;
            Diagnostic = "shutdown";
        }

        /// <summary>Marks the adapter as intentionally detected but disabled.</summary>
        /// <param name="reason">Short reason key.</param>
        protected void SetDetected(string reason)
        {
            State = AdapterState.Detected;
            Diagnostic = reason;
            FailureKey = null;
        }

        /// <summary>Marks the adapter as enabled.</summary>
        /// <param name="reason">Short reason key.</param>
        protected void Enable(string reason)
        {
            State = AdapterState.Enabled;
            Diagnostic = reason;
            FailureKey = null;
        }

        /// <summary>Marks the adapter as failed; the failure key is derived from the stable mod id.</summary>
        /// <param name="reason">Short reason key.</param>
        protected void Fail(string reason)
        {
            State = AdapterState.Failed;
            Diagnostic = reason;
            FailureKey = BuildFailureKey(reason);
        }

        /// <summary>Called once when the mod is available; concrete adapters hook their setup here.</summary>
        protected virtual void OnAvailable()
        {
        }

        /// <summary>Called once on shutdown; must mirror whatever <see cref="OnAvailable"/> set up.</summary>
        protected virtual void OnShutdown()
        {
        }

        private string BuildFailureKey(string reason)
        {
            string prefix = CompatAnchors.Normalize(ModId);
            if (prefix.Length == 0)
            {
                prefix = "external";
            }

            int cut = reason.IndexOf(':');
            string token = cut > 0 ? reason.Substring(0, cut) : reason;
            var builder = new System.Text.StringBuilder(prefix.Length + token.Length + 1);
            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];
                builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
            }

            return prefix + "-" + builder.ToString().Trim('-');
        }
    }
}
