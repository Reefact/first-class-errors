#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for the composition seams — <see cref="AnyExtensions.As{TSource,TResult}" />,
///     <c>OrNull()</c>, <see cref="Any.Combine{T1,T2,TResult}" />, <c>PairOf</c>/<c>TripleOf</c> — and for the explicit
///     pools (<c>OneOf</c>, <c>ElementOf</c>). Where the example-based suite pins one hand-picked constraint per part
///     (<c>Between(0, 100)</c>, <c>WithLength(12)</c>) and, at the higher arities, passes the <b>same</b> generator to
///     every slot, these quantify over the constraint of each part independently — so a part routed to the wrong slot,
///     a constraint dropped on the way through a seam, or a pool value invented out of nothing is found and shrunk to
///     its minimal counter-example.
/// </summary>
[TestSubject(typeof(AnyExtensions))]
public sealed class CompositionProperties {

    #region Statics members declarations

    /// <summary>Arbitrary non-empty pools of integers — the explicit domains <c>OneOf</c> and <c>ElementOf</c> draw from.</summary>
    private static Gen<int[]> IntegerPools() {
        return Gen.NonEmptyListOf(Generators.Int32()).Select(values => values.Take(24).ToArray());
    }

    /// <summary>
    ///     Arbitrary non-empty pools of non-null strings. The values are built from a drawn number rather than taken
    ///     from FsCheck's own string generator, which yields <c>null</c> — an element the library rejects by design, and
    ///     a case the dedicated null-element property covers on purpose rather than by accident.
    /// </summary>
    private static Gen<string[]> StringPools() {
        return Gen.NonEmptyListOf(Gen.Choose(0, 20).Select(value => "v" + value)).Select(values => values.Take(24).ToArray());
    }

    /// <summary>
    ///     A copy of <paramref name="pool" /> carrying a <c>null</c> at <paramref name="index" /> (clamped to the pool's
    ///     length), so the null-element rejection is exercised at every position rather than only at the end.
    /// </summary>
    private static string[] Poisoned(string[] pool, int index) {
        List<string> poisoned = new(pool);
        poisoned.Insert(Math.Min(index, poisoned.Count), null!);

        return poisoned.ToArray();
    }

    /// <summary>A generator pinned to a single value — one distinct part per slot, so a mis-routed slot changes the result.</summary>
    private static IAny<int> Pinned(int value) {
        return Any.Int32().Between(value, value);
    }

    #endregion

