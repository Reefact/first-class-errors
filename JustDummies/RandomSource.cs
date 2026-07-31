namespace JustDummies;

/// <summary>
///     The random context a generator draws from when it generates: a pseudo-random generator paired with the seed
///     that created it, so any failure can name the seed that replays the run. Generators hold a
///     <see cref="RandomSource" /> and resolve it at <see cref="IAny{T}.Generate" /> time — never at construction
///     time — which is what lets a recipe built outside an <c>Any.Reproducibly(...)</c> scope generate
///     deterministically inside one.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1694:An abstract class should have both abstract and concrete methods",
                                                 Justification =
                                                     "The abstract class is the root of a closed, internal hierarchy and is deliberately not an interface: a class cannot be " +
                                                     "implemented from outside the assembly, keeps the option of adding shared state without breaking every subtype, and on the " +
                                                     "netstandard2.0 leg cannot be replaced by an interface with non-public members.")]
internal abstract class RandomSource {

    /// <summary>The seeded generator to draw from right now. Every draw goes through it, serialized on its own lock.</summary>
    internal abstract SeededRandom Current { get; }

    /// <summary>
    ///     The reproduction guidance to append to a generation-failure message, phrased for this kind of source: one
    ///     <b>sentence</b>, which <b>embeds</b> a replay <b>snippet</b> — the code the reader copies. The two words are
    ///     a whole and its part, and they are never interchangeable: guidance is the sentence, a snippet is the
    ///     fragment it names.
    /// </summary>
    /// <remarks>
    ///     Which snippet the sentence names depends on how the run was pinned, and getting that wrong is the whole
    ///     point of this method existing. The ambient source names <c>Any.Reproducibly(seed, ...)</c> — or whatever
    ///     snippet the opener of the current <see cref="Any.UseSeed(int, string)" /> scope supplied, since a run pinned
    ///     by a test-framework adapter is replayed by changing what the adapter reads, not by adding a call the test
    ///     never had. A fixed <c>Any.WithSeed(...)</c> context replays deterministically on its own, so pinning the
    ///     ambient source would not apply. Naming a snippet the reader's code does not contain is exactly the
    ///     misleading diagnostic this method exists to avoid.
    /// </remarks>
    internal abstract string ReplayGuidance(int seed);

    /// <summary>
    ///     The reproduction guidance for a failure whose seeded draws this source drove but whose result also depends on
    ///     a generator that does not draw from this source — a foreign <see cref="IAny{T}" />, or a derivation built over
    ///     one (including a <c>Combine</c> that mixes a foreign operand with a sourced one). It names the same replay
    ///     mechanism as <see cref="ReplayGuidance" /> for the seeded part, but scopes the promise to it: the foreign values
    ///     are not reproducible from this seed alone, so claiming a full replay would be the misleading diagnostic the
    ///     seed reporting exists to avoid.
    /// </summary>
    internal abstract string PartialReplayGuidance(int seed);

}

/// <summary>
///     A pseudo-random generator that remembers the seed it was created from, and the <b>only</b> door to it: the
///     underlying <see cref="Random" /> is never handed out, so every draw goes through this type and is serialized
///     on its own lock.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Random" /> is not thread-safe, and a source reaches several threads by two ordinary routes: the
///         ambient state flows with the execution context into every task a test spawns, and an
///         <see cref="AnyContext" /> is shared by whoever holds it. Left unsynchronized, concurrent draws converge the
///         generator's two internal indices and it returns zero <b>for ever</b> — so every generator settles on the
///         minimum of its declared range (<c>0</c>, <c>""</c>, <see cref="Guid.Empty" />) and the source never
///         recovers. Silent, and exactly the values most likely to make an assertion pass for the wrong reason.
///     </para>
///     <para>
///         Keeping the <see cref="Random" /> private is what makes the guarantee hold: a synchronized subclass would
///         leak any member left un-overridden, whereas here a draw that bypasses the lock does not compile. An
///         uncontended lock leaves single-threaded sequences bit-identical, so a pinned seed replays exactly as
///         before, and the cost is immaterial on paths that are not hot loops.
///     </para>
///     <para>
///         What this does <b>not</b> buy is a value-level guarantee across threads: the lock is per primitive draw, so
///         two threads interleave inside a multi-draw generation (a string consumes one draw per character). Neither
///         the sequence nor the multiset of generated values is stable under parallelism — see
///         <see cref="Any.UseSeed(int)" /> for the per-work-item scope that is.
///     </para>
/// </remarks>
internal sealed class SeededRandom {

    #region Fields declarations

    private readonly object _gate = new();
    private readonly Random _random;

