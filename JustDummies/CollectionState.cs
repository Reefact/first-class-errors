#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     The immutable specification behind every collection generator (<see cref="AnyList{T}" />,
///     <see cref="AnyArray{T}" />, <see cref="AnySequence{T}" />, <see cref="AnySet{T}" /> and, for its keys,
///     <see cref="AnyDictionary{TKey,TValue}" />): the element generator, a shared <see cref="CountSpec" />, whether
///     the collection must be distinct, an optional equality comparer, and the values it must contain. Every mutation
///     returns a new state and validates the whole eagerly — so a collection generator that exists can always
///     produce, laid out directly rather than generated-then-filtered.
/// </summary>
/// <remarks>
///     Distinctness follows the two-layer contract the library commits to: when the element generator advertises a
///     small <see cref="ICardinalityHint{T}">cardinality</see> that the requested count would exceed — counting only
///     the elements that must be drawn from that generator, since values pinned with <c>Containing(...)</c> outside
///     its domain (see <see cref="ICardinalityHint{T}" />) are supplied directly and extend the effective domain —
///     the conflict is caught at declaration time (<see cref="ConflictingAnyConstraintException" />); otherwise the
///     count is drawn and the elements are filled by a bounded dedup-draw, and a genuine shortfall surfaces at
///     generation as an <see cref="AnyGenerationException" /> naming the seed to replay.
/// </remarks>
/// <typeparam name="T">The element type.</typeparam>
internal sealed class CollectionState<T> {

    #region Statics members declarations

    internal static CollectionState<T> Create(IAny<T> item, bool distinct, IEqualityComparer<T>? comparer) {
        if (item is null) { throw new ArgumentNullException(nameof(item)); }

        return new CollectionState<T>(item, AnyDerivation.CardinalityOf(item), CountSpec.Unconstrained, distinct, comparer,
                                      Array.Empty<T>(), Array.Empty<IAny<T>>());
    }

    private static string Elements(int count) {
        return count == 1 ? "1 element" : $"{count.ToString(CultureInfo.InvariantCulture)} elements";
    }

    private static IReadOnlyList<TItem> Append<TItem>(IReadOnlyList<TItem> list, TItem value) {
        List<TItem> copy = [.. list, value];

        return copy;
    }

    private static void Shuffle(List<T> items, SeededRandom random) {
        // Fisher-Yates: contained values and filler are appended in a fixed order, so a shuffle keeps a dummy
        // collection from advertising a positional invariant a caller might accidentally rely on.
        for (int index = items.Count - 1; index > 0; index--) {
            int swap = random.Next(index + 1);
            (items[index], items[swap]) = (items[swap], items[index]);
        }
    }

    #endregion

    #region Fields declarations

    private readonly IEqualityComparer<T>?     _comparer;
    private readonly CountSpec                 _count;
    private readonly bool                      _distinct;
    private readonly IReadOnlyList<T>          _fixedContaining;
    private readonly IReadOnlyList<IAny<T>>    _generatedContaining;
    private readonly IAny<T>                   _item;
    private readonly long?                     _itemCardinality;

    #endregion

    private CollectionState(IAny<T> item, long? itemCardinality, CountSpec count, bool distinct,
                            IEqualityComparer<T>? comparer,
                            IReadOnlyList<T> fixedContaining, IReadOnlyList<IAny<T>> generatedContaining) {
        _item                = item;
        _itemCardinality     = itemCardinality;
        _count               = count;
        _distinct            = distinct;
        _comparer            = comparer;
        _fixedContaining     = fixedContaining;
        _generatedContaining = generatedContaining;
    }

    /// <summary>The equality comparer distinct collections deduplicate with, or <c>null</c> for the default.</summary>
    internal IEqualityComparer<T>? Comparer => _comparer;

    /// <summary>Fixes the exact element count.</summary>
    internal CollectionState<T> WithExactCount(int count, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        return Rebuild(_count.WithExactCount(count, applying), _distinct, _comparer, _fixedContaining, _generatedContaining, applying);
    }

    /// <summary>Tightens the minimum element count.</summary>
    internal CollectionState<T> WithMinCount(int count, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        return Rebuild(_count.WithMinCount(count, applying), _distinct, _comparer, _fixedContaining, _generatedContaining, applying);
    }

    /// <summary>Tightens the maximum element count.</summary>
    internal CollectionState<T> WithMaxCount(int count, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        return Rebuild(_count.WithMaxCount(count, applying), _distinct, _comparer, _fixedContaining, _generatedContaining, applying);
    }

