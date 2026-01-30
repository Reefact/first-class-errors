namespace DiagnosableExceptions.Generation;

public sealed class SolutionDocumentationGenerationException : Exception {

    #region Constructors & Destructor

    public SolutionDocumentationGenerationException(string message)
        : base(message) { }

    public SolutionDocumentationGenerationException(string message, Exception innerException)
        : base(message, innerException) { }

    #endregion

}