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
    internal static AnyGenerationException LocalSearchExhausted(string typeName, Replay replay, int budget) {
        return NearTheCandidate(typeName, replay,
                                $"Every representable value within {budget.ToString(CultureInfo.InvariantCulture)} steps of the drawn candidate, in both directions, is excluded or out of bounds. Values further away were not examined, so this is an exhausted local search rather than an empty range.");
    }

    /// <summary>
    ///     Snapping the drawn candidate onto the scale lattice could not leave an excluded point without leaving the
    ///     allowed range.
    /// </summary>
    internal static AnyGenerationException GridNudgeExhausted(string typeName, Replay replay) {
        return NearTheCandidate(typeName, replay, "The grid nudge could not leave the excluded point within the allowed range.");
    }

    /// <summary>
    ///     Nudging the drawn candidate away from an excluded point could not find a free value without leaving the
    ///     allowed range.
    /// </summary>
    internal static AnyGenerationException ExclusionNudgeExhausted(string typeName, Replay replay) {
        return NearTheCandidate(typeName, replay, "The exclusion nudge could not leave the excluded point within the allowed range.");
    }

    /// <summary>
    ///     Builds the exception for an enum with no member to draw from — nothing was constrained, the type simply
    ///     offers nothing.
    /// </summary>
    internal static AnyGenerationException EnumDeclaresNoMembers(string enumName) {
        return new AnyGenerationException($"Cannot generate an arbitrary {enumName} value because the enum declares no members.");
    }

    /// <summary>
    ///     Builds the exception for a relative URI whose every component was declared away — no path segment, no query,
    ///     no fragment, no root — leaving the empty string, which is not a valid URI reference.
    /// </summary>
    internal static AnyGenerationException EmptyRelativeReference(Replay replay) {
        return new AnyGenerationException("A relative URI with exactly 0 path segments and no query, fragment or root is empty, which is not a valid URI reference. " +
                                          $"Add a query, a fragment, Rooted(), or a positive segment count. {replay.Guidance}",
                                          replay.Seed);
    }

    /// <summary>
    ///     Builds the exception for a pattern whose expansion outgrew the generation ceiling, which exists so no
    ///     pattern can grow the buffer without bound.
    /// </summary>
    internal static AnyGenerationException PatternExceedsGenerationLimit(int limit) {
        return new AnyGenerationException($"The pattern produced a string longer than the {limit}-character generation limit. This ceiling guards against runaway expansion; a pattern can reach it " +
                                          "either through a nested unbounded quantifier (such as \"(a+)+\") or through bounded quantifiers whose product is very large (such as \"(a{1000}){1000}\").");
    }

    /// <summary>
    ///     Builds the exception for a pattern every draw of which the .NET engine refused to match — the generator and
    ///     the engine disagree about the same pattern, which only a degenerate empty-match shape provokes.
    /// </summary>
    internal static AnyGenerationException PatternVerificationFailed(string attempts) {
        return new AnyGenerationException($"Generation failed: after {attempts} attempts, every value the pattern generator built was rejected by the .NET engine for the same pattern. " +
                                          "This happens only for a degenerate pattern whose empty-match behaviour the generator cannot mirror; rewrite it with the supported subset, or generate the value another way.");
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
        // A derivation over a foreign generator carries no source to name, and then there is nothing to replay.
        Replay? replay = null;
        if (source is not null) {
            replay = reproducible ? Replay.Of(source) : Replay.PartialOf(source);
        }

        string message = $"Generation failed: {failure()} ({cause.GetType().Name}: {cause.Message}).";
        if (replay is not null) {
            message += $" {replay.Guidance}";
        }

        return new AnyGenerationException(message, replay?.Seed, cause);
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
    private static AnyGenerationException NearTheCandidate(string typeName, Replay replay, string diagnostic) {
        return new AnyGenerationException($"Generation failed: no {typeName} value near the drawn candidate satisfies the exclusions. {replay.Guidance}",
                                          replay.Seed,
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