    /// <summary>Requires the elements to be pairwise distinct, optionally under <paramref name="comparer" />.</summary>
    internal CollectionState<T> AsDistinct(IEqualityComparer<T>? comparer, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // One collection is distinct under ONE equality, so a second, different comparer cannot be honoured
        // alongside the first: keeping the last one silently would drop a constraint the caller wrote. Declaring
        // the same comparer again, or re-declaring distinctness without one, asks for the equality already in
        // force and stays a no-op.
        if (comparer is not null && _comparer is not null && !ReferenceEquals(comparer, _comparer)) {
            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because a different comparer is already defined by an earlier {applying}.");
        }

        return Rebuild(_count, true, comparer ?? _comparer, _fixedContaining, _generatedContaining, applying);
    }

    /// <summary>Requires the collection to contain <paramref name="value" />.</summary>
    internal CollectionState<T> WithContaining(T value, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        return Rebuild(_count, _distinct, _comparer, Append(_fixedContaining, value), _generatedContaining, applying);
    }

    /// <summary>Requires the collection to contain a value drawn from <paramref name="generator" />.</summary>
    internal CollectionState<T> WithContaining(IAny<T> generator, string applying) {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        return Rebuild(_count, _distinct, _comparer, _fixedContaining, Append(_generatedContaining, generator), applying);
    }

