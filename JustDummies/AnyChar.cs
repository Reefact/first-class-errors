namespace JustDummies;

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
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

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
    private readonly ConstraintCall?      _allowedConstraint;
    private readonly List<char>           _pool;
    private readonly LetterCasing?        _casing;
    private readonly ConstraintCall?      _casingConstraint;
    private readonly CharacterSet?        _charset;
    private readonly ConstraintCall?      _charsetConstraint;
    private readonly IReadOnlyList<char>  _excluded;
    private readonly RandomSource         _source;

    #endregion

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
                                                     Justification =
                                                         "This private constructor carries the engine's whole immutable state: the 'constrain once, draw many' design rebuilds the spec on " +
                                                         "every With* call, so every field has to be threaded through it. A parameter object would only rename the same list, and the " +
                                                         "constructor is private — no caller ever writes this argument list.")]
    private AnyChar(RandomSource source,
                    CharacterSet? charset, ConstraintCall? charsetConstraint,
                    LetterCasing? casing,  ConstraintCall? casingConstraint,
                    IReadOnlyList<char>? allowed, ConstraintCall? allowedConstraint,
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
        return WithCharset(CharacterSet.Alpha, ConstraintCall.Of(nameof(Alpha)));
    }

    /// <summary>Restricts the character to ASCII digits only. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar Numeric() {
        return WithCharset(CharacterSet.Numeric, ConstraintCall.Of(nameof(Numeric)));
    }

    /// <summary>Restricts the character to ASCII letters and digits only. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar AlphaNumeric() {
        return WithCharset(CharacterSet.AlphaNumeric, ConstraintCall.Of(nameof(AlphaNumeric)));
    }

    /// <summary>Requires an alphabetic character to be lowercase. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar LowerCase() {
        return WithCasing(LetterCasing.Lower, ConstraintCall.Of(nameof(LowerCase)));
    }

    /// <summary>Requires an alphabetic character to be uppercase. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar UpperCase() {
        return WithCasing(LetterCasing.Upper, ConstraintCall.Of(nameof(UpperCase)));
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

        ConstraintCall constraint = ConstraintCall.Of(nameof(OneOf), Join(values));
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_allowedConstraint == constraint) { return this; }
        if (_allowedConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(constraint, _allowedConstraint); }

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

        return WithExcluded(values, ConstraintCall.Of(nameof(Except), Join(values)));
    }

    /// <summary>
    ///     Requires the character to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The character the generated character must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar DifferentFrom(char value) {
        return WithExcluded([value], ConstraintCall.Of(nameof(DifferentFrom), V(value)));
    }

    /// <inheritdoc />
    public char Generate() {
        return _pool[_source.Current.Next(_pool.Count)];
    }

    private AnyChar WithCharset(CharacterSet charset, ConstraintCall applying) {
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_charsetConstraint == applying) { return this; }
        if (_charsetConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _charsetConstraint); }

        return Validated(new AnyChar(_source, charset, applying, _casing, _casingConstraint, _allowed, _allowedConstraint, _excluded), applying);
    }

    private AnyChar WithCasing(LetterCasing casing, ConstraintCall applying) {
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_casingConstraint == applying) { return this; }
        if (_casingConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _casingConstraint); }

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, casing, applying, _allowed, _allowedConstraint, _excluded), applying);
    }

    private AnyChar WithExcluded(char[] values, ConstraintCall applying) {
        List<char> excluded = [.. _excluded, .. values];

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, _casing, _casingConstraint, _allowed, _allowedConstraint, excluded), applying);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
                                                     Justification =
                                                         "Validated is the uniform validation hook of the fluent builders: every With* method routes its candidate through it, and all " +
                                                         "seven engines declare it with the same signature. It reads the CANDIDATE's state rather than this instance's — which is what " +
                                                         "the rule notices — but that is a builder validating its own successor, not an oversight. Making it static across seven types " +
                                                         "would break a family resemblance the reader relies on, for no measurable gain on a path that runs once per declared " +
                                                         "constraint.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2325:Methods and properties that do not access instance data should be static",
                                                     Justification =
                                                         "Validated is the uniform validation hook of the fluent builders: every With* method routes its candidate through it, and all " +
                                                         "seven engines declare it with the same signature. It reads the CANDIDATE's state rather than this instance's — which is what " +
                                                         "the rule notices — but that is a builder validating its own successor, not an oversight. Making it static across seven types " +
                                                         "would break a family resemblance the reader relies on, for no measurable gain on a path that runs once per declared " +
                                                         "constraint.")]
    private AnyChar Validated(AnyChar candidate, ConstraintCall applying) {
        if (candidate._pool.Count > 0) { return candidate; }

        string pool = candidate._allowedConstraint is null
                          ? "no character remains in the pool the declared constraints allow"
                          : $"no character {candidate._allowedConstraint} allows satisfies the constraints already defined";

        throw ConflictingAnyConstraintException.NoValueRemains(applying, pool);
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
