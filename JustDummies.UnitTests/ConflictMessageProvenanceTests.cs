#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     A conflict message must name the constraint that actually caused the conflict. When an <b>exclusion</b>
///     (<c>NonZero</c>/<c>Except</c>/<c>DifferentFrom</c>) empties the domain, the interval engines used to name a
///     bound instead — or the constraint being applied — producing messages that were self-referential
///     (<c>"Cannot apply Zero() because Zero() already pins the value to 0"</c>) or factually false
///     (<c>"GreaterThanOrEqualTo(5) already pins the value to 5"</c>, which allows 5..MaxValue). Issue #312.
/// </summary>
/// <remarks>
///     Message content is the example suite's job (ADR-0040): these pin the contract "name the excluding
///     constraint", not the exact prose. Each asserts that the offending exclusion appears in the message — the
///     information that was missing — which is red against the old engines and green once exclusions carry
///     provenance. The four interval engines (ordinal, wide, decimal, continuous) share one exhaustion path, so a
///     case per engine guards them all.
/// </remarks>
[TestSubject(typeof(ConflictingAnyConstraintException))]
public sealed class ConflictMessageProvenanceTests {

    #region Statics members declarations

    private static string ConflictMessage(Action build) {
        try {
            build();
        } catch (ConflictingAnyConstraintException exception) {
            return exception.Message;
        }

        return "<no ConflictingAnyConstraintException was thrown>";
    }

    #endregion

    // ----- OrdinalIntervalSpec: integers (and, by sharing the engine, TimeSpan/DateTime/DateOnly/TimeOnly) -----

    [Fact(DisplayName = "A pin emptied by NonZero names NonZero, not the pin itself.")]
    public void PinEmptiedByNonZeroNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Byte().NonZero().Zero());

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("NonZero()");
    }

    [Fact(DisplayName = "A single-value bound emptied by NonZero names NonZero.")]
    public void BoundEmptiedByNonZeroNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Byte().NonZero().LessThan(1));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("NonZero()");
    }

    [Fact(DisplayName = "A range pinned by two bounds and emptied by Except names Except, not a bound.")]
    public void PinEmptiedByExceptNamesTheExclusionNotABound() {
        string message = ConflictMessage(() => Any.Int32().Except(5).GreaterThanOrEqualTo(5).LessThanOrEqualTo(5));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("Except(5)");
    }

    [Fact(DisplayName = "A pin emptied by DifferentFrom names DifferentFrom.")]
    public void PinEmptiedByDifferentFromNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Int32().DifferentFrom(-1).Negative().GreaterThan(-2));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("DifferentFrom(-1)");
    }

    [Fact(DisplayName = "A lattice emptied by Except names Except and the lattice, not the lattice alone.")]
    public void LatticeEmptiedByExceptNamesBothTheExclusionAndTheLattice() {
        string message = ConflictMessage(() => Any.Int32().MultipleOf(5).Except(0).Between(-4, 4));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("Except(0)");
        Check.WithCustomMessage($"The lattice was not named. Message: {message}").That(message).Contains("MultipleOf(5)");
    }

    [Fact(DisplayName = "An allow-list emptied by Except names Except, not just the allow-list.")]
    public void AllowListEmptiedByExceptNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Int32().Except(1, 2).OneOf(1, 2));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("Except(1, 2)");
    }

    // ----- DecimalIntervalSpec -----

    [Fact(DisplayName = "A decimal pin emptied by DifferentFrom names DifferentFrom.")]
    public void DecimalPinEmptiedByDifferentFromNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Decimal().DifferentFrom(1m).Between(1m, 1m));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("DifferentFrom(1)");
    }

    // ----- ContinuousIntervalSpec: double (and, by sharing the engine, Single/Half) -----

    [Fact(DisplayName = "A double pin emptied by DifferentFrom names DifferentFrom.")]
    public void DoublePinEmptiedByDifferentFromNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Double().DifferentFrom(1d).Between(1d, 1d));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("DifferentFrom(1)");
    }

#if NET8_0_OR_GREATER
    // ----- WideIntervalSpec: Int128/UInt128 (net8.0 leg only) -----

    [Fact(DisplayName = "An Int128 pin emptied by NonZero names NonZero.")]
    public void Int128PinEmptiedByNonZeroNamesTheExclusion() {
        string message = ConflictMessage(() => Any.Int128().NonZero().Between(System.Int128.Zero, System.Int128.Zero));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("NonZero()");
    }
#endif

    // ----- Correctness of the claim, not only the naming (surfaced by the exhaustive audit). -----

    [Fact(DisplayName = "An allow-list narrowed by a bound before an exclusion empties it does not claim the exclusion forbids every allowed value.")]
    public void AllowListNarrowedByABoundIsNotOverclaimed() {
        // OneOf(1, 3) offers two values, but Between(0, 1) already drops 3; Except(1) then removes the only one
        // that survived. Saying Except(1) forbids *every* value OneOf allows would be false — it never forbids 3 —
        // so the claim must be qualified to the values the other constraints leave.
        string message = ConflictMessage(() => Any.Int32().Except(1).Between(0, 1).OneOf(1, 3));

        Check.WithCustomMessage($"The exclusion was not named. Message: {message}").That(message).Contains("Except(1)");
        Check.WithCustomMessage($"The message overclaims that Except(1) forbids every value OneOf allows. Message: {message}")
             .That(message).Contains("that the other constraints leave");
    }

    [Fact(DisplayName = "When the applied exclusion is itself the sole cause, the message reads 'it forbids', not the constraint twice.")]
    public void AnExclusionAppliedLastIsNotRepeatedOnBothSides() {
        // Zero() pins the byte to 0; NonZero(), applied last, is itself the forbidder. Repeating "NonZero()" on
        // both sides of "because" reads as circular, so the clause refers back to the applied constraint as "it".
        string message = ConflictMessage(() => Any.Byte().Zero().NonZero());

        Check.WithCustomMessage($"The applied constraint should be referred to as 'it'. Message: {message}").That(message).Contains("it forbids");
        Check.WithCustomMessage($"The applied constraint is echoed after 'because'. Message: {message}").That(message).Not.Contains("because NonZero()");
    }

    // ----- Regression guard: bound-vs-bound messages must stay correct and unchanged. -----

    [Fact(DisplayName = "A bound-vs-bound conflict still names the opposing bound (unchanged).")]
    public void BoundVersusBoundStillNamesBothSides() {
        string message = ConflictMessage(() => Any.Int32().Between(1, 10).GreaterThan(50));

        Check.That(message).Contains("Between(1, 10)");
        Check.That(message).Contains("GreaterThan(50)");
    }

}
