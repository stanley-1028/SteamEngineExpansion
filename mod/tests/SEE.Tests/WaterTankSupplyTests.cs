using System;
using System.IO;

namespace SEE.Tests
{
    /// <summary>
    /// Regression contract for the 2026-11-08 hauling bug fix: ordinary Hauling-Post haulers must be
    /// able to carry water and fuel into the steam boilers.
    ///
    /// The blueprint side of the contract lives in <see cref="BlueprintContractTests"/> (fuel is the
    /// only GoodConsuming good, the tank size stays on the boiler spec). This suite locks the code
    /// side, which cannot be instantiated offline:
    /// <list type="bullet">
    /// <item>the water tank is a dedicated dedicated-decorator inventory created by
    /// <c>SEEWaterInventoryInitializer</c> and declared as a public input (the vanilla hauler
    /// destination declaration);</item>
    /// <item>the boiler template module registers that initializer (with the game's
    /// <c>InventoryInitializerFactory</c>), not only the controller;</item>
    /// <item>the controller resolves the inventory from <c>Inventories.AllInventories</c>, enables it
    /// on finish / disables it on revert, and withdraws water with <c>TakeExisting</c> (the good is
    /// giveable-and-takeable so haulers can deliver it);</item>
    /// <item>SEEGameplay no longer depends on the crashed Stockpile path.</item>
    /// </list>
    /// Source-text scans are used because the gameplay assembly cannot run without the game; the
    /// same style is already used by the release-gate static checks.
    /// </summary>
    internal static class WaterTankSupplyTests
    {
        internal static void Run(TestRunner t)
        {
            InitializerTests(t);
            ConfiguratorTests(t);
            ControllerTests(t);
            DependencyTests(t);
        }

        private static void InitializerTests(TestRunner t)
        {
            t.Run("Water tank initializer is a dedicated public input inventory fed to haulers", () =>
            {
                string source = Read("src", "SEE.Gameplay", "Components", "SEEWaterInventoryInitializer.cs");
                Contains(source, "IDedicatedDecoratorInitializer<SEEBoilerSpec, Inventory>", "initializer interface");
                Contains(source, "public const string InventoryComponentName = \"WaterTank\"", "stable component name");
                Contains(source, "StorableGood.CreateGiveableAndTakeable(GoodIds.Water)", "haulable/takeable water good");
                Contains(source, "initializer.HasPublicInput()", "public input declaration");
                Contains(source, "spec.WaterCapacityGoods", "capacity comes from the tabulated boiler spec");
                Contains(source, "initializer.Initialize()", "inventory initialization call");
            });

            t.Run("Water tank capacity degrades safely to at least one good", () =>
            {
                string source = Read("src", "SEE.Gameplay", "Components", "SEEWaterInventoryInitializer.cs");
                Contains(source, "Math.Max(1,", "capacity floor");
                Contains(source, "spec == null ? 0 : spec.WaterCapacityGoods", "capacity never becomes zero");
            });
        }

        private static void ConfiguratorTests(TestRunner t)
        {
            t.Run("Boiler template module registers the dedicated water inventory", () =>
            {
                string source = Read("src", "SEE.Gameplay", "SeeGameConfigurator.cs");
                Contains(source, "IProvider<TemplateModule>", "configurator-owned template module provider");
                Contains(source, "InventoryInitializerFactory", "game inventory factory injection");
                Contains(source, "AddDedicatedDecorator(new SEEWaterInventoryInitializer(", "dedicated water inventory decorator");
                Contains(source, "AddDecorator<SEEBoilerSpec, SEEBoilerController>()", "controller decorator kept");
            });
        }

        private static void ControllerTests(TestRunner t)
        {
            t.Run("Controller finds the tank among all inventories, not just the enabled ones", () =>
            {
                string source = Read("src", "SEE.Gameplay", "Components", "SEEBoilerController.WaterTank.cs");
                Contains(source, "GetComponent<Inventories>().AllInventories", "all-inventory scan");
                Contains(source, "TryResolveWaterInventory()", "resolution entry point");
                Contains(source, "SEEWaterInventoryInitializer.InventoryComponentName", "named-component lookup");
            });

            t.Run("Controller owns the tank lifecycle (enable on finish, disable on revert)", () =>
            {
                string source = Read("src", "SEE.Gameplay", "Components", "SEEBoilerController.WaterTank.cs");
                Contains(source, "inventory.Enable()", "enable call");
                Contains(source, "inventory.Disable()", "disable call");
                string lifecycle = Read("src", "SEE.Gameplay", "Components", "SEEBoilerController.Lifecycle.cs");
                Contains(lifecycle, "EnableWaterInventory();", "enable on activation");
                Contains(lifecycle, "DisableWaterInventory();", "disable on deactivation");
            });

            t.Run("Water is withdrawn with TakeExisting (giveable-and-takeable tank good)", () =>
            {
                string source = Read("src", "SEE.Gameplay", "Components", "SEEBoilerController.Consumption.cs");
                Contains(source, "water.TakeExisting(new GoodAmount(GoodIds.Water, amount));", "water take");
                Contains(source, "fuel.TakeConsumed(new GoodAmount(goodId, amount));", "fuel stays a consumer good");
            });

            t.Run("Gameplay no longer uses the crashed Stockpile/SingleGoodAllower path", () =>
            {
                string root = TestPaths.FromMod("src", "SEE.Gameplay");
                foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                        || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string source = File.ReadAllText(file);
                    Assert.False(source.Contains("using Timberborn.Stockpiles;", StringComparison.Ordinal),
                        Path.GetFileName(file) + " must not reference Timberborn.Stockpiles");
                    Assert.False(source.Contains("SingleGoodAllower", StringComparison.Ordinal),
                        Path.GetFileName(file) + " must not use SingleGoodAllower");
                }
            });
        }

        private static void DependencyTests(TestRunner t)
        {
            t.Run("SEE.Gameplay no longer references the Stockpiles game assembly", () =>
            {
                string csproj = Read("src", "SEE.Gameplay", "SEE.Gameplay.csproj");
                Assert.False(csproj.Contains("<Reference Include=\"Timberborn.Stockpiles\">", StringComparison.Ordinal),
                    "SEE.Gameplay.csproj must not reference the Timberborn.Stockpiles assembly");
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
