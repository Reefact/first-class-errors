namespace Dummies;

/// <summary>
///     Implemented by the library's own generators that draw from a <b>small, countable</b> domain, so a distinct
///     collection (<see cref="AnySet{T}" />, <c>ListOf(...).Distinct()</c>, a dictionary's keys) can tell — at
///     declaration time — that it is being asked for more distinct elements than the element generator could ever
///     produce, and fail eagerly with a <see cref="ConflictingAnyConstraintException" /> instead of only discovering
///     it while drawing.
/// </summary>
/// <remarks>
///     The value is a conservative <b>upper</b> bound on the number of distinct elements the generator yields. A
///     generator whose domain is unbounded, effectively unbounded, or simply unknown (a foreign
///     <see cref="IAny{T}" />, a derived generator) does not implement this interface — the collection then relies on
///     the bounded dedup-draw fallback, which surfaces a genuine shortfall as an <see cref="AnyGenerationException" />.
///     Because the bound ignores any custom <see cref="IEqualityComparer{T}" /> (which can only <i>merge</i> values,
///     never create new ones), it stays a sound upper bound under a comparer too.
///     <para>
///         A generator that advertises a finite cardinality here must also implement
///         <see cref="IDomainMembership{T}" />, so a distinct collection can tell whether a value pinned with
///         <c>Containing(...)</c> extends the domain or already sits inside it; the two capabilities travel together.
///     </para>
/// </remarks>
internal interface ICardinalityHint {

    /// <summary>The number of distinct values the generator can produce, or <c>null</c> when that is unbounded or unknown.</summary>
    long? DistinctCardinality { get; }

}
