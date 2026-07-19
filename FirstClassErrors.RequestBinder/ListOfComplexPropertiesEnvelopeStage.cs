namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The mandatory envelope stage of a list of complex properties: declares the envelope error each failing element's
///     nested binding is grouped into, before <c>AsRequired</c> / <c>AsOptional</c> become available.
/// </summary>
/// <typeparam name="TArgument">The element type of the DTO list.</typeparam>
public sealed class ListOfComplexPropertiesEnvelopeStage<TArgument> {

    #region Fields declarations

    private readonly RequestBinding           _binding;
    private readonly object                   _argumentPathOrProperty;
    private readonly IEnumerable<TArgument?>? _values;
    private readonly bool                     _isMissing;

    #endregion

    #region Constructors declarations

    /// <param name="binding">The binding failures are recorded into.</param>
    /// <param name="argumentPathOrProperty">The resolved path or the selected property, deferred as in <see cref="ArgumentPaths" />.</param>
    /// <param name="values">The list elements.</param>
    /// <param name="isMissing">Whether the property was absent.</param>
    internal ListOfComplexPropertiesEnvelopeStage(RequestBinding binding, object argumentPathOrProperty, IEnumerable<TArgument?>? values, bool isMissing) {
        _binding                = binding;
        _argumentPathOrProperty = argumentPathOrProperty;
        _values                 = values;
        _isMissing              = isMissing;
    }

    #endregion

    /// <summary>
    ///     Declares the per-element envelope: the factory producing the <see cref="PrimaryPortError" /> under which each
    ///     failing element's nested-binding failures are grouped.
    /// </summary>
    /// <param name="envelope">The per-element envelope factory, receiving the collected failures of one element.</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope" /> is <c>null</c>.</exception>
    public ListOfComplexPropertiesConverter<TArgument> FailWith(Func<PrimaryPortInnerErrors, PrimaryPortError> envelope) {
        if (envelope is null) { throw new ArgumentNullException(nameof(envelope)); }

        return new ListOfComplexPropertiesConverter<TArgument>(_binding, _argumentPathOrProperty, _values, _isMissing, envelope);
    }

}
