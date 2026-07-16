namespace FirstClassErrors.GenDoc;

/// <summary>
///     Raised when the documentation generation fails. The failure is carried as a first-class
///     <see cref="FirstClassErrors.Error" /> — with its stable <c>GENDOC_</c>-prefixed code, structured context and
///     generated documentation — accessible through <see cref="DiagnosableException.Error" />: the tool that documents
///     errors reports its own failures with the model it promotes.
/// </summary>
public sealed class SolutionDocumentationGenerationException : DiagnosableException {

    #region Constructors declarations

    /// <summary>
    ///     Initializes the exception with the <paramref name="error" /> describing the generation failure.
    /// </summary>
    /// <param name="error">The error describing the failure (see <see cref="DocumentationRequestError" /> and <see cref="DocumentationToolchainError" />).</param>
    public SolutionDocumentationGenerationException(Error error)
        : base(error) { }

    /// <summary>
    ///     Initializes the exception with the <paramref name="error" /> describing the generation failure and the
    ///     runtime <paramref name="innerException" /> that caused it.
    /// </summary>
    /// <param name="error">The error describing the failure (see <see cref="DocumentationRequestError" /> and <see cref="DocumentationToolchainError" />).</param>
    /// <param name="innerException">The runtime exception that caused the failure, preserved with its stack trace.</param>
    public SolutionDocumentationGenerationException(Error error, Exception innerException)
        : base(error, innerException) { }

    #endregion

}
