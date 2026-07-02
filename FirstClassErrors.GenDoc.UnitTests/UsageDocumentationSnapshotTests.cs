#region Usings declarations

using FirstClassErrors.GenDoc.Rendering;
using FirstClassErrors.Usage.Model;

using VerifyXunit;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

/// <summary>
///     End-to-end snapshots of the real <c>FirstClassErrors.Usage</c> catalog. The catalog is extracted
///     <b>in-process</b> with the same reader the worker runs, so this exercises the real reflection and the real
///     code execution (living examples) without spawning a worker or needing the SDK. The extraction is snapshotted
///     as a model; each rendered output is snapshotted as its own document (JSON / Markdown), not as a wrapper object.
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
        string json = new JsonErrorDocumentationRenderer().Render(Extract().Documentation)[0].Content;

        await Verifier.Verify(json, extension: "json");
    }

    [Fact(DisplayName = "The single-file Markdown rendering of the Usage catalog matches its snapshot.")]
    public async Task TheSingleMarkdownRenderingOfTheUsageCatalog() {
        string markdown = new MarkdownErrorDocumentationRenderer(MarkdownLayout.Single).Render(Extract().Documentation)[0].Content;

        await Verifier.Verify(markdown, extension: "md");
    }

    [Fact(DisplayName = "Each file of the split Markdown rendering of the Usage catalog matches its snapshot.")]
    public async Task TheSplitMarkdownRenderingOfTheUsageCatalog() {
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer(MarkdownLayout.Split).Render(Extract().Documentation);

        // One snapshot per produced file, mirroring exactly what the split layout writes to disk (no wrapper).
        foreach (RenderedDocument document in documents) {
            await Verifier.Verify(document.Content, extension: "md")
                          .UseMethodName($"Split_{Path.GetFileNameWithoutExtension(document.RelativePath)}");
        }
    }

}
