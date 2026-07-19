namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The binding options of a <see cref="RequestBinder" />. A binder's options are fixed once, before
///     binding begins — through <see cref="Bind.WithOptions" /> or the application-wide <see cref="Default" /> — and
///     inherited by nested binders; they never change while a binder is binding. The process-wide
///     <see cref="Default" /> may be configured once at application startup and is frozen on first use.
/// </summary>
public sealed class RequestBinderOptions {

    #region Statics members declarations

    private static readonly RequestBinderOptions                     BuiltIn      = new(new DefaultArgumentNameProvider());
    private static readonly AsyncLocal<RequestBinderOptions?>        TestOverride = new();
    private static readonly object                                   Gate         = new();
    private static          RequestBinderOptions                     _default     = BuiltIn;
    private static          bool                                     _frozen;

    /// <summary>
    ///     The application-wide default options that <see cref="Bind.Request" /> binds with. Assign it
    ///     once at application startup — before any binding — to configure the binder host-wide without threading
    ///     options through every call; a per-call <see cref="Bind.WithOptions" /> still overrides it. The first bind
    ///     reads it and thereby <b>freezes</b> it, so the default cannot drift once binding has begun. Defaults to the
    ///     built-in options (C# property names and the default structural-error definitions).
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when set to <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when set after the first bind has already read it.</exception>
    public static RequestBinderOptions Default {
        get {
            RequestBinderOptions? overridden = TestOverride.Value;
            if (overridden is not null) { return overridden; }

            // Freeze and read under the gate: a concurrent set() either wins entirely (before any read froze the
            // default) or observes the freeze and throws — never a torn "first bind on the old default, later binds
            // on the new one". The gate is uncontended once the default is configured at startup.
            lock (Gate) {
                _frozen = true;

                return _default;
            }
        }
        set {
            if (value is null) { throw new ArgumentNullException(nameof(value)); }

            lock (Gate) {
                if (_frozen) {
                    throw new InvalidOperationException(
                        "RequestBinderOptions.Default was already read by a binding; configure it once at application startup, before the first bind.");
                }

                _default = value;
            }
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

    /// <summary>Instantiates options with the given argument-name provider and, optionally, custom structural-error definitions.</summary>
    /// <param name="argumentNameProvider">The provider resolving the argument name of a bound DTO property.</param>
    /// <param name="argumentRequired">
    ///     The definition — code and public messages, kept together — raised when a required argument is missing.
    ///     <c>null</c> keeps the default <see cref="RequestBindingError.DefaultArgumentRequired" />
    ///     (<c>REQUEST_ARGUMENT_REQUIRED</c> and its English messages). Derive from the default with
    ///     <see cref="BinderErrorDefinition.WithCode" /> / <see cref="BinderErrorDefinition.WithMessage" /> to align the
    ///     code with your catalog, localize the messages, or both — code and message stay one coherent unit.
    /// </param>
    /// <param name="argumentInvalid">
    ///     The definition — code and public messages, kept together — raised when an argument is present but fails to
    ///     convert. <c>null</c> keeps the default <see cref="RequestBindingError.DefaultArgumentInvalid" />
    ///     (<c>REQUEST_ARGUMENT_INVALID</c> and its English messages).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="argumentNameProvider" /> is <c>null</c>.</exception>
    public RequestBinderOptions(IArgumentNameProvider argumentNameProvider, BinderErrorDefinition? argumentRequired = null, BinderErrorDefinition? argumentInvalid = null) {
        if (argumentNameProvider is null) { throw new ArgumentNullException(nameof(argumentNameProvider)); }

        ArgumentNameProvider = argumentNameProvider;
        ArgumentRequired     = argumentRequired ?? RequestBindingError.DefaultArgumentRequired;
        ArgumentInvalid      = argumentInvalid  ?? RequestBindingError.DefaultArgumentInvalid;
    }

    #endregion

    /// <summary>The provider resolving the argument name of a bound DTO property (see <see cref="IArgumentNameProvider" />).</summary>
    public IArgumentNameProvider ArgumentNameProvider { get; }

    /// <summary>The definition — code and public messages — the binder raises when a required argument is missing (defaults to <see cref="RequestBindingError.DefaultArgumentRequired" />).</summary>
    public BinderErrorDefinition ArgumentRequired { get; }

    /// <summary>The definition — code and public messages — the binder raises when an argument is present but fails to convert (defaults to <see cref="RequestBindingError.DefaultArgumentInvalid" />).</summary>
    public BinderErrorDefinition ArgumentInvalid { get; }

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
