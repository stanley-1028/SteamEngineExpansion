using Bindito.Core;
using SEE.UI.Panels;
using Timberborn.EntityPanelSystem;

namespace SEE.UI
{
    /// <summary>
    /// Composition root of the UI assembly (CONTRACT_SEE §2/§15).
    ///
    /// Bindito discovers every non-abstract <see cref="Configurator"/> in the loaded assemblies and
    /// installs the ones whose <see cref="ContextAttribute"/> matches the active context, so this
    /// configurator is picked up from the mod assembly without a manual registration call. The
    /// fragment itself is injected into the entity panel through the same
    /// <see cref="EntityPanelModule"/> multi-binding the vanilla UI configurators use.
    ///
    /// SEE.UI references SEE.Core only: the fragment reads IBoilerQuery and writes through
    /// IBoilerLogic, both registered as session singletons by the gameplay composition root
    /// (REQ-ARCH-03).
    /// </summary>
    [Context("Game")]
    public sealed class SeeUiConfigurator : Configurator
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            Bind<BoilerPanelFragment>().AsSingleton();
            MultiBind<EntityPanelModule>().ToProvider<BoilerPanelModuleProvider>().AsSingleton();
        }
    }
}