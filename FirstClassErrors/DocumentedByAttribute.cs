namespace FirstClassErrors;

/// <summary>
///     Specifies the method that documents the error produced by the annotated error factory method.
/// </summary>
/// <remarks>
///     The named method is resolved at extraction time as a parameterless, non-generic <c>static</c> method declared on
///     the <b>same type</b> as the annotated factory. Inherited members are not considered: a documentation method
///     declared only on a base type is not resolved. Declare the documentation method on the type itself. The FCE006
///     analyzer reports a reference whose target does not exist on the containing type.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
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