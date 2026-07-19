namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The request-independent core of a binding: it owns the collected failures, the failure envelope, the binding
///     options and the argument-path prefix, and it is the identity every bound field token is checked against inside a
///     build terminal.
/// </summary>
/// <remarks>
///     A <see cref="RequestBinder" /> builds a command or query on top of one instance; a
///     <see cref="PropertySource{TDto}" /> and the argument sources record their failures into the same instance — so a
///     DTO's properties and out-of-DTO arguments bound on one binder share a single collect-all envelope and one set of
///     argument paths. Instances are not thread-safe: one binding, in one scope.
/// </remarks>
internal sealed class RequestBinding {

    #region Fields declarations

    private readonly Func<PrimaryPortInnerErrors, PrimaryPortError> _envelope;
    private readonly string?                                        _argumentPrefix;
    private readonly List<PrimaryPortError>                         _errors = new();

    #endregion

    #region Constructors declarations

    internal RequestBinding(Func<PrimaryPortInnerErrors, PrimaryPortError> envelope, RequestBinderOptions options, string? argumentPrefix) {
        _envelope       = envelope;
        Options         = options;
        _argumentPrefix = argumentPrefix;
    }

    #endregion

    /// <summary>The options this binding (and every binding nested under it) binds with; fixed before binding begins.</summary>
    internal RequestBinderOptions Options { get; }

    /// <summary>
    ///     The envelope instance the most recent failing build terminal produced, or <c>null</c> when no build has
    ///     failed. A parent binding compares a nested failure against this <b>by reference</b> to tell a nested binding's
    ///     own self-describing envelope (recorded as-is) from a leaf error a nested binding returned directly (wrapped
    ///     under the argument path).
    /// </summary>
    internal PrimaryPortError? BuiltEnvelope { get; private set; }

    /// <summary>Whether any binding failure has been recorded; a build terminal assembles only when this is <c>false</c>.</summary>
    internal bool HasErrors => _errors.Count > 0;

    /// <summary>Records a binding failure; it will surface in the envelope built by a failing build terminal.</summary>
    internal void Record(PrimaryPortError error) {
        _errors.Add(error);
    }

    /// <summary>
    ///     Records a missing-required-argument failure at <paramref name="argumentPath" /> under the configured
    ///     <see cref="RequestBinderOptions.ArgumentRequired" />, tagging it with <paramref name="source" /> when the value
    ///     was supplied as an out-of-DTO argument (<c>null</c> for a DTO property, whose provenance is implicit).
    /// </summary>
    internal void RecordArgumentRequired(string argumentPath, string? source = null) {
        Record(RequestBindingError.ArgumentRequired(Options.ArgumentRequired, argumentPath, source));
    }

    /// <summary>
    ///     Records a present-but-invalid-argument failure at <paramref name="argumentPath" /> under the configured
    ///     <see cref="RequestBinderOptions.ArgumentInvalid" />, wrapping <paramref name="cause" /> and tagging it with
    ///     <paramref name="source" /> when the value was supplied as an out-of-DTO argument.
    /// </summary>
    internal void RecordArgumentInvalid(string argumentPath, Error cause, string? source = null) {
        Record(RequestBindingError.ArgumentInvalid(Options.ArgumentInvalid, argumentPath, cause, source));
    }

    /// <summary>
    ///     Binds every element of a list at its indexed path (<c>Tags[2]</c>), collecting <b>every</b> failure so one bad
    ///     element never hides the others: a <c>null</c> element records the required-argument failure, and
    ///     <paramref name="convertElementAt" /> binds each non-null element — recording its own failure (a converter
    ///     error, or a nested envelope wrapped under the path) and yielding its value on success. The list converters
    ///     share this single iteration and null-element rule, so a change to either is made in one place.
    /// </summary>
    /// <typeparam name="TStored">The element type as stored (a reference or a <see cref="Nullable{T}" />).</typeparam>
    /// <typeparam name="TProperty">The type each element binds to.</typeparam>
    /// <param name="argumentPath">The list's argument path; each element is reported under <c>argumentPath[index]</c>.</param>
    /// <param name="values">The list elements.</param>
    /// <param name="source">The provenance of an out-of-DTO argument list (<c>null</c> for a DTO property).</param>
    /// <param name="convertElementAt">Binds a non-null element at its indexed path, recording its own failure.</param>
    /// <returns>The bound field token carrying the successfully bound elements.</returns>
    internal RequiredField<IReadOnlyList<TProperty>> ConvertEachElement<TStored, TProperty>(
        string argumentPath, IEnumerable<TStored> values, string? source, Func<TStored, string, Outcome<TProperty>> convertElementAt) where TProperty : notnull {
        List<TProperty> converted = new();
        int             index     = 0;

        foreach (TStored element in values) {
            string elementPath = $"{argumentPath}[{index}]";
            index++;

            if (element is null) {
                RecordArgumentRequired(elementPath, source);

                continue;
            }

            Outcome<TProperty> outcome = convertElementAt(element, elementPath);
            if (outcome.IsSuccess) { converted.Add(outcome.GetResultOrThrow()); }
            // A failing element was already recorded by convertElementAt (REQUEST_ARGUMENT_INVALID, or the nested
            // envelope wrapped under the indexed path), so it is simply skipped here.
        }

        // The value is read only through a BindingScope, which a build terminal creates solely when no failure was
        // recorded — i.e. only when every element bound — so `converted` is the complete list when it is observed.
        return new RequiredField<IReadOnlyList<TProperty>>(this, converted);
    }

    /// <summary>Prepends this binding's argument prefix to a path segment ("CheckIn" -&gt; "Stay.CheckIn").</summary>
    internal string PathOf(string argumentName) {
        return _argumentPrefix is null ? argumentName : $"{_argumentPrefix}.{argumentName}";
    }

    /// <summary>Builds and caches the envelope grouping every recorded failure; the cached instance disambiguates a nested failure by reference.</summary>
    internal PrimaryPortError BuildFailureEnvelope() {
        PrimaryPortInnerErrors innerErrors = new();
        foreach (PrimaryPortError error in _errors) {
            innerErrors.Add(error);
        }

        BuiltEnvelope = _envelope(innerErrors);

        return BuiltEnvelope;
    }

}
