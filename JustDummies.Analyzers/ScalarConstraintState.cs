using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     The integer domain a scalar chain has narrowed to, rebuilt constraint by constraint in declaration order.
///     Both <c>JD023</c> (the domain became empty) and <c>JD024</c> (a constraint narrowed nothing) read it: one asks
///     whether anything remains, the other whether anything changed.
/// </summary>
/// <remarks>
///     Integers only, and only where every argument folds to a constant. A chain carrying one unfoldable argument
///     stops being tracked rather than being guessed at: a rule that claims a chain is unsatisfiable must be certain.
///     Bounds are kept in <see cref="long" /> so a constraint at <see cref="int.MinValue" /> cannot overflow the
///     arithmetic that tests them.
/// </remarks>
internal sealed class ScalarConstraintState {

    /// <summary>
    ///     How many values the emptiness checks will walk before giving up and answering "not empty". It bounds both
    ///     walks below — the range and the lattice — so the rule stays cheap on a chain over a huge domain, at the
    ///     price of a deliberate false negative on one no realistic exclusion set could empty anyway.
    /// </summary>
    private const long MaxWalkLength = 64;

    private ScalarConstraintState(long minimum, long maximum, long? multipleOf, HashSet<long>? allowed, HashSet<long> excluded, bool saturated = false) {
        Minimum    = minimum;
        Maximum    = maximum;
        MultipleOf = multipleOf;
        Allowed    = allowed;
        Excluded   = excluded;
        Saturated  = saturated;
    }

    public long           Minimum    { get; }
    public long           Maximum    { get; }
    public long?          MultipleOf { get; }
    public HashSet<long>? Allowed    { get; }
    public HashSet<long>  Excluded   { get; }

    /// <summary>
    ///     Set when a bound asked for values beyond the representable range — <c>GreaterThan(long.MaxValue)</c>. The
    ///     domain is empty, and saying so needs a flag rather than an out-of-range bound, because the bounds run to
    ///     the extremes: <c>LessThanOrEqualTo(long.MinValue)</c> is a legal chain that yields exactly one value.
    /// </summary>
    public bool Saturated { get; }

    public static ScalarConstraintState Unconstrained() {
        return new ScalarConstraintState(long.MinValue, long.MaxValue, null, null, []);
    }

    /// <summary>Whether no value at all survives the constraints declared so far.</summary>
    public bool IsEmpty() {
        if (Saturated) { return true; }
        if (Allowed is not null) { return !Allowed.Any(Admits); }
        if (Minimum > Maximum) { return true; }

        // A small finite range can be emptied by its exclusions alone, with the bounds still consistent:
        // Zero().NonZero() pins [0, 0] and then forbids the only value in it.
        if (Excluded.Count > 0 && FitsInAWalk()) {
            for (long value = Minimum; value <= Maximum; value++) {
                if (Admits(value)) { return false; }
            }

            return true;
        }

        return MultipleOf is long step && !HasMultipleInRange(step);
    }

    // Only walk a range small enough to enumerate, and far enough from the extremes that the arithmetic cannot
    // overflow. A wider range is never declared empty by exclusions: no realistic exclusion set could empty it.
    private bool FitsInAWalk() {
        return Minimum > long.MinValue / 2 && Maximum < long.MaxValue / 2 && Maximum - Minimum < MaxWalkLength;
    }

    /// <summary>Whether <paramref name="value" /> survives every constraint declared so far.</summary>
    public bool Admits(long value) {
        if (value < Minimum || value > Maximum) { return false; }
        if (Excluded.Contains(value)) { return false; }
        if (MultipleOf is long step && step != 0 && value % step != 0) { return false; }

        return Allowed is null || Allowed.Contains(value);
    }

