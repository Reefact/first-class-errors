namespace FirstClassErrors;

/// <summary>
///     Represents the base class for application exceptions that are designed to be diagnosable, identifiable, and
///     observable beyond their raw message and stack trace.
/// </summary>
public abstract class DiagnosableException : Exception {

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
    ///     Gets the <see cref="Error" /> instance associated with this exception.
    /// </summary>
    /// <value>
    ///     The <see cref="Error" /> instance that provides detailed information about the exception.
    /// </value>
    /// <remarks>
    ///     The full diagnostic payload — error code, messages, context and inner errors — is carried by this
    ///     <see cref="Error" />. Inner errors are surfaced through <see cref="FirstClassErrors.Error.InnerErrors" />
    ///     and are intentionally <b>not</b> mirrored onto <see cref="Exception.InnerException" />; traverse
    ///     <c>exception.Error.InnerErrors</c> to walk the diagnostic chain.
    /// </remarks>
    public Error Error { get; }

}