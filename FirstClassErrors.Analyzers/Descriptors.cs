using Microsoft.CodeAnalysis;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     The <see cref="DiagnosticDescriptor" /> for every FirstClassErrors rule. One field per FCExxx, added as the
///     rule is implemented.
/// </summary>
internal static class Descriptors {

    public static readonly DiagnosticDescriptor EmptyErrorCode = new(
        id: DiagnosticIds.EmptyErrorCode,
        title: "Error code must not be empty",
        messageFormat: "Error code must not be null, empty or whitespace",
        category: DiagnosticCategories.ErrorCodes,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ErrorCode.Create requires a non-empty code; an empty or whitespace literal throws an ArgumentException at runtime.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.EmptyErrorCode));

    public static readonly DiagnosticDescriptor DocumentedByTargetNotFound = new(
        id: DiagnosticIds.DocumentedByTargetNotFound,
        title: "Documentation method referenced by [DocumentedBy] was not found",
        messageFormat: "No method named '{0}' exists on the type; [DocumentedBy] cannot be resolved and this error will not be documented",
        category: DiagnosticCategories.DocumentationWiring,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[DocumentedBy] references its documentation method by name; a name that resolves to nothing is silently skipped when documentation is extracted.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DocumentedByTargetNotFound));

}
