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

    public static readonly DiagnosticDescriptor SensitiveDataInErrorContext = new(
        id: DiagnosticIds.SensitiveDataInErrorContext,
        title: "Error context key looks like it carries sensitive data",
        messageFormat: "Context key '{0}' looks like it carries a secret or personal data; keep passwords, tokens and secrets out of error context",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "Error context is copied into logs, dashboards and error catalogs, so a secret placed there leaks wherever the error travels. This rule matches the key name against a curated set of secret/credential/PII terms (password, token, secret, api key, connection string, credit card, SSN...). It is a name heuristic: it cannot see the runtime value, so it neither proves a leak nor catches a secret hidden behind an innocuous key name. Opt-in; may need per-project tuning.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.SensitiveDataInErrorContext));

    public static readonly DiagnosticDescriptor OversizedErrorContextValue = new(
        id: DiagnosticIds.OversizedErrorContextValue,
        title: "Error context value type is a large payload",
        messageFormat: "Context key '{0}' stores '{1}'; error context is for small, loggable values, not files, streams or byte buffers",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Error context should hold small, serializable facts that log cleanly. A key typed as a byte array, a Stream or a FileInfo carries a whole file or buffer into every log line and error-catalog entry, bloating output and often smuggling sensitive data along with it. Detection is based on the key's declared value type. Opt-in.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.OversizedErrorContextValue));

    public static readonly DiagnosticDescriptor TryCatchesTooBroadly = new(
        id: DiagnosticIds.TryCatchesTooBroadly,
        title: "Outcome.Try catches a too-broad exception type",
        messageFormat: "Outcome.Try catches '{0}'; catch the specific exception the operation is documented to throw, not a near-root type that also swallows bugs",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Outcome.Try is meant to catch the single exception type that denotes an anticipated failure and let everything else propagate. Catching System.Exception turns unexpected bugs (a null dereference, an invalid state) into anticipated errors and defeats the purpose of Outcome. Name the specific exception instead; if a boundary genuinely must map every failure, do it explicitly rather than through Try.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.TryCatchesTooBroadly));

    public static readonly DiagnosticDescriptor TryCatchesRichProtocolException = new(
        id: DiagnosticIds.TryCatchesRichProtocolException,
        title: "Outcome.Try catches a protocol failure that carries more than the exception",
        messageFormat: "Outcome.Try catches '{0}', a protocol failure carrying status or result data beyond the exception; review whether a dedicated adapter should inspect the result and preserve that structured information",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "HTTP, socket and database failures are not fully described by the exception: a status code, a provider error number or a response body carries the real signal, one exception type spans several distinct failures with different transience, and a request timeout surfaces as an OperationCanceledException that Try lets through. Outcome.Try can reduce all of that to 'it threw'; the mapper can still read the status off the caught exception, but needing to is a sign the failure wants a dedicated result-inspecting adapter. Advisory and opt-in.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.TryCatchesRichProtocolException));

    public static readonly DiagnosticDescriptor TryCatchesCancellation = new(
        id: DiagnosticIds.TryCatchesCancellation,
        title: "Outcome.Try catches a cancellation type, making the catch unreachable",
        messageFormat: "Outcome.Try catches '{0}', a cancellation type; Outcome.Try always lets cancellation propagate, so this catch is unreachable and the mapper never runs",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Outcome.Try guards its catch with 'when (exception is not OperationCanceledException)' so a cancellation is never turned into an error — it always propagates. Binding TException to OperationCanceledException (or a subtype such as TaskCanceledException) therefore produces a catch that can never engage: the exception filter becomes a contradiction ('is an OperationCanceledException and is not one'), the mapper never runs, and no Outcome is produced. This is always a mistake, and it is silent (an always-false filter is not a compile error). Cancellation cannot be modelled as a Try failure: remove the cancellation handling from Try, or catch a specific non-cancellation exception.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.TryCatchesCancellation));

    public static readonly DiagnosticDescriptor PreferNonThrowingAlternativeToTry = new(
        id: DiagnosticIds.PreferNonThrowingAlternativeToTry,
        title: "Outcome.Try wraps an operation that has a non-throwing alternative",
        messageFormat: "Outcome.Try wraps '{0}'; a non-throwing '{1}' is available — consider mapping its result instead of catching",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a wrapped call already has a non-throwing counterpart — a 'bool TryParse(..., out T)' or 'TryCreate' with a matching drop-in signature — there is usually no exception worth catching, so Outcome.Try adds cost and hides the cheaper path. It fires wherever such a counterpart resolves, regardless of where the wrapped type is declared. The rule is framework-aware: it fires only when the counterpart actually resolves, with a compatible signature, in the compilation being analyzed, so a target framework that lacks it (older .NET Standard / .NET Framework, where MailAddress.TryCreate or Convert.TryFromBase64String do not exist) is never flagged. It is an advisory (a suggestion, not an equivalence claim): a structurally-matching TryXxx may still normalize its input, diverge on culture, or report a different set of failures than the exception you catch (int.Parse also throws on overflow, which int.TryParse folds into false), and the TryXxx form has no exception to hand your mapper — so confirm it behaves identically before rewriting, and suppress the rule (SuppressMessage / #pragma) where it does not fit. Detection is limited to a single static call or a constructor in the lambda body, whose signature the counterpart must match exactly.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.PreferNonThrowingAlternativeToTry));

}
