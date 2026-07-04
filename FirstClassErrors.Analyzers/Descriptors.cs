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
        helpLinkUri: HelpLinks.For(DiagnosticIds.DuplicateErrorCode),
        customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

    public static readonly DiagnosticDescriptor EmptyErrorCode = new(
        id: DiagnosticIds.EmptyErrorCode,
        title: "Error code must not be empty",
        messageFormat: "Error code must not be null, empty or whitespace",
        category: DiagnosticCategories.ErrorCodes,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ErrorCode.Create requires a non-empty code; an empty or whitespace literal throws an ArgumentException at runtime.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.EmptyErrorCode));

<<<<<<< 1af5e392cae2440e9940e433595f2ca8292bd0fc
<<<<<<< b99d792aab6c528239b0e92feef667084d9dea0a
<<<<<<< b57606bb883403c1b1a6d14247e5e03a167aa892
=======
>>>>>>> 25f33ab91f85a53fce3220cf3b67a341780288de
=======
    public static readonly DiagnosticDescriptor NonLiteralErrorCode = new(
        id: DiagnosticIds.NonLiteralErrorCode,
        title: "Error code is not a compile-time literal",
        messageFormat: "Error code is computed at runtime; duplicate-code analysis (FCE001) cannot verify it",
        category: DiagnosticCategories.ErrorCodes,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Only literal error codes can be checked statically. A code built at runtime is a blind spot for duplicate detection; this rule is opt-in for teams that want codes to stay literal.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.NonLiteralErrorCode));

<<<<<<< 95f74d7908a30384e6792240de23c0551ded0a78
>>>>>>> ef8557a936a90c63d0fd16092e81044e7e626d7d
=======
    public static readonly DiagnosticDescriptor InvalidErrorCodeFormat = new(
        id: DiagnosticIds.InvalidErrorCodeFormat,
        title: "Error code does not follow the UPPER_SNAKE_CASE convention",
        messageFormat: "Error code '{0}' does not match the expected UPPER_SNAKE_CASE format",
        category: DiagnosticCategories.ErrorCodes,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "A consistent code format keeps catalogs and logs scannable. This convention check is opt-in.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.InvalidErrorCodeFormat));

>>>>>>> 004ead8f5bec3b420b50ac32c98ca284b5a09f29
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

<<<<<<< 2cd7e88849d2856333ec71c2146d75e038de3f35
<<<<<<< 993934e7f8182457844cd4b01cded521601592e6
=======
>>>>>>> 8229f61d518a97b78334466dffa438b4a7426c3d
=======
>>>>>>> 25f33ab91f85a53fce3220cf3b67a341780288de
=======
>>>>>>> 42551f455f05a68042840626428e601821a47626
=======
>>>>>>> 1110bd90366a90aca8cc009aea4091673f8c9dee
=======
    public static readonly DiagnosticDescriptor UnusedToExceptionResult = new(
        id: DiagnosticIds.UnusedToExceptionResult,
        title: "The result of ToException() is not used",
        messageFormat: "The result of ToException() is discarded; did you mean to throw it?",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ToException() only builds an exception; discarding it as a standalone statement means nothing is thrown and the error is lost.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.UnusedToExceptionResult));

<<<<<<< 5494378d3d4e079a3405b6b07d3c49987a45b79c
>>>>>>> e82f3b87253ce42f9e0497e6d8a41054cd608b17
=======
    public static readonly DiagnosticDescriptor ErrorFactoryNotDocumented = new(
        id: DiagnosticIds.ErrorFactoryNotDocumented,
        title: "Error factory is not documented",
        messageFormat: "Factory '{0}' returns an error but has no [DocumentedBy]; it will not appear in the generated documentation",
        category: DiagnosticCategories.DocumentationWiring,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A non-private static factory in a [ProvidesErrorsFor] type that returns an Error is expected to carry [DocumentedBy]; without it the error is left out of the generated catalog.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.ErrorFactoryNotDocumented));

<<<<<<< 906c991f34df57e7390a0e7bf1e8bfc00c89a922
>>>>>>> e8ea977457cba6404369a7eb5c5530147d4ee52d
=======
    public static readonly DiagnosticDescriptor MultipleFactoriesShareDocumentation = new(
        id: DiagnosticIds.MultipleFactoriesShareDocumentation,
        title: "Multiple factories share the same documentation",
        messageFormat: "Documentation method '{0}' is referenced by more than one factory; each error should have its own documentation",
        category: DiagnosticCategories.DocumentationWiring,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "One documentation method describes one error (its title, description and examples). Sharing it between factories means at least one error is mis-documented.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.MultipleFactoriesShareDocumentation));

<<<<<<< b2642695b7e235070c2ef66b55d1aedc4dc5159e
>>>>>>> ee4e6faead546d60fd6995f2a279ccc7c2bf7934
=======
    public static readonly DiagnosticDescriptor EmptyExamples = new(
        id: DiagnosticIds.EmptyExamples,
        title: "Documentation declares no examples",
        messageFormat: "WithExamples was called without any example factory; add at least one representative example",
        category: DiagnosticCategories.DocumentationContent,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Examples expose the real messages an error produces; calling WithExamples with no factory yields documentation that shows none.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.EmptyExamples));

<<<<<<< 919b0936d2ff932e1a282dde842a3dcb52e58a3b
>>>>>>> fa43e3873d390a51a58a51b6e9e5119f10aae02a
=======
    public static readonly DiagnosticDescriptor DuplicateDocumentedCode = new(
        id: DiagnosticIds.DuplicateDocumentedCode,
        title: "Duplicate documented error code",
        messageFormat: "Error code '{0}' is produced by more than one documented factory; documentation extraction keeps only one of them",
        category: DiagnosticCategories.DocumentationContent,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Documentation extraction groups by error code and keeps a single entry per code. Two documented factories that share the same code field silently collapse to one in the catalog.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DuplicateDocumentedCode),
        customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

<<<<<<< 8b1cd7af60fadf279bc7a6db4401d132a5b4cda7
>>>>>>> fe13baf310cee0ef8a50260c9618a900c9c052c8
=======
    public static readonly DiagnosticDescriptor ExampleDoesNotCallDocumentedFactory = new(
        id: DiagnosticIds.ExampleDoesNotCallDocumentedFactory,
        title: "Documentation example does not construct the documented error",
        messageFormat: "This example does not call any factory of '{0}'; an example should build the error it documents",
        category: DiagnosticCategories.DocumentationContent,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Examples are meant to expose the real messages of the documented error, so each should invoke a factory of the type that declares the documentation.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.ExampleDoesNotCallDocumentedFactory));

>>>>>>> 6b9bac398957700cc935135cb9decc007d4cdf60
}