    /// <summary>Builds one collection satisfying the whole specification — laid out directly, never generate-then-retry.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with LINQ expressions",
                                                     Justification =
                                                         "The condition reads the collection the body mutates. Where is lazily evaluated, so lifting the filter out would run each " +
                                                         "predicate against a snapshot taken before the additions it is meant to see, and let duplicates through.")]
    internal List<T> Materialize(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        SeededRandom random   = source.Current;
        int          required = _fixedContaining.Count + _generatedContaining.Count;
        int?         cap      = _distinct ? CardinalityCap() : null;
        int          count    = _count.Resolve(random, required, cap);

        if (!_distinct) {
            List<T> items = new(Math.Max(count, required));
            items.AddRange(_fixedContaining);
            foreach (IAny<T> generator in _generatedContaining) { items.Add(generator.Generate()); }
            while (items.Count < count) { items.Add(_item.Generate()); }
            Shuffle(items, random);

            return items;
        }

        HashSet<T> seen    = new(_comparer ?? EqualityComparer<T>.Default);
        List<T>    ordered = new(count);
        foreach (T value in _fixedContaining) {
            if (seen.Add(value)) { ordered.Add(value); }
        }
        foreach (IAny<T> generator in _generatedContaining) {
            T value = DrawFresh(generator, seen, source, count);
            seen.Add(value);
            ordered.Add(value);
        }
        FillDistinct(ordered, seen, source, count);
        Shuffle(ordered, random);

        return ordered;
    }

    private CollectionState<T> Rebuild(CountSpec count, bool distinct, IEqualityComparer<T>? comparer,
                                       IReadOnlyList<T> fixedContaining, IReadOnlyList<IAny<T>> generatedContaining, string applying) {
        CollectionState<T> candidate = new(_item, _itemCardinality, count, distinct, comparer, fixedContaining, generatedContaining);
        candidate.Validate(applying);

        return candidate;
    }

    private void Validate(string applying) {
        int required = _fixedContaining.Count + _generatedContaining.Count;
        _count.EnsureFits(required, applying);

        if (!_distinct) { return; }

        IEqualityComparer<T> comparer = _comparer ?? EqualityComparer<T>.Default;
        for (int left = 0; left < _fixedContaining.Count; left++) {
            for (int right = left + 1; right < _fixedContaining.Count; right++) {
                if (comparer.Equals(_fixedContaining[left], _fixedContaining[right])) {
                    throw new ConflictingAnyConstraintException($"Cannot apply {applying} because a distinct collection cannot contain {AnyDerivation.Display(_fixedContaining[left])} more than once.");
                }
            }
        }

        if (_itemCardinality is long cardinality) {
            // Only the elements that must be drawn from the generator count against its cardinality: values pinned
            // outside its domain occupy their own slots, and each opaque ContainingAny draw is credited as if it
            // could land outside too (conservative — an unprovable overlap defers to the bounded draw rather than
            // a false conflict). The subtractive form keeps the left side within int, so a near-long.MaxValue
            // domain never overflows the comparison.
            int need          = Math.Max(_count.Floor, required);
            int fromGenerator = need - FixedOutsideCount() - _generatedContaining.Count;
            if (fromGenerator > cardinality) {
                throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {Elements(fromGenerator)} required to be distinct exceed the {cardinality.ToString(CultureInfo.InvariantCulture)} distinct value(s) the element generator can produce.");
            }
        }
    }

    private int? CardinalityCap() {
        // The effective ceiling is the generator's own cardinality plus the values pinned outside its domain, which
        // fill their own slots without drawing on it — so a distinct collection over a small domain can still reach
        // the size those extra values allow. Guard the cast: a domain wider than int cannot cap an int count anyway,
        // and once the generator's cardinality is int-bounded the add stays within long.
        if (_itemCardinality is not long cardinality || cardinality > int.MaxValue) { return null; }

        long effective = cardinality + FixedOutsideCount();

        return effective <= int.MaxValue ? (int)effective : null;
    }

    private int FixedOutsideCount() {
        if (_fixedContaining.Count == 0) { return 0; }
        // A fixed value the element generator could never produce extends the effective distinct domain; a value
        // already inside it does not. The cardinality snapshot came from this same generator, so whenever the eager
        // check runs it also answers membership (cardinality and membership are one interface). Both other branches
        // are the same defensive fallback: treat every fixed value as outside, so the check can only defer to the
        // bounded draw, never falsely reject.
        //
        // A custom comparer takes that fallback because the hint answers membership under the DEFAULT comparer, and a
        // custom one can be stricter — reference equality over a type with value equality is the plain case. Two
        // values the hint reports as one are then two values this collection keeps apart, so consulting it would call
        // a pinned value already-inside when it genuinely extends the domain, and refuse a specification the
        // collection can satisfy. The cardinality itself survives a comparer (a pool of n values is at most n
        // distinct under any of them, so the bound only ever over-states); membership does not.
        if (_comparer is not null || _item is not ICardinalityHint<T> hint) { return _fixedContaining.Count; }

        return _fixedContaining.Count(value => !hint.Contains(value));
    }

    private T DrawFresh(IAny<T> generator, HashSet<T> seen, RandomSource source, int target) {
        int budget = ExhaustionBudget(target);
        for (int collisions = 0;;) {
            T value = generator.Generate();
            if (!seen.Contains(value)) { return value; }
            if (++collisions > budget) { throw Exhausted(source, seen.Count, target, "a ContainingAny(...) generator", generator); }
        }
    }

    private void FillDistinct(List<T> ordered, HashSet<T> seen, RandomSource source, int target) {
        int budget     = ExhaustionBudget(target);
        int collisions = 0;
        while (ordered.Count < target) {
            T value = _item.Generate();
            if (seen.Add(value)) {
                ordered.Add(value);
                collisions = 0;
            } else if (++collisions > budget) {
                throw Exhausted(source, ordered.Count, target, "the element generator", _item);
            }
        }
    }

    private int ExhaustionBudget(int target) {
        // A known finite domain gets a coupon-collector-generous ceiling; an unknown or huge one gets a fixed
        // floor that collisions only reach if the domain is unexpectedly small (for example a comparer that
        // merges most values). Either way the fill is bounded — never an unbounded retry loop.
        long cardinality = _itemCardinality ?? long.MaxValue;
        long bounded     = cardinality <= 1_000_000L ? 64L * cardinality : 64L * target;

        return (int)Math.Min(Math.Max(bounded, 10_000L), int.MaxValue);
    }

    private static AnyGenerationException Exhausted(RandomSource source, int reached, int target, string what, IAny<T> culprit) {
        int seed = source.Current.Seed;
        // The reported seed reproduces the count and layout, but it reproduces the elements only when the failing
        // generator's every draw follows that same source. A foreign IAny, a derivation built over one, or a Combine
        // that mixes a foreign operand with a sourced one is not fully reproducible (AnyDerivation.IsReproducible is
        // false) — even where it still carries a non-null source to name — so promising a full replay of its elements
        // would be false: qualify the hint instead.
        string replay = AnyDerivation.IsReproducible(culprit)
                            ? source.ReplayGuidance(seed)
                            : source.PartialReplayGuidance(seed);
        string message = $"Could not generate a distinct collection of {Elements(target)}: {what} produced only {reached} distinct value(s) before the draw budget was exhausted. Loosen the count or widen the element generator's domain. {replay}";

        return new AnyGenerationException(message, seed);
    }

}
