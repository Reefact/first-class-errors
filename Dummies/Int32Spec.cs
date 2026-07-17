#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     The immutable specification behind <see cref="AnyInt32" />: an inclusive interval, an optional allow-list
///     (<c>OneOf</c>), and an exclusion list — each bound remembering the constraint that set it, so a conflict
///     message can name both sides. Every mutation returns a new specification and validates satisfiability eagerly:
///     an <see cref="AnyInt32" /> that exists can always generate.
/// </summary>
internal sealed class Int32Spec {

    #region Statics members declarations

    internal static readonly Int32Spec Unconstrained = new(int.MinValue, null, int.MaxValue, null, null, null, []);

    private static string V(long value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<int>? _allowed;
    private readonly string?             _allowedConstraint;
    private readonly IReadOnlyList<int>  _excluded;
    private readonly long                _max;
    private readonly string?             _maxConstraint;
    private readonly long                _min;
    private readonly string?             _minConstraint;

    #endregion

    private Int32Spec(long                min,     string? minConstraint, long max, string? maxConstraint,
                      IReadOnlyList<int>? allowed, string? allowedConstraint, IReadOnlyList<int> excluded) {
        _min               = min;
        _minConstraint     = minConstraint;
        _max               = max;
        _maxConstraint     = maxConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
    }

    /// <summary>Tightens the lower bound; a looser bound than the current one is a no-op.</summary>
    internal Int32Spec WithMinimum(long minimum, string applying) {
        if (minimum > int.MaxValue) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no Int32 value satisfies it."); }
        if (minimum <= _min) { return this; }

        if (minimum > _max) {
            if (_maxConstraint is null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no Int32 value satisfies it."); }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_maxConstraint} already requires values less than or equal to {V(_max)}.");
        }

        return Validated(new Int32Spec(minimum, applying, _max, _maxConstraint, _allowed, _allowedConstraint, _excluded), applying);
    }

    /// <summary>Tightens the upper bound; a looser bound than the current one is a no-op.</summary>
    internal Int32Spec WithMaximum(long maximum, string applying) {
        if (maximum < int.MinValue) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no Int32 value satisfies it."); }
        if (maximum >= _max) { return this; }

        if (maximum < _min) {
            if (_minConstraint is null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because no Int32 value satisfies it."); }

            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_minConstraint} already requires values greater than or equal to {V(_min)}.");
        }

        return Validated(new Int32Spec(_min, _minConstraint, maximum, applying, _allowed, _allowedConstraint, _excluded), applying);
    }

    /// <summary>Restricts the domain to an explicit allow-list; declared once per generator.</summary>
    internal Int32Spec WithAllowed(int[] values, string applying) {
        if (_allowedConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_allowedConstraint} is already defined."); }

        int[] distinct = values.Distinct().ToArray();

        return Validated(new Int32Spec(_min, _minConstraint, _max, _maxConstraint, distinct, applying, _excluded), applying);
    }

    /// <summary>Adds values the generator must never produce.</summary>
    internal Int32Spec WithExcluded(int[] values, string applying) {
        List<int> excluded = new(_excluded);
        excluded.AddRange(values);

        return Validated(new Int32Spec(_min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, excluded), applying);
    }

    /// <summary>Draws one value satisfying the whole specification — built directly, never generate-then-retry.</summary>
    internal int Generate(Random random) {
        if (_allowed is not null) {
            List<int> pool = EffectiveAllowed();

            return pool[random.Next(pool.Count)];
        }

        List<int> excluded   = ExcludedInRangeSorted();
        long      validCount = (_max - _min + 1) - excluded.Count;
        long      candidate  = _min + random.NextInt64(0, validCount - 1);
        // Map the drawn index onto the k-th non-excluded value of the interval: every excluded value at or
        // below the candidate shifts it up by one. Sorted ascending, so a single pass suffices.
        foreach (int value in excluded) {
            if (candidate >= value) { candidate++; }
        }

        return (int)candidate;
    }

    private static Int32Spec Validated(Int32Spec candidate, string applying) {
        if (candidate.CountCandidates() > 0) { return candidate; }

        throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {candidate.DescribeExhaustion()}.");
    }

    private long CountCandidates() {
        if (_allowed is not null) { return EffectiveAllowed().Count; }

        return (_max - _min + 1) - ExcludedInRangeSorted().Count;
    }

    private List<int> EffectiveAllowed() {
        HashSet<int> excluded = new(_excluded);

        return _allowed!.Where(value => value >= _min && value <= _max && !excluded.Contains(value)).ToList();
    }

    private List<int> ExcludedInRangeSorted() {
        List<int> excluded = _excluded.Where(value => value >= _min && value <= _max).Distinct().ToList();
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

            return $"{pinning} already pins the value to {V(_min)}";
        }

        return $"no value remains between {V(_min)} and {V(_max)} once the excluded values are removed";
    }

}
