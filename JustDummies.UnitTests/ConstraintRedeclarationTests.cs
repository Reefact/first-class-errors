#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The cross-type convention for re-declaring a constraint that is "declared once per generator".
///     <para>
///         Declaring the <b>same</b> constraint twice is not a contradiction — the second declaration asks for exactly
///         what the first already guarantees — so it is a no-op. Declaring a <b>different</b> value for the same
///         once-only constraint is a contradiction and still fails at declaration time. The rule is one rule across
///         every generator family, which is why it is pinned here rather than scattered through each type's own tests.
///     </para>
///     <para>
///         This is a structural convention over a fixed table, not a property: the input space is "which constraint",
///         and the constraints are heterogeneous — there is nothing to generate.
///     </para>
/// </summary>
public sealed class ConstraintRedeclarationTests {

    #region Statics members declarations

    private static readonly Guid Pinned = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

    /// <summary>Every once-only constraint, declared twice with identical arguments.</summary>
    private static IEnumerable<(string Label, Func<object> Redeclare)> IdenticalRedeclarations() {
        yield return ("String().WithLength(3)", () => Any.String().WithLength(3).WithLength(3));
        yield return ("String().StartingWith(\"a\")", () => Any.String().StartingWith("a").StartingWith("a"));
        yield return ("String().EndingWith(\"z\")", () => Any.String().EndingWith("z").EndingWith("z"));
        yield return ("String().Alpha()", () => Any.String().Alpha().Alpha());
        yield return ("String().Numeric()", () => Any.String().Numeric().Numeric());
        yield return ("String().AlphaNumeric()", () => Any.String().AlphaNumeric().AlphaNumeric());
        yield return ("String().WithChars(\"ab\")", () => Any.String().WithChars("ab").WithChars("ab"));
        yield return ("String().LowerCase()", () => Any.String().LowerCase().LowerCase());
        yield return ("String().UpperCase()", () => Any.String().UpperCase().UpperCase());
        yield return ("Int32().OneOf(1, 2)", () => Any.Int32().OneOf(1, 2).OneOf(1, 2));
        yield return ("Int32().MultipleOf(3)", () => Any.Int32().MultipleOf(3).MultipleOf(3));
        yield return ("Int64().OneOf(1L)", () => Any.Int64().OneOf(1L).OneOf(1L));
        yield return ("Double().OneOf(1.5)", () => Any.Double().OneOf(1.5).OneOf(1.5));
        yield return ("Decimal().WithScale(2)", () => Any.Decimal().WithScale(2).WithScale(2));
        yield return ("Char().Alpha()", () => Any.Char().Alpha().Alpha());
        yield return ("Char().LowerCase()", () => Any.Char().LowerCase().LowerCase());
        yield return ("Guid().OneOf(pinned)", () => Any.Guid().OneOf(Pinned).OneOf(Pinned));
        yield return ("Uri().Web().WithPathSegments(2)", () => Any.Uri().Web().WithPathSegments(2).WithPathSegments(2));
        yield return ("ListOf().WithCount(3)", () => Any.ListOf(Any.Int32()).WithCount(3).WithCount(3));
        yield return ("DateTimeOffset().WithOffset(zero)", () => Any.DateTimeOffset().WithOffset(TimeSpan.Zero).WithOffset(TimeSpan.Zero));
    }

    /// <summary>The same constraints declared twice with arguments that genuinely contradict.</summary>
    private static IEnumerable<(string Label, Func<object> Contradict)> ContradictoryRedeclarations() {
        yield return ("String().WithLength(3).WithLength(5)", () => Any.String().WithLength(3).WithLength(5));
        yield return ("String().StartingWith(\"a\").StartingWith(\"b\")", () => Any.String().StartingWith("a").StartingWith("b"));
        yield return ("String().Alpha().Numeric()", () => Any.String().Alpha().Numeric());
        yield return ("String().LowerCase().UpperCase()", () => Any.String().LowerCase().UpperCase());
        yield return ("Int32().OneOf(1, 2).OneOf(3, 4)", () => Any.Int32().OneOf(1, 2).OneOf(3, 4));
        yield return ("Int32().MultipleOf(2).MultipleOf(3)", () => Any.Int32().MultipleOf(2).MultipleOf(3));
        yield return ("Decimal().WithScale(2).WithScale(4)", () => Any.Decimal().WithScale(2).WithScale(4));
        yield return ("Char().Alpha().Numeric()", () => Any.Char().Alpha().Numeric());
        yield return ("Uri().Web().WithPathSegments(2).WithPathSegments(3)", () => Any.Uri().Web().WithPathSegments(2).WithPathSegments(3));
        yield return ("ListOf().WithCount(3).WithCount(4)", () => Any.ListOf(Any.Int32()).WithCount(3).WithCount(4));
        yield return ("DateTimeOffset().WithOffset(0h).WithOffset(1h)", () => Any.DateTimeOffset().WithOffset(TimeSpan.Zero).WithOffset(TimeSpan.FromHours(1)));
    }

    #endregion

    [Fact(DisplayName = "Re-declaring a once-only constraint with identical arguments is a no-op, in every generator family.")]
    public void IdenticalRedeclarationIsANoOp() {
        // A constraint declared twice with the same argument is not a contradiction: the domain the second declaration
        // asks for is exactly the one the first already produced. Refusing it made the fluent reject a specification it
        // can satisfy — the one thing the eager check exists to avoid.
        List<string> refused = new();
        foreach ((string label, Func<object> redeclare) in IdenticalRedeclarations()) {
            try {
                redeclare();
            } catch (ConflictingAnyConstraintException) {
                refused.Add(label);
            }
        }

        Check.WithCustomMessage($"identical re-declarations still refused: {string.Join(", ", refused)}")
             .That(refused).IsEmpty();
    }

    [Fact(DisplayName = "Re-declaring a once-only constraint with a different argument is still a conflict.")]
    public void ContradictoryRedeclarationStillConflicts() {
        // The other half, and the reason the fix compares the rendered declaration rather than simply dropping the
        // guard: tolerating an identical re-declaration must not tolerate a contradictory one.
        List<string> accepted = new();
        foreach ((string label, Func<object> contradict) in ContradictoryRedeclarations()) {
            try {
                contradict();
                accepted.Add(label);
            } catch (ConflictingAnyConstraintException) {
                // expected
            }
        }

        Check.WithCustomMessage($"contradictions silently accepted: {string.Join(", ", accepted)}")
             .That(accepted).IsEmpty();
    }

    [Fact(DisplayName = "A no-op re-declaration leaves the generator's domain untouched.")]
    public void ANoOpRedeclarationDoesNotWidenTheDomain() {
        // The no-op must be a no-op: returning `this` rather than rebuilding means the second declaration cannot
        // loosen anything. Asserted on the observable domain, not on object identity.
        for (int i = 0; i < 200; i++) {
            Check.That(Any.String().WithLength(4).WithLength(4).Generate().Length).IsEqualTo(4);
            Check.That(new[] { 7, 9 }).Contains(Any.Int32().OneOf(7, 9).OneOf(7, 9).Generate());
            Check.That(Any.ListOf(Any.Int32()).WithCount(3).WithCount(3).Generate().Count).IsEqualTo(3);
        }
    }

}
