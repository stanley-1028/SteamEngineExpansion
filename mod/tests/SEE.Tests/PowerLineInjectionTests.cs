using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SEE.Core.Json;
using SEE.Integration.Adapters;
using SEE.Integration.Detection;
using SEE.Integration.Patching;

namespace SEE.Tests
{
    /// <summary>
    /// Non-Harmony Power Lines injection (requirements §10.4/§8.7, REQ-POWER-16): the JSON fragment
    /// shape and coordinates, the blueprint-path filter, the config/detection gate and the
    /// fail-closed optional patch host. Also proves the shipped main blueprints never contain
    /// <c>PowerLineSpec</c>.
    /// </summary>
    internal static class PowerLineInjectionTests
    {
        private static readonly string[] Sizes = { "Small", "Medium", "Large" };
        private static readonly string[] Tiers = { "T1", "T2", "T3", "T4" };
        private static readonly string[] Factions = { "Folktails", "IronTeeth" };

        internal static void Run(TestRunner t)
        {
            PathMatchingTests(t);
            FragmentShapeTests(t);
            GateTests(t);
            PatchHostTests(t);
            MainBlueprintTests(t);
        }

        private static void PathMatchingTests(TestRunner t)
        {
            t.Run("PowerLine fragment: all 24 boiler paths are recognised", () =>
            {
                foreach (string size in Sizes)
                {
                    foreach (string tier in Tiers)
                    {
                        foreach (string faction in Factions)
                        {
                            string path = "Buildings/Power/SteamBoiler/SteamBoiler." + size + "." + tier + "." + faction;
                            Assert.True(
                                PowerLineInjectionModifier.TryBuildForPath(path, out string json),
                                "path recognised: " + path);
                            Assert.Contains(json, "\"PowerLineSpec\"", "spec key");
                        }
                    }
                }
            });

            t.Run("PowerLine fragment: non-boiler and malformed paths are ignored", () =>
            {
                string[] ignored =
                {
                    "Buildings/Power/SteamEngine/SteamEngine.IronTeeth",
                    "Buildings/Power/SteamBoiler/OtherBuilding",
                    "Buildings/Storage/SmallTank/SmallTank.Folktails",
                    "",
                    "Buildings/Power/SteamBoiler/SteamBoiler.Small.T2"
                };
                foreach (string path in ignored)
                {
                    Assert.False(PowerLineInjectionModifier.TryBuildForPath(path, out _), "ignored: " + path);
                }
            });
        }

