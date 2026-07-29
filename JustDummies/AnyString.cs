#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

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
///     <para>
///         <see cref="OneOf(string[])" /> is the one constraint that replaces the layout rather than shaping it: the
///         caller supplies the values, so the draw is a uniform pick from them and every other constraint narrows
///         that set instead of building a string. The constraints still fail at declaration when they contradict
///         each other — which is why a value set is best declared <b>first</b>: constraints that contradict each
///         other on their own terms are refused the moment they are declared, before any value set can reinterpret
///         them as a filter (see <see cref="OneOf(string[])" />).
///     </para>
///     <example>
///         <code>
///         string code = Any.String().NonEmpty().WithMaxLength(50).StartingWith("ORD-").Generate();
///         Any.String().WithLength(3).StartingWith("ORD-");  // throws ConflictingAnyConstraintException
///         Any.String().Numeric().StartingWith("ORD-");      // throws ConflictingAnyConstraintException
///         </code>
///     </example>
/// </remarks>
public sealed class AnyString : IAny<string>, IHasRandomSource, ICardinalityHint<string> {

    #region Statics members declarations

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Join(string[] values) {
        return string.Join(", ", values.Select(value => $"\"{value}\""));
    }

    private static void RequireText(string value, string parameterName) {
        if (value is null) { throw new ArgumentNullException(parameterName); }
        if (value.Length == 0) { throw new ArgumentException("The value must not be empty.", parameterName); }
    }

    private static void RequireNonNegative(int length, string parameterName) {
        SizeGuard.RequireNonNegative(length, parameterName, "length");
    }

    private static void RequireProducible(int length, string parameterName) {
        SizeGuard.RequireProducible(length, parameterName, "length");
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource _source;
    private readonly StringSpec   _spec;

    #endregion

    internal AnyString(RandomSource source, StringSpec spec) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (spec is null) { throw new ArgumentNullException(nameof(spec)); }
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    // Only a value set (OneOf) makes the domain small and countable: it is then the exact surviving pool, so a
    // distinct collection gates on it eagerly. A shaped string has no such bound — the specification answers null,
    // and membership is never consulted on that path (the two answers travel together on one interface).
    long? ICardinalityHint<string>.DistinctCardinality => _spec.Cardinality;

    // A distinct collection may pin a null value — an unlikely but legal Containing(null) — and asking whether the
    // generator could produce it is a question, not a boundary violation: the answer is simply no, since a value set
    // rejects a null element at declaration. The specification's own guard stays the internal boundary (ADR-0045);
    // this membership answer must not turn a pinned null into an exception the pool generator never raises.
    bool ICardinalityHint<string>.Contains(string value) => value is not null && _spec.Contains(value);

    /// <summary>Requires at least one character.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString NonEmpty() {
        return new AnyString(_source, _spec.WithMinLength(1, "NonEmpty()"));
    }

    /// <summary>Fixes the exact length. Declared once per generator.</summary>
    /// <param name="length">The exact number of characters.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length" /> is negative or exceeds 1000000, the largest length a generator is asked to produce.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString WithLength(int length) {
        RequireProducible(length, nameof(length));

