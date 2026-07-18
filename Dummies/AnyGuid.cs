namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="Guid" /> values, drawn from the seedable source — unlike
///     <see cref="System.Guid.NewGuid" />, a generated identifier is reproducible inside an
///     <c>Any.Reproducibly(...)</c> run. An unconstrained draw is, for every practical purpose, never
///     <see cref="System.Guid.Empty" />; chain <see cref="NonEmpty" /> to make that requirement explicit, or
///     <see cref="Empty" /> to pin the empty identifier. Contradictory constraints fail eagerly with a
///     <see cref="ConflictingAnyConstraintException" /> naming both sides.
/// </summary>
public sealed class AnyGuid : IAny<Guid>, IHasRandomSource {

    #region Statics members declarations

    /// <summary>
    ///     Generates the value — an <see cref="AnyGuid" /> can be used wherever a <see cref="Guid" /> is expected.
    ///     Each conversion draws a fresh value.
    /// </summary>
    /// <param name="generator">The generator to draw from.</param>
    /// <returns>An arbitrary value satisfying the generator's constraints.</returns>
    public static implicit operator Guid(AnyGuid generator) {
        return generator.Generate();
    }

    internal static AnyGuid Create(RandomSource source) {
        return new AnyGuid(source, null, null, null, null, []);
    }

    private static string V(Guid value) {
        return value.ToString("D");
    }

    private static string Join(Guid[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<Guid>? _allowed;
    private readonly string?              _allowedConstraint;
    private readonly List<Guid>?          _effectiveAllowed;
    private readonly IReadOnlyList<Guid>  _excluded;
    private readonly Guid?                _pinned;
    private readonly string?              _pinnedConstraint;
    private readonly RandomSource         _source;

    #endregion

    private AnyGuid(RandomSource source, Guid? pinned, string? pinnedConstraint,
                    IReadOnlyList<Guid>? allowed, string? allowedConstraint, IReadOnlyList<Guid> excluded) {
        _source            = source;
        _pinned            = pinned;
        _pinnedConstraint  = pinnedConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        // Materialized once here — "constrain once, draw many": Generate never refilters the allow-list.
        _effectiveAllowed  = allowed?.Where(value => !excluded.Contains(value)).ToList();
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Requires an identifier different from <see cref="System.Guid.Empty" />.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyGuid NonEmpty() {
        return WithExcluded([Guid.Empty], "NonEmpty()");
    }

    /// <summary>Pins the identifier to <see cref="System.Guid.Empty" />.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyGuid Empty() {
        return Validated(new AnyGuid(_source, Guid.Empty, "Empty()", _allowed, _allowedConstraint, _excluded), "Empty()");
    }

    /// <summary>Requires the identifier to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyGuid OneOf(params Guid[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        string constraint = $"OneOf({Join(values)})";
        if (_allowedConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {constraint} because {_allowedConstraint} is already defined."); }

        return Validated(new AnyGuid(_source, _pinned, _pinnedConstraint, values.Distinct().ToArray(), constraint, _excluded), constraint);
    }

    /// <summary>Requires the identifier to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyGuid Except(params Guid[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return WithExcluded(values, $"Except({Join(values)})");
    }

    /// <summary>
    ///     Requires the identifier to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated identifier must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyGuid DifferentFrom(Guid value) {
        return WithExcluded([value], $"DifferentFrom({V(value)})");
    }

    /// <inheritdoc />
    public Guid Generate() {
        if (_pinned is Guid pinned) { return pinned; }

        Random random = _source.Current.Random;
        if (_effectiveAllowed is not null) {
            return _effectiveAllowed[random.Next(_effectiveAllowed.Count)];
        }

        byte[] bytes = new byte[16];
        random.NextBytes(bytes);
        Guid candidate = new(bytes);
        // Colliding with an excluded identifier has probability ~2^-122 per draw; walking the last byte is a
        // deterministic, bounded escape — not a retry loop.
        while (_excluded.Contains(candidate)) {
            bytes[15]++;
            candidate = new Guid(bytes);
        }

        return candidate;
    }

    private AnyGuid WithExcluded(Guid[] values, string applying) {
        List<Guid> excluded = new(_excluded);
        excluded.AddRange(values);

        return Validated(new AnyGuid(_source, _pinned, _pinnedConstraint, _allowed, _allowedConstraint, excluded), applying);
    }

    private AnyGuid Validated(AnyGuid candidate, string applying) {
        if (candidate._pinned is Guid pinned) {
            if (candidate._excluded.Contains(pinned)) {
                throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {candidate._pinnedConstraint} already pins the value to {V(pinned)}, which the exclusions forbid.");
            }
            if (candidate._allowed is not null && !candidate._allowed.Contains(pinned)) {
                throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {candidate._pinnedConstraint} already pins the value to {V(pinned)}, which {candidate._allowedConstraint} does not allow.");
            }

            return candidate;
        }

        if (candidate._effectiveAllowed is not null && candidate._effectiveAllowed.Count == 0) {
            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no value {candidate._allowedConstraint} allows remains available.");
        }

        return candidate;
    }

}
