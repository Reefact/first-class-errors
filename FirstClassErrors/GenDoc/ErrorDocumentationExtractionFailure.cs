namespace FirstClassErrors.GenDoc;

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