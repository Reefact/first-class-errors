namespace DiagnosableExceptions;

/// <summary>
///     Represents an error that occurs while building error documentation.
/// </summary>
/// <remarks>
///     This exception indicates a misuse of the documentation builder API,
///     such as inconsistent example factories, null factories, or invalid
///     example outputs. These errors are deterministic and should be fixed
///     by correcting the documentation configuration.
/// </remarks>
public sealed class ErrorDocumentationException : InvalidOperationException {

    #region Static members

    internal static ErrorDocumentationException InconsistentErrorCode(int exampleIndex, string expectedErrorCode, ErrorCode receivedErrorCode) {
        return new ErrorDocumentationException($"All example factories must produce exceptions with the same ErrorCode. Example at index {exampleIndex} produced a different ErrorCode. Expected '{expectedErrorCode}', but received '{receivedErrorCode}'.");
    }

    internal static ErrorDocumentationException AtLeastOneExampleMustBeProvided() {
        return new ErrorDocumentationException("At least one example factory must be provided to build documentation examples.");
    }

    internal static ErrorDocumentationException ExampleFactoryIsNull(int factoryIndex) {
        return new ErrorDocumentationException($"Example factory at index {factoryIndex} is null. All factories must be valid delegates.");
    }

    internal static ErrorDocumentationException ExampleFactoryThrewAnException(int factoryIndex, Exception exception) {
        return new ErrorDocumentationException($"Example factory at index {factoryIndex} threw an exception. Factories must be deterministic and side-effect free.", exception);
    }

    internal static ErrorDocumentationException NullExample(int factoryIndex) {
        return new ErrorDocumentationException($"Example factory at index {factoryIndex} returned null. Factories must return a valid exception instance.");
    }

    #endregion

    #region Constructors & Destructor

    /// <inheritdoc />
    private ErrorDocumentationException(string message)
        : base(message) { }

    /// <inheritdoc />
    private ErrorDocumentationException(string message, Exception innerException)
        : base(message, innerException) { }

    #endregion

}