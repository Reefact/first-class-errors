#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The rendering contract of <see cref="ConstraintCall" />: a constraint is quoted in diagnostics as the caller
///     spelled it. The expected spellings below are the ones the generators write by hand today
///     (<c>"Zero()"</c>, <c>$"Between({V(minimum)}, {V(maximum)})"</c>, <c>"OneOf(...)"</c>), pinned here so the
///     type can take that job over without moving a single character of any conflict message.
/// </summary>
/// <remarks>
///     These belong to the example suite (ADR-0040): each asserts one named spelling, and the null cases have no
///     input space. The guards themselves are also held by
///     <see cref="NullArgumentGuardConventionTests" /> — reflected over every internal member — but a constraint's
///     own contract is worth reading locally.
/// </remarks>
[TestSubject(typeof(ConstraintCall))]
public sealed class ConstraintCallTests {

    [Fact(DisplayName = "A constraint given no argument renders as an empty argument list.")]
    public void RendersAConstraintWithoutArguments() {
        ConstraintCall call = ConstraintCall.Of("Zero");

        Check.That(call.ToString()).IsEqualTo("Zero()");
    }

    [Fact(DisplayName = "A constraint given one argument renders it between the parentheses.")]
    public void RendersAConstraintWithOneArgument() {
        ConstraintCall call = ConstraintCall.Of("MultipleOf", "5");

        Check.That(call.ToString()).IsEqualTo("MultipleOf(5)");
    }

    [Fact(DisplayName = "A constraint given several arguments separates them with a comma and a space.")]
    public void RendersSeveralArgumentsSeparatedByACommaAndASpace() {
        ConstraintCall call = ConstraintCall.Of("Between", "0", "100");

        Check.That(call.ToString()).IsEqualTo("Between(0, 100)");
    }

    // The migration path for the generators that pre-join a pool through their own Join helper: the joined text is
    // one argument, and passing it through must not re-punctuate it.
    [Fact(DisplayName = "An argument already carrying separators is rendered untouched.")]
    public void KeepsAnAlreadyJoinedArgumentIntact() {
        ConstraintCall call = ConstraintCall.Of("OneOf", "1, 2, 3");

        Check.That(call.ToString()).IsEqualTo("OneOf(1, 2, 3)");
    }

    [Fact(DisplayName = "A constraint whose arguments cannot be rendered elides them with an ellipsis.")]
    public void RendersElidedArgumentsAsAnEllipsis() {
        ConstraintCall call = ConstraintCall.OfElided("OneOf");

        Check.That(call.ToString()).IsEqualTo("OneOf(...)");
    }

    [Fact(DisplayName = "The declaring method's name reaches the rendering as written.")]
    public void CarriesTheNameItWasGiven() {
        ConstraintCall call = ConstraintCall.Of(nameof(Any.ElementOf));

        Check.That(call.ToString()).IsEqualTo("ElementOf()");
    }

    // The specs compare the constraint being applied against the one already recorded to tell a harmless
    // redeclaration from a conflict, so equality is over what the constraint reads as, not over identity.
    [Fact(DisplayName = "Two constraints built apart but reading the same are equal.")]
    public void EqualsAnotherConstraintThatReadsTheSame() {
        ConstraintCall first  = ConstraintCall.Of("Between", "0", "100");
        ConstraintCall second = ConstraintCall.Of("Between", "0", "100");

        Check.That(first.Equals(second)).IsTrue();
        Check.That(first == second).IsTrue();
        Check.That(first != second).IsFalse();
        Check.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
    }

    [Fact(DisplayName = "The same name carrying different arguments is a different constraint.")]
    public void DiffersFromTheSameNameWithOtherArguments() {
        ConstraintCall first  = ConstraintCall.Of("Between", "0", "100");
        ConstraintCall second = ConstraintCall.Of("Between", "5", "50");

        Check.That(first.Equals(second)).IsFalse();
        Check.That(first != second).IsTrue();
    }

    [Fact(DisplayName = "Different names are different constraints, and the ellipsis is not an empty list.")]
    public void DiffersFromAnotherName() {
        Check.That(ConstraintCall.Of("Zero") == ConstraintCall.Of("NonZero")).IsFalse();
        Check.That(ConstraintCall.Of("OneOf") == ConstraintCall.OfElided("OneOf")).IsFalse();
    }

    [Fact(DisplayName = "Equality is ordinal, so casing tells two constraints apart.")]
    public void ComparesOrdinally() {
        Check.That(ConstraintCall.Of("zero") == ConstraintCall.Of("Zero")).IsFalse();
    }

    [Fact(DisplayName = "A constraint equals neither null nor a value of another type.")]
    public void EqualsNeitherNullNorAnotherType() {
        ConstraintCall  call    = ConstraintCall.Of("Zero");
        ConstraintCall? nothing = null;
        object          text    = "Zero()";

        Check.That(call.Equals(nothing)).IsFalse();
        Check.That(call.Equals(text)).IsFalse();
        Check.That(call == nothing).IsFalse();
        Check.That(call != nothing).IsTrue();
    }

    [Fact(DisplayName = "Two absent constraints compare equal, which is what an unset spec slot relies on.")]
    public void TreatsTwoAbsentConstraintsAsEqual() {
        ConstraintCall? absent = null;

        Check.That(absent == null).IsTrue();
        Check.That(absent != null).IsFalse();
    }

    [Fact(DisplayName = "Both factories reject a null name, and Of rejects a null argument array.")]
    public void RejectsNullArguments() {
        Check.ThatCode(() => ConstraintCall.Of(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => ConstraintCall.Of("Between", (string[])null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => ConstraintCall.OfElided(null!)).Throws<ArgumentNullException>();
    }

}
