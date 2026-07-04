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

<<<<<<< b99d792aab6c528239b0e92feef667084d9dea0a
<<<<<<< b57606bb883403c1b1a6d14247e5e03a167aa892
=======
>>>>>>> 25f33ab91f85a53fce3220cf3b67a341780288de
    public static readonly DiagnosticDescriptor DocumentedByTargetNotFound = new(
        id: DiagnosticIds.DocumentedByTargetNotFound,
        title: "Documentation method referenced by [DocumentedBy] was not found",
        messageFormat: "No method named '{0}' exists on the type; [DocumentedBy] cannot be resolved and this error will not be documented",
        category: DiagnosticCategories.DocumentationWiring,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[DocumentedBy] references its documentation method by name; a name that resolves to nothing is silently skipped when documentation is extracted.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DocumentedByTargetNotFound));

<<<<<<< 76f96d9b547b96b7d8a14048a2f6036c69baf88d
<<<<<<< b99d792aab6c528239b0e92feef667084d9dea0a
=======
>>>>>>> 42551f455f05a68042840626428e601821a47626
    public static readonly DiagnosticDescriptor DocumentedByInvalidSignature = new(
        id: DiagnosticIds.DocumentedByInvalidSignature,
        title: "[DocumentedBy] target has an invalid signature",
        messageFormat: "Method '{0}' must be static, parameterless and return ErrorDocumentation to be used by [DocumentedBy]",
        category: DiagnosticCategories.DocumentationWiring,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The documentation factory referenced by [DocumentedBy] is invoked as a static parameterless method returning ErrorDocumentation; any other shape is skipped at extraction time.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DocumentedByInvalidSignature));

<<<<<<< 993934e7f8182457844cd4b01cded521601592e6
<<<<<<< 76f96d9b547b96b7d8a14048a2f6036c69baf88d
=======
>>>>>>> 1110bd90366a90aca8cc009aea4091673f8c9dee
    public static readonly DiagnosticDescriptor DocumentedByWithoutProvidesErrorsFor = new(
        id: DiagnosticIds.DocumentedByWithoutProvidesErrorsFor,
        title: "[DocumentedBy] used in a type without [ProvidesErrorsFor]",
        messageFormat: "Type '{0}' declares [DocumentedBy] factories but is missing [ProvidesErrorsFor]; its error documentation will be silently ignored",
        category: DiagnosticCategories.DocumentationWiring,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Documentation extraction only scans types annotated with [ProvidesErrorsFor]; [DocumentedBy] methods on an unannotated type are never extracted.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DocumentedByWithoutProvidesErrorsFor));

<<<<<<< 993934e7f8182457844cd4b01cded521601592e6
=======
>>>>>>> 8229f61d518a97b78334466dffa438b4a7426c3d
=======
>>>>>>> 25f33ab91f85a53fce3220cf3b67a341780288de
=======
>>>>>>> 42551f455f05a68042840626428e601821a47626
=======
>>>>>>> 1110bd90366a90aca8cc009aea4091673f8c9dee
}
