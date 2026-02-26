namespace DiagnosableExceptions;

/// <summary>
///     Represents documentation for an entry in the error context, providing details about a specific key, its
///     description, associated value type, and example values.
/// </summary>
/// <remarks>
///     This class is used to describe the structure and metadata of a context entry in error documentation, enabling
///     better understanding and diagnostics of errors.
/// </remarks>
public sealed class ErrorContextEntryDocumentation {

    /// <summary>
    ///     Gets or sets the unique identifier for the context entry in the error documentation.
    /// </summary>
    /// <remarks>
    ///     This property represents the key associated with a specific entry in the error context, which is used to identify
    ///     and categorize the entry within the error documentation.
    /// </remarks>
    public string? Key { get; set; }

    /// <summary>
    ///     Gets or sets the description of the context entry, providing additional details about its purpose or usage within
    ///     the error documentation.
    /// </summary>
    /// <remarks>
    ///     This property is intended to offer a human-readable explanation of the context entry, aiding in diagnostics and
    ///     understanding of the error.
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the type of the value associated with the context entry.
    /// </summary>
    /// <remarks>
    ///     This property specifies the <see cref="System.Type" /> of the value that the context entry represents. It helps in
    ///     understanding the expected data type for the associated context key.
    /// </remarks>
    public Type? ValueType { get; set; }

    /// <summary>
    ///     Gets or sets a collection of example values associated with the context entry.
    /// </summary>
    /// <remarks>
    ///     Example values provide illustrative data that can help understand the typical values associated with this context
    ///     entry. These values are often used for diagnostics or documentation purposes.
    /// </remarks>
    public IReadOnlyList<object?> ExampleValues { get; set; } = [];

}