namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Binds a list property whose elements are each bound by a nested binder. Every failing element records its own
///     envelope, whose inner errors carry the full, indexed argument paths (<c>Guests[1].FirstName</c>) — so one bad
///     element never hides the others.
/// </summary>
/// <typeparam name="TArgument">The element type of the DTO list.</typeparam>
public sealed class ListOfComplexPropertiesConverter<TArgument> : IElementPathSource {

    #region Fields declarations

    private readonly RequestBinding                                 _binding;
    private          object                                         _argumentPathOrProperty;
    private readonly IEnumerable<TArgument?>?                       _values;
    private readonly bool                                           _isMissing;
    private readonly Func<PrimaryPortInnerErrors, PrimaryPortError> _envelope;

    #endregion

    #region Constructors declarations

    internal ListOfComplexPropertiesConverter(RequestBinding                                 binding,
                                              object                                         argumentPathOrProperty,
                                              IEnumerable<TArgument?>?                       values,
                                              bool                                           isMissing,
                                              Func<PrimaryPortInnerErrors, PrimaryPortError> envelope) {
        _binding                = binding;
        _argumentPathOrProperty = argumentPathOrProperty;
        _values                 = values;
        _isMissing              = isMissing;
        _envelope               = envelope;
    }

    #endregion

    /// <inheritdoc />
    string IElementPathSource.ElementPathAt(int index) {
        return ElementPathAt(index);
    }

    /// <summary>
    ///     Binds a required list: only an <b>absent</b> (<c>null</c>) list records <c>REQUEST_ARGUMENT_REQUIRED</c> —
    ///     a list that is <b>present but empty</b> is valid and binds an empty list, because a required list
    ///     constrains the list's <b>presence</b>, not its element count. Each failing element records its envelope
    ///     under its indexed path.
    /// </summary>
    /// <typeparam name="TProperty">The type each element's nested binding produces.</typeparam>
    /// <param name="bindElement">The nested binding function, receiving the child binder and the element DTO (typically a method group).</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bindElement" /> is <c>null</c>.</exception>
    public RequiredField<IReadOnlyList<TProperty>> AsRequired<TProperty>(Func<RequestBinder, TArgument, Outcome<TProperty>> bindElement) where TProperty : notnull {
        if (bindElement is null) { throw new ArgumentNullException(nameof(bindElement)); }

        if (_isMissing) {
            _binding.RecordArgumentRequired(ArgumentPath());

            return new RequiredField<IReadOnlyList<TProperty>>(_binding, default!);
        }

        return BindElements(bindElement);
    }

    /// <summary>
    ///     Binds an optional list: absent yields an <b>empty</b> list (never <c>null</c>) and records nothing; each
    ///     failing element of a present list still records its envelope under its indexed path.
    /// </summary>
    /// <typeparam name="TProperty">The type each element's nested binding produces.</typeparam>
    /// <param name="bindElement">The nested binding function, receiving the child binder and the element DTO (typically a method group).</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bindElement" /> is <c>null</c>.</exception>
    public RequiredField<IReadOnlyList<TProperty>> AsOptional<TProperty>(Func<RequestBinder, TArgument, Outcome<TProperty>> bindElement) where TProperty : notnull {
        if (bindElement is null) { throw new ArgumentNullException(nameof(bindElement)); }

        if (_isMissing) {
            IReadOnlyList<TProperty> empty = new List<TProperty>();

            return new RequiredField<IReadOnlyList<TProperty>>(_binding, empty);
        }

        return BindElements(bindElement);
    }

    private RequiredField<IReadOnlyList<TProperty>> BindElements<TProperty>(Func<RequestBinder, TArgument, Outcome<TProperty>> bindElement) where TProperty : notnull {
        return _binding.ConvertEachElement<TArgument?, TProperty>(this, _values!, source: null, (element, index) => {
            // The element's binding defers its own prefix ("Guests[2]") through this converter: it materializes
            // only when a path inside the element is first needed — a recorded failure, a nested complex prefix,
            // or an Argument name — never for an element whose properties all bind through simple converters.
            RequestBinding     nested  = new(_envelope, _binding.Options, this, index);
            Outcome<TProperty> outcome = bindElement(new RequestBinder(nested), element!);
            if (outcome.IsFailure) {
                _binding.Record(NestedFailure.Group(outcome.Error!, nested.BuiltEnvelope, ElementPathAt(index), _binding.Options.ArgumentInvalid));
            }

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
