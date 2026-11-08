// Polyfill required to declare record structs against netstandard2.1.
//
// The C# compiler emits init-only property setters (modreq IsExternalInit) for positional
// record structs such as SEE.Core.Abstractions.MaintenanceMaterial. netstandard2.1 does not
// ship that marker type, so SEE.Core declares it locally. The type is internal to this
// assembly; SEE.Tests does not need it because it only consumes the compiled record.

namespace System.Runtime.CompilerServices
{
    /// <summary>Compiler marker that enables init-only setters on netstandard2.1.</summary>
    internal static class IsExternalInit
    {
    }
}
