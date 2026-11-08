using System;
using System.Collections.Generic;
using System.IO;
using SEE.Core.Json;

namespace SEE.Tests
{
    /// <summary>
    /// L2 blueprint contract tests (CONTRACT_SEE §3/§4, requirements §4.1/§7/§8/§13.3): the 24
    /// generated blueprints must carry the frozen SEE specs with exactly the contract fields, the
    /// values must equal the single data source (<c>engines.derived.json</c>), the fuel consumer
    /// must not swallow the water tank (bug fix 2026-11-08: the tank is a dedicated public input
    /// inventory created by <c>SEEWaterInventoryInitializer</c>) and no forbidden spec
    /// (AD-9 / red lines) may appear. These tests read the shipped JSON only; they never need the
    /// game or the gameplay assembly.
    /// </summary>
    internal static class BlueprintContractTests
    {
        private static readonly string[] BoilerSpecFields =
        {
            "AutoMaintenanceDefault", "Faction", "FuelCapacityGoods", "FuelGoodIds", "FuelPerHour",
            "FuelScaling", "FuelValues", "Horsepower", "Size", "Tier", "WaterCapacityGoods", "WaterPerHour"
        };

        private static readonly string[] MaintenanceSpecFields =
        {
            "DurationHours", "Enabled", "IntervalDays", "LoadFactorRange", "LoadFloor", "Materials",
            "TriggerThreshold"
        };

        private static readonly string[] ForbiddenSpecs =
        {
            "WorkplaceSpec", "GoodPoweredGeneratorSpec", "BuildingPipeSpec",
            "BuildingPipeTargetPortsSpec", "PowerLineSpec",
            // v0.1.1 hotfix: Stockpile + GoodConsumingBuilding double-initialises
            // NoHaulingPostStatus and crashes the construction finish.
            "StockpileSpec"
        };

        internal static void Run(TestRunner t)
        {
            List<Variant> variants = LoadVariants();
            BlueprintInventoryTests(t, variants);
            SpecShapeTests(t, variants);
            SpecValueTests(t, variants);
            NodeAndInventoryTests(t, variants);
            NamingTests(t, variants);
        }

        private static void BlueprintInventoryTests(TestRunner t, List<Variant> variants)
        {
            t.Run("24 boiler blueprints ship and every derived variant resolves to one file", () =>
            {
                string folder = TestPaths.FromMod("data", "Buildings", "Power", "SteamBoiler");
                string[] files = Directory.GetFiles(folder, "SteamBoiler.*.blueprint.json");
                Assert.Equal(24, files.Length, "blueprint file count");
                Assert.Equal(24, variants.Count, "derived variant count");
                var stems = new HashSet<string>(StringComparer.Ordinal);
                foreach (Variant variant in variants)
                {
                    Assert.True(File.Exists(variant.FilePath), "missing blueprint " + variant.FilePath);
                    Assert.True(stems.Add(variant.FileStem), "duplicate blueprint " + variant.FileStem);
                }
            });

            t.Run("Every blueprint declares both frozen SEE specs", () =>
            {
                foreach (Variant variant in variants)
                {
                    Assert.True(variant.Blueprint.ContainsKey("SEE.SEEBoilerSpec"),
                        variant.FileStem + " has SEE.SEEBoilerSpec");
                    Assert.True(variant.Blueprint.ContainsKey("SEE.SEEMaintenanceSpec"),
                        variant.FileStem + " has SEE.SEEMaintenanceSpec");
                }
            });
        }

