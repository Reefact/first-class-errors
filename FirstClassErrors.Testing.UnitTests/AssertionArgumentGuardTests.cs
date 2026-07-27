#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Testing.UnitTests;

/// <summary>
///     Guard tests for the arguments every public entry point of the testing package documents as
///     <see cref="ArgumentNullException" />. They exist because the promise is made in the XML documentation and was,
///     until now, made nowhere else: deleting any of these <c>throw</c> statements left the whole suite green.
/// </summary>
/// <remarks>
///     A guard that nothing asserts is a guard that can be removed by accident. These are cheap, and they are the
///     difference between a documented contract and a comment.
/// </remarks>
[TestSubject(typeof(OutcomeAssertions))]
[TestSubject(typeof(ErrorAssertion))]
public sealed class AssertionArgumentGuardTests {

    #region Statics members declarations

    private static DomainError AnError() {
        return DomainError.Create(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any())
                          .WithPublicMessage(ShortMessageFactory.Any());
    }

    #endregion

    [Fact(DisplayName = "ShouldSucceed on a null Outcome throws ArgumentNullException.")]
    public void ShouldSucceedRejectsANullOutcome() {
        Outcome outcome = null!;

        Check.ThatCode(() => outcome.ShouldSucceed())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShouldFail on a null Outcome throws ArgumentNullException.")]
    public void ShouldFailRejectsANullOutcome() {
        Outcome outcome = null!;

        Check.ThatCode(() => outcome.ShouldFail())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShouldSucceed on a null Outcome<T> throws ArgumentNullException.")]
    public void ShouldSucceedRejectsANullGenericOutcome() {
        Outcome<int> outcome = null!;

        Check.ThatCode(() => outcome.ShouldSucceed())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShouldFail on a null Outcome<T> throws ArgumentNullException.")]
    public void ShouldFailRejectsANullGenericOutcome() {
        Outcome<int> outcome = null!;

        Check.ThatCode(() => outcome.ShouldFail())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "WithCode rejects a null expected code, whichever overload is called.")]
    public void WithCodeRejectsANullExpectedCode() {
        ErrorAssertion assertion = Outcome.Failure(AnError()).ShouldFail();

        Check.ThatCode(() => assertion.WithCode((string)null!))
             .Throws<ArgumentNullException>();
        Check.ThatCode(() => assertion.WithCode((ErrorCode)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "WithCode accepts a matching ErrorCode and returns the same assertion for chaining.")]
    public void WithCodeAcceptsAMatchingErrorCode() {
        ErrorCode code = ErrorCodeFactory.Any();
        DomainError error = DomainError.Create(code, DiagnosticMessageFactory.Any()).WithPublicMessage(ShortMessageFactory.Any());
        ErrorAssertion assertion = Outcome.Failure(error).ShouldFail();

        // The ErrorCode overload delegates to the string one; both the guard and the delegation are asserted here.
        Check.That(assertion.WithCode(code)).IsSameReferenceAs(assertion);
    }

    [Fact(DisplayName = "WithContextEntry rejects a null key.")]
    public void WithContextEntryRejectsANullKey() {
        ErrorAssertion assertion = Outcome.Failure(AnError()).ShouldFail();

        Check.ThatCode(() => assertion.WithContextEntry(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Clock.Use rejects a null clock.")]
    public void ClockUseRejectsANullClock() {
        Check.ThatCode(() => Clock.Use(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "InstanceIds.Use rejects a null identifier source.")]
    public void InstanceIdsUseRejectsANullSource() {
        Check.ThatCode(() => InstanceIds.Use(null!))
             .Throws<ArgumentNullException>();
    }

}
