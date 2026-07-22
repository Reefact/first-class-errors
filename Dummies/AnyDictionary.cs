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

    #region Fields declarations

    private readonly CollectionState<TKey> _keys;
    private readonly RandomSource?         _source;
    private readonly IAny<TValue>          _values;

    #endregion

    internal AnyDictionary(RandomSource? source, CollectionState<TKey> keys, IAny<TValue> values) {
        _source = source;
        _keys   = keys;
        _values = values;
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

    /// <inheritdoc />
    public Dictionary<TKey, TValue> Generate() {
        List<TKey>                 keys       = _keys.Materialize(_source ?? AmbientRandomSource.Instance);
        Dictionary<TKey, TValue>   dictionary = new(keys.Count, _keys.Comparer);
        foreach (TKey key in keys) { dictionary[key] = _values.Generate(); }

        return dictionary;
    }

    private AnyDictionary<TKey, TValue> With(CollectionState<TKey> keys) {
        return new AnyDictionary<TKey, TValue>(_source, keys, _values);
    }

}
