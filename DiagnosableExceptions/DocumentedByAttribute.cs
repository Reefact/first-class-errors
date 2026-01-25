namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Specifies the method that documents the exception produced by the annotated exception factory method.
/// </summary>
public sealed class DocumentedByAttribute : Attribute {

    #region Constructors & Destructor

    /// <summary>
    ///     Initializes a new instance of the <see cref="DocumentedByAttribute" /> class.
    /// </summary>
    /// <param name="documentationMethodName">
    ///     The name of the method that documents the exception produced by the annotated exception factory method.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="documentationMethodName" /> is <c>null</c>, empty, or consists only of white-space
    ///     characters.
    /// </exception>
    public DocumentedByAttribute(string documentationMethodName) {
        if (string.IsNullOrWhiteSpace(documentationMethodName)) { throw new ArgumentException("Value cannot be null or whitespace.", nameof(documentationMethodName)); }

        MethodName = documentationMethodName;
    }

    #endregion

    /// <summary>
    ///     Gets the name of the method that documents the exception produced by the annotated exception factory method.
    /// </summary>
    public string MethodName { get; }

}