using System;
using System.IO;
using System.Reflection;
using SEE.Core.Config;
using SEE.Core.Constants;

namespace SEE.UI.Panels
{
    /// <summary>
    /// Resolves the panel options from the shipped <c>SEE_Config.json</c> (requirements §7.7,
    /// §15.7). Gameplay parses the same file once at startup; the UI cannot read the gameplay
    /// singleton without referencing the gameplay assembly (CONTRACT_SEE §2.1), so it re-parses
    /// the same document with the same validated Core codec and keeps only the seven values the
    /// panel needs. The file is read once, lazily, on first panel use - never in
    /// <c>UpdateFragment</c> (REQ-OPS-02).
    ///
    /// Failure policy: a missing or unreadable file falls back to <see cref="SeeConfig.Default"/>,
    /// which is exactly the behaviour the contract requires for a missing config, so the panel
    /// can never throw because of configuration.
    /// </summary>
    internal static class SeeUiConfigSource
    {
        private static BoilerPanelOptions? _options;

        /// <summary>Panel options of this session; parsed on first use and cached afterwards.</summary>
        internal static BoilerPanelOptions Options
        {
            get
            {
                if (_options == null)
                {
                    _options = Load();
                }

                return _options!;
            }
        }

        private static BoilerPanelOptions Load()
        {
            try
            {
                string? json = ReadConfigJson();
                SeeConfig config = SeeConfigCodec.Parse(json ?? string.Empty, null);
                return BoilerPanelOptions.FromConfig(config);
            }
            catch (Exception)
            {
                // A broken install must not break the panel (REQ-SAFE-01).
                return BoilerPanelOptions.Default;
            }
        }

        private static string? ReadConfigJson()
        {
            string? path = FindConfigPath();
            return string.IsNullOrEmpty(path) ? null : File.ReadAllText(path);
        }

        private static string? FindConfigPath()
        {
            string? besideAssembly = TryFindConfigBesideAssembly();
            if (!string.IsNullOrEmpty(besideAssembly))
            {
                return besideAssembly;
            }

            return TryFindConfigInDocuments();
        }

        private static string? TryFindConfigBesideAssembly()
        {
            try
            {
                string location = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(location))
                {
                    return null;
                }

                string? folder = Path.GetDirectoryName(location);
                if (string.IsNullOrEmpty(folder))
                {
                    return null;
                }

                string candidate = Path.Combine(folder, SEEConst.ConfigFileName);
                return File.Exists(candidate) ? candidate : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string? TryFindConfigInDocuments()
        {
            try
            {
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (string.IsNullOrEmpty(documents))
                {
                    return null;
                }

                string candidate = Path.Combine(documents, "Timberborn", "Mods", SEEConst.ModId, SEEConst.ConfigFileName);
                return File.Exists(candidate) ? candidate : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
