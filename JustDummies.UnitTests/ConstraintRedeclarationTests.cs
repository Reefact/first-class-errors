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
        yield return ("String().OneOf(\"a\", \"b\")", () => Any.String().OneOf("a", "b").OneOf("a", "b"));
        yield return ("Int32().OneOf(1, 2)", () => Any.Int32().OneOf(1, 2).OneOf(1, 2));
        yield return ("Int32().MultipleOf(3)", () => Any.Int32().MultipleOf(3).MultipleOf(3));
        yield return ("Int64().OneOf(1L)", () => Any.Int64().OneOf(1L).OneOf(1L));
        yield return ("Double().OneOf(1.5)", () => Any.Double().OneOf(1.5).OneOf(1.5));
        yield return ("Decimal().WithScale(2)", () => Any.Decimal().WithScale(2).WithScale(2));
        yield return ("Char().Alpha()", () => Any.Char().Alpha().Alpha());
        yield return ("Char().LowerCase()", () => Any.Char().LowerCase().LowerCase());
        yield return ("Guid().OneOf(pinned)", () => Any.Guid().OneOf(Pinned).OneOf(Pinned));
        yield return ("Uri().Web().WithPathSegments(2)", () => Any.Uri().Web().WithPathSegments(2).WithPathSegments(2));
        yield return ("Uri().Web().WithHost(\"a.example\")", () => Any.Uri().Web().WithHost("a.example").WithHost("a.example"));
        yield return ("Uri().Web().WithPort(8080)", () => Any.Uri().Web().WithPort(8080).WithPort(8080));
        yield return ("Uri().Web().WithUserInfo(\"alice\")", () => Any.Uri().Web().WithUserInfo("alice").WithUserInfo("alice"));
        yield return ("Uri().Mailto().WithDomain(\"a.example\")", () => Any.Uri().Mailto().WithDomain("a.example").WithDomain("a.example"));
        yield return ("ListOf().WithCount(3)", () => Any.ListOf(Any.Int32()).WithCount(3).WithCount(3));
        yield return ("DateTimeOffset().WithOffset(zero)", () => Any.DateTimeOffset().WithOffset(TimeSpan.Zero).WithOffset(TimeSpan.Zero));
    }

    /// <summary>The same constraints declared twice with arguments that genuinely contradict.</summary>
    private static IEnumerable<(string Label, Func<object> Contradict)> ContradictoryRedeclarations() {
        yield return ("String().WithLength(3).WithLength(5)", () => Any.String().WithLength(3).WithLength(5));
        yield return ("String().StartingWith(\"a\").StartingWith(\"b\")", () => Any.String().StartingWith("a").StartingWith("b"));
        yield return ("String().Alpha().Numeric()", () => Any.String().Alpha().Numeric());
        yield return ("String().LowerCase().UpperCase()", () => Any.String().LowerCase().UpperCase());
        yield return ("String().OneOf(\"a\", \"b\").OneOf(\"c\", \"d\")", () => Any.String().OneOf("a", "b").OneOf("c", "d"));
        yield return ("Int32().OneOf(1, 2).OneOf(3, 4)", () => Any.Int32().OneOf(1, 2).OneOf(3, 4));
        yield return ("Int32().MultipleOf(2).MultipleOf(3)", () => Any.Int32().MultipleOf(2).MultipleOf(3));
        yield return ("Decimal().WithScale(2).WithScale(4)", () => Any.Decimal().WithScale(2).WithScale(4));
        yield return ("Char().Alpha().Numeric()", () => Any.Char().Alpha().Numeric());
        yield return ("Uri().Web().WithPathSegments(2).WithPathSegments(3)", () => Any.Uri().Web().WithPathSegments(2).WithPathSegments(3));
        yield return ("Uri().Web().WithHost(a).WithHost(b)", () => Any.Uri().Web().WithHost("first.example").WithHost("second.example"));
        yield return ("Uri().Web().WithPort(8080).WithPort(9090)", () => Any.Uri().Web().WithPort(8080).WithPort(9090));
        yield return ("Uri().Web().WithUserInfo(a).WithUserInfo(b)", () => Any.Uri().Web().WithUserInfo("alice").WithUserInfo("bob"));
        yield return ("Uri().Ftp().WithPort().WithPort(21)", () => Any.Uri().Ftp().WithPort().WithPort(21));
        yield return ("Uri().Mailto().WithDomain(a).WithDomain(b)", () => Any.Uri().Mailto().WithDomain("a.example").WithDomain("b.example"));
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

    [Fact(DisplayName = "A second URI component pin names both sides instead of silently replacing the first.")]
    public void ASecondComponentPinNamesBothSides() {
        // Regression: WithHost, WithPort and WithUserInfo were plain setters — a second, different value replaced the
        // first and the first vanished without a word, while WithPathSegments in the very same generator raised a
        // conflict. A URI has one host, one port and one user-info, so a second declaration can never be honoured
        // alongside the first; dropping it silently discards a constraint the caller wrote.
        ConflictingAnyConstraintException host = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Uri().Web().WithHost("first.example").WithHost("second.example"));

        Check.That(host.Message).Contains("WithHost(\"second.example\")");
        Check.That(host.Message).Contains("WithHost(\"first.example\")");

        // The message names the PUBLIC call, so a mailto's WithDomain reads as WithDomain and not as the host setter
        // it shares with the web families.
        ConflictingAnyConstraintException domain = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Uri().Mailto().WithDomain("a.example").WithDomain("b.example"));

        Check.That(domain.Message).Contains("WithDomain(\"b.example\")");
        Check.That(domain.Message).Not.Contains("WithHost");
    }

    [Fact(DisplayName = "A second, different Distinct comparer conflicts; the same one again is a no-op.")]
    public void ASecondDistinctComparerConflicts() {
        // One collection is distinct under one equality. Two different comparers cannot both be honoured, and the
        // second was silently winning.
        Check.ThatCode(() => Any.ListOf(Any.Int32()).Distinct(new ModuloComparer(10)).Distinct(new ModuloComparer(100)))
             .Throws<ConflictingAnyConstraintException>();

        IEqualityComparer<int> comparer = new ModuloComparer(10);

        Check.ThatCode(() => Any.ListOf(Any.Int32()).Distinct(comparer).Distinct(comparer)).DoesNotThrow();
        // Re-declaring distinctness without naming a comparer asks for the equality already in force.
        Check.ThatCode(() => Any.ListOf(Any.Int32()).Distinct(comparer).Distinct()).DoesNotThrow();
    }

    [Fact(DisplayName = "A no-op re-declaration leaves the generator's domain untouched.")]
    public void ANoOpRedeclarationDoesNotWidenTheDomain() {
        // The no-op must be a no-op: returning `this` rather than rebuilding means the second declaration cannot
        // loosen anything. Asserted on the observable domain, not on object identity.
        for (int i = 0; i < 200; i++) {
            Check.That(Any.String().WithLength(4).WithLength(4).Generate().Length).IsEqualTo(4);
            Check.That(new[] { 7, 9 }).Contains(Any.Int32().OneOf(7, 9).OneOf(7, 9).Generate());
            Check.That(new[] { "a", "b" }).Contains(Any.String().OneOf("a", "b").OneOf("a", "b").Generate());
            Check.That(Any.ListOf(Any.Int32()).WithCount(3).WithCount(3).Generate().Count).IsEqualTo(3);
        }
    }

    #region Nested types

    private sealed class ModuloComparer : IEqualityComparer<int> {

        private readonly int _modulus;

        public ModuloComparer(int modulus) {
            _modulus = modulus;
        }

        public bool Equals(int x, int y) {
            return x % _modulus == y % _modulus;
        }

        public int GetHashCode(int obj) {
            return obj % _modulus;
        }

    }

    #endregion

}
