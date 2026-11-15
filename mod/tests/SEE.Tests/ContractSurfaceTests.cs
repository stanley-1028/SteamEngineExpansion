using System;
using System.Collections.Generic;
using System.Reflection;
using SEE.Core.Abstractions;
using SEE.Core.Config;
using SEE.Core.Constants;
using SEE.Core.Formulas;

namespace SEE.Tests
{
    /// <summary>
    /// Frozen-surface tests (CONTRACT_SEE §2.4/§8, requirements §11.4/§20): the command contract,
    /// the enum sets, the runtime constants and the read-only UI view. They exist so a rename or a
    /// silent semantic change fails the L1 gate instead of surfacing in the game.
    /// </summary>
    internal static class ContractSurfaceTests
    {
        internal static void Run(TestRunner t)
        {
            CommandsContractTests(t);
            EnumTests(t);
            ConstantTests(t);
            StatusViewTests(t);
            FormulaSurfaceTests(t);
        }

        private static void CommandsContractTests(TestRunner t)
        {
            t.Run("IBoilerCommands declares the four Guid-addressed commands", () =>
            {
                Type type = typeof(IBoilerCommands);
                AssertMethod(type, "SetPaused", typeof(Guid), typeof(bool));
                AssertMethod(type, "RequestMaintenanceNow", typeof(Guid));
                AssertMethod(type, "SetAutoMaintenance", typeof(Guid), typeof(bool));
                AssertMethod(type, "SetWaterSupplyMode", typeof(Guid), typeof(WaterSupplyMode));
            });

            t.Run("IBoilerLogic is the panel-facing alias of IBoilerCommands", () =>
            {
                Assert.True(typeof(IBoilerCommands).IsAssignableFrom(typeof(IBoilerLogic)), "IBoilerLogic extends IBoilerCommands");
            });

            t.Run("IBoilerQuery is Guid-keyed and read-only", () =>
            {
                MethodInfo method = typeof(IBoilerQuery).GetMethod("TryGetView")!;
                Assert.NotNull(method, "TryGetView");
                ParameterInfo[] parameters = method.GetParameters();
                Assert.Equal(typeof(Guid), parameters[0].ParameterType, "entity id parameter");
                Assert.Equal(typeof(IBoilerStatusView).MakeByRefType(), parameters[1].ParameterType, "out view");
            });
        }

        private static void EnumTests(TestRunner t)
        {
            t.Run("BoilerStatus has the nine main states plus the Maintaining override", () =>
            {
                BoilerStatus[] values = (BoilerStatus[])Enum.GetValues(typeof(BoilerStatus));
                Assert.Equal(10, values.Length, "status count");
                foreach (string name in new[]
                         {
                             "Paused", "Maintaining", "NoFuel", "NoWater", "NeedsMaintenance",
                             "Critical", "Worn", "MaintenanceDue", "Idle", "Running"
                         })
                {
                    Assert.True(Enum.IsDefined(typeof(BoilerStatus), name), "status " + name);
                }
            });

            t.Run("MaintenancePhase has the four persisted phases", () =>
            {
                MaintenancePhase[] values = (MaintenancePhase[])Enum.GetValues(typeof(MaintenancePhase));
                Assert.Equal(4, values.Length, "phase count");
                foreach (string name in new[] { "None", "Maintaining", "MissingMaterials", "NoAccess" })
                {
                    Assert.True(Enum.IsDefined(typeof(MaintenancePhase), name), "phase " + name);
                }
            });

            t.Run("WaterSupplyMode has all four modes", () =>
            {
                WaterSupplyMode[] values = (WaterSupplyMode[])Enum.GetValues(typeof(WaterSupplyMode));
                Assert.Equal(4, values.Length, "mode count");
                foreach (string name in new[] { "Auto", "Tank", "Valve", "Hauler" })
                {
                    Assert.True(Enum.IsDefined(typeof(WaterSupplyMode), name), "mode " + name);
                }
            });

            t.Run("FuelScalingMode has exactly proportional and vanilla", () =>
            {
                FuelScalingMode[] values = (FuelScalingMode[])Enum.GetValues(typeof(FuelScalingMode));
                Assert.Equal(2, values.Length, "scaling count");
                Assert.True(Enum.IsDefined(typeof(FuelScalingMode), "Proportional"), "proportional");
                Assert.True(Enum.IsDefined(typeof(FuelScalingMode), "Vanilla"), "vanilla");
            });

            t.Run("BoilerStatusReason attributes every state", () =>
            {
                BoilerStatusReason[] values = (BoilerStatusReason[])Enum.GetValues(typeof(BoilerStatusReason));
                Assert.Equal(10, values.Length, "reason count");
            });
        }

