#region Usings declarations

using System.Linq.Expressions;
using System.Reflection;

#endregion

namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Binds the properties of a request DTO into value objects, collecting <b>every</b> failure — instead of
///     stopping at the first — and grouping them under the envelope declared with
///     <see cref="RequestBinderEnvelopeStage{TRequest}.FailWith" />.
/// </summary>
/// <remarks>
///     <para>
///         <b>No throw on the invalid-input path.</b> Converters return <see cref="Outcome{T}" />; every failure is
///         recorded as a coded error and surfaces once, as the failure of <see cref="Build{TCommand}" />. A request
///         whose every field is invalid raises zero exceptions. Exceptions are reserved for programming errors
///         (a converter that throws, an invalid selector, a mis-declared fallback): the binder catches nothing, so a
///         genuine bug propagates to the host's exception boundary instead of being disguised as a client error.
///     </para>
///     <para>
///         Instances are created through <see cref="Bind.PropertiesOf{TRequest}" /> and are not thread-safe: a binder
///         binds one request, in one scope.
///     </para>
/// </remarks>
/// <typeparam name="TRequest">The type of the request DTO.</typeparam>
public sealed class RequestBinder<TRequest> {

    #region Fields declarations

    private readonly TRequest                                        _request;
    private readonly Func<PrimaryPortInnerErrors, PrimaryPortError>  _envelope;
    private readonly string?                                         _argumentPrefix;
    private readonly List<PrimaryPortError>                          _errors = new();

    #endregion

    #region Constructors declarations

    internal RequestBinder(TRequest request, Func<PrimaryPortInnerErrors, PrimaryPortError> envelope, RequestBinderOptions options, string? argumentPrefix) {
        _request        = request;
        _envelope       = envelope;
        Options         = options;
        _argumentPrefix = argumentPrefix;
    }

    #endregion

    /// <summary>The options this binder (and every binder nested under it) binds with.</summary>
    internal RequestBinderOptions Options { get; private set; }

    /// <summary>
    ///     The envelope instance the most recent failing <see cref="Build{TCommand}" /> produced, or <c>null</c> when
    ///     no build has failed. A parent binder compares a nested failure against this <b>by reference</b> to tell this
    ///     binder's own self-describing envelope (recorded as-is) from a leaf error a nested binding returned directly
    ///     (wrapped under the argument path).
    /// </summary>
    internal PrimaryPortError? BuiltEnvelope { get; private set; }

    /// <summary>
    ///     Replaces the binder options (for example to plug a serializer-aware
    ///     <see cref="IArgumentNameProvider" />). Call it before binding any property; nested binders inherit the
    ///     options in effect when they are created.
    /// </summary>
    /// <param name="options">The options to bind with.</param>
    /// <returns>This binder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <c>null</c>.</exception>
    public RequestBinder<TRequest> WithOptions(RequestBinderOptions options) {
        if (options is null) { throw new ArgumentNullException(nameof(options)); }

        Options = options;

        return this;
    }

    /// <summary>
    ///     Selects a scalar property, converted by a plain value-object converter
    ///     (<c>Func&lt;TArgument, Outcome&lt;T&gt;&gt;</c>).
    /// </summary>
    /// <typeparam name="TArgument">The type of the DTO property.</typeparam>
    /// <param name="selector">A direct property access on the request parameter (e.g. <c>r =&gt; r.GuestEmail</c>).</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c> and their variants.</returns>
    public SimplePropertyConverter<TRequest, TArgument> SimpleProperty<TArgument>(Expression<Func<TRequest, TArgument?>> selector) {
        (string path, object? value) = ResolveArgument(selector);

        return new SimplePropertyConverter<TRequest, TArgument>(this, path, (TArgument?)value, value is null);
    }

    /// <summary>
    ///     Selects a complex property, bound by a nested binder. Declare the nested envelope next, with
    ///     <see cref="ComplexPropertyEnvelopeStage{TRequest, TArgument}.FailWith" />.
    /// </summary>
    /// <typeparam name="TArgument">The type of the nested DTO.</typeparam>
    /// <param name="selector">A direct property access on the request parameter (e.g. <c>r =&gt; r.Stay</c>).</param>
    /// <returns>The stage on which the nested envelope is declared.</returns>
    public ComplexPropertyEnvelopeStage<TRequest, TArgument> ComplexProperty<TArgument>(Expression<Func<TRequest, TArgument?>> selector) {
        (string path, object? value) = ResolveArgument(selector);

        return new ComplexPropertyEnvelopeStage<TRequest, TArgument>(this, path, (TArgument?)value, value is null);
    }

