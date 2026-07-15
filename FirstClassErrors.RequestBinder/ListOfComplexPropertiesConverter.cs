namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Binds a list request property whose elements are each bound by a nested binder. Every failing element
///     records its own envelope, whose inner errors carry the full, indexed argument paths
///     (<c>Guests[1].FirstName</c>) — so one bad element never hides the others.
/// </summary>
/// <typeparam name="TRequest">The type of the request DTO.</typeparam>
/// <typeparam name="TArgument">The element type of the DTO list.</typeparam>
public sealed class ListOfComplexPropertiesConverter<TRequest, TArgument> {

    #region Fields declarations

    private readonly RequestBinder<TRequest>                        _binder;
    private readonly string                                         _argumentPath;
    private readonly IEnumerable<TArgument?>?                       _values;
    private readonly bool                                           _isMissing;
    private readonly Func<PrimaryPortInnerErrors, PrimaryPortError> _envelope;

    #endregion

    #region Constructors declarations

    internal ListOfComplexPropertiesConverter(RequestBinder<TRequest>                        binder,
                                              string                                         argumentPath,
                                              IEnumerable<TArgument?>?                       values,
                                              bool                                           isMissing,
                                              Func<PrimaryPortInnerErrors, PrimaryPortError> envelope) {
        _binder       = binder;
        _argumentPath = argumentPath;
        _values       = values;
        _isMissing    = isMissing;
        _envelope     = envelope;
    }

    #endregion

    /// <summary>
    ///     Binds a required list: a missing list records <c>REQUEST_ARGUMENT_REQUIRED</c>; each failing element
    ///     records its envelope under its indexed path.
    /// </summary>
    /// <typeparam name="TProperty">The type each element's nested binding produces.</typeparam>
    /// <param name="bindElement">The nested binding function applied to each element (typically a method group).</param>
    /// <returns>The bound property handle.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bindElement" /> is <c>null</c>.</exception>
    public RequiredProperty<IReadOnlyList<TProperty>> AsRequired<TProperty>(Func<RequestBinder<TArgument>, Outcome<TProperty>> bindElement) where TProperty : notnull {
        if (bindElement is null) { throw new ArgumentNullException(nameof(bindElement)); }

        if (_isMissing) {
            PrimaryPortError error = RequestBindingError.ArgumentRequired(_argumentPath);
            _binder.Record(error);

            return new RequiredProperty<IReadOnlyList<TProperty>>(default!, error);
        }

        return BindElements(bindElement);
    }

    /// <summary>
    ///     Binds an optional list: absent yields an <b>empty</b> list (never <c>null</c>) and records nothing; each
    ///     failing element of a present list still records its envelope under its indexed path.
    /// </summary>
    /// <typeparam name="TProperty">The type each element's nested binding produces.</typeparam>
    /// <param name="bindElement">The nested binding function applied to each element (typically a method group).</param>
    /// <returns>The bound property handle.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bindElement" /> is <c>null</c>.</exception>
    public RequiredProperty<IReadOnlyList<TProperty>> AsOptional<TProperty>(Func<RequestBinder<TArgument>, Outcome<TProperty>> bindElement) where TProperty : notnull {
        if (bindElement is null) { throw new ArgumentNullException(nameof(bindElement)); }

        if (_isMissing) {
            IReadOnlyList<TProperty> empty = new List<TProperty>();

            return new RequiredProperty<IReadOnlyList<TProperty>>(empty, failure: null);
        }

        return BindElements(bindElement);
    }

    private RequiredProperty<IReadOnlyList<TProperty>> BindElements<TProperty>(Func<RequestBinder<TArgument>, Outcome<TProperty>> bindElement) where TProperty : notnull {
        List<TProperty>   bound        = new();
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

            RequestBinder<TArgument> nested  = new(element!, _envelope, _binder.Options, elementPath);
            Outcome<TProperty>       outcome = bindElement(nested);
            if (outcome.IsFailure) {
                Error            error   = outcome.Error!;
                PrimaryPortError grouped = error as PrimaryPortError ?? RequestBindingError.ArgumentInvalid(elementPath, error);
                _binder.Record(grouped);
                firstFailure ??= grouped;

                continue;
            }

            bound.Add(outcome.GetResultOrThrow());
        }

        if (firstFailure is not null) {
            return new RequiredProperty<IReadOnlyList<TProperty>>(default!, firstFailure);
        }

        return new RequiredProperty<IReadOnlyList<TProperty>>(bound, failure: null);
    }

}
