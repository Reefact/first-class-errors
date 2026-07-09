#region Usings declarations

using FirstClassErrors.GenDoc.Rendering;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

[TestSubject(typeof(GenerateCommand))]
public sealed class GenerateCommandOutputTests {

    // Verbose off, so the logger's Info lines are suppressed and the tests produce no console noise.
    private static readonly ConsoleGenerationLogger QuietLogger = new(false);

    [Fact(DisplayName = "A single document with no output target is written to standard output.")]
    public void ASingleDocumentWithoutATargetGoesToStandardOutput() {
        // Setup
        RecordingOutputSink sink    = new();
        GenerateCommand     command = new(new StubGenerator(), sink);

        // Exercise
        command.WriteOutput([new RenderedDocument("errors.json", "{}")], "json", null, QuietLogger);

        // Verify
        Check.That(sink.StandardOutput).ContainsExactly("{}");
        Check.That(sink.Files).IsEmpty();
    }

    [Fact(DisplayName = "A single document with a file target is written to that file.")]
    public void ASingleDocumentWithAFileTargetIsWrittenToThatFile() {
        // Setup
        RecordingOutputSink sink       = new();
        GenerateCommand     command    = new(new StubGenerator(), sink);
        string              outputPath = Path.Combine(Path.GetTempPath(), $"fce-out-{Guid.NewGuid():N}.json");

        // Exercise
        command.WriteOutput([new RenderedDocument("errors.json", "{}")], "json", outputPath, QuietLogger);

        // Verify: written to the resolved full path, nothing to standard output.
        Check.That(sink.Files.Keys).ContainsExactly(Path.GetFullPath(outputPath));
        Check.That(sink.Files[Path.GetFullPath(outputPath)]).IsEqualTo("{}");
        Check.That(sink.StandardOutput).IsEmpty();
    }

    [Fact(DisplayName = "Several documents without an output directory are rejected.")]
    public void SeveralDocumentsWithoutATargetAreRejected() {
        // Setup
        GenerateCommand command = new(new StubGenerator(), new RecordingOutputSink());
        RenderedDocument[] documents = [
            new("a.md", "a"),
            new("b.md", "b")
        ];

        // Exercise & verify
        Check.ThatCode(() => command.WriteOutput(documents, "markdown", null, QuietLogger))
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "A renderer that produces no document is rejected.")]
    public void ARendererProducingNoDocumentIsRejected() {
        // Setup
        GenerateCommand command = new(new StubGenerator(), new RecordingOutputSink());

        // Exercise & verify
        Check.ThatCode(() => command.WriteOutput([], "json", null, QuietLogger))
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "ResolveWithinOutput keeps a well-behaved relative path inside the output directory.")]
    public void ResolveWithinOutputAcceptsAPathInsideTheDirectory() {
        // Setup
        string directory = Path.Combine(Path.GetTempPath(), "fce-out");

        // Exercise
        string resolved = GenerateCommand.ResolveWithinOutput(directory, Path.Combine("nested", "file.md"));

        // Verify
        Check.That(resolved).IsEqualTo(Path.GetFullPath(Path.Combine(directory, "nested", "file.md")));
    }

    [Fact(DisplayName = "ResolveWithinOutput rejects a relative path that escapes the output directory.")]
    public void ResolveWithinOutputRejectsAnEscapingRelativePath() {
        // Setup
        string directory = Path.Combine(Path.GetTempPath(), "fce-out");

        // Exercise & verify: a '..' segment climbing out of the directory is refused.
        Check.ThatCode(() => GenerateCommand.ResolveWithinOutput(directory, Path.Combine("..", "escape.md")))
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "ResolveWithinOutput rejects an absolute path handed back by a renderer.")]
    public void ResolveWithinOutputRejectsAnAbsolutePath() {
        // Setup
        string directory = Path.Combine(Path.GetTempPath(), "fce-out");
        string absolute  = Path.Combine(Path.GetTempPath(), "elsewhere.md");

        // Exercise & verify: an absolute path resolves outside the directory and is refused.
        Check.ThatCode(() => GenerateCommand.ResolveWithinOutput(directory, absolute))
             .Throws<InvalidOperationException>();
    }

}