        private static void FragmentShapeTests(TestRunner t)
        {
            t.Run("PowerLine fragment: three connection points per size with requirement coordinates", () =>
            {
                AssertFragment(2, new[] { 0.0, 1.0, 0.5 }, new[] { 2.0, 1.0, 0.5 }, new[] { 1.0, 2.0, 0.5 });
                AssertFragment(3, new[] { 0.0, 1.5, 0.5 }, new[] { 3.0, 1.5, 0.5 }, new[] { 1.5, 3.0, 0.5 });
                AssertFragment(4, new[] { 0.0, 2.0, 0.5 }, new[] { 4.0, 2.0, 0.5 }, new[] { 2.0, 4.0, 0.5 });
            });

            t.Run("PowerLine fragment: defaults are the vanilla generator values", () =>
            {
                object? tree = MiniJson.Parse(PowerLineInjectionModifier.BuildModifierJson(3));
                var root = (IDictionary<string, object?>)tree!;
                var spec = (IDictionary<string, object?>)root["PowerLineSpec"]!;
                Assert.Equal((double)PowerLineInjectionModifier.DefaultMaxConnections, spec["MaxConnections"], "max connections");
                Assert.Equal((double)PowerLineInjectionModifier.DefaultMaxConnectionLength, spec["MaxConnectionLength"], "max length");
            });

            t.Run("PowerLine fragment: invalid size is rejected", () =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => PowerLineInjectionModifier.BuildModifierJson(5), "size 5");
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => PowerLineInjectionModifier.BuildModifierJson(1), "size 1");
            });
        }

        private static void GateTests(TestRunner t)
        {
            t.Run("PowerLine gate: injection needs the config switch and an available detection", () =>
            {
                Assert.True(PowerLineInjectionModifier.ShouldInject(true, Availability.Available), "both on");
                Assert.False(PowerLineInjectionModifier.ShouldInject(false, Availability.Available), "config off");
                Assert.False(PowerLineInjectionModifier.ShouldInject(true, Availability.Degraded), "degraded");
                Assert.False(PowerLineInjectionModifier.ShouldInject(true, Availability.Absent), "absent");
            });
        }

        private static void PatchHostTests(TestRunner t)
        {
            t.Run("Patch host: never opens when a non-Harmony path is proven (v1)", () =>
            {
                var host = new OptionalPatchHost();
                CompatRig rig = new CompatRig(
                    powerLines: Statuses.Available(CompatAnchors.PowerLines)).Initialize();
                bool enabled = host.TryEnable(rig.Detector, configEnabled: true, nonHarmonyInjectionProven: true);
                Assert.False(enabled, "patch host stays closed");
                Assert.False(host.Enabled, "enabled flag");
                Assert.Equal(OptionalPatchHost.NotRequiredReason, host.State, "state");
                Assert.True(host.Attempted, "attempted");
            });

            t.Run("Patch host: single attempt is never retried", () =>
            {
                var host = new OptionalPatchHost();
                CompatRig rig = new CompatRig().Initialize();
                host.TryEnable(rig.Detector, configEnabled: false, nonHarmonyInjectionProven: true);
                Assert.Equal(OptionalPatchHost.ConfigDisabledReason, host.State, "first attempt");
                host.TryEnable(rig.Detector, configEnabled: true, nonHarmonyInjectionProven: false);
                Assert.Equal(OptionalPatchHost.ConfigDisabledReason, host.State, "second call is a no-op");
            });

            t.Run("Patch host: Power Lines absent keeps it closed", () =>
            {
                var host = new OptionalPatchHost();
                CompatRig rig = new CompatRig().Initialize();
                host.TryEnable(rig.Detector, configEnabled: true, nonHarmonyInjectionProven: false);
                Assert.Equal(OptionalPatchHost.PowerLinesAbsentReason, host.State, "state");
            });

            t.Run("Patch host: a null detector cannot throw", () =>
            {
                var host = new OptionalPatchHost();
                Assert.False(host.TryEnable(null, true, true), "returns false");
                Assert.Equal(OptionalPatchHost.PowerLinesAbsentReason, host.State, "state");
            });
        }

        private static void MainBlueprintTests(TestRunner t)
        {
            t.Run("PowerLine spec is absent from the 24 shipped main blueprints", () =>
            {
                string folder = TestPaths.FromMod("data", "Buildings", "Power", "SteamBoiler");
                Assert.True(Directory.Exists(folder), "generated blueprint folder exists");
                string[] files = Directory.GetFiles(folder, "*.blueprint.json", SearchOption.TopDirectoryOnly);
                Assert.Equal(24, files.Length, "blueprint count");
                foreach (string file in files)
                {
                    string text = File.ReadAllText(file);
                    Assert.False(text.Contains("PowerLineSpec", StringComparison.Ordinal),
                        "no PowerLineSpec in " + Path.GetFileName(file));
                }
            });

            t.Run("PowerLine fragment is valid JSON with no external type names", () =>
            {
                string json = PowerLineInjectionModifier.BuildModifierJson(3);
                Assert.NotNull(MiniJson.Parse(json), "parses");
                Assert.False(Regex.IsMatch(json, "PowerLines\\."), "no external namespace in fragment");
            });
        }

        private static void AssertFragment(int size, double[] left, double[] right, double[] back)
        {
            object? tree = MiniJson.Parse(PowerLineInjectionModifier.BuildModifierJson(size));
            var root = (IDictionary<string, object?>)tree!;
            var spec = (IDictionary<string, object?>)root["PowerLineSpec"]!;
            var points = (IList<object?>)spec["ConnectionLocations"]!;
            Assert.Equal(3, points.Count, "point count");
            AssertPoint((IDictionary<string, object?>)points[0]!, left, "left");
            AssertPoint((IDictionary<string, object?>)points[1]!, right, "right");
            AssertPoint((IDictionary<string, object?>)points[2]!, back, "back");
        }

        private static void AssertPoint(IDictionary<string, object?> point, double[] expected, string label)
        {
            Assert.Equal(expected[0], point["X"], label + " X");
            Assert.Equal(expected[1], point["Y"], label + " Y");
            Assert.Equal(expected[2], point["Z"], label + " Z");
        }
    }
}
