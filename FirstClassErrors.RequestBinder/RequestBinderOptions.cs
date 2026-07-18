namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The binding options of a <see cref="RequestBinder{TRequest}" />. A binder's options are fixed once, before
///     binding begins — through <see cref="Bind.WithOptions" /> or the application-wide <see cref="Default" /> — and
///     inherited by nested binders; they never change while a binder is binding. The process-wide
///     <see cref="Default" /> may be configured once at application startup and is frozen on first use.
/// </summary>
public sealed class RequestBinderOptions {

    #region Statics members declarations

    private static readonly RequestBinderOptions                     BuiltIn      = new(new DefaultArgumentNameProvider());
    private static readonly AsyncLocal<RequestBinderOptions?>        TestOverride = new();
    private static          RequestBinderOptions                     _default     = BuiltIn;
    private static          bool                                     _frozen;

    /// <summary>
    ///     The application-wide default options that <see cref="Bind.PropertiesOf{TRequest}" /> binds with. Assign it
    ///     once at application startup — before any binding — to configure the binder host-wide without threading
    ///     options through every call; a per-call <see cref="Bind.WithOptions" /> still overrides it. The first bind
    ///     reads it and thereby <b>freezes</b> it, so the default cannot drift once binding has begun. Defaults to the
    ///     built-in options (C# property names and the default structural codes).
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when set to <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when set after the first bind has already read it.</exception>
    public static RequestBinderOptions Default {
        get {
            RequestBinderOptions? overridden = TestOverride.Value;
            if (overridden is not null) { return overridden; }

            _frozen = true;

            return _default;
        }
        set {
            if (value is null) { throw new ArgumentNullException(nameof(value)); }
            if (_frozen) {
                throw new InvalidOperationException(
                    "RequestBinderOptions.Default was already read by a binding; configure it once at application startup, before the first bind.");
            }

            _default = value;
        }
    }

    /// <summary>
    ///     Test-only seam: overrides <see cref="Default" /> for the current execution context until the returned scope
    ///     is disposed. Backed by an <see cref="AsyncLocal{T}" />, so it flows with the test's context and never leaks
    ///     across tests running in parallel — the same pattern as the ambient clock. It does not touch the production
    ///     default and never freezes it. Internal, for the library's own tests; a consumer-facing seam belongs in a
    ///     dedicated testing package.
    /// </summary>
    /// <param name="options">The options to bind with while the scope is active.</param>
    /// <returns>A scope that restores the previous override when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <c>null</c>.</exception>
    internal static IDisposable OverrideDefaultForTests(RequestBinderOptions options) {
        if (options is null) { throw new ArgumentNullException(nameof(options)); }

        RequestBinderOptions? previous = TestOverride.Value;
        TestOverride.Value = options;

        return new TestOverrideScope(previous);
    }

    #endregion

    #region Constructors declarations

    /// <summary>Instantiates options with the given argument-name provider and, optionally, custom structural error codes.</summary>
    /// <param name="argumentNameProvider">The provider resolving the argument name of a bound DTO property.</param>
    /// <param name="argumentRequiredCode">
    ///     The code raised when a required argument is missing. <c>null</c> keeps the default
    ///     <see cref="RequestBindingError.DefaultArgumentRequiredCode" /> (<c>REQUEST_ARGUMENT_REQUIRED</c>); pass a
    ///     code of your own catalog's convention to keep the binder's failures consistent with your other error codes.
    /// </param>
    /// <param name="argumentInvalidCode">
    ///     The code raised when an argument is present but fails to convert. <c>null</c> keeps the default
    ///     <see cref="RequestBindingError.DefaultArgumentInvalidCode" /> (<c>REQUEST_ARGUMENT_INVALID</c>).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="argumentNameProvider" /> is <c>null</c>.</exception>
    public RequestBinderOptions(IArgumentNameProvider argumentNameProvider, ErrorCode? argumentRequiredCode = null, ErrorCode? argumentInvalidCode = null) {
        if (argumentNameProvider is null) { throw new ArgumentNullException(nameof(argumentNameProvider)); }

        ArgumentNameProvider = argumentNameProvider;
        ArgumentRequiredCode = argumentRequiredCode ?? RequestBindingError.DefaultArgumentRequiredCode;
        ArgumentInvalidCode  = argumentInvalidCode  ?? RequestBindingError.DefaultArgumentInvalidCode;
    }

    #endregion

    /// <summary>The provider resolving the argument name of a bound DTO property (see <see cref="IArgumentNameProvider" />).</summary>
    public IArgumentNameProvider ArgumentNameProvider { get; }

    /// <summary>The code the binder raises when a required argument is missing (defaults to <c>REQUEST_ARGUMENT_REQUIRED</c>).</summary>
    public ErrorCode ArgumentRequiredCode { get; }

    /// <summary>The code the binder raises when an argument is present but fails to convert (defaults to <c>REQUEST_ARGUMENT_INVALID</c>).</summary>
    public ErrorCode ArgumentInvalidCode { get; }

    #region Nested types

    private sealed class TestOverrideScope : IDisposable {

        private readonly RequestBinderOptions? _previous;
        private          bool                  _disposed;

        internal TestOverrideScope(RequestBinderOptions? previous) {
            _previous = previous;
        }

        public void Dispose() {
            if (_disposed) { return; }

            _disposed          = true;
            TestOverride.Value = _previous;
        }

    }

    #endregion

}
