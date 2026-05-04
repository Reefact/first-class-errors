namespace DiagnosableExceptions;

/// <summary>
///     Represents the base class for application exceptions that are designed to be diagnosable, identifiable, and
///     observable beyond their raw message and stack trace.
/// </summary>
public abstract class DiagnosableException : Exception {

    #region Statics members declarations

    private static IReadOnlyList<Exception> CreateInnerExceptionList(Exception? innerException) {
        if (innerException is null) { return CreateInnerExceptionList(); }

        return Array.AsReadOnly([innerException]);
    }

    private static IReadOnlyList<Exception> CreateInnerExceptionList() {
        return Array.AsReadOnly(Array.Empty<Exception>());
    }

    private static IReadOnlyList<Exception> CreateInnerExceptionList(IEnumerable<Exception>? innerExceptions) {
        if (innerExceptions is null) { return CreateInnerExceptionList(); }

        Exception[] array = innerExceptions as Exception[] ?? innerExceptions.ToArray();

        return Array.AsReadOnly(array);
    }

    private static ErrorContext BuildContext(Action<ErrorContextBuilder>? configureContext) {
        if (configureContext is null) { return ErrorContext.Empty; }

        ErrorContextBuilder builder = new();
        configureContext(builder);

        return builder.Build();
    }

    #endregion

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class with the specified
    ///     <paramref name="error" />.
    /// </summary>
    /// <param name="error">
    ///     An instance of the <see cref="Error" /> class that provides detailed information about the exception.
    /// </param>
    protected DiagnosableException(Error error)
        : base(error.DetailedMessage) {
        Error = error;
    }

    #endregion

    /// <summary>
    ///     Gets or sets the <see cref="Error" /> instance associated with this exception.
    /// </summary>
    /// <value>
    ///     The <see cref="Error" /> instance that provides detailed information about the exception.
    /// </value>
    public Error Error { get; set; }

}