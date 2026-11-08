using System;
using System.IO;
using SEE.Core.Config;
using SEE.Core.Constants;
using SEE.Core.Logging;
using SEE.Gameplay.Logging;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace SEE.Gameplay
{
    /// <summary>
    /// Mod entry point: the game instantiates every <see cref="IModStarter"/> with a parameterless
    /// constructor and calls <see cref="StartMod"/> once, before the game context DI container
    /// exists. This method therefore only records the mod path and loads <c>SEE_Config.json</c>;
    /// the resolved values are handed to the DI singleton <see cref="SEERuntime"/> later.
    ///
    /// It never throws: a broken config or a missing file degrades to compiled defaults and must
    /// not break game startup (REQ-SAFE-01/02).
    /// </summary>
    public sealed class SeeModStarter : IModStarter
    {
        /// <inheritdoc />
        public void StartMod(IModEnvironment modEnvironment)
        {
            try
            {
                SeeStartup.Initialize(modEnvironment != null ? modEnvironment.ModPath : null);
            }
            catch (Exception exception)
            {
                // Never let the mod entry point break game startup (REQ-SAFE-01).
                TryLogStartupFailure(exception);
            }
        }

        private static void TryLogStartupFailure(Exception exception)
        {
            try
            {
                Debug.LogError("[SEE/Error][" + SeeLogKeys.Startup + "] SEE StartMod failed: "
                    + exception.GetType().Name + ": " + exception.Message);
            }
            catch
            {
                // Unity logging itself is unavailable: there is nothing else safe to do.
            }
        }
    }

    /// <summary>
    /// Write-once hand-off between <see cref="SeeModStarter"/> (runs before DI exists) and
    /// <see cref="SEERuntime"/> (created by the game container).
    ///
    /// This is the same minimal seam the verified reference mod uses: the mod environment is not
    /// injectable, mod assemblies are loaded from bytes so <c>Assembly.Location</c> is empty, and
    /// the config file must be read exactly once before any gameplay runs. The state is written
    /// once during <c>StartMod</c> and read-only afterwards, so no simulation state lives here.
    /// </summary>
    internal static class SeeStartup
    {
        private static readonly object Gate = new object();

        private static bool _initialized;
        private static string? _modPath;
        private static SeeConfig _config = SeeConfig.Default;
        private static ThrottledLog? _log;
        private static ISeeLog? _rawLog;

        /// <summary>Absolute mod root as supplied by the game; null before/without <c>StartMod</c>.</summary>
        internal static string? ModPath
        {
            get
            {
                EnsureInitialized();
                return _modPath;
            }
        }

        /// <summary>Validated configuration (compiled defaults when the file is absent/corrupt).</summary>
        internal static SeeConfig Config
        {
            get
            {
                EnsureInitialized();
                return _config;
            }
        }

        /// <summary>Session-wide throttled log created during startup.</summary>
        internal static ThrottledLog Log
        {
            get
            {
                EnsureInitialized();
                return _log ??= new ThrottledLog(new UnitySeeLog(), SEEConst.LogThrottleSeconds);
            }
        }

        /// <summary>Raw unthrottled sink behind <see cref="Log"/>, for one-shot diagnostics only.</summary>
        internal static ISeeLog RawLog
        {
            get
            {
                EnsureInitialized();
                return _rawLog ??= new UnitySeeLog();
            }
        }

        /// <summary>Reads the environment and the config file; idempotent so a hot reload cannot corrupt it.</summary>
        /// <param name="modPath">Mod root supplied by the game; may be null.</param>
        internal static void Initialize(string? modPath)
        {
            lock (Gate)
            {
                if (_initialized)
                {
                    return;
                }

                _initialized = true;
                _modPath = modPath;
                LoadConfiguration(modPath);
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            Initialize(null);
        }

        private static void LoadConfiguration(string? modPath)
        {
            var rawLog = new UnitySeeLog();
            SeeConfig config = SeeConfig.Default;
            string? configPath = null;

            try
            {
                configPath = FindConfigPath(modPath);
                if (configPath != null)
                {
                    string json = File.ReadAllText(configPath);
                    config = SeeConfigCodec.Parse(json, rawLog);
                }
                else
                {
                    rawLog.Warn(
                        SeeLogKeys.ConfigInvalid,
                        "SEE_Config.json was not found next to the mod; compiled-in defaults are used");
                }
            }
            catch (Exception exception)
            {
                // A missing/unreadable/partially written config is not fatal (REQ-SAFE-01).
                TryWarn(rawLog, "SEE_Config.json could not be read (" + exception.GetType().Name
                    + "); compiled-in defaults are used");
                config = SeeConfig.Default;
                configPath = null;
            }

            _config = config;
            _rawLog = rawLog;
            _log = new ThrottledLog(rawLog, config.Compat.LogThrottleSeconds);
            try
            {
                _log.Info(
                    SeeLogKeys.ConfigLoaded,
                    "SEE " + SEEVersion.ModVersion + " config=" + (configPath ?? "<compiled defaults>")
                    + " schema=" + config.ConfigSchemaVersion
                    + " maintenance=" + (config.Maintenance.Enabled ? "on" : "off")
                    + " modPath=" + (modPath ?? "<unknown>"));
            }
            catch (Exception)
            {
                // Diagnostics only.
            }
        }

        private static string? FindConfigPath(string? modPath)
        {
            if (string.IsNullOrEmpty(modPath) || !Directory.Exists(modPath))
            {
                return null;
            }

            string candidate = Path.Combine(modPath!, SEEConst.ConfigFileName);
            return File.Exists(candidate) ? candidate : null;
        }

        private static void TryWarn(ISeeLog log, string message)
        {
            try
            {
                log.Warn(SeeLogKeys.ConfigInvalid, message);
            }
            catch (Exception)
            {
                // Diagnostics only.
            }
        }
    }
}