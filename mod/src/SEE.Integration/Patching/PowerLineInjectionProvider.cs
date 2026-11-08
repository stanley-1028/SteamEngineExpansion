using System;
using System.Collections.Generic;
using System.Text;
using SEE.Core.Logging;
using SEE.Gameplay;
using SEE.Integration.Adapters;
using SEE.Integration.Detection;
using Timberborn.BlueprintSystem;

namespace SEE.Integration.Patching
{
    /// <summary>
    /// Public game extension point used to inject <c>PowerLineSpec</c> into SEE boiler blueprints
    /// (requirements §10.4 method ②, REQ-POWER-16). Vanilla multi-binds
    /// <c>IBlueprintModifierProvider</c> (verified in <c>GameFactionSystemConfigurator.Configure</c>)
    /// and <c>SpecService.Deserialize</c> merges the returned JSON strings into the blueprint bundle
    /// before deserialization. No Harmony patch and no external blueprint edit is involved.
    /// <para>
    /// The provider answers only the 24 SEE boiler paths, only while Power Lines detection is
    /// <see cref="Availability.Available"/> and <c>compat.enablePowerLineInjection</c> is on; every
    /// other path and every failure returns an empty list, so the vanilla MUST path is untouched.
    /// </para>
    /// </summary>
    public sealed class PowerLineInjectionProvider : IBlueprintModifierProvider
    {
        private readonly ICompatDetector _detector;
        private readonly ICompatOptionsSource _options;
        private readonly SEERuntime _runtime;
        private bool _logged;

        /// <summary>Creates the provider.</summary>
        /// <param name="detector">Session compatibility detector.</param>
        /// <param name="options">Late-bound <c>compat.*</c> options.</param>
        /// <param name="runtime">Gameplay runtime for the one-shot compat log.</param>
        public PowerLineInjectionProvider(
            ICompatDetector detector,
            ICompatOptionsSource options,
            SEERuntime runtime)
        {
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        /// <inheritdoc />
        public string ModifierName => PowerLineInjectionModifier.ModifierName;

        /// <summary>True once at least one boiler blueprint received the fragment this session.</summary>
        public bool InjectionActive { get; private set; }

        /// <summary>Short diagnostic of the last decision; "not-requested" before any SEE path.</summary>
        public string LastDecision { get; private set; } = "not-requested";

        /// <inheritdoc />
        public IEnumerable<string> GetModifiers(string blueprintPath)
        {
            try
            {
                if (!PowerLineInjectionModifier.TryBuildForPath(blueprintPath, out string json))
                {
                    return Array.Empty<string>();
                }

                if (!_options.Current.EnablePowerLineInjection)
                {
                    LastDecision = "config-disabled";
                    return Array.Empty<string>();
                }

                ExternalModStatus powerLines = _detector.Detect(CompatAnchors.PowerLines);
                if (!powerLines.IsAvailable)
                {
                    LastDecision = "power-lines-unavailable:" + (powerLines.Detail ?? "unknown");
                    return Array.Empty<string>();
                }

                InjectionActive = true;
                LastDecision = "injected";
                LogOnce("power-line-injection-active: " + blueprintPath);
                return new[] { json };
            }
            catch (Exception exception)
            {
                LastDecision = "injection-failed:" + exception.GetType().Name;
                LogOnce("pl-inject-failed: " + exception.GetType().Name);
                return Array.Empty<string>();
            }
        }

        private void LogOnce(string message)
        {
            if (_logged)
            {
                return;
            }

            _logged = true;
            try
            {
                _runtime.Log.Compat(SeeLogKeys.CompatPatch, message);
            }
            catch (Exception)
            {
                // Diagnostics only (REQ-SAFE-01).
            }
        }

        /// <summary>Renders the provider state for diagnostics/tests.</summary>
        /// <returns>Short human-readable state.</returns>
        public string Describe()
        {
            var builder = new StringBuilder(64);
            builder.Append("powerline-injection active=");
            builder.Append(InjectionActive ? "yes" : "no");
            builder.Append(" last=");
            builder.Append(LastDecision);
            return builder.ToString();
        }
    }
}