    /// <summary>
    ///     Applies one constraint, returning the narrowed state — or <c>null</c> when the constraint is one this model
    ///     does not track, which abandons the chain rather than misreading it.
    /// </summary>
    public ScalarConstraintState? Apply(string name, IReadOnlyList<long> arguments) {
        switch (name) {
            case "Positive":            return WithMinimum(1);
            case "Negative":            return WithMaximum(-1);
            case "Zero":                return WithMinimum(0)?.WithMaximum(0);
            case "NonZero":             return WithExcluded(0);

            // Nothing is greater than the largest representable value, nor less than the smallest: those two ask for
            // an empty domain rather than for a bound, and computing one would overflow.
            case "GreaterThan" when arguments.Count == 1:
                return arguments[0] == long.MaxValue ? Saturate() : WithMinimum(arguments[0] + 1);

            case "LessThan" when arguments.Count == 1:
                return arguments[0] == long.MinValue ? Saturate() : WithMaximum(arguments[0] - 1);

            case "GreaterThanOrEqualTo" when arguments.Count == 1: return WithMinimum(arguments[0]);
            case "LessThanOrEqualTo" when arguments.Count == 1:    return WithMaximum(arguments[0]);

            case "Between" when arguments.Count == 2:              return WithMinimum(arguments[0])?.WithMaximum(arguments[1]);
            case "MultipleOf" when arguments.Count == 1 && arguments[0] != 0:
                return new ScalarConstraintState(Minimum, Maximum, arguments[0] < 0 ? -arguments[0] : arguments[0], Allowed, Excluded);

            case "OneOf" when arguments.Count > 0:
                return new ScalarConstraintState(Minimum, Maximum, MultipleOf, [.. arguments], Excluded);

            case "Except" or "DifferentFrom" when arguments.Count > 0: {
                HashSet<long> excluded = [.. Excluded, .. arguments];

                return new ScalarConstraintState(Minimum, Maximum, MultipleOf, Allowed, excluded);
            }

            // Anything else — a granularity, a scale, a name this model has never seen — ends the walk.
            default: return null;
        }
    }

    /// <summary>Whether applying <paramref name="candidate" /> would leave the domain exactly as it is.</summary>
    public bool NarrowsNothing(ScalarConstraintState candidate) {
        return candidate.Minimum == Minimum
            && candidate.Maximum == Maximum
            && candidate.MultipleOf == MultipleOf
            && candidate.Excluded.Count == Excluded.Count
            && (candidate.Allowed?.Count ?? -1) == (Allowed?.Count ?? -1);
    }

    /// <summary>
    ///     Whether an exclusion removes a value the domain could never have produced anyway — the silent case, where
    ///     the author excluded a sentinel the generator was never going to draw.
    /// </summary>
    public bool ExclusionIsInert(IReadOnlyList<long> values) {
        return values.Count > 0 && values.All(value => !Admits(value));
    }

    private ScalarConstraintState Saturate() {
        return new ScalarConstraintState(Minimum, Maximum, MultipleOf, Allowed, Excluded, saturated: true);
    }

    private ScalarConstraintState? WithMinimum(long minimum) {
        return minimum <= Minimum
            ? new ScalarConstraintState(Minimum, Maximum, MultipleOf, Allowed, Excluded)
            : new ScalarConstraintState(minimum, Maximum, MultipleOf, Allowed, Excluded);
    }

    private ScalarConstraintState? WithMaximum(long maximum) {
        return maximum >= Maximum
            ? new ScalarConstraintState(Minimum, Maximum, MultipleOf, Allowed, Excluded)
            : new ScalarConstraintState(Minimum, maximum, MultipleOf, Allowed, Excluded);
    }

    private ScalarConstraintState WithExcluded(long value) {
        HashSet<long> excluded = [.. Excluded, value];

        return new ScalarConstraintState(Minimum, Maximum, MultipleOf, Allowed, excluded);
    }

    // Is there any multiple of step inside [Minimum, Maximum] that survives the exclusions? The range can be huge, so
    // this walks the lattice from its first multiple rather than the range itself, and gives up (answering "yes")
    // once the walk is longer than any realistic exclusion set could rule out.
    private bool HasMultipleInRange(long step) {
        if (step == 0) { return false; }

        long first = Minimum >= 0
            ? (Minimum + step - 1) / step * step
            : -((-Minimum) / step) * step;

        for (long candidate = first, seen = 0; candidate <= Maximum && seen < MaxWalkLength; candidate += step, seen++) {
            if (!Excluded.Contains(candidate) && (Allowed is null || Allowed.Contains(candidate))) { return true; }
        }

        // The walk gave up before finding one; only a range genuinely wider than the walk can still hold a multiple.
        // Compare by division so the subtraction cannot overflow at the representable extremes — which is why both
        // sides are halved, the right one carrying half the walk length rather than the whole of it.
        return Maximum / 2 - Minimum / 2 >= step * (MaxWalkLength / 2);
    }

}
