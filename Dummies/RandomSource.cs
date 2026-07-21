namespace Dummies;

/// <summary>
///     The random context a generator draws from when it generates: a pseudo-random generator paired with the seed
///     that created it, so any failure can name the seed that replays the run. Generators hold a
///     <see cref="RandomSource" /> and resolve it at <see cref="IAny{T}.Generate" /> time — never at construction
///     time — which is what lets a recipe built outside an <c>Any.Reproducibly(...)</c> scope generate
///     deterministically inside one.
/// </summary>
internal abstract class RandomSource {

    /// <summary>The seeded pseudo-random generator to draw from right now.</summary>
    internal abstract SeededRandom Current { get; }

    /// <summary>
    ///     The reproduction guidance to append to a generation-failure message, phrased for this kind of source. The
    ///     ambient source points at <c>Any.Reproducibly(seed, ...)</c>; a fixed <c>Any.WithSeed(...)</c> context replays
    ///     deterministically on its own, so pinning the ambient source would not apply — naming the wrong instruction is
    ///     exactly the misleading diagnostic this method exists to avoid.
    /// </summary>
    internal abstract string ReplayHint(int seed);

    /// <summary>
    ///     The reproduction guidance for a failure whose seeded draws this source drove but whose result also depends on
    ///     a generator that does not draw from this source — a foreign <see cref="IAny{T}" />, or a derivation built over
    ///     one (including a <c>Combine</c> that mixes a foreign operand with a sourced one). It names the same replay
    ///     mechanism as <see cref="ReplayHint" /> for the seeded part, but scopes the promise to it: the foreign values
    ///     are not reproducible from this seed alone, so claiming a full replay would be the misleading diagnostic the
    ///     seed reporting exists to avoid.
    /// </summary>
    internal abstract string PartialReplayHint(int seed);

}

/// <summary>A pseudo-random generator that remembers the seed it was created from.</summary>
internal sealed class SeededRandom {

    internal SeededRandom(int seed) {
        Seed   = seed;
        Random = new Random(seed);
    }

    internal int    Seed   { get; }
    internal Random Random { get; }

}

/// <summary>
///     The default random context behind the static <see cref="Any" /> entry points. The state is stored in an
///     <see cref="AsyncLocal{T}" />, so it flows with the current execution context and never leaks across tests
///     running in parallel. Outside an <see cref="UseSeed" /> scope it lazily seeds itself with a fresh seed — every
///     run differs, which surfaces a test that secretly depends on a value — and that seed is remembered, so a
///     generation failure can still report it. Inside a scope (how <c>Any.Reproducibly(...)</c> pins a run) it is
///     deterministic.
/// </summary>
internal sealed class AmbientRandomSource : RandomSource {

    #region Statics members declarations

    internal static readonly AmbientRandomSource Instance = new();

    private static readonly AsyncLocal<SeededRandom?> State = new();

    internal static int NewSeed() {
        return Guid.NewGuid().GetHashCode();
    }

    internal static IDisposable UseSeed(int seed) {
        SeededRandom? previous = State.Value;
        State.Value = new SeededRandom(seed);

        return new SeedScope(previous);
    }

    #endregion

    private AmbientRandomSource() { }

    internal override SeededRandom Current {
        get {
            SeededRandom? current = State.Value;
            if (current is null) {
                current     = new SeededRandom(NewSeed());
                State.Value = current;
            }

            return current;
        }
    }

    internal override string ReplayHint(int seed) {
        return $"The arbitrary values were seeded with {seed}; reproduce this run with Any.Reproducibly({seed}, ...).";
    }

    internal override string PartialReplayHint(int seed) {
        return $"The seeded draws were made with {seed} (Any.Reproducibly({seed}, ...)), but some values come from a generator that does not draw from this source, so they are not reproducible from this seed alone.";
    }

    #region Nested types

    private sealed class SeedScope : IDisposable {

        private readonly SeededRandom? _previous;
        private          bool          _disposed;

        internal SeedScope(SeededRandom? previous) {
            _previous = previous;
        }

        public void Dispose() {
            if (_disposed) { return; }

            _disposed   = true;
            State.Value = _previous;
        }

    }

