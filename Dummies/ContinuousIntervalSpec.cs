namespace Dummies;

/// <summary>
///     The shared immutable engine behind the binary floating-point generators (<see cref="AnyDouble" />,
///     <see cref="AnySingle" />, and <c>Half</c> on modern targets): an inclusive interval of finite doubles, an
///     optional allow-list, and point exclusions — each bound remembering the constraint that set it, so a conflict
///     message can name both sides. NaN and the infinities are never generated nor accepted: arbitrary test values
///     should cross invariants, not sabotage arithmetic.
/// </summary>
/// <remarks>
///     <para>
///         Narrower value types ride the double engine through a <c>quantize</c> step (for example
///         <c>double → float</c>): bounds are supplied already-representable in the narrow type, sampling happens in
///         double, and the drawn value is quantized then clamped back into the bounds.
///     </para>
///     <para>
///         Excluding a point from a continuum can only collide with a draw on a set of measure zero, but the engine
///         still guarantees the constraint: a colliding draw is nudged to the neighbouring representable value, a
///         bounded deterministic walk — not a retry loop. When the walk cannot stay within the bounds the generation
///         fails with an <see cref="AnyGenerationException" /> naming the seed.
///     </para>
/// </remarks>
internal sealed class ContinuousIntervalSpec {

    private const int NudgeBudget = 128;

    #region Statics members declarations

    internal static ContinuousIntervalSpec Unconstrained(string typeName, Func<double, string> render, Func<double, double> quantize, Func<double, double> nextUp, double domainMin, double domainMax) {
        return new ContinuousIntervalSpec(typeName, render, quantize, nextUp, domainMin, null, domainMax, null, null, null, []);
    }

