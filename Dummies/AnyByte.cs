#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="byte" /> values — the same contract as <see cref="AnyInt32" />:
///     constraints express what the surrounding code requires of the value, never what the test asserts;
///     contradictory constraints fail eagerly with a <see cref="ConflictingAnyConstraintException" /> naming both
///     sides; instances are immutable recipes, and each value is built to satisfy the constraints in one draw.
/// </summary>
public sealed class AnyByte : IAny<byte>, IHasRandomSource {

    #region Statics members declarations

    /// <summary>
    ///     Generates the value — an <see cref="AnyByte" /> can be used wherever a <see cref="byte" /> is expected.
    ///     Each conversion draws a fresh value.
    /// </summary>
    /// <param name="generator">The generator to draw from.</param>
    /// <returns>An arbitrary value satisfying the generator's constraints.</returns>
    public static implicit operator byte(AnyByte generator) {
        return generator.Generate();
    }

    internal static AnyByte Create(RandomSource source) {
        return new AnyByte(source, OrdinalIntervalSpec.Unconstrained("Byte", ordinal => V(Val(ordinal)), Ord(byte.MinValue), Ord(byte.MaxValue)));
    }

    private static ulong Ord(byte value) {
        return value;
    }

    private static byte Val(ulong ordinal) {
        return (byte)ordinal;
    }

    private static string V(byte value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Join(byte[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource        _source;
    private readonly OrdinalIntervalSpec _spec;

    #endregion

    private AnyByte(RandomSource source, OrdinalIntervalSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Pins the value to exactly zero. Useful for symmetry with the other constraints when a test sweeps cases.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyByte Zero() {
        return new AnyByte(_source, _spec.WithMinimum(Ord(0), "Zero()").WithMaximum(Ord(0), "Zero()"));
    }

    /// <summary>Requires a value different from zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyByte NonZero() {
        return new AnyByte(_source, _spec.WithExcluded([Ord(0)], "NonZero()"));
    }

    /// <summary>Requires a value strictly greater than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyByte GreaterThan(byte value) {
        return new AnyByte(_source, _spec.WithMinimumAbove(Ord(value), $"GreaterThan({V(value)})"));
    }

    /// <summary>Requires a value greater than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyByte GreaterThanOrEqualTo(byte value) {
        return new AnyByte(_source, _spec.WithMinimum(Ord(value), $"GreaterThanOrEqualTo({V(value)})"));
    }

    /// <summary>Requires a value strictly less than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyByte LessThan(byte value) {
        return new AnyByte(_source, _spec.WithMaximumBelow(Ord(value), $"LessThan({V(value)})"));
    }

    /// <summary>Requires a value less than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyByte LessThanOrEqualTo(byte value) {
        return new AnyByte(_source, _spec.WithMaximum(Ord(value), $"LessThanOrEqualTo({V(value)})"));
    }

    /// <summary>Requires a value within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].</summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minimum" /> is greater than <paramref name="maximum" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyByte Between(byte minimum, byte maximum) {
        if (minimum > maximum) { throw new ArgumentException($"The minimum ({V(minimum)}) must be less than or equal to the maximum ({V(maximum)}).", nameof(minimum)); }

        string constraint = $"Between({V(minimum)}, {V(maximum)})";

        return new AnyByte(_source, _spec.WithMinimum(Ord(minimum), constraint).WithMaximum(Ord(maximum), constraint));
    }

    /// <summary>Requires the value to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyByte OneOf(params byte[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new AnyByte(_source, _spec.WithAllowed(values.Select(Ord).ToArray(), $"OneOf({Join(values)})"));
    }

    /// <summary>Requires the value to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyByte Except(params byte[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new AnyByte(_source, _spec.WithExcluded(values.Select(Ord).ToArray(), $"Except({Join(values)})"));
    }

    /// <summary>
    ///     Requires the value to differ from <paramref name="value" /> — typically an existing value the test already
    ///     holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyByte DifferentFrom(byte value) {
        return new AnyByte(_source, _spec.WithExcluded([Ord(value)], $"DifferentFrom({V(value)})"));
    }

    /// <inheritdoc />
    public byte Generate() {
        return Val(_spec.GenerateOrdinal(_source.Current.Random));
    }

}
