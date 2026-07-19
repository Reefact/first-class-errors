namespace FirstClassErrors.RequestBinder;

/// <summary>
///     An options-configured request-binding entry point, produced by <see cref="Bind.WithOptions" />. It fixes the
///     <see cref="RequestBinderOptions" /> once, before any binding begins, so a binder's naming policy can never change
///     mid-binding. It carries no per-request state, so a single instance can be created once (for example at application
///     setup) and reused for every request.
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
    ///     Starts binding a request with the configured options, declaring the failure envelope up front. Attach the
    ///     inputs next, through <see cref="RequestBinder.PropertiesOf{TDto}" /> and <see cref="RequestBinder.Argument" />,
    ///     and assemble the command or query with <see cref="RequestBinder.New{TCommand}" />.
    /// </summary>
    /// <param name="envelope">The envelope factory, receiving the collected failures.</param>
    /// <returns>The request binder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope" /> is <c>null</c>.</exception>
    public RequestBinder Request(Func<PrimaryPortInnerErrors, PrimaryPortError> envelope) {
        if (envelope is null) { throw new ArgumentNullException(nameof(envelope)); }

        return new RequestBinder(new RequestBinding(envelope, _options, argumentPrefix: null));
    }

}
