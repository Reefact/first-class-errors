#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     The one property in this suite that reads a conflict <b>message</b> rather than only its exception type. The
///     rule "assert exception types, never message text" (ADR-0040) exists because message <i>wording</i> is unstable;
///     the invariant proven here is not the wording but its <i>truthfulness</i> — for every legal combination of
///     constraints that empties the domain, the emitted message must make only claims that are literally true, must
///     name no falsehood, and must not echo the applied constraint on both sides of "because". That correctness has a
///     genuine input space (every bound / lattice / allow-list / exclusion combination) and no type-level proxy, so it
///     is a property, and the audit that first found the defect (issue #312: an allow-list narrowed by a bound was
///     reported as forbidden "entirely" by the exclusion) becomes a standing guard rather than a one-off check.
/// </summary>
/// <remarks>
///     <para>
///         The oracle recomputes feasibility independently over a small integer universe and rejects any message whose
///         universal claim ("forbids every …", "every value between …") is not backed by that ground truth, any bare
///         allow-list claim used when the allow-list was in fact narrowed by another constraint, and any "Cannot apply
///         X because X forbids …" echo. It is deliberately coupled to the stable claim fragments, not the full prose:
///         a reworded message that stops making a checkable claim would pass, but a message that makes a <i>false</i>
///         claim cannot. This property fails on the pre-fix engines (the audit counted tens of thousands of false
///         messages), which is the falsifiability the suite requires.
///     </para>
///     <para>
///         The engines share one exhaustion path, so a builder per family — ordinal (<see cref="AnyInt32" />), decimal,
///         continuous (<see cref="AnyDouble" />) — proves them all; the 128-bit sibling is checked in
///         <c>ModernTypeInvariantProperties</c>, which the net472 floor leg excludes.
///     </para>
/// </remarks>
[TestSubject(typeof(ConflictingAnyConstraintException))]
public sealed class ConflictMessageTruthfulnessProperties {

    #region Statics members declarations

    /// <summary>Small, exact-in-every-numeric-type values; some (10) fall outside the generated <c>Between</c> window on purpose, to drive the narrowed-allow-list case.</summary>
    internal static readonly int[] Universe = [0, 1, 2, 3, 5, 10];

    /// <summary>Builds an engine's chain in the order Between?, MultipleOf?, OneOf?, Except?; returns the conflict message, or null when the chain is satisfiable.</summary>
    internal delegate string? EngineBuilder(bool hasBetween, int lo, int hi, int step, int[] allow, int[] excl);

    /// <summary>Runs the truthfulness property against one engine, quantifying over the constraint combinations it supports.</summary>
    internal static void CheckEngine(EngineBuilder build, bool supportsLattice) {
        Gen<(bool HasBetween, int Lo, int Hi, int Step, int[] Allow, int[] Excl)> combos =
            from hasBetween in Gen.Elements(true, false)
            from a in Gen.Choose(0, 4)
            from b in Gen.Choose(0, 4)
            from step in Gen.Choose(1, supportsLattice ? 3 : 1)
            from allowMask in Gen.Choose(0, (1 << Universe.Length) - 1)
            from exclMask in Gen.Choose(0, (1 << Universe.Length) - 1)
            select (hasBetween, Math.Min(a, b), Math.Max(a, b), step, Subset(allowMask), Subset(exclMask));

        Prop.ForAll(combos.ToArbitrary(),
                    combo => {
                        string? message = build(combo.HasBetween, combo.Lo, combo.Hi, combo.Step, combo.Allow, combo.Excl);
                        if (message is null) { return true; }

                        (int step, int[] allow, int[] excl) = InEffect(combo.HasBetween, combo.Lo, combo.Hi, combo.Step, combo.Allow, combo.Excl);

                        return MessageIsTruthful(message, combo.HasBetween, combo.Lo, combo.Hi, step, allow, excl);
                    })
            .QuickCheckThrowOnFailure();
    }