    #endregion

}

/// <summary>
///     The isolated random context behind <see cref="Any.WithSeed" />: one fixed, seeded generator owned by a single
///     <see cref="AnyContext" />. Unlike the ambient source it does not flow with the execution context — it is
///     deterministic by construction and belongs to whoever holds the context.
/// </summary>
internal sealed class FixedRandomSource : RandomSource {

    private readonly SeededRandom _random;

    internal FixedRandomSource(int seed) {
        _random = new SeededRandom(seed);
    }

    internal override SeededRandom Current => _random;

    internal override string ReplayHint(int seed) {
        return $"The arbitrary values were drawn from Any.WithSeed({seed}), which already replays deterministically.";
    }

    internal override string PartialReplayHint(int seed) {
        return $"The seeded draws were made from Any.WithSeed({seed}), but some values come from a generator that does not draw from it, so they are not reproducible from this seed alone.";
    }

}

/// <summary>
///     Implemented by the library's own generators so that derived generators (<c>As</c>, <c>Combine</c>) can
///     propagate the random context of their operands, and so that a generation failure can resolve the seed to
///     report. Foreign <see cref="IAny{T}" /> implementations simply do not carry one, and a derived generator
///     built over a foreign one carries <c>null</c>.
/// </summary>
internal interface IHasRandomSource {

    RandomSource? Source { get; }

}

/// <summary>
///     Implemented by derived generators (<c>As</c>, <c>Combine</c>) to report whether every operand they draw from is
///     itself reproducible. A single source-less (foreign) operand makes the derived value unreproducible even when
///     another operand supplies a non-null <see cref="IHasRandomSource.Source" /> for the replay hint to name — so a
///     full-replay promise must be withheld. Generators that draw only from their own source do not implement this and
///     are treated as reproducible whenever they carry a source.
/// </summary>
internal interface IReproducibilityHint {

    bool DrawsOnlyFromSource { get; }

}

/// <summary>Uniform sampling helpers shared by the generators.</summary>
internal static class RandomSampling {

    /// <summary>
    ///     Draws a uniform value in the inclusive range [<paramref name="minInclusive" />,
    ///     <paramref name="maxInclusive" />]. Unlike <see cref="Random.Next(int, int)" /> the upper bound is reachable,
    ///     which matters for full-range and boundary draws. The draw maps 8 random bytes onto the range size; the
    ///     modulo bias is at most 2^-32 for the ranges an <see cref="int" /> can express — irrelevant for arbitrary
    ///     test values. Deliberately NOT named NextInt64: on the net8.0 leg the framework's own
    ///     <c>Random.NextInt64(long, long)</c> instance method — whose upper bound is EXCLUSIVE — would win
    ///     overload resolution over a same-named extension and silently change the semantics.
    /// </summary>
    internal static long NextInt64Inclusive(this Random random, long minInclusive, long maxInclusive) {
        if (minInclusive > maxInclusive) { throw new ArgumentOutOfRangeException(nameof(maxInclusive), "The maximum must be greater than or equal to the minimum."); }

        ulong rangeSize = (ulong)(maxInclusive - minInclusive) + 1UL;
        ulong draw      = random.NextUInt64();

        // rangeSize is 0 only when the range spans the full ulong width, which int-derived bounds never do;
        // guard anyway so the helper stays correct if reused with wider bounds.
        if (rangeSize == 0UL) { return unchecked((long)draw); }

        return minInclusive + (long)(draw % rangeSize);
    }

    /// <summary>Draws a uniform <see cref="int" /> in the inclusive range — see <see cref="NextInt64Inclusive" />.</summary>
    internal static int NextInt32Inclusive(this Random random, int minInclusive, int maxInclusive) {
        return (int)random.NextInt64Inclusive(minInclusive, maxInclusive);
    }

    /// <summary>Draws 8 random bytes as a <see cref="ulong" /> — the raw material of the ordinal sampling.</summary>
    internal static ulong NextUInt64(this Random random) {
        byte[] bytes = new byte[8];
        random.NextBytes(bytes);

        return BitConverter.ToUInt64(bytes, 0);
    }


}
