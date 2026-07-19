namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="IEnumerable{T}" /> values over an element generator. The generated
///     sequence is <b>fully materialized</b>: it never defers work, so enumerating it twice yields the same elements
///     and never re-draws. Shares the collection constraint surface
///     (<see cref="AnyCollection{TItem,TResult,TSelf}" />) — count bounds and contained values — and adds
///     <see cref="Distinct()" /> to require pairwise-distinct elements.
/// </summary>
/// <remarks>
///     Materialize the sequence with <see cref="AnyCollection{TItem,TResult,TSelf}.Generate" />, or use the generator
///     through <see cref="IAny{T}" />.
/// </remarks>
/// <typeparam name="T">The element type.</typeparam>
public sealed class AnySequence<T> : AnyCollection<T, IEnumerable<T>, AnySequence<T>> {

    internal AnySequence(RandomSource? source, CollectionState<T> state) : base(source, state) { }

    /// <summary>Requires the elements to be pairwise distinct (default equality).</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint cannot be satisfied by the element generator's domain.</exception>
    public AnySequence<T> Distinct() {
        return With(State.AsDistinct(null, "Distinct()"));
    }

    /// <summary>Requires the elements to be pairwise distinct under <paramref name="comparer" />.</summary>
    /// <param name="comparer">The equality comparer deciding whether two elements are the same.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint cannot be satisfied by the element generator's domain.</exception>
    public AnySequence<T> Distinct(IEqualityComparer<T> comparer) {
        if (comparer is null) { throw new ArgumentNullException(nameof(comparer)); }

        return With(State.AsDistinct(comparer, "Distinct(comparer)"));
    }

    private protected override AnySequence<T> With(CollectionState<T> state) {
        return new AnySequence<T>(SourceOrNull, state);
    }

    private protected override IEnumerable<T> Build(List<T> items) {
        return items;
    }

}
