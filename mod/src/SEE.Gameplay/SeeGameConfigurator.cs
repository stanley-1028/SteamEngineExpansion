using System;
using Bindito.Core;
using SEE.Core.Abstractions;
using SEE.Gameplay.Components;
using Timberborn.InventorySystem;
using Timberborn.TemplateInstantiation;

namespace SEE.Gameplay
{
    /// <summary>
    /// Game-context composition root for SEE.Gameplay (CONTRACT_SEE §2/§5, REQ-ARCH-07).
    ///
    /// Registrations:
    /// <list type="bullet">
    /// <item><see cref="SEERuntime"/>: session config + the one shared throttled log;</item>
    /// <item><see cref="SEEBoilerRegistry"/>: one instance exposed as <see cref="IBoilerQuery"/>,
    /// <see cref="IBoilerLogic"/> and the internal lifecycle interface with <c>ToExisting</c>, so
    /// the UI query and the UI commands hit the same dictionary (REQ-UI-07);</item>
    /// <item><see cref="BoilerTickSystem"/>: the single 1 s tick entry point (also auto-registered
    /// as a game tickable singleton through the [Singleton]-marked interface);</item>
    /// <item><see cref="SeeSaveBridge"/>: the world-node "SEE" save/load singleton (D-6);</item>
    /// <item><see cref="SEEBoilerController"/>: transient, one per boiler entity, added by the
    /// template decorator below;</item>
    /// <item><see cref="SEEWaterInventoryInitializer"/>: the dedicated public water-tank input
    /// inventory haulers deliver water into (bug fix 2026-11-08, see the initializer docs);</item>
    /// <item>the template module: every blueprint carrying <see cref="SEEBoilerSpec"/> gets exactly
    /// one controller plus one water-tank inventory.</item>
    /// </list>
    ///
    /// No runtime patch is registered: the whole mod uses native specs, decorators and DI
    /// (requirements §11.6, REQ-IMPL-07).
    /// </summary>
    [Context("Game")]
    public sealed class SeeGameConfigurator : Configurator
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            Bind<SEERuntime>().AsSingleton();

            Bind<SEEBoilerRegistry>().AsSingleton();
            Bind<ISEEBoilerRegistry>().ToExisting<SEEBoilerRegistry>();
            Bind<IBoilerQuery>().ToExisting<SEEBoilerRegistry>();
            Bind<IBoilerLogic>().ToExisting<SEEBoilerRegistry>();

            Bind<BoilerTickSystem>().AsSingleton();
            Bind<SeeSaveBridge>().AsSingleton();

            Bind<SEEBoilerController>().AsTransient();

            MultiBind<TemplateModule>().ToProvider<TemplateModuleProvider>().AsSingleton();
        }

        /// <summary>
        /// Template module provider. Unlike a static factory it can take the vanilla
        /// <see cref="InventoryInitializerFactory"/> from DI, which the water-tank initializer needs.
        /// </summary>
        public sealed class TemplateModuleProvider : IProvider<TemplateModule>
        {
            private readonly InventoryInitializerFactory _inventoryInitializerFactory;

            /// <summary>Creates the provider; the factory is a game DI singleton.</summary>
            /// <param name="inventoryInitializerFactory">Vanilla inventory initializer factory.</param>
            public TemplateModuleProvider(InventoryInitializerFactory inventoryInitializerFactory)
            {
                _inventoryInitializerFactory = inventoryInitializerFactory
                    ?? throw new ArgumentNullException(nameof(inventoryInitializerFactory));
            }

            /// <inheritdoc />
            public TemplateModule Get()
            {
                return BuildTemplateModule(_inventoryInitializerFactory);
            }
        }

        /// <summary>
        /// Binds the custom spec to the shared controller and the dedicated water inventory. One
        /// decorator set for all 24 blueprints keeps them data-only and balancing code-free
        /// (REQ-NFR-04).
        ///
        /// Fuel stays on the vanilla <c>GoodConsumingBuildingSpec</c> inventory (the game already
        /// provides the hauler fill-input chain for it). Water gets its own public input inventory
        /// so ordinary haulers can deliver it without the combined-inventory hotfix workaround.
        /// </summary>
        /// <param name="inventoryInitializerFactory">Vanilla inventory initializer factory.</param>
        /// <returns>The template module for the game container.</returns>
        internal static TemplateModule BuildTemplateModule(InventoryInitializerFactory inventoryInitializerFactory)
        {
            var builder = new TemplateModule.Builder();
            builder.AddDecorator<SEEBoilerSpec, SEEBoilerController>();
            builder.AddDedicatedDecorator(new SEEWaterInventoryInitializer(inventoryInitializerFactory));
            return builder.Build();
        }
    }
}