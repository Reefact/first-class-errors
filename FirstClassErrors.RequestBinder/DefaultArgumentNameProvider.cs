#region Usings declarations

using System.Reflection;

#endregion

namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The default <see cref="IArgumentNameProvider" />: the argument name is the C# property name, unchanged.
/// </summary>
/// <remarks>
///     It deliberately reads no serialization attribute: which serializer names the wire keys is the host's
///     knowledge, not this library's. Provide your own <see cref="IArgumentNameProvider" /> through
///     <see cref="RequestBinderOptions" /> when the error paths must match serialized names.
/// </remarks>
internal sealed class DefaultArgumentNameProvider : IArgumentNameProvider {

    /// <inheritdoc />
    public string GetArgumentNameFrom(PropertyInfo property) {
        if (property is null) { throw new ArgumentNullException(nameof(property)); }

        return property.Name;
    }

}
