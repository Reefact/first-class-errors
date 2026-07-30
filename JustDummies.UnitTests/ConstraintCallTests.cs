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

    [Fact(DisplayName = "Both factories reject a null name, and Of rejects a null argument array.")]
    public void RejectsNullArguments() {
        Check.ThatCode(() => ConstraintCall.Of(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => ConstraintCall.Of("Between", (string[])null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => ConstraintCall.OfElided(null!)).Throws<ArgumentNullException>();
    }

}
