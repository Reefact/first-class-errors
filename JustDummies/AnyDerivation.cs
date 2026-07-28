#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     A generator derived from other generators (<c>As</c>, <c>Combine</c>): it delegates generation to a closure
///     and carries, when known, the random context of the generators it derives from — so a failure inside the
///     derivation can still name the seed that replays the run. It also remembers whether every operand it draws from
///     is reproducible (<see cref="IReproducibilityHint" />): a single foreign operand leaves a non-null source to name
///     but makes the derived value unreproducible, which the seed reporting must not over-promise.
/// </summary>
/// <typeparam name="T">The type of the generated values.</typeparam>
internal sealed class DerivedAny<T> : IAny<T>, IHasRandomSource, IReproducibilityHint {

    #region Fields declarations

    private readonly bool          _drawsOnlyFromSource;
    private readonly Func<T>       _generate;
    private readonly RandomSource? _source;

    #endregion

    internal DerivedAny(RandomSource? source, bool drawsOnlyFromSource, Func<T> generate) {
        if (generate is null) { throw new ArgumentNullException(nameof(generate)); }

        _source              = source;
        _drawsOnlyFromSource = drawsOnlyFromSource;
        _generate            = generate;
    }

    RandomSource? IHasRandomSource.Source => _source;

    bool IReproducibilityHint.DrawsOnlyFromSource => _drawsOnlyFromSource;

    /// <inheritdoc />
    public T Generate() {
        return _generate();
    }

}

/// <summary>Shared plumbing of the derived generators.</summary>
internal static class AnyDerivation {

    /// <summary>The random context of <paramref name="generator" />, when it is one of the library's own.</summary>
    internal static RandomSource? SourceOf<T>(IAny<T> generator) {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }

        return (generator as IHasRandomSource)?.Source;
    }

    /// <summary>
    ///     Whether every value <paramref name="generator" /> yields is replayable from the source it reports: <c>true</c>
    ///     for a library generator carrying a source, and for a derivation whose operands are all themselves
    ///     reproducible; <c>false</c> for a foreign generator (no source) or a derivation built over one. This is
    ///     stronger than <see cref="SourceOf{T}" /> being non-null — a <c>Combine</c> that mixes a foreign operand with a
    ///     library one keeps a non-null source to name, yet its composed value follows the foreign draw and cannot be
    ///     replayed from that seed.
    /// </summary>
    internal static bool IsReproducible<T>(IAny<T> generator) {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }

        if (generator is IReproducibilityHint hint) { return hint.DrawsOnlyFromSource; }

        return SourceOf(generator) is not null;
    }

    /// <summary>
    ///     Whether <paramref name="generator" /> is reproducible <b>and</b> draws from <paramref name="source" />
    ///     specifically — the per-operand condition for a <c>Combine</c>'s full-replay promise. An operand that is
    ///     individually reproducible but draws from a <i>different</i> seeded source (a second <see cref="Any.WithSeed" />
    ///     context, or the ambient source alongside a fixed one) leaves the reported seed covering only part of the run,
    ///     so naming it as a deterministic full replay would over-promise. When the operands do not all draw from the one
    ///     reported source, the hint is qualified instead — exactly as it is for a foreign operand.
    /// </summary>
    internal static bool DrawsOnlyFrom<T>(IAny<T> generator, RandomSource? source) {
        return IsReproducible(generator) && ReferenceEquals(SourceOf(generator), source);
    }

    /// <summary>
    ///     A conservative upper bound on the number of distinct values <paramref name="generator" /> yields, when it
    ///     advertises one through <see cref="ICardinalityHint{T}" />; <c>null</c> when the domain is unbounded or unknown.
    /// </summary>
    internal static long? CardinalityOf<T>(IAny<T> generator) {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }

        return (generator as ICardinalityHint<T>)?.DistinctCardinality;
    }

    /// <summary>
    ///     Runs a user-supplied factory or composer and converts its failure into an
    ///     <see cref="AnyGenerationException" /> that names the generated value(s) and, when the random context is
    ///     known, the seed that replays the run. <paramref name="reproducible" /> tells whether the derived value draws
    ///     only from that source: when it does not — a foreign operand contributes — the hint is qualified rather than
    ///     promising a full replay the seed cannot deliver. The library's own exceptions pass through untouched.
    ///     <para>
    ///         <paramref name="failure" /> is a <b>thunk</b>, not a string, because rendering the generated values is
    ///         only ever needed on the failing path: an eagerly interpolated message would run the caller's
    ///         <c>ToString()</c> — and allocate the whole sentence — on every successful draw, which is every draw a
    ///         test actually makes.
    ///     </para>
    /// </summary>
    internal static T Invoke<T>(Func<T> invoke, RandomSource? source, bool reproducible, Func<string> failure) {
        if (invoke is null) { throw new ArgumentNullException(nameof(invoke)); }
        if (failure is null) { throw new ArgumentNullException(nameof(failure)); }

        try {
            return invoke();
        } catch (DummyException) {
            throw;
        } catch (Exception exception) {
            int?   seed    = source?.Current.Seed;
            string message = $"Generation failed: {failure()} ({exception.GetType().Name}: {exception.Message}).";
            if (source is not null) {
                message += $" {(reproducible ? source.ReplayGuidance(seed!.Value) : source.PartialReplayGuidance(seed!.Value))}";
            }

            throw new AnyGenerationException(message, seed, exception);
        }
    }

    /// <summary>
    ///     Renders a generated value for an exception message. A value's own <c>ToString()</c> is user code and may
    ///     throw — a domain object rendering state the fixture never set is the ordinary case. A renderer that let
    ///     that through would replace the diagnostic being built with an unrelated failure, hiding the constraint
    ///     conflict or factory rejection the caller needs to read, so a throwing rendering falls back to the type
    ///     name: the message loses a detail, never the report it was explaining.
    /// </summary>
    internal static string Display(object? value) {
        switch (value) {
            case null:        return "null";
            case string text: return "\"" + text + "\"";
            default:
                try {
                    return value is IFormattable formattable
                               ? formattable.ToString(null, CultureInfo.InvariantCulture)
                               : value.ToString() ?? value.GetType().Name;
                } catch (Exception) {
                    return value.GetType().Name;
                }
        }
    }

}
