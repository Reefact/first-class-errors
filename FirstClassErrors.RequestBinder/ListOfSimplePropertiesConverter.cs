namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Binds a list input — a DTO list property or an out-of-DTO list argument — whose elements are converted by a plain
///     value-object converter. Each failing element is recorded individually, under its indexed path (<c>Tags[2]</c>), so
///     one bad element never hides the others.
/// </summary>
/// <typeparam name="TArgument">The raw element type of the list.</typeparam>
public sealed class ListOfSimplePropertiesConverter<TArgument> {

    #region Fields declarations

    private readonly RequestBinding            _binding;
    private readonly string                    _argumentPath;
    private readonly IEnumerable<TArgument?>?  _values;
    private readonly bool                      _isMissing;
    private readonly string?                   _source;

    #endregion

    #region Constructors declarations

    internal ListOfSimplePropertiesConverter(RequestBinding binding, string argumentPath, IEnumerable<TArgument?>? values, bool isMissing, string? source) {
        _binding      = binding;
        _argumentPath = argumentPath;
        _values       = values;
        _isMissing    = isMissing;
        _source       = source;
    }

    #endregion

    /// <summary>
    ///     Binds a required list: only an <b>absent</b> (<c>null</c>) list records <c>REQUEST_ARGUMENT_REQUIRED</c> —
    ///     a list that is <b>present but empty</b> is valid and binds an empty list, because a required list
    ///     constrains the list's <b>presence</b>, not its element count. Each failing element records
    ///     <c>REQUEST_ARGUMENT_INVALID</c> under its indexed path.
    /// </summary>
    /// <typeparam name="TProperty">The type of the element value object.</typeparam>
    /// <param name="convertElement">The value-object converter applied to each element.</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convertElement" /> is <c>null</c>.</exception>
    public RequiredField<IReadOnlyList<TProperty>> AsRequired<TProperty>(Func<TArgument, Outcome<TProperty>> convertElement) where TProperty : notnull {
        if (convertElement is null) { throw new ArgumentNullException(nameof(convertElement)); }

        if (_isMissing) {
            _binding.RecordArgumentRequired(_argumentPath, _source);

            return new RequiredField<IReadOnlyList<TProperty>>(_binding, default!);
        }

        return ConvertElements(convertElement);
    }

    /// <summary>
    ///     Binds an optional list: absent yields an <b>empty</b> list (never <c>null</c>) and records nothing; each
    ///     failing element of a present list still records <c>REQUEST_ARGUMENT_INVALID</c> under its indexed path.
    /// </summary>
    /// <typeparam name="TProperty">The type of the element value object.</typeparam>
    /// <param name="convertElement">The value-object converter applied to each element.</param>
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
        return _binding.ConvertEachElement<TArgument?, TProperty>(_argumentPath, _values!, _source, (element, elementPath) => {
            Outcome<TProperty> outcome = convertElement(element!);
            if (outcome.IsFailure) { _binding.RecordArgumentInvalid(elementPath, outcome.Error!, _source); }

            return outcome;
        });
    }

}
