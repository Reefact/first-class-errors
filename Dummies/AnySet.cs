namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="HashSet{T}" /> values over an element generator. A set is
///     distinct by nature, so it carries the collection constraint surface
///     (<see cref="AnyCollection{TItem,TResult,TSelf}" />) — count bounds and contained values — without a
///     <c>Distinct()</c> toggle. When the element generator advertises fewer distinct values than the requested
///     count, the contradiction is caught eagerly with a <see cref="ConflictingAnyConstraintException" />; otherwise a
///     genuine shortfall surfaces at generation as an <see cref="AnyGenerationException" />.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class AnySet<T> : AnyCollection<T, HashSet<T>, AnySet<T>> {

    internal AnySet(RandomSource? source, CollectionState<T> state) : base(source, state) { }

    private protected override AnySet<T> With(CollectionState<T> state) {
        return new AnySet<T>(SourceOrNull, state);
    }

    private protected override HashSet<T> Build(List<T> items) {
        // The state already deduplicated under the comparer; the set carries the same comparer so later lookups
        // by the caller behave identically.
        return new HashSet<T>(items, State.Comparer);
    }

}
