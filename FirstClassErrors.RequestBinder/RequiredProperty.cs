namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The result of binding a required property (or an optional property with a fallback): a handle whose
///     <see cref="Value" /> is meant to be read inside <see cref="RequestBinder{TRequest}.Build{TCommand}" />.
/// </summary>
/// <remarks>
///     <see cref="Value" /> is a pure property: the binding outcome is unwrapped <b>once</b>, by the binder, at
///     binding time — never lazily by the property itself. When the binding failed, the failure has already been
///     recorded on the binder, so <see cref="RequestBinder{TRequest}.Build{TCommand}" /> never runs the assembly
///     function and the (default) value is never observed. Reading <see cref="Value" /> therefore never throws.
/// </remarks>
/// <typeparam name="TProperty">The type of the bound property.</typeparam>
public sealed class RequiredProperty<TProperty> {

    #region Fields declarations

    private readonly TProperty _value;
    private readonly Error?    _failure;

    #endregion

    #region Constructors declarations

    internal RequiredProperty(TProperty value, Error? failure) {
        _value   = value;
        _failure = failure;
    }

    #endregion

    /// <summary>Gets the bound value. Safe by construction inside <c>Build</c> (see the class remarks).</summary>
    public TProperty Value => _value;

    /// <summary>The failure recorded for this property, or <c>null</c> when it bound successfully. Kept for inspection.</summary>
    internal Error? Failure => _failure;

}
