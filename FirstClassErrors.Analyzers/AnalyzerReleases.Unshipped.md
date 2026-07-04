; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|--------------------------------------|----------|-------------------------------------
FCE001  | FirstClassErrors.ErrorCodes          | Error    | DuplicateErrorCodeAnalyzer
FCE002  | FirstClassErrors.ErrorCodes          | Error    | EmptyErrorCodeAnalyzer
FCE003  | FirstClassErrors.ErrorCodes          | Info     | NonLiteralErrorCodeAnalyzer (disabled by default)
FCE004  | FirstClassErrors.ErrorCodes          | Info     | InvalidErrorCodeFormatAnalyzer (disabled by default)
FCE005  | FirstClassErrors.ErrorCodes          | Info     | TooGenericErrorCodeAnalyzer (disabled by default)
FCE006  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByTargetNotFoundAnalyzer
FCE007  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByInvalidSignatureAnalyzer
FCE008  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByWithoutProvidesErrorsForAnalyzer
FCE009  | FirstClassErrors.DocumentationWiring | Warning  | ErrorFactoryNotDocumentedAnalyzer
FCE010  | FirstClassErrors.DocumentationWiring | Warning  | MultipleFactoriesShareDocumentationAnalyzer
FCE011  | FirstClassErrors.DocumentationContent| Error    | DuplicateDocumentedCodeAnalyzer
FCE012  | FirstClassErrors.DocumentationContent| Warning  | EmptyExamplesAnalyzer
FCE013  | FirstClassErrors.DocumentationContent| Warning  | ExampleDoesNotCallDocumentedFactoryAnalyzer
FCE014  | FirstClassErrors.DocumentationContent| Info     | ShortMessageSameAsDetailedMessageAnalyzer
FCE015  | FirstClassErrors.DocumentationContent| Info     | DocumentationTitleTooGenericAnalyzer (disabled by default)
FCE016  | FirstClassErrors.Usage               | Warning  | UnusedToExceptionResultAnalyzer
