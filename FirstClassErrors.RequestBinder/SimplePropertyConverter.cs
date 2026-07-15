namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Converts a scalar request property into a value object, through a plain converter
///     (<c>Func&lt;TArgument, Outcome&lt;TProperty&gt;&gt;</c>).
/// </summary>
/// <remarks>
///     A converter fails by <b>returning</b> <c>Outcome.Failure</c> with a <see cref="DomainError" /> or a
///     <see cref="PrimaryPortError" /> — never by throwing. The binder catches nothing: a converter that throws is a
///     bug, and the exception propagates to the host's exception boundary.
/// </remarks>
/// <typeparam name="TRequest">The type of the request DTO.</typeparam>
/// <typeparam name="TArgument">The type of the DTO property.</typeparam>
public sealed class SimplePropertyConverter<TRequest, TArgument> {

    #region Fields declarations

    private readonly RequestBinder<TRequest> _binder;
    private readonly string                  _argumentPath;
    private readonly TArgument?              _value;
    private readonly bool                    _isMissing;

    #endregion

    #region Constructors declarations

    internal SimplePropertyConverter(RequestBinder<TRequest> binder, string argumentPath, TArgument? value, bool isMissing) {
        _binder       = binder;
        _argumentPath = argumentPath;
        _value        = value;
        _isMissing    = isMissing;
    }

    #endregion

    /// <summary>
    ///     Binds a required argument: missing records <c>REQUEST_ARGUMENT_REQUIRED</c>, a failed conversion records
    ///     <c>REQUEST_ARGUMENT_INVALID</c> wrapping the converter's error.
    /// </summary>
    /// <typeparam name="TProperty">The type of the value object.</typeparam>
    /// <param name="convert">The value-object converter.</param>
    /// <returns>The bound property handle.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convert" /> is <c>null</c>.</exception>
    public RequiredProperty<TProperty> AsRequired<TProperty>(Func<TArgument, Outcome<TProperty>> convert) where TProperty : notnull {
        if (convert is null) { throw new ArgumentNullException(nameof(convert)); }

        if (_isMissing) { return RequiredMissing<TProperty>(); }

        return RecordIfInvalid(convert(_value!));
    }

    /// <summary>
    ///     Binds a required argument without conversion: only the presence is checked, and the raw value is the bound
    ///     value.
    /// </summary>
    /// <returns>The bound property handle.</returns>
    public RequiredProperty<TArgument> AsRequired() {
        if (_isMissing) {
            PrimaryPortError error = RequestBindingError.ArgumentRequired(_argumentPath);
            _binder.Record(error);

            return new RequiredProperty<TArgument>(default!, error);
        }

        return new RequiredProperty<TArgument>(_value!, failure: null);
    }

    /// <summary>
    ///     Binds an optional argument with a fallback: when the argument is absent, <paramref name="rawFallback" />
    ///     is converted instead, so the bound property always has a value. A present-but-invalid argument still
    ///     records <c>REQUEST_ARGUMENT_INVALID</c> — optional means "may be absent", never "may be malformed".
    /// </summary>
    /// <typeparam name="TProperty">The type of the value object.</typeparam>
    /// <param name="convert">The value-object converter.</param>
    /// <param name="rawFallback">The raw value converted when the argument is absent.</param>
    /// <returns>The bound property handle.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convert" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <paramref name="rawFallback" /> itself fails to convert: a fallback is developer-supplied
    ///     configuration, so an invalid one is a bug, not a client error.
    /// </exception>
    public RequiredProperty<TProperty> AsOptional<TProperty>(Func<TArgument, Outcome<TProperty>> convert, TArgument rawFallback) where TProperty : notnull {
        if (convert is null) { throw new ArgumentNullException(nameof(convert)); }

        if (!_isMissing) { return RecordIfInvalid(convert(_value!)); }

        Outcome<TProperty> fallback = convert(rawFallback);
        if (fallback.IsFailure) {
            throw new InvalidOperationException(
                $"The configured fallback of optional argument '{_argumentPath}' does not convert: {fallback.Error!.DiagnosticMessage}");
        }

        return new RequiredProperty<TProperty>(fallback.GetResultOrThrow(), failure: null);
    }

