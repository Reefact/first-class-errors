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

/// <summary>A generator that always reports the run was cancelled, to exercise the command's cancellation handling.</summary>
internal sealed class CancellingGenerator : IErrorDocumentationGenerator {

    public IEnumerable<ErrorDocumentation> GetErrorDocumentationFrom(string solutionPath, SolutionGenerationOptions options) {
        throw new OperationCanceledException();
    }

    public IEnumerable<ErrorDocumentation> GetErrorDocumentationFromAssemblies(IReadOnlyList<string> assemblyPaths, SolutionGenerationOptions options) {
        throw new OperationCanceledException();
    }

}

/// <summary>A logger that records each message per level, so tests can assert what the command reported.</summary>
internal sealed class RecordingLogger : IGenerationLogger {

    public List<string> Infos    { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<string> Errors   { get; } = new();
    public List<string> Debugs   { get; } = new();

    public void Info(string    message) { Infos.Add(message); }
    public void Warning(string message) { Warnings.Add(message); }
    public void Error(string   message) { Errors.Add(message); }
    public void Debug(string   message) { Debugs.Add(message); }

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
