namespace JustDummies;

public static partial class Any {

    /// <summary>
    ///     Starts an arbitrary <see cref="List{T}" /> generator over <paramref name="item" />. Unconstrained, it yields
    ///     0 to 8 elements; chain constraints to express what the surrounding code requires (<c>NonEmpty()</c>,
    ///     <c>WithCount(...)</c>, <c>Distinct()</c>, <c>Containing(...)</c>).
    /// </summary>
    /// <param name="item">The generator each element is drawn from.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A list generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> is <c>null</c>.</exception>
    public static AnyList<T> ListOf<T>(IAny<T> item) {
        if (item is null) { throw new ArgumentNullException(nameof(item)); }

        return new AnyList<T>(AnyDerivation.SourceOf(item), CollectionState<T>.Create(item, false, null));
    }

    /// <summary>
    ///     Starts an arbitrary array (<c>T[]</c>) generator over <paramref name="item" /> — same constraint surface as
    ///     <see cref="ListOf{T}" />, producing an array.
    /// </summary>
    /// <param name="item">The generator each element is drawn from.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>An array generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> is <c>null</c>.</exception>
    public static AnyArray<T> ArrayOf<T>(IAny<T> item) {
        if (item is null) { throw new ArgumentNullException(nameof(item)); }

        return new AnyArray<T>(AnyDerivation.SourceOf(item), CollectionState<T>.Create(item, false, null));
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="IEnumerable{T}" /> generator over <paramref name="item" /> — same constraint
    ///     surface as <see cref="ListOf{T}" />. The generated sequence is fully materialized, so it never re-draws when
    ///     enumerated more than once.
    /// </summary>
    /// <param name="item">The generator each element is drawn from.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A sequence generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> is <c>null</c>.</exception>
    public static AnySequence<T> SequenceOf<T>(IAny<T> item) {
        if (item is null) { throw new ArgumentNullException(nameof(item)); }

        return new AnySequence<T>(AnyDerivation.SourceOf(item), CollectionState<T>.Create(item, false, null));
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="HashSet{T}" /> generator over <paramref name="item" /> — distinct by nature.
    ///     When the count exceeds the number of distinct values <paramref name="item" /> can produce, the conflict is
    ///     reported eagerly.
    /// </summary>
    /// <param name="item">The generator each element is drawn from.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A set generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> is <c>null</c>.</exception>
    public static AnySet<T> SetOf<T>(IAny<T> item) {
        if (item is null) { throw new ArgumentNullException(nameof(item)); }

        return new AnySet<T>(AnyDerivation.SourceOf(item), CollectionState<T>.Create(item, true, null));
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="HashSet{T}" /> generator over <paramref name="item" />, deduplicating
    ///     elements with <paramref name="comparer" /> — the same comparer the resulting set carries.
    /// </summary>
    /// <param name="item">The generator each element is drawn from.</param>
    /// <param name="comparer">The equality comparer deciding whether two elements are the same.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A set generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> or <paramref name="comparer" /> is <c>null</c>.</exception>
    public static AnySet<T> SetOf<T>(IAny<T> item, IEqualityComparer<T> comparer) {
        if (item is null) { throw new ArgumentNullException(nameof(item)); }
        if (comparer is null) { throw new ArgumentNullException(nameof(comparer)); }

        return new AnySet<T>(AnyDerivation.SourceOf(item), CollectionState<T>.Create(item, true, comparer));
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="Dictionary{TKey,TValue}" /> generator drawing keys from
    ///     <paramref name="keys" /> and values from <paramref name="values" />. Keys are distinct by nature, so the key
    ///     generator's domain gates feasibility exactly as it does for <see cref="SetOf{T}(IAny{T})" />.
    /// </summary>
    /// <param name="keys">The generator each key is drawn from.</param>
    /// <param name="values">The generator each value is drawn from.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>A dictionary generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keys" /> or <paramref name="values" /> is <c>null</c>.</exception>
    public static AnyDictionary<TKey, TValue> DictionaryOf<TKey, TValue>(IAny<TKey> keys, IAny<TValue> values)
        where TKey : notnull {
        if (keys is null) { throw new ArgumentNullException(nameof(keys)); }
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        RandomSource? source = AnyDerivation.SourceOf(keys) ?? AnyDerivation.SourceOf(values);

        return new AnyDictionary<TKey, TValue>(source, CollectionState<TKey>.Create(keys, true, null), values);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="Dictionary{TKey,TValue}" /> generator whose keys are deduplicated with
    ///     <paramref name="keyComparer" /> — the same comparer the resulting dictionary carries.
    /// </summary>
    /// <param name="keys">The generator each key is drawn from.</param>
    /// <param name="values">The generator each value is drawn from.</param>
    /// <param name="keyComparer">The equality comparer deciding whether two keys are the same.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>A dictionary generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static AnyDictionary<TKey, TValue> DictionaryOf<TKey, TValue>(IAny<TKey> keys, IAny<TValue> values, IEqualityComparer<TKey> keyComparer)
        where TKey : notnull {
        if (keys is null) { throw new ArgumentNullException(nameof(keys)); }
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (keyComparer is null) { throw new ArgumentNullException(nameof(keyComparer)); }

        RandomSource? source = AnyDerivation.SourceOf(keys) ?? AnyDerivation.SourceOf(values);

        return new AnyDictionary<TKey, TValue>(source, CollectionState<TKey>.Create(keys, true, keyComparer), values);
    }

}