    /// <summary>
    ///     Binds an optional reference-type argument without a fallback: absent yields a <c>null</c>
    ///     <see cref="OptionalReferenceProperty{TProperty}.Value" /> and records nothing; a present-but-invalid
    ///     argument records <c>REQUEST_ARGUMENT_INVALID</c>.
    /// </summary>
    /// <typeparam name="TProperty">The reference type of the value object.</typeparam>
    /// <param name="convert">The value-object converter.</param>
    /// <returns>The bound property handle.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convert" /> is <c>null</c>.</exception>
    public OptionalReferenceProperty<TProperty> AsOptionalReference<TProperty>(Func<TArgument, Outcome<TProperty>> convert) where TProperty : class {
        if (convert is null) { throw new ArgumentNullException(nameof(convert)); }

        if (_isMissing) { return new OptionalReferenceProperty<TProperty>(value: null, failure: null); }

        Outcome<TProperty> outcome = convert(_value!);
        if (outcome.IsFailure) {
            PrimaryPortError wrapped = RecordInvalid(outcome.Error!);

            return new OptionalReferenceProperty<TProperty>(value: null, wrapped);
        }

        return new OptionalReferenceProperty<TProperty>(outcome.GetResultOrThrow(), failure: null);
    }

    /// <summary>
    ///     Binds an optional value-type argument without a fallback: absent yields a <c>null</c>
    ///     <see cref="OptionalValueProperty{TProperty}.Value" /> — a real <see cref="Nullable{T}" /> <c>null</c>,
    ///     never <c>default(TProperty)</c> — and records nothing; a present-but-invalid argument records
    ///     <c>REQUEST_ARGUMENT_INVALID</c>.
    /// </summary>
    /// <typeparam name="TProperty">The value type of the value object.</typeparam>
    /// <param name="convert">The value-object converter.</param>
    /// <returns>The bound property handle.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convert" /> is <c>null</c>.</exception>
    public OptionalValueProperty<TProperty> AsOptionalValue<TProperty>(Func<TArgument, Outcome<TProperty>> convert) where TProperty : struct {
        if (convert is null) { throw new ArgumentNullException(nameof(convert)); }

        if (_isMissing) { return new OptionalValueProperty<TProperty>(value: null, failure: null); }

        Outcome<TProperty> outcome = convert(_value!);
        if (outcome.IsFailure) {
            PrimaryPortError wrapped = RecordInvalid(outcome.Error!);

            return new OptionalValueProperty<TProperty>(value: null, wrapped);
        }

        return new OptionalValueProperty<TProperty>(outcome.GetResultOrThrow(), failure: null);
    }

    private RequiredProperty<TProperty> RequiredMissing<TProperty>() where TProperty : notnull {
        PrimaryPortError error = RequestBindingError.ArgumentRequired(_argumentPath);
        _binder.Record(error);

        return new RequiredProperty<TProperty>(default!, error);
    }

    private RequiredProperty<TProperty> RecordIfInvalid<TProperty>(Outcome<TProperty> outcome) where TProperty : notnull {
        if (outcome.IsFailure) {
            PrimaryPortError wrapped = RecordInvalid(outcome.Error!);

            return new RequiredProperty<TProperty>(default!, wrapped);
        }

        return new RequiredProperty<TProperty>(outcome.GetResultOrThrow(), failure: null);
    }

    private PrimaryPortError RecordInvalid(Error cause) {
        PrimaryPortError wrapped = RequestBindingError.ArgumentInvalid(_argumentPath, cause);
        _binder.Record(wrapped);

        return wrapped;
    }

}