    [Fact(DisplayName = "As projects every draw: the composed value is the factory's image of a value the source constraint allows.")]
    public void AsProjectsEveryDrawThroughTheFactory() {
        // Doubling is invertible, so the projected value can be mapped back and checked against the source interval —
        // which is what "the image of a value satisfying the source constraint" means, stated without naming the draw.
        Prop.ForAll(Generators.OrderedPair(Generators.Int32()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.Int32().Between(bounds.Min, bounds.Max).As(value => (long)value * 2),
                                               projected => projected % 2 == 0
                                                            && projected / 2 >= bounds.Min
                                                            && projected / 2 <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "As bridges to a value object: constraints inside the factory's invariant always pass, constraints entirely outside it always fail.")]
    public void AsBridgesToAValueObjectFactory() {
        Prop.ForAll((from inside in Generators.OrderedPair(Gen.Choose(0, 100))
                     from outside in Generators.OrderedPair(Gen.Choose(101, 100_000))
                     select (inside, outside)).ToArbitrary(),
                    testCase => {
                        bool accepted = Expect.EveryDraw(Any.Int32().Between(testCase.inside.Min, testCase.inside.Max).As(Ratio.Create),
                                                         ratio => ratio.Value >= testCase.inside.Min && ratio.Value <= testCase.inside.Max);

                        // Constraints weaker than the invariant the factory enforces are the documented cause of a
                        // generation failure. Entirely outside the window every draw is rejected, so the wrap is
                        // certain rather than probable — no interval in the quantified space can slip through.
                        bool rejected = Expect.Throws<AnyGenerationException>(
                            () => Any.Int32().Between(testCase.outside.Min, testCase.outside.Max).As(Ratio.Create).Generate());

                        return accepted && rejected;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A factory that throws surfaces as AnyGenerationException carrying the original failure and the seed that replays it.")]
    public void AsWrapsFactoryFailuresPreservingTheCause() {
        Prop.ForAll((from bounds in Generators.OrderedPair(Generators.Int32())
                     from seed in Generators.Seed()
                     select (bounds, seed)).ToArbitrary(),
                    testCase => {
                        IAny<int> generator = Any.WithSeed(testCase.seed)
                                                 .Int32()
                                                 .Between(testCase.bounds.Min, testCase.bounds.Max)
                                                 .As<int, int>(_ => throw new FactoryRejection());

                        try {
                            generator.Generate();

                            return false;
                        } catch (AnyGenerationException exception) {
                            return exception.InnerException is FactoryRejection && exception.Seed == testCase.seed;
                        }
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "OrNull on a value type: every non-null draw satisfies the wrapped generator's constraint.")]
    public void ValueTypeOrNullKeepsTheWrappedConstraint() {
        Prop.ForAll(Generators.OrderedPair(Generators.Int32()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.Int32().Between(bounds.Min, bounds.Max).OrNull(),
                                               value => value is null || (value.Value >= bounds.Min && value.Value <= bounds.Max)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "OrNull on a reference type: every non-null draw satisfies the wrapped generator's constraint.")]
    public void ReferenceTypeOrNullKeepsTheWrappedConstraint() {
        Prop.ForAll(Gen.Choose(1, 12).ToArbitrary(),
                    length => Expect.EveryDraw(Any.String().WithLength(length).OrNull(),
                                               value => value is null || value.Length == length))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "OrNull is a coin flip: over enough draws from any seed, both the null and the value branch appear.")]
    public void OrNullEventuallyYieldsBothBranches() {
        // The null decision is an even coin flip, so 64 draws miss a branch with probability about 2^-63 — vanishing,
        // but a probability nonetheless. Drawing from an Any.WithSeed(...) context removes the residual flakiness: each
        // FsCheck case is a fixed, replayable run, so a case that passes passes identically on every execution.
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        AnyContext context = Any.WithSeed(seed);

                        List<int?>    values     = Expect.Draws(context.Int32().Between(1, 100).OrNull(), 64);
                        List<string?> references = Expect.Draws(context.String().WithLength(4).OrNull(), 64);

                        return values.Any(value => value is null)
                               && values.Any(value => value is not null)
                               && values.All(value => value is null || (value.Value >= 1 && value.Value <= 100))
                               && references.Any(reference => reference is null)
                               && references.Any(reference => reference is not null)
                               && references.All(reference => reference is null || reference.Length == 4);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A null draw from OrNull does not consume a value from the wrapped generator.")]
    public void OrNullDoesNotConsumeTheWrappedGeneratorOnANullDraw() {
        // Counting the wrapped generator's draws is the only way to observe this: the wrapped values themselves cannot
        // distinguish "not drawn" from "drawn and discarded".
        Prop.ForAll(Gen.Choose(1, 40).ToArbitrary(),
                    drawCount => {
                        CountingAny<int> wrapped = new(7);

                        List<int?> values = Expect.Draws(wrapped.OrNull(), drawCount);

                        return wrapped.Draws == values.Count(value => value is not null)
                               && values.All(value => value is null || value.Value == 7);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Combine of two parts: the composed value carries each part's own constraint.")]
    public void CombineOfTwoPartsCarriesBothConstraints() {
        Gen<(int Min, int Max)> intervals = Generators.OrderedPair(Generators.Int32());

        Prop.ForAll((from first in intervals
                     from second in intervals
                     select (first, second)).ToArbitrary(),
                    testCase => Expect.EveryDraw(
                        Any.Combine(Any.Int32().Between(testCase.first.Min, testCase.first.Max),
                                    Any.Int32().Between(testCase.second.Min, testCase.second.Max),
                                    (one, two) => (Head: one, Tail: two)),
                        composed => composed.Head >= testCase.first.Min
                                    && composed.Head <= testCase.first.Max
                                    && composed.Tail >= testCase.second.Min
                                    && composed.Tail <= testCase.second.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Combine of three parts routes each constraint to its own slot, across types.")]
    public void CombineOfThreePartsCarriesEveryConstraint() {
        Gen<(int Min, int Max)> intervals = Generators.OrderedPair(Generators.Int32());

        Prop.ForAll((from first in intervals
                     from length in Gen.Choose(1, 12)
                     from third in intervals
                     select (first, length, third)).ToArbitrary(),
                    testCase => Expect.EveryDraw(
                        Any.Combine(Any.Int32().Between(testCase.first.Min, testCase.first.Max),
                                    Any.String().WithLength(testCase.length),
                                    Any.Int32().Between(testCase.third.Min, testCase.third.Max),
                                    (one, two, three) => (Head: one, Text: two, Tail: three)),
                        composed => composed.Head >= testCase.first.Min
                                    && composed.Head <= testCase.first.Max
                                    && composed.Text.Length == testCase.length
                                    && composed.Tail >= testCase.third.Min
                                    && composed.Tail <= testCase.third.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Combine at its arity ceiling passes every part to its own slot, in order.")]
    public void CombineAtArityEightRoutesEveryPart() {
        Gen<int> pins = Generators.Int32();

        Prop.ForAll((from one in pins
                     from two in pins
                     from three in pins
                     from four in pins
                     from five in pins
                     from six in pins
                     from seven in pins
                     from eight in pins
                     select new[] { one, two, three, four, five, six, seven, eight }).ToArbitrary(),
                    expected => {
                        // Each of the eight parts is pinned to a value of its own, so two slots swapped change the
                        // composed array. The example-based suite passes the SAME generator to all eight slots and
                        // therefore cannot see such a mix-up at all — only the arity itself.
                        IAny<int[]> generator = Any.Combine(
                            Pinned(expected[0]), Pinned(expected[1]), Pinned(expected[2]), Pinned(expected[3]),
                            Pinned(expected[4]), Pinned(expected[5]), Pinned(expected[6]), Pinned(expected[7]),
                            (one, two, three, four, five, six, seven, eight) => new[] { one, two, three, four, five, six, seven, eight });

                        return Expect.EveryDraw(generator, parts => parts.SequenceEqual(expected), 4);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "PairOf and TripleOf keep every component within its own constraint.")]
    public void PairOfAndTripleOfKeepEveryComponentConstraint() {
        Gen<(int Min, int Max)> intervals = Generators.OrderedPair(Generators.Int32());

        Prop.ForAll((from first in intervals
                     from length in Gen.Choose(1, 12)
                     from third in intervals
                     select (first, length, third)).ToArbitrary(),
                    testCase => {
                        AnyInt32  head = Any.Int32().Between(testCase.first.Min, testCase.first.Max);
                        AnyString text = Any.String().WithLength(testCase.length);
                        AnyInt32  tail = Any.Int32().Between(testCase.third.Min, testCase.third.Max);

                        bool pairs = Expect.EveryDraw(Any.PairOf(head, text),
                                                      pair => pair.Item1 >= testCase.first.Min
                                                              && pair.Item1 <= testCase.first.Max
                                                              && pair.Item2.Length == testCase.length);

                        bool triples = Expect.EveryDraw(Any.TripleOf(head, text, tail),
                                                        triple => triple.Item1 >= testCase.first.Min
                                                                  && triple.Item1 <= testCase.first.Max
                                                                  && triple.Item2.Length == testCase.length
                                                                  && triple.Item3 >= testCase.third.Min
                                                                  && triple.Item3 <= testCase.third.Max);

                        return pairs && triples;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "OneOf and ElementOf draw only from the pool they were given, whatever the pool and whichever overload.")]
    public void PoolGeneratorsStayWithinTheirPool() {
        Prop.ForAll(IntegerPools().ToArbitrary(),
                    pool => Expect.EveryDraw(Any.OneOf(pool), value => pool.Contains(value))
                            && Expect.EveryDraw(Any.ElementOf((IReadOnlyList<int>)pool), value => pool.Contains(value))
                            && Expect.EveryDraw(Any.ElementOf(pool.Select(value => value)), value => pool.Contains(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "OneOf reaches exactly the distinct values of its pool: duplicates collapse, nothing is dropped and nothing is invented.")]
    public void OneOfReachesExactlyTheDistinctValuesOfItsPool() {
        // At most four distinct values over 96 draws: one of them is missed with probability at most 4 x (3/4)^96,
        // about 1e-11. A seeded context turns that residual chance into a fixed, replayable run per FsCheck case.
        // The pool is drawn from a four-value alphabet on purpose, so duplicates are the common case, not the corner.
        Gen<int[]> pools = Gen.NonEmptyListOf(Gen.Choose(0, 3)).Select(values => values.Take(6).ToArray());

        Prop.ForAll((from pool in pools
                     from seed in Generators.Seed()
                     select (pool, seed)).ToArbitrary(),
                    testCase => {
                        HashSet<int> distinct = new(testCase.pool);
                        HashSet<int> drawn    = new(Expect.Draws(Any.WithSeed(testCase.seed).OneOf(testCase.pool), 96));

                        return drawn.SetEquals(distinct);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Any.String().OneOf draws only from its value set, whatever the set and whichever overload.")]
    public void StringOneOfStaysWithinItsValueSet() {
        Prop.ForAll(StringPools().ToArbitrary(),
                    pool => Expect.EveryDraw(Any.String().OneOf(pool), value => pool.Contains(value))
                            && Expect.EveryDraw(Any.String().OneOf(pool.Select(value => value)), value => pool.Contains(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Excluding part of a pool leaves exactly the complement; excluding all of it conflicts.")]
    public void ExcludingAPoolLeavesItsComplement() {
        Gen<(string[] Pool, string[] Excluded)> cases =
            from pool in StringPools()
            from taken in Gen.Choose(0, 24)
            select (Pool: pool, Excluded: pool.Distinct().Take(taken).ToArray());

        Prop.ForAll(cases.ToArbitrary(),
                    // The verdict follows the values: the generator survives exactly when some pooled value escapes
                    // the exclusion, and it then draws from the complement and nothing else. An exclusion carrying
                    // no value at all is an argument error, not a domain question, so it is left to the example suite.
                    testCase => {
                        if (testCase.Excluded.Length == 0) { return true; }

                        string[] surviving = testCase.Pool.Distinct().Except(testCase.Excluded).ToArray();

                        return surviving.Length == 0
                                   ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.OneOf(testCase.Pool).Except(testCase.Excluded))
                                   : Expect.EveryDraw(Any.OneOf(testCase.Pool).Except(testCase.Excluded), value => surviving.Contains(value));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "ElementOf materializes its sequence once, however many values are drawn from it.")]
    public void ElementOfMaterializesItsSequenceOnce() {
        Prop.ForAll((from pool in IntegerPools()
                     from drawCount in Gen.Choose(1, 40)
                     select (pool, drawCount)).ToArbitrary(),
                    testCase => {
                        int enumerations = 0;

                        IEnumerable<int> LazyPool() {
                            enumerations++;
                            foreach (int value in testCase.pool) {
                                yield return value;
                            }
                        }

                        AnyOneOf<int> generator = Any.ElementOf(LazyPool());
                        List<int>     drawn     = Expect.Draws(generator, testCase.drawCount);

                        // One enumeration at construction, none per draw: a lazy query re-run per draw would both cost
                        // and, for a non-deterministic source, silently change the pool between two values.
                        return enumerations == 1 && drawn.All(value => testCase.pool.Contains(value));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A null anywhere in the pool is an argument error, at every position and on every pool entry point.")]
    public void ANullPoolElementIsAnArgumentError() {
        Prop.ForAll((from pool in StringPools()
                     from index in Gen.Choose(0, 24)
                     select (pool, index)).ToArbitrary(),
                    testCase => {
                        string[] poisoned = Poisoned(testCase.pool, testCase.index);

                        return Expect.Throws<ArgumentException>(() => Any.OneOf(poisoned))
                               && Expect.Throws<ArgumentException>(() => Any.ElementOf((IReadOnlyList<string>)poisoned))
                               && Expect.Throws<ArgumentException>(() => Any.ElementOf(poisoned.Select(value => value)))
                               && Expect.Throws<ArgumentException>(() => Any.String().OneOf(poisoned));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "An absent pool is a null-argument error and an empty one an argument error, on the ambient and seeded entry points alike.")]
    public void AbsentAndEmptyPoolsAreArgumentErrors() {
        // There is nothing to quantify inside the pool — it is absent or empty by definition — so the quantification
        // runs over the context instead: Any and Any.WithSeed(...) mirror the same surface and must reject identically.
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        AnyContext context = Any.WithSeed(seed);

                        return Expect.Throws<ArgumentNullException>(() => Any.OneOf((string[])null!))
                               && Expect.Throws<ArgumentNullException>(() => Any.ElementOf((IReadOnlyList<string>)null!))
                               && Expect.Throws<ArgumentNullException>(() => Any.ElementOf((IEnumerable<string>)null!))
                               && Expect.Throws<ArgumentNullException>(() => Any.String().OneOf((string[])null!))
                               && Expect.Throws<ArgumentNullException>(() => context.OneOf((string[])null!))
                               && Expect.Throws<ArgumentException>(() => Any.OneOf<string>())
                               && Expect.Throws<ArgumentException>(() => Any.ElementOf(new List<string>()))
                               && Expect.Throws<ArgumentException>(() => Any.ElementOf(Enumerable.Empty<string>()))
                               && Expect.Throws<ArgumentException>(() => Any.String().OneOf())
                               && Expect.Throws<ArgumentException>(() => context.ElementOf(new List<string>()));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "The composition seams reject a null generator or a null lambda, whatever the source constraint.")]
    public void CompositionSeamsRejectNullArguments() {
        Prop.ForAll(Generators.OrderedPair(Generators.Int32()).ToArbitrary(),
                    bounds => {
                        AnyInt32 part = Any.Int32().Between(bounds.Min, bounds.Max);

                        return Expect.Throws<ArgumentNullException>(() => part.As<int, int>(null!))
                               && Expect.Throws<ArgumentNullException>(() => AnyExtensions.As(null!, (int value) => value))
                               && Expect.Throws<ArgumentNullException>(() => ((IAny<int>)null!).OrNull())
                               && Expect.Throws<ArgumentNullException>(() => ((IAny<string>)null!).OrNull())
                               && Expect.Throws<ArgumentNullException>(() => Any.Combine(null!, part, (int one, int two) => one + two))
                               && Expect.Throws<ArgumentNullException>(() => Any.Combine(part, null!, (int one, int two) => one + two))
                               && Expect.Throws<ArgumentNullException>(() => Any.Combine(part, part, (Func<int, int, int>)null!))
                               && Expect.Throws<ArgumentNullException>(() => Any.PairOf(part, (IAny<int>)null!))
                               && Expect.Throws<ArgumentNullException>(() => Any.TripleOf(part, (IAny<int>)null!, part))
                               && Expect.Throws<ArgumentNullException>(
                                   () => Any.Combine(part, part, part, part, part, part, part, (IAny<int>)null!,
                                                     (int one, int two, int three, int four, int five, int six, int seven, int eight) => one));
                    })
            .QuickCheckThrowOnFailure();
    }

    #region Nested types

    /// <summary>
    ///     A minimal value object whose factory enforces an invariant — the shape <c>As</c> exists to bridge to, and the
    ///     one that tells a well-constrained source from a source weaker than the invariant.
    /// </summary>
    private sealed class Ratio {

        #region Statics members declarations

        internal static Ratio Create(int value) {
            if (value is < 0 or > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }

            return new Ratio(value);
        }

        #endregion

        private Ratio(int value) {
            Value = value;
        }

        internal int Value { get; }

    }

    /// <summary>The failure a factory raises, distinguishable from anything the library itself could throw.</summary>
    private sealed class FactoryRejection : Exception {

        internal FactoryRejection() : base("The factory rejected the generated value.") { }

    }

    /// <summary>
    ///     A generator counting how many times it is asked for a value. Foreign on purpose — it implements
    ///     <see cref="IAny{T}" /> only — which is exactly what makes the count observable from outside the library.
    /// </summary>
    /// <typeparam name="T">The type of the generated values.</typeparam>
    private sealed class CountingAny<T> : IAny<T> {

        #region Fields declarations

        private readonly T _value;

        #endregion

        internal CountingAny(T value) {
            _value = value;
        }

        internal int Draws { get; private set; }

        /// <inheritdoc />
        public T Generate() {
            Draws++;

            return _value;
        }

    }

    #endregion

}
