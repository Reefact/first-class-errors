namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The mandatory envelope stage of a complex property: declares the envelope error the nested binding's failures are
///     grouped into, before <c>AsRequired</c> / <c>AsOptionalReference</c> become available.
/// </summary>
/// <typeparam name="TArgument">The type of the nested DTO.</typeparam>
public sealed class ComplexPropertyEnvelopeStage<TArgument> {

    #region Fields declarations

    private readonly RequestBinding _binding;
    private readonly object         _argumentPathOrProperty;
    private readonly TArgument?     _value;
    private readonly bool           _isMissing;

    #endregion

    #region Constructors declarations

    /// <param name="binding">The binding failures are recorded into.</param>
    /// <param name="argumentPathOrProperty">The resolved path or the selected property, deferred as in <see cref="ArgumentPaths" />.</param>
    /// <param name="value">The nested DTO.</param>
    /// <param name="isMissing">Whether the property was absent.</param>
    internal ComplexPropertyEnvelopeStage(RequestBinding binding, object argumentPathOrProperty, TArgument? value, bool isMissing) {
        _binding                = binding;
        _argumentPathOrProperty = argumentPathOrProperty;
        _value                  = value;
        _isMissing              = isMissing;
    }

    #endregion

    /// <summary>
    ///     Declares the nested envelope: the factory producing the <see cref="PrimaryPortError" /> under which the nested
    ///     binding's failures are grouped.
    /// </summary>
    /// <param name="envelope">The nested envelope factory, receiving the collected failures.</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptionalReference</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope" /> is <c>null</c>.</exception>
    public ComplexPropertyConverter<TArgument> FailWith(Func<PrimaryPortInnerErrors, PrimaryPortError> envelope) {
        if (envelope is null) { throw new ArgumentNullException(nameof(envelope)); }

        return new ComplexPropertyConverter<TArgument>(_binding, _argumentPathOrProperty, _value, _isMissing, envelope);
    }

}
