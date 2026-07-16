#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[Collection("SmartEnumSideEffects")]
[TestSubject(typeof(OutcomeAssertions))]
public sealed class OutcomeAssertionsTests : IDisposable {

    #region Constructors declarations

    public OutcomeAssertionsTests() {
        ErrorContextKey.ResetForTests();
    }

    #endregion

    public void Dispose() {
        ErrorContextKey.ResetForTests();
    }

    #region Statics members declarations

    private static DomainError AnError(string code = "PAYMENT_DECLINED", string diagnostic = "diagnostic", string @short = "short") {
        return DomainError.Create(ErrorCode.Create(code), diagnostic).WithPublicMessage(@short);
    }

    #endregion

    [Fact(DisplayName = "ShouldSucceed on a successful Outcome<T> returns the carried value.")]
    public void ShouldSucceedOnSuccessReturnsValue() {
        Outcome<int> outcome = Outcome<int>.Success(42);

        Check.That(outcome.ShouldSucceed()).IsEqualTo(42);
    }

    [Fact(DisplayName = "ShouldSucceed on a failed outcome throws, naming the failing code.")]
    public void ShouldSucceedOnFailureThrowsNamingTheCode() {
        Outcome<int> outcome = Outcome<int>.Failure(AnError());

        OutcomeAssertionException exception = Assert.Throws<OutcomeAssertionException>(() => outcome.ShouldSucceed());
        Check.That(exception.Message).Contains("PAYMENT_DECLINED");
    }

    [Fact(DisplayName = "ShouldFail on a failed outcome returns a fluent handle that matches the code.")]
    public void ShouldFailReturnsFluentHandleMatchingTheCode() {
        Outcome<int> outcome = Outcome<int>.Failure(AnError());

        outcome.ShouldFail()
               .WithCode("PAYMENT_DECLINED")
               .WithDiagnosticMessage("diagnostic")
               .WithShortMessage("short");
    }

    [Fact(DisplayName = "ShouldFail with a mismatching code throws.")]
    public void ShouldFailWithMismatchingCodeThrows() {
        Outcome<int> outcome = Outcome<int>.Failure(AnError());

        Assert.Throws<OutcomeAssertionException>(() => outcome.ShouldFail().WithCode("SOMETHING_ELSE"));
    }

    [Fact(DisplayName = "ShouldFail on a successful outcome throws.")]
    public void ShouldFailOnSuccessThrows() {
        Outcome<int> outcome = Outcome<int>.Success(Any.Int());

        Assert.Throws<OutcomeAssertionException>(() => outcome.ShouldFail());
    }

    [Fact(DisplayName = "The non-generic Outcome assertions behave the same way.")]
    public void NonGenericOutcomeAssertions() {
        Outcome.Success.ShouldSucceed();
        Outcome.Failure(AnError()).ShouldFail().WithCode("PAYMENT_DECLINED");

        Assert.Throws<OutcomeAssertionException>(() => Outcome.Success.ShouldFail());
        Assert.Throws<OutcomeAssertionException>(() => Outcome.Failure(AnError()).ShouldSucceed());
    }

    [Fact(DisplayName = "WithContextEntry checks the presence and value of a context entry.")]
    public void WithContextEntryChecksPresenceAndValue() {
        ErrorContextKey<string> cardNetwork = ErrorContextKey.Create<string>("CardNetwork", "The card network.");
        DomainError error = DomainError.Create(Any.ErrorCode(), Any.DiagnosticMessage(), context => context.Add(cardNetwork, "VISA"))
                                       .WithPublicMessage(Any.ShortMessage());
        Outcome<int> outcome = Outcome<int>.Failure(error);

        // Passes for the present key and matching value.
        outcome.ShouldFail().WithContextEntry("CardNetwork", "VISA");

        // Throws for a wrong value and for an absent key.
        Assert.Throws<OutcomeAssertionException>(() => outcome.ShouldFail().WithContextEntry("CardNetwork", "MASTERCARD"));
        Assert.Throws<OutcomeAssertionException>(() => outcome.ShouldFail().WithContextEntry("Missing"));
    }

}
