namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Binds a list request property whose elements are converted by a plain value-object converter. Each failing
///     element is recorded individually, under its indexed path (<c>Tags[2]</c>), so one bad element never hides the
///     others.
/// </summary>
/// <typeparam name="TRequest">The type of the request DTO.</typeparam>
/// <typeparam name="TArgument">The element type of the DTO list.</typeparam>
public sealed class ListOfSimplePropertiesConverter<TRequest, TArgument> {

    #region Fields declarations

    private readonly RequestBinder<TRequest>  _binder;
    private readonly string                   _argumentPath;
    private readonly IEnumerable<TArgument?>? _values;
    private readonly bool                     _isMissing;

    #endregion

    #region Constructors declarations

    internal ListOfSimplePropertiesConverter(RequestBinder<TRequest> binder, string argumentPath, IEnumerable<TArgument?>? values, bool isMissing) {
        _binder       = binder;
        _argumentPath = argumentPath;
        _values       = values;
        _isMissing    = isMissing;
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
            _binder.RecordArgumentRequired(_argumentPath);

            return new RequiredField<IReadOnlyList<TProperty>>(_binder, default!);
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

            return new RequiredField<IReadOnlyList<TProperty>>(_binder, empty);
        }

        return ConvertElements(convertElement);
    }

    private RequiredField<IReadOnlyList<TProperty>> ConvertElements<TProperty>(Func<TArgument, Outcome<TProperty>> convertElement) where TProperty : notnull {
        return _binder.ConvertEachElement<TArgument?, TProperty>(_argumentPath, _values!, (element, elementPath) => {
            Outcome<TProperty> outcome = convertElement(element!);
            if (outcome.IsFailure) { _binder.RecordArgumentInvalid(elementPath, outcome.Error!); }

            return outcome;
        });
    }

}
