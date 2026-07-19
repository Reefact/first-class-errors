namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Binds an incoming request into a typed command or query of value objects at the primary-adapter boundary,
///     collecting <b>every</b> failure — instead of stopping at the first — into a single coded
///     <see cref="PrimaryPortError" /> tree. The binder is <b>source-agnostic</b>: its inputs are attached as peers — a
///     DTO's properties through <see cref="PropertiesOf{TDto}" />, and individually named out-of-DTO values through
///     <see cref="Argument" /> / <see cref="ArgumentList" /> — so a route/query/header value binds into the same envelope
///     as a body property, with the same paths, and the command type is named only at the build terminal
///     (<see cref="New{TCommand}" /> / <see cref="Create{TCommand}" />).
/// </summary>
/// <remarks>
///     <para>
///         <b>No throw on the invalid-input path.</b> Converters return <see cref="Outcome{T}" />; every failure is
///         recorded as a coded error and surfaces once, as the failure of the build terminal (<see cref="New{TCommand}" />
///         / <see cref="Create{TCommand}" />). A request whose every field is invalid raises zero exceptions. Exceptions
///         are reserved for programming errors (a converter that throws, an invalid selector, a mis-declared fallback):
///         the binder catches nothing, so a genuine bug propagates to the host's exception boundary instead of being
///         disguised as a client error.
///     </para>
///     <para>
///         Instances are created through <see cref="Bind.Request" /> and are not thread-safe: a binder binds one
///         request, in one scope.
///     </para>
/// </remarks>
public sealed class RequestBinder {

    #region Fields declarations

    private readonly RequestBinding _binding;

    #endregion

    #region Constructors declarations

    internal RequestBinder(RequestBinding binding) {
        _binding = binding;
    }

    #endregion

    /// <summary>
    ///     Attaches a request DTO as a source of inputs: its properties are bound through the returned
    ///     <see cref="PropertySource{TDto}" /> (<c>SimpleProperty</c>, <c>ComplexProperty</c>, <c>ListOf…</c>). The DTO is
    ///     one source among peers; out-of-DTO values are attached separately through <see cref="Argument" />.
    /// </summary>
    /// <typeparam name="TDto">The type of the request DTO.</typeparam>
    /// <param name="dto">The request DTO whose properties are bound.</param>
    /// <returns>The property source offering the DTO-property selectors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dto" /> is <c>null</c>.</exception>
    public PropertySource<TDto> PropertiesOf<TDto>(TDto dto) {
        if (dto is null) { throw new ArgumentNullException(nameof(dto)); }

        return new PropertySource<TDto>(_binding, dto);
    }

    /// <summary>
    ///     Names an out-of-DTO argument — a value that does not come from a request DTO (a route, query, header or claim
    ///     value). State where it comes from and supply its value next, with <see cref="ArgumentSource.From{TArgument}(string, TArgument)" />
    ///     (or a host helper such as <c>FromRoute</c>). The failure is recorded under <paramref name="name" />, into the
    ///     same envelope as every other input.
    /// </summary>
    /// <param name="name">The argument's logical name, used verbatim as its error path.</param>
    /// <returns>The stage on which the source and value are supplied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name" /> is <c>null</c>.</exception>
    public ArgumentSource Argument(string name) {
        if (name is null) { throw new ArgumentNullException(nameof(name)); }

        return new ArgumentSource(_binding, _binding.PathOf(name));
    }

    /// <summary>
    ///     Names an out-of-DTO <b>list</b> argument — repeated out-of-DTO values under one name (a repeated query
    ///     parameter, for example). Supply its source and values next, with
    ///     <see cref="ArgumentListSource.From{TArgument}(string, IEnumerable{TArgument})" />. Each failing element is
    ///     recorded under its indexed path (<c>tag[2]</c>).
    /// </summary>
    /// <param name="name">The argument's logical name, the stem of each element's indexed error path.</param>
    /// <returns>The stage on which the source and values are supplied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name" /> is <c>null</c>.</exception>
    public ArgumentListSource ArgumentList(string name) {
        if (name is null) { throw new ArgumentNullException(nameof(name)); }

        return new ArgumentListSource(_binding, _binding.PathOf(name));
    }

