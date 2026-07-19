namespace Dummies;

/// <summary>
///     Implemented by the library's own generators that draw from a <b>small, countable</b> domain, so a distinct
///     collection (<see cref="AnySet{T}" />, <c>ListOf(...).Distinct()</c>, a dictionary's keys) can tell — at
///     declaration time — whether a requested count, together with any values pinned through <c>Containing(...)</c>,
///     can be satisfied from the effective domain, and fail eagerly with a
///     <see cref="ConflictingAnyConstraintException" /> instead of only discovering it while drawing.
/// </summary>
/// <remarks>
///     The two members travel together on purpose — that is the whole point of putting them on one interface:
///     <see cref="DistinctCardinality" /> answers "<i>how many</i> distinct values can the generator produce" (a
///     conservative <b>upper</b> bound), and <see cref="Contains" /> answers "<i>is this one</i> of them". A distinct
///     collection needs both: the size to gate the count, and membership to tell a contained value that
///     <i>extends</i> the domain (one the generator could never draw) from one already inside it. Because they are a
///     single contract, a generator cannot advertise a cardinality without also answering membership — the compiler
///     keeps the promise, so no generator can drift out of the eager perimeter unnoticed.
///     <para>
///         A generator whose domain is unbounded, effectively unbounded, or simply unknown (a foreign
///         <see cref="IAny{T}" />, a derived generator) does not implement this interface; the collection then relies
///         on the bounded dedup-draw fallback, which surfaces a genuine shortfall as an
///         <see cref="AnyGenerationException" />. Because both the bound and membership ignore any custom
///         <see cref="IEqualityComparer{T}" /> (which can only <i>merge</i> values, never create new ones), they stay
///         sound under a comparer too.
///     </para>
/// </remarks>
/// <typeparam name="T">The element type.</typeparam>
internal interface ICardinalityHint<T> {

    /// <summary>The number of distinct values the generator can produce, or <c>null</c> when that is unbounded or unknown.</summary>
    long? DistinctCardinality { get; }

    /// <summary>Whether the generator, as constrained, could ever produce <paramref name="value" />.</summary>
    /// <param name="value">The candidate value.</param>
    /// <returns><c>true</c> when <paramref name="value" /> is within the generator's domain; otherwise <c>false</c>.</returns>
    bool Contains(T value);

}
