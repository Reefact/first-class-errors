#region Usings declarations

using System.Linq.Expressions;
using System.Reflection;

#endregion

namespace FirstClassErrors.RequestBinder;

/// <summary>
///     A request DTO attached to a <see cref="RequestBinder" /> as a source of inputs: its properties are
///     selected and bound into value objects, recording every failure into the binder's single collect-all envelope.
///     Obtained from <see cref="RequestBinder.PropertiesOf{TDto}" />; a DTO is one source among peers, sitting
///     beside the out-of-DTO arguments bound directly on the binder.
/// </summary>
/// <typeparam name="TDto">The type of the request DTO.</typeparam>
public sealed class PropertySource<TDto> {

    #region Fields declarations

    private readonly RequestBinding _binding;
    private readonly TDto           _dto;

    #endregion

    #region Constructors declarations

    internal PropertySource(RequestBinding binding, TDto dto) {
        _binding = binding;
        _dto     = dto;
    }

    #endregion

    /// <summary>
    ///     Selects a scalar property, converted by a plain value-object converter
    ///     (<c>Func&lt;TArgument, Outcome&lt;T&gt;&gt;</c>).
    /// </summary>
    /// <typeparam name="TArgument">The type of the DTO property.</typeparam>
    /// <param name="selector">A direct property access on the DTO (e.g. <c>d =&gt; d.GuestEmail</c>).</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c> and their variants.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="selector" /> points at a <b>non-nullable value-type</b> property: a missing value
    ///     would be indistinguishable from its default (<c>0</c>, <c>false</c>, ...), so such a property must be declared
    ///     nullable (e.g. <c>int?</c>) for the binder to detect an absent argument.
    /// </exception>
    public SimplePropertyConverter<TArgument> SimpleProperty<TArgument>(Expression<Func<TDto, TArgument?>> selector) {
        (string path, object? value) = ResolveArgument(selector);

        return new SimplePropertyConverter<TArgument>(_binding, path, (TArgument?)value, value is null, source: null);
    }

    /// <summary>
    ///     Selects a scalar <b>value-type</b> property declared nullable (e.g. <c>int?</c>), converted by a value-object
    ///     converter over the <b>underlying, non-nullable type</b> (<c>Func&lt;int, Outcome&lt;T&gt;&gt;</c>).
    /// </summary>
    /// <remarks>
    ///     A nullable value-type property surfaces its underlying type here, not its <see cref="Nullable{T}" />: a
    ///     converter written against the underlying type binds as a method group, exactly as a reference-type converter
    ///     does on the reference overload. This overload exists because a value type and a reference type cannot share one
    ///     selector method (the two carry genuinely different parameter types, <c>TArgument</c> versus
    ///     <c>Nullable&lt;TArgument&gt;</c>), so they coexist.
    /// </remarks>
    /// <typeparam name="TArgument">The underlying (non-nullable) value type of the DTO property.</typeparam>
    /// <param name="selector">A direct property access on a nullable value-type property (e.g. <c>d =&gt; d.MaxNights</c>).</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c> and their variants.</returns>
    public SimplePropertyConverter<TArgument> SimpleProperty<TArgument>(Expression<Func<TDto, TArgument?>> selector) where TArgument : struct {
        (string path, object? value) = ResolveArgument(selector);

        return new SimplePropertyConverter<TArgument>(_binding, path, value is null ? default : (TArgument)value, value is null, source: null);
    }

    /// <summary>
    ///     Selects a complex property, bound by a nested binder. Declare the nested envelope next, with
    ///     <see cref="ComplexPropertyEnvelopeStage{TArgument}.FailWith" />.
    /// </summary>
    /// <typeparam name="TArgument">The type of the nested DTO.</typeparam>
    /// <param name="selector">A direct property access on the DTO (e.g. <c>d =&gt; d.Stay</c>).</param>
    /// <returns>The stage on which the nested envelope is declared.</returns>
    public ComplexPropertyEnvelopeStage<TArgument> ComplexProperty<TArgument>(Expression<Func<TDto, TArgument?>> selector) {
        (string path, object? value) = ResolveArgument(selector);

        return new ComplexPropertyEnvelopeStage<TArgument>(_binding, path, (TArgument?)value, value is null);
    }

