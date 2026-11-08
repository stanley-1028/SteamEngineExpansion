using System;
using System.IO;
using System.Reflection;

namespace SEE.Tests
{
    /// <summary>
    /// Loads the built SEE.Gameplay assembly from bytes (metadata only, no game assemblies resolved)
    /// and reflects one pure type. Used by the offline tests to cover the gameplay consumption
    /// accounting without making the test project reference the game-facing assembly.
    /// </summary>
    internal static class GameplayProbe
    {
        private static Assembly? _assembly;

        internal static Assembly Assembly()
        {
            if (_assembly != null)
            {
                return _assembly;
            }

            string path = TestPaths.FromMod(
                "src", "SEE.Gameplay", "bin", "Release", "netstandard2.1", "SEE.Gameplay.dll");
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    "SEE.Gameplay.dll not built; run 'dotnet build mod/src/SEE.Gameplay -c Release -m:1' first");
            }

            _assembly = System.Reflection.Assembly.Load(File.ReadAllBytes(path));
            return _assembly;
        }

        internal static Type FindType(string fullName)
        {
            Type? type = Assembly().GetType(fullName, throwOnError: false);
            if (type == null)
            {
                throw new InvalidOperationException("type not found in SEE.Gameplay: " + fullName);
            }

            return type;
        }
    }
}