    #endregion

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Vulnerability", "S2245:Pseudorandom number generators (PRNGs) should not be used in security contexts",
                                                     Justification =
                                                         "S2245 is right that this generator is predictable; that predictability is the type's contract. A dummy is worth " +
                                                         "generating only if the seed a failing run reports replays it, and a seeded System.Random is the one BCL generator " +
                                                         "whose sequence a recorded seed reproduces. RandomNumberGenerator is seedless by design, so adopting it would delete " +
                                                         "Any.Reproducibly, Any.WithSeed and Any.UseSeed outright, along with the seed every generation failure reports. It " +
                                                         "would also break a checked contract: justdummies.yml compares the SEEDBATCH banner that tools/justdummies-check draws " +
                                                         "from CrossTfmSeed byte-for-byte between the lib/netstandard2.0 and lib/net8.0 assets. No draw in this solution is " +
                                                         "security material: SeededRandom is internal and reachable only through the Any.* test-data generators, and " +
                                                         "README.nuget.md tells consumers never to draw a secret, key, token or nonce from Any.*.")]
    internal SeededRandom(int seed) {
        Seed    = seed;
        _random = new Random(seed);
    }

    internal int Seed { get; }

    /// <summary>Draws a non-negative <see cref="int" /> below <paramref name="maxExclusive" />.</summary>
    internal int Next(int maxExclusive) {
        lock (_gate) { return _random.Next(maxExclusive); }
    }

    /// <summary>Draws an <see cref="int" /> in the half-open range [<paramref name="minInclusive" />, <paramref name="maxExclusive" />).</summary>
    internal int Next(int minInclusive, int maxExclusive) {
        lock (_gate) { return _random.Next(minInclusive, maxExclusive); }
    }

    /// <summary>Fills <paramref name="buffer" /> with random bytes.</summary>
    internal void NextBytes(byte[] buffer) {
        if (buffer is null) { throw new ArgumentNullException(nameof(buffer)); }

        lock (_gate) { _random.NextBytes(buffer); }
    }

    /// <summary>Draws a <see cref="double" /> in the half-open range [0, 1).</summary>
    internal double NextDouble() {
        lock (_gate) { return _random.NextDouble(); }
    }

}

/// <summary>
///     The default random context behind the static <see cref="Any" /> entry points. The state is stored in an
///     <see cref="AsyncLocal{T}" />, so it flows with the current execution context and never leaks across tests
///     running in parallel. Outside an <see cref="UseSeed(int)" /> scope it lazily seeds itself with a fresh seed — every
///     run differs, which surfaces a test that secretly depends on a value — and that seed is remembered, so a
///     generation failure can still report it. Inside a scope (how <c>Any.Reproducibly(...)</c> pins a run) it is
///     deterministic.
/// </summary>
internal sealed class AmbientRandomSource : RandomSource {

    #region Statics members declarations

    internal static readonly AmbientRandomSource Instance = new();

    private static readonly AsyncLocal<AmbientState?> State = new();

    internal static int NewSeed() {
        return Guid.NewGuid().GetHashCode();
    }

    internal static IDisposable UseSeed(int seed) {
        return UseSeed(seed, null);
    }

    internal static IDisposable UseSeed(int seed, string? replaySnippet) {
        AmbientState frame = new(new SeededRandom(seed), replaySnippet, State.Value);
        State.Value = frame;

        return new SeedScope(frame);
    }

    #endregion

    private AmbientRandomSource() { }

    internal override SeededRandom Current {
        get {
            AmbientState? current = State.Value;
            if (current is null) {
                current     = new AmbientState(new SeededRandom(NewSeed()), null, null);
                State.Value = current;
            }

            return current.Random;
        }
    }

    internal override string ReplayGuidance(int seed) {
        return $"The arbitrary values were seeded with {seed}; reproduce this run with {ReplaySnippet(seed)}.";
    }

    internal override string PartialReplayGuidance(int seed) {
        return $"The seeded draws were made with {seed} ({ReplaySnippet(seed)}), but some values come from a generator that does not draw from this source, so they are not reproducible from this seed alone.";
    }

    /// <summary>
    ///     The code the reader copies to replay the current run — the fragment the guidance sentence embeds, never the
    ///     sentence itself: the snippet the opener of the scope supplied, or the delegate runner when none was. Read
    ///     from the scope rather than fixed on the source, because the ambient source is pinned by several mechanisms
    ///     and each is replayed differently.
    /// </summary>
    private static string ReplaySnippet(int seed) {
        return State.Value?.Snippet ?? $"Any.Reproducibly({seed}, ...)";
    }

    #region Nested types

