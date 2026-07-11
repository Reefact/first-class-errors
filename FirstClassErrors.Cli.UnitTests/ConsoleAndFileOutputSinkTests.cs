#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

[TestSubject(typeof(ConsoleAndFileOutputSink))]
public sealed class ConsoleAndFileOutputSinkTests {

    [Fact(DisplayName = "Writing a file into a not-yet-existing nested directory creates the directory and the file.")]
    public void WritingIntoANestedDirectoryCreatesItAndTheFile() {
        // Setup: a target whose parent directory does not exist yet, so WriteFile must create it.
        ConsoleAndFileOutputSink sink     = new();
        string                   rootDir  = Path.Combine(Path.GetTempPath(), $"fce-sink-{Guid.NewGuid():N}");
        string                   filePath = Path.Combine(rootDir, "nested", "out.txt");

        try {
            // Exercise
            sink.WriteFile(filePath, "content");

            // Verify: the nested directory was created and the content written.
            Check.That(File.Exists(filePath)).IsTrue();
            Check.That(File.ReadAllText(filePath)).IsEqualTo("content");
        } finally {
            if (Directory.Exists(rootDir)) { Directory.Delete(rootDir, recursive: true); }
        }
    }

}
