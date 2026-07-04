using Microsoft.CodeAnalysis;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     The <see cref="DiagnosticDescriptor" /> for every FirstClassErrors rule. One field per FCExxx, added as the
///     rule is implemented.
/// </summary>
internal static class Descriptors {

    public static readonly DiagnosticDescriptor DuplicateErrorCode = new(
        id: DiagnosticIds.DuplicateErrorCode,
        title: "Duplicate error code",
        messageFormat: "Error code '{0}' is created more than once; each ErrorCode must be unique",
        category: DiagnosticCategories.ErrorCodes,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ErrorCode.Create registers each code in a process-wide set and throws when the same code is created twice. Detection is per-compilation and limited to literal codes.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DuplicateErrorCode));

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

    public static readonly DiagnosticDescriptor DocumentedByInvalidSignature = new(
        id: DiagnosticIds.DocumentedByInvalidSignature,
        title: "[DocumentedBy] target has an invalid signature",
        messageFormat: "Method '{0}' must be static, parameterless and return ErrorDocumentation to be used by [DocumentedBy]",
        category: DiagnosticCategories.DocumentationWiring,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The documentation factory referenced by [DocumentedBy] is invoked as a static parameterless method returning ErrorDocumentation; any other shape is skipped at extraction time.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DocumentedByInvalidSignature));

    public static readonly DiagnosticDescriptor DocumentedByWithoutProvidesErrorsFor = new(
        id: DiagnosticIds.DocumentedByWithoutProvidesErrorsFor,
        title: "[DocumentedBy] used in a type without [ProvidesErrorsFor]",
        messageFormat: "Type '{0}' declares [DocumentedBy] factories but is missing [ProvidesErrorsFor]; its error documentation will be silently ignored",
        category: DiagnosticCategories.DocumentationWiring,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Documentation extraction only scans types annotated with [ProvidesErrorsFor]; [DocumentedBy] methods on an unannotated type are never extracted.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DocumentedByWithoutProvidesErrorsFor));

    public static readonly DiagnosticDescriptor UnusedToExceptionResult = new(
        id: DiagnosticIds.UnusedToExceptionResult,
        title: "The result of ToException() is not used",
        messageFormat: "The result of ToException() is discarded; did you mean to throw it?",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ToException() only builds an exception; discarding it as a standalone statement means nothing is thrown and the error is lost.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.UnusedToExceptionResult));

    public static readonly DiagnosticDescriptor ErrorFactoryNotDocumented = new(
        id: DiagnosticIds.ErrorFactoryNotDocumented,
        title: "Error factory is not documented",
        messageFormat: "Factory '{0}' returns an error but has no [DocumentedBy]; it will not appear in the generated documentation",
        category: DiagnosticCategories.DocumentationWiring,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A non-private static factory in a [ProvidesErrorsFor] type that returns an Error is expected to carry [DocumentedBy]; without it the error is left out of the generated catalog.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.ErrorFactoryNotDocumented));

}