        private static void SpecShapeTests(TestRunner t, List<Variant> variants)
        {
            t.Run("SEEBoilerSpec has exactly the twelve contract fields", () =>
            {
                foreach (Variant variant in variants)
                {
                    var spec = MapOf(variant.Blueprint["SEE.SEEBoilerSpec"]);
                    AssertFieldSet(BoilerSpecFields, spec, variant.FileStem + " boiler spec");
                }
            });

            t.Run("SEEMaintenanceSpec has exactly the seven contract fields", () =>
            {
                foreach (Variant variant in variants)
                {
                    var spec = MapOf(variant.Blueprint["SEE.SEEMaintenanceSpec"]);
                    AssertFieldSet(MaintenanceSpecFields, spec, variant.FileStem + " maintenance spec");
                }
            });

            t.Run("No forbidden spec or red-line declaration appears in a blueprint", () =>
            {
                foreach (Variant variant in variants)
                {
                    foreach (string forbidden in ForbiddenSpecs)
                    {
                        Assert.False(variant.Blueprint.ContainsKey(forbidden),
                            variant.FileStem + " must not declare " + forbidden);
                    }
                }
            });

            t.Run("Every blueprint keeps the mechanical node / player-entry specs", () =>
            {
                foreach (Variant variant in variants)
                {
                    foreach (string required in new[]
                             {
                                 "MechanicalNodeSpec", "MechanicalBuildingSpec", "GoodConsumingBuildingSpec",
                                 "EnterableSpec", "BuildingAccessibleSpec", "PlaceableBlockObjectSpec"
                             })
                    {
                        Assert.True(variant.Blueprint.ContainsKey(required),
                            variant.FileStem + " requires " + required);
                    }
                }
            });
        }

        private static void SpecValueTests(TestRunner t, List<Variant> variants)
        {
            t.Run("SEEBoilerSpec matches engines.derived.json for every size/tier/faction", () =>
            {
                foreach (Variant variant in variants)
                {
                    var spec = MapOf(variant.Blueprint["SEE.SEEBoilerSpec"]);
                    Assert.Equal(variant.Tier, GetString(spec, "Tier"), variant.FileStem + " Tier");
                    Assert.Equal(variant.Size, GetString(spec, "Size"), variant.FileStem + " Size");
                    Assert.Equal(variant.Faction, GetString(spec, "Faction"), variant.FileStem + " Faction");
                    Assert.Equal((int)GetNumber(variant.Data, "horsepower"), GetInt(spec, "Horsepower"),
                        variant.FileStem + " Horsepower");
                    Assert.Near(GetNumber(variant.Data, "fuelPerHour"), GetNumber(spec, "FuelPerHour"), 1e-4,
                        variant.FileStem + " FuelPerHour");
                    Assert.Near(GetNumber(variant.Data, "waterPerHour"), GetNumber(spec, "WaterPerHour"), 1e-4,
                        variant.FileStem + " WaterPerHour");
                    Assert.Equal((int)GetNumber(variant.Data, "fuelStock"), GetInt(spec, "FuelCapacityGoods"),
                        variant.FileStem + " FuelCapacityGoods");
                    Assert.Equal((int)GetNumber(variant.Data, "waterTankGoods"), GetInt(spec, "WaterCapacityGoods"),
                        variant.FileStem + " WaterCapacityGoods");
                    Assert.Equal("proportional", GetString(spec, "FuelScaling"), variant.FileStem + " FuelScaling");
                    Assert.True(GetBool(spec, "AutoMaintenanceDefault"), variant.FileStem + " AutoMaintenanceDefault");
                    AssertEqualSlice(StringArray(variant.Data, "fuelGoodIds"), StringArray(spec, "FuelGoodIds"),
                        variant.FileStem + " FuelGoodIds");
                    AssertEqualSlice(FloatArray(variant.Data, "fuelValues"), FloatArray(spec, "FuelValues"),
                        variant.FileStem + " FuelValues");
                }
            });

            t.Run("SEEMaintenanceSpec interval and materials match the engine row", () =>
            {
                foreach (Variant variant in variants)
                {
                    var spec = MapOf(variant.Blueprint["SEE.SEEMaintenanceSpec"]);
                    Assert.Near(GetNumber(variant.Row, "maintenanceIntervalDays"),
                        GetNumber(spec, "IntervalDays"), 1e-4, variant.FileStem + " IntervalDays");
                    Assert.Near(0.4, GetNumber(spec, "LoadFloor"), 1e-4, variant.FileStem + " LoadFloor");
                    Assert.Near(0.6, GetNumber(spec, "LoadFactorRange"), 1e-4, variant.FileStem + " LoadFactorRange");
                    Assert.Near(50.0, GetNumber(spec, "TriggerThreshold"), 1e-4, variant.FileStem + " TriggerThreshold");
                    Assert.Near(2.0, GetNumber(spec, "DurationHours"), 1e-4, variant.FileStem + " DurationHours");
                    Assert.True(GetBool(spec, "Enabled"), variant.FileStem + " Enabled");

                    List<object?> expected = ListOf(variant.Row["maintenanceMaterials"]);
                    List<object?> actual = ListOf(spec["Materials"]);
                    Assert.Equal(expected.Count, actual.Count, variant.FileStem + " material count");
                    for (int i = 0; i < expected.Count; i++)
                    {
                        var expectedMaterial = MapOf(expected[i]);
                        var actualMaterial = MapOf(actual[i]);
                        Assert.Equal(GetString(expectedMaterial, "good"), GetString(actualMaterial, "Id"),
                            variant.FileStem + " material " + i + " id");
                        Assert.Equal((int)GetNumber(expectedMaterial, "amount"), GetInt(actualMaterial, "Amount"),
                            variant.FileStem + " material " + i + " amount");
                    }
                }
            });

            t.Run("MechanicalNodeSpec equals the spec horsepower and the frozen generator shape", () =>
            {
                foreach (Variant variant in variants)
                {
                    var node = MapOf(variant.Blueprint["MechanicalNodeSpec"]);
                    var spec = MapOf(variant.Blueprint["SEE.SEEBoilerSpec"]);
                    Assert.Equal(GetInt(spec, "Horsepower"), GetInt(node, "PowerOutput"),
                        variant.FileStem + " PowerOutput equals Horsepower");
                    Assert.Equal(0, GetInt(node, "PowerInput"), variant.FileStem + " PowerInput");
                    Assert.False(GetBool(node, "IsShaft"), variant.FileStem + " IsShaft");
                }
            });
        }

