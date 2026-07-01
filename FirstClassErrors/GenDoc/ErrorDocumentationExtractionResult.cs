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
        Failures      = failures      ?? throw new ArgumentNullException(nameof(failures));
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