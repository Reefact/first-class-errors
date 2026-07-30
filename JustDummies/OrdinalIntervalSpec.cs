namespace JustDummies;

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
///     <c>DateTime</c>, ...): an inclusive interval of <b>ordinals</b>, an optional allow-list (<c>OneOf</c>), an
///     exclusion list, and an optional <b>lattice</b> (a step and an anchor, set by <c>MultipleOf</c> / a temporal
///     granularity) restricting the domain to values spaced a fixed distance apart — each bound remembering the
///     constraint that set it, so a conflict message can name both sides. Every mutation returns a new specification
///     and validates satisfiability eagerly: a generator that exists can always generate, in one draw, with no retry.
/// </summary>
/// <remarks>
///     The engine is domain-agnostic: each public generator supplies its type's display name (for "no Int64 value
///     satisfies it" messages), a renderer turning an ordinal back into a displayable value, and the ordinal bounds
///     of its domain. The conflict logic therefore lives once, and a fix to a message or an edge case reaches every
///     discrete type at the same time.
///     <para>
///         The lattice works because the ordinal map is affine: consecutive multiples of a step in value space stay a
///         constant step apart in ordinal space. The valid ordinals are therefore an arithmetic progression through a
///         known lattice ordinal (the anchor — the ordinal of the value <c>0</c>, itself a multiple of every step),
///         found by striding from the first lattice point at or above the minimum. Sampling stays inside the drawn
///         window, so the wraparound at the ordinal-space edge is never crossed.
///     </para>
/// </remarks>
internal sealed class OrdinalIntervalSpec {

    #region Statics members declarations

    internal static OrdinalIntervalSpec Unconstrained(string typeName, Func<ulong, string> render, ulong domainMin, ulong domainMax) {
        if (typeName is null) { throw new ArgumentNullException(nameof(typeName)); }
        if (render is null) { throw new ArgumentNullException(nameof(render)); }

        return new OrdinalIntervalSpec(typeName, render, domainMin, domainMax,
                                       domainMin, null, domainMax, null, null, null, [],
                                       1UL, 0UL, null);
    }

    /// <summary>Whether <paramref name="ordinal" /> sits on the lattice anchored at <paramref name="anchor" /> with the given step.</summary>
    private static bool IsOnLattice(ulong ordinal, ulong anchor, ulong step) {
        ulong delta = ordinal >= anchor ? ordinal - anchor : anchor - ordinal;

        return delta % step == 0UL;
    }

