#region Usings declarations

using System.Linq.Expressions;
using System.Reflection;

#endregion

namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Resolves the <see cref="PropertyInfo" /> behind a property-selector lambda (<c>r =&gt; r.GuestEmail</c>).
/// </summary>
/// <remarks>
///     Only a direct property access on the lambda parameter is a valid selector: the property name is what the
///     argument path is built from, so an arbitrary expression has no meaningful name. An invalid selector is a
///     programming error and is reported by throwing (the binder's bug channel).
/// </remarks>
internal static class PropertySelectors {

    #region Statics members declarations

    internal static PropertyInfo GetProperty<TRequest, TArgument>(Expression<Func<TRequest, TArgument>> selector) {
        if (selector is null) { throw new ArgumentNullException(nameof(selector)); }

        // The compiler may wrap the member access in a Convert node (e.g. IReadOnlyList<T> -> IEnumerable<T>).
        Expression body = selector.Body;
        while (body is UnaryExpression { NodeType: ExpressionType.Convert } convert) {
            body = convert.Operand;
        }

        if (body is not MemberExpression { Member: PropertyInfo property } member || member.Expression is not ParameterExpression) {
            throw new ArgumentException(
                $"The selector '{selector}' must be a direct property access on the request parameter (e.g. r => r.GuestEmail).",
                nameof(selector));
        }

        return property;
    }

    #endregion

}
