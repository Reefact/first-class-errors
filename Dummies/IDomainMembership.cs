namespace Dummies;

/// <summary>
///     Companion to <see cref="ICardinalityHint" />: implemented by the library's own generators that draw from a
///     <b>small, countable</b> domain so a distinct collection can decide, at declaration time, whether a value
///     pinned with <c>Containing(...)</c> lies inside or outside the element generator's domain. A fixed value the
///     generator could never produce (<see cref="Contains" /> returns <c>false</c>) adds one to the effective
///     distinct cardinality — it is a value the generator itself cannot draw — while a value already in the domain
///     does not. See <see cref="CollectionState{T}" /> for how the effective cardinality gates satisfiability.
/// </summary>
/// <remarks>
///     <para>
///         The pairing is load-bearing: every generator that advertises a finite
///         <see cref="ICardinalityHint.DistinctCardinality" /> must also implement this interface, and
///         <see cref="Contains" /> must agree exactly with that finite domain — the same predicate the generator
///         honours when it draws. Cardinality answers "how many", membership answers "which". A generator whose
///         domain is unbounded or unknown implements neither; the collection then relies on the bounded dedup-draw
///         fallback. Should a finite-cardinality generator ever omit this interface, <see cref="CollectionState{T}" />
///         stays safe by treating every contained value as outside the domain — conservative, so the eager check
///         never rejects a request that might be satisfiable.
///     </para>
///     <para>
///         Membership is decided under the generator's own (default) equality. A custom collection
///         <see cref="IEqualityComparer{T}" /> can only <i>merge</i> values, never create new ones, so a value that
///         is "provably outside" under default equality remains a sound upper-bound contribution to the effective
///         cardinality; a comparer that collapses it back into the domain is caught by the bounded draw instead.
///     </para>
/// </remarks>
/// <typeparam name="T">The element type.</typeparam>
internal interface IDomainMembership<T> {

    /// <summary>Whether the generator, as constrained, could ever produce <paramref name="value" />.</summary>
    /// <param name="value">The candidate value.</param>
    /// <returns><c>true</c> when <paramref name="value" /> is within the generator's domain; otherwise <c>false</c>.</returns>
    bool Contains(T value);

}
