namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="Dictionary{TKey,TValue}" /> values over a key generator and a value
///     generator. The keys are distinct by nature — the count constraints bound the number of entries, and the key
///     generator's domain gates feasibility exactly as it does for a <see cref="AnySet{T}" />: too small a key domain
///     for the requested count fails eagerly with a <see cref="ConflictingAnyConstraintException" />, a genuine
///     shortfall surfaces at generation as an <see cref="AnyGenerationException" />.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public sealed class AnyDictionary<TKey, TValue> : IAny<Dictionary<TKey, TValue>>, IHasRandomSource
    where TKey : notnull {

    #region Statics members declarations

    private static readonly IReadOnlyDictionary<TKey, TValue> NoPinnedValues = new Dictionary<TKey, TValue>();

    #endregion

    #region Fields declarations

    private readonly CollectionState<TKey>             _keys;
    private readonly IReadOnlyDictionary<TKey, TValue> _pinnedValues;
    private readonly RandomSource?                     _source;
    private readonly IAny<TValue>                      _values;

    #endregion

    internal AnyDictionary(RandomSource? source, CollectionState<TKey> keys, IAny<TValue> values)
        : this(source, keys, values, NoPinnedValues) { }

    private AnyDictionary(RandomSource? source, CollectionState<TKey> keys, IAny<TValue> values, IReadOnlyDictionary<TKey, TValue> pinnedValues) {
        _source       = source;
        _keys         = keys;
        _values       = values;
        _pinnedValues = pinnedValues;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Requires at least one entry.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDictionary<TKey, TValue> NonEmpty() {
        return With(CountConstraints.NonEmpty(_keys));
    }

    /// <summary>Fixes the dictionary to no entries.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDictionary<TKey, TValue> Empty() {
        return With(CountConstraints.Empty(_keys));
    }

    /// <summary>Fixes the exact number of entries. Declared once per generator.</summary>
    /// <param name="count">The exact number of entries.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDictionary<TKey, TValue> WithCount(int count) {
        return With(CountConstraints.WithCount(_keys, count));
    }

    /// <summary>Requires at least <paramref name="count" /> entries.</summary>
    /// <param name="count">The inclusive minimum number of entries.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDictionary<TKey, TValue> WithMinCount(int count) {
        return With(CountConstraints.WithMinCount(_keys, count));
    }

    /// <summary>Requires at most <paramref name="count" /> entries.</summary>
    /// <param name="count">The inclusive maximum number of entries.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDictionary<TKey, TValue> WithMaxCount(int count) {
        return With(CountConstraints.WithMaxCount(_keys, count));
    }

    /// <summary>Requires a number of entries within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].</summary>
    /// <param name="minimum">The inclusive minimum number of entries.</param>
    /// <param name="maximum">The inclusive maximum number of entries.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a bound is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minimum" /> is greater than <paramref name="maximum" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDictionary<TKey, TValue> WithCountBetween(int minimum, int maximum) {
        return With(CountConstraints.WithCountBetween(_keys, minimum, maximum));
    }

    /// <summary>
    ///     Requires the dictionary to contain an entry for <paramref name="key" />. May be declared several times;
    ///     each required key takes one entry's room and the required keys must be distinct. A key outside the key
    ///     generator's domain extends the effective cardinality exactly as <see cref="AnySet{T}" />'s containment
    ///     does, so an otherwise impossible entry count becomes reachable; the entry's value is generated like any
    ///     other. Named <c>ContainingKey</c> rather than a bare <c>Containing</c> so the surface reads unambiguously
    ///     on a dictionary, whose elements are key/value pairs.
    /// </summary>
    /// <param name="key">The key the generated dictionary must contain.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDictionary<TKey, TValue> ContainingKey(TKey key) {
        return With(_keys.WithContaining(key, $"ContainingKey({AnyDerivation.Display(key)})"));
    }

    /// <summary>
    ///     Requires the dictionary to contain an entry whose key is drawn from <paramref name="generator" /> at
    ///     generation time — the key analogue of a collection's <c>ContainingAny</c>. Named apart from
    ///     <see cref="ContainingKey" /> to keep the two cases legible: <see cref="ContainingKey" /> pins a concrete
    ///     key known now, whereas this draws one from a generator when the dictionary is built. The drawn key takes
    ///     one entry's room and its value is generated like any other.
    /// </summary>
    /// <param name="generator">The generator whose drawn key the dictionary must contain.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDictionary<TKey, TValue> ContainingAnyKey(IAny<TKey> generator) {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }

        return With(_keys.WithContaining(generator, "ContainingAnyKey(<generator>)"));
    }

    /// <summary>
    ///     Requires the dictionary to contain the entry <paramref name="key" /> → <paramref name="value" />: the key
    ///     is forced in exactly as <see cref="ContainingKey" /> does (inheriting the out-of-domain cardinality
    ///     credit), and its value is pinned to <paramref name="value" /> instead of being drawn from the value
    ///     generator; the other entries stay arbitrary. Declaring two entries for the same key — or an entry and a
    ///     <see cref="ContainingKey" /> for it — conflicts, since the keys must be distinct.
    /// </summary>
    /// <param name="key">The key the generated dictionary must contain.</param>
    /// <param name="value">The value pinned to <paramref name="key" />.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDictionary<TKey, TValue> ContainingEntry(TKey key, TValue value) {
        CollectionState<TKey> keys = _keys.WithContaining(key, $"ContainingEntry({AnyDerivation.Display(key)}, {AnyDerivation.Display(value)})");

        Dictionary<TKey, TValue> pinned = new(_pinnedValues.Count + 1, _keys.Comparer);
        foreach (KeyValuePair<TKey, TValue> entry in _pinnedValues) { pinned[entry.Key] = entry.Value; }
        pinned[key] = value;

        return new AnyDictionary<TKey, TValue>(_source, keys, _values, pinned);
    }

    /// <inheritdoc />
    public Dictionary<TKey, TValue> Generate() {
        List<TKey>                 keys       = _keys.Materialize(_source ?? AmbientRandomSource.Instance);
        Dictionary<TKey, TValue>   dictionary = new(keys.Count, _keys.Comparer);
        foreach (TKey key in keys) {
            dictionary[key] = _pinnedValues.ContainsKey(key) ? _pinnedValues[key] : _values.Generate();
        }

        return dictionary;
    }

    private AnyDictionary<TKey, TValue> With(CollectionState<TKey> keys) {
        return new AnyDictionary<TKey, TValue>(_source, keys, _values, _pinnedValues);
    }

}
