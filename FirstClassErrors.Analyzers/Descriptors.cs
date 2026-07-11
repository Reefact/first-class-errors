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
        description: "An ErrorCode is compared by value, so the same literal code created more than once yields equal instances that documentation extraction and lookups collapse into a single identity. Detection is per-compilation and limited to literal codes.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DuplicateErrorCode),
        WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor EmptyErrorCode = new(
        id: DiagnosticIds.EmptyErrorCode,
        title: "Error code must not be empty",
        messageFormat: "Error code must not be null, empty or whitespace",
        category: DiagnosticCategories.ErrorCodes,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ErrorCode.Create requires a non-empty code; an empty or whitespace literal throws an ArgumentException at runtime.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.EmptyErrorCode));

    public static readonly DiagnosticDescriptor NonLiteralErrorCode = new(
        id: DiagnosticIds.NonLiteralErrorCode,
        title: "Error code is not a compile-time literal",
        messageFormat: "Error code is computed at runtime; duplicate-code analysis (FCE001) cannot verify it",
        category: DiagnosticCategories.ErrorCodes,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Only literal error codes can be checked statically. A code built at runtime is a blind spot for duplicate detection; this rule is opt-in for teams that want codes to stay literal.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.NonLiteralErrorCode));

    public static readonly DiagnosticDescriptor InvalidErrorCodeFormat = new(
        id: DiagnosticIds.InvalidErrorCodeFormat,
        title: "Error code does not follow the UPPER_SNAKE_CASE convention",
        messageFormat: "Error code '{0}' does not match the expected UPPER_SNAKE_CASE format",
        category: DiagnosticCategories.ErrorCodes,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "A consistent code format keeps catalogs and logs scannable. This convention check is opt-in.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.InvalidErrorCodeFormat));

    public static readonly DiagnosticDescriptor TooGenericErrorCode = new(
        id: DiagnosticIds.TooGenericErrorCode,
        title: "Error code is too generic",
        messageFormat: "Error code '{0}' is too generic to identify a specific failure",
        category: DiagnosticCategories.ErrorCodes,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "A code such as ERROR or INVALID carries no diagnostic value. Prefer a code that names the specific condition. Opt-in.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.TooGenericErrorCode));

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

    public static readonly DiagnosticDescriptor MultipleFactoriesShareDocumentation = new(
        id: DiagnosticIds.MultipleFactoriesShareDocumentation,
        title: "Multiple factories share the same documentation",
        messageFormat: "Documentation method '{0}' is referenced by more than one factory; each error should have its own documentation",
        category: DiagnosticCategories.DocumentationWiring,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "One documentation method describes one error (its title, description and examples). Sharing it between factories means at least one error is mis-documented.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.MultipleFactoriesShareDocumentation));

    public static readonly DiagnosticDescriptor EmptyExamples = new(
        id: DiagnosticIds.EmptyExamples,
        title: "Documentation declares no examples",
        messageFormat: "WithExamples was called without any example factory; add at least one representative example",
        category: DiagnosticCategories.DocumentationContent,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Examples expose the real messages an error produces; calling WithExamples with no factory yields documentation that shows none.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.EmptyExamples));

    public static readonly DiagnosticDescriptor DuplicateDocumentedCode = new(
        id: DiagnosticIds.DuplicateDocumentedCode,
        title: "Duplicate documented error code",
        messageFormat: "Error code '{0}' is produced by more than one documented factory; documentation extraction keeps only one of them",
        category: DiagnosticCategories.DocumentationContent,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Documentation extraction groups by error code and keeps a single entry per code. Two documented factories that share the same code field silently collapse to one in the catalog.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DuplicateDocumentedCode),
        WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor ExampleDoesNotCallDocumentedFactory = new(
        id: DiagnosticIds.ExampleDoesNotCallDocumentedFactory,
        title: "Documentation example does not construct the documented error",
        messageFormat: "This example does not call any factory of '{0}'; an example should build the error it documents",
        category: DiagnosticCategories.DocumentationContent,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Examples are meant to expose the real messages of the documented error, so each should invoke a factory of the type that declares the documentation.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.ExampleDoesNotCallDocumentedFactory));

    public static readonly DiagnosticDescriptor ShortMessageSameAsDetailedMessage = new(
        id: DiagnosticIds.ShortMessageSameAsDetailedMessage,
        title: "Short message duplicates the detailed message",
        messageFormat: "The short public message is identical to the detailed message; give the caller a shorter summary",
        category: DiagnosticCategories.DocumentationContent,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The short message is a public summary and the detailed message an optional public detail; making them identical usually signals a copy-paste.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.ShortMessageSameAsDetailedMessage));

    public static readonly DiagnosticDescriptor DocumentationTitleTooGeneric = new(
        id: DiagnosticIds.DocumentationTitleTooGeneric,
        title: "Documentation title is too generic",
        messageFormat: "Documentation title '{0}' is too generic; state what the error is",
        category: DiagnosticCategories.DocumentationContent,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "A title such as \"Error\" or \"Invalid value\" tells the reader nothing. A good title names the condition (e.g. \"Temperature below absolute zero\"). Opt-in.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DocumentationTitleTooGeneric));

}
