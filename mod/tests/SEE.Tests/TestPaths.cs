using System;
using System.IO;

namespace SEE.Tests
{
    /// <summary>
    /// Resolves workspace paths for the offline contract tests. The runner walks up from its output
    /// folder until the mod root is found, so it works from <c>bin/Release/net9.0</c> as well as from
    /// a test runner invoked with an explicit working directory.
    /// </summary>
    internal static class TestPaths
    {
        private static string? _modRoot;

        /// <summary>Absolute path of the <c>mod</c> directory.</summary>
        internal static string ModRoot()
        {
            if (_modRoot != null)
            {
                return _modRoot;
            }

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            for (var depth = 0; depth < 10 && directory != null; depth++)
            {
                if (File.Exists(Path.Combine(directory.FullName, "src", "SEE.Core", "SEE.Core.csproj"))
                    && Directory.Exists(Path.Combine(directory.FullName, "tests")))
                {
                    _modRoot = directory.FullName;
                    return _modRoot;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                "could not locate the mod root from " + AppContext.BaseDirectory + "; run the tests from mod/tests/SEE.Tests");
        }

        /// <summary>Absolute path of one file below the mod root.</summary>
        internal static string FromMod(params string[] parts)
        {
            string path = ModRoot();
            for (int i = 0; i < parts.Length; i++)
            {
                path = Path.Combine(path, parts[i]);
            }

            return path;
        }

        /// <summary>Absolute path of one file below <c>mod/data</c>.</summary>
        internal static string DataFile(string relativePath)
        {
            return Path.Combine(ModRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
