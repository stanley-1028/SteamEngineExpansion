using System;
using SEE.Core.Abstractions;
using Timberborn.Goods;
using Timberborn.InventorySystem;
using Timberborn.TemplateInstantiation;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Creates the dedicated water-tank input inventory of every boiler (bug fix 2026-11-08:
    /// ordinary Hauling-Post haulers must be able to carry water into the engine).
    ///
    /// Why a dedicated inventory instead of the v0.1.1 hotfix that listed Water as a second
    /// <c>GoodConsumingBuildingSpec</c> good:
    /// <list type="bullet">
    /// <item>the water tank keeps its own capacity and its own save component, so it can never eat
    /// into the fuel stock; the engine's fuel capacity stays the tabulated value, not the sum of
    /// the two goods;</item>
    /// <item>the inventory is declared as a public input (<see cref="InventoryInitializer.HasPublicInput"/>),
    /// the same declaration the vanilla water tanks use, so the district's hauling priorities treat
    /// it as a real delivery target;</item>
    /// <item>the water good is created as giveable-and-takeable, so haulers can bring it in and the
    /// controller can still withdraw whole goods with <c>TakeExisting</c>;</item>
    /// <item>it avoids the <c>StockpileSpec</c> that crashed the construction finish in v0.1.1
    /// (Stockpile + GoodConsumingBuilding registered two <c>NoHaulingPostStatus</c> initializers);
    /// a plain <see cref="Inventory"/> adds no second status initializer.</item>
    /// </list>
    ///
    /// The dedicated decorator initializer only declares and initializes the inventory; the owner
    /// (<see cref="SEEBoilerController"/>) enables it when the building is finished and disables it
    /// again when the building leaves the finished state, exactly like vanilla inventory owners.
    /// It is registered in <see cref="SeeGameConfigurator"/>'s template module.
    /// </summary>
    public sealed class SEEWaterInventoryInitializer : IDedicatedDecoratorInitializer<SEEBoilerSpec, Inventory>
    {
        /// <summary>
        /// Inventory component name used for the save slot and the hauling/UI layer.
        /// Kept stable: renaming it drops the stored tank contents of existing saves.
        /// </summary>
        public const string InventoryComponentName = "WaterTank";

        private readonly InventoryInitializerFactory _initializerFactory;

        /// <summary>Creates the initializer with the vanilla inventory factory (DI singleton).</summary>
        /// <param name="initializerFactory">Vanilla inventory initializer factory.</param>
        public SEEWaterInventoryInitializer(InventoryInitializerFactory initializerFactory)
        {
            _initializerFactory = initializerFactory
                                  ?? throw new ArgumentNullException(nameof(initializerFactory));
        }

        /// <summary>
        /// Whole water goods the boiler can store. The blueprint value is the tabulated tank size
        /// (<c>SEE.SEEBoilerSpec.WaterCapacityGoods</c>, 18–100 goods for the 12 tiers); an invalid
        /// value degrades to a 1-good tank instead of a zero-capacity inventory (a zero-capacity
        /// inventory would be silently excluded from hauling).
        /// </summary>
        /// <param name="spec">Boiler spec of the building being instantiated.</param>
        /// <returns>Inventory capacity in goods, at least 1.</returns>
        public static int CapacityGoods(SEEBoilerSpec spec)
        {
            return Math.Max(1, spec == null ? 0 : spec.WaterCapacityGoods);
        }

        /// <inheritdoc />
        public void Initialize(SEEBoilerSpec subject, Inventory decorator)
        {
            int capacity = CapacityGoods(subject);
            InventoryInitializer initializer =
                _initializerFactory.Create(decorator, capacity, InventoryComponentName);
            initializer.AddAllowedGood(new StorableGoodAmount(
                StorableGood.CreateGiveableAndTakeable(GoodIds.Water), capacity));
            initializer.HasPublicInput();
            initializer.Initialize();
        }
    }
}