    /// <summary>
    ///     Terminal (total assembler): builds the command <typeparamref name="TCommand" /> with a <c>new</c> — when, and
    ///     only when, no binding failure was recorded; otherwise returns the failure of the envelope grouping every
    ///     recorded error. The assembler receives a <see cref="BindingScope" /> and reads each bound value through it;
    ///     because that scope is created only on this success branch, every read is valid by construction, and the
    ///     assembler itself cannot fail. The command type is inferred from the assembler, so it need not be named.
    /// </summary>
    /// <remarks>
    ///     Mirror the shape of the assembler at the call site: <see cref="New{TCommand}" /> takes a <c>new</c> — a total
    ///     constructor, because all validation already happened field by field. When the command is produced by a
    ///     validating factory returning <see cref="Outcome{T}" /> — one that may still reject an all-valid combination
    ///     through a cross-field rule (<c>CheckOut &gt; CheckIn</c>) — call <see cref="Create{TCommand}" /> instead.
    /// </remarks>
    /// <typeparam name="TCommand">The type of the command or query to build, inferred from <paramref name="assemble" />.</typeparam>
    /// <param name="assemble">The assembler, reading the bound values from the supplied <see cref="BindingScope" />.</param>
    /// <returns>The bound command, or the envelope failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemble" /> is <c>null</c>.</exception>
    public Outcome<TCommand> New<TCommand>(BindingAssembler<TCommand> assemble) where TCommand : notnull {
        if (assemble is null) { throw new ArgumentNullException(nameof(assemble)); }

        if (!_binding.HasErrors) { return Outcome<TCommand>.Success(assemble(new BindingScope(_binding))); }

        return Outcome<TCommand>.Failure(_binding.BuildFailureEnvelope());
    }

    /// <summary>
    ///     Terminal (validating assembler): builds the command through a factory returning <see cref="Outcome{T}" /> —
    ///     when, and only when, no binding failure was recorded — and <b>flattens</b> the result, so a cross-field rule
    ///     the factory enforces (<c>CheckOut &gt; CheckIn</c>) surfaces directly instead of nesting a second
    ///     <see cref="Outcome{T}" />. When a binding failure was recorded, the factory is never called and the envelope
    ///     grouping every recorded error is returned.
    /// </summary>
    /// <remarks>
    ///     The factory runs only on the zero-error branch — every field is already bound and readable through the
    ///     supplied <see cref="BindingScope" /> — so a cross-field rule can assume all its inputs are present and valid.
    ///     Its failure is returned <b>as-is</b>: the factory owns that error, and only field-binding failures are grouped
    ///     under the envelope declared with <see cref="Bind.Request" />. For a total constructor that cannot fail, call
    ///     <see cref="New{TCommand}" />.
    /// </remarks>
    /// <typeparam name="TCommand">The type of the command or query to build, inferred from <paramref name="assemble" />.</typeparam>
    /// <param name="assemble">
    ///     The validating assembler, reading the bound values from the supplied <see cref="BindingScope" /> and returning
    ///     an <see cref="Outcome{T}" />.
    /// </param>
    /// <returns>The bound command, the factory's own failure, or the envelope failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemble" /> is <c>null</c>.</exception>
    public Outcome<TCommand> Create<TCommand>(ValidatingAssembler<TCommand> assemble) where TCommand : notnull {
        if (assemble is null) { throw new ArgumentNullException(nameof(assemble)); }

        if (!_binding.HasErrors) { return assemble(new BindingScope(_binding)); }

        return Outcome<TCommand>.Failure(_binding.BuildFailureEnvelope());
    }

}
