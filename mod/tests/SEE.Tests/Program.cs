using System;

namespace SEE.Tests
{
    /// <summary>
    /// Entry point of the L1 test runner. Runs every suite in contract order, prints the last line
    /// as "PASS n/n" on success and returns a non-zero exit code on any failure.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            bool listOnly = args.Length > 0 && string.Equals(args[0], "--list", StringComparison.Ordinal);
            if (args.Length > 0 && string.Equals(args[0], "--evidence", StringComparison.Ordinal))
            {
                // P4B acceptance evidence: the four new panel rows as the UI model builds them.
                UiContractTests.PrintPanelEvidence();
                return 0;
            }

            var runner = new TestRunner(listOnly);
            ConditionCurveTests.Run(runner);
            BoilerFormulasTests.Run(runner);
            MaintenanceLedgerTests.Run(runner);
            StateMachineTests.Run(runner);
            ConfigTests.Run(runner);
            ConfigClampTests.Run(runner);
            SaveCodecTests.Run(runner);
            SaveNodeTests.Run(runner);
            SaveNodeContractTests.Run(runner);
            TierCatalogTests.Run(runner);
            ContractSurfaceTests.Run(runner);
            BlueprintContractTests.Run(runner);
            WaterTankSupplyTests.Run(runner);
            SaveBridgeContractTests.Run(runner);
            ConsumptionReconciliationTests.Run(runner);
            UiContractTests.Run(runner);
            MiniJsonTests.Run(runner);
            ThrottledLogTests.Run(runner);
            CompatScenarioTests.Run(runner);
            WaterSupplyPolicyTests.Run(runner);
            PowerLineInjectionTests.Run(runner);
            IntegrationSaveTests.Run(runner);
            LayerRulesTests.Run(runner);
            return runner.Report();
        }
    }
}
