namespace DiagnosableExceptions.GenDoc;

public sealed class SolutionDocumentationGenerationException : Exception {

    #region Constructors declarations

    public SolutionDocumentationGenerationException(string message)
        : base(message) { }

    public SolutionDocumentationGenerationException(string message, Exception innerException)
        : base(message, innerException) { }

    #endregion

}