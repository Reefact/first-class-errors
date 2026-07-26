#if NET8_0_OR_GREATER
namespace Dummies;

/// <summary>
///     The 128-bit sibling of <see cref="OrdinalIntervalSpec" />, backing <see cref="AnyInt128" /> and
///     <see cref="AnyUInt128" />: their ordinal space exceeds 64 bits, so the same algebra — descriptor-tracked
///     inclusive bounds, allow-list, exclusions, an optional lattice (<c>MultipleOf</c>), eager conflicts, one-draw
///     generation — runs over <see cref="UInt128" /> ordinals. Net8-only, like the types it serves.
/// </summary>
internal sealed class WideIntervalSpec {

    #region Statics members declarations

    internal static WideIntervalSpec Unconstrained(string typeName, Func<UInt128, string> render, UInt128 domainMin, UInt128 domainMax) {
        return new WideIntervalSpec(typeName, render, domainMin, domainMax, domainMin, null, domainMax, null, null, null, [], UInt128.One, UInt128.Zero, null);
    }

    private static UInt128 NextUInt128(Random random) {
        return new UInt128(random.NextUInt64(), random.NextUInt64());
    }

    /// <summary>Whether <paramref name="ordinal" /> sits on the lattice anchored at <paramref name="anchor" /> with the given step.</summary>
    private static bool IsOnLattice(UInt128 ordinal, UInt128 anchor, UInt128 step) {
        UInt128 delta = ordinal >= anchor ? ordinal - anchor : anchor - ordinal;

        return delta % step == UInt128.Zero;
    }

