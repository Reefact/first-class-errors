#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

/// <summary>
///     The public documentation seams a consumer uses to surface its <b>overridden</b> binder structural errors in its
///     own catalog: <c>SampleArgumentRequired/Invalid</c> (a faithful example error) and
///     <c>DescribeArgumentRequired/Invalid</c> (the binder's generic prose plus that example). Both are built from a
///     <see cref="BinderErrorDefinition" />, so a consumer that renamed the codes or localized the messages documents
///     what it actually emits — structurally identical to what the binder raises at binding time.
/// </summary>
public sealed class StructuralErrorDescriptionTests {

    private static readonly BinderErrorDefinition CustomRequired =
        RequestBindingError.DefaultArgumentRequired
                           .WithCode(ErrorCode.Create("ACME_ARGUMENT_REQUIRED"))
                           .WithMessage(argumentPath => new BindingMessage("Champ obligatoire.", $"Le champ '{argumentPath}' est obligatoire."));

    private static readonly BinderErrorDefinition CustomInvalid =
        RequestBindingError.DefaultArgumentInvalid
                           .WithCode(ErrorCode.Create("ACME_ARGUMENT_INVALID"))
                           .WithMessage(argumentPath => new BindingMessage("Valeur invalide.", $"Le champ '{argumentPath}' est invalide."));

    // ── Samples carry the definition's code + messages, and the binder's structural shape ─────────────────

    [Fact(DisplayName = "SampleArgumentRequired builds an error with the definition's custom code and messages, under the argument-path context.")]
    public void SampleRequiredCarriesTheDefinition() {
        PrimaryPortError error = RequestBindingError.SampleArgumentRequired(CustomRequired);

        Check.That(error.Code.ToString()).IsEqualTo("ACME_ARGUMENT_REQUIRED");
        Check.That(error.ShortMessage).IsEqualTo("Champ obligatoire.");
        Check.That(error.DetailedMessage).IsEqualTo("Le champ 'Guests[1].FirstName' est obligatoire.");
        Check.That(BindingAssertions.ArgumentPathOf(error)).IsEqualTo("Guests[1].FirstName");
    }

    [Fact(DisplayName = "SampleArgumentInvalid builds an error with the definition's custom code and messages, and still wraps a sample cause.")]
    public void SampleInvalidCarriesTheDefinitionAndCause() {
        PrimaryPortError error = RequestBindingError.SampleArgumentInvalid(CustomInvalid);

        Check.That(error.Code.ToString()).IsEqualTo("ACME_ARGUMENT_INVALID");
        Check.That(error.ShortMessage).IsEqualTo("Valeur invalide.");
        Check.That(error.InnerErrors).Not.IsEmpty();
    }

    // ── Describe reuses the binder's generic prose, with the definition's example ──────────────────────────

    [Fact(DisplayName = "DescribeArgumentRequired reuses the binder's title and diagnoses; its example carries the definition's messages.")]
    public void DescribeRequiredReusesProseWithCustomExample() {
        ErrorDocumentation doc = RequestBindingError.DescribeArgumentRequired(CustomRequired);

        Check.That(doc.Title).IsEqualTo("Required request argument missing");                                     // binder's generic prose
        Check.That(doc.Diagnostics.Select(d => d.Origin)).ContainsExactly(ErrorOrigin.External, ErrorOrigin.Internal);
        Check.That(doc.Examples.Single().ShortMessage).IsEqualTo("Champ obligatoire.");                           // consumer's message
        Check.That(doc.Examples.Single().DetailedMessage).IsEqualTo("Le champ 'Guests[1].FirstName' est obligatoire.");
    }

    [Fact(DisplayName = "DescribeArgumentInvalid reuses the binder's title and diagnoses; its example carries the definition's messages.")]
    public void DescribeInvalidReusesProseWithCustomExample() {
        ErrorDocumentation doc = RequestBindingError.DescribeArgumentInvalid(CustomInvalid);

        Check.That(doc.Title).IsEqualTo("Request argument invalid");
        Check.That(doc.Diagnostics.Select(d => d.Origin)).ContainsExactly(ErrorOrigin.External, ErrorOrigin.Internal);
        Check.That(doc.Examples.Single().ShortMessage).IsEqualTo("Valeur invalide.");
    }

    // ── The default-catalog path is unchanged (the parameterless documentation delegates here) ────────────

    [Fact(DisplayName = "The seams with the default definition still yield the shipped default codes and messages.")]
    public void DefaultDefinitionYieldsTheShippedCatalog() {
        Check.That(RequestBindingError.SampleArgumentRequired(RequestBindingError.DefaultArgumentRequired).Code.ToString())
             .IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(RequestBindingError.DescribeArgumentRequired(RequestBindingError.DefaultArgumentRequired).Examples.Single().ShortMessage)
             .IsEqualTo("A required argument is missing.");
    }

}
