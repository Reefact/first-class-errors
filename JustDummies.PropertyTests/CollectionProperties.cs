#region Usings declarations

using System.Runtime.CompilerServices;

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for the collection generators — <see cref="AnyList{T}" />, <see cref="AnyArray{T}" />,
///     <see cref="AnySequence{T}" />, <see cref="AnySet{T}" /> and <see cref="AnyDictionary{TKey,TValue}" />. The
///     example-based suite pins a handful of hand-picked sizes (<c>WithCount(5)</c>, <c>WithCountBetween(4, 6)</c>)
///     and can only prove the count algebra right for those; these quantify over the counts themselves — every size
///     from empty to thirty, every ordered bound pair, every pool size against every requested count — so a count
///     that is resolved one element short, or a distinctness gate that fires one element too early, is found and
///     shrunk to its minimal counter-example.
/// </summary>
/// <remarks>
///     The count and the element domain are quantified <b>together</b> wherever they interact, because that is where
///     the interesting behaviour lives: a distinct collection is satisfiable or contradictory depending on how the
///     requested count compares to the cardinality its element generator advertises, and the library promises to
///     decide that at declaration time rather than while drawing. A property that fixed either side would only ever
///     visit one side of that frontier.
/// </remarks>
[TestSubject(typeof(AnyList<int>))]
public sealed class CollectionProperties {

    #region Statics members declarations

    /// <summary>
    ///     Draws per generator for the properties asserting over several collection shapes at once. Lower than the
    ///     shared default, so covering five shapes in one property costs about what one shape costs elsewhere.
    /// </summary>
    private const int DrawsPerShape = 4;

    /// <summary>Negative counts, including the extremes an argument check that reasoned on magnitude would let through.</summary>
    private static Gen<int> NegativeCount() {
        return Generators.WithEdges(Gen.Choose(-30, -1), int.MinValue, int.MinValue + 1, -1);
    }

    /// <summary>The pool <c>1..size</c> — the same domain as <c>Any.Int32().Between(1, size)</c>, held as an explicit set of values.</summary>
    private static int[] Pool(int size) {
        return Enumerable.Range(1, size).ToArray();
    }

    /// <summary>
    ///     Requires each of <paramref name="values" /> in turn, so a property can quantify over <i>how many</i> values
    ///     a collection is required to contain rather than pinning that number in the test.
    /// </summary>
    private static AnyList<int> RequiringAll(AnyList<int> generator, int[] values) {
        AnyList<int> required = generator;
        foreach (int value in values) { required = required.Containing(value); }

        return required;
    }

    #endregion

