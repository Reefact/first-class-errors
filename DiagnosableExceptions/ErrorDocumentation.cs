#region Usings declarations

using System.Diagnostics;

#endregion

namespace DiagnosableExceptions;

/// <summary>
///     Represents detailed documentation for an error.
/// </summary>
/// <remarks>
///     This class is used to encapsulate all relevant information about an error, making it easier to document,
///     diagnose, and provide examples for specific error cases.
/// </remarks>
[DebuggerDisplay("{ToString()}")]
public sealed class ErrorDocumentation {

    /// <summary>
    ///     Gets the unique code identifying the type of error.
    /// </summary>
    /// <remarks>
    ///     This code serves as a key reference for production support teams, enabling them to quickly
    ///     identify and categorize the error type for efficient troubleshooting and resolution.
    /// </remarks>
    public string? Code { get; set; }

    /// <summary>
    ///     Gets the title of the error documentation.
    /// </summary>
    /// <remarks>
    ///     The title provides a concise and clear name for the error, summarizing its essence.
    ///     It is intended to be a human-readable identifier for the error, aiding in quick recognition
    ///     and understanding of the issue.
    /// </remarks>
    public string? Title { get; set; }

    /// <summary>
    ///     Gets a detailed explanation of the error.
    /// </summary>
    /// <remarks>
    ///     This property provides a comprehensive description of the error,
    ///     offering additional context and insights to help understand the nature
    ///     and circumstances of the issue.
    /// </remarks>
    public string? Explanation { get; set; }

    /// <summary>
    ///     Gets the business rule associated with the error.
    /// </summary>
    /// <remarks>
    ///     This property provides a description of the business rule that was violated or is relevant
    ///     to the error. It helps in understanding the context of the error in relation to the
    ///     application's business logic.
    /// </remarks>
    public string? BusinessRule { get; set; }

    /// <summary>
    ///     Gets the collection of diagnostics associated with the error.
    /// </summary>
    /// <value>
    ///     A read-only list of <see cref="ErrorDiagnostic" /> instances, where each diagnostic provides
    ///     details about a specific cause of the error and its corresponding corrective action.
    /// </value>
    /// <remarks>
    ///     This property is used to document the potential causes of an error and the recommended
    ///     solutions, aiding in the diagnosis and resolution of the issue.
    /// </remarks>
    public IReadOnlyList<ErrorDiagnostic> Diagnostics { get; set; } = [];

    /// <summary>
    ///     Gets a collection of examples that illustrate specific instances of the error.
    /// </summary>
    /// <remarks>
    ///     Each example provides a detailed and a short description of an error scenario, helping to clarify
    ///     the nature of the error and its potential occurrences.
    /// </remarks>
    public IReadOnlyList<ErrorDescription> Examples { get; set; } = [];
    public Type?   Exception         { get;                set; }
    public Type?   ErrorSource       { get;                set; }
    public string? FactoryMethodName { get;                set; }

    /// <inheritdoc />
    public override string ToString() {
        return Code ?? string.Empty;
    }

}