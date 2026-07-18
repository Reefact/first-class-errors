#region Usings declarations

using System.Reflection;
using System.Text;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Options;

/// <summary>
///     A sample <see cref="IArgumentNameProvider" /> that reports argument paths in <c>snake_case</c> (for example
///     <c>guest_email</c>, <c>stay.check_in</c>) instead of the C# property names. Plugged in through
///     <c>Bind.WithOptions(new RequestBinderOptions(new SnakeCaseArgumentNames()))</c>, it makes the paths reported in
///     binding errors match the keys a snake_case JSON client actually sent. A real provider would usually read the
///     serializer's naming policy or a <c>[JsonPropertyName]</c> attribute; this one derives the name mechanically to
///     stay dependency-free.
/// </summary>
public sealed class SnakeCaseArgumentNames : IArgumentNameProvider {

    /// <inheritdoc />
    public string GetArgumentNameFrom(PropertyInfo property) {
        ArgumentNullException.ThrowIfNull(property);

        return ToSnakeCase(property.Name);
    }

    private static string ToSnakeCase(string name) {
        StringBuilder builder = new(name.Length + 8);
        for (int i = 0; i < name.Length; i++) {
            char c = name[i];
            if (char.IsUpper(c)) {
                if (i > 0) { builder.Append('_'); }
                builder.Append(char.ToLowerInvariant(c));
            } else {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

}
