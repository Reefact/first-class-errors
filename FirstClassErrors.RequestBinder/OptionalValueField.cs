namespace FirstClassErrors.RequestBinder;

/// <summary>
///     A token standing for a bound optional value-type property. Read through
///     <see cref="BindingScope.Get{TProperty}(OptionalValueField{TProperty})" />, its value is the bound value as a
///     real <see cref="Nullable{T}" /> — <c>null</c> when the argument was absent, never <c>default(T)</c> (an absent
///     count is <c>null</c>, not <c>0</c>).
/// </summary>
/// <remarks>
///     Returned by the <c>AsOptionalValue</c> family. Inside a build terminal
///     (<see cref="RequestBinder.New{TCommand}" /> / <see cref="RequestBinder.Create{TCommand}" />)
///     a <c>null</c> read means exactly "the argument was absent": a present-but-invalid argument recorded a failure on
///     the binder, so the assembler never runs and that state is never observed.
/// </remarks>
/// <typeparam name="TProperty">The value type of the bound property.</typeparam>
public sealed class OptionalValueField<TProperty> where TProperty : struct {

    #region Fields declarations

    private readonly TProperty? _value;

    #endregion

    #region Constructors declarations

    internal OptionalValueField(object owner, TProperty? value) {
        Owner  = owner;
        _value = value;
    }

    #endregion

    /// <summary>The binder that produced this token; used to reject a token read through a different binder's scope.</summary>
    internal object Owner { get; }

    /// <summary>The bound value, or <c>null</c> when absent. Internal: consumers reach it only through <see cref="BindingScope" />.</summary>
    internal TProperty? Value => _value;

}
