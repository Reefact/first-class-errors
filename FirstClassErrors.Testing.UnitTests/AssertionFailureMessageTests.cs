#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Testing.UnitTests;

/// <summary>
///     Tests over the <i>content</i> of the messages the assertions produce when an expectation is not met.
/// </summary>
/// <remarks>
///     <para>
///         The message is this package's product. An assertion helper that detects the mismatch but describes it badly
///         wastes exactly the minutes it exists to save — and nothing else in the suite looked at these strings: every
///         message could be replaced by an empty one without a single test noticing.
///     </para>
///     <para>
///         Each test therefore names the two facts a reader needs: what was expected, and what was actually there.
///     </para>
/// </remarks>
[TestSubject(typeof(OutcomeAssertions))]
[TestSubject(typeof(ErrorAssertion))]
public sealed class AssertionFailureMessageTests {

    #region Statics members declarations

    private static DomainError AnError(string diagnostic, string @short) {
        return DomainError.Create(ErrorCodeFactory.Any(), diagnostic).WithPublicMessage(@short);
    }

    private static DomainError AnErrorWithContext(Action<ErrorContextBuilder> context) {
        return DomainError.Create(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), context)
                          .WithPublicMessage(ShortMessageFactory.Any());
    }

    private static string MessageOf(Action assertion) {
        return Assert.Throws<OutcomeAssertionException>(assertion).Message;
    }

    #endregion

    [Fact(DisplayName = "A mismatching code is reported with both the expected and the actual code.")]
    public void MismatchingCodeNamesBothCodes() {
        ErrorCode actual = ErrorCode.Create("ACTUAL_CODE");
        DomainError error = DomainError.Create(actual, DiagnosticMessageFactory.Any()).WithPublicMessage(ShortMessageFactory.Any());
        ErrorAssertion assertion = Outcome.Failure(error).ShouldFail();

        string message = MessageOf(() => assertion.WithCode("EXPECTED_CODE"));

        Check.That(message).Contains("EXPECTED_CODE", "ACTUAL_CODE");
    }

    [Fact(DisplayName = "A mismatching diagnostic message is reported with both the expected and the actual text.")]
    public void MismatchingDiagnosticMessageNamesBothTexts() {
        ErrorAssertion assertion = Outcome.Failure(AnError("actual diagnostic", ShortMessageFactory.Any())).ShouldFail();

        string message = MessageOf(() => assertion.WithDiagnosticMessage("expected diagnostic"));

        Check.That(message).Contains("expected diagnostic", "actual diagnostic");
    }

    [Fact(DisplayName = "A mismatching short message is reported with both the expected and the actual text.")]
    public void MismatchingShortMessageNamesBothTexts() {
        ErrorAssertion assertion = Outcome.Failure(AnError(DiagnosticMessageFactory.Any(), "actual short")).ShouldFail();

        string message = MessageOf(() => assertion.WithShortMessage("expected short"));

        Check.That(message).Contains("expected short", "actual short");
    }

    [Fact(DisplayName = "An absent context entry is reported with the missing key and the keys that are present.")]
    public void AbsentContextEntryNamesTheMissingKeyAndThePresentOnes() {
        ErrorContextKey<string> network = ErrorContextKey.Create<string>("AssertionMessageNetwork", "The card network.");
        ErrorContextKey<string> issuer = ErrorContextKey.Create<string>("AssertionMessageIssuer", "The card issuer.");
        DomainError error = AnErrorWithContext(context => context.Add(network, "VISA").Add(issuer, "ACME"));
        ErrorAssertion assertion = Outcome.Failure(error).ShouldFail();

        string message = MessageOf(() => assertion.WithContextEntry("Absent"));

        Check.That(message).Contains("Absent", "AssertionMessageNetwork", "AssertionMessageIssuer");

        // The listed keys must be separated rather than run together. Checked on the list itself, not on the whole
        // message: the surrounding prose contains commas of its own, so a separator dropped from the join would go
        // unnoticed by a check over the full string.
        string listed = message[(message.IndexOf("Present keys: ", StringComparison.Ordinal) + "Present keys: ".Length)..];
        Check.That(listed).Contains(", ");
    }

    [Fact(DisplayName = "An absent context entry on an error with no context says so explicitly.")]
    public void AbsentContextEntryOnAnEmptyContextSaysNone() {
        ErrorAssertion assertion = Outcome.Failure(AnError(DiagnosticMessageFactory.Any(), ShortMessageFactory.Any())).ShouldFail();

        string message = MessageOf(() => assertion.WithContextEntry("Absent"));

        Check.That(message).Contains("(none)");
    }

    [Fact(DisplayName = "A context entry with the wrong value is reported with both values.")]
    public void MismatchingContextValueNamesBothValues() {
        ErrorContextKey<string> network = ErrorContextKey.Create<string>("AssertionMessageValueNetwork", "The card network.");
        DomainError error = AnErrorWithContext(context => context.Add(network, "VISA"));
        ErrorAssertion assertion = Outcome.Failure(error).ShouldFail();

        string message = MessageOf(() => assertion.WithContextEntry("AssertionMessageValueNetwork", "MASTERCARD"));

        Check.That(message).Contains("AssertionMessageValueNetwork", "MASTERCARD", "VISA");
    }

    [Fact(DisplayName = "A context entry expected to be null is reported as null, not as an empty string.")]
    public void MismatchingContextValueRendersNullDistinctly() {
        ErrorContextKey<string> network = ErrorContextKey.Create<string>("AssertionMessageNullNetwork", "The card network.");
        DomainError error = AnErrorWithContext(context => context.Add(network, "VISA"));
        ErrorAssertion assertion = Outcome.Failure(error).ShouldFail();

        string message = MessageOf(() => assertion.WithContextEntry("AssertionMessageNullNetwork", null));

        // "null" unquoted for the absent value, quoted for the present one: the reader must be able to tell a null
        // apart from the four-letter string.
        Check.That(message).Contains("null", "\"VISA\"");
    }

    [Fact(DisplayName = "Asking for the value of an absent entry fails as an assertion, not as a lookup.")]
    public void ValueCheckOnAnAbsentKeyReportsTheAbsence() {
        ErrorAssertion assertion = Outcome.Failure(AnError(DiagnosticMessageFactory.Any(), ShortMessageFactory.Any())).ShouldFail();

        // The two-argument overload delegates to the presence check first; without it the dictionary lookup would
        // throw a KeyNotFoundException, which reads as a bug in the test helper rather than as a failed expectation.
        string message = MessageOf(() => assertion.WithContextEntry("Absent", "whatever"));

        Check.That(message).Contains("Absent");
    }

    [Fact(DisplayName = "A success where a failure was expected is reported as such.")]
    public void UnexpectedSuccessIsReported() {
        string message = MessageOf(() => Outcome.Success.ShouldFail());

        Check.That(message).Contains("failure", "success");
    }

    [Fact(DisplayName = "A success carrying a value where a failure was expected reports that value.")]
    public void UnexpectedSuccessReportsTheCarriedValue() {
        string message = MessageOf(() => Outcome<string>.Success("the carried value").ShouldFail());

        Check.That(message).Contains("\"the carried value\"");
    }

    [Fact(DisplayName = "A failure where a success was expected reports the code and the diagnostic message.")]
    public void UnexpectedFailureReportsCodeAndDiagnostic() {
        ErrorCode code = ErrorCode.Create("UNEXPECTED_FAILURE");
        DomainError error = DomainError.Create(code, "the diagnostic text").WithPublicMessage(ShortMessageFactory.Any());

        string message = MessageOf(() => Outcome.Failure(error).ShouldSucceed());

        Check.That(message).Contains("UNEXPECTED_FAILURE", "the diagnostic text");
    }

}
