namespace Dummies;

/// <summary>
///     Thrown when a generation cannot be completed even though every declared constraint was accepted — most
///     commonly when a factory passed to <see cref="AnyExtensions.As{TSource,TResult}" /> or a composer passed to
///     <see cref="Any.Combine{T1,T2,TResult}" /> rejects a generated value. Whenever the failing generator draws from
///     one of the library's random contexts, the message names the seed that replays the run and <see cref="Seed" />
///     carries it.
/// </summary>
/// <remarks>
///     The library prefers detecting contradictions <i>before</i> generation — those throw
///     <see cref="ConflictingAnyConstraintException" /> at declaration time. Reaching this exception therefore
///     usually means the constraints declared on the generator were weaker than the invariant the factory enforces;
///     the fix is to tighten the constraints so they express that invariant.
/// </remarks>
public sealed class AnyGenerationException : AnyException {

    /// <summary>
    ///     Initializes a new instance of the <see cref="AnyGenerationException" /> class.
    /// </summary>
    /// <param name="message">A description of the failed generation.</param>
    public AnyGenerationException(string message) : base(message) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AnyGenerationException" /> class wrapping an underlying failure.
    /// </summary>
    /// <param name="message">A description of the failed generation.</param>
    /// <param name="innerException">The underlying failure.</param>
    public AnyGenerationException(string message, Exception innerException) : base(message, innerException) { }

    internal AnyGenerationException(string message, int? seed, Exception innerException) : base(message, innerException) {
        Seed = seed;
    }

    /// <summary>
    ///     The seed of the random context the failing generation drew from, when it is known — pass it to
    ///     <c>Any.Reproducibly(seed, ...)</c> to replay the run. <c>null</c> when the failing generator does not draw
    ///     from one of the library's random contexts.
    /// </summary>
    public int? Seed { get; }

}
