namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Binds a list input whose elements are a nullable <b>value type</b> (e.g. <c>int?</c>), each converted by a
///     value-object converter over the underlying non-nullable type. Each failing element is recorded individually, under
///     its indexed path (<c>Quantities[2]</c>), so one bad element never hides the others.
/// </summary>
/// <remarks>
///     The value-type counterpart of <see cref="ListOfSimplePropertiesConverter{TArgument}" />: because a
///     <c>Nullable&lt;TArgument&gt;</c> element and a reference-type element need different null handling, the value-type
///     path is a separate converter rather than a reuse of the reference one. It differs only in that a present element
///     is unwrapped (<c>element.Value</c>) before conversion.
/// </remarks>
/// <typeparam name="TArgument">The underlying (non-nullable) value type of the list elements.</typeparam>
public sealed class ListOfSimpleValuePropertiesConverter<TArgument> : IElementPathSource where TArgument : struct {

    #region Fields declarations

    private readonly RequestBinding            _binding;
    private          object                    _argumentPathOrProperty;
    private readonly IEnumerable<TArgument?>?  _values;
    private readonly bool                      _isMissing;
    private readonly string?                   _source;

    #endregion

    #region Constructors declarations

    internal ListOfSimpleValuePropertiesConverter(RequestBinding binding, object argumentPathOrProperty, IEnumerable<TArgument?>? values, bool isMissing, string? source) {
        _binding                = binding;
        _argumentPathOrProperty = argumentPathOrProperty;
        _values                 = values;
        _isMissing              = isMissing;
        _source                 = source;
    }

    #endregion

    /// <inheritdoc />
    string IElementPathSource.ElementPathAt(int index) {
        return ElementPathAt(index);
    }

    /// <summary>
    ///     Binds a required list: only an <b>absent</b> (<c>null</c>) list records <c>REQUEST_ARGUMENT_REQUIRED</c> —
    ///     a list that is <b>present but empty</b> is valid and binds an empty list, because a required list
    ///     constrains the list's <b>presence</b>, not its element count. Each failing element records
    ///     <c>REQUEST_ARGUMENT_INVALID</c> under its indexed path, and a <c>null</c> element records
    ///     <c>REQUEST_ARGUMENT_REQUIRED</c>.
    /// </summary>
    /// <typeparam name="TProperty">The type of the element value object.</typeparam>
    /// <param name="convertElement">The value-object converter applied to each element's underlying value.</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convertElement" /> is <c>null</c>.</exception>
    public RequiredField<IReadOnlyList<TProperty>> AsRequired<TProperty>(Func<TArgument, Outcome<TProperty>> convertElement) where TProperty : notnull {
        if (convertElement is null) { throw new ArgumentNullException(nameof(convertElement)); }

        if (_isMissing) {
            _binding.RecordArgumentRequired(ArgumentPath(), _source);

            return new RequiredField<IReadOnlyList<TProperty>>(_binding, default!);
        }

        return ConvertElements(convertElement);
    }

    /// <summary>
    ///     Binds an optional list: absent yields an <b>empty</b> list (never <c>null</c>) and records nothing; each
    ///     failing element of a present list still records <c>REQUEST_ARGUMENT_INVALID</c> under its indexed path.
    /// </summary>
    /// <typeparam name="TProperty">The type of the element value object.</typeparam>
    /// <param name="convertElement">The value-object converter applied to each element's underlying value.</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convertElement" /> is <c>null</c>.</exception>
    public RequiredField<IReadOnlyList<TProperty>> AsOptional<TProperty>(Func<TArgument, Outcome<TProperty>> convertElement) where TProperty : notnull {
        if (convertElement is null) { throw new ArgumentNullException(nameof(convertElement)); }

        if (_isMissing) {
            IReadOnlyList<TProperty> empty = new List<TProperty>();

            return new RequiredField<IReadOnlyList<TProperty>>(_binding, empty);
        }

        return ConvertElements(convertElement);
    }

    private RequiredField<IReadOnlyList<TProperty>> ConvertElements<TProperty>(Func<TArgument, Outcome<TProperty>> convertElement) where TProperty : notnull {
        return _binding.ConvertEachElement<TArgument?, TProperty>(this, _values!, _source, (element, index) => {
            Outcome<TProperty> outcome = convertElement(element!.Value);
            if (outcome.IsFailure) { _binding.RecordArgumentInvalid(ElementPathAt(index), outcome.Error!, _source); }

            return outcome;
        });
    }

    private string ElementPathAt(int index) {
        return $"{ArgumentPath()}[{index}]";
    }

    private string ArgumentPath() {
        return ArgumentPaths.Resolve(ref _argumentPathOrProperty, _binding);
    }

}
