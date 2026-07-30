namespace JustDummies;

/// <summary>
///     What a failed draw needs in order to be replayed: the seed of the run, and the sentence telling the reader how
///     to use it. The two are always derived from the same source and always travel together, so they are one value
///     rather than two arguments that a call site could pair wrongly.
/// </summary>
/// <remarks>
///     A class rather than a struct, like every value object here: a struct carries a parameterless constructor that
///     would yield a seedless, guidance-less instance bypassing the factories below.
///     <para>
///         Two replays are equal when they replay the same run the same way — the seed alone does not settle it,
///         since the same seed replays a run in full or only in part depending on what drew. Being a value with no
///         identity beyond what it holds, it says so rather than leaving the reference comparison a reader would get
///         by default (ADR-0065).
///     </para>
///     <para>
///         Built while a failure is being reported, so it guards nothing (ADR-0064): a guard on this path would throw
///         while a failure is being reported and lose the original. Its parameters are non-nullable instead, which
///         makes the contract the compiler's. Comparing and hashing keep that footing: neither composes anything, so
///         neither can fail while a failure is reported.
///     </para>
///     <para>
///         The seed is <b>supplied</b> rather than read back from the source, because the two are not always the same
///         thing: <see cref="RandomSource.Current" /> on the ambient source creates a state — and a fresh seed — when
///         none exists, so a caller that drew with a seed captured earlier must hand that seed over rather than let it
///         be resolved a second time.
///     </para>
/// </remarks>
[BuiltOnTheFailurePath]
internal sealed class Replay : IEquatable<Replay> {

    #region Statics members declarations

    /// <summary>
    ///     The run replays in full: every draw the failing generator made followed <paramref name="source" />.
    /// </summary>
    internal static Replay Of(RandomSource source) {
        return Of(source, source.Current.Seed);
    }

    /// <summary>
    ///     The run replays in full, for a caller holding the seed it drew with — see the remark on the type about why
    ///     that seed is not read back from <paramref name="source" />.
    /// </summary>
    internal static Replay Of(RandomSource source, int seed) {
        return new Replay(seed, source.ReplayGuidance(seed));
    }

    /// <summary>
    ///     The run replays only in part: a foreign generator contributed values this source never drew, so promising a
    ///     full replay of them would be false. The seeded part still replays.
    /// </summary>
    internal static Replay PartialOf(RandomSource source) {
        int seed = source.Current.Seed;

        return new Replay(seed, source.PartialReplayGuidance(seed));
    }

    #endregion

    /// <summary>Determines whether two replays replay the same run the same way.</summary>
    /// <param name="left">The first replay to compare.</param>
    /// <param name="right">The second replay to compare.</param>
    /// <returns><c>true</c> when both carry the same seed and guidance, or both are <c>null</c>; otherwise <c>false</c>.</returns>
    public static bool operator ==(Replay? left, Replay? right) {
        return Equals(left, right);
    }

    /// <summary>Determines whether two replays differ in their seed or in what they promise to replay.</summary>
    /// <param name="left">The first replay to compare.</param>
    /// <param name="right">The second replay to compare.</param>
    /// <returns><c>true</c> when they differ, or exactly one is <c>null</c>; otherwise <c>false</c>.</returns>
    public static bool operator !=(Replay? left, Replay? right) {
        return !Equals(left, right);
    }

    private Replay(int seed, string guidance) {
        Seed     = seed;
        Guidance = guidance;
    }

    /// <summary>The seed that replays the run, carried on the exception so a caller can read it without parsing prose.</summary>
    internal int Seed { get; }

    /// <summary>The sentence naming the seed and scoping what it replays, appended to the failure message.</summary>
    internal string Guidance { get; }

    /// <inheritdoc />
    public bool Equals(Replay? other) {
        return other is not null && Seed == other.Seed && string.Equals(Guidance, other.Guidance, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return obj is Replay other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            return (Seed * 397) ^ StringComparer.Ordinal.GetHashCode(Guidance);
        }
    }

}
