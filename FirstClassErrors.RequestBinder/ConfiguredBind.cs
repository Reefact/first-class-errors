namespace FirstClassErrors.RequestBinder;

/// <summary>
///     An options-configured request-binding entry point, produced by <see cref="Bind.WithOptions" />. It fixes the
///     <see cref="RequestBinderOptions" /> once, before any binding begins, so a binder's naming policy can never
///     change mid-binding. It carries no per-request state, so a single instance can be created once (for example at
///     application setup) and reused for every request.
/// </summary>
public sealed class ConfiguredBind {

    #region Fields declarations

    private readonly RequestBinderOptions _options;

    #endregion

    #region Constructors declarations

    internal ConfiguredBind(RequestBinderOptions options) {
        _options = options;
    }

    #endregion

    /// <summary>
    ///     Starts binding the properties of a request DTO with the configured options. Declare the failure envelope
    ///     next, with <see cref="RequestBinderEnvelopeStage{TRequest}.FailWith" />.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request DTO.</typeparam>
    /// <param name="request">The request DTO to bind.</param>
    /// <returns>The stage on which the failure envelope is declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is <c>null</c>.</exception>
    public RequestBinderEnvelopeStage<TRequest> PropertiesOf<TRequest>(TRequest request) {
        if (request is null) { throw new ArgumentNullException(nameof(request)); }

        return new RequestBinderEnvelopeStage<TRequest>(request, _options);
    }

}
