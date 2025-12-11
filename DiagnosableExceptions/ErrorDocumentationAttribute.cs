namespace Reefact.DiagnosableExceptions;

/// <summary>
///     An attribute used to document the specific error produced by an exception factory method.
/// </summary>
/// <remarks>
///     This attribute is applied to factory methods that produce exceptions. Each factory method is expected to
///     produce an exception with a unique error code, as determined by the developer. The attribute provides metadata
///     to describe the error, including:
///     <list type="bullet">
///         <item>A <see cref="Title" /> that summarizes the error.</item>
///         <item>An <see cref="Explanation" /> that explains the context or cause of the error.</item>
///         <item>An optional <see cref="BusinessRule" /> that describes the domain rule violated by the error.</item>
///         <item>
///             Optional diagnostic information, including a <see cref="DiagnosticType" /> and
///             <see cref="DiagnosticMemberName" />,     to assist in identifying and resolving the issue.
///         </item>
///     </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ErrorDocumentationAttribute : Attribute {

    /// <summary>
    ///     Gets a short, human-readable title summarizing the error.
    /// </summary>
    /// <remarks>
    ///     The title provides a concise description of the error, making it easier to identify and understand
    ///     the nature of the issue at a glance.
    /// </remarks>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    ///     Provides a detailed description of the error, explaining what the error represents or the context in which it
    ///     occurs.
    /// </summary>
    /// <remarks>
    ///     This property is intended to describe the nature of the error in a clear and concise manner,
    ///     helping developers and users understand what went wrong. Unlike the cause, which identifies
    ///     why the error occurred, the explanation focuses on describing the error itself.
    /// </remarks>
    public string Explanation { get; set; } = string.Empty;
    /// <summary>
    ///     Gets or sets the description of the business rule violated by the error.
    /// </summary>
    /// <remarks>
    ///     This property provides additional context about the domain rule that was breached,
    ///     helping to clarify the nature of the error in terms of business logic.
    /// </remarks>
    public string? BusinessRule { get; set; } = null;
    /// <summary>
    ///     Gets or sets the type that provides diagnostic information related to the error.
    /// </summary>
    /// <remarks>
    ///     This property is used to specify a type that contains diagnostic details to help identify and resolve the issue.
    ///     The type should typically define members that provide additional context or guidance for troubleshooting.
    /// </remarks>
    public Type? DiagnosticType { get; set; } = null;
    /// <summary>
    ///     Gets or sets the name of the diagnostic member associated with the error.
    /// </summary>
    /// <remarks>
    ///     This property is used to specify the name of a member within the type defined by <see cref="DiagnosticType" />
    ///     that provides diagnostic information about the error. This can help developers identify and resolve the issue
    ///     by offering additional context or guidance.
    /// </remarks>
    public string? DiagnosticMemberName { get; set; } = null;

}