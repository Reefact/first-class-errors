namespace Dummies;

/// <summary>
///     The immutable engine behind <see cref="AnyDecimal" /> — the same algebra as
///     <see cref="ContinuousIntervalSpec" /> in <see cref="decimal" /> arithmetic. <see cref="decimal" /> has no
///     next-representable-value ladder, so exclusive bounds are expressed as an inclusive bound plus a point
///     exclusion, and a colliding draw is nudged by the smallest decimal increment within a bounded budget.
/// </summary>
internal sealed class DecimalIntervalSpec {

    private const int NudgeBudget = 128;

    private static readonly decimal SmallestStep = 0.0000000000000000000000000001m;
    private static readonly decimal MaxFraction  = 7.9228162514264337593543950335m;

    #region Statics members declarations

    internal static DecimalIntervalSpec Unconstrained(string typeName, Func<decimal, string> render) {
        return new DecimalIntervalSpec(typeName, render, decimal.MinValue, null, decimal.MaxValue, null, null, null, []);
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<decimal>? _allowed;
    private readonly string?                 _allowedConstraint;
    private readonly IReadOnlyList<decimal>  _excluded;
    private readonly decimal                 _max;
    private readonly string?                 _maxConstraint;
    private readonly decimal                 _min;
    private readonly string?                 _minConstraint;
    private readonly Func<decimal, string>   _render;
    private readonly string                  _typeName;

    #endregion

    private DecimalIntervalSpec(string  typeName, Func<decimal, string> render,
                                decimal min,      string? minConstraint,
                                decimal max,      string? maxConstraint,
                                IReadOnlyList<decimal>? allowed, string? allowedConstraint,
                                IReadOnlyList<decimal>  excluded) {
        _typeName          = typeName;
        _render            = render;
        _min               = min;
        _minConstraint     = minConstraint;
        _max               = max;
        _maxConstraint     = maxConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
    }

    /// <summary>Tightens the lower bound; a looser bound than the current one is a no-op.</summary>
    internal DecimalIntervalSpec WithMinimum(decimal minimum, string applying) {
        if (minimum <= _min) { return this; }

        if (minimum > _max) {
            if (_maxConstraint is null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_maxConstraint} already requires values less than or equal to {_render(_max)}.");
        }

        return Validated(new DecimalIntervalSpec(_typeName, _render, minimum, applying, _max, _maxConstraint, _allowed, _allowedConstraint, _excluded), applying);
    }

    /// <summary>Tightens the upper bound; a looser bound than the current one is a no-op.</summary>
    internal DecimalIntervalSpec WithMaximum(decimal maximum, string applying) {
        if (maximum >= _max) { return this; }

        if (maximum < _min) {
            if (_minConstraint is null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no {_typeName} value satisfies it."); }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_minConstraint} already requires values greater than or equal to {_render(_min)}.");
        }

        return Validated(new DecimalIntervalSpec(_typeName, _render, _min, _minConstraint, maximum, applying, _allowed, _allowedConstraint, _excluded), applying);
    }

    /// <summary>Restricts the domain to an explicit allow-list; declared once per generator.</summary>
    internal DecimalIntervalSpec WithAllowed(decimal[] values, string applying) {
        if (_allowedConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_allowedConstraint} is already defined."); }

        decimal[] distinct = values.Distinct().ToArray();

        return Validated(new DecimalIntervalSpec(_typeName, _render, _min, _minConstraint, _max, _maxConstraint, distinct, applying, _excluded), applying);
    }

    /// <summary>Adds values the generator must never produce.</summary>
    internal DecimalIntervalSpec WithExcluded(decimal[] values, string applying) {
        List<decimal> excluded = new(_excluded);
        excluded.AddRange(values);

        return Validated(new DecimalIntervalSpec(_typeName, _render, _min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, excluded), applying);
    }

    /// <summary>Draws one value satisfying the whole specification.</summary>
    internal decimal Generate(Random random, int seed) {
        if (_allowed is not null) {
            List<decimal> pool = EffectiveAllowed();

            return pool[random.Next(pool.Count)];
        }

        if (_min == _max) { return _min; }

        // A uniform-enough fraction in [0, 1): 93 random bits over the full decimal mantissa scale.
        decimal fraction = new decimal(random.Next(), random.Next(), random.Next(), false, 28) / MaxFraction;
        // Sample around the midpoint so the span (max - min) never overflows on wide ranges.
        decimal mid       = _min / 2 + _max / 2;
        decimal half      = _max / 2 - _min / 2;
        decimal candidate = Clamped(mid + (fraction * 2 - 1) * half);

        // A draw colliding with an excluded point is walked by the smallest decimal step — deterministic and
        // bounded, not a retry loop. (At extreme magnitudes the step can vanish in rounding; the budget then
        // fails the generation loudly instead of looping.)
        int budget = NudgeBudget;
        while (IsExcluded(candidate)) {
            decimal next = Clamped(candidate + SmallestStep);
            if (next == candidate || budget-- == 0) {
                throw new AnyGenerationException(
                    $"Generation failed: no {_typeName} value near the drawn candidate satisfies the exclusions. The arbitrary values were seeded with {seed}; reproduce this run with Any.Reproducibly({seed}, ...).",
                    seed,
                    new InvalidOperationException("The exclusion nudge could not leave the excluded point within the allowed range."));
            }

            candidate = next;
        }

        return candidate;
    }

    private decimal Clamped(decimal value) {
        if (value < _min) { return _min; }
        if (value > _max) { return _max; }

        return value;
    }

    private bool IsExcluded(decimal value) {
        foreach (decimal excluded in _excluded) {
            if (value == excluded) { return true; }
        }

        return false;
    }

    private DecimalIntervalSpec Validated(DecimalIntervalSpec candidate, string applying) {
        if (candidate.IsSatisfiable()) { return candidate; }

        throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {candidate.DescribeExhaustion()}.");
    }

    private bool IsSatisfiable() {
        if (_allowed is not null) { return EffectiveAllowed().Count > 0; }
        if (_min < _max) { return true; }

        return !IsExcluded(_min);
    }

    private List<decimal> EffectiveAllowed() {
        return _allowed!.Where(value => value >= _min && value <= _max && !IsExcluded(value)).ToList();
    }

    private string DescribeExhaustion() {
        if (_allowed is not null) {
            if (_excluded.Count > 0 && _allowed.All(value => IsExcluded(value) || value < _min || value > _max)) {
                return $"no value {_allowedConstraint} allows remains available";
            }

            return $"none of the values {_allowedConstraint} allows satisfies the constraints already defined";
        }

        string pinning = _minConstraint ?? _maxConstraint ?? "the declared bounds";

        return $"{pinning} already pins the value to {_render(_min)}, which the exclusions forbid";
    }

}
