#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

/// <summary>
///     What an outcome reads as when it is inspected rather than acted on — the rendering
///     <c>[DebuggerDisplay]</c> shows, so a breakpoint on a chain of outcomes tells the reader which succeeded and
///     what the others carried, instead of repeating their type name.
/// </summary>
[TestSubject(typeof(Outcome))]
public sealed class OutcomeRenderingTests {

    #region Statics members declarations

    private static DomainError AnError() {
        return ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
    }

    #endregion

    // A result whose ToString throws is the caller's, and an outcome is read on failure paths. Degrading to the type
    // name keeps a secondary exception from replacing the problem being diagnosed — the doctrine Error states for its
    // own construction.
    private sealed class Unrenderable {

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3877:Exceptions should not be thrown from unexpected methods",
                                                         Justification =
                                                             "The throw is the fixture, not a defect: this type exists to be a result whose ToString fails, which is what " +
                                                             "Outcome<T> must survive. The rule is right about production code and is exactly what Outcome<T> obeys by " +
                                                             "catching this; a test that could not produce the case could not prove it.")]
        public override string ToString() {
            throw new InvalidOperationException("this type cannot render itself");
        }

    }

    [Fact(DisplayName = "A successful outcome carrying no value reads as a success.")]
    public void SuccessfulOutcomeReadsAsSuccess() {
        Check.That(Outcome.Success.ToString()).IsEqualTo("Success");
    }

    [Fact(DisplayName = "A failed outcome carrying no value names its error.")]
    public void FailedOutcomeNamesItsError() {
        DomainError error = AnError();

        Check.That(Outcome.Failure(error).ToString()).IsEqualTo($"Failure: {error}");
    }

    [Fact(DisplayName = "A successful outcome shows the result it carries.")]
    public void SuccessfulOutcomeShowsItsResult() {
        Check.That(Outcome<string>.Success("ORD-42").ToString()).IsEqualTo("Success: ORD-42");
        Check.That(Outcome<int>.Success(7).ToString()).IsEqualTo("Success: 7");
    }

    [Fact(DisplayName = "A failed outcome names its error rather than the result it does not carry.")]
    public void FailedGenericOutcomeNamesItsError() {
        DomainError error = AnError();

        Check.That(Outcome<string>.Failure(error).ToString()).IsEqualTo($"Failure: {error}");
    }

    [Fact(DisplayName = "A result that cannot render itself degrades to its type name instead of throwing.")]
    public void AResultThatCannotRenderItselfDegradesToItsTypeName() {
        Outcome<Unrenderable> outcome = Outcome<Unrenderable>.Success(new Unrenderable());

        Check.ThatCode(() => outcome.ToString()).DoesNotThrow();
        Check.That(outcome.ToString()).IsEqualTo($"Success: {nameof(Unrenderable)}");
    }

}
