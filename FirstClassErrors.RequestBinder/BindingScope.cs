namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The reader handed to a build terminal's assembler (<see cref="RequestBinder{TRequest}.New{TCommand}" /> /
///     <see cref="RequestBinder{TRequest}.Create{TCommand}" />): the <b>only</b> channel through which a bound value can
///     be obtained from a field token (<see cref="RequiredField{TProperty}" /> and its optional siblings).
/// </summary>
/// <remarks>
///     <para>
///         <b>Safety by construction.</b> <see cref="BindingScope" /> is a <c>readonly ref struct</c>: it cannot be
///         captured, boxed, stored in a field, or returned, so it lives only for the duration of the assembler it is
///         passed to. And a build terminal (<see cref="RequestBinder{TRequest}.New{TCommand}" /> /
///         <see cref="RequestBinder{TRequest}.Create{TCommand}" />) creates one <b>only</b> on its success branch —
///         after it has verified that not a single binding failure was recorded. A field token exposes no public value
///         member, so the one way to read a bound value is <c>Get</c> through this scope, and this scope only ever
///         exists where every binding is known to have succeeded. Reading a value before a build terminal runs, or
///         outside its assembler, is therefore not merely discouraged: it does not compile.
///     </para>
///     <para>
///         A token produced by a <i>different</i> binder is rejected with <see cref="InvalidOperationException" />:
///         mixing tokens across binders is a programming error, so it throws loudly rather than reading a value this
///         scope never validated.
///     </para>
/// </remarks>
public readonly ref struct BindingScope {

    #region Fields declarations

    private readonly object _owner;

    #endregion

    #region Constructors declarations

    internal BindingScope(object owner) {
        _owner = owner;
    }

    #endregion

    /// <summary>Reads a required bound value.</summary>
    /// <typeparam name="TProperty">The type of the bound value.</typeparam>
    /// <param name="field">The token returned when the property was bound.</param>
    /// <returns>The bound value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="field" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="field" /> was produced by a different binder.</exception>
    public TProperty Get<TProperty>(RequiredField<TProperty> field) {
        if (field is null) { throw new ArgumentNullException(nameof(field)); }
        EnsureOwned(field.Owner);

        return field.Value;
    }

    /// <summary>Reads an optional reference-type bound value, or <c>null</c> when the argument was absent.</summary>
    /// <typeparam name="TProperty">The reference type of the bound value.</typeparam>
    /// <param name="field">The token returned when the property was bound.</param>
    /// <returns>The bound value, or <c>null</c> when the argument was absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="field" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="field" /> was produced by a different binder.</exception>
    public TProperty? Get<TProperty>(OptionalReferenceField<TProperty> field) where TProperty : class {
        if (field is null) { throw new ArgumentNullException(nameof(field)); }
        EnsureOwned(field.Owner);

        return field.Value;
    }

    /// <summary>Reads an optional value-type bound value as a real <see cref="Nullable{T}" /> — <c>null</c> when absent.</summary>
    /// <typeparam name="TProperty">The value type of the bound value.</typeparam>
    /// <param name="field">The token returned when the property was bound.</param>
    /// <returns>The bound value, or <c>null</c> when the argument was absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="field" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="field" /> was produced by a different binder.</exception>
    public TProperty? Get<TProperty>(OptionalValueField<TProperty> field) where TProperty : struct {
        if (field is null) { throw new ArgumentNullException(nameof(field)); }
        EnsureOwned(field.Owner);

        return field.Value;
    }

    private void EnsureOwned(object owner) {
        if (!ReferenceEquals(owner, _owner)) {
            throw new InvalidOperationException(
                "This bound field was produced by a different binder. A field can only be read inside the New/Create terminal of the binder that bound it.");
        }
    }

}
