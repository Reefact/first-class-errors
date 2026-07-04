; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|--------------------------------------|----------|-------------------------------------
FCE001  | FirstClassErrors.ErrorCodes          | Error    | DuplicateErrorCodeAnalyzer
FCE002  | FirstClassErrors.ErrorCodes          | Error    | EmptyErrorCodeAnalyzer
FCE006  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByTargetNotFoundAnalyzer
FCE007  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByInvalidSignatureAnalyzer
FCE008  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByWithoutProvidesErrorsForAnalyzer
FCE016  | FirstClassErrors.Usage               | Warning  | UnusedToExceptionResultAnalyzer
