namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="char" /> values. Unconstrained, it draws from ASCII letters and
///     digits — the same readable default as <see cref="AnyString" />'s filler — and the constraints mirror the
///     string character families: <see cref="Alpha" />, <see cref="Numeric" />, <see cref="AlphaNumeric" />,
///     <see cref="LowerCase" />, <see cref="UpperCase" />, plus <see cref="OneOf" /> / <see cref="Except" /> /
///     <see cref="DifferentFrom" />. A combination that empties the pool fails eagerly with a
///     <see cref="ConflictingAnyConstraintException" />.
/// </summary>
public sealed class AnyChar : IAny<char>, IHasRandomSource, ICardinalityHint<char> {

    #region Statics members declarations

    internal static AnyChar Create(RandomSource source) {
        return new AnyChar(source, null, null, null, null, null, null, []);
    }

    private static string V(char value) {
        return $"'{value}'";
    }

    private static string Join(char[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<char>? _allowed;
    private readonly string?              _allowedConstraint;
    private readonly List<char>           _pool;
    private readonly LetterCasing?        _casing;
    private readonly string?              _casingConstraint;
    private readonly CharacterSet?        _charset;
    private readonly string?              _charsetConstraint;
    private readonly IReadOnlyList<char>  _excluded;
    private readonly RandomSource         _source;

    #endregion

    private AnyChar(RandomSource source,
                    CharacterSet? charset, string? charsetConstraint,
                    LetterCasing? casing,  string? casingConstraint,
                    IReadOnlyList<char>? allowed, string? allowedConstraint,
                    IReadOnlyList<char>  excluded) {
        _source            = source;
        _charset           = charset;
        _charsetConstraint = charsetConstraint;
        _casing            = casing;
        _casingConstraint  = casingConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        // Materialized once here — "constrain once, draw many": Generate never refilters the pool. The full
        // constant pool is the unconstrained start; MatchesCharset narrows it, so no per-charset pre-narrowing
        // is needed.
        IEnumerable<char> candidates = allowed ?? (IEnumerable<char>)(CharacterPools.UpperLetters + CharacterPools.LowerLetters + CharacterPools.Digits);
        _pool = candidates.Where(character => MatchesCharset(character) && MatchesCasing(character) && !excluded.Contains(character)).ToList();
    }

    RandomSource? IHasRandomSource.Source => _source;

    // The pool is materialized once at construction, so its size is the exact number of characters drawable.
    long? ICardinalityHint<char>.DistinctCardinality => _pool.Count;

    // The pool is the exact draw set, so membership is a direct pool lookup.
    bool ICardinalityHint<char>.Contains(char value) => _pool.Contains(value);

    /// <summary>Restricts the character to ASCII letters only. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar Alpha() {
        return WithCharset(CharacterSet.Alpha, "Alpha()");
    }

    /// <summary>Restricts the character to ASCII digits only. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar Numeric() {
        return WithCharset(CharacterSet.Numeric, "Numeric()");
    }

    /// <summary>Restricts the character to ASCII letters and digits only. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar AlphaNumeric() {
        return WithCharset(CharacterSet.AlphaNumeric, "AlphaNumeric()");
    }

    /// <summary>Requires an alphabetic character to be lowercase. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar LowerCase() {
        return WithCasing(LetterCasing.Lower, "LowerCase()");
    }

    /// <summary>Requires an alphabetic character to be uppercase. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar UpperCase() {
        return WithCasing(LetterCasing.Upper, "UpperCase()");
    }

    /// <summary>Requires the character to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed characters; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar OneOf(params char[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        string constraint = $"OneOf({Join(values)})";
        if (_allowedConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {constraint} because {_allowedConstraint} is already defined."); }

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, _casing, _casingConstraint, values.Distinct().ToArray(), constraint, _excluded), constraint);
    }

    /// <summary>Requires the character to be none of the supplied values.</summary>
    /// <param name="values">The forbidden characters.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar Except(params char[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return WithExcluded(values, $"Except({Join(values)})");
    }

    /// <summary>
    ///     Requires the character to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The character the generated character must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar DifferentFrom(char value) {
        return WithExcluded([value], $"DifferentFrom({V(value)})");
    }

    /// <inheritdoc />
    public char Generate() {
        return _pool[_source.Current.Random.Next(_pool.Count)];
    }

    private AnyChar WithCharset(CharacterSet charset, string applying) {
        if (_charsetConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_charsetConstraint} is already defined."); }

        return Validated(new AnyChar(_source, charset, applying, _casing, _casingConstraint, _allowed, _allowedConstraint, _excluded), applying);
    }

    private AnyChar WithCasing(LetterCasing casing, string applying) {
        if (_casingConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_casingConstraint} is already defined."); }

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, casing, applying, _allowed, _allowedConstraint, _excluded), applying);
    }

    private AnyChar WithExcluded(char[] values, string applying) {
        List<char> excluded = new(_excluded);
        excluded.AddRange(values);

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, _casing, _casingConstraint, _allowed, _allowedConstraint, excluded), applying);
    }

    private AnyChar Validated(AnyChar candidate, string applying) {
        if (candidate._pool.Count > 0) { return candidate; }

        string pool = candidate._allowedConstraint is null
                          ? "no character remains in the pool the declared constraints allow"
                          : $"no character {candidate._allowedConstraint} allows satisfies the constraints already defined";

        throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {pool}.");
    }

    private bool MatchesCharset(char character) {
        return _charset switch {
            CharacterSet.Alpha        => CharacterPools.IsAsciiLetter(character),
            CharacterSet.Numeric      => CharacterPools.IsAsciiDigit(character),
            CharacterSet.AlphaNumeric => CharacterPools.IsAsciiLetter(character) || CharacterPools.IsAsciiDigit(character),
            _                         => true
        };
    }

    private bool MatchesCasing(char character) {
        return _casing switch {
            LetterCasing.Lower => character is not (>= 'A' and <= 'Z'),
            LetterCasing.Upper => character is not (>= 'a' and <= 'z'),
            _                  => true
        };
    }

}
