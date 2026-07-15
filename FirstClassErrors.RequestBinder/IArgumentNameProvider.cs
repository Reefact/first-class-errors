#region Usings declarations

using System.Reflection;

#endregion

namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Provides the argument name used in error paths for a bound DTO property.
/// </summary>
/// <remarks>
///     The default (<see cref="RequestBinderOptions.Default" />) uses the C# property name. Plug a
///     serializer-aware implementation (reading, for example, <c>JsonPropertyName</c> attributes or a naming
///     policy) so the paths reported in errors match the keys the client actually sent.
/// </remarks>
public interface IArgumentNameProvider {

    /// <summary>Returns the argument name to use in error paths for the given DTO property.</summary>
    /// <param name="property">The DTO property being bound.</param>
    /// <returns>The client-facing argument name.</returns>
    string GetArgumentNameFrom(PropertyInfo property);

}