        private static void NodeAndInventoryTests(TestRunner t, List<Variant> variants)
        {
            t.Run("Fuel is the only GoodConsuming good; water owns a dedicated inventory (fix 2026-11-08)", () =>
            {
                foreach (Variant variant in variants)
                {
                    var consumer = MapOf(variant.Blueprint["GoodConsumingBuildingSpec"]);
                    Assert.Equal((int)GetNumber(variant.Row, "fullInventoryWorkHours"),
                        GetInt(consumer, "FullInventoryWorkHours"), variant.FileStem + " FullInventoryWorkHours");

                    List<object?> goods = ListOf(consumer["ConsumedGoods"]);
                    Assert.True(goods.Count > 0, variant.FileStem + " has consumed goods");
                    foreach (object? entry in goods)
                    {
                        string goodId = GetString(MapOf(entry), "GoodId");
                        Assert.True(goodId == "Log" || goodId == "Plank",
                            variant.FileStem + " fuel good must be Log/Plank, was " + goodId);
                    }

                    // Bug fix 2026-11-08: the water tank lives in a dedicated public input
                    // inventory (SEEWaterInventoryInitializer), not in the fuel consumer. Keeping
                    // the tank size on the boiler spec is what lets the code-created inventory
                    // derive its capacity without a shared capacity.
                    Assert.False(BlueprintConsumesWater(variant.Blueprint),
                        variant.FileStem + " must not carry Water in ConsumedGoods");
                    Assert.True(GetInt(MapOf(variant.Blueprint["SEE.SEEBoilerSpec"]), "WaterCapacityGoods") > 0,
                        variant.FileStem + " has a positive WaterCapacityGoods for the dedicated tank");
                    Assert.False(variant.Blueprint.ContainsKey("StockpileSpec"),
                        variant.FileStem + " must not declare StockpileSpec");
                }
            });

            t.Run("Fuel capacity derivation matches the table cell (float32 ceil)", () =>
            {
                foreach (Variant variant in variants)
                {
                    var consumer = MapOf(variant.Blueprint["GoodConsumingBuildingSpec"]);
                    List<object?> goods = ListOf(consumer["ConsumedGoods"]);
                    int expected = (int)GetNumber(variant.Data, "fuelStock");
                    int derived = 0;
                    foreach (object? entry in goods)
                    {
                        var good = MapOf(entry);
                        float rate = (float)GetNumber(good, "GoodPerHour");
                        derived += (int)Math.Ceiling((double)(rate * 25f));
                    }

                    Assert.Equal(expected, derived, variant.FileStem + " derived capacity (fuel only)");
                    Assert.Equal(expected, GetInt(MapOf(variant.Blueprint["SEE.SEEBoilerSpec"]), "FuelCapacityGoods"),
                        variant.FileStem + " spec capacity");
                }
            });

            t.Run("ConsumedGoods rates match engines.derived.json", () =>
            {
                foreach (Variant variant in variants)
                {
                    List<object?> expected = ListOf(variant.Data["consumedGoods"]);
                    List<object?> actual = ListOf(MapOf(variant.Blueprint["GoodConsumingBuildingSpec"])["ConsumedGoods"]);
                    Assert.Equal(expected.Count, actual.Count, variant.FileStem + " consumed good count");
                    for (int i = 0; i < expected.Count; i++)
                    {
                        var expectedGood = MapOf(expected[i]);
                        var actualGood = MapOf(actual[i]);
                        Assert.Equal(GetString(expectedGood, "goodId"), GetString(actualGood, "GoodId"),
                            variant.FileStem + " consumed good " + i);
                        Assert.Near(GetNumber(expectedGood, "goodPerHour"),
                            GetNumber(actualGood, "GoodPerHour"), 1e-5, variant.FileStem + " rate " + i);
                    }
                }
            });
        }

