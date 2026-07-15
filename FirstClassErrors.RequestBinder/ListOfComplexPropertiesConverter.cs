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
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bindElement" /> is <c>null</c>.</exception>
    public RequiredField<IReadOnlyList<TProperty>> AsRequired<TProperty>(Func<RequestBinder<TArgument>, Outcome<TProperty>> bindElement) where TProperty : notnull {
        if (bindElement is null) { throw new ArgumentNullException(nameof(bindElement)); }

        if (_isMissing) {
            _binder.Record(RequestBindingError.ArgumentRequired(_argumentPath));

            return new RequiredField<IReadOnlyList<TProperty>>(_binder, default!);
        }

        return BindElements(bindElement);
    }

    /// <summary>
    ///     Binds an optional list: absent yields an <b>empty</b> list (never <c>null</c>) and records nothing; each
    ///     failing element of a present list still records its envelope under its indexed path.
    /// </summary>
    /// <typeparam name="TProperty">The type each element's nested binding produces.</typeparam>
    /// <param name="bindElement">The nested binding function applied to each element (typically a method group).</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bindElement" /> is <c>null</c>.</exception>
    public RequiredField<IReadOnlyList<TProperty>> AsOptional<TProperty>(Func<RequestBinder<TArgument>, Outcome<TProperty>> bindElement) where TProperty : notnull {
        if (bindElement is null) { throw new ArgumentNullException(nameof(bindElement)); }

        if (_isMissing) {
            IReadOnlyList<TProperty> empty = new List<TProperty>();

            return new RequiredField<IReadOnlyList<TProperty>>(_binder, empty);
        }

        return BindElements(bindElement);
    }

    private RequiredField<IReadOnlyList<TProperty>> BindElements<TProperty>(Func<RequestBinder<TArgument>, Outcome<TProperty>> bindElement) where TProperty : notnull {
        List<TProperty> bound = new();
        int             index = 0;

        foreach (TArgument? element in _values!) {
            string elementPath = $"{_argumentPath}[{index}]";
            index++;

            if (element is null) {
                _binder.Record(RequestBindingError.ArgumentRequired(elementPath));

                continue;
            }

            RequestBinder<TArgument> nested  = new(element!, _envelope, _binder.Options, elementPath);
            Outcome<TProperty>       outcome = bindElement(nested);
            if (outcome.IsFailure) {
                _binder.Record(NestedFailure.Group(outcome.Error!, nested.BuiltEnvelope, elementPath));

                continue;
            }

            bound.Add(outcome.GetResultOrThrow());
        }

        // The value is read only through a BindingScope, which Build creates solely when no failure was recorded —
        // i.e. only when every element bound — so `bound` is the complete list exactly when it is observed.
        return new RequiredField<IReadOnlyList<TProperty>>(_binder, bound);
    }

}
