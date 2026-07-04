namespace FirstClassErrors.Analyzers;

/// <summary>
///     Stable identifiers for every FirstClassErrors diagnostic. The number is only a stable handle; rules are grouped
///     for the user through <see cref="DiagnosticCategories" />, not through contiguous numbering.
/// </summary>
internal static class DiagnosticIds {

    // Category: ErrorCodes
    public const string DuplicateErrorCode  = "FCE001";
    public const string EmptyErrorCode      = "FCE002";
    public const string NonLiteralErrorCode = "FCE003";
    public const string InvalidErrorCodeFormat = "FCE004";
    public const string TooGenericErrorCode = "FCE005";

    // Category: DocumentationWiring
    public const string DocumentedByTargetNotFound         = "FCE006";
    public const string DocumentedByInvalidSignature       = "FCE007";
    public const string DocumentedByWithoutProvidesErrorsFor = "FCE008";
    public const string ErrorFactoryNotDocumented          = "FCE009";
    public const string MultipleFactoriesShareDocumentation = "FCE010";

    // Category: DocumentationContent
    public const string DuplicateDocumentedCode          = "FCE011";
    public const string EmptyExamples                    = "FCE012";
    public const string ExampleDoesNotCallDocumentedFactory = "FCE013";
    public const string ShortMessageSameAsDetailedMessage = "FCE014";
    public const string DocumentationTitleTooGeneric     = "FCE015";

    // Category: Usage
    public const string UnusedToExceptionResult = "FCE016";

}