    private static int[] Subset(int mask) {
        List<int> chosen = [];
        for (int i = 0; i < Universe.Length; i++) {
            if ((mask & (1 << i)) != 0) { chosen.Add(Universe[i]); }
        }

        return chosen.ToArray();
    }

    /// <summary>The constraints actually in force at the throw: the prefix of the fixed build order up to the first one that empties the domain.</summary>
    private static (int Step, int[] Allow, int[] Excl) InEffect(bool hasBetween, int lo, int hi, int step, int[] allow, int[] excl) {
        int   curStep  = 1;
        int[] curAllow = [];
        int[] curExcl  = [];

        if (Feasible(hasBetween, lo, hi, curStep, curAllow, curExcl).Count == 0) { return (curStep, curAllow, curExcl); }
        if (step > 1)         { curStep  = step;  if (Feasible(hasBetween, lo, hi, curStep, curAllow, curExcl).Count == 0) { return (curStep, curAllow, curExcl); } }
        if (allow.Length > 0) { curAllow = allow; if (Feasible(hasBetween, lo, hi, curStep, curAllow, curExcl).Count == 0) { return (curStep, curAllow, curExcl); } }
        if (excl.Length > 0)  { curExcl  = excl; }

        return (curStep, curAllow, curExcl);
    }

    private static List<int> Feasible(bool hasBetween, int lo, int hi, int step, int[] allow, int[] excl) {
        int rangeLo = hasBetween ? lo : -50;
        int rangeHi = hasBetween ? hi : 50;

        List<int> feasible = [];
        for (int value = rangeLo; value <= rangeHi; value++) {
            if (allow.Length > 0 && !allow.Contains(value)) { continue; }
            if (excl.Contains(value)) { continue; }
            if (step > 1 && value % step != 0) { continue; }
            feasible.Add(value);
        }

        return feasible;
    }

    /// <summary>The oracle: true when every checkable claim the message makes is backed by independently computed ground truth.</summary>
    private static bool MessageIsTruthful(string message, bool hasBetween, int lo, int hi, int step, int[] allow, int[] excl) {
        // A conflict was thrown, so the in-effect domain must genuinely be empty.
        if (Feasible(hasBetween, lo, hi, step, allow, excl).Count != 0) { return false; }

        if (!AllowListClaimHolds(message, hasBetween, lo, hi, step, allow, excl)) { return false; }
        if (!RangeClaimHolds(message, hasBetween, lo, hi, excl)) { return false; }
        if (!LatticeClaimHolds(message, hasBetween, lo, hi, step, excl)) { return false; }

        return MessageReadsWell(message);
    }

    /// <summary>
    ///     Allow-list claims. The bare form asserts every allowed value is forbidden; the qualified form asserts only
    ///     the values the bounds and lattice still permit are forbidden. True when the message makes no such claim.
    /// </summary>
    private static bool AllowListClaimHolds(string message, bool hasBetween, int lo, int hi, int step, int[] allow, int[] excl) {
        if (!message.Contains("every value") || !message.Contains("allows") || allow.Length == 0) { return true; }

        int   rangeLo   = hasBetween ? lo : -50;
        int   rangeHi   = hasBetween ? hi : 50;
        int[] reachable = allow.Where(a => a >= rangeLo && a <= rangeHi && (step <= 1 || a % step == 0)).ToArray();

        bool qualified = message.Contains("that the other constraints leave");
        bool claimTrue = qualified ? reachable.All(a => excl.Contains(a)) : allow.All(a => excl.Contains(a));
        if (!claimTrue) { return false; }

        // The bare form must not be used when a bound or the lattice already dropped an allowed value.
        return qualified || reachable.Length == allow.Length;
    }

    /// <summary>A range claim must hold for every value in the window. True when the message makes no such claim.</summary>
    private static bool RangeClaimHolds(string message, bool hasBetween, int lo, int hi, int[] excl) {
        if (!hasBetween || !message.Contains($"every value between {lo} and {hi}")) { return true; }

        return Enumerable.Range(lo, hi - lo + 1).All(value => excl.Contains(value));
    }