    /// <summary>Rejects NaN and the infinities — the shared argument guard of every floating-point generator.</summary>
    internal static void EnsureFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) { throw new ArgumentException("The value must be finite: NaN and infinities are never generated.", parameterName); }
    }

    /// <summary>The next representable double above <paramref name="value" /> — the exclusive-bound arithmetic.</summary>
    internal static double NextUp(double value) {
        long bits = BitConverter.DoubleToInt64Bits(value);
        if (bits >= 0L) { bits++; } else if (bits == long.MinValue) { bits = 1L; } else { bits--; }

        return BitConverter.Int64BitsToDouble(bits);
    }

    /// <summary>The next representable double below <paramref name="value" />.</summary>
    internal static double NextDown(double value) {
        return -NextUp(-value);
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<double>? _allowed;
    private readonly string?                _allowedConstraint;
    private readonly List<double>?          _effectiveAllowed;
    private readonly IReadOnlyList<double>  _excluded;
    private readonly Func<double, double>   _nextUp;
    private readonly double                 _max;
    private readonly string?                _maxConstraint;
    private readonly double                 _min;
    private readonly string?                _minConstraint;
    private readonly Func<double, double>   _quantize;
    private readonly Func<double, string>   _render;
    private readonly string                 _typeName;

    #endregion

    private ContinuousIntervalSpec(string  typeName, Func<double, string> render, Func<double, double> quantize, Func<double, double> nextUp,
                                   double  min,      string? minConstraint,
                                   double  max,      string? maxConstraint,
                                   IReadOnlyList<double>? allowed, string? allowedConstraint,
                                   IReadOnlyList<double>  excluded) {
        _typeName          = typeName;
        _render            = render;
        _quantize          = quantize;
        _nextUp            = nextUp;
        _min               = min;
        _minConstraint     = minConstraint;
        _max               = max;
        _maxConstraint     = maxConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        // Materialized once here — "constrain once, draw many": Generate never refilters the allow-list.
        _effectiveAllowed  = allowed?.Where(value => value >= min && value <= max && !IsExcluded(value)).ToList();
    }

    /// <summary>Tightens the lower bound; a looser bound than the current one is a no-op.</summary>
    internal ContinuousIntervalSpec WithMinimum(double minimum, string applying) {
        if (double.IsInfinity(minimum)) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }
        if (minimum <= _min) { return this; }

        if (minimum > _max) {
            if (_maxConstraint is null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_maxConstraint} already requires values less than or equal to {_render(_max)}.");
        }

        return Validated(new ContinuousIntervalSpec(_typeName, _render, _quantize, _nextUp, minimum, applying, _max, _maxConstraint, _allowed, _allowedConstraint, _excluded), applying);
    }

    /// <summary>Tightens the upper bound; a looser bound than the current one is a no-op.</summary>
    internal ContinuousIntervalSpec WithMaximum(double maximum, string applying) {
        if (double.IsInfinity(maximum)) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }
        if (maximum >= _max) { return this; }

        if (maximum < _min) {
            if (_minConstraint is null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_minConstraint} already requires values greater than or equal to {_render(_min)}.");
        }

        return Validated(new ContinuousIntervalSpec(_typeName, _render, _quantize, _nextUp, _min, _minConstraint, maximum, applying, _allowed, _allowedConstraint, _excluded), applying);
    }

    /// <summary>Tightens the lower bound to strictly above <paramref name="bound" /> — via the type's next representable value.</summary>
    internal ContinuousIntervalSpec WithMinimumAbove(double bound, string applying) {
        return WithMinimum(_nextUp(bound), applying);
    }

    /// <summary>Tightens the upper bound to strictly below <paramref name="bound" /> — via the type's next representable value.</summary>
    internal ContinuousIntervalSpec WithMaximumBelow(double bound, string applying) {
        return WithMaximum(-_nextUp(-bound), applying);
    }

    /// <summary>Restricts the domain to an explicit allow-list; declared once per generator.</summary>
    internal ContinuousIntervalSpec WithAllowed(double[] values, string applying) {
        if (_allowedConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_allowedConstraint} is already defined."); }

        double[] distinct = values.Distinct().ToArray();

        return Validated(new ContinuousIntervalSpec(_typeName, _render, _quantize, _nextUp, _min, _minConstraint, _max, _maxConstraint, distinct, applying, _excluded), applying);
    }

    /// <summary>Adds values the generator must never produce.</summary>
    internal ContinuousIntervalSpec WithExcluded(double[] values, string applying) {
        List<double> excluded = new(_excluded);
        excluded.AddRange(values);

        return Validated(new ContinuousIntervalSpec(_typeName, _render, _quantize, _nextUp, _min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, excluded), applying);
    }

    /// <summary>
    ///     The number of distinct values the specification can produce — the allow-list size when one is set, and
    ///     <c>null</c> otherwise: a floating-point continuum is not a countable domain, so it stays outside the eager
    ///     cardinality perimeter and a distinct collection over it falls back to the bounded draw. Feeds
    ///     <see cref="ICardinalityHint{T}" />.
    /// </summary>
    internal long? Cardinality => _effectiveAllowed?.Count;

    /// <summary>
    ///     Whether <paramref name="value" /> is one the specification could produce — a member of the allow-list when
    ///     one is set, otherwise inside the interval and not excluded. Non-finite inputs fall outside the bounds and
    ///     so return <c>false</c>. Mirrors <see cref="Generate" />'s own domain.
    /// </summary>
    internal bool Contains(double value) {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Contains(value); }

        return value >= _min && value <= _max && !IsExcluded(value);
    }

    /// <summary>Draws one value satisfying the whole specification.</summary>
    internal double Generate(Random random, int seed) {
        if (_effectiveAllowed is not null) {
            return _effectiveAllowed[random.Next(_effectiveAllowed.Count)];
        }

        if (_min == _max) { return _min; }

        // Sample around the midpoint so the span (max - min) never overflows to infinity on wide ranges.
        double mid       = _min / 2 + _max / 2;
        double half      = _max / 2 - _min / 2;
        double candidate = Quantized(mid + (2 * random.NextDouble() - 1) * half);

        // A draw colliding with an excluded point (a measure-zero event) is walked to the neighbouring
        // representable value — deterministic and bounded, not a retry loop.
        int budget = NudgeBudget;
        while (IsExcluded(candidate)) {
            double next = Quantized(NextUp(candidate));
            if (next > _max || budget-- == 0) {
                throw new AnyGenerationException(
                    $"Generation failed: no {_typeName} value near the drawn candidate satisfies the exclusions. The arbitrary values were seeded with {seed}; reproduce this run with Any.Reproducibly({seed}, ...).",
                    seed,
                    new InvalidOperationException("The exclusion nudge walked out of the allowed range."));
            }

            candidate = next;
        }

        return candidate;
    }

    private double Quantized(double value) {
        double quantized = _quantize(value);
        if (quantized < _min) { return _min; }
        if (quantized > _max) { return _max; }

        return quantized;
    }

    private bool IsExcluded(double value) {
        foreach (double excluded in _excluded) {
            if (value.Equals(excluded)) { return true; }
        }

        return false;
    }

    private ContinuousIntervalSpec Validated(ContinuousIntervalSpec candidate, string applying) {
        if (candidate.IsSatisfiable()) { return candidate; }

        throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {candidate.DescribeExhaustion()}.");
    }

    private bool IsSatisfiable() {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Count > 0; }
        if (_min < _max) { return true; }

        return !IsExcluded(_min);
    }

    private string DescribeExhaustion() {
        if (_allowed is not null) {
            if (_excluded.Count > 0) {
                return $"no value {_allowedConstraint} allows remains available";
            }

            return $"none of the values {_allowedConstraint} allows satisfies the constraints already defined";
        }

        string pinning = _minConstraint ?? _maxConstraint ?? "the declared bounds";

        return $"{pinning} already pins the value to {_render(_min)}, which the exclusions forbid";
    }

}
