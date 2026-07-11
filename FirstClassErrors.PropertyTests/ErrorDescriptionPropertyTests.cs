#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace FirstClassErrors.PropertyTests;

/// <summary>
///     Property-based tests for <see cref="ErrorDescription" />, focusing on its normalization contract: the
///     mandatory messages are trimmed and required to be non-blank, while the optional detailed message collapses
///     to <c>null</c> whenever it is blank.
/// </summary>
[TestSubject(typeof(ErrorDescription))]
public sealed class ErrorDescriptionPropertyTests {

    [Fact(DisplayName = "The mandatory messages are trimmed and never keep surrounding whitespace.")]
    public void MandatoryMessagesAreTrimmed() {
        Gen<string> text = Generators.NonBlank();
        var inputs = (from shortMessage in text
                      from diagnostic in text
                      select (shortMessage, diagnostic)).ToArbitrary();

        Prop.ForAll(inputs,
                    input => {
                        ErrorDescription description = new(input.shortMessage, input.diagnostic);

                        return description.ShortMessage == input.shortMessage.Trim()
                               && description.DiagnosticMessage == input.diagnostic.Trim()
                               && description.ShortMessage == description.ShortMessage.Trim()
                               && description.DiagnosticMessage == description.DiagnosticMessage.Trim();
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Re-describing with already-trimmed messages is idempotent.")]
    public void NormalizationIsIdempotent() {
        Gen<string> text = Generators.NonBlank();
        var inputs = (from shortMessage in text
                      from diagnostic in text
                      select (shortMessage, diagnostic)).ToArbitrary();

        Prop.ForAll(inputs,
                    input => {
                        ErrorDescription once  = new(input.shortMessage, input.diagnostic);
                        ErrorDescription twice = new(once.ShortMessage, once.DiagnosticMessage);

                        return once.ShortMessage == twice.ShortMessage && once.DiagnosticMessage == twice.DiagnosticMessage;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A blank detailed message collapses to null; a non-blank one is trimmed.")]
    public void DetailedMessageCollapsesWhenBlank() {
        Gen<string> text = Generators.NonBlank();
        Gen<string?> optionalDetail = Gen.OneOf(Generators.NonBlank().Select(value => (string?)value),
                                                Generators.Blank().Select(value => (string?)value),
                                                Gen.Constant((string?)null));
        var inputs = (from shortMessage in text
                      from diagnostic in text
                      from detail in optionalDetail
                      select (shortMessage, diagnostic, detail)).ToArbitrary();

        Prop.ForAll(inputs,
                    input => {
                        ErrorDescription description = new(input.shortMessage, input.diagnostic, input.detail);
                        string?          expected    = string.IsNullOrWhiteSpace(input.detail) ? null : input.detail.Trim();

                        return description.DetailedMessage == expected;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A blank short message is rejected with an ArgumentException.")]
    public void BlankShortMessageIsRejected() {
        Prop.ForAll(Generators.Blank().ToArbitrary(), blank => Expect.Throws<ArgumentException>(() => new ErrorDescription(blank, "diagnostic")))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A blank diagnostic message is rejected with an ArgumentException.")]
    public void BlankDiagnosticMessageIsRejected() {
        Prop.ForAll(Generators.Blank().ToArbitrary(), blank => Expect.Throws<ArgumentException>(() => new ErrorDescription("short", blank)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A null mandatory message is rejected with an ArgumentNullException.")]
    public void NullMandatoryMessageIsRejected() {
        Assert.Throws<ArgumentNullException>(() => new ErrorDescription(null!, "diagnostic"));
        Assert.Throws<ArgumentNullException>(() => new ErrorDescription("short", null!));
    }

}
