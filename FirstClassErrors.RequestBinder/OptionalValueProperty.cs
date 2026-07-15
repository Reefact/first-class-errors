namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The result of binding an optional value-type property without a fallback: <see cref="Value" /> is the bound
///     value, or <c>null</c> when the argument was absent from the request — a real <see cref="Nullable{T}" />
///     <c>null</c>, never <c>default(T)</c> (an absent count is <c>null</c>, not <c>0</c>).
/// </summary>
/// <remarks>
///     <see cref="Value" /> is a pure property (see <see cref="RequiredProperty{TProperty}" /> for the guarantee).
///     Inside <see cref="RequestBinder{TRequest}.Build{TCommand}" />, <c>Value == null</c> means exactly "the
///     argument was absent": an argument that was present but invalid has recorded an error on the binder, so
///     <c>Build</c> never runs and that value is never observed.
/// </remarks>
/// <typeparam name="TProperty">The value type of the bound property.</typeparam>
public sealed class OptionalValueProperty<TProperty> where TProperty : struct {

    #region Fields declarations

    private readonly TProperty? _value;
    private readonly Error?     _failure;

    #endregion

    #region Constructors declarations

    internal OptionalValueProperty(TProperty? value, Error? failure) {
        _value   = value;
        _failure = failure;
    }

    #endregion

    /// <summary>Gets the bound value, or <c>null</c> when the argument was absent from the request.</summary>
    public TProperty? Value => _value;

    /// <summary>The failure recorded for this property, or <c>null</c> when it bound successfully or was absent. Kept for inspection.</summary>
    internal Error? Failure => _failure;

}