    /// <summary>
    ///     Selects a list property whose elements are converted by a plain value-object converter.
    /// </summary>
    /// <typeparam name="TArgument">The element type of the DTO list.</typeparam>
    /// <param name="selector">A direct property access on the DTO (e.g. <c>d =&gt; d.Tags</c>).</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c>.</returns>
    public ListOfSimplePropertiesConverter<TArgument> ListOfSimpleProperties<TArgument>(Expression<Func<TDto, IEnumerable<TArgument?>?>> selector) {
        (string path, object? value) = ResolveArgument(selector);

        return new ListOfSimplePropertiesConverter<TArgument>(_binding, path, (IEnumerable<TArgument?>?)value, value is null, source: null);
    }

    /// <summary>
    ///     Selects a list property whose elements are a nullable <b>value type</b> (e.g. <c>int?</c>), each converted by
    ///     a value-object converter over the <b>underlying, non-nullable type</b>.
    /// </summary>
    /// <remarks>
    ///     The value-type counterpart of the reference/string list overload: it exists for the same reason as the
    ///     value-type <c>SimpleProperty</c> overload — an <c>IEnumerable&lt;TArgument&gt;</c> selector and an
    ///     <c>IEnumerable&lt;Nullable&lt;TArgument&gt;&gt;</c> selector are distinct parameter types, so the two coexist.
    /// </remarks>
    /// <typeparam name="TArgument">The underlying (non-nullable) value type of the DTO list elements.</typeparam>
    /// <param name="selector">A direct property access on a list of nullable value types (e.g. <c>d =&gt; d.Quantities</c>).</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c>.</returns>
    public ListOfSimpleValuePropertiesConverter<TArgument> ListOfSimpleProperties<TArgument>(Expression<Func<TDto, IEnumerable<TArgument?>?>> selector) where TArgument : struct {
        (string path, object? value) = ResolveArgument(selector);

        return new ListOfSimpleValuePropertiesConverter<TArgument>(_binding, path, (IEnumerable<TArgument?>?)value, value is null, source: null);
    }

    /// <summary>
    ///     Selects a list property whose elements are bound by a nested binder (one per element). Declare the per-element
    ///     envelope next, with <see cref="ListOfComplexPropertiesEnvelopeStage{TArgument}.FailWith" />.
    /// </summary>
    /// <typeparam name="TArgument">The element type of the DTO list.</typeparam>
    /// <param name="selector">A direct property access on the DTO (e.g. <c>d =&gt; d.Guests</c>).</param>
    /// <returns>The stage on which the per-element envelope is declared.</returns>
    public ListOfComplexPropertiesEnvelopeStage<TArgument> ListOfComplexProperties<TArgument>(Expression<Func<TDto, IEnumerable<TArgument?>?>> selector) {
        (string path, object? value) = ResolveArgument(selector);

        return new ListOfComplexPropertiesEnvelopeStage<TArgument>(_binding, path, (IEnumerable<TArgument?>?)value, value is null);
    }

    private (string Path, object? Value) ResolveArgument<TArgument>(Expression<Func<TDto, TArgument>> selector) {
        PropertyInfo property = PropertySelectors.GetProperty(selector);

        // A non-nullable value-type property can never be null, so a missing argument (deserialized to default(T) —
        // 0, false, ...) is indistinguishable from a legitimately-sent default: absence would be silently lost. The
        // information does not exist at runtime, so reject the mis-declaration loudly (the binder's programming-error
        // channel) — the DTO property must be declared nullable so that an absent argument arrives as null.
        if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null) {
            throw new ArgumentException(
                $"The request property '{property.Name}' is a non-nullable value type ({property.PropertyType.Name}); a missing value would be indistinguishable from its default. Declare it as {property.PropertyType.Name}? so the binder can detect an absent argument.",
                nameof(selector));
        }

        string path = _binding.PathOf(_binding.Options.ArgumentNameProvider.GetArgumentNameFrom(property));

        return (path, property.GetValue(_dto));
    }

}
