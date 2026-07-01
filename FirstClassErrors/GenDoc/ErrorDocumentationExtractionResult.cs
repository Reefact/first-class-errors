namespace FirstClassErrors.GenDoc;

/// <summary>
///     The outcome of extracting the documentation model from a single assembly: the documentation that could be
///     read, together with every failure encountered while reading it.
/// </summary>
/// <remarks>
///     A non-empty <see cref="Failures" /> does not necessarily mean the extraction is unusable — it lets the caller
///     decide, per its own policy, whether a broken factory should be logged, ignored, or turned into a hard error.
/// </remarks>
public sealed class ErrorDocumentationExtractionResult {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new <see cref="ErrorDocumentationExtractionResult" />.
    /// </summary>
    /// <param name="documentation">The documentation successfully extracted from the assembly.</param>
    /// <param name="failures">The failures encountered during extraction.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="documentation" /> or <paramref name="failures" /> is <c>null</c>.
    /// </exception>
    public ErrorDocumentationExtractionResult(IReadOnlyList<ErrorDocumentation>                  documentation,
                                              IReadOnlyList<ErrorDocumentationExtractionFailure> failures) {
        Documentation = documentation ?? throw new ArgumentNullException(nameof(documentation));
        Failures      = failures ?? throw new ArgumentNullException(nameof(failures));
    }

    #endregion

    /// <summary>
    ///     Gets the deduplicated documentation successfully extracted from the assembly.
    /// </summary>
    public IReadOnlyList<ErrorDocumentation> Documentation { get; }

    /// <summary>
    ///     Gets the type- or factory-level failures encountered during extraction. Empty on a fully successful read.
    /// </summary>
    public IReadOnlyList<ErrorDocumentationExtractionFailure> Failures { get; }

    /// <summary>
    ///     Gets a value indicating whether at least one failure was recorded during extraction.
    /// </summary>
    public bool HasFailures => Failures.Count > 0;

}

/// <summary>
///     Describes a single failure encountered while extracting documentation from an assembly — a type that could not
///     be loaded, a <c>[DocumentedBy]</c> reference that could not be resolved, or a documentation factory that threw.
/// </summary>
public sealed class ErrorDocumentationExtractionFailure {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new <see cref="ErrorDocumentationExtractionFailure" />.
    /// </summary>
    /// <param name="typeName">The full name of the type the failure relates to.</param>
    /// <param name="memberName">The factory / documentation method involved, or <c>null</c> for type-level failures.</param>
    /// <param name="message">A human-readable description of what went wrong.</param>
    /// <param name="exception">The underlying exception, when the failure originated from one; otherwise <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="typeName" /> or <paramref name="message" /> is <c>null</c>.
    /// </exception>
    public ErrorDocumentationExtractionFailure(string typeName, string? memberName, string message, Exception? exception) {
        TypeName   = typeName ?? throw new ArgumentNullException(nameof(typeName));
        MemberName = memberName;
        Message    = message ?? throw new ArgumentNullException(nameof(message));
        Exception  = exception;
    }

    #endregion

    /// <summary>Gets the full name of the type the failure relates to.</summary>
    public string TypeName { get; }

    /// <summary>Gets the factory / documentation method involved, when the failure is method-scoped; otherwise <c>null</c>.</summary>
    public string? MemberName { get; }

    /// <summary>Gets a human-readable description of what went wrong.</summary>
    public string Message { get; }

    /// <summary>Gets the underlying exception, when the failure originated from one; otherwise <c>null</c>.</summary>
    public Exception? Exception { get; }

    /// <inheritdoc />
    public override string ToString() {
        string where = MemberName is null ? TypeName : $"{TypeName}.{MemberName}";

        return Exception is null
            ? $"{where}: {Message}"
            : $"{where}: {Message} ({Exception.GetType().Name}: {Exception.Message})";
    }

}