    /// <summary>
    ///     One frame of the ambient seed stack a scope installs: the seeded generator, how to replay the run that uses
    ///     it, and the frame it was pushed on top of. The frames form a linked stack (each points at its
    ///     <see cref="Parent" />) so a scope disposed out of order can be removed without stranding the ones still open
    ///     — see <see cref="SeedScope" />. <see cref="Disposed" /> tombstones a frame whose scope has closed but which is
    ///     not yet the top of the stack, so the top's later disposal can skip past it.
    /// </summary>
    private sealed class AmbientState {

        internal AmbientState(SeededRandom random, string? replaySnippet, AmbientState? parent) {
            if (random is null) { throw new ArgumentNullException(nameof(random)); }

            Random  = random;
            Snippet = replaySnippet;
            Parent  = parent;
        }

        internal SeededRandom  Random   { get; }

        /// <summary>
        ///     The replay snippet the opener of this scope supplied, if any — the fragment, never the whole guidance
        ///     sentence. Named <c>Snippet</c> rather than <c>ReplaySnippet</c> so it does not shadow the enclosing
        ///     <see cref="AmbientRandomSource.ReplaySnippet(int)" />, which reads it.
        /// </summary>
        internal string?       Snippet  { get; }
        internal AmbientState? Parent   { get; }
        internal bool          Disposed { get; set; }

    }

    /// <summary>
    ///     The handle returned by <see cref="UseSeed(int, string?)" />. Disposal is <b>order-independent</b>: it
    ///     tombstones its own frame, and only the frame that is currently the top of the stack rewrites the ambient
    ///     slot — walking past any tombstoned ancestors to the nearest frame whose scope is still open (or to
    ///     <c>null</c> when none is). So the documented "scopes nest, disposing restores whatever was pinned before"
    ///     holds even when scopes are disposed out of order: an outer scope closed early strands nothing, and no order
    ///     leaves a dead seed pinned for whatever runs next. Disposing twice is a no-op.
    /// </summary>
    private sealed class SeedScope : IDisposable {

        private readonly AmbientState _frame;
        private          bool         _disposed;

        internal SeedScope(AmbientState frame) {
            if (frame is null) { throw new ArgumentNullException(nameof(frame)); }

            _frame = frame;
        }

        public void Dispose() {
            if (_disposed) { return; }

            _disposed       = true;
            _frame.Disposed = true;

            // Only the current top owns the ambient slot; an out-of-order dispose of an inner frame just tombstones
            // it and lets the top's own dispose skip it later.
            if (ReferenceEquals(State.Value, _frame)) {
                AmbientState? restored = _frame.Parent;
                while (restored is { Disposed: true }) { restored = restored.Parent; }
                State.Value = restored;
            }
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

    internal override string ReplayGuidance(int seed) {
        return $"The arbitrary values were drawn from Any.WithSeed({seed}), which already replays deterministically.";
    }

    internal override string PartialReplayGuidance(int seed) {
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out",
                                                     Justification =
                                                         "The flagged lines are prose, not disabled code. The heuristic reads an equation, a bracketed range or a semicolon inside an " +
                                                         "explanatory sentence as a statement. These comments carry the WHY this codebase asks for and deleting them would lose the " +
                                                         "reasoning, so the finding is recorded here instead.")]
    internal static long NextInt64Inclusive(this SeededRandom random, long minInclusive, long maxInclusive) {
        if (random is null) { throw new ArgumentNullException(nameof(random)); }
        if (minInclusive > maxInclusive) { throw new ArgumentOutOfRangeException(nameof(maxInclusive), "The maximum must be greater than or equal to the minimum."); }

        ulong rangeSize = (ulong)(maxInclusive - minInclusive) + 1UL;
        ulong draw      = random.NextUInt64();

        // rangeSize is 0 only when the range spans the full ulong width, which int-derived bounds never do;
        // guard anyway so the helper stays correct if reused with wider bounds.
        if (rangeSize == 0UL) { return unchecked((long)draw); }

        return minInclusive + (long)(draw % rangeSize);
    }

    /// <summary>Draws a uniform <see cref="int" /> in the inclusive range — see <see cref="NextInt64Inclusive" />.</summary>
    internal static int NextInt32Inclusive(this SeededRandom random, int minInclusive, int maxInclusive) {
        if (random is null) { throw new ArgumentNullException(nameof(random)); }

        return (int)random.NextInt64Inclusive(minInclusive, maxInclusive);
    }

    /// <summary>Draws 8 random bytes as a <see cref="ulong" /> — the raw material of the ordinal sampling.</summary>
    internal static ulong NextUInt64(this SeededRandom random) {
        if (random is null) { throw new ArgumentNullException(nameof(random)); }

        byte[] bytes = new byte[sizeof(ulong)];
        random.NextBytes(bytes);

        return BitConverter.ToUInt64(bytes, 0);
    }


}
