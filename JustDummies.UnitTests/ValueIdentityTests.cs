#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The value identity of the small values the failure-reporting path is built from. Each is immutable and
///     documented as a value, so each answers "is this the same one?" by what it holds rather than by which instance
///     it is — the answer a reference type gives by default, and gives silently.
/// </summary>
/// <remarks>
///     Example-suite material (ADR-0040): each case pins one named pair, and there is no argument to quantify over.
///     <see cref="ConstraintCall" /> has its own equality cases in <see cref="ConstraintCallTests" />; this fixture
///     covers the two values built beside it.
/// </remarks>
[TestSubject(typeof(ConstraintClaim))]
public sealed class ValueIdentityTests {

    #region Statics members declarations

    private static ConstraintCall Length(string bound) {
        return ConstraintCall.Of("WithLength", bound);
    }

    #endregion

    // The halves are asserted directly rather than only through ToString and equality: a getter carries no logic, so
    // a mutation score says nothing about it, and Constraint is what the blame choice reads.
    [Fact(DisplayName = "A claim on a constraint exposes the constraint, its rendering and its clause.")]
    public void ClaimOnAConstraintExposesItsHalves() {
        ConstraintCall  length = Length("3");
        ConstraintClaim claim  = ConstraintClaim.Of(length, "already fixes the length at 3");

        Check.That(claim.Constraint).IsEqualTo(length);
        Check.That(claim.Subject).IsEqualTo("WithLength(3)");
        Check.That(claim.Claims).IsEqualTo("already fixes the length at 3");
        Check.That(claim.ToString()).IsEqualTo("WithLength(3) already fixes the length at 3");
    }

    [Fact(DisplayName = "A claim on a phrase exposes the phrase and carries no constraint.")]
    public void ClaimOnAPhraseCarriesNoConstraint() {
        ConstraintClaim claim = ConstraintClaim.OfPhrase("the contained value \"ABC\"", "contains 'x', which it does not allow");

        Check.That(claim.Constraint).IsNull();
        Check.That(claim.Subject).IsEqualTo("the contained value \"ABC\"");
        Check.That(claim.Claims).IsEqualTo("contains 'x', which it does not allow");
        Check.That(claim.ToString()).IsEqualTo("the contained value \"ABC\" contains 'x', which it does not allow");
    }

    [Fact(DisplayName = "A replay exposes the seed it was given and the guidance naming it.")]
    public void ReplayExposesItsSeedAndGuidance() {
        FixedRandomSource source = new(7);

        Replay full    = Replay.Of(source, 42);
        Replay partial = Replay.PartialOf(source);

        Check.That(full.Seed).IsEqualTo(42);
        Check.That(full.Guidance).Contains("42");
        // The seed is supplied, not read back from the source, so the two need not agree.
        Check.That(partial.Seed).IsEqualTo(7);
        Check.That(partial.Guidance).Contains("not reproducible from this seed alone");
    }

    // Rendered for a reader rather than for a message: nothing appends a replay whole — AnyGenerationException takes
    // its guidance and its seed separately — so this exists for the debugger, which the type points at it.
    [Fact(DisplayName = "A replay renders its seed and guidance rather than its type name.")]
    public void ReplayRendersItself() {
        FixedRandomSource source = new(7);

        string rendered = Replay.Of(source, 42).ToString();

        Check.That(rendered).StartsWith("seed 42:");
        Check.That(rendered).Contains("Any.WithSeed(42)");
        Check.That(rendered).Not.Contains(nameof(Replay));
    }

    [Fact(DisplayName = "Two claims blaming the same constraint for the same thing are equal.")]
    public void ClaimsWithTheSameConstraintAndClaimAreEqual() {
        ConstraintClaim first  = ConstraintClaim.Of(Length("3"), "already fixes the length at 3");
        ConstraintClaim second = ConstraintClaim.Of(Length("3"), "already fixes the length at 3");

        Check.That(first.Equals(second)).IsTrue();
        Check.That(first == second).IsTrue();
        Check.That(first != second).IsFalse();
        Check.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
    }

