namespace FirstClassErrors.GenDoc;

/// <summary>
///     Describes a single failure encountered while extracting documentation from an assembly — a type that could not
///     be loaded, a <c>[DocumentedBy]</c> reference that could not be resolved, a documentation factory that threw, or
///     a duplicate error code whose documentation had to be dropped to keep the catalog single-valued.
/// </summary>
public sealed class ErrorDocumentationExtractionFailure {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new <see cref="ErrorDocumentationExtractionFailure" />.
    /// </summary>
    /// <param name="typeName">The full name of the type the failure relates to.</param>
    /// <param name="memberName">The factory / documentation method involved, or <c>null</c> for type-level failures.</param>
    /// <param name="message">A human-readable description of what went wrong.</param>
    /// <param name="exceptionDetail">
    ///     The textual detail of the underlying exception (its <see cref="object.ToString" />), when the failure
    ///     originated from one; otherwise <c>null</c>. Kept as text — not a live <see cref="System.Exception" /> — so the
    ///     failure stays serializable and free of any dependency on a runtime load context.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="typeName" /> or <paramref name="message" /> is <c>null</c>.
    /// </exception>
    public ErrorDocumentationExtractionFailure(string typeName, string? memberName, string message, string? exceptionDetail) {
        TypeName        = typeName ?? throw new ArgumentNullException(nameof(typeName));
        MemberName      = memberName;
        Message         = message ?? throw new ArgumentNullException(nameof(message));
        ExceptionDetail = exceptionDetail;
    }

    #endregion

    /// <summary>Gets the full name of the type the failure relates to.</summary>
    public string TypeName { get; }

    /// <summary>Gets the factory / documentation method involved, when the failure is method-scoped; otherwise <c>null</c>.</summary>
    public string? MemberName { get; }

    /// <summary>Gets a human-readable description of what went wrong.</summary>
    public string Message { get; }

    /// <summary>Gets the textual detail of the underlying exception, when the failure originated from one; otherwise <c>null</c>.</summary>
    public string? ExceptionDetail { get; }

    /// <inheritdoc />
    public override string ToString() {
        string where = MemberName is null ? TypeName : $"{TypeName}.{MemberName}";

        return ExceptionDetail is null
            ? $"{where}: {Message}"
            : $"{where}: {Message} ({ExceptionDetail})";
    }

}
