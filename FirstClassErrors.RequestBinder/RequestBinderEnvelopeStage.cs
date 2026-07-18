namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The mandatory first stage of a request binder: declares the envelope error every binding failure is grouped
///     into. Mirrors the staged-builder pattern the library uses for errors themselves (an error can never be left
///     without its public message; a binder can never be left without its envelope).
/// </summary>
/// <typeparam name="TRequest">The type of the request DTO.</typeparam>
public sealed class RequestBinderEnvelopeStage<TRequest> {

    #region Fields declarations

    private readonly TRequest             _request;
    private readonly RequestBinderOptions _options;

    #endregion

    #region Constructors declarations

    internal RequestBinderEnvelopeStage(TRequest request, RequestBinderOptions options) {
        _request = request;
        _options = options;
    }

    #endregion

    /// <summary>
    ///     Declares the envelope: the factory producing the single <see cref="PrimaryPortError" /> under which every
    ///     failure recorded during the binding is grouped — typically an aggregate factory of the application's error
    ///     catalog, passed as a method group.
    /// </summary>
    /// <param name="envelope">The envelope factory, receiving the collected failures.</param>
    /// <returns>The request binder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope" /> is <c>null</c>.</exception>
    public RequestBinder<TRequest> FailWith(Func<PrimaryPortInnerErrors, PrimaryPortError> envelope) {
        if (envelope is null) { throw new ArgumentNullException(nameof(envelope)); }

        return new RequestBinder<TRequest>(_request, envelope, _options, argumentPrefix: null);
    }

}