        return new AnyString(_source, _spec.WithExactLength(length, $"WithLength({V(length)})"));
    }

    /// <summary>
    ///     Requires at least <paramref name="length" /> characters. A minimum is the only one-sided length bound that
    ///     enlarges the generated string: the draw spans <paramref name="length" /> to <paramref name="length" /> plus
    ///     the default spread.
    /// </summary>
    /// <param name="length">The inclusive minimum number of characters.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length" /> is negative or exceeds 1000000, the largest length a generator is asked to produce.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString WithMinLength(int length) {
        RequireProducible(length, nameof(length));

        return new AnyString(_source, _spec.WithMinLength(length, $"WithMinLength({V(length)})"));
    }

    /// <summary>
    ///     Requires at most <paramref name="length" /> characters. A maximum only ever narrows the draw: it never
    ///     widens it beyond the default spread, so a loose cap still yields the small unconstrained string rather than
    ///     one sized after the cap. Any non-negative value is accepted, since nothing has to be produced to honour it.
    /// </summary>
    /// <param name="length">The inclusive maximum number of characters.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString WithMaxLength(int length) {
        RequireNonNegative(length, nameof(length));

        return new AnyString(_source, _spec.WithMaxLength(length, $"WithMaxLength({V(length)})"));
    }

    /// <summary>
    ///     Requires a length within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].
    ///     Equivalent to declaring the two bounds separately, and behaves as they do: the minimum sets the size, the
    ///     maximum only caps it. A range whose minimum is 0 therefore yields the default spread, not values spread
    ///     across the whole range — raise <paramref name="minimum" /> to ask for larger strings.
    /// </summary>
    /// <param name="minimum">The inclusive minimum number of characters.</param>
    /// <param name="maximum">The inclusive maximum number of characters.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a bound is negative, or when <paramref name="minimum" /> exceeds 1000000, the largest length a generator is asked to produce.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minimum" /> is greater than <paramref name="maximum" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString WithLengthBetween(int minimum, int maximum) {
        RequireProducible(minimum, nameof(minimum));
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

    /// <summary>
    ///     Restricts the string to the characters of an explicit <paramref name="pool" /> — a custom alphabet, the
    ///     general form of <see cref="Alpha" />/<see cref="Numeric" />/<see cref="AlphaNumeric" />. Use it to reach
    ///     characters the named sets cannot, most notably non-ASCII text (accents, other scripts), without a
    ///     <see cref="Any.StringMatching(string)" /> literal. Declared once per generator: it occupies the same
    ///     character-family slot as the named sets, and because the pool is the whole character definition it cannot
    ///     combine with <see cref="LowerCase" />/<see cref="UpperCase" /> — put only the casing you want in the pool.
    ///     Any anchored fragment (prefix, suffix, contained value) must be drawn from the pool, otherwise the conflict
    ///     is reported at declaration naming both sides. Duplicate characters collapse and each distinct character is
    ///     equally likely. The pool is a sequence of UTF-16 code units and must stay within the Basic Multilingual
    ///     Plane: a surrogate — an emoji or other astral code point, which spans two units — is rejected, because it
    ///     would be drawn and split unit by unit; draw such values as whole strings with <see cref="OneOf(string[])" />
    ///     instead.
    /// </summary>
    /// <param name="pool">The characters the generated string is drawn from; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pool" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pool" /> is empty or contains a surrogate (an astral code point).</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString WithChars(string pool) {
        if (pool is null) { throw new ArgumentNullException(nameof(pool)); }
        if (pool.Length == 0) { throw new ArgumentException("The character pool must not be empty.", nameof(pool)); }
        if (pool.Any(char.IsSurrogate)) { throw new ArgumentException("The character pool must not contain a surrogate: an emoji or other astral code point spans two UTF-16 code units, which WithChars would draw and split independently. Draw such values as whole strings with OneOf(...) instead.", nameof(pool)); }

        string distinct = new(pool.Distinct().ToArray());

        return new AnyString(_source, _spec.WithCharPool(distinct, $"WithChars(\"{pool}\")"));
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

    /// <summary>
    ///     Requires the generated string to be none of the supplied <paramref name="values" />. May be declared several
    ///     times; the exclusions accumulate. On a <i>shaped</i> string, and unlike the shape constraints, an exclusion
    ///     is met by a <b>bounded</b> redraw of the constructed layout, so one tight enough to leave the shape
    ///     unsatisfiable surfaces at <see cref="Generate" /> as a seed-bearing
    ///     <see cref="AnyGenerationException" /> rather than as a declaration-time conflict. On a string drawn from a
    ///     value set (<see cref="OneOf(string[])" />) there is nothing to redraw: the excluded values are removed from
    ///     the set at once, and removing all of them is a conflict here and now. The empty string is a valid value to
    ///     exclude; a <c>null</c> element is not.
    /// </summary>
    /// <param name="values">The values the generated string must differ from; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a value set is in force and the exclusion leaves none of its values.</exception>
    public AnyString Except(params string[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        if (values.Any(value => value is null)) { throw new ArgumentException("The values must not contain a null element.", nameof(values)); }

        return new AnyString(_source, _spec.WithExcluded(values, $"Except({Join(values)})"));
    }

    /// <summary>
    ///     Requires the generated string to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds, to exercise an inequality path while preserving the declared shape. Semantically equivalent to
    ///     <see cref="Except(string[])" />, including when a value set is in force; the name carries the intent at the
    ///     call site.
    /// </summary>
    /// <param name="value">The value the generated string must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a value set is in force and <paramref name="value" /> is the only value it leaves.</exception>
    public AnyString DifferentFrom(string value) {
        if (value is null) { throw new ArgumentNullException(nameof(value)); }

        return new AnyString(_source, _spec.WithExcluded([value], $"DifferentFrom(\"{value}\")"));
    }

    /// <summary>
    ///     Draws the string from an explicit, fixed set of <paramref name="values" /> instead of shaping one — the
    ///     dummy for a value whose domain is a closed list the test does not assert on (a currency code, a well-known
    ///     name). Declared once per generator, and <b>composable</b> like every other family's <c>OneOf</c>: the other
    ///     constraints keep their meaning and narrow the set rather than shaping a string, so
    ///     <c>OneOf("abc", "de").WithLength(3)</c> yields <c>"abc"</c>. A constraint no supplied value satisfies is a
    ///     <see cref="ConflictingAnyConstraintException" /> naming both sides, whichever order the two were declared
    ///     in. Duplicate values are collapsed; the generated string is one of the surviving values, drawn uniformly
    ///     and reproducibly under a seed.
    /// </summary>
    /// <remarks>
    ///     Declare it <b>first</b> when the values are the point. Constraints that contradict each other on their own
    ///     terms are still refused the moment they are declared — the generator cannot know a value set is coming, and
    ///     deferring that refusal would cost every shaped string its eager conflict. So
    ///     <c>OneOf("aba").WithMaxLength(3).Containing("ab").Containing("ba")</c> yields <c>"aba"</c>, while the same
    ///     constraints with <c>OneOf</c> last conflict on the layout budget before the values are ever seen: laid out
    ///     side by side those two fragments need four characters, even though the supplied value contains both in
    ///     three.
    /// </remarks>
    /// <param name="values">The values the generated string is drawn from; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString OneOf(params string[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        if (values.Any(value => value is null)) { throw new ArgumentException("The values must not contain a null element; use OrNull() to make the whole generator nullable.", nameof(values)); }

        return new AnyString(_source, _spec.WithAllowed(values, $"OneOf({Join(values)})"));
    }

    /// <summary>
    ///     Draws the string from an explicit, fixed set of <paramref name="values" /> — the
    ///     <see cref="IEnumerable{T}" /> counterpart of <see cref="OneOf(string[])" />, for a set already held as a
    ///     sequence (a list, a LINQ result, values loaded at test setup). Same contract: the set composes with the
    ///     other constraints, duplicates collapse, and the draw is uniform and reproducible under a seed.
    /// </summary>
    /// <param name="values">The values the generated string is drawn from; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyString OneOf(IEnumerable<string> values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return OneOf(values as string[] ?? values.ToArray());
    }

    /// <inheritdoc />
    public string Generate() {
        return _spec.Generate(_source);
    }

}
