namespace FirstClassErrors.RequestBinder;

/// <summary>
///     A token standing for a bound required property (or an optional property with a fallback). It carries no public
///     value: the bound value is reachable only through
///     <see cref="BindingScope.Get{TProperty}(RequiredField{TProperty})" />, inside a build terminal
///     (<see cref="RequestBinder.New{TCommand}" /> / <see cref="RequestBinder.Create{TCommand}" />)
///     — where every binding is known to have succeeded.
/// </summary>
/// <remarks>
///     Returned by the <c>AsRequired</c> family. When the binding failed, the failure has already been recorded on the
///     binder (so it surfaces in the envelope built by the terminal) and this token stands in with no readable value —
///     harmless, because the terminal never runs the assembler when any failure was recorded.
/// </remarks>
/// <typeparam name="TProperty">The type of the bound property.</typeparam>
public sealed class RequiredField<TProperty> {

    #region Fields declarations

    private readonly TProperty _value;

    #endregion

    #region Constructors declarations

    internal RequiredField(object owner, TProperty value) {
        Owner  = owner;
        _value = value;
    }

    #endregion

    /// <summary>The binder that produced this token; used to reject a token read through a different binder's scope.</summary>
    internal object Owner { get; }

    /// <summary>The bound value. Internal: consumers reach it only through <see cref="BindingScope" />.</summary>
    internal TProperty Value => _value;

}
