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
///         Built while a failure is being reported, so it guards nothing (ADR-0064): a guard on this path would throw
///         while a failure is being reported and lose the original. Its parameters are non-nullable instead, which
///         makes the contract the compiler's.
///     </para>
///     <para>
///         The seed is <b>supplied</b> rather than read back from the source, because the two are not always the same
///         thing: <see cref="RandomSource.Current" /> on the ambient source creates a state — and a fresh seed — when
///         none exists, so a caller that drew with a seed captured earlier must hand that seed over rather than let it
///         be resolved a second time.
///     </para>
/// </remarks>
[BuiltOnTheFailurePath]
internal sealed class Replay {

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

    private Replay(int seed, string guidance) {
        Seed     = seed;
        Guidance = guidance;
    }

    /// <summary>The seed that replays the run, carried on the exception so a caller can read it without parsing prose.</summary>
    internal int Seed { get; }

    /// <summary>The sentence naming the seed and scoping what it replays, appended to the failure message.</summary>
    internal string Guidance { get; }

}
