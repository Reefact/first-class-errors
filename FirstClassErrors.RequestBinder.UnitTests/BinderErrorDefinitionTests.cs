#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

/// <summary>
///     Unit tests for the two configuration value types the structural-error definitions introduce:
///     <see cref="BindingMessage" /> (a plain public-message carrier) and <see cref="BinderErrorDefinition" /> (a code
///     bundled with its message builder, immutable, derivable through WithCode / WithMessage).
/// </summary>
public sealed class BinderErrorDefinitionTests {

    private static readonly ErrorCode SampleCode = ErrorCode.Create("SOME_CODE");

    private static BindingMessage Message(string argumentPath) {
        return new BindingMessage($"short {argumentPath}", $"detailed {argumentPath}");
    }

    // ── BindingMessage ────────────────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "BindingMessage carries the short and detailed messages; the detail defaults to null.")]
    public void BindingMessageCarriesBothMessages() {
        var full = new BindingMessage("short", "detailed");
        Check.That(full.ShortMessage).IsEqualTo("short");
        Check.That(full.DetailedMessage).IsEqualTo("detailed");

        var shortOnly = new BindingMessage("short");
        Check.That(shortOnly.DetailedMessage).IsNull();
    }

    [Fact(DisplayName = "BindingMessage never throws on a null short message — coalescing is deferred to the error factory.")]
    public void BindingMessageToleratesNullShortMessage() {
        Check.ThatCode(() => new BindingMessage(null!)).DoesNotThrow();
    }

    // ── BinderErrorDefinition: construction and accessors ─────────────────────────────────────────────────

    [Fact(DisplayName = "GetMessage invokes the builder with the argument path; Code exposes the code.")]
    public void GetMessageInvokesTheBuilder() {
        var definition = new BinderErrorDefinition(SampleCode, Message);

        BindingMessage message = definition.GetMessage("Guests[1].FirstName");

        Check.That(definition.Code).IsEqualTo(SampleCode);
        Check.That(message.ShortMessage).IsEqualTo("short Guests[1].FirstName");
        Check.That(message.DetailedMessage).IsEqualTo("detailed Guests[1].FirstName");
    }

    [Fact(DisplayName = "A null code or a null message builder is rejected at construction.")]
    public void ConstructorRejectsNulls() {
        Check.ThatCode(() => new BinderErrorDefinition(null!, Message)).Throws<ArgumentNullException>();
        Check.ThatCode(() => new BinderErrorDefinition(SampleCode, null!)).Throws<ArgumentNullException>();
    }

    // ── Immutable derivation: WithCode / WithMessage ──────────────────────────────────────────────────────

    [Fact(DisplayName = "WithCode swaps the code and keeps the message builder, without mutating the original.")]
    public void WithCodeSwapsCodeKeepsMessage() {
        var original = new BinderErrorDefinition(SampleCode, Message);
        var other    = ErrorCode.Create("OTHER_CODE");

        BinderErrorDefinition derived = original.WithCode(other);

        Check.That(derived.Code).IsEqualTo(other);
        Check.That(derived.GetMessage("X").ShortMessage).IsEqualTo("short X"); // same builder
        Check.That(original.Code).IsEqualTo(SampleCode);                       // original untouched
    }

    [Fact(DisplayName = "WithMessage swaps the message builder and keeps the code, without mutating the original.")]
    public void WithMessageSwapsMessageKeepsCode() {
        var original = new BinderErrorDefinition(SampleCode, Message);

        BinderErrorDefinition derived = original.WithMessage(argumentPath => new BindingMessage($"new {argumentPath}"));

        Check.That(derived.Code).IsEqualTo(SampleCode);                          // same code
        Check.That(derived.GetMessage("X").ShortMessage).IsEqualTo("new X");
        Check.That(original.GetMessage("X").ShortMessage).IsEqualTo("short X");  // original untouched
    }

    [Fact(DisplayName = "WithCode and WithMessage reject nulls.")]
    public void WithersRejectNulls() {
        var definition = new BinderErrorDefinition(SampleCode, Message);

        Check.ThatCode(() => definition.WithCode(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => definition.WithMessage(null!)).Throws<ArgumentNullException>();
    }

}
