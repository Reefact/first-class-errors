#if NET8_0_OR_GREATER
namespace Dummies;

/// <summary>
///     The 128-bit sibling of <see cref="OrdinalIntervalSpec" />, backing <see cref="AnyInt128" /> and
///     <see cref="AnyUInt128" />: their ordinal space exceeds 64 bits, so the same algebra — descriptor-tracked
///     inclusive bounds, allow-list, exclusions, eager conflicts, one-draw generation — runs over
///     <see cref="UInt128" /> ordinals. Net8-only, like the types it serves.
/// </summary>
internal sealed class WideIntervalSpec {

    #region Statics members declarations

    internal static WideIntervalSpec Unconstrained(string typeName, Func<UInt128, string> render, UInt128 domainMin, UInt128 domainMax) {
        return new WideIntervalSpec(typeName, render, domainMin, domainMax, domainMin, null, domainMax, null, null, null, []);
    }

    private static UInt128 NextUInt128(Random random) {
        return new UInt128(random.NextUInt64(), random.NextUInt64());
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<UInt128>? _allowed;
    private readonly string?                 _allowedConstraint;
    private readonly UInt128                 _domainMax;
    private readonly List<UInt128>?          _effectiveAllowed;
    private readonly List<UInt128>           _excludedInRange;
    private readonly UInt128                 _domainMin;
    private readonly IReadOnlyList<UInt128>  _excluded;
    private readonly UInt128                 _max;
    private readonly string?                 _maxConstraint;
    private readonly UInt128                 _min;
    private readonly string?                 _minConstraint;
    private readonly Func<UInt128, string>   _render;
    private readonly string                  _typeName;

    #endregion

    private WideIntervalSpec(string typeName, Func<UInt128, string> render, UInt128 domainMin, UInt128 domainMax,
                             UInt128 min, string? minConstraint,
                             UInt128 max, string? maxConstraint,
                             IReadOnlyList<UInt128>? allowed, string? allowedConstraint,
                             IReadOnlyList<UInt128> excluded) {
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
            HashSet<UInt128> forbidden = new(excluded);
            _effectiveAllowed = allowed.Where(value => value >= min && value <= max && !forbidden.Contains(value)).ToList();
        }
    }

    /// <summary>Tightens the lower bound; a looser bound than the current one is a no-op.</summary>
    internal WideIntervalSpec WithMinimum(UInt128 minimum, string applying) {
        if (minimum <= _min) { return this; }

        if (minimum > _max) {
            if (_maxConstraint is null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_maxConstraint} already requires values less than or equal to {_render(_max)}.");
        }

        return Validated(new WideIntervalSpec(_typeName, _render, _domainMin, _domainMax, minimum, applying, _max, _maxConstraint, _allowed, _allowedConstraint, _excluded), applying);
    }

    /// <summary>Tightens the lower bound to strictly above <paramref name="bound" /> — the exclusive form of <see cref="WithMinimum" />.</summary>
    internal WideIntervalSpec WithMinimumAbove(UInt128 bound, string applying) {
        if (bound == _domainMax) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

        return WithMinimum(bound + 1, applying);
    }

    /// <summary>Tightens the upper bound; a looser bound than the current one is a no-op.</summary>
    internal WideIntervalSpec WithMaximum(UInt128 maximum, string applying) {
        if (maximum >= _max) { return this; }

        if (maximum < _min) {
            if (_minConstraint is null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_minConstraint} already requires values greater than or equal to {_render(_min)}.");
        }

        return Validated(new WideIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, maximum, applying, _allowed, _allowedConstraint, _excluded), applying);
    }

    /// <summary>Tightens the upper bound to strictly below <paramref name="bound" /> — the exclusive form of <see cref="WithMaximum" />.</summary>
    internal WideIntervalSpec WithMaximumBelow(UInt128 bound, string applying) {
        if (bound == _domainMin) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

        return WithMaximum(bound - 1, applying);
    }

    /// <summary>Restricts the domain to an explicit allow-list; declared once per generator.</summary>
    internal WideIntervalSpec WithAllowed(UInt128[] ordinals, string applying) {
        if (_allowedConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_allowedConstraint} is already defined."); }

        UInt128[] distinct = ordinals.Distinct().ToArray();

        return Validated(new WideIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, _max, _maxConstraint, distinct, applying, _excluded), applying);
    }

    /// <summary>Adds values the generator must never produce.</summary>
    internal WideIntervalSpec WithExcluded(UInt128[] ordinals, string applying) {
        List<UInt128> excluded = new(_excluded);
        excluded.AddRange(ordinals);

        return Validated(new WideIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, excluded), applying);
    }

    /// <summary>Draws one ordinal satisfying the whole specification — built directly, never generate-then-retry.</summary>
    internal UInt128 GenerateOrdinal(Random random) {
        if (_effectiveAllowed is not null) {
            return _effectiveAllowed[random.Next(_effectiveAllowed.Count)];
        }

        List<UInt128> excluded = _excludedInRange;
        if (IsFullWidth()) {
            // Same escape as OrdinalIntervalSpec: the full 128-bit space has no representable size, so draw
            // anywhere and walk off an excluded value deterministically.
            UInt128 candidate = NextUInt128(random);
            while (excluded.Contains(candidate)) { candidate = unchecked(candidate + 1); }

            return candidate;
        }

        UInt128 size             = _max - _min + 1 - (UInt128)excluded.Count;
        UInt128 candidateOrdinal = _min + NextUInt128(random) % size;
        foreach (UInt128 value in excluded) {
            if (candidateOrdinal >= value) { candidateOrdinal++; }
        }

        return candidateOrdinal;
    }

    private bool IsFullWidth() {
        return _min == UInt128.MinValue && _max == UInt128.MaxValue;
    }

    private WideIntervalSpec Validated(WideIntervalSpec candidate, string applying) {
        if (candidate.IsSatisfiable()) { return candidate; }

        throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {candidate.DescribeExhaustion()}.");
    }

    private bool IsSatisfiable() {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Count > 0; }
        if (IsFullWidth()) { return true; }

        return _max - _min + 1 - (UInt128)_excludedInRange.Count > 0;
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
#endif
