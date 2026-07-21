namespace Dummies;

/// <summary>
///     A generator that draws an arbitrary value from an <b>explicit, fixed pool</b> supplied by the caller — the
///     dummy for a value whose domain is a closed set a test does not assert on (one of the currencies a context is
///     configured with, one of the orders already in a fixture, one of a handful of domain states). Unlike the typed
///     builders' <c>OneOf</c>, which narrows <i>within</i> a scalar's own domain, this draws from values the library
///     could never synthesize on its own, so the pool is the whole specification: it is a <i>terminal</i> generator
///     exposing no further constraints. It still composes like any other generator — pipe it through <c>As(...)</c>
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
///         A <c>null</c> element is rejected at construction: nullability is an orthogonal concern expressed by
///         <c>OrNull()</c>, never smuggled into the pool — the same rule the string value-set generator applies.
///     </para>
///     <example>
///         <code>
///         Currency currency = Any.OneOf(eur, usd, gbp).Generate();
///         Order    order    = Any.ElementOf(existingOrders).Generate();
///         </code>
///     </example>
/// </remarks>
/// <typeparam name="T">The type of the pooled values.</typeparam>
public sealed class AnyOneOf<T> : IAny<T>, IHasRandomSource, ICardinalityHint<T> {

    #region Statics members declarations

    // Validates and deduplicates the caller's pool, then builds the generator. The array-null check belongs to the
    // public factories (they own the parameter name); by the time we get here the pool is non-null and materialized.
    internal static AnyOneOf<T> FromPool(RandomSource source, IReadOnlyList<T> values) {
        if (values.Count == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        if (values.Any(value => value is null)) { throw new ArgumentException("The values must not contain a null element; use OrNull() to make the whole generator nullable.", nameof(values)); }

        return new AnyOneOf<T>(source, values.Distinct().ToArray());
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource     _source;
    private readonly IReadOnlyList<T> _values;

    #endregion

    private AnyOneOf(RandomSource source, IReadOnlyList<T> values) {
        _source = source;
        _values = values;
    }

    RandomSource? IHasRandomSource.Source => _source;

    // The pool is fixed and deduplicated at construction under the default comparer, so its count is the exact number
    // of distinct values drawable, and membership is a direct lookup under that same comparer. Both deliberately ignore
    // any custom comparer a downstream distinct collection carries: such a comparer can only merge values, never create
    // new ones, so the advertised size stays a sound upper bound and membership never claims a value the pool lacks.
    long? ICardinalityHint<T>.DistinctCardinality => _values.Count;

    bool ICardinalityHint<T>.Contains(T value) => _values.Contains(value);

    /// <inheritdoc />
    public T Generate() {
        return _values[_source.Current.Random.Next(_values.Count)];
    }

}
