; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category                    | Severity | Notes
--------|-----------------------------|----------|-------------------------------------------
JD001   | JustDummies.Reproducibility | Error    | AsyncBodyPassedToReproduciblyAnalyzer
JD002   | JustDummies.Reproducibility | Error    | DiscardedReproduciblyAsyncResultAnalyzer
JD003   | JustDummies.Reproducibility | Error    | AwaitableBodyPassedToReproduciblyAnalyzer
JD004   | JustDummies.Reproducibility | Error    | DiscardedSeedingResultAnalyzer
JD005   | JustDummies.Usage           | Error    | GeneratorRenderedAsTextAnalyzer
JD006   | JustDummies.Usage           | Warning  | DiscardedGeneratorResultAnalyzer
