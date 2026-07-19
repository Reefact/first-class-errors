#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="int" /> values. Each constraint narrows the domain the value is
///     drawn from — the constraints express what the surrounding code <b>requires</b> of the value (an invariant, a
///     precondition), never what the test asserts. Constraints that contradict each other fail immediately with a
///     <see cref="ConflictingAnyConstraintException" /> naming both sides, so an impossible <c>Arrange</c> reads as
///     the test defect it is.
/// </summary>
/// <remarks>
///     <para>
///         Instances are immutable recipes: every method returns a new generator, and the value is drawn only when
///         <see cref="Generate" /> runs, from
///         the random context the generator was created with. Values are <b>built to satisfy</b> the constraints in
///         one draw — the library never generates candidates and retries.
///     </para>
///     <example>
///         <code>
///         int quantity = Any.Int32().Positive().Generate();
///         int percentage = Any.Int32().Between(0, 100).Generate();
///         Any.Int32().GreaterThan(100).LessThan(10); // throws ConflictingAnyConstraintException
///         </code>
///     </example>
/// </remarks>
public sealed class AnyInt32 : IAny<int>, IHasRandomSource, ICardinalityHint<int> {

    #region Statics members declarations

    internal static AnyInt32 Create(RandomSource source) {
        return new AnyInt32(source, OrdinalIntervalSpec.Unconstrained("Int32", ordinal => V(Val(ordinal)), Ord(int.MinValue), Ord(int.MaxValue)));
    }

    private static ulong Ord(int value) {
        return OrdinalMapping.FromInt64(value);
    }

    private static int Val(ulong ordinal) {
        return (int)OrdinalMapping.ToInt64(ordinal);
    }

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Join(int[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource        _source;
    private readonly OrdinalIntervalSpec _spec;

    #endregion

    private AnyInt32(RandomSource source, OrdinalIntervalSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    long? ICardinalityHint<int>.DistinctCardinality => _spec.Cardinality;

    bool ICardinalityHint<int>.Contains(int value) => _spec.Contains(Ord(value));

    /// <summary>Requires a value strictly greater than zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 Positive() {
        return new AnyInt32(_source, _spec.WithMinimum(Ord(1), "Positive()"));
    }

    /// <summary>Requires a value strictly less than zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 Negative() {
        return new AnyInt32(_source, _spec.WithMaximum(Ord(-1), "Negative()"));
    }

    /// <summary>Pins the value to exactly zero. Useful for symmetry with the other constraints when a test sweeps cases.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 Zero() {
        return new AnyInt32(_source, _spec.WithMinimum(Ord(0), "Zero()").WithMaximum(Ord(0), "Zero()"));
    }

    /// <summary>Requires a value different from zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 NonZero() {
        return new AnyInt32(_source, _spec.WithExcluded([Ord(0)], "NonZero()"));
    }

    /// <summary>Requires a value strictly greater than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 GreaterThan(int value) {
        return new AnyInt32(_source, _spec.WithMinimumAbove(Ord(value), $"GreaterThan({V(value)})"));
    }

    /// <summary>Requires a value greater than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 GreaterThanOrEqualTo(int value) {
        return new AnyInt32(_source, _spec.WithMinimum(Ord(value), $"GreaterThanOrEqualTo({V(value)})"));
    }

    /// <summary>Requires a value strictly less than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 LessThan(int value) {
        return new AnyInt32(_source, _spec.WithMaximumBelow(Ord(value), $"LessThan({V(value)})"));
    }

    /// <summary>Requires a value less than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 LessThanOrEqualTo(int value) {
        return new AnyInt32(_source, _spec.WithMaximum(Ord(value), $"LessThanOrEqualTo({V(value)})"));
    }

    /// <summary>Requires a value within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].</summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minimum" /> is greater than <paramref name="maximum" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 Between(int minimum, int maximum) {
        if (minimum > maximum) { throw new ArgumentException($"The minimum ({V(minimum)}) must be less than or equal to the maximum ({V(maximum)}).", nameof(minimum)); }

        string constraint = $"Between({V(minimum)}, {V(maximum)})";

        return new AnyInt32(_source, _spec.WithMinimum(Ord(minimum), constraint).WithMaximum(Ord(maximum), constraint));
    }

    /// <summary>Requires the value to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 OneOf(params int[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new AnyInt32(_source, _spec.WithAllowed(values.Select(Ord).ToArray(), $"OneOf({Join(values)})"));
    }

    /// <summary>Requires the value to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 Except(params int[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new AnyInt32(_source, _spec.WithExcluded(values.Select(Ord).ToArray(), $"Except({Join(values)})"));
    }

    /// <summary>
    ///     Requires the value to differ from <paramref name="value" /> — typically an existing value the test already
    ///     holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyInt32 DifferentFrom(int value) {
        return new AnyInt32(_source, _spec.WithExcluded([Ord(value)], $"DifferentFrom({V(value)})"));
    }

    /// <inheritdoc />
    public int Generate() {
        return Val(_spec.GenerateOrdinal(_source.Current.Random));
    }

}
