using System;
using System.Collections.Generic;

namespace SEE.Core.Logging
{
    /// <summary>In-memory logger used by unit tests and offline tools.</summary>
    public sealed class RecordingLog : ISeeLog
    {
        private readonly List<string> _entries = new List<string>();

        /// <summary>Captured entries in "<c>level|key|message</c>" form.</summary>
        public IReadOnlyList<string> Entries => _entries;

        /// <inheritdoc />
        public void Info(string key, string message) => _entries.Add("Info|" + key + "|" + message);

        /// <inheritdoc />
        public void Warn(string key, string message) => _entries.Add("Warn|" + key + "|" + message);

        /// <inheritdoc />
        public void Error(string key, string message) => _entries.Add("Error|" + key + "|" + message);

        /// <inheritdoc />
        public void Compat(string key, string message) => _entries.Add("Compat|" + key + "|" + message);
    }
}
