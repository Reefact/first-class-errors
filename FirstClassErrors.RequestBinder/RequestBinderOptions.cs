namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The binding options of a <see cref="RequestBinder{TRequest}" />. Options are fixed once, before binding
///     begins — through <see cref="Bind.WithOptions" /> — and inherited by nested binders; they are never global
///     mutable state and can never change while a binder is binding.
/// </summary>
public sealed class RequestBinderOptions {

    #region Statics members declarations

    /// <summary>The default options: argument names are the C# property names.</summary>
    public static RequestBinderOptions Default { get; } = new(new DefaultArgumentNameProvider());

    #endregion

    #region Constructors declarations

    /// <summary>Instantiates options with the given argument-name provider.</summary>
    /// <param name="argumentNameProvider">The provider resolving the argument name of a bound DTO property.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="argumentNameProvider" /> is <c>null</c>.</exception>
    public RequestBinderOptions(IArgumentNameProvider argumentNameProvider) {
        if (argumentNameProvider is null) { throw new ArgumentNullException(nameof(argumentNameProvider)); }

        ArgumentNameProvider = argumentNameProvider;
    }

    #endregion

    /// <summary>The provider resolving the argument name of a bound DTO property (see <see cref="IArgumentNameProvider" />).</summary>
    public IArgumentNameProvider ArgumentNameProvider { get; }

}