        private static void NamingTests(TestRunner t, List<Variant> variants)
        {
            t.Run("TemplateName, display name and icon follow the naming contract", () =>
            {
                foreach (Variant variant in variants)
                {
                    var template = MapOf(variant.Blueprint["TemplateSpec"]);
                    Assert.Equal(variant.FileStem, GetString(template, "TemplateName"),
                        variant.FileStem + " TemplateName");
                    var label = MapOf(variant.Blueprint["LabeledEntitySpec"]);
                    string displayKey = "Building.SEE.SteamBoiler." + variant.Size + "." + variant.Tier + ".DisplayName";
                    Assert.Equal(displayKey, GetString(label, "DisplayNameLocKey"), variant.FileStem + " display key");
                    Assert.Equal("Icons/SteamBoiler" + variant.Size + variant.Tier + variant.Faction,
                        GetString(label, "Icon"), variant.FileStem + " icon");
                }
            });

            t.Run("Tool orders are unique per size/tier, even and inside the 110..132 band", () =>
            {
                var orders = new HashSet<int>();
                var perSlot = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (Variant variant in variants)
                {
                    var tool = MapOf(variant.Blueprint["PlaceableBlockObjectSpec"]);
                    int order = GetInt(tool, "ToolOrder");
                    Assert.True(order >= 110 && order <= 132, variant.FileStem + " tool order " + order);
                    Assert.Equal(0, order % 2, variant.FileStem + " tool order is even");
                    string slot = variant.Size + "." + variant.Tier;
                    if (perSlot.TryGetValue(slot, out int existing))
                    {
                        Assert.Equal(existing, order, slot + " tool order shared by both factions");
                    }
                    else
                    {
                        perSlot[slot] = order;
                        Assert.True(orders.Add(order), "duplicate tool order " + order);
                    }
                }

                Assert.Equal(12, orders.Count, "12 unique tool orders");
            });

            t.Run("Both factions share one size/tier data row", () =>
            {
                foreach (Variant variant in variants)
                {
                    Assert.Equal(GetString(variant.Row, "size"), variant.Size, variant.FileStem + " size");
                    Assert.Equal(GetString(variant.Row, "tier"), variant.Tier, variant.FileStem + " tier");
                }
            });
        }

        // ------------------------------------------------------------------ loading

        private sealed class Variant
        {
            internal string Size = string.Empty;
            internal string Tier = string.Empty;
            internal string Faction = string.Empty;
            internal string FilePath = string.Empty;
            internal string FileStem = string.Empty;
            internal IDictionary<string, object?> Row = new Dictionary<string, object?>();
            internal IDictionary<string, object?> Data = new Dictionary<string, object?>();
            internal IDictionary<string, object?> Blueprint = new Dictionary<string, object?>();
        }

