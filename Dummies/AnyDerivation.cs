#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     A generator derived from other generators (<c>As</c>, <c>Combine</c>): it delegates generation to a closure
///     and carries, when known, the random context of the generators it derives from — so a failure inside the
///     derivation can still name the seed that replays the run.
/// </summary>
/// <typeparam name="T">The type of the generated values.</typeparam>
internal sealed class DerivedAny<T> : IAny<T>, IHasRandomSource {

    #region Fields declarations

    private readonly Func<T>       _generate;
    private readonly RandomSource? _source;

    #endregion

    internal DerivedAny(RandomSource? source, Func<T> generate) {
        _source   = source;
        _generate = generate;
    }

    RandomSource? IHasRandomSource.Source => _source;

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
    ///     Runs a user-supplied factory or composer and converts its failure into an
    ///     <see cref="AnyGenerationException" /> that names the generated value(s) and, when the random context is
    ///     known, the seed that replays the run. The library's own exceptions pass through untouched.
    /// </summary>
    internal static T Invoke<T>(Func<T> invoke, RandomSource? source, string failure) {
        try {
            return invoke();
        } catch (AnyException) {
            throw;
        } catch (Exception exception) {
            int?   seed    = source?.Current.Seed;
            string message = $"Generation failed: {failure} ({exception.GetType().Name}: {exception.Message}).";
            if (seed is not null) {
                message += $" The arbitrary values were seeded with {seed}; reproduce this run with Any.Reproducibly({seed}, ...).";
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
