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
JD007   | JustDummies.Reproducibility | Warning  | DrawOutsideThePinnedScopeAnalyzer
JD008   | JustDummies.Reproducibility | Warning  | ArbitraryValueInTheoryDataAnalyzer
JD009   | JustDummies.Reproducibility | Warning  | DrawInStaticInitializerAnalyzer
JD010   | JustDummies.Reproducibility | Warning  | ReproducibleOnNonTestMethodAnalyzer
JD011   | JustDummies.Usage           | Disabled | GeneratorWhereValueExpectedAnalyzer
JD012   | JustDummies.Usage           | Warning  | GeneratorPooledAsValueAnalyzer
JD013   | JustDummies.Usage           | Warning  | HeldCollectionPassedToOneOfAnalyzer
JD014   | JustDummies.Constraints     | Warning  | RejectedConstantArgumentAnalyzer
JD015   | JustDummies.Constraints     | Warning  | StringConstraintsAdmitNoValueAnalyzer
