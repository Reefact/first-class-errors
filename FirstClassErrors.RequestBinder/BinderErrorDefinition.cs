namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The definition of one of the binder's structural errors: its <see cref="Code" /> and its public messages, kept
///     together so a consumer overrides them as one coherent unit — never a code stranded from its message. The message
///     is a builder over the failing argument path, invoked <b>when the error is raised</b> (not when the options are
///     built), so it may read the ambient culture (<c>CultureInfo.CurrentUICulture</c>) and return a localized message
///     per request, consistent with the library's internationalization pattern.
/// </summary>
/// <remarks>
///     The type is immutable: <see cref="WithCode" /> and <see cref="WithMessage" /> return a new definition rather than
///     mutating this one, so deriving a consumer override from a shared default (for example
///     <see cref="RequestBindingError.DefaultArgumentRequired" />) never alters the default other callers see.
/// </remarks>
public sealed class BinderErrorDefinition {

    #region Fields declarations

    private readonly Func<string, BindingMessage> _message;

    #endregion

    #region Constructors declarations

    /// <summary>Instantiates a structural-error definition from its code and its public-message builder.</summary>
    /// <param name="code">The structural error code raised for this failure.</param>
    /// <param name="message">
    ///     Builds the public messages for a given argument path. Invoked at error emission, so it may resolve
    ///     culture-specific resources; it must not be <c>null</c>, but the <see cref="BindingMessage" /> it returns is
    ///     taken as-is (a missing short message is coalesced downstream, never rejected).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="code" /> or <paramref name="message" /> is <c>null</c>.</exception>
    public BinderErrorDefinition(ErrorCode code, Func<string, BindingMessage> message) {
        if (code is null) { throw new ArgumentNullException(nameof(code)); }
        if (message is null) { throw new ArgumentNullException(nameof(message)); }

        Code     = code;
        _message = message;
    }

    #endregion

    /// <summary>The structural error code raised for this failure.</summary>
    public ErrorCode Code { get; }

    /// <summary>Builds the public messages for the failing argument at <paramref name="argumentPath" />.</summary>
    /// <param name="argumentPath">The full path of the failing argument (for example <c>Guests[1].FirstName</c>).</param>
    /// <returns>The public messages to attach to the raised error.</returns>
    public BindingMessage GetMessage(string argumentPath) {
        return _message(argumentPath);
    }

    /// <summary>Returns a copy of this definition raising <paramref name="code" /> instead, keeping the same messages.</summary>
    /// <param name="code">The structural error code the copy raises.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="code" /> is <c>null</c>.</exception>
    public BinderErrorDefinition WithCode(ErrorCode code) {
        return new BinderErrorDefinition(code, _message);
    }

    /// <summary>Returns a copy of this definition building its messages with <paramref name="message" /> instead, keeping the same code.</summary>
    /// <param name="message">The public-message builder the copy uses.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message" /> is <c>null</c>.</exception>
    public BinderErrorDefinition WithMessage(Func<string, BindingMessage> message) {
        return new BinderErrorDefinition(Code, message);
    }

}
