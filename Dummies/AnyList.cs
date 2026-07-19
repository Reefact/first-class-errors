namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="List{T}" /> values over an element generator. Shares the collection
///     constraint surface (<see cref="AnyCollection{TItem,TResult,TSelf}" />) — count bounds and contained values —
///     and adds <see cref="Distinct()" /> to require pairwise-distinct elements.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class AnyList<T> : AnyCollection<T, List<T>, AnyList<T>> {

    internal AnyList(RandomSource? source, CollectionState<T> state) : base(source, state) { }

    /// <summary>Requires the elements to be pairwise distinct (default equality).</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint cannot be satisfied by the element generator's domain.</exception>
    public AnyList<T> Distinct() {
        return With(State.AsDistinct(null, "Distinct()"));
    }

    /// <summary>Requires the elements to be pairwise distinct under <paramref name="comparer" />.</summary>
    /// <param name="comparer">The equality comparer deciding whether two elements are the same.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint cannot be satisfied by the element generator's domain.</exception>
    public AnyList<T> Distinct(IEqualityComparer<T> comparer) {
        if (comparer is null) { throw new ArgumentNullException(nameof(comparer)); }

        return With(State.AsDistinct(comparer, "Distinct(comparer)"));
    }

    private protected override AnyList<T> With(CollectionState<T> state) {
        return new AnyList<T>(SourceOrNull, state);
    }

    private protected override List<T> Build(List<T> items) {
        return items;
    }

}