    [Fact(DisplayName = "A claim differs when its constraint differs, and when its claim does.")]
    public void ClaimsDifferOnEitherHalf() {
        ConstraintClaim reference = ConstraintClaim.Of(Length("3"), "already fixes the length at 3");

        Check.That(reference == ConstraintClaim.Of(Length("5"), "already fixes the length at 3")).IsFalse();
        Check.That(reference == ConstraintClaim.Of(Length("3"), "already caps the length at 3")).IsFalse();
    }

    // The blame choice turns on whether a claim's subject IS the constraint being applied, so a phrase that merely
    // reads like one must not pass for it — which is what keeps the two apart here.
    [Fact(DisplayName = "A phrase never equals a claim on the constraint it reads like.")]
    public void APhraseIsNotTheConstraintItReadsLike() {
        ConstraintClaim onAConstraint = ConstraintClaim.Of(Length("3"), "already fixes the length at 3");
        ConstraintClaim onAPhrase     = ConstraintClaim.OfPhrase("WithLength(3)", "already fixes the length at 3");

        Check.That(onAConstraint.ToString()).IsEqualTo(onAPhrase.ToString());
        Check.That(onAConstraint == onAPhrase).IsFalse();
    }

    [Fact(DisplayName = "A claim equals neither null nor a value of another type.")]
    public void ClaimEqualsNeitherNullNorAnotherType() {
        ConstraintClaim claim   = ConstraintClaim.Of(Length("3"), "already fixes the length at 3");
        ConstraintClaim? nothing = null;
        object          text    = "WithLength(3) already fixes the length at 3";

        Check.That(claim.Equals(nothing)).IsFalse();
        Check.That(claim.Equals(text)).IsFalse();
        Check.That(claim == nothing).IsFalse();
        Check.That(claim != nothing).IsTrue();
        Check.That(nothing == null).IsTrue();
    }

    [Fact(DisplayName = "Two replays of the same run under the same seed are equal.")]
    public void ReplaysOfTheSameRunAreEqual() {
        FixedRandomSource source = new(7);

        Replay first  = Replay.Of(source, 42);
        Replay second = Replay.Of(source, 42);

        Check.That(first.Equals(second)).IsTrue();
        Check.That(first == second).IsTrue();
        Check.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
    }

    [Fact(DisplayName = "A replay differs when its seed differs.")]
    public void ReplaysDifferOnTheirSeed() {
        FixedRandomSource source = new(7);

        Check.That(Replay.Of(source, 42) == Replay.Of(source, 43)).IsFalse();
        Check.That(Replay.Of(source, 42) != Replay.Of(source, 43)).IsTrue();
    }

    // The seed alone does not settle it: the same seed replays a run in full or only in part depending on whether a
    // foreign generator contributed values this source never drew.
    [Fact(DisplayName = "A partial replay differs from a full one carrying the same seed.")]
    public void APartialReplayIsNotAFullOne() {
        FixedRandomSource source = new(7);

        Replay full    = Replay.Of(source);
        Replay partial = Replay.PartialOf(source);

        Check.That(full.Seed).IsEqualTo(partial.Seed);
        Check.That(full == partial).IsFalse();
    }

    [Fact(DisplayName = "A replay equals neither null nor a value of another type.")]
    public void ReplayEqualsNeitherNullNorAnotherType() {
        FixedRandomSource source  = new(7);
        Replay            replay  = Replay.Of(source, 42);
        Replay?           nothing = null;
        object            text    = "42";

        Check.That(replay.Equals(nothing)).IsFalse();
        Check.That(replay.Equals(text)).IsFalse();
        Check.That(replay == nothing).IsFalse();
        Check.That(replay != nothing).IsTrue();
        Check.That(nothing == null).IsTrue();
    }

}
