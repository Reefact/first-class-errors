#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="string" /> values. Each constraint narrows the shape of the
///     generated string — the constraints express what the surrounding code <b>requires</b> of the value (a value
///     object's format invariant, a contract precondition), never what the test asserts. Constraints that contradict
///     each other fail immediately with a <see cref="ConflictingAnyConstraintException" /> naming both sides, so an
///     impossible <c>Arrange</c> reads as the test defect it is.
/// </summary>
/// <remarks>
///     <para>
///         Instances are immutable recipes: every method returns a new generator, and the value is drawn only when
///         <see cref="Generate" /> runs,
///         from the random context the generator was created with. Strings are <b>built to satisfy</b> the
///         constraints — laid out as <c>prefix + filler + contained values + filler + suffix</c> — never generated
///         and filtered. That layout means fragments never overlap: the length budget they require is the plain sum
///         of their lengths.
///     </para>
///     <para>
///         Unconstrained, the generator yields 0 to 16 ASCII letters and digits; an unconstrained draw can therefore
///         be empty — chain <see cref="NonEmpty" /> when the surrounding code requires content.
///     </para>
///     <example>
///         <code>
///         string code = Any.String().NonEmpty().WithMaxLength(50).StartingWith("ORD-").Generate();
///         Any.String().WithLength(3).StartingWith("ORD-");  // throws ConflictingAnyConstraintException
///         Any.String().Numeric().StartingWith("ORD-");      // throws ConflictingAnyConstraintException
///         </code>
///     </example>
/// </remarks>
public sealed class AnyString : IAny<string>, IHasRandomSource {

    #region Statics members declarations

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string RequireText(string value, string parameterName) {
        if (value is null) { throw new ArgumentNullException(parameterName); }
        if (value.Length == 0) { throw new ArgumentException("The value must not be empty.", parameterName); }

        return value;
    }

    private static int RequireNonNegative(int length, string parameterName) {
        if (length < 0) { throw new ArgumentOutOfRangeException(parameterName, length, "The length must not be negative."); }

        return length;
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource _source;
    private readonly StringSpec   _spec;

    #endregion

    internal AnyString(RandomSource source, StringSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Requires at least one character.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString NonEmpty() {
        return new AnyString(_source, _spec.WithMinLength(1, "NonEmpty()"));
    }

    /// <summary>Fixes the exact length. Declared once per generator.</summary>
    /// <param name="length">The exact number of characters.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString WithLength(int length) {
        RequireNonNegative(length, nameof(length));

        return new AnyString(_source, _spec.WithExactLength(length, $"WithLength({V(length)})"));
    }

    /// <summary>Requires at least <paramref name="length" /> characters.</summary>
    /// <param name="length">The inclusive minimum number of characters.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString WithMinLength(int length) {
        RequireNonNegative(length, nameof(length));

        return new AnyString(_source, _spec.WithMinLength(length, $"WithMinLength({V(length)})"));
    }

    /// <summary>Requires at most <paramref name="length" /> characters.</summary>
    /// <param name="length">The inclusive maximum number of characters.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString WithMaxLength(int length) {
        RequireNonNegative(length, nameof(length));

        return new AnyString(_source, _spec.WithMaxLength(length, $"WithMaxLength({V(length)})"));
    }

    /// <summary>Requires a length within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].</summary>
    /// <param name="minimum">The inclusive minimum number of characters.</param>
    /// <param name="maximum">The inclusive maximum number of characters.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a bound is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minimum" /> is greater than <paramref name="maximum" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString WithLengthBetween(int minimum, int maximum) {
        RequireNonNegative(minimum, nameof(minimum));
        RequireNonNegative(maximum, nameof(maximum));
        if (minimum > maximum) { throw new ArgumentException($"The minimum ({V(minimum)}) must be less than or equal to the maximum ({V(maximum)}).", nameof(minimum)); }

        string constraint = $"WithLengthBetween({V(minimum)}, {V(maximum)})";

        return new AnyString(_source, _spec.WithMinLength(minimum, constraint).WithMaxLength(maximum, constraint));
    }

    /// <summary>Requires the string to start with <paramref name="prefix" />. Declared once per generator.</summary>
    /// <param name="prefix">The required prefix.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="prefix" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="prefix" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString StartingWith(string prefix) {
        RequireText(prefix, nameof(prefix));

        return new AnyString(_source, _spec.WithPrefix(prefix, $"StartingWith(\"{prefix}\")"));
    }

    /// <summary>Requires the string to end with <paramref name="suffix" />. Declared once per generator.</summary>
    /// <param name="suffix">The required suffix.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="suffix" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="suffix" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString EndingWith(string suffix) {
        RequireText(suffix, nameof(suffix));

        return new AnyString(_source, _spec.WithSuffix(suffix, $"EndingWith(\"{suffix}\")"));
    }

    /// <summary>
    ///     Requires the string to contain <paramref name="value" />. May be declared several times; the contained
    ///     values are laid out side by side, without overlap, between the prefix and the suffix.
    /// </summary>
    /// <param name="value">The value the generated string must contain.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString Containing(string value) {
        RequireText(value, nameof(value));

        return new AnyString(_source, _spec.WithFragment(value, $"Containing(\"{value}\")"));
    }

    /// <summary>Restricts the string to ASCII letters only. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString Alpha() {
        return new AnyString(_source, _spec.WithCharset(CharacterSet.Alpha, "Alpha()"));
    }

    /// <summary>Restricts the string to ASCII digits only. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString Numeric() {
        return new AnyString(_source, _spec.WithCharset(CharacterSet.Numeric, "Numeric()"));
    }

    /// <summary>Restricts the string to ASCII letters and digits only. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString AlphaNumeric() {
        return new AnyString(_source, _spec.WithCharset(CharacterSet.AlphaNumeric, "AlphaNumeric()"));
    }

    /// <summary>Requires every alphabetic character to be lowercase. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString LowerCase() {
        return new AnyString(_source, _spec.WithCasing(LetterCasing.Lower, "LowerCase()"));
    }

    /// <summary>Requires every alphabetic character to be uppercase. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString UpperCase() {
        return new AnyString(_source, _spec.WithCasing(LetterCasing.Upper, "UpperCase()"));
    }

    /// <inheritdoc />
    public string Generate() {
        return _spec.Generate(_source.Current.Random);
    }

}
