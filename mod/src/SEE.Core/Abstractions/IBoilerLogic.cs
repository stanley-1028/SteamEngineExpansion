namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Per-boiler command surface the entity panel binds its buttons to (REQ-UI-07). Implemented
    /// by <c>SEE.Gameplay.Components.SEEBoilerController</c> and exposed through the boiler
    /// registry, so SEE.UI can call it without referencing the gameplay assembly.
    ///
    /// The methods themselves are declared by <see cref="IBoilerCommands"/> (the contract name used
    /// in CONTRACT_SEE §2.4); this alias keeps the panel/skeleton name stable. Every method
    /// addresses exactly one boiler by entity id; a command never affects another boiler and
    /// returns false when the id is unknown or the boiler is not active.
    /// </summary>
    public interface IBoilerLogic : IBoilerCommands
    {
    }
}
