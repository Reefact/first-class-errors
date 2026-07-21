#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

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
        if (generator is IReproducibilityHint hint) { return hint.DrawsOnlyFromSource; }

        return SourceOf(generator) is not null;
    }

    /// <summary>
    ///     A conservative upper bound on the number of distinct values <paramref name="generator" /> yields, when it
    ///     advertises one through <see cref="ICardinalityHint{T}" />; <c>null</c> when the domain is unbounded or unknown.
    /// </summary>
    internal static long? CardinalityOf<T>(IAny<T> generator) {
        return (generator as ICardinalityHint<T>)?.DistinctCardinality;
    }

    /// <summary>
    ///     Runs a user-supplied factory or composer and converts its failure into an
    ///     <see cref="AnyGenerationException" /> that names the generated value(s) and, when the random context is
    ///     known, the seed that replays the run. <paramref name="reproducible" /> tells whether the derived value draws
    ///     only from that source: when it does not — a foreign operand contributes — the hint is qualified rather than
    ///     promising a full replay the seed cannot deliver. The library's own exceptions pass through untouched.
    /// </summary>
    internal static T Invoke<T>(Func<T> invoke, RandomSource? source, bool reproducible, string failure) {
        try {
            return invoke();
        } catch (AnyException) {
            throw;
        } catch (Exception exception) {
            int?   seed    = source?.Current.Seed;
            string message = $"Generation failed: {failure} ({exception.GetType().Name}: {exception.Message}).";
            if (source is not null) {
                message += $" {(reproducible ? source.ReplayHint(seed!.Value) : source.PartialReplayHint(seed!.Value))}";
            }

            throw new AnyGenerationException(message, seed, exception);
        }
    }

    /// <summary>Renders a generated value for an exception message.</summary>
    internal static string Display(object? value) {
        switch (value) {
            case null:            return "null";
            case string text:     return "\"" + text + "\"";
            case IFormattable formattable: return formattable.ToString(null, CultureInfo.InvariantCulture);
            default:              return value.ToString() ?? value.GetType().Name;
        }
    }

}
