#region Usings declarations

using FirstClassErrors.GenDoc.Rendering;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

[TestSubject(typeof(DocumentationOutputWriter))]
public sealed class DocumentationOutputWriterTests {

    // Verbose off, so the writer's Info lines are irrelevant to these tests; a recording logger keeps them off the console.
    private static readonly RecordingLogger QuietLogger = new();

    [Fact(DisplayName = "A single document with no output target is written to standard output.")]
    public void ASingleDocumentWithoutATargetGoesToStandardOutput() {
        // Setup
        RecordingOutputSink       sink   = new();
        DocumentationOutputWriter writer = new(sink);

        // Exercise
        writer.Write([new RenderedDocument("errors.json", "{}")], "json", null, QuietLogger);

        // Verify
        Check.That(sink.StandardOutput).ContainsExactly("{}");
        Check.That(sink.Files).IsEmpty();
    }

    [Fact(DisplayName = "A single document with a file target is written to that file.")]
    public void ASingleDocumentWithAFileTargetIsWrittenToThatFile() {
        // Setup
        RecordingOutputSink       sink       = new();
        DocumentationOutputWriter writer     = new(sink);
        string                    outputPath = Path.Combine(Path.GetTempPath(), $"fce-out-{Guid.NewGuid():N}.json");

        // Exercise
        writer.Write([new RenderedDocument("errors.json", "{}")], "json", outputPath, QuietLogger);

        // Verify: written to the resolved full path, nothing to standard output.
        Check.That(sink.Files.Keys).ContainsExactly(Path.GetFullPath(outputPath));
        Check.That(sink.Files[Path.GetFullPath(outputPath)]).IsEqualTo("{}");
        Check.That(sink.StandardOutput).IsEmpty();
    }

    [Fact(DisplayName = "Several documents without an output directory are rejected.")]
    public void SeveralDocumentsWithoutATargetAreRejected() {
        // Setup
        DocumentationOutputWriter writer = new(new RecordingOutputSink());
        RenderedDocument[] documents = [
            new("a.md", "a"),
            new("b.md", "b")
        ];

        // Exercise & verify
        Check.ThatCode(() => writer.Write(documents, "markdown", null, QuietLogger))
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "A renderer that produces no document is rejected.")]
    public void ARendererProducingNoDocumentIsRejected() {
        // Setup
        DocumentationOutputWriter writer = new(new RecordingOutputSink());

        // Exercise & verify
        Check.ThatCode(() => writer.Write([], "json", null, QuietLogger))
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "ResolveWithinOutput keeps a well-behaved relative path inside the output directory.")]
    public void ResolveWithinOutputAcceptsAPathInsideTheDirectory() {
        // Setup
        string directory = Path.Combine(Path.GetTempPath(), "fce-out");

        // Exercise
        string resolved = DocumentationOutputWriter.ResolveWithinOutput(directory, Path.Combine("nested", "file.md"));

        // Verify
        Check.That(resolved).IsEqualTo(Path.GetFullPath(Path.Combine(directory, "nested", "file.md")));
    }

    [Fact(DisplayName = "ResolveWithinOutput rejects a relative path that escapes the output directory.")]
    public void ResolveWithinOutputRejectsAnEscapingRelativePath() {
        // Setup
        string directory = Path.Combine(Path.GetTempPath(), "fce-out");

        // Exercise & verify: a '..' segment climbing out of the directory is refused.
        Check.ThatCode(() => DocumentationOutputWriter.ResolveWithinOutput(directory, Path.Combine("..", "escape.md")))
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "ResolveWithinOutput rejects an absolute path handed back by a renderer.")]
    public void ResolveWithinOutputRejectsAnAbsolutePath() {
        // Setup
        string directory = Path.Combine(Path.GetTempPath(), "fce-out");
        string absolute  = Path.Combine(Path.GetTempPath(), "elsewhere.md");

        // Exercise & verify: an absolute path resolves outside the directory and is refused.
        Check.ThatCode(() => DocumentationOutputWriter.ResolveWithinOutput(directory, absolute))
             .Throws<InvalidOperationException>();
    }

}
