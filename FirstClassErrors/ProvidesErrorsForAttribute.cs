namespace FirstClassErrors;

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

    /// <summary>
    ///     Gets or sets an optional human description of this error source. When set, it is rendered as an
    ///     introduction to the source's group in the generated documentation.
    /// </summary>
    /// <remarks>
    ///     When <see cref="DescriptionResourceType" /> is also set, this value is treated as a <b>resource key</b>
    ///     looked up in that type's resources (localized to the current UI culture) rather than as literal text —
    ///     mirroring the <c>[Display(ResourceType = …, Description = …)]</c> pattern of data annotations.
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the type whose resources hold the localized <see cref="Description" />. When set, the extractor
    ///     resolves <see cref="Description" /> as a resource key against this type's <see cref="System.Resources.ResourceManager" />
    ///     under the extraction's current UI culture; when <c>null</c>, <see cref="Description" /> is used verbatim.
    /// </summary>
    public Type? DescriptionResourceType { get; set; }

}