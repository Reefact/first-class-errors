#region Usings declarations

using FirstClassErrors.GenDoc;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

/// <summary>A generator that records nothing and returns an empty catalog; used when the generator is irrelevant.</summary>
internal sealed class StubGenerator : IErrorDocumentationGenerator {

    public IEnumerable<ErrorDocumentation> GetErrorDocumentationFrom(string solutionPath, SolutionGenerationOptions options) {
        return [];
    }

    public IEnumerable<ErrorDocumentation> GetErrorDocumentationFromAssemblies(IReadOnlyList<string> assemblyPaths, SolutionGenerationOptions options) {
        return [];
    }

}

/// <summary>An output sink that records what would have been written, instead of touching the console or disk.</summary>
internal sealed class RecordingOutputSink : IOutputSink {

    public List<string>               StandardOutput { get; } = new();
    public Dictionary<string, string> Files          { get; } = new();

    public void WriteStandardOutput(string content) {
        StandardOutput.Add(content);
    }

    public void WriteFile(string fullPath, string content) {
        Files[fullPath] = content;
    }

}