    /// <summary>A lattice claim must hold for every on-lattice value in the window. True when the message makes no such claim.</summary>
    private static bool LatticeClaimHolds(string message, bool hasBetween, int lo, int hi, int step, int[] excl) {
        if (!hasBetween || step <= 1 || !message.Contains($"every MultipleOf({step}) value between {lo} and {hi}")) { return true; }

        return Enumerable.Range(lo, hi - lo + 1).Where(value => value % step == 0).All(value => excl.Contains(value));
    }

    /// <summary>Comprehensibility: no malformed fragment, and no "Cannot apply X because X forbids …" echo.</summary>
    private static bool MessageReadsWell(string message) {
        if (message.Contains("  ") || message.Contains(" ,") || message.Contains("forbids ,") || message.Contains("forbid ,")) { return false; }

        int because = message.IndexOf(" because ", StringComparison.Ordinal);
        if (because < 0 || !message.StartsWith("Cannot apply ", StringComparison.Ordinal)) { return true; }

        int    prefix  = "Cannot apply ".Length;
        string applied = message.Substring(prefix, because - prefix);
        string clause  = message.Substring(because + " because ".Length);

        return !clause.StartsWith(applied + " forbids", StringComparison.Ordinal) && !clause.StartsWith(applied + " forbid", StringComparison.Ordinal);
    }

    #endregion

    [Fact(DisplayName = "Ordinal: every exclusion-caused conflict message makes only true claims, over the whole combination space.")]
    public void OrdinalConflictMessagesAreTruthful() {
        CheckEngine(BuildInt32, supportsLattice: true);
    }

    [Fact(DisplayName = "Decimal: every exclusion-caused conflict message makes only true claims, over the whole combination space.")]
    public void DecimalConflictMessagesAreTruthful() {
        CheckEngine(BuildDecimal, supportsLattice: false);
    }

    [Fact(DisplayName = "Continuous: every exclusion-caused conflict message makes only true claims, over the whole combination space.")]
    public void ContinuousConflictMessagesAreTruthful() {
        CheckEngine(BuildDouble, supportsLattice: false);
    }

    #region Engine builders

    private static string? BuildInt32(bool hasBetween, int lo, int hi, int step, int[] allow, int[] excl) {
        try {
            AnyInt32 spec = Any.Int32();
            if (hasBetween) { spec = spec.Between(lo, hi); }
            if (step > 1)   { spec = spec.MultipleOf(step); }
            if (allow.Length > 0) { spec = spec.OneOf(allow); }
            if (excl.Length  > 0) { spec = spec.Except(excl); }

            return null;
        } catch (ConflictingAnyConstraintException exception) { return exception.Message; }
    }

    private static string? BuildDecimal(bool hasBetween, int lo, int hi, int step, int[] allow, int[] excl) {
        try {
            AnyDecimal spec = Any.Decimal();
            if (hasBetween) { spec = spec.Between(lo, hi); }
            if (allow.Length > 0) { spec = spec.OneOf(allow.Select(value => (decimal)value).ToArray()); }
            if (excl.Length  > 0) { spec = spec.Except(excl.Select(value => (decimal)value).ToArray()); }

            return null;
        } catch (ConflictingAnyConstraintException exception) { return exception.Message; }
    }

    private static string? BuildDouble(bool hasBetween, int lo, int hi, int step, int[] allow, int[] excl) {
        try {
            AnyDouble spec = Any.Double();
            if (hasBetween) { spec = spec.Between(lo, hi); }
            if (allow.Length > 0) { spec = spec.OneOf(allow.Select(value => (double)value).ToArray()); }
            if (excl.Length  > 0) { spec = spec.Except(excl.Select(value => (double)value).ToArray()); }

            return null;
        } catch (ConflictingAnyConstraintException exception) { return exception.Message; }
    }

    #endregion

}
