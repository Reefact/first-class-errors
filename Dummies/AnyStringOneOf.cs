namespace Dummies;

/// <summary>
///     A generator of arbitrary strings drawn from an <b>explicit, fixed set of values</b> — the dummy for a value
///     whose domain is a closed list a test does not assert on (a well-known company name, a currency code from a
///     short table, a status label). The set is the whole specification, so this is a <i>terminal</i> generator:
///     unlike <see cref="AnyString" /> it exposes no further shape, length or character constraints — whatever
///     matters goes into the values themselves. It still composes like any other generator: pipe it through
///     <c>As(...)</c> into a value object, make it optional with <c>OrNull()</c>, or fold it into <c>Combine(...)</c>
///     and the collection generators.
/// </summary>
/// <remarks>
///     <para>
///         Each <see cref="Generate" /> draws one value uniformly from the set, from the generator's random context —
///         so a run is reproducible under a seed, exactly like every other generator. Duplicate values are collapsed,
///         so no value carries a heavier weight for being listed twice, and the number of distinct values is the exact
///         size of the domain a distinct collection (<c>SetOf</c>, a dictionary's keys) gates against.
///     </para>
///     <example>
///         <code>
///         string vendor = Any.String().OneOf("Apple", "Microsoft", "Google").Generate();
///         IAny&lt;Currency&gt; currency = Any.String().OneOf("EUR", "USD", "GBP").As(Currency.Create);
///         </code>
///     </example>
/// </remarks>
public sealed class AnyStringOneOf : IAny<string>, IHasRandomSource, ICardinalityHint<string> {

    #region Fields declarations

    private readonly RandomSource         _source;
    private readonly IReadOnlyList<string> _values;

    #endregion

    internal AnyStringOneOf(RandomSource source, IReadOnlyList<string> values) {
        _source = source;
        _values = values;
    }

    RandomSource? IHasRandomSource.Source => _source;

    // The value set is fixed and deduplicated at construction, so its count is the exact number of distinct strings
    // drawable, and membership is a direct set lookup under the same ordinal comparison used to deduplicate.
    long? ICardinalityHint<string>.DistinctCardinality => _values.Count;

    bool ICardinalityHint<string>.Contains(string value) => _values.Contains(value, StringComparer.Ordinal);

    /// <inheritdoc />
    public string Generate() {
        return _values[_source.Current.Random.Next(_values.Count)];
    }

}
