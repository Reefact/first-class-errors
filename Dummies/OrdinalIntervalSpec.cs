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

    /// <summary>Draws one ordinal satisfying the whole specification — built directly, never generate-then-retry.</summary>
    internal ulong GenerateOrdinal(Random random) {
        if (_allowed is not null) {
            List<ulong> pool = EffectiveAllowed();

            return pool[random.Next(pool.Count)];
        }

        List<ulong> excluded = ExcludedInRangeSorted();
        if (IsFullWidth()) {
            // The interval spans the whole ordinal space, so its size does not fit a ulong and the index
            // mapping below cannot run. Draw anywhere and, in the astronomically rare case the draw hits an
            // excluded value, walk to the next free ordinal — a deterministic, bounded step, not a retry loop.
            ulong candidate = random.NextUInt64();
            while (excluded.Contains(candidate)) { candidate = unchecked(candidate + 1UL); }

            return candidate;
        }

        ulong validCount = _max - _min + 1 - (ulong)excluded.Count;
        ulong candidateOrdinal = _min + random.NextUInt64(0, validCount - 1);
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
        if (_allowed is not null) { return EffectiveAllowed().Count > 0; }
        if (IsFullWidth()) { return true; }

        return _max - _min + 1 - (ulong)ExcludedInRangeSorted().Count > 0;
    }

    private List<ulong> EffectiveAllowed() {
        HashSet<ulong> excluded = new(_excluded);

        return _allowed!.Where(value => value >= _min && value <= _max && !excluded.Contains(value)).ToList();
    }

    private List<ulong> ExcludedInRangeSorted() {
        List<ulong> excluded = _excluded.Where(value => value >= _min && value <= _max).Distinct().ToList();
        excluded.Sort();

        return excluded;
    }

    private string DescribeExhaustion() {
        if (_allowed is not null) {
            if (_excluded.Count > 0 && _allowed.All(value => _excluded.Contains(value) || value < _min || value > _max)) {
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
