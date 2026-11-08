using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SEE.Tests
{
    /// <summary>
    /// REQ-ARCH-02 layering gate: reads the ACTUAL built shipping assemblies
    /// (<c>mod/src/&lt;Assembly&gt;/bin/Release/netstandard2.1/&lt;Assembly&gt;.dll</c>) and asserts the
    /// dependency directions with <see cref="Assembly.GetReferencedAssemblies()"/>.
    ///
    /// The test loads the DLL bytes only to read metadata; it never references a Gameplay/UI/
    /// Integration type, and the game assemblies the DLLs reference are never resolved. If the
    /// shipping assemblies have not been built yet the test fails with the build command.
    /// </summary>
    internal static class LayerRulesTests
    {
        private static readonly string[] ShippingAssemblies = { "SEE.Core", "SEE.Gameplay", "SEE.Integration", "SEE.UI" };

        internal static void Run(TestRunner t)
        {
            BuildPresenceTests(t);
            StaleBuildTests(t);
            CoreRules(t);
            GameplayRules(t);
            UiRules(t);
            IntegrationRules(t);
            ExternalReferenceRules(t);
        }

        private static void BuildPresenceTests(TestRunner t)
        {
            t.Run("All four shipping assemblies are built (no SEE.Tests.dll in the shipping set)", () =>
            {
                foreach (var assembly in ShippingAssemblies)
                {
                    Assert.True(File.Exists(FindDll(assembly)),
                        "missing " + assembly + ".dll; build first: pwsh -File mod/build.ps1 -Clean");
                }

                var testDll = Path.Combine(ModRoot(), "src", "SEE.Tests", "bin", "Release", "netstandard2.1", "SEE.Tests.dll");
                Assert.False(File.Exists(testDll), "SEE.Tests must not be built as a shipping assembly");
            });

            t.Run("Each DLL declares its own assembly name", () =>
            {
                foreach (var assembly in ShippingAssemblies)
                {
                    Assert.Equal(assembly, Load(assembly).GetName().Name, assembly + " assembly name");
                }
            });
        }

        private static void StaleBuildTests(TestRunner t)
        {
            t.Run("The built DLLs are newer than their sources (no stale artifacts)", () =>
            {
                foreach (var assembly in ShippingAssemblies)
                {
                    var dll = FindDll(assembly);
                    Assert.True(File.Exists(dll), "missing " + assembly + ".dll; build first");

                    var sourceRoot = Path.Combine(ModRoot(), "src", assembly);
                    var newestSource = NewestSourceTimestamp(sourceRoot);
                    var builtAt = File.GetLastWriteTimeUtc(dll);
                    Assert.True(builtAt >= newestSource,
                        assembly + ".dll is older than its sources; re-run 'pwsh -File mod/build.ps1 -Clean' "
                        + "(built " + builtAt.ToString("O") + ", newest source " + newestSource.ToString("O") + ")");
                }
            });
        }

        private static DateTime NewestSourceTimestamp(string sourceRoot)
        {
            var newest = DateTime.MinValue;
            foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                {
                    continue;
                }

                var timestamp = File.GetLastWriteTimeUtc(file);
                if (timestamp > newest)
                {
                    newest = timestamp;
                }
            }

            return newest;
        }

        private static void CoreRules(TestRunner t)
        {
            t.Run("SEE.Core references no other SEE assembly", () =>
            {
                foreach (var reference in References("SEE.Core"))
                {
                    Assert.False(reference.StartsWith("SEE.", StringComparison.Ordinal),
                        "SEE.Core must not reference " + reference);
                }
            });

            t.Run("SEE.Core references no Timberborn/UnityEngine/Bindito assembly", () =>
            {
                foreach (var reference in References("SEE.Core"))
                {
                    Assert.False(IsGameAssembly(reference),
                        "SEE.Core must stay game-independent, found " + reference);
                }
            });

            t.Run("SEE.Core references only the base framework", () =>
            {
                foreach (var reference in References("SEE.Core"))
                {
                    var framework = reference.Equals("mscorlib", StringComparison.Ordinal)
                                    || reference.Equals("netstandard", StringComparison.Ordinal)
                                    || reference.StartsWith("System", StringComparison.Ordinal);
                    Assert.True(framework, "SEE.Core has an unexpected dependency: " + reference);
                }
            });
        }

        private static void GameplayRules(TestRunner t)
        {
            t.Run("SEE.Gameplay references SEE.Core but not SEE.Integration/SEE.UI", () =>
            {
                var references = References("SEE.Gameplay");
                Assert.True(references.Contains("SEE.Core", StringComparer.Ordinal), "Gameplay must reference Core");
                Assert.False(references.Contains("SEE.Integration", StringComparer.Ordinal),
                    "Gameplay must not reference SEE.Integration");
                Assert.False(references.Contains("SEE.UI", StringComparer.Ordinal), "Gameplay must not reference SEE.UI");
            });
        }

        private static void UiRules(TestRunner t)
        {
            t.Run("SEE.UI references SEE.Core but not SEE.Gameplay/SEE.Integration", () =>
            {
                var references = References("SEE.UI");
                Assert.True(references.Contains("SEE.Core", StringComparer.Ordinal), "UI must reference Core");
                Assert.False(references.Contains("SEE.Gameplay", StringComparer.Ordinal),
                    "UI must not reference SEE.Gameplay");
                Assert.False(references.Contains("SEE.Integration", StringComparer.Ordinal),
                    "UI must not reference SEE.Integration");
            });
        }

        private static void IntegrationRules(TestRunner t)
        {
            t.Run("SEE.Integration references Core+Gameplay (R-1) but not SEE.UI", () =>
            {
                var references = References("SEE.Integration");
                Assert.True(references.Contains("SEE.Core", StringComparer.Ordinal), "Integration must reference Core");
                Assert.True(references.Contains("SEE.Gameplay", StringComparer.Ordinal),
                    "Integration must reference Gameplay (R-1 save bridge/config seam)");
                Assert.False(references.Contains("SEE.UI", StringComparer.Ordinal),
                    "Integration must not reference SEE.UI");
            });
        }

        private static void ExternalReferenceRules(TestRunner t)
        {
            t.Run("No shipping assembly references an external mod DLL or Harmony", () =>
            {
                string[] forbidden = { "TimberPipes", "PowerLines", "ConveyorBelt", "ConveyorBelts", "Harmony" };
                foreach (var assembly in ShippingAssemblies)
                {
                    foreach (var reference in References(assembly))
                    {
                        foreach (var external in forbidden)
                        {
                            Assert.False(reference.StartsWith(external, StringComparison.OrdinalIgnoreCase),
                                assembly + " must not reference the external assembly " + reference);
                        }
                    }
                }
            });

            t.Run("No shipping assembly references the test runner or a tool assembly", () =>
            {
                foreach (var assembly in ShippingAssemblies)
                {
                    foreach (var reference in References(assembly))
                    {
                        Assert.False(reference.StartsWith("SEE.Tests", StringComparison.OrdinalIgnoreCase),
                            assembly + " must not reference the test runner");
                        Assert.False(reference.Equals("SeeValidate", StringComparison.OrdinalIgnoreCase)
                                     || reference.StartsWith("ApiIndex", StringComparison.OrdinalIgnoreCase),
                            assembly + " must not reference a build tool");
                    }
                }
            });
        }

        private static bool IsGameAssembly(string reference) =>
            reference.StartsWith("Timberborn.", StringComparison.Ordinal)
            || reference.StartsWith("UnityEngine", StringComparison.Ordinal)
            || reference.StartsWith("Bindito.", StringComparison.Ordinal);

        /// <summary>Reference names of one built shipping assembly.</summary>
        private static string[] References(string assemblyName) =>
            Load(assemblyName)
                .GetReferencedAssemblies()
                .Select(name => name.Name ?? string.Empty)
                .ToArray();

        private static Assembly Load(string assemblyName)
        {
            var path = FindDll(assemblyName);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("missing " + assemblyName + ".dll; run 'pwsh -File mod/build.ps1 -Clean' first");
            }

            // Load from bytes: metadata-only inspection, no game dependency resolution, no code execution.
            return Assembly.Load(File.ReadAllBytes(path));
        }

        private static string FindDll(string assemblyName)
        {
            var root = ModRoot();
            foreach (var configuration in new[] { "Release", "Debug" })
            {
                var candidate = Path.Combine(root, "src", assemblyName, "bin", configuration, "netstandard2.1",
                    assemblyName + ".dll");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine(root, "src", assemblyName, "bin", "Release", "netstandard2.1", assemblyName + ".dll");
        }

        /// <summary>Walks up from the test output folder until the mod root (src/SEE.Core + tests) is found.</summary>
        private static string ModRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            for (var depth = 0; depth < 10 && directory is not null; depth++)
            {
                if (File.Exists(Path.Combine(directory.FullName, "src", "SEE.Core", "SEE.Core.csproj"))
                    && Directory.Exists(Path.Combine(directory.FullName, "tests")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                "could not locate the mod root from " + AppContext.BaseDirectory + "; run the tests from mod/tests/SEE.Tests");
        }
    }
}