    /// <summary>
    ///     The smallest lattice ordinal at or above <paramref name="min" />, staying within <c>[min, max]</c>. Returns
    ///     <c>false</c> when none exists (the stride steps past <paramref name="max" />, or the domain top overflows).
    /// </summary>
    private static bool TryFirstLatticePoint(ulong min, ulong max, ulong anchor, ulong step, out ulong first) {
        if (min >= anchor) {
            ulong ahead = (min - anchor) % step;
            if (ahead == 0UL) {
                first = min;
            } else {
                first = min + (step - ahead);
                if (first < min) { first = 0UL; return false; } // wrapped past the top of the ordinal domain
            }
        } else {
            // The nearest lattice point at or above min is min plus its distance up to the anchor's phase.
            first = min + (anchor - min) % step;
        }

        return first <= max;
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<ulong>? _allowed;
    private readonly string?               _allowedConstraint;
    private readonly ulong                 _anchor;
    private readonly ulong                 _domainMax;
    private readonly ulong                 _domainMin;
    private readonly List<ulong>?          _effectiveAllowed;
    private readonly IReadOnlyList<(string Constraint, ulong[] Ordinals)> _exclusions;
    private readonly List<ulong>           _excludedInRange;
    private readonly List<ulong>           _excludedOnLattice;
    private readonly ulong                 _latticeFirst;
    private readonly bool                  _latticeHasPoint;
    private readonly ulong                 _max;
    private readonly string?               _maxConstraint;
    private readonly ulong                 _min;
    private readonly string?               _minConstraint;
    private readonly Func<ulong, string>   _render;
    private readonly ulong                 _step;
    private readonly string?               _stepConstraint;
    private readonly string                _typeName;

    #endregion

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
                                                     Justification =
                                                         "This private constructor carries the engine's whole immutable state: the 'constrain once, draw many' design rebuilds the spec on " +
                                                         "every With* call, so every field has to be threaded through it. A parameter object would only rename the same list, and the " +
                                                         "constructor is private — no caller ever writes this argument list.")]
    private OrdinalIntervalSpec(string typeName, Func<ulong, string> render, ulong domainMin, ulong domainMax,
                                ulong  min,     string? minConstraint,
                                ulong  max,     string? maxConstraint,
                                IReadOnlyList<ulong>? allowed, string? allowedConstraint,
                                IReadOnlyList<(string Constraint, ulong[] Ordinals)> exclusions,
                                ulong  step,    ulong anchor, string? stepConstraint) {
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
        _exclusions        = exclusions;
        _step              = step;
        _anchor            = anchor;
        _stepConstraint    = stepConstraint;
        // The flat ordinal set drives every hot-path decision; the provenance in _exclusions is consulted only
        // when a conflict message must name the excluding constraint. Materialized once here — "constrain once,
        // draw many": GenerateOrdinal never refilters or resorts.
        ulong[] excluded = exclusions.SelectMany(pair => pair.Ordinals).ToArray();
        _excludedInRange = excluded.Where(value => value >= min && value <= max).Distinct().ToList();
        _excludedInRange.Sort();
        // Lattice-derived state, kept alongside so the hot path is a straight index-and-stride.
        if (step > 1UL) {
            _latticeHasPoint   = TryFirstLatticePoint(min, max, anchor, step, out _latticeFirst);
            _excludedOnLattice = _excludedInRange.Where(value => IsOnLattice(value, anchor, step)).ToList(); // stays sorted: filtered from a sorted list
        } else {
            _latticeHasPoint   = true;
            _latticeFirst      = min;
            _excludedOnLattice = _excludedInRange;
        }

        if (allowed is not null) {
            HashSet<ulong> forbidden = [.. excluded];
            _effectiveAllowed = allowed.Where(value => value >= min && value <= max && !forbidden.Contains(value) && (step <= 1UL || IsOnLattice(value, anchor, step))).ToList();
        }
    }

    /// <summary>Tightens the lower bound; a looser bound than the current one is a no-op.</summary>
    internal OrdinalIntervalSpec WithMinimum(ulong minimum, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (minimum <= _min) { return this; }

        if (minimum > _max) {
            if (_maxConstraint is null) { throw ConflictingAnyConstraintException.NoValueSatisfies(applying, _typeName); }

            throw ConflictingAnyConstraintException.AlreadyBoundedAbove(applying, _maxConstraint, _render(_max));
        }

        return Validated(new OrdinalIntervalSpec(_typeName, _render, _domainMin, _domainMax, minimum, applying, _max, _maxConstraint, _allowed, _allowedConstraint, _exclusions, _step, _anchor, _stepConstraint), applying);
    }

    /// <summary>Tightens the lower bound to strictly above <paramref name="bound" /> — the exclusive form of <see cref="WithMinimum" />.</summary>
    internal OrdinalIntervalSpec WithMinimumAbove(ulong bound, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (bound == _domainMax) { throw ConflictingAnyConstraintException.NoValueSatisfies(applying, _typeName); }

        return WithMinimum(bound + 1, applying);
    }

    /// <summary>Tightens the upper bound; a looser bound than the current one is a no-op.</summary>
    internal OrdinalIntervalSpec WithMaximum(ulong maximum, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (maximum >= _max) { return this; }

        if (maximum < _min) {
            if (_minConstraint is null) { throw ConflictingAnyConstraintException.NoValueSatisfies(applying, _typeName); }

            throw ConflictingAnyConstraintException.AlreadyBoundedBelow(applying, _minConstraint, _render(_min));
        }

        return Validated(new OrdinalIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, maximum, applying, _allowed, _allowedConstraint, _exclusions, _step, _anchor, _stepConstraint), applying);
    }

    /// <summary>Tightens the upper bound to strictly below <paramref name="bound" /> — the exclusive form of <see cref="WithMaximum" />.</summary>
    internal OrdinalIntervalSpec WithMaximumBelow(ulong bound, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (bound == _domainMin) { throw ConflictingAnyConstraintException.NoValueSatisfies(applying, _typeName); }

        return WithMaximum(bound - 1, applying);
    }

    /// <summary>Restricts the domain to an explicit allow-list; declared once per generator.</summary>
    internal OrdinalIntervalSpec WithAllowed(ulong[] ordinals, string applying) {
        if (ordinals is null) { throw new ArgumentNullException(nameof(ordinals)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (string.Equals(_allowedConstraint, applying, StringComparison.Ordinal)) { return this; }
        if (_allowedConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _allowedConstraint); }

        ulong[] distinct = ordinals.Distinct().ToArray();

        return Validated(new OrdinalIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, _max, _maxConstraint, distinct, applying, _exclusions, _step, _anchor, _stepConstraint), applying);
    }

    /// <summary>
    ///     Narrows an allow-list already in force to a subset of itself, keeping the constraint that declared it.
    ///     This is not a second declaration — the caller is removing values another constraint forbids — so it does
    ///     not trip the declared-once guard, and the original provenance stays the one a later conflict names.
    /// </summary>
    internal OrdinalIntervalSpec NarrowingAllowed(ulong[] kept, string applying) {
        if (kept is null) { throw new ArgumentNullException(nameof(kept)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        ulong[] distinct = kept.Distinct().ToArray();

        return Validated(new OrdinalIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, _max, _maxConstraint, distinct, _allowedConstraint ?? applying, _exclusions, _step, _anchor, _stepConstraint), applying);
    }

    /// <summary>Adds values the generator must never produce.</summary>
    internal OrdinalIntervalSpec WithExcluded(ulong[] ordinals, string applying) {
        if (ordinals is null) { throw new ArgumentNullException(nameof(ordinals)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        // The applied constraint tags its own ordinals, so a later exhaustion message can name the exclusion
        // that actually emptied the domain rather than a bound that merely happens to border it.
        List<(string Constraint, ulong[] Ordinals)> exclusions = [.. _exclusions, (applying, ordinals)];

        return Validated(new OrdinalIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, exclusions, _step, _anchor, _stepConstraint), applying);
    }

    /// <summary>
    ///     Restricts the domain to a lattice: values a multiple of <paramref name="step" /> away from
    ///     <paramref name="anchor" /> — a known lattice ordinal, the ordinal of the value <c>0</c>. Declared once per
    ///     generator (a second, different lattice conflicts rather than silently intersecting).
    /// </summary>
    internal OrdinalIntervalSpec WithStep(ulong step, ulong anchor, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (step <= 1UL) { return this; } // every value is a multiple of one: a no-op, not a constraint

        if (_step > 1UL) {
            if (_step == step && _anchor == anchor) { return this; }

            // _step and _stepConstraint are written as a pair by the constructor and rethreaded as a pair by every
            // rebuild, so a declared step always carries the name of the constraint that declared it.
            throw ConflictingAnyConstraintException.AlreadyDefined(applying, _stepConstraint!);
        }

        return Validated(new OrdinalIntervalSpec(_typeName, _render, _domainMin, _domainMax, _min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, _exclusions, step, anchor, applying), applying);
    }

    /// <summary>
    ///     The number of distinct values the specification can produce, or <c>null</c> when that exceeds
    ///     <see cref="long.MaxValue" /> (a range too wide to ever conflict with a collection count). Feeds
    ///     <see cref="ICardinalityHint{T}" />, so a distinct collection over a narrow integer range can fail eagerly.
    /// </summary>
    internal long? Cardinality {
        get {
            if (_effectiveAllowed is not null) { return _effectiveAllowed.Count; }
            if (_step > 1UL) {
                if (!_latticeHasPoint) { return 0; }

                ulong onLattice = (_max - _latticeFirst) / _step + 1UL - (ulong)_excludedOnLattice.Count;

                return onLattice <= long.MaxValue ? (long)onLattice : null;
            }
            if (IsFullWidth()) { return null; }

            ulong count = _max - _min + 1UL - (ulong)_excludedInRange.Count;

            return count <= long.MaxValue ? (long)count : null;
        }
    }

    /// <summary>
    ///     Whether <paramref name="ordinal" /> is a value the specification could produce — the exact domain
    ///     <see cref="GenerateOrdinal" /> draws from: a member of the allow-list when one is set, otherwise on the
    ///     lattice (when one is set), inside the interval and not excluded. Feeds <see cref="ICardinalityHint{T}" />,
    ///     so a distinct collection can tell a contained value that extends the domain from one already inside it.
    /// </summary>
    internal bool Contains(ulong ordinal) {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Contains(ordinal); }
        if (_step > 1UL && !IsOnLattice(ordinal, _anchor, _step)) { return false; }

        return ordinal >= _min && ordinal <= _max && !_excludedInRange.Contains(ordinal);
    }

    /// <summary>Draws one ordinal satisfying the whole specification — built directly, never generate-then-retry.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with LINQ expressions",
                                                     Justification =
                                                         "The loop body advances the very accumulator the condition tests — each iteration changes what the next one compares against — so " +
                                                         "the filter cannot be lifted out of the loop. A Where clause would evaluate every predicate against the value the accumulator " +
                                                         "held on entry and silently skip exclusions.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with LINQ expressions",
                                                     Justification =
                                                         "The loop body advances the very accumulator the condition tests — each iteration changes what the next one compares against — so " +
                                                         "the filter cannot be lifted out of the loop. A Where clause would evaluate every predicate against the value the accumulator " +
                                                         "held on entry and silently skip exclusions.")]
    internal ulong GenerateOrdinal(SeededRandom random) {
        if (random is null) { throw new ArgumentNullException(nameof(random)); }

        if (_effectiveAllowed is not null) {
            return _effectiveAllowed[random.Next(_effectiveAllowed.Count)];
        }

        if (_step > 1UL) {
            // The lattice caps the count below 2^64 (a step of two already halves the domain), so the
            // full-width special case below never applies here. Draw an index over the surviving lattice
            // points, then shift past any excluded lattice point at or below the drawn ordinal.
            ulong latticeCount = (_max - _latticeFirst) / _step + 1UL;
            ulong validCount   = latticeCount - (ulong)_excludedOnLattice.Count;
            ulong ordinal      = _latticeFirst + (random.NextUInt64() % validCount) * _step;
            foreach (ulong value in _excludedOnLattice) {
                if (ordinal >= value) { ordinal += _step; }
            }

            return ordinal;
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

        ulong validCountInRange = _max - _min + 1 - (ulong)excluded.Count;
        ulong candidateOrdinal  = _min + random.NextUInt64() % validCountInRange;
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
    private OrdinalIntervalSpec Validated(OrdinalIntervalSpec candidate, string applying) {
        if (candidate.IsSatisfiable()) { return candidate; }

        throw ConflictingAnyConstraintException.NoValueRemains(applying, candidate.DescribeExhaustion(applying));
    }

    private bool IsSatisfiable() {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Count > 0; }
        if (_step > 1UL) {
            if (!_latticeHasPoint) { return false; }

            return (_max - _latticeFirst) / _step + 1UL > (ulong)_excludedOnLattice.Count;
        }
        if (IsFullWidth()) { return true; }

        return _max - _min + 1 - (ulong)_excludedInRange.Count > 0;
    }

    private string DescribeExhaustion(string applying) {
        IReadOnlyList<string> culprits = ExcludingConstraintsInEffect();

        if (_allowed is not null) {
            if (culprits.Count == 0) { return $"none of the values {_allowedConstraint} allows satisfies the constraints already defined"; }

            // Only the allow-list values the bounds and lattice still permit can be forbidden by an exclusion; if
            // some allowed value was already dropped by a bound or the lattice, the exclusions do not forbid
            // "every" allowed value, so the claim is qualified rather than overstated.
            string allowed = _allowed.All(WouldAllowIgnoringExclusions)
                                 ? $"every value {_allowedConstraint} allows"
                                 : $"every value {_allowedConstraint} allows that the other constraints leave";

            return $"{Forbids(culprits, applying)} {allowed}";
        }

        if (_step > 1UL) {
            if (!_latticeHasPoint || culprits.Count == 0) { return $"no {_typeName} value {_stepConstraint} allows remains between {_render(_min)} and {_render(_max)}"; }

            return $"{Forbids(culprits, applying)} every {_stepConstraint} value between {_render(_min)} and {_render(_max)}";
        }

        if (_min == _max) {
            if (culprits.Count == 0) {
                string pinning = _minConstraint ?? _maxConstraint ?? "the declared bounds";

                return $"{pinning} already pins the value to {_render(_min)}";
            }

            return $"{Forbids(culprits, applying)} {_render(_min)}, {PinningClause()}";
        }

        if (culprits.Count == 0) { return $"no value remains between {_render(_min)} and {_render(_max)} once the excluded values are removed"; }

        return $"{Forbids(culprits, applying)} every value between {_render(_min)} and {_render(_max)}";
    }

    /// <summary>
    ///     The distinct exclusion constraints that actually caused the exhaustion — those forbidding at least one
    ///     value the interval, lattice and allow-list would otherwise permit. An exclusion whose values fall outside
    ///     the surviving domain never bit, so naming it would mislead; first-declared order is preserved.
    /// </summary>
    private IReadOnlyList<string> ExcludingConstraintsInEffect() {
        List<string> names = [];
        foreach ((string constraint, ulong[] ordinals) in _exclusions) {
            if (names.Contains(constraint)) { continue; }
            if (ordinals.Any(WouldAllowIgnoringExclusions)) { names.Add(constraint); }
        }

        return names;
    }

    /// <summary>Whether <paramref name="ordinal" /> would be in the domain if no exclusion were applied.</summary>
    private bool WouldAllowIgnoringExclusions(ulong ordinal) {
        if (_allowed is not null && !_allowed.Contains(ordinal)) { return false; }
        if (_step > 1UL && !IsOnLattice(ordinal, _anchor, _step)) { return false; }

        return ordinal >= _min && ordinal <= _max;
    }

    /// <summary>
    ///     The subject of the exhaustion clause. A single culprit that is the constraint being applied becomes "it",
    ///     so the message reads "Cannot apply Except(1) because it forbids …" rather than repeating the constraint on
    ///     both sides of "because".
    /// </summary>
    private static string Forbids(IReadOnlyList<string> names, string applying) {
        if (names.Count == 1) { return names[0] == applying ? "it forbids" : $"{names[0]} forbids"; }

        return $"{string.Join(", ", names)} forbid";
    }

    /// <summary>Names the bounds that pinned the domain to its single value, for the "forbids X, the only value ... leaves" form.</summary>
    private string PinningClause() {
        List<string> bounds = [];
        if (_minConstraint is not null) { bounds.Add(_minConstraint); }
        if (_maxConstraint is not null && _maxConstraint != _minConstraint) { bounds.Add(_maxConstraint); }

        if (bounds.Count == 0) { return "the only value the declared bounds leave"; }
        if (bounds.Count == 1) { return $"the only value {bounds[0]} leaves"; }

        return $"the only value {string.Join(" and ", bounds)} leave";
    }

}
