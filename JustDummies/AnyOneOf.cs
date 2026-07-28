namespace JustDummies;

/// <summary>
///     A generator that draws an arbitrary value from an <b>explicit, fixed pool</b> supplied by the caller — the
///     dummy for a value whose domain is a closed set a test does not assert on (one of the currencies a context is
///     configured with, one of the orders already in a fixture, one of a handful of domain states). Unlike the typed
///     builders' <c>OneOf</c>, which narrows <i>within</i> a scalar's own domain, this draws from values the library
///     could never synthesize on its own. It still composes like any other generator — pipe it through <c>As(...)</c>
///     into a value object, make it optional with <c>OrNull()</c>, or fold it into <c>Combine(...)</c> and the
///     collection generators.
/// </summary>
/// <remarks>
///     <para>
///         Each <see cref="Generate" /> draws one value uniformly from the pool, from the generator's random context —
///         so a run is reproducible under a seed, exactly like every other generator. Duplicate values are collapsed
///         under <see cref="EqualityComparer{T}.Default" />, so no value carries a heavier weight for being listed
///         twice, and the number of distinct values is the exact size of the domain a distinct collection
///         (<c>SetOf</c>, a dictionary's keys) gates against.
///     </para>
///     <para>
///         The pool is the whole <i>shape</i> of the specification: <typeparamref name="T" /> is opaque to the
///         library, so there is no type-specific constraint to offer. What it does expose is the type-agnostic
///         exclusion pair <see cref="Except" />/<see cref="DifferentFrom" />, which every other generator carries —
///         they remove values from the pool rather than describing a shape, and removing everything is a
///         <see cref="ConflictingAnyConstraintException" /> at declaration, like any other emptied domain.
///     </para>
///     <para>
///         A <c>null</c> element is rejected at construction: nullability is an orthogonal concern expressed by
///         <c>OrNull()</c>, never smuggled into the pool.
///     </para>
///     <example>
///         <code>
///         Currency currency = Any.OneOf(eur, usd, gbp).Generate();
///         Order    order    = Any.ElementOf(existingOrders).DifferentFrom(theOneAlreadyUsed).Generate();
///         </code>
///     </example>
/// </remarks>
/// <typeparam name="T">The type of the pooled values.</typeparam>
public sealed class AnyOneOf<T> : IAny<T>, IHasRandomSource, ICardinalityHint<T> {

    #region Statics members declarations

    // Validates and deduplicates the caller's pool, then builds the generator. As an internal boundary it guards its
    // own arguments per the null-argument convention (ADR-0045); the public factories additionally reject a null
    // array first, under the caller-facing parameter name, before delegating here. The factory names itself so a
    // later exclusion conflict can say which declaration it emptied.
    internal static AnyOneOf<T> FromPool(RandomSource source, IReadOnlyList<T> values, string declaring) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (declaring is null) { throw new ArgumentNullException(nameof(declaring)); }
        if (values.Count == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        if (values.Any(value => value is null)) { throw new ArgumentException("The values must not contain a null element; use OrNull() to make the whole generator nullable.", nameof(values)); }

        T[] distinct = values.Distinct().ToArray();

        return new AnyOneOf<T>(source, distinct, distinct, declaring);
    }

    #endregion

    #region Fields declarations

    // The pool as declared, before any exclusion removed a value from it. It is what tells a conflict message
    // whether the applied exclusion forbids the whole declared pool or merely the part earlier exclusions had left,
    // so the message can make the stronger claim exactly when it is true.
    private readonly IReadOnlyList<T> _declared;
    private readonly string           _declaringConstraint;
    private readonly RandomSource     _source;
    private readonly IReadOnlyList<T> _values;

    #endregion

    private AnyOneOf(RandomSource source, IReadOnlyList<T> values, IReadOnlyList<T> declared, string declaringConstraint) {
        _source              = source;
        _values              = values;
        _declared            = declared;
        _declaringConstraint = declaringConstraint;
    }

    RandomSource? IHasRandomSource.Source => _source;

    // The pool is fixed and deduplicated at construction under the default comparer, and an exclusion filters it
    // under that same comparer, so its count is the exact number of distinct values still drawable and membership is
    // a direct lookup. The two answers do not survive a custom comparer equally. The count does: a pool of n values
    // is at most n distinct under any comparer, so the advertised size stays a sound upper bound. Membership does
    // not: a comparer stricter than the default one keeps apart values this lookup calls equal, so it may report a
    // value as drawable that, under that comparer, the pool can never yield. A distinct collection carrying a custom
    // comparer must therefore not consult membership — CollectionState.FixedOutsideCount is where that is enforced.
    long? ICardinalityHint<T>.DistinctCardinality => _values.Count;

    bool ICardinalityHint<T>.Contains(T value) => _values.Contains(value);

    /// <summary>
    ///     Requires the generated value to be none of the supplied <paramref name="values" /> — they are removed from
    ///     the pool under <see cref="EqualityComparer{T}.Default" />, so the draw stays a single uniform pick over
    ///     what is left. May be declared several times; the exclusions accumulate. A value that is not in the pool
    ///     removes nothing.
    /// </summary>
    /// <param name="values">The values the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no pooled value is left once the excluded ones are removed.</exception>
    public AnyOneOf<T> Except(params T[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        if (values.Any(value => value is null)) { throw new ArgumentException("The values must not contain a null element.", nameof(values)); }

        return Excluding(values, "Except(...)");
    }

    /// <summary>
    ///     Requires the generated value to differ from <paramref name="value" /> — typically a value the test already
    ///     holds, to exercise an inequality path while still drawing from the pool
    ///     (<c>Any.ElementOf(orders).DifferentFrom(theOneAlreadyUsed)</c>). Semantically equivalent to
    ///     <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no pooled value is left once <paramref name="value" /> is removed.</exception>
    public AnyOneOf<T> DifferentFrom(T value) {
        if (value is null) { throw new ArgumentNullException(nameof(value)); }

        return Excluding([value], "DifferentFrom(...)");
    }

    /// <inheritdoc />
    public T Generate() {
        return _values[_source.Current.Next(_values.Count)];
    }

    private AnyOneOf<T> Excluding(IReadOnlyList<T> excluded, string applying) {
        T[] survivors = _values.Where(value => !excluded.Contains(value)).ToArray();
        if (survivors.Length == 0) {
            // The values themselves are never rendered: T is opaque, so its ToString is the caller's, not the
            // library's, and could be anything. Naming the two declarations in play is what the caller needs. The
            // claim is qualified only when this exclusion leaves some declared value standing — the emptiness then
            // genuinely took the earlier exclusions too. When it forbids the whole declared pool, dropping the
            // earlier ones could not help, and saying otherwise would send the caller at the wrong constraint.
            string emptied = _declared.Any(value => !excluded.Contains(value))
                                 ? $"it forbids every value {_declaringConstraint} allows that the exclusions already declared leave"
                                 : $"it forbids every value {_declaringConstraint} allows";

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {emptied}.");
        }

        return new AnyOneOf<T>(_source, survivors, _declared, _declaringConstraint);
    }

}
