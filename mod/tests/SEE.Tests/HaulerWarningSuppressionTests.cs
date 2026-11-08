using System;
using System.IO;

namespace SEE.Tests
{
    /// <summary>
    /// Regression contract for the 2026-11-08 gameplay fix: the vanilla "requires haulers" status
    /// (loc key <c>Status.Hauling.NoHaulingPost</c>, enUS "Requires haulers to function.") must never
    /// appear on a finished steam boiler.
    ///
    /// Root cause: every building whose blueprint has a <c>GoodConsumingBuildingSpec</c> receives
    /// the vanilla <c>GoodConsumingBuildingStatusInitializer</c>, whose <c>OnEnterFinishedState</c>
    /// initialises <c>NoHaulingPostStatus</c> when the building has no <c>Workplace</c>. The boiler
    /// is deliberately worker-free, so the status was shown even though belts and ordinary haulers
    /// are both valid fuel deliveries. The controller disables the status through the public
    /// <c>NoHaulingPostStatus.Disable()</c> opt-out (no Harmony, no reflection) and re-disables it
    /// after a district reassignment (the only remaining re-activation path), deferred to the next
    /// settlement tick because listener ordering at building finish is not guaranteed.
    ///
    /// Source-text scans are used because the gameplay assembly cannot run without the game; the
    /// same style is already used by <see cref="WaterTankSupplyTests"/> and the release gate.
    /// </summary>
    internal static class HaulerWarningSuppressionTests
    {
        internal static void Run(TestRunner t)
        {
            SuppressionSourceTests(t);
            LifecycleAndTickTests(t);
            DependencyTests(t);
        }

        private static void SuppressionSourceTests(TestRunner t)
        {
            t.Run("Controller suppresses the vanilla hauler-requirement status", () =>
            {
                string source = Read("src", "SEE.Gameplay", "Components", "SEEBoilerController.HaulerStatus.cs");
                Contains(source, "NoHaulingPostStatus", "vanilla status component");
                Contains(source, "_noHaulingPostStatus.Disable()", "vanilla public opt-out call");
                Contains(source, "ReassignedDistrict", "district reassignment re-activation path");
                Contains(source, "ReleaseHaulerStatusSuppression", "cleanup entry point");
            });

            t.Run("Suppression is deferred and exception-isolated", () =>
            {
                string source = Read("src", "SEE.Gameplay", "Components", "SEEBoilerController.HaulerStatus.cs");
                Contains(source, "HaulerStatusSuppressionTicks", "deferred re-suppression window");
                Contains(source, "ApplyHaulerStatusSuppression", "settlement-tick disable entry point");
                Contains(source, "ResolveHaulerStatusSuppression", "component resolution entry point");
                Contains(source, "OnHaulerStatusDistrictReassigned", "district reassignment handler");
                Contains(source, "catch (Exception", "REQ-SAFE-02 style exception isolation");
                Contains(source, "LogWarn(", "one diagnostic on failure");
            });
        }

        private static void LifecycleAndTickTests(TestRunner t)
        {
            t.Run("Lifecycle resolves on activation and releases on deactivation", () =>
            {
                string source = Read("src", "SEE.Gameplay", "Components", "SEEBoilerController.Lifecycle.cs");
                Contains(source, "TryResolveComponents();", "component resolution call");
                Contains(source, "ResolveHaulerStatusSuppression();", "arm the suppression on activation");
                Contains(source, "ReleaseHaulerStatusSuppression();", "drop the subscription on deactivation");
            });

            t.Run("Settlement tick applies the deferred suppression", () =>
            {
                string source = Read("src", "SEE.Gameplay", "Components", "SEEBoilerController.Tick.cs");
                Contains(source, "ResolveHaulerStatusSuppression();", "re-resolve on the settlement tick");
                Contains(source, "ApplyHaulerStatusSuppression();", "deferred disable on the settlement tick");
            });
        }

        private static void DependencyTests(TestRunner t)
        {
            t.Run("Gameplay references the hauling game assembly for the public opt-out", () =>
            {
                string csproj = Read("src", "SEE.Gameplay", "SEE.Gameplay.csproj");
                Contains(csproj, "<Reference Include=\"Timberborn.Hauling\">", "hauling assembly reference");
                Contains(csproj, "Timberborn.Hauling.dll", "hauling assembly hint path");
            });

            t.Run("Hauler status fix never uses Harmony", () =>
            {
                // The fix must use the vanilla public opt-out, not runtime code patching. Only the
                // implementation files touched by the fix are scanned (this test names the token
                // itself, so scanning the test sources would be self-referential).
                string[] sources =
                {
                    Read("src", "SEE.Gameplay", "Components", "SEEBoilerController.HaulerStatus.cs"),
                    Read("src", "SEE.Gameplay", "Components", "SEEBoilerController.Lifecycle.cs"),
                    Read("src", "SEE.Gameplay", "Components", "SEEBoilerController.Tick.cs"),
                    Read("src", "SEE.Gameplay", "SEE.Gameplay.csproj"),
                };
                for (int index = 0; index < sources.Length; index++)
                {
                    Assert.False(sources[index].Contains("Harmony", StringComparison.Ordinal),
                        "hauler status fix must not use Harmony");
                }
            });
        }

        private static string Read(params string[] parts)
        {
            string path = TestPaths.FromMod(parts);
            Assert.True(File.Exists(path), "missing source file " + path);
            return File.ReadAllText(path);
        }

        private static void Contains(string source, string fragment, string what)
        {
            Assert.True(source.Contains(fragment, StringComparison.Ordinal),
                "expected " + what + ": '" + fragment + "'");
        }
    }
}
