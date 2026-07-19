namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Binds a complex property through a nested binder: the binding function receives a child
///     <see cref="RequestBinder" /> — building the nested value object, prefixed with this property's path, inheriting the
///     parent options, failing with the envelope declared on the previous stage — together with the nested DTO to attach
///     to it, and typically lives in a dedicated method passed as a method group.
/// </summary>
/// <remarks>
///     The nested binder is a full <see cref="RequestBinder" />, so the nested value object is built exactly like a
///     top-level command: attach the supplied DTO with <see cref="RequestBinder.PropertiesOf{TDto}" />, bind its
///     properties (and, if needed, out-of-DTO arguments), then assemble with <c>New</c> / <c>Create</c>.
/// </remarks>
/// <typeparam name="TArgument">The type of the nested DTO.</typeparam>
public sealed class ComplexPropertyConverter<TArgument> {

    #region Fields declarations

    private readonly RequestBinding                                 _binding;
    private readonly string                                         _argumentPath;
    private readonly TArgument?                                     _value;
    private readonly bool                                           _isMissing;
    private readonly Func<PrimaryPortInnerErrors, PrimaryPortError> _envelope;

    #endregion

    #region Constructors declarations

    internal ComplexPropertyConverter(RequestBinding                                 binding,
                                      string                                         argumentPath,
                                      TArgument?                                     value,
                                      bool                                           isMissing,
                                      Func<PrimaryPortInnerErrors, PrimaryPortError> envelope) {
        _binding      = binding;
        _argumentPath = argumentPath;
        _value        = value;
        _isMissing    = isMissing;
        _envelope     = envelope;
    }

    #endregion

    /// <summary>
    ///     Binds a required complex property: missing records <c>REQUEST_ARGUMENT_REQUIRED</c>; a failed nested binding
    ///     records its envelope, whose inner errors carry the full, prefixed argument paths.
    /// </summary>
    /// <typeparam name="TProperty">The type the nested binding produces.</typeparam>
    /// <param name="bindNested">The nested binding function, receiving the child binder and the nested DTO (typically a method group such as <c>BindStay</c>).</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bindNested" /> is <c>null</c>.</exception>
    public RequiredField<TProperty> AsRequired<TProperty>(Func<RequestBinder, TArgument, Outcome<TProperty>> bindNested) where TProperty : notnull {
        if (bindNested is null) { throw new ArgumentNullException(nameof(bindNested)); }

        if (_isMissing) {
            _binding.RecordArgumentRequired(_argumentPath);

            return new RequiredField<TProperty>(_binding, default!);
        }

        RequestBinding     nested  = NestedBinding();
        Outcome<TProperty> outcome = bindNested(new RequestBinder(nested), _value!);
        if (outcome.IsFailure) {
            _binding.Record(NestedFailure.Group(outcome.Error!, nested.BuiltEnvelope, _argumentPath, _binding.Options.ArgumentInvalid));

            return new RequiredField<TProperty>(_binding, default!);
        }

        return new RequiredField<TProperty>(_binding, outcome.GetResultOrThrow());
    }

    /// <summary>
    ///     Binds an optional complex property whose bound type is a <b>reference type</b>: absent yields a <c>null</c>
    ///     <see cref="OptionalReferenceField{TProperty}" /> value and records nothing; a present-but-invalid nested
    ///     binding records its envelope. The complex counterpart of the scalar <c>AsOptionalReference</c>; the split name
    ///     keeps <c>AsOptionalValue</c> free for a future value-type nested object.
    /// </summary>
    /// <typeparam name="TProperty">The reference type the nested binding produces.</typeparam>
    /// <param name="bindNested">The nested binding function, receiving the child binder and the nested DTO (typically a method group such as <c>BindAddress</c>).</param>
    /// <returns>The bound field token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bindNested" /> is <c>null</c>.</exception>
    public OptionalReferenceField<TProperty> AsOptionalReference<TProperty>(Func<RequestBinder, TArgument, Outcome<TProperty>> bindNested) where TProperty : class {
        if (bindNested is null) { throw new ArgumentNullException(nameof(bindNested)); }

        if (_isMissing) { return new OptionalReferenceField<TProperty>(_binding, value: null); }

        RequestBinding     nested  = NestedBinding();
        Outcome<TProperty> outcome = bindNested(new RequestBinder(nested), _value!);
        if (outcome.IsFailure) {
            _binding.Record(NestedFailure.Group(outcome.Error!, nested.BuiltEnvelope, _argumentPath, _binding.Options.ArgumentInvalid));

            return new OptionalReferenceField<TProperty>(_binding, value: null);
        }

        return new OptionalReferenceField<TProperty>(_binding, outcome.GetResultOrThrow());
    }

    private RequestBinding NestedBinding() {
        return new RequestBinding(_envelope, _binding.Options, _argumentPath);
    }

}