    [Fact(DisplayName = "WithCount fixes the size exactly, for every count and every collection shape.")]
    public void WithCountFixesTheSize() {
        Prop.ForAll(Generators.Count(30).ToArbitrary(),
                    count => Expect.EveryDraw(Any.ListOf(Any.Int32()).WithCount(count), list => list.Count == count, DrawsPerShape)
                             && Expect.EveryDraw(Any.ArrayOf(Any.Int32()).WithCount(count), array => array.Length == count, DrawsPerShape)
                             && Expect.EveryDraw(Any.SequenceOf(Any.Int32()).WithCount(count), sequence => sequence.Count() == count, DrawsPerShape)
                             && Expect.EveryDraw(Any.SetOf(Any.Int32()).WithCount(count), set => set.Count == count, DrawsPerShape)
                             && Expect.EveryDraw(Any.DictionaryOf(Any.Int32(), Any.Int32()).WithCount(count), map => map.Count == count, DrawsPerShape))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithMinCount floors the size, for every minimum.")]
    public void WithMinCountFloorsTheSize() {
        Prop.ForAll(Generators.Count(30).ToArbitrary(),
                    minimum => Expect.EveryDraw(Any.ListOf(Any.Int32()).WithMinCount(minimum), list => list.Count >= minimum, DrawsPerShape)
                               && Expect.EveryDraw(Any.SetOf(Any.Int32()).WithMinCount(minimum), set => set.Count >= minimum, DrawsPerShape)
                               && Expect.EveryDraw(Any.DictionaryOf(Any.Int32(), Any.Int32()).WithMinCount(minimum), map => map.Count >= minimum, DrawsPerShape))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithMaxCount caps the size, for every maximum — including zero.")]
    public void WithMaxCountCapsTheSize() {
        Prop.ForAll(Generators.Count(30).ToArbitrary(),
                    maximum => Expect.EveryDraw(Any.ArrayOf(Any.Int32()).WithMaxCount(maximum), array => array.Length <= maximum, DrawsPerShape)
                               && Expect.EveryDraw(Any.SequenceOf(Any.Int32()).WithMaxCount(maximum), sequence => sequence.Count() <= maximum, DrawsPerShape)
                               && Expect.EveryDraw(Any.SetOf(Any.Int32()).WithMaxCount(maximum), set => set.Count <= maximum, DrawsPerShape))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithCountBetween keeps the size within its inclusive bounds, for every ordered pair.")]
    public void WithCountBetweenStaysWithinItsBounds() {
        // Degenerate pairs (min == max) are deliberately kept: a range that pins the count is the corner where a
        // range resolved as half-open would show up as an off-by-one.
        Prop.ForAll(Generators.OrderedPair(Generators.Count(30)).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.ListOf(Any.Int32()).WithCountBetween(bounds.Min, bounds.Max),
                                               list => list.Count >= bounds.Min && list.Count <= bounds.Max, DrawsPerShape)
                              && Expect.EveryDraw(Any.DictionaryOf(Any.Int32(), Any.Int32()).WithCountBetween(bounds.Min, bounds.Max),
                                                  map => map.Count >= bounds.Min && map.Count <= bounds.Max, DrawsPerShape))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Crossed WithCountBetween arguments are an argument error, never a silent swap.")]
    public void CrossedCountBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Generators.Count(30)).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || Expect.Throws<ArgumentException>(() => Any.ListOf(Any.Int32()).WithCountBetween(bounds.Max, bounds.Min)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A negative count is rejected as an argument error by every count method.")]
    public void NegativeCountsAreAnArgumentError() {
        // Argument validation precedes conflict checking, so these must be ArgumentOutOfRangeException whatever else
        // the generator already carries — a negative count is never a "contradiction with a declared constraint".
        Prop.ForAll(NegativeCount().ToArbitrary(),
                    count => Expect.Throws<ArgumentOutOfRangeException>(() => Any.ListOf(Any.Int32()).WithCount(count))
                             && Expect.Throws<ArgumentOutOfRangeException>(() => Any.SetOf(Any.Int32()).WithMinCount(count))
                             && Expect.Throws<ArgumentOutOfRangeException>(() => Any.ArrayOf(Any.Int32()).WithMaxCount(count))
                             && Expect.Throws<ArgumentOutOfRangeException>(() => Any.DictionaryOf(Any.Int32(), Any.Int32()).WithCountBetween(count, 0)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Empty always yields nothing and NonEmpty never does, whatever the element domain.")]
    public void EmptyAndNonEmptyHoldOverEveryElementDomain() {
        Prop.ForAll(Generators.OrderedPair(Generators.Int32()).ToArbitrary(),
                    bounds => {
                        // A pinned element domain (Min == Max) is the corner that matters here: NonEmpty() must still
                        // resolve a count a distinct collection can fill, which for a single-value domain is exactly
                        // one element — not a conflict, and not an empty draw.
                        AnyInt32 element = Any.Int32().Between(bounds.Min, bounds.Max);

                        return Expect.EveryDraw(Any.ListOf(element).Empty(), list => list.Count == 0, DrawsPerShape)
                               && Expect.EveryDraw(Any.SetOf(element).Empty(), set => set.Count == 0, DrawsPerShape)
                               && Expect.EveryDraw(Any.DictionaryOf(element, Any.Int32()).Empty(), map => map.Count == 0, DrawsPerShape)
                               && Expect.EveryDraw(Any.ListOf(element).NonEmpty(), list => list.Count > 0, DrawsPerShape)
                               && Expect.EveryDraw(Any.ArrayOf(element).NonEmpty(), array => array.Length > 0, DrawsPerShape)
                               && Expect.EveryDraw(Any.SequenceOf(element).NonEmpty(), sequence => sequence.Any(), DrawsPerShape)
                               && Expect.EveryDraw(Any.SetOf(element).NonEmpty(), set => set.Count > 0, DrawsPerShape)
                               && Expect.EveryDraw(Any.DictionaryOf(element, Any.Int32()).NonEmpty(), map => map.Count > 0, DrawsPerShape);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Containing places the value and leaves the count untouched, for every value.")]
    public void ContainingPlacesTheValue() {
        Prop.ForAll((from value in Generators.Int32()
                     from count in Gen.Choose(1, 12)
                     select (value, count)).ToArbitrary(),
                    testCase => Expect.EveryDraw(Any.ListOf(Any.Int32()).WithCount(testCase.count).Containing(testCase.value),
                                                 list => list.Count == testCase.count && list.Contains(testCase.value), DrawsPerShape)
                                && Expect.EveryDraw(Any.ArrayOf(Any.Int32()).WithCount(testCase.count).Containing(testCase.value),
                                                    array => array.Length == testCase.count && array.Contains(testCase.value), DrawsPerShape)
                                && Expect.EveryDraw(Any.SequenceOf(Any.Int32()).WithCount(testCase.count).Containing(testCase.value),
                                                    sequence => sequence.Count() == testCase.count && sequence.Contains(testCase.value), DrawsPerShape)
                                && Expect.EveryDraw(Any.SetOf(Any.Int32()).WithCount(testCase.count).Containing(testCase.value),
                                                    set => set.Count == testCase.count && set.Contains(testCase.value), DrawsPerShape))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "ContainingKey places the key, and ContainingEntry pins exactly that mapping.")]
    public void DictionaryContainmentPlacesTheKeyAndPinsTheEntry() {
        Prop.ForAll((from key in Generators.Int32()
                     from value in Generators.Int32()
                     from count in Gen.Choose(1, 12)
                     select (key, value, count)).ToArbitrary(),
                    testCase => Expect.EveryDraw(Any.DictionaryOf(Any.Int32(), Any.Int32()).WithCount(testCase.count).ContainingKey(testCase.key),
                                                 map => map.Count == testCase.count && map.ContainsKey(testCase.key), DrawsPerShape)
                                && Expect.EveryDraw(Any.DictionaryOf(Any.Int32(), Any.Int32()).WithCount(testCase.count).ContainingEntry(testCase.key, testCase.value),
                                                    map => map.Count == testCase.count
                                                           && map.ContainsKey(testCase.key)
                                                           && map[testCase.key] == testCase.value, DrawsPerShape))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Distinct yields no duplicate and still honours the count, for every count the domain can hold.")]
    public void DistinctYieldsNoDuplicateAndHonoursTheCount() {
        Prop.ForAll((from count in Generators.Count(24)
                     from slack in Generators.Count(16)
                     select (count, slack)).ToArbitrary(),
                    testCase => {
                        // The domain is at its narrowest exactly one value wider than the request, so the count always
                        // fits — this property is about the dedup-draw filling it, not about the eager gate below. The
                        // tightest fits are where a fill that gave up early, or one that let a duplicate through, shows.
                        AnyInt32 element = Any.Int32().Between(1, testCase.count + testCase.slack + 1);

                        return Expect.EveryDraw(Any.ListOf(element).WithCount(testCase.count).Distinct(),
                                                list => list.Count == testCase.count && new HashSet<int>(list).Count == testCase.count, DrawsPerShape)
                               && Expect.EveryDraw(Any.ArrayOf(element).WithCount(testCase.count).Distinct(),
                                                   array => array.Length == testCase.count && new HashSet<int>(array).Count == testCase.count, DrawsPerShape)
                               && Expect.EveryDraw(Any.SequenceOf(element).WithCount(testCase.count).Distinct(),
                                                   sequence => sequence.Count() == testCase.count && new HashSet<int>(sequence).Count == testCase.count, DrawsPerShape);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A set and a dictionary are distinct by nature: their size still matches the requested count.")]
    public void SetAndDictionaryAreDistinctByNature() {
        Prop.ForAll((from count in Generators.Count(24)
                     from slack in Generators.Count(16)
                     select (count, slack)).ToArbitrary(),
                    testCase => {
                        AnyInt32 element = Any.Int32().Between(1, testCase.count + testCase.slack + 1);

                        // A HashSet collapses a repeated element silently and a Dictionary a repeated key, so a size
                        // equal to the request IS the distinctness assertion: a duplicate could only surface as a
                        // collection one element short of what was asked for.
                        return Expect.EveryDraw(Any.SetOf(element).WithCount(testCase.count),
                                                set => set.Count == testCase.count, DrawsPerShape)
                               && Expect.EveryDraw(Any.DictionaryOf(element, Any.Int32()).WithCount(testCase.count),
                                                   map => map.Count == testCase.count, DrawsPerShape);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A distinct count beyond the element generator's advertised cardinality conflicts eagerly, and one within it generates.")]
    public void DistinctCountBeyondTheAdvertisedCardinalityConflictsEagerly() {
        Prop.ForAll((from poolSize in Gen.Choose(1, 8)
                     from count in Generators.Count(12)
                     select (poolSize, count)).ToArbitrary(),
                    testCase => {
                        // Two generators over the very same domain {1..poolSize}, one bounded and one pooled: both
                        // advertise a cardinality, so both must decide feasibility at declaration time. Quantifying
                        // over the pool size AND the requested count walks the whole frontier between the two verdicts
                        // — an example can only ever stand on one side of it.
                        AnyInt32      bounded = Any.Int32().Between(1, testCase.poolSize);
                        AnyOneOf<int> pooled  = Any.OneOf(Pool(testCase.poolSize));

                        if (testCase.count > testCase.poolSize) {
                            return Expect.Throws<ConflictingAnyConstraintException>(() => Any.SetOf(bounded).WithCount(testCase.count))
                                   && Expect.Throws<ConflictingAnyConstraintException>(() => Any.SetOf(pooled).WithCount(testCase.count))
                                   && Expect.Throws<ConflictingAnyConstraintException>(() => Any.ListOf(pooled).WithCount(testCase.count).Distinct())
                                   && Expect.Throws<ConflictingAnyConstraintException>(() => Any.DictionaryOf(bounded, Any.Int32()).WithCount(testCase.count));
                        }

                        return Expect.EveryDraw(Any.SetOf(bounded).WithCount(testCase.count),
                                                set => set.Count == testCase.count && set.All(value => value >= 1 && value <= testCase.poolSize), DrawsPerShape)
                               && Expect.EveryDraw(Any.SetOf(pooled).WithCount(testCase.count),
                                                   set => set.Count == testCase.count && set.All(value => value >= 1 && value <= testCase.poolSize), DrawsPerShape)
                               && Expect.EveryDraw(Any.ListOf(pooled).WithCount(testCase.count).Distinct(),
                                                   list => list.Count == testCase.count && new HashSet<int>(list).Count == testCase.count, DrawsPerShape)
                               && Expect.EveryDraw(Any.DictionaryOf(bounded, Any.Int32()).WithCount(testCase.count),
                                                   map => map.Count == testCase.count, DrawsPerShape);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "More required values than WithMaxCount allows conflicts; up to that many are all placed.")]
    public void RequiredValuesBeyondTheCountCapConflict() {
        Gen<int[]> requiredValues = Gen.NonEmptyListOf(Generators.Int32()).Select(drawn => drawn.Distinct().Take(6).ToArray());

        Prop.ForAll((from values in requiredValues
                     from maximum in Generators.Count(8)
                     select (values, maximum)).ToArbitrary(),
                    testCase => {
                        // Each required value takes one element's room, so the verdict is decided by a single
                        // comparison — and the property holds it over every (how many, how big a cap) pair rather than
                        // over the one pair an example would pin.
                        if (testCase.values.Length > testCase.maximum) {
                            return Expect.Throws<ConflictingAnyConstraintException>(
                                () => RequiringAll(Any.ListOf(Any.Int32()).WithMaxCount(testCase.maximum), testCase.values));
                        }

                        return Expect.EveryDraw(RequiringAll(Any.ListOf(Any.Int32()).WithMaxCount(testCase.maximum), testCase.values),
                                                list => list.Count <= testCase.maximum
                                                        && testCase.values.All(value => list.Contains(value)));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Under a comparer stricter than the default one, every pinned reference-distinct value extends the domain.")]
    public void AStricterComparerLetsEveryPinnedValueExtendTheDomain() {
        Gen<(int Pool, int Pinned)> cases =
            from pool in Gen.Choose(1, 4)
            from pinned in Gen.Choose(1, 4)
            select (Pool: pool, Pinned: pinned);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        // The pool holds Tag(0)..Tag(pool-1) — distinct by value, so AnyOneOf keeps all of them (it
                        // deduplicates under the DEFAULT comparer, which is why the pool cannot itself carry
                        // reference-distinct twins). The pinned values are fresh instances of the SAME values, so
                        // each is value-equal to a pool member and reference-distinct from it. Under ReferenceComparer
                        // the effective domain is therefore pool + pinned for every pair — the input space the pinned
                        // example cannot reach, and the one the eager check got wrong for every pinned >= 1 by
                        // consulting a membership answered under the default comparer.
                        Tag[] pooled = Enumerable.Range(0, testCase.Pool).Select(value => new Tag(value)).ToArray();
                        Tag[] pinned = Enumerable.Range(0, testCase.Pinned).Select(value => new Tag(value % testCase.Pool)).ToArray();

                        AnyList<Tag> generator = Any.ListOf(Any.OneOf(pooled)).Distinct(new ReferenceComparer());
                        foreach (Tag value in pinned) { generator = generator.Containing(value); }

                        List<Tag> list = generator.WithCount(testCase.Pool + testCase.Pinned).Generate();

                        // Reference-counted on both sides: every pinned value present exactly once, every pooled value
                        // present exactly once, and nothing else — the collection really did keep the twins apart.
                        return list.Count == testCase.Pool + testCase.Pinned
                               && pinned.All(value => list.Count(element => ReferenceEquals(element, value)) == 1)
                               && pooled.All(value => list.Count(element => ReferenceEquals(element, value)) == 1);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A generated sequence is fully materialized: enumerating it twice yields the same elements.")]
    public void SequenceIsFullyMaterialized() {
        Prop.ForAll(Generators.Count(30).ToArbitrary(),
                    count => {
                        IEnumerable<int> sequence = Any.SequenceOf(Any.Int32()).WithCount(count).Generate();

                        List<int> first  = sequence.ToList();
                        List<int> second = sequence.ToList();

                        return first.Count == count && first.SequenceEqual(second);
                    })
            .QuickCheckThrowOnFailure();
    }

    #region Nested types

    // A value-equal reference type: two Tag(1) are one value under the default comparer and two under reference
    // equality. That gap between the two comparers is exactly what the eager cardinality check has to respect.
    private sealed class Tag {

        private readonly int _value;

        public Tag(int value) {
            _value = value;
        }

        public override bool Equals(object? other) {
            return other is Tag tag && tag._value == _value;
        }

        public override int GetHashCode() {
            return _value;
        }

    }

    private sealed class ReferenceComparer : IEqualityComparer<Tag> {

        public bool Equals(Tag? x, Tag? y) {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(Tag obj) {
            return RuntimeHelpers.GetHashCode(obj);
        }

    }

    #endregion

}
