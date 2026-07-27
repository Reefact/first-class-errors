#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Testing.UnitTests;

/// <summary>
///     Tests over the <i>shape</i> of the values the arbitrary-value factories return.
/// </summary>
/// <remarks>
///     The shape is the whole point of these factories: <c>Any detailed message 7F3A9C.</c> announces itself as
///     arbitrary in a failure message, where a bare random string would read as a value someone chose. Nothing asserted
///     that shape, so the recognisable prefix and the trailing period could both vanish unnoticed — and the factories
///     would keep passing while losing the only property that distinguishes them from a plain random draw.
/// </remarks>
[TestSubject(typeof(DetailedMessageFactory))]
[TestSubject(typeof(DiagnosticMessageFactory))]
[TestSubject(typeof(ShortMessageFactory))]
[TestSubject(typeof(ErrorOriginFactory))]
public sealed class ArbitraryValueFactoryShapeTests {

    [Fact(DisplayName = "DetailedMessageFactory.Any announces itself and ends as a sentence.")]
    public void DetailedMessageIsRecognisableAsArbitrary() {
        string message = DetailedMessageFactory.Any();

        Check.That(message).StartsWith("Any detailed message ");
        Check.That(message).EndsWith(".");
    }

    [Fact(DisplayName = "DiagnosticMessageFactory.Any announces itself and ends as a sentence.")]
    public void DiagnosticMessageIsRecognisableAsArbitrary() {
        string message = DiagnosticMessageFactory.Any();

        Check.That(message).StartsWith("Any diagnostic message ");
        Check.That(message).EndsWith(".");
    }

    [Fact(DisplayName = "ShortMessageFactory.Any announces itself and ends as a sentence.")]
    public void ShortMessageIsRecognisableAsArbitrary() {
        string message = ShortMessageFactory.Any();

        Check.That(message).StartsWith("Any short message ");
        Check.That(message).EndsWith(".");
    }

    [Fact(DisplayName = "The message factories vary their arbitrary part between calls.")]
    public void MessageFactoriesVaryTheirArbitraryPart() {
        // A factory pinned to a constant would still satisfy the shape checks above while defeating the purpose of
        // drawing a value at all: two errors built from it would be indistinguishable in a report.
        HashSet<string> drawn = [];
        for (int i = 0; i < 50; i++) { drawn.Add(DetailedMessageFactory.Any()); }

        Check.That(drawn).Not.HasSize(1);
    }

    [Fact(DisplayName = "ErrorOriginFactory.Any draws across the origins rather than returning a constant.")]
    public void ErrorOriginFactoryDrawsMoreThanOneOrigin() {
        // Documented as uniform across all members: a factory collapsed to a single origin — the enum's default being
        // the likeliest accident — would pass every test that only reads "some origin".
        HashSet<ErrorOrigin> drawn = [];
        for (int i = 0; i < 200; i++) { drawn.Add(ErrorOriginFactory.Any()); }

        Check.That(drawn).Not.HasSize(1);
    }

}
