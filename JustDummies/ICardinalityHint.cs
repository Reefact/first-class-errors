namespace JustDummies;

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
///         <see cref="AnyGenerationException" />.
///     </para>
///     <para>
///         Both answers are given under <see cref="EqualityComparer{T}.Default" />, and only one of them survives a
///         collection carrying its own <see cref="IEqualityComparer{T}" />. <see cref="DistinctCardinality" /> does:
///         it is an upper bound, and no comparer can make a generator yield more distinct values than it has.
///         <see cref="Contains" /> does not — a comparer <i>stricter</i> than the default one (reference equality over
///         a type with value equality) keeps apart values this membership calls the same, so a value it reports as
///         inside the domain may be one the collection would count as extending it. A collection carrying a custom
///         comparer therefore gates on the bound alone and treats every pinned value as outside: that can only defer
///         to the bounded dedup-draw, never refuse a specification the comparer makes satisfiable.
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
