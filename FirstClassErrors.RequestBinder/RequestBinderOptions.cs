namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The binding options of a <see cref="RequestBinder{TRequest}" />. Options are fixed once, before binding
///     begins — through <see cref="Bind.WithOptions" /> — and inherited by nested binders; they are never global
///     mutable state and can never change while a binder is binding.
/// </summary>
public sealed class RequestBinderOptions {

    #region Statics members declarations

    /// <summary>The default options: argument names are the C# property names, structural codes are the defaults.</summary>
    public static RequestBinderOptions Default { get; } = new(new DefaultArgumentNameProvider());

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

}