        private static void ConstantTests(TestRunner t)
        {
            t.Run("SEEConst carries the six frozen runtime constants", () =>
            {
                Assert.Near(1.0, SEEConst.TickIntervalSeconds, 1e-12, "TickIntervalSeconds");
                Assert.Near(2.0, SEEConst.DetectBudgetSeconds, 1e-12, "DetectBudgetSeconds");
                Assert.Near(30.0, SEEConst.LogThrottleSeconds, 1e-12, "LogThrottleSeconds");
                Assert.Near(2.0, SEEConst.MaintenanceDurationHours, 1e-6, "MaintenanceDurationHours");
                Assert.Near(50.0, SEEConst.AutoThreshold, 1e-6, "AutoThreshold");
                Assert.Near(0.4, SEEConst.LoadFloor, 1e-6, "LoadFloor");
            });

            t.Run("SEEConst keeps the unit and identity constants", () =>
            {
                Assert.Equal("SteamEngineExpansion", SEEConst.ModId, "mod id");
                Assert.Equal("SEE", SEEConst.SaveComponentKey, "save key");
                Assert.Equal("SEE_Config.json", SEEConst.ConfigFileName, "config file name");
                Assert.Near(24.0, SEEConst.HoursPerDay, 1e-12, "hours per day");
                Assert.Near(100.0, SEEConst.MaxCondition, 1e-6, "max condition");
                Assert.Equal("Power", SEEConst.ToolGroupId, "tool group");
                Assert.Near(2.12, SEEConst.BeaverWaterPerDay, 1e-6, "beaver water per day");
            });
        }

        private static void StatusViewTests(TestRunner t)
        {
            t.Run("IBoilerStatusView exposes the panel fields", () =>
            {
                foreach (string property in new[]
                         {
                             "EntityId", "Status", "MaintenancePhase", "Tier", "Size", "Faction",
                             "IsFinished", "IsPaused", "RatedHorsepower", "ActualOutput", "Condition",
                             "ConditionMultiplier", "LoadRatio", "NetworkSupplyHp", "NetworkDemandHp",
                             "NetworkEfficiency", "FuelInStock", "FuelCapacity", "WaterInStock",
                             "WaterCapacity", "FuelPerHour", "WaterPerHour", "AutoMaintenance",
                             "MaintenanceRemainingHours", "LastMaintenanceDay", "MaintenanceTriggerThreshold",
                             "WaterSupplyMode", "PowerLineAttached",
                             "WaterBeaverEquivalent", "FuelGoods", "MaintenanceMaterials",
                             "NextMaintenanceDay", "MissingMaterialCount", "WaterSupplyAvailability",
                             "PipeSupplyActive", "BeltSupplyActive", "BeltHasPower"
                         })
                {
                    Assert.NotNull(typeof(IBoilerStatusView).GetProperty(property), "property " + property);
                }
            });

            t.Run("The remaining-hours helpers derive from stock and rate", () =>
            {
                var view = new FakeView { FuelInStock = 6, FuelPerHour = 0.12f, WaterInStock = 32, WaterPerHour = 0.32f };
                Assert.Near(50.0, view.FuelRemainingHours(), 1e-4, "fuel hours");
                Assert.Near(100.0, view.WaterRemainingHours(), 1e-4, "water hours");
            });

            t.Run("The remaining-hours helpers are safe for a zero rate", () =>
            {
                var view = new FakeView();
                Assert.Near(0.0, view.FuelRemainingHours(), 1e-6, "zero fuel rate");
                Assert.Near(0.0, view.WaterRemainingHours(), 1e-6, "zero water rate");
            });
        }