        private static List<Variant> LoadVariants()
        {
            string derivedPath = TestPaths.FromMod("data", "engines.derived.json");
            var derived = MapOf(MiniJson.Parse(File.ReadAllText(derivedPath)));
            List<object?> rows = ListOf(derived["rows"]);
            var variants = new List<Variant>(24);
            foreach (object? rowValue in rows)
            {
                var row = MapOf(rowValue);
                string size = GetString(row, "size");
                string tier = GetString(row, "tier");
                var factions = MapOf(row["variants"]);
                foreach (string faction in new[] { "Folktails", "IronTeeth" })
                {
                    Assert.True(factions.ContainsKey(faction), size + "." + tier + " has " + faction);
                    var data = MapOf(factions[faction]);
                    string blueprintPath = GetString(data, "blueprint");
                    string filePath = TestPaths.FromMod("data", blueprintPath.Replace("data/", string.Empty));
                    variants.Add(new Variant
                    {
                        Size = size,
                        Tier = tier,
                        Faction = faction,
                        FilePath = filePath,
                        FileStem = Path.GetFileName(filePath).Replace(".blueprint.json", string.Empty),
                        Row = row,
                        Data = data,
                        Blueprint = MapOf(MiniJson.Parse(File.ReadAllText(filePath)))
                    });
                }
            }

            return variants;
        }

        // ------------------------------------------------------------------ JSON helpers

        private static IDictionary<string, object?> MapOf(object? value)
        {
            return value as IDictionary<string, object?> ?? new Dictionary<string, object?>();
        }

        private static List<object?> ListOf(object? value)
        {
            return value as List<object?> ?? new List<object?>();
        }

        private static string GetString(IDictionary<string, object?> map, string key)
        {
            return MiniJson.GetString(map, key, string.Empty);
        }

        private static double GetNumber(IDictionary<string, object?> map, string key)
        {
            return MiniJson.GetNumber(map, key, 0.0);
        }

        private static int GetInt(IDictionary<string, object?> map, string key)
        {
            return MiniJson.GetInt(map, key, 0);
        }

        private static bool GetBool(IDictionary<string, object?> map, string key)
        {
            return MiniJson.GetBool(map, key, false);
        }

        private static string[] StringArray(IDictionary<string, object?> map, string key)
        {
            List<object?> values = ListOf(map.TryGetValue(key, out object? value) ? value : null);
            var result = new string[values.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = values[i] as string ?? string.Empty;
            }

            return result;
        }

        private static float[] FloatArray(IDictionary<string, object?> map, string key)
        {
            List<object?> values = ListOf(map.TryGetValue(key, out object? value) ? value : null);
            var result = new float[values.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = values[i] is double number ? (float)number : 0f;
            }

            return result;
        }

        private static void AssertFieldSet(string[] expected, IDictionary<string, object?> actual, string context)
        {
            var expectedSet = new HashSet<string>(expected, StringComparer.Ordinal);
            var actualSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in actual.Keys)
            {
                actualSet.Add(key);
            }

            Assert.Equal(expectedSet.Count, actualSet.Count, context + " field count");
            foreach (string field in expected)
            {
                Assert.True(actualSet.Contains(field), context + " is missing " + field);
            }

            foreach (string field in actualSet)
            {
                Assert.True(expectedSet.Contains(field), context + " has unexpected field " + field);
            }
        }

        private static void AssertEqualSlice<T>(T[] expected, T[] actual, string message)
        {
            Assert.Equal(expected.Length, actual.Length, message + " length");
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i], message + "[" + i + "]");
            }
        }

        private static bool BlueprintConsumesWater(IDictionary<string, object?> blueprint)
        {
            List<object?> goods = ListOf(MapOf(blueprint["GoodConsumingBuildingSpec"])["ConsumedGoods"]);
            foreach (object? entry in goods)
            {
                if (GetString(MapOf(entry), "GoodId") == "Water")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
