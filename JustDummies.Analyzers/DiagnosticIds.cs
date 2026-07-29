namespace JustDummies.Analyzers;

/// <summary>
///     Stable identifiers for every JustDummies diagnostic. <c>JD</c> is the JustDummies prefix, mirroring
///     <c>FCE</c> for FirstClassErrors; the number is only a stable handle.
/// </summary>
internal static class DiagnosticIds {

    // Category: Reproducibility
    public const string AsyncBodyPassedToReproducibly     = "JD001";
    public const string DiscardedReproduciblyAsyncResult  = "JD002";
    public const string AwaitableBodyPassedToReproducibly = "JD003";
    public const string DiscardedSeedingResult            = "JD004";

    // Category: Usage
    public const string GeneratorRenderedAsText   = "JD005";
    public const string DiscardedGeneratorResult  = "JD006";

    // Category: Reproducibility — draws that escape the pinned seed scope
    public const string DrawOutsideThePinnedScope  = "JD007";
    public const string ArbitraryValueInTheoryData = "JD008";
    public const string DrawInStaticInitializer    = "JD009";
    public const string ReproducibleOnNonTestMethod = "JD010";

}
