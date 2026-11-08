using System;
using SEE.Gameplay;

namespace SEE.Integration.Adapters
{
    /// <summary>
    /// Game-side <see cref="ICompatOptionsSource"/> reading the session configuration from
    /// <see cref="SEERuntime"/> on every access (REQ-COMPAT-15: config edits apply at load without a
    /// save migration). Never throws.
    /// </summary>
    public sealed class RuntimeCompatOptions : ICompatOptionsSource
    {
        private readonly SEERuntime _runtime;

        /// <summary>Creates the source.</summary>
        /// <param name="runtime">Gameplay runtime singleton.</param>
        public RuntimeCompatOptions(SEERuntime runtime)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        /// <inheritdoc />
        public CompatAdapterOptions Current
        {
            get
            {
                try
                {
                    return CompatAdapterOptions.FromConfig(_runtime.Config);
                }
                catch (Exception)
                {
                    return CompatAdapterOptions.Default;
                }
            }
        }
    }
}
