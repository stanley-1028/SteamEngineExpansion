using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace SEE.UI.Panels
{
    /// <summary>
    /// Contributes the boiler fragment to the game's entity panel.
    ///
    /// This mirrors the pattern the vanilla UI assemblies use: each one multi-binds
    /// <see cref="EntityPanelModule"/> with an <see cref="IProvider{T}"/> that builds a module from
    /// its fragments. No gameplay-side registration is needed for the panel itself; gameplay only
    /// registers the two services the fragment consumes (<c>IBoilerQuery</c> and
    /// <c>IBoilerLogic</c>).
    /// </summary>
    public sealed class BoilerPanelModuleProvider : IProvider<EntityPanelModule>
    {
        /// <summary>
        /// Order inside the content area. Vanilla building content uses order 1, so the boiler rows
        /// sit next to the other building rows instead of in a separate header/footer slot.
        /// </summary>
        private const int ContentOrder = 1;

        private readonly BoilerPanelFragment _fragment;

        /// <summary>Creates the provider.</summary>
        /// <param name="fragment">Singleton boiler panel fragment to contribute.</param>
        public BoilerPanelModuleProvider(BoilerPanelFragment fragment)
        {
            _fragment = fragment;
        }

        /// <inheritdoc />
        public EntityPanelModule Get()
        {
            var builder = new EntityPanelModule.Builder();
            builder.AddMiddleFragment(_fragment, ContentOrder);
            return builder.Build();
        }
    }
}
