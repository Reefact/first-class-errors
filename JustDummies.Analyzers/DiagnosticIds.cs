namespace JustDummies.Analyzers;

/// <summary>
///     Stable identifiers for every JustDummies diagnostic. <c>JD</c> is the JustDummies prefix, mirroring
///     <c>FCE</c> for FirstClassErrors; the number is only a stable handle.
/// </summary>
internal static class DiagnosticIds {

    // Category: Reproducibility
    public const string AsyncBodyPassedToReproducibly   = "JD001";
    public const string DiscardedReproduciblyAsyncResult = "JD002";

}
