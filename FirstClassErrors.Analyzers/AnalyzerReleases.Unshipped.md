; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|--------------------------------------|----------|-------------------------------------
FCE001  | FirstClassErrors.ErrorCodes          | Error    | DuplicateErrorCodeAnalyzer
FCE002  | FirstClassErrors.ErrorCodes          | Error    | EmptyErrorCodeAnalyzer
FCE003  | FirstClassErrors.ErrorCodes          | Disabled | NonLiteralErrorCodeAnalyzer
FCE004  | FirstClassErrors.ErrorCodes          | Disabled | InvalidErrorCodeFormatAnalyzer
FCE005  | FirstClassErrors.ErrorCodes          | Disabled | TooGenericErrorCodeAnalyzer
FCE006  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByTargetNotFoundAnalyzer
FCE007  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByInvalidSignatureAnalyzer
FCE008  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByWithoutProvidesErrorsForAnalyzer
FCE009  | FirstClassErrors.DocumentationWiring | Warning  | ErrorFactoryNotDocumentedAnalyzer
FCE010  | FirstClassErrors.DocumentationWiring | Warning  | MultipleFactoriesShareDocumentationAnalyzer
FCE011  | FirstClassErrors.DocumentationContent| Error    | DuplicateDocumentedCodeAnalyzer
FCE012  | FirstClassErrors.DocumentationContent| Warning  | EmptyExamplesAnalyzer
FCE013  | FirstClassErrors.DocumentationContent| Warning  | ExampleDoesNotCallDocumentedFactoryAnalyzer
FCE014  | FirstClassErrors.DocumentationContent| Info     | ShortMessageSameAsDetailedMessageAnalyzer
FCE015  | FirstClassErrors.DocumentationContent| Disabled | DocumentationTitleTooGenericAnalyzer
FCE016  | FirstClassErrors.Usage               | Warning  | UnusedToExceptionResultAnalyzer
FCE017  | FirstClassErrors.Usage               | Disabled | SensitiveDataInErrorContextAnalyzer
FCE018  | FirstClassErrors.Usage               | Disabled | OversizedErrorContextValueAnalyzer
