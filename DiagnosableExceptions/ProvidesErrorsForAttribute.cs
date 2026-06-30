namespace DiagnosableExceptions;

/// <summary>
///     Specifies that the attributed class provides error definitions for a specific source.
/// </summary>
/// <remarks>
///     This attribute is used to associate a class with a source, typically representing a domain model or
///     component, for which the class provides error definitions or factory methods.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ProvidesErrorsForAttribute : Attribute {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProvidesErrorsForAttribute" /> class with the specified source.
    /// </summary>
    /// <param name="source">
    ///     The name of the source (e.g., a domain model or component) for which the attributed class provides error
    ///     definitions.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="source" /> is <c>null</c>, empty, or consists only of white-space characters.
    /// </exception>
    public ProvidesErrorsForAttribute(string source) {
        if (string.IsNullOrWhiteSpace(source)) { throw new ArgumentException("Value cannot be null or whitespace.", nameof(source)); }

        Source = source;
    }

    #endregion

    /// <summary>
    ///     Gets the source associated with the attributed class that provides error definitions.
    /// </summary>
    /// <remarks>
    ///     This property represents the specific source, typically a domain model or component,
    ///     for which the attributed class defines errors or provides error factory methods.
    /// </remarks>
    public string Source { get; }

}