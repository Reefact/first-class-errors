namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary array (<c>T[]</c>) values over an element generator. Shares the collection
///     constraint surface (<see cref="AnyCollection{TItem,TResult,TSelf}" />) — count bounds and contained values —
///     and adds <see cref="Distinct()" /> to require pairwise-distinct elements.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class AnyArray<T> : AnyCollection<T, T[], AnyArray<T>> {

    internal AnyArray(RandomSource? source, CollectionState<T> state) : base(source, state) { }

    /// <summary>Requires the elements to be pairwise distinct (default equality).</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint cannot be satisfied by the element generator's domain.</exception>
    public AnyArray<T> Distinct() {
        return With(State.AsDistinct(null, "Distinct()"));
    }

    /// <summary>Requires the elements to be pairwise distinct under <paramref name="comparer" />.</summary>
    /// <param name="comparer">The equality comparer deciding whether two elements are the same.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint cannot be satisfied by the element generator's domain.</exception>
    public AnyArray<T> Distinct(IEqualityComparer<T> comparer) {
        if (comparer is null) { throw new ArgumentNullException(nameof(comparer)); }

        return With(State.AsDistinct(comparer, "Distinct(comparer)"));
    }

    private protected override AnyArray<T> With(CollectionState<T> state) {
        return new AnyArray<T>(SourceOrNull, state);
    }

    private protected override T[] Build(List<T> items) {
        return items.ToArray();
    }

}
