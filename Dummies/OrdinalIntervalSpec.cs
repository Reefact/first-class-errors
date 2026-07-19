namespace Dummies;

/// <summary>
///     Order-preserving mappings between the discrete domains the generators expose and the unsigned 64-bit
///     <b>ordinal space</b> the shared interval engine works in. Every discrete type whose values fit 64 bits —
///     the integers, ticks-based time types, day numbers — maps onto <c>[0, 2^64-1]</c> so that one engine owns
///     bounds, exclusions, conflicts, and sampling for all of them.
/// </summary>
internal static class OrdinalMapping {

    private const ulong SignBit = 1UL << 63;

    /// <summary>Maps a signed 64-bit value to its ordinal: flips the sign bit, so ordering is preserved.</summary>
    internal static ulong FromInt64(long value) {
        return unchecked((ulong)value ^ SignBit);
    }

    /// <summary>Maps an ordinal back to the signed 64-bit value it came from.</summary>
    internal static long ToInt64(ulong ordinal) {
        return unchecked((long)(ordinal ^ SignBit));
    }

}

/// <summary>
///     The shared immutable engine behind every discrete interval-shaped generator (integers, <c>TimeSpan</c>,
///     <c>DateTime</c>, ...): an inclusive interval of <b>ordinals</b>, an optional allow-list (<c>OneOf</c>), and an
///     exclusion list — each bound remembering the constraint that set it, so a conflict message can name both
///     sides. Every mutation returns a new specification and validates satisfiability eagerly: a generator that
///     exists can always generate, in one draw, with no retry.
/// </summary>
/// <remarks>
///     The engine is domain-agnostic: each public generator supplies its type's display name (for "no Int64 value
///     satisfies it" messages), a renderer turning an ordinal back into a displayable value, and the ordinal bounds
///     of its domain. The conflict logic therefore lives once, and a fix to a message or an edge case reaches every
///     discrete type at the same time.
/// </remarks>
internal sealed class OrdinalIntervalSpec {

    #region Statics members declarations

    internal static OrdinalIntervalSpec Unconstrained(string typeName, Func<ulong, string> render, ulong domainMin, ulong domainMax) {
        return new OrdinalIntervalSpec(typeName, render, domainMin, domainMax,
                                       domainMin, null, domainMax, null, null, null, []);
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<ulong>? _allowed;
    private readonly string?               _allowedConstraint;
    private readonly ulong                 _domainMax;
    private readonly List<ulong>?          _effectiveAllowed;
    private readonly List<ulong>           _excludedInRange;
    private readonly ulong                 _domainMin;
    private readonly IReadOnlyList<ulong>  _excluded;
    private readonly ulong                 _max;
    private readonly string?               _maxConstraint;
    private readonly ulong                 _min;
    private readonly string?               _minConstraint;
    private readonly Func<ulong, string>   _render;
    private readonly string                _typeName;

    #endregion

    private OrdinalIntervalSpec(string typeName, Func<ulong, string> render, ulong domainMin, ulong domainMax,
                                ulong  min,     string? minConstraint,
                                ulong  max,     string? maxConstraint,
                                IReadOnlyList<ulong>? allowed, string? allowedConstraint,
                                IReadOnlyList<ulong>  excluded) {
        _typeName          = typeName;
        _render            = render;
        _domainMin         = domainMin;
        _domainMax         = domainMax;
        _min               = min;
        _minConstraint     = minConstraint;
        _max               = max;
        _maxConstraint     = maxConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        // Materialized once here — "constrain once, draw many": GenerateOrdinal never refilters or resorts.
        _excludedInRange = excluded.Where(value => value >= min && value <= max).Distinct().ToList();
        _excludedInRange.Sort();
        if (allowed is not null) {
            HashSet<ulong> forbidden = new(excluded);
            _effectiveAllowed = allowed.Where(value => value >= min && value <= max && !forbidden.Contains(value)).ToList();
        }
    }

    /// <summary>Tightens the lower bound; a looser bound than the current one is a no-op.</summary>
    internal OrdinalIntervalSpec WithMinimum(ulong minimum, string applying) {
        if (minimum <= _min) { return this; }

        if (minimum > _max) {
            if (_maxConstraint is null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_maxConstraint} already requires values less than or equal to {_render(_max)}.");
        }

        return Validated(new OrdinalIntervalSpec(_typeName, _render, _domainMin, _domainMax, minimum, applying, _max, _maxConstraint, _allowed, _allowedConstraint, _excluded), applying);
    }

    /// <summary>Tightens the lower bound to strictly above <paramref name="bound" /> — the exclusive form of <see cref="WithMinimum" />.</summary>
    internal OrdinalIntervalSpec WithMinimumAbove(ulong bound, string applying) {
        if (bound == _domainMax) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

        return WithMinimum(bound + 1, applying);
    }

    /// <summary>Tightens the upper bound; a looser bound than the current one is a no-op.</summary>
    internal OrdinalIntervalSpec WithMaximum(ulong maximum, string applying) {
        if (maximum >= _max) { return this; }

        if (maximum < _min) {
            if (_minConstraint is null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_minConstraint} already requires values greater than or equal to {_render(_min)}.");
        }

        return Validated(new OrdinalIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, maximum, applying, _allowed, _allowedConstraint, _excluded), applying);
    }