        private static void FormulaSurfaceTests(TestRunner t)
        {
            t.Run("DaysUntilMaintenance reaches exactly one interval from 100 to the threshold", () =>
            {
                Assert.Near(8.0, BoilerFormulas.DaysUntilMaintenance(100f, 8.0, 1f), 1e-6, "T2 full load");
                Assert.Near(4.0, BoilerFormulas.DaysUntilMaintenance(75f, 8.0, 1f), 1e-6, "half way");
                Assert.Near(0.0, BoilerFormulas.DaysUntilMaintenance(50f, 8.0, 1f), 1e-9, "already due");
            });

            t.Run("DaysUntilMaintenance is infinite when the boiler does not wear", () =>
            {
                Assert.True(double.IsPositiveInfinity(BoilerFormulas.DaysUntilMaintenance(100f, 0.0, 1f)), "disabled");
                Assert.True(double.IsPositiveInfinity(BoilerFormulas.DaysUntilMaintenance(100f, 8.0, 0f)), "idle");
            });
        }

        private static void AssertMethod(Type type, string name, params Type[] parameterTypes)
        {
            MethodInfo? method = type.GetMethod(name, parameterTypes);
            Assert.NotNull(method, "method " + name);
            Assert.Equal(typeof(bool), method!.ReturnType, name + " returns bool");
        }

        private sealed class FakeView : IBoilerStatusView
        {
            public Guid EntityId { get; set; } = Guid.Empty;

            public BoilerStatus Status { get; set; } = BoilerStatus.Running;

            public MaintenancePhase MaintenancePhase { get; set; } = MaintenancePhase.None;

            public BoilerTier Tier { get; set; } = BoilerTier.T1;

            public BoilerSize Size { get; set; } = BoilerSize.Small;

            public BoilerFaction Faction { get; set; } = BoilerFaction.Folktails;

            public bool IsFinished { get; set; } = true;

            public bool IsPaused { get; set; }

            public int RatedHorsepower { get; set; } = 130;

            public int ActualOutput { get; set; }

            public float Condition { get; set; } = 100f;

            public float ConditionMultiplier { get; set; } = 1f;

            public float LoadRatio { get; set; }

            public int NetworkSupplyHp { get; set; }

            public int NetworkDemandHp { get; set; }

            public float NetworkEfficiency { get; set; }

            public int FuelInStock { get; set; }

            public int FuelCapacity { get; set; }

            public int WaterInStock { get; set; }

            public int WaterCapacity { get; set; }

            public float FuelPerHour { get; set; }

            public float WaterPerHour { get; set; }

            public bool AutoMaintenance { get; set; } = true;

            public float MaintenanceRemainingHours { get; set; }

            public float LastMaintenanceDay { get; set; }

            public float MaintenanceTriggerThreshold { get; set; } = 50f;

            public WaterSupplyMode WaterSupplyMode { get; set; } = WaterSupplyMode.Auto;

            public bool PowerLineAttached { get; set; }

            public float WaterBeaverEquivalent { get; set; }

            public IReadOnlyList<string> FuelGoods { get; set; } = new[] { "Log" };

            public IReadOnlyList<MaintenanceMaterialStatus> MaintenanceMaterials { get; set; } =
                Array.Empty<MaintenanceMaterialStatus>();

            public float NextMaintenanceDay { get; set; }

            public int MissingMaterialCount { get; set; }

            public WaterSupplyAvailability WaterSupplyAvailability { get; set; } = WaterSupplyAvailability.Available;

            public bool PipeSupplyActive { get; set; }

            public bool BeltSupplyActive { get; set; }

            public bool BeltHasPower { get; set; } = true;
        }
    }
}
