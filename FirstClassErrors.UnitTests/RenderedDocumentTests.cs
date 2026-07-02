#region Usings declarations

using FirstClassErrors.GenDoc.Rendering;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(RenderedDocument))]
public sealed class RenderedDocumentTests {

    [Fact(DisplayName = "A rendered document exposes the relative path and content it is built from.")]
    public void ARenderedDocumentExposesItsRelativePathAndContent() {
        // Exercise
        RenderedDocument document = new("errors/README.md", "# Error Catalog");

        // Verify
        Check.That(document.RelativePath).IsEqualTo("errors/README.md");
        Check.That(document.Content).IsEqualTo("# Error Catalog");
    }

    [Fact(DisplayName = "A rendered document cannot be created without a relative path.")]
    public void ARenderedDocumentCannotBeCreatedWithoutARelativePath() {
        // Exercise & verify
        Check.ThatCode(() => new RenderedDocument(null!, "content"))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A rendered document cannot be created without content.")]
    public void ARenderedDocumentCannotBeCreatedWithoutContent() {
        // Exercise & verify
        Check.ThatCode(() => new RenderedDocument("errors.md", null!))
             .Throws<ArgumentNullException>();
    }

}
