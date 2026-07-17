namespace Dummies;

/// <summary>
///     Composition over <see cref="IAny{T}" />. These extensions are the bridge between constrained primitives and
///     domain types: a generator of raw values becomes a generator of value objects by going through the type's own
///     factory — no reflection, and the domain's validation stays the single gatekeeper.
/// </summary>
public static class AnyExtensions {

    /// <summary>
    ///     Derives a generator of <typeparamref name="TResult" /> by passing each generated
    ///     <typeparamref name="TSource" /> through <paramref name="factory" /> — typically a value object's own
    ///     factory method, so the constraints declared upstream express the invariant that factory enforces.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the factory throws, the failure is wrapped in an <see cref="AnyGenerationException" /> naming the
    ///         generated value and, when known, the seed that replays the run — the usual cause is constraints weaker
    ///         than the invariant the factory enforces, and the fix is to tighten them.
    ///     </para>
    ///     <example>
    ///         <code>
    ///         IAny&lt;OrderReference&gt; reference = Any.String()
    ///             .StartingWith("ORD-")
    ///             .WithLength(12)
    ///             .As(OrderReference.Create);
    ///         </code>
    ///     </example>
    /// </remarks>
    /// <param name="generator">The generator of the raw values.</param>
    /// <param name="factory">The factory turning a raw value into a <typeparamref name="TResult" />.</param>
    /// <typeparam name="TSource">The type of the raw generated values.</typeparam>
    /// <typeparam name="TResult">The type the factory produces.</typeparam>
    /// <returns>A generator of <typeparamref name="TResult" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator" /> or <paramref name="factory" /> is <c>null</c>.</exception>
    public static IAny<TResult> As<TSource, TResult>(this IAny<TSource> generator, Func<TSource, TResult> factory) {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }
        if (factory is null) { throw new ArgumentNullException(nameof(factory)); }

        RandomSource? source = AnyDerivation.SourceOf(generator);

        return new DerivedAny<TResult>(source, () => {
            TSource value = generator.Generate();

            return AnyDerivation.Invoke(() => factory(value), source, $"the factory passed to As(...) threw for the generated value {AnyDerivation.Display(value)}");
        });
    }

}
