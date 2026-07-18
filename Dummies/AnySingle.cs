#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="float" /> values — the same contract as <see cref="AnyInt32" />:
///     constraints express what the surrounding code requires of the value, never what the test asserts;
///     contradictory constraints fail eagerly with a <see cref="ConflictingAnyConstraintException" /> naming both
///     sides; instances are immutable recipes. NaN and the infinities are never generated nor accepted.
/// </summary>
public sealed class AnySingle : IAny<float>, IHasRandomSource {

    #region Statics members declarations

    /// <summary>
    ///     Generates the value — an <see cref="AnySingle" /> can be used wherever a <see cref="float" /> is expected.
    ///     Each conversion draws a fresh value.
    /// </summary>
    /// <param name="generator">The generator to draw from.</param>
    /// <returns>An arbitrary value satisfying the generator's constraints.</returns>
    public static implicit operator float(AnySingle generator) {
        return generator.Generate();
    }

    internal static AnySingle Create(RandomSource source) {
        return new AnySingle(source, ContinuousIntervalSpec.Unconstrained("Single", value => V((float)value), value => (float)value, value => NextUp((float)value), -float.MaxValue, float.MaxValue));
    }

    private static string V(float value) {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string Join(float[] values) {
        return string.Join(", ", values.Select(V));
    }

    /// <summary>The next representable float above <paramref name="value" /> — the exclusive-bound arithmetic.</summary>
    private static double NextUp(float value) {
        int bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        if (bits >= 0) { bits++; } else if (bits == int.MinValue) { bits = 1; } else { bits--; }

        float next = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);

        return float.IsInfinity(next) ? double.PositiveInfinity : next;
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource           _source;
    private readonly ContinuousIntervalSpec _spec;

    #endregion

    private AnySingle(RandomSource source, ContinuousIntervalSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Requires a value strictly greater than zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle Positive() {
        return new AnySingle(_source, _spec.WithMinimumAbove(0d, "Positive()"));
    }

    /// <summary>Requires a value strictly less than zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle Negative() {
        return new AnySingle(_source, _spec.WithMaximumBelow(0d, "Negative()"));
    }

    /// <summary>Pins the value to exactly zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle Zero() {
        return new AnySingle(_source, _spec.WithMinimum(0d, "Zero()").WithMaximum(0d, "Zero()"));
    }

    /// <summary>Requires a value different from zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle NonZero() {
        return new AnySingle(_source, _spec.WithExcluded([0d], "NonZero()"));
    }

    /// <summary>Requires a value strictly greater than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle GreaterThan(float value) {
        ContinuousIntervalSpec.EnsureFinite(value, nameof(value));
        return new AnySingle(_source, _spec.WithMinimumAbove(value, $"GreaterThan({V(value)})"));
    }

    /// <summary>Requires a value greater than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle GreaterThanOrEqualTo(float value) {
        ContinuousIntervalSpec.EnsureFinite(value, nameof(value));
        return new AnySingle(_source, _spec.WithMinimum((double)value, $"GreaterThanOrEqualTo({V(value)})"));
    }

    /// <summary>Requires a value strictly less than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle LessThan(float value) {
        ContinuousIntervalSpec.EnsureFinite(value, nameof(value));
        return new AnySingle(_source, _spec.WithMaximumBelow(value, $"LessThan({V(value)})"));
    }

    /// <summary>Requires a value less than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle LessThanOrEqualTo(float value) {
        ContinuousIntervalSpec.EnsureFinite(value, nameof(value));
        return new AnySingle(_source, _spec.WithMaximum((double)value, $"LessThanOrEqualTo({V(value)})"));
    }

    /// <summary>Requires a value within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].</summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when a bound is not finite or <paramref name="minimum" /> is greater than <paramref name="maximum" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle Between(float minimum, float maximum) {
        ContinuousIntervalSpec.EnsureFinite(minimum, nameof(minimum));
        ContinuousIntervalSpec.EnsureFinite(maximum, nameof(maximum));
        if (minimum > maximum) { throw new ArgumentException($"The minimum ({V(minimum)}) must be less than or equal to the maximum ({V(maximum)}).", nameof(minimum)); }

        string constraint = $"Between({V(minimum)}, {V(maximum)})";

        return new AnySingle(_source, _spec.WithMinimum((double)minimum, constraint).WithMaximum((double)maximum, constraint));
    }

    /// <summary>Requires the value to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a non-finite value.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle OneOf(params float[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        foreach (float value in values) { ContinuousIntervalSpec.EnsureFinite(value, nameof(values)); }

        return new AnySingle(_source, _spec.WithAllowed(values.Select(value => (double)value).ToArray(), $"OneOf({Join(values)})"));
    }

    /// <summary>Requires the value to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a non-finite value.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle Except(params float[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        foreach (float value in values) { ContinuousIntervalSpec.EnsureFinite(value, nameof(values)); }

        return new AnySingle(_source, _spec.WithExcluded(values.Select(value => (double)value).ToArray(), $"Except({Join(values)})"));
    }

    /// <summary>
    ///     Requires the value to differ from <paramref name="value" /> — typically an existing value the test already
    ///     holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle DifferentFrom(float value) {
        ContinuousIntervalSpec.EnsureFinite(value, nameof(value));
        return new AnySingle(_source, _spec.WithExcluded([(double)value], $"DifferentFrom({V(value)})"));
    }

    /// <inheritdoc />
    public float Generate() {
        SeededRandom current = _source.Current;

        return (float)_spec.Generate(current.Random, current.Seed);
    }

}
