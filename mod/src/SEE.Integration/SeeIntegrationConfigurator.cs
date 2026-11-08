using Bindito.Core;
using SEE.Integration.Adapters;
using SEE.Integration.Detection;
using SEE.Integration.Patching;
using SEE.Integration.Save;
using SEE.Integration.Water;
using Timberborn.BlueprintSystem;

namespace SEE.Integration
{
    /// <summary>
    /// Composition root of the optional integration assembly (CONTRACT_SEE §2/§11).
    ///
    /// Bindito discovers every non-abstract <see cref="Configurator"/> in the loaded assemblies and
    /// installs the ones whose <see cref="ContextAttribute"/> matches the active context, so this
    /// configurator is picked up without a manual registration call. Everything here is optional:
    /// with none of the three mods installed the core gameplay runs unchanged on vanilla blueprints
    /// and components (REQ-COMPAT-01).
    ///
    /// The Power Lines injection is registered as a vanilla <c>IBlueprintModifierProvider</c>
    /// multi-binding: the game itself calls it while deserializing SEE boiler blueprints, so no
    /// Harmony patch and no external blueprint edit is involved (REQ-POWER-16, API_FACTS_SEE §7.3).
    /// </summary>
    [Context("Game")]
    public sealed class SeeIntegrationConfigurator : Configurator
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            Bind<CompatDetector>().AsSingleton();
            Bind<ICompatDetector>().ToExisting<CompatDetector>();

            Bind<RuntimeCompatOptions>().AsSingleton();
            Bind<ICompatOptionsSource>().ToExisting<RuntimeCompatOptions>();

            Bind<TimberPipesAdapter>().AsSingleton();
            Bind<PowerLinesAdapter>().AsSingleton();
            Bind<ConveyorBeltAdapter>().AsSingleton();
            Bind<OptionalPatchHost>().AsSingleton();

            MultiBind<IBlueprintModifierProvider>().To<PowerLineInjectionProvider>().AsSingleton();

            Bind<CompatSaveIntegrator>().AsSingleton();
            Bind<WaterSupplyModeService>().AsSingleton();
        }
    }
}
