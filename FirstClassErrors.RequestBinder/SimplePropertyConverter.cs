namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Converts a scalar input — a DTO property or an out-of-DTO argument — into a value object, through a plain
///     converter (<c>Func&lt;TArgument, Outcome&lt;TProperty&gt;&gt;</c>).
/// </summary>
/// <remarks>
///     A converter fails by <b>returning</b> <c>Outcome.Failure</c> with a <see cref="DomainError" /> or a
///     <see cref="PrimaryPortError" /> — never by throwing. The binder catches nothing: a converter that throws is a bug,
///     and the exception propagates to the host's exception boundary.
/// </remarks>
/// <typeparam name="TArgument">The raw type of the input.</typeparam>
public sealed class SimplePropertyConverter<TArgument> {

    #region Fields declarations

    private readonly RequestBinding _binding;
    private readonly string         _argumentPath;
    private readonly TArgument?     _value;
    private readonly bool           _isMissing;
    private readonly string?        _source;

    #endregion

    #region Constructors declarations

    internal SimplePropertyConverter(RequestBinding binding, string argumentPath, TArgument? value, bool isMissing, string? source) {
        _binding      = binding;
        _argumentPath = argumentPath;
        _value        = value;
        _isMissing    = isMissing;
        _source       = source;
    }

    #endregion

    /// <summary>
    ///     Binds a required input: missing records <c>REQUEST_ARGUMENT_REQUIRED</c>, a failed conversion records
    ///     <c>REQUEST_ARGUMENT_INVALID</c> wrapping the converter's error.
    /// </summary>
    /// <typeparam name="TProperty">The type of the value object.</typeparam>
    /// <param name="convert">The value-object converter.</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convert" /> is <c>null</c>.</exception>
    public RequiredField<TProperty> AsRequired<TProperty>(Func<TArgument, Outcome<TProperty>> convert) where TProperty : notnull {
        if (convert is null) { throw new ArgumentNullException(nameof(convert)); }

        if (_isMissing) { return RequiredMissing<TProperty>(); }

        return RecordIfInvalid(convert(_value!));
    }

    /// <summary>
    ///     Binds a required input without conversion: only the presence is checked, and the raw value is the bound value.
    /// </summary>
    /// <returns>The bound field token.</returns>
    public RequiredField<TArgument> AsRequired() {
        if (_isMissing) {
            _binding.RecordArgumentRequired(_argumentPath, _source);

            return new RequiredField<TArgument>(_binding, default!);
        }

        return new RequiredField<TArgument>(_binding, _value!);
    }

    /// <summary>
    ///     Binds an optional input with a fallback: when the input is absent, <paramref name="rawFallback" /> is
    ///     converted instead, so the bound property always has a value. A present-but-invalid input still records
    ///     <c>REQUEST_ARGUMENT_INVALID</c> — optional means "may be absent", never "may be malformed".
    /// </summary>
    /// <typeparam name="TProperty">The type of the value object.</typeparam>
    /// <param name="convert">The value-object converter.</param>
    /// <param name="rawFallback">The raw value converted when the input is absent.</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convert" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <paramref name="rawFallback" /> itself fails to convert: a fallback is developer-supplied
    ///     configuration, so an invalid one is a bug, not a client error.
    /// </exception>
    public RequiredField<TProperty> AsOptional<TProperty>(Func<TArgument, Outcome<TProperty>> convert, TArgument rawFallback) where TProperty : notnull {
        if (convert is null) { throw new ArgumentNullException(nameof(convert)); }

        if (!_isMissing) { return RecordIfInvalid(convert(_value!)); }

        Outcome<TProperty> fallback = convert(rawFallback);
        if (fallback.IsFailure) {
            throw new InvalidOperationException(
                $"The configured fallback of optional argument '{_argumentPath}' does not convert: {fallback.Error!.DiagnosticMessage}");
        }

        return new RequiredField<TProperty>(_binding, fallback.GetResultOrThrow());
    }

    /// <summary>
    ///     Binds an optional reference-type input without a fallback: absent yields a <c>null</c>
    ///     <see cref="OptionalReferenceField{TProperty}" /> value and records nothing; a present-but-invalid input records
    ///     <c>REQUEST_ARGUMENT_INVALID</c>.
    /// </summary>
    /// <typeparam name="TProperty">The reference type of the value object.</typeparam>
    /// <param name="convert">The value-object converter.</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convert" /> is <c>null</c>.</exception>
    public OptionalReferenceField<TProperty> AsOptionalReference<TProperty>(Func<TArgument, Outcome<TProperty>> convert) where TProperty : class {
        if (convert is null) { throw new ArgumentNullException(nameof(convert)); }

        if (_isMissing) { return new OptionalReferenceField<TProperty>(_binding, value: null); }

        Outcome<TProperty> outcome = convert(_value!);
        if (outcome.IsFailure) {
            RecordInvalid(outcome.Error!);

            return new OptionalReferenceField<TProperty>(_binding, value: null);
        }

        return new OptionalReferenceField<TProperty>(_binding, outcome.GetResultOrThrow());
    }

    /// <summary>
    ///     Binds an optional value-type input without a fallback: absent yields a <c>null</c>
    ///     <see cref="OptionalValueField{TProperty}" /> value — a real <see cref="Nullable{T}" /> <c>null</c>, never
    ///     <c>default(TProperty)</c> — and records nothing; a present-but-invalid input records
    ///     <c>REQUEST_ARGUMENT_INVALID</c>.
    /// </summary>
    /// <typeparam name="TProperty">The value type of the value object.</typeparam>
    /// <param name="convert">The value-object converter.</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convert" /> is <c>null</c>.</exception>
    public OptionalValueField<TProperty> AsOptionalValue<TProperty>(Func<TArgument, Outcome<TProperty>> convert) where TProperty : struct {
        if (convert is null) { throw new ArgumentNullException(nameof(convert)); }

        if (_isMissing) { return new OptionalValueField<TProperty>(_binding, value: null); }

        Outcome<TProperty> outcome = convert(_value!);
        if (outcome.IsFailure) {
            RecordInvalid(outcome.Error!);

            return new OptionalValueField<TProperty>(_binding, value: null);
        }

        return new OptionalValueField<TProperty>(_binding, outcome.GetResultOrThrow());
    }

    private RequiredField<TProperty> RequiredMissing<TProperty>() {
        _binding.RecordArgumentRequired(_argumentPath, _source);

        return new RequiredField<TProperty>(_binding, default!);
    }

    private RequiredField<TProperty> RecordIfInvalid<TProperty>(Outcome<TProperty> outcome) where TProperty : notnull {
        if (outcome.IsFailure) {
            RecordInvalid(outcome.Error!);

            return new RequiredField<TProperty>(_binding, default!);
        }

        return new RequiredField<TProperty>(_binding, outcome.GetResultOrThrow());
    }

    private void RecordInvalid(Error cause) {
        _binding.RecordArgumentInvalid(_argumentPath, cause, _source);
    }

}
