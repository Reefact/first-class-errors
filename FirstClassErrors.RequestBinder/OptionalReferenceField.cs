namespace FirstClassErrors.RequestBinder;

/// <summary>
///     A token standing for a bound optional reference-type property. Read through
///     <see cref="BindingScope.Get{TProperty}(OptionalReferenceField{TProperty})" />, its value is the bound value, or
///     <c>null</c> when the argument was absent from the request.
/// </summary>
/// <remarks>
///     Returned by the <c>AsOptionalReference</c> family. Inside <see cref="RequestBinder{TRequest}.Build{TCommand}" />
///     a <c>null</c> read means exactly "the argument was absent": a present-but-invalid argument recorded a failure on
///     the binder, so <c>Build</c> never runs the assembler and that state is never observed.
/// </remarks>
/// <typeparam name="TProperty">The reference type of the bound property.</typeparam>
public sealed class OptionalReferenceField<TProperty> where TProperty : class {

    #region Fields declarations

    private readonly TProperty? _value;

    #endregion

    #region Constructors declarations

    internal OptionalReferenceField(object owner, TProperty? value) {
        Owner  = owner;
        _value = value;
    }

    #endregion

    /// <summary>The binder that produced this token; used to reject a token read through a different binder's scope.</summary>
    internal object Owner { get; }

    /// <summary>The bound value, or <c>null</c> when absent. Internal: consumers reach it only through <see cref="BindingScope" />.</summary>
    internal TProperty? Value => _value;

}
