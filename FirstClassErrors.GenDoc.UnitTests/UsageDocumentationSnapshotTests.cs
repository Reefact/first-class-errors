#region Usings declarations

using FirstClassErrors.GenDoc.Rendering;
using FirstClassErrors.Usage.Model;

using VerifyXunit;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

/// <summary>
///     End-to-end snapshots of the real <c>FirstClassErrors.Usage</c> catalog. The catalog is extracted
///     <b>in-process</b> with the same reader the worker runs, so this exercises the real reflection and the real
///     code execution (living examples) without spawning a worker or needing the SDK. The extraction and every
///     rendered output are snapshotted with Verify.
/// </summary>
/// <remarks>
///     On the first run Verify writes <c>*.received.*</c> files and fails; review and approve them (they become
///     <c>*.verified.*</c>) to lock the golden output.
/// </remarks>
public sealed class UsageDocumentationSnapshotTests {

    #region Statics members declarations

    private static ErrorDocumentationExtractionResult Extract() {
        return AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(typeof(Temperature).Assembly);
    }

    #endregion

    [Fact(DisplayName = "The extraction of the Usage catalog matches its snapshot.")]
    public async Task TheExtractedUsageCatalog() {
        await Verifier.Verify(Extract());
    }

    [Fact(DisplayName = "The JSON rendering of the Usage catalog matches its snapshot.")]
    public async Task TheJsonRenderingOfTheUsageCatalog() {
        await Verifier.Verify(new JsonErrorDocumentationRenderer().Render(Extract().Documentation));
    }

    [Fact(DisplayName = "The single-file Markdown rendering of the Usage catalog matches its snapshot.")]
    public async Task TheSingleMarkdownRenderingOfTheUsageCatalog() {
        await Verifier.Verify(new MarkdownErrorDocumentationRenderer(MarkdownLayout.Single).Render(Extract().Documentation));
    }

    [Fact(DisplayName = "The split Markdown rendering of the Usage catalog matches its snapshot.")]
    public async Task TheSplitMarkdownRenderingOfTheUsageCatalog() {
        await Verifier.Verify(new MarkdownErrorDocumentationRenderer(MarkdownLayout.Split).Render(Extract().Documentation));
    }

}