    /// <summary>Tightens the upper bound to strictly below <paramref name="bound" /> — the exclusive form of <see cref="WithMaximum" />.</summary>
    internal OrdinalIntervalSpec WithMaximumBelow(ulong bound, string applying) {
        if (bound == _domainMin) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

        return WithMaximum(bound - 1, applying);
    }

    /// <summary>Restricts the domain to an explicit allow-list; declared once per generator.</summary>
    internal OrdinalIntervalSpec WithAllowed(ulong[] ordinals, string applying) {
        if (_allowedConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_allowedConstraint} is already defined."); }

        ulong[] distinct = ordinals.Distinct().ToArray();

        return Validated(new OrdinalIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, _max, _maxConstraint, distinct, applying, _excluded), applying);
    }

    /// <summary>Adds values the generator must never produce.</summary>
    internal OrdinalIntervalSpec WithExcluded(ulong[] ordinals, string applying) {
        List<ulong> excluded = new(_excluded);
        excluded.AddRange(ordinals);

        return Validated(new OrdinalIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, excluded), applying);
    }

    /// <summary>
    ///     The number of distinct values the specification can produce, or <c>null</c> when that exceeds
    ///     <see cref="long.MaxValue" /> (a range too wide to ever conflict with a collection count). Feeds
    ///     <see cref="ICardinalityHint" />, so a distinct collection over a narrow integer range can fail eagerly.
    /// </summary>
    internal long? Cardinality {
        get {
            if (_effectiveAllowed is not null) { return _effectiveAllowed.Count; }
            if (IsFullWidth()) { return null; }

            ulong count = _max - _min + 1UL - (ulong)_excludedInRange.Count;

            return count <= long.MaxValue ? (long)count : null;
        }
    }

    /// <summary>
    ///     Whether <paramref name="ordinal" /> is a value the specification could produce — the exact domain
    ///     <see cref="GenerateOrdinal" /> draws from: a member of the allow-list when one is set, otherwise inside
    ///     the interval and not excluded. Feeds <see cref="IDomainMembership{T}" />, so a distinct collection can tell
    ///     a contained value that extends the domain from one already inside it.
    /// </summary>
    internal bool Contains(ulong ordinal) {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Contains(ordinal); }

        return ordinal >= _min && ordinal <= _max && !_excludedInRange.Contains(ordinal);
    }

    /// <summary>Draws one ordinal satisfying the whole specification — built directly, never generate-then-retry.</summary>
    internal ulong GenerateOrdinal(Random random) {
        if (_effectiveAllowed is not null) {
            return _effectiveAllowed[random.Next(_effectiveAllowed.Count)];
        }

        List<ulong> excluded = _excludedInRange;
        if (IsFullWidth()) {
            // The interval spans the whole ordinal space, so its size does not fit a ulong and the index
            // mapping below cannot run. Draw anywhere and, in the astronomically rare case the draw hits an
            // excluded value, walk to the next free ordinal — a deterministic, bounded step, not a retry loop.
            ulong candidate = random.NextUInt64();
            while (excluded.Contains(candidate)) { candidate = unchecked(candidate + 1UL); }

            return candidate;
        }

        ulong validCount       = _max - _min + 1 - (ulong)excluded.Count;
        ulong candidateOrdinal = _min + random.NextUInt64() % validCount;
        // Map the drawn index onto the k-th non-excluded ordinal of the interval: every excluded ordinal at
        // or below the candidate shifts it up by one. Sorted ascending, so a single pass suffices.
        foreach (ulong value in excluded) {
            if (candidateOrdinal >= value) { candidateOrdinal++; }
        }

        return candidateOrdinal;
    }

    private bool IsFullWidth() {
        return _min == ulong.MinValue && _max == ulong.MaxValue;
    }

    private OrdinalIntervalSpec Validated(OrdinalIntervalSpec candidate, string applying) {
        if (candidate.IsSatisfiable()) { return candidate; }

        throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {candidate.DescribeExhaustion()}.");
    }

    private bool IsSatisfiable() {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Count > 0; }
        if (IsFullWidth()) { return true; }

        return _max - _min + 1 - (ulong)_excludedInRange.Count > 0;
    }

    private string DescribeExhaustion() {
        if (_allowed is not null) {
            if (_excluded.Count > 0) {
                return $"no value {_allowedConstraint} allows remains available";
            }

            return $"none of the values {_allowedConstraint} allows satisfies the constraints already defined";
        }

        if (_min == _max) {
            string pinning = _minConstraint ?? _maxConstraint ?? "the declared bounds";

            return $"{pinning} already pins the value to {_render(_min)}";
        }

        return $"no value remains between {_render(_min)} and {_render(_max)} once the excluded values are removed";
    }

}