    /// <summary>
    ///     Selects a list property whose elements are converted by a plain value-object converter.
    /// </summary>
    /// <typeparam name="TArgument">The element type of the DTO list.</typeparam>
    /// <param name="selector">A direct property access on the request parameter (e.g. <c>r =&gt; r.Tags</c>).</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c>.</returns>
    public ListOfSimplePropertiesConverter<TRequest, TArgument> ListOfSimpleProperties<TArgument>(Expression<Func<TRequest, IEnumerable<TArgument?>?>> selector) {
        (string path, object? value) = ResolveArgument(selector);

        return new ListOfSimplePropertiesConverter<TRequest, TArgument>(this, path, (IEnumerable<TArgument?>?)value, value is null);
    }

    /// <summary>
    ///     Selects a list property whose elements are bound by a nested binder (one per element). Declare the
    ///     per-element envelope next, with
    ///     <see cref="ListOfComplexPropertiesEnvelopeStage{TRequest, TArgument}.FailWith" />.
    /// </summary>
    /// <typeparam name="TArgument">The element type of the DTO list.</typeparam>
    /// <param name="selector">A direct property access on the request parameter (e.g. <c>r =&gt; r.Guests</c>).</param>
    /// <returns>The stage on which the per-element envelope is declared.</returns>
    public ListOfComplexPropertiesEnvelopeStage<TRequest, TArgument> ListOfComplexProperties<TArgument>(Expression<Func<TRequest, IEnumerable<TArgument?>?>> selector) {
        (string path, object? value) = ResolveArgument(selector);

        return new ListOfComplexPropertiesEnvelopeStage<TRequest, TArgument>(this, path, (IEnumerable<TArgument?>?)value, value is null);
    }

    /// <summary>
    ///     Terminal: assembles the command when — and only when — no binding failure was recorded; otherwise returns
    ///     the failure of the envelope grouping every recorded error. The assembler receives a
    ///     <see cref="BindingScope" /> and reads each bound value through it; because that scope is created only on this
    ///     success branch, every read is valid by construction.
    /// </summary>
    /// <typeparam name="TCommand">The type of the bound command or query.</typeparam>
    /// <param name="assemble">The assembler, reading the bound values from the supplied <see cref="BindingScope" />.</param>
    /// <returns>The bound command, or the envelope failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemble" /> is <c>null</c>.</exception>
    public Outcome<TCommand> Build<TCommand>(BindingAssembler<TCommand> assemble) where TCommand : notnull {
        if (assemble is null) { throw new ArgumentNullException(nameof(assemble)); }

        if (_errors.Count == 0) { return Outcome<TCommand>.Success(assemble(new BindingScope(this))); }

        PrimaryPortInnerErrors innerErrors = new();
        foreach (PrimaryPortError error in _errors) {
            innerErrors.Add(error);
        }

        // Remember the exact envelope instance produced here, so a parent binder can tell this self-describing
        // envelope (recorded as-is) from any other failure a nested binding returned (wrapped under the path).
        BuiltEnvelope = _envelope(innerErrors);

        return Outcome<TCommand>.Failure(BuiltEnvelope);
    }

    /// <summary>Records a binding failure; it will surface in the envelope built by <see cref="Build{TCommand}" />.</summary>
    internal void Record(PrimaryPortError error) {
        _errors.Add(error);
    }

    /// <summary>Prepends this binder's argument prefix to a path segment ("CheckIn" -&gt; "Stay.CheckIn").</summary>
    internal string PathOf(string argumentName) {
        return _argumentPrefix is null ? argumentName : $"{_argumentPrefix}.{argumentName}";
    }

    private (string Path, object? Value) ResolveArgument<TArgument>(Expression<Func<TRequest, TArgument>> selector) {
        PropertyInfo property = PropertySelectors.GetProperty(selector);
        string       path     = PathOf(Options.ArgumentNameProvider.GetArgumentNameFrom(property));

        return (path, property.GetValue(_request));
    }

}
