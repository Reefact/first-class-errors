namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="Guid" /> values, drawn from the seedable source — unlike
///     <see cref="Guid.NewGuid" />, a generated identifier is reproducible inside an
///     <c>Any.Reproducibly(...)</c> run. An unconstrained draw is, for every practical purpose, never
///     <see cref="Guid.Empty" />; chain <see cref="NonEmpty" /> to make that requirement explicit, or
///     <see cref="Empty" /> to pin the empty identifier. Contradictory constraints fail eagerly with a
///     <see cref="ConflictingAnyConstraintException" /> naming both sides.
/// </summary>
public sealed class AnyGuid : IAny<Guid>, IHasRandomSource, ICardinalityHint<Guid> {

    #region Statics members declarations

    internal static AnyGuid Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new AnyGuid(source, null, null, null, null, []);
    }

    private static string V(Guid value) {
        return value.ToString("D");
    }

    private static string Join(Guid[] values) {
        return string.Join(", ", values.Select(V));
    }

    // Increments the 16-byte buffer by one with carry, from the last byte down — the full-width successor of
    // new Guid(bytes). Incrementing only the last byte (the former behaviour) wraps 255 back to 0 and can cycle
    // forever when every last-byte variant of a prefix is excluded; propagating the carry into the higher bytes
    // cannot, because it walks distinct values across the whole 128-bit space.
    private static void Increment(byte[] bytes) {
        for (int i = bytes.Length - 1; i >= 0; i--) {
            if (++bytes[i] != 0) { return; }
        }
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<Guid>? _allowed;
    private readonly ConstraintCall?      _allowedConstraint;
    private readonly List<Guid>?          _effectiveAllowed;
    private readonly IReadOnlyList<Guid>  _excluded;
    private readonly HashSet<Guid>        _excludedSet;
    private readonly Guid?                _pinned;
    private readonly ConstraintCall?      _pinnedConstraint;
    private readonly RandomSource         _source;

    #endregion

    private AnyGuid(RandomSource source, Guid? pinned, ConstraintCall? pinnedConstraint,
                    IReadOnlyList<Guid>? allowed, ConstraintCall? allowedConstraint, IReadOnlyList<Guid> excluded) {
        _source            = source;
        _pinned            = pinned;
        _pinnedConstraint  = pinnedConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        // Materialized once here — "constrain once, draw many": Generate never refilters the allow-list, and
        // the exclusion walk tests membership against a set rather than rescanning the list on every step.
        _excludedSet       = [.. excluded];
        _effectiveAllowed  = allowed?.Where(value => !_excludedSet.Contains(value)).ToList();
    }

    RandomSource? IHasRandomSource.Source => _source;

    // Pinned to a single value, or bounded by an allow-list; otherwise the domain is effectively unbounded.
    long? ICardinalityHint<Guid>.DistinctCardinality => _pinned is not null ? 1 : _effectiveAllowed?.Count;

    // Mirrors Generate: the pin, then the allow-list, then the full space minus the exclusions.
    bool ICardinalityHint<Guid>.Contains(Guid value) {
        if (_pinned is Guid pinned) { return pinned == value; }
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Contains(value); }

        return !_excluded.Contains(value);
    }

    /// <summary>Requires an identifier different from <see cref="Guid.Empty" />.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyGuid NonEmpty() {
        return WithExcluded([Guid.Empty], ConstraintCall.Of(nameof(NonEmpty)));
    }

    /// <summary>Pins the identifier to <see cref="Guid.Empty" />.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyGuid Empty() {
        return Validated(new AnyGuid(_source, Guid.Empty, ConstraintCall.Of(nameof(Empty)), _allowed, _allowedConstraint, _excluded), ConstraintCall.Of(nameof(Empty)));
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

        ConstraintCall constraint = ConstraintCall.Of(nameof(OneOf), Join(values));
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_allowedConstraint == constraint) { return this; }
        if (_allowedConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(constraint, _allowedConstraint); }

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

        return WithExcluded(values, ConstraintCall.Of(nameof(Except), Join(values)));
    }

    /// <summary>
    ///     Requires the identifier to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated identifier must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyGuid DifferentFrom(Guid value) {
        return WithExcluded([value], ConstraintCall.Of(nameof(DifferentFrom), V(value)));
    }

    /// <inheritdoc />
    public Guid Generate() {
        if (_pinned is Guid pinned) { return pinned; }

        SeededRandom random = _source.Current;
        if (_effectiveAllowed is not null) {
            return _effectiveAllowed[random.Next(_effectiveAllowed.Count)];
        }

        byte[] bytes = new byte[16];
        random.NextBytes(bytes);
        Guid candidate = new(bytes);
        // Colliding with an excluded identifier has probability |excluded| / 2^128 per draw. On a collision,
        // walk the whole 128-bit value with carry — the full-width successor of the drawn bytes — off the
        // excluded values. The exclusion set can never fill the 128-bit space, so the walk visits distinct
        // values until it lands on an allowed one and terminates: the same deterministic escape
        // OrdinalIntervalSpec and WideIntervalSpec already use for their 128-bit siblings.
        while (_excludedSet.Contains(candidate)) {
            Increment(bytes);
            candidate = new(bytes);
        }

        return candidate;
    }

    private AnyGuid WithExcluded(Guid[] values, ConstraintCall applying) {
        List<Guid> excluded = [.. _excluded, .. values];

        return Validated(new AnyGuid(_source, _pinned, _pinnedConstraint, _allowed, _allowedConstraint, excluded), applying);
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
    private AnyGuid Validated(AnyGuid candidate, ConstraintCall applying) {
        if (candidate._pinned is Guid pinned) {
            if (candidate._excluded.Contains(pinned)) {
                throw ConflictingAnyConstraintException.PinnedValueExcluded(applying, candidate._pinnedConstraint!, V(pinned));
            }
            if (candidate._allowed is not null && !candidate._allowed.Contains(pinned)) {
                throw ConflictingAnyConstraintException.PinnedValueNotAllowed(applying, candidate._pinnedConstraint!, V(pinned), candidate._allowedConstraint!);
            }

            return candidate;
        }

        if (candidate._effectiveAllowed is not null && candidate._effectiveAllowed.Count == 0) {
            throw ConflictingAnyConstraintException.NoValueRemains(applying, $"no value {candidate._allowedConstraint} allows remains available");
        }

        return candidate;
    }

}