    /// <summary>
    ///     The smallest lattice ordinal at or above <paramref name="min" />, staying within <c>[min, max]</c>. Returns
    ///     <c>false</c> when none exists (the stride steps past <paramref name="max" />, or the domain top overflows).
    /// </summary>
    private static bool TryFirstLatticePoint(UInt128 min, UInt128 max, UInt128 anchor, UInt128 step, out UInt128 first) {
        if (min >= anchor) {
            UInt128 ahead = (min - anchor) % step;
            if (ahead == UInt128.Zero) {
                first = min;
            } else {
                first = min + (step - ahead);
                if (first < min) { first = UInt128.Zero; return false; } // wrapped past the top of the ordinal domain
            }
        } else {
            first = min + (anchor - min) % step;
        }

        return first <= max;
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<UInt128>? _allowed;
    private readonly string?                 _allowedConstraint;
    private readonly UInt128                 _anchor;
    private readonly UInt128                 _domainMax;
    private readonly List<UInt128>?          _effectiveAllowed;
    private readonly List<UInt128>           _excludedInRange;
    private readonly List<UInt128>           _excludedOnLattice;
    private readonly UInt128                 _domainMin;
    private readonly IReadOnlyList<UInt128>  _excluded;
    private readonly UInt128                 _latticeFirst;
    private readonly bool                    _latticeHasPoint;
    private readonly UInt128                 _max;
    private readonly string?                 _maxConstraint;
    private readonly UInt128                 _min;
    private readonly string?                 _minConstraint;
    private readonly Func<UInt128, string>   _render;
    private readonly UInt128                 _step;
    private readonly string?                 _stepConstraint;
    private readonly string                  _typeName;

    #endregion

    private WideIntervalSpec(string typeName, Func<UInt128, string> render, UInt128 domainMin, UInt128 domainMax,
                             UInt128 min, string? minConstraint,
                             UInt128 max, string? maxConstraint,
                             IReadOnlyList<UInt128>? allowed, string? allowedConstraint,
                             IReadOnlyList<UInt128> excluded,
                             UInt128 step, UInt128 anchor, string? stepConstraint) {
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
        _step              = step;
        _anchor            = anchor;
        _stepConstraint    = stepConstraint;
        // Materialized once here — "constrain once, draw many": GenerateOrdinal never refilters or resorts.
        _excludedInRange = excluded.Where(value => value >= min && value <= max).Distinct().ToList();
        _excludedInRange.Sort();
        if (step > UInt128.One) {
            _latticeHasPoint   = TryFirstLatticePoint(min, max, anchor, step, out _latticeFirst);
            _excludedOnLattice = _excludedInRange.Where(value => IsOnLattice(value, anchor, step)).ToList(); // stays sorted: filtered from a sorted list
        } else {
            _latticeHasPoint   = true;
            _latticeFirst      = min;
            _excludedOnLattice = _excludedInRange;
        }

        if (allowed is not null) {
            HashSet<UInt128> forbidden = new(excluded);
            _effectiveAllowed = allowed.Where(value => value >= min && value <= max && !forbidden.Contains(value) && (step <= UInt128.One || IsOnLattice(value, anchor, step))).ToList();
        }
    }

    /// <summary>Tightens the lower bound; a looser bound than the current one is a no-op.</summary>
    internal WideIntervalSpec WithMinimum(UInt128 minimum, string applying) {
        if (minimum <= _min) { return this; }

        if (minimum > _max) {
            if (_maxConstraint is null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_maxConstraint} already requires values less than or equal to {_render(_max)}.");
        }

        return Validated(new WideIntervalSpec(_typeName, _render, _domainMin, _domainMax, minimum, applying, _max, _maxConstraint, _allowed, _allowedConstraint, _excluded, _step, _anchor, _stepConstraint), applying);
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

        return Validated(new WideIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, maximum, applying, _allowed, _allowedConstraint, _excluded, _step, _anchor, _stepConstraint), applying);
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

        return Validated(new WideIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, _max, _maxConstraint, distinct, applying, _excluded, _step, _anchor, _stepConstraint), applying);
    }

    /// <summary>Adds values the generator must never produce.</summary>
    internal WideIntervalSpec WithExcluded(UInt128[] ordinals, string applying) {
        List<UInt128> excluded = new(_excluded);
        excluded.AddRange(ordinals);

        return Validated(new WideIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, excluded, _step, _anchor, _stepConstraint), applying);
    }

    /// <summary>
    ///     Restricts the domain to a lattice: values a multiple of <paramref name="step" /> away from
    ///     <paramref name="anchor" /> — a known lattice ordinal, the ordinal of the value <c>0</c>. Declared once per
    ///     generator.
    /// </summary>
    internal WideIntervalSpec WithStep(UInt128 step, UInt128 anchor, string applying) {
        if (step <= UInt128.One) { return this; } // every value is a multiple of one: a no-op, not a constraint

        if (_step > UInt128.One) {
            if (_step == step && _anchor == anchor) { return this; }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_stepConstraint} is already defined.");
        }

        return Validated(new WideIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, _excluded, step, anchor, applying), applying);
    }

    /// <summary>
    ///     The number of distinct values the specification can produce, or <c>null</c> when the interval is full-width
    ///     or wider than <see cref="long.MaxValue" /> (a range too vast to ever conflict with a collection count).
    ///     Feeds <see cref="ICardinalityHint{T}" />, so a distinct collection over a narrow 128-bit range or allow-list
    ///     can fail eagerly.
    /// </summary>
    internal long? Cardinality {
        get {
            if (_effectiveAllowed is not null) { return _effectiveAllowed.Count; }
            if (_step > UInt128.One) {
                if (!_latticeHasPoint) { return 0; }

                UInt128 onLattice = (_max - _latticeFirst) / _step + 1 - (UInt128)_excludedOnLattice.Count;

                return onLattice <= (UInt128)long.MaxValue ? (long)onLattice : null;
            }
            if (IsFullWidth()) { return null; }

            UInt128 count = _max - _min + 1 - (UInt128)_excludedInRange.Count;

            return count <= (UInt128)long.MaxValue ? (long)count : null;
        }
    }

    /// <summary>
    ///     Whether <paramref name="ordinal" /> is a value the specification could produce — the exact domain
    ///     <see cref="GenerateOrdinal" /> draws from. Feeds <see cref="ICardinalityHint{T}" />.
    /// </summary>
    internal bool Contains(UInt128 ordinal) {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Contains(ordinal); }
        if (_step > UInt128.One && !IsOnLattice(ordinal, _anchor, _step)) { return false; }

        return ordinal >= _min && ordinal <= _max && !_excludedInRange.Contains(ordinal);
    }

    /// <summary>Draws one ordinal satisfying the whole specification — built directly, never generate-then-retry.</summary>
    internal UInt128 GenerateOrdinal(Random random) {
        if (_effectiveAllowed is not null) {
            return _effectiveAllowed[random.Next(_effectiveAllowed.Count)];
        }

        if (_step > UInt128.One) {
            // The lattice caps the count below the full 128-bit width, so the full-width special case never
            // applies here. Draw an index over the surviving lattice points, then shift past any excluded
            // lattice point at or below the drawn ordinal.
            UInt128 latticeCount = (_max - _latticeFirst) / _step + 1;
            UInt128 validCount   = latticeCount - (UInt128)_excludedOnLattice.Count;
            UInt128 ordinal      = _latticeFirst + NextUInt128(random) % validCount * _step;
            foreach (UInt128 value in _excludedOnLattice) {
                if (ordinal >= value) { ordinal += _step; }
            }

            return ordinal;
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
        if (_step > UInt128.One) {
            if (!_latticeHasPoint) { return false; }

            return (_max - _latticeFirst) / _step + 1 > (UInt128)_excludedOnLattice.Count;
        }
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

        if (_step > UInt128.One) {
            return $"no {_typeName} value {_stepConstraint} allows remains between {_render(_min)} and {_render(_max)}";
        }

        if (_min == _max) {
            string pinning = _minConstraint ?? _maxConstraint ?? "the declared bounds";

            return $"{pinning} already pins the value to {_render(_min)}";
        }

        return $"no value remains between {_render(_min)} and {_render(_max)} once the excluded values are removed";
    }

}
#endif
