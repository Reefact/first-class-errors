namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The mandatory envelope stage of a list of complex properties: declares the envelope error each failing
///     element's nested binding is grouped into, before <c>AsRequired</c> / <c>AsOptional</c> become available.
/// </summary>
/// <typeparam name="TRequest">The type of the request DTO.</typeparam>
/// <typeparam name="TArgument">The element type of the DTO list.</typeparam>
public sealed class ListOfComplexPropertiesEnvelopeStage<TRequest, TArgument> {

    #region Fields declarations

    private readonly RequestBinder<TRequest>  _binder;
    private readonly string                   _argumentPath;
    private readonly IEnumerable<TArgument?>? _values;
    private readonly bool                     _isMissing;

    #endregion

    #region Constructors declarations

    internal ListOfComplexPropertiesEnvelopeStage(RequestBinder<TRequest> binder, string argumentPath, IEnumerable<TArgument?>? values, bool isMissing) {
        _binder       = binder;
        _argumentPath = argumentPath;
        _values       = values;
        _isMissing    = isMissing;
    }

    #endregion

    /// <summary>
    ///     Declares the per-element envelope: the factory producing the <see cref="PrimaryPortError" /> under which
    ///     each failing element's nested-binding failures are grouped.
    /// </summary>
    /// <param name="envelope">The per-element envelope factory, receiving the collected failures of one element.</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope" /> is <c>null</c>.</exception>
    public ListOfComplexPropertiesConverter<TRequest, TArgument> FailWith(Func<PrimaryPortInnerErrors, PrimaryPortError> envelope) {
        if (envelope is null) { throw new ArgumentNullException(nameof(envelope)); }

        return new ListOfComplexPropertiesConverter<TRequest, TArgument>(_binder, _argumentPath, _values, _isMissing, envelope);
    }

}
