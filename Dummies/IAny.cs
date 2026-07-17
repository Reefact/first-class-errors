namespace Dummies;

/// <summary>
///     A recipe for an arbitrary value of type <typeparamref name="T" /> that satisfies the constraints declared on
///     it. This is the composition seam of the library: every generator — built-in or derived through
///     <see cref="AnyExtensions.As{TSource,TResult}" /> and <see cref="Any.Combine{T1,T2,TResult}" /> — implements it,
///     so a constrained primitive, a value object built from one, and an object assembled from several all flow
///     through the same contract.
/// </summary>
/// <remarks>
///     <para>
///         A generator is an <b>immutable recipe</b>, not a value: each fluent constraint returns a new generator, and
///         randomness is drawn only when <see cref="Generate" /> runs, from the random context the generator was
///         created with — the ambient context for the static <see cref="Any" /> entry points (see
///         <see cref="Any.Reproducibly(Action, Action{String})" />), or the isolated context of
///         <see cref="Any.WithSeed" />. The same recipe can therefore be generated from several times, yielding a
///         fresh value each time.
///     </para>
///     <para>
///         Generic inference flows through this interface — <c>Materialize(Any.String().NonEmpty())</c> infers
///         <c>T = string</c> — whereas the implicit conversions the concrete generators offer are an ergonomic
///         convenience only.
///     </para>
/// </remarks>
/// <typeparam name="T">The type of the generated values.</typeparam>
public interface IAny<out T> {

    /// <summary>
    ///     Produces one arbitrary value satisfying every constraint declared on this generator.
    /// </summary>
    /// <returns>A value that satisfies the declared constraints.</returns>
    /// <exception cref="AnyGenerationException">
    ///     Thrown when the value cannot be produced even though the declared constraints were accepted — for example
    ///     when a factory passed to <see cref="AnyExtensions.As{TSource,TResult}" /> rejects a generated value.
    /// </exception>
    T Generate();

}
