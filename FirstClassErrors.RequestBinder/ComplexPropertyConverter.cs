namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Binds a complex request property through a nested binder: the binding function receives a child
///     <see cref="RequestBinder{TArgument}" /> — prefixed with this property's path, inheriting the parent options,
///     failing with the envelope declared on the previous stage — and typically lives in a dedicated method, passed
///     as a method group.
/// </summary>
/// <typeparam name="TRequest">The type of the request DTO.</typeparam>
/// <typeparam name="TArgument">The type of the nested DTO.</typeparam>
public sealed class ComplexPropertyConverter<TRequest, TArgument> {

    #region Fields declarations

    private readonly RequestBinder<TRequest>                        _binder;
    private readonly string                                         _argumentPath;
    private readonly TArgument?                                     _value;
    private readonly bool                                           _isMissing;
    private readonly Func<PrimaryPortInnerErrors, PrimaryPortError> _envelope;

    #endregion

    #region Constructors declarations

    internal ComplexPropertyConverter(RequestBinder<TRequest>                        binder,
                                      string                                         argumentPath,
                                      TArgument?                                     value,
                                      bool                                           isMissing,
                                      Func<PrimaryPortInnerErrors, PrimaryPortError> envelope) {
        _binder       = binder;
        _argumentPath = argumentPath;
        _value        = value;
        _isMissing    = isMissing;
        _envelope     = envelope;
    }

    #endregion

    /// <summary>
    ///     Binds a required complex argument: missing records <c>REQUEST_ARGUMENT_REQUIRED</c>; a failed nested
    ///     binding records its envelope, whose inner errors carry the full, prefixed argument paths.
    /// </summary>
    /// <typeparam name="TProperty">The type the nested binding produces.</typeparam>
    /// <param name="bindNested">The nested binding function (typically a method group such as <c>BindStay</c>).</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bindNested" /> is <c>null</c>.</exception>
    public RequiredField<TProperty> AsRequired<TProperty>(Func<RequestBinder<TArgument>, Outcome<TProperty>> bindNested) where TProperty : notnull {
        if (bindNested is null) { throw new ArgumentNullException(nameof(bindNested)); }

        if (_isMissing) {
            _binder.Record(RequestBindingError.ArgumentRequired(_argumentPath));

            return new RequiredField<TProperty>(_binder, default!);
        }

        Outcome<TProperty> outcome = BindNested(bindNested);
        if (outcome.IsFailure) {
            _binder.Record(Grouped(outcome.Error!));

            return new RequiredField<TProperty>(_binder, default!);
        }

        return new RequiredField<TProperty>(_binder, outcome.GetResultOrThrow());
    }

    /// <summary>
    ///     Binds an optional complex argument: absent yields a <c>null</c>
    ///     <see cref="OptionalReferenceField{TProperty}" /> value and records nothing; a present-but-invalid nested
    ///     binding records its envelope.
    /// </summary>
    /// <typeparam name="TProperty">The reference type the nested binding produces.</typeparam>
    /// <param name="bindNested">The nested binding function (typically a method group such as <c>BindAddress</c>).</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bindNested" /> is <c>null</c>.</exception>
    public OptionalReferenceField<TProperty> AsOptional<TProperty>(Func<RequestBinder<TArgument>, Outcome<TProperty>> bindNested) where TProperty : class {
        if (bindNested is null) { throw new ArgumentNullException(nameof(bindNested)); }

        if (_isMissing) { return new OptionalReferenceField<TProperty>(_binder, value: null); }

        Outcome<TProperty> outcome = BindNested(bindNested);
        if (outcome.IsFailure) {
            _binder.Record(Grouped(outcome.Error!));

            return new OptionalReferenceField<TProperty>(_binder, value: null);
        }

        return new OptionalReferenceField<TProperty>(_binder, outcome.GetResultOrThrow());
    }

    private Outcome<TProperty> BindNested<TProperty>(Func<RequestBinder<TArgument>, Outcome<TProperty>> bindNested) where TProperty : notnull {
        RequestBinder<TArgument> nested = new(_value!, _envelope, _binder.Options, _argumentPath);

        return bindNested(nested);
    }

    /// <summary>
    ///     A nested binding built with <c>Build</c> fails with its own envelope, already a
    ///     <see cref="PrimaryPortError" /> that self-describes the group — record it as-is. Any other failure (a
    ///     nested function returning a bare conversion failure) is wrapped so the argument path is preserved.
    /// </summary>
    private PrimaryPortError Grouped(Error error) {
        return error as PrimaryPortError ?? RequestBindingError.ArgumentInvalid(_argumentPath, error);
    }

}
