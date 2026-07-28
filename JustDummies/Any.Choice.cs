namespace JustDummies;

public static partial class Any {

    /// <summary>
    ///     Draws an arbitrary value from an explicit pool of caller-supplied <paramref name="values" />, over the
    ///     ambient random context — the top-level choice combinator for a value whose domain is a closed set the test
    ///     does not assert on ("one of the configured currencies", "one of the states in this table"). The pool is the
    ///     whole specification, so the returned generator is terminal, yet it still composes through <c>As(...)</c>,
    ///     <c>OrNull()</c>, <c>Combine(...)</c> and the collection generators like any other. To draw from a pool
    ///     already held as a collection, use <see cref="ElementOf{T}(IReadOnlyList{T})" />.
    /// </summary>
    /// <remarks>
    ///     The draw is uniform and reproducible under a seed; duplicates collapse under
    ///     <see cref="EqualityComparer{T}.Default" /> so no value is implicitly weighted, and the distinct count is
    ///     advertised so a distinct collection over the pool gates eagerly. A <c>null</c> element is rejected — make the
    ///     whole generator nullable with <c>OrNull()</c> instead of placing <c>null</c> in the pool.
    /// </remarks>
    /// <param name="values">The pool the generated value is drawn from; duplicates are ignored.</param>
    /// <typeparam name="T">The type of the pooled values.</typeparam>
    /// <returns>A terminal generator drawing uniformly from <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    public static AnyOneOf<T> OneOf<T>(params T[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return AnyOneOf<T>.FromPool(AmbientRandomSource.Instance, values);
    }

    /// <summary>
    ///     Draws an arbitrary value from an explicit pool held as a list — the <see cref="IReadOnlyList{T}" />
    ///     counterpart of <see cref="OneOf{T}(T[])" />, for a pool already materialized (a configuration list, a
    ///     fixture, a lookup table of domain objects). Same terminal contract as <see cref="OneOf{T}(T[])" />: the pool
    ///     is the whole specification, duplicates collapse, and the draw is uniform and reproducible under a seed.
    /// </summary>
    /// <param name="values">The pool the generated value is drawn from; duplicates are ignored.</param>
    /// <typeparam name="T">The type of the pooled values.</typeparam>
    /// <returns>A terminal generator drawing uniformly from <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    public static AnyOneOf<T> ElementOf<T>(IReadOnlyList<T> values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return AnyOneOf<T>.FromPool(AmbientRandomSource.Instance, values);
    }

    /// <summary>
    ///     Draws an arbitrary value from an explicit pool held as a sequence — the <see cref="IEnumerable{T}" />
    ///     counterpart of <see cref="ElementOf{T}(IReadOnlyList{T})" /> (a LINQ result, values loaded at setup). The
    ///     sequence is materialized once, so a lazy query is never re-enumerated per draw.
    /// </summary>
    /// <param name="values">The pool the generated value is drawn from; duplicates are ignored.</param>
    /// <typeparam name="T">The type of the pooled values.</typeparam>
    /// <returns>A terminal generator drawing uniformly from <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    public static AnyOneOf<T> ElementOf<T>(IEnumerable<T> values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return AnyOneOf<T>.FromPool(AmbientRandomSource.Instance, values as IReadOnlyList<T> ?? values.ToArray());
    }

}
