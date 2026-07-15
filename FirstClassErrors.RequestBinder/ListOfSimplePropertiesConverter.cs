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
    ///     Binds a required list: a missing list records <c>REQUEST_ARGUMENT_REQUIRED</c>; each failing element
    ///     records <c>REQUEST_ARGUMENT_INVALID</c> under its indexed path.
    /// </summary>
    /// <typeparam name="TProperty">The type of the element value object.</typeparam>
    /// <param name="convertElement">The value-object converter applied to each element.</param>
    /// <returns>The bound property handle.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convertElement" /> is <c>null</c>.</exception>
    public RequiredProperty<IReadOnlyList<TProperty>> AsRequired<TProperty>(Func<TArgument, Outcome<TProperty>> convertElement) where TProperty : notnull {
        if (convertElement is null) { throw new ArgumentNullException(nameof(convertElement)); }

        if (_isMissing) {
            PrimaryPortError error = RequestBindingError.ArgumentRequired(_argumentPath);
            _binder.Record(error);

            return new RequiredProperty<IReadOnlyList<TProperty>>(default!, error);
        }

        return ConvertElements(convertElement);
    }

    /// <summary>
    ///     Binds an optional list: absent yields an <b>empty</b> list (never <c>null</c>) and records nothing; each
    ///     failing element of a present list still records <c>REQUEST_ARGUMENT_INVALID</c> under its indexed path.
    /// </summary>
    /// <typeparam name="TProperty">The type of the element value object.</typeparam>
    /// <param name="convertElement">The value-object converter applied to each element.</param>
    /// <returns>The bound property handle.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convertElement" /> is <c>null</c>.</exception>
    public RequiredProperty<IReadOnlyList<TProperty>> AsOptional<TProperty>(Func<TArgument, Outcome<TProperty>> convertElement) where TProperty : notnull {
        if (convertElement is null) { throw new ArgumentNullException(nameof(convertElement)); }

        if (_isMissing) {
            IReadOnlyList<TProperty> empty = new List<TProperty>();

            return new RequiredProperty<IReadOnlyList<TProperty>>(empty, failure: null);
        }

        return ConvertElements(convertElement);
    }

    private RequiredProperty<IReadOnlyList<TProperty>> ConvertElements<TProperty>(Func<TArgument, Outcome<TProperty>> convertElement) where TProperty : notnull {
        List<TProperty>   converted    = new();
        PrimaryPortError? firstFailure = null;
        int               index        = 0;

        foreach (TArgument? element in _values!) {
            string elementPath = $"{_argumentPath}[{index}]";
            index++;

            if (element is null) {
                // Every failing element is recorded (collect-all); the FIRST one also seeds the handle's outcome.
                PrimaryPortError missing = RequestBindingError.ArgumentRequired(elementPath);
                _binder.Record(missing);
                firstFailure ??= missing;

                continue;
            }

            Outcome<TProperty> outcome = convertElement(element!);
            if (outcome.IsFailure) {
                PrimaryPortError invalid = RequestBindingError.ArgumentInvalid(elementPath, outcome.Error!);
                _binder.Record(invalid);
                firstFailure ??= invalid;

                continue;
            }

            converted.Add(outcome.GetResultOrThrow());
        }

        if (firstFailure is not null) {
            return new RequiredProperty<IReadOnlyList<TProperty>>(default!, firstFailure);
        }

        return new RequiredProperty<IReadOnlyList<TProperty>>(converted, failure: null);
    }

}
