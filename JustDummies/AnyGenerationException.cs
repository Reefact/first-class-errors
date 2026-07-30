#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     Thrown when a generation cannot be completed even though every declared constraint was accepted — most
///     commonly when a factory passed to <see cref="AnyExtensions.As{TSource,TResult}" /> or a composer passed to
///     <see cref="Any.Combine{T1,T2,TResult}" /> rejects a generated value. Whenever the failing generator draws from
///     one of the library's random contexts, the message names the seed that replays the run and <see cref="Seed" />
///     carries it.
/// </summary>
/// <remarks>
///     The library prefers detecting contradictions <i>before</i> generation — those throw
///     <see cref="ConflictingAnyConstraintException" /> at declaration time. Reaching this exception therefore
///     usually means the constraints declared on the generator were weaker than the invariant the factory enforces;
///     the fix is to tighten the constraints so they express that invariant.
/// </remarks>
public sealed class AnyGenerationException : DummyException {

    #region Statics members declarations

    /// <summary>
    ///     The bounded walk around the drawn candidate found nothing the exclusions allow: every representable value
    ///     within <paramref name="budget" /> steps of it, in both directions, is excluded or out of bounds.
    /// </summary>
    internal static AnyGenerationException LocalSearchExhausted(string typeName, string replayGuidance, int? seed, int budget) {
        return NearTheCandidate(typeName, replayGuidance, seed,
                                $"Every representable value within {budget.ToString(CultureInfo.InvariantCulture)} steps of the drawn candidate, in both directions, is excluded or out of bounds. Values further away were not examined, so this is an exhausted local search rather than an empty range.");
    }

    /// <summary>
    ///     Snapping the drawn candidate onto the scale lattice could not leave an excluded point without leaving the
    ///     allowed range.
    /// </summary>
    internal static AnyGenerationException GridNudgeExhausted(string typeName, string replayGuidance, int? seed) {
        return NearTheCandidate(typeName, replayGuidance, seed, "The grid nudge could not leave the excluded point within the allowed range.");
    }

    /// <summary>
    ///     Nudging the drawn candidate away from an excluded point could not find a free value without leaving the
    ///     allowed range.
    /// </summary>
    internal static AnyGenerationException ExclusionNudgeExhausted(string typeName, string replayGuidance, int? seed) {
        return NearTheCandidate(typeName, replayGuidance, seed, "The exclusion nudge could not leave the excluded point within the allowed range.");
    }

    /// <summary>
    ///     Builds the exception for an enum with no member to draw from — nothing was constrained, the type simply
    ///     offers nothing.
    /// </summary>
    internal static AnyGenerationException EnumDeclaresNoMembers(string enumName) {
        return new AnyGenerationException($"Cannot generate an arbitrary {enumName} value because the enum declares no members.");
    }

    /// <summary>
    ///     Builds the exception for a caller-supplied factory or composer that threw, naming what was being generated
    ///     and how to replay the run.
    /// </summary>
    /// <remarks>
    ///     <paramref name="failure" /> stays a thunk all the way in here, and is called once, on this path only:
    ///     rendering the generated values would run the caller's <c>ToString()</c> and allocate the sentence on every
    ///     successful draw otherwise — which is every draw a test actually makes.
    /// </remarks>
    internal static AnyGenerationException FactoryFailed(Func<string> failure, Exception cause, RandomSource? source, bool reproducible) {
        int?   seed    = source?.Current.Seed;
        string message = $"Generation failed: {failure()} ({cause.GetType().Name}: {cause.Message}).";
        if (source is not null) {
            message += $" {(reproducible ? source.ReplayGuidance(seed!.Value) : source.PartialReplayGuidance(seed!.Value))}";
        }

        return new AnyGenerationException(message, seed, cause);
    }

    /// <summary>
    ///     Writes the sentence every near-the-candidate failure shares, and wraps <paramref name="diagnostic" /> as the
    ///     inner failure so the developer-facing detail travels with the exception rather than in its message.
    /// </summary>
    /// <remarks>
    ///     Private on purpose, like the factories above are internal on purpose: it names the grammar of the message,
    ///     not a failure, so every caller is a named case. And nothing here guards its arguments — building an
    ///     exception must never throw, or the failure being reported is replaced by a failure about reporting it
    ///     (ADR-0045, which exempts exception types for exactly that reason).
    /// </remarks>
    private static AnyGenerationException NearTheCandidate(string typeName, string replayGuidance, int? seed, string diagnostic) {
        return new AnyGenerationException($"Generation failed: no {typeName} value near the drawn candidate satisfies the exclusions. {replayGuidance}",
                                          seed,
                                          new InvalidOperationException(diagnostic));
    }

    #endregion

    /// <summary>
    ///     Initializes a new instance of the <see cref="AnyGenerationException" /> class.
    /// </summary>
    /// <param name="message">A description of the failed generation.</param>
    public AnyGenerationException(string message) : base(message) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AnyGenerationException" /> class wrapping an underlying failure.
    /// </summary>
    /// <param name="message">A description of the failed generation.</param>
    /// <param name="innerException">The underlying failure.</param>
    public AnyGenerationException(string message, Exception innerException) : base(message, innerException) { }

    internal AnyGenerationException(string message, int? seed, Exception innerException) : base(message, innerException) {
        Seed = seed;
    }

    internal AnyGenerationException(string message, int? seed) : base(message) {
        Seed = seed;
    }

    /// <summary>
    ///     The seed of the random context the failing generation drew from, when it is known. Under the ambient context
    ///     (<c>Any.Reproducibly(...)</c>) pass it to <c>Any.Reproducibly(seed, ...)</c> to replay the run; a value drawn
    ///     from an <c>Any.WithSeed(seed)</c> context already replays deterministically on its own. The failure message
    ///     states which of the two applies. <c>null</c> when the failing generator does not draw from one of the
    ///     library's random contexts.
    /// </summary>
    public int? Seed { get; }

}
