#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

[TestSubject(typeof(SolutionErrorDocumentationGenerator))]
public sealed class SolutionErrorDocumentationGeneratorTests {

    [Fact(DisplayName = "GetErrorDocumentationFrom rejects a null solution path.")]
    public void GetErrorDocumentationFromRejectsANullSolutionPath() {
        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetErrorDocumentationFrom rejects null options.")]
    public void GetErrorDocumentationFromRejectsNullOptions() {
        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom("app.sln", null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetErrorDocumentationFrom fails when the solution file does not exist.")]
    public void GetErrorDocumentationFromFailsWhenTheSolutionDoesNotExist() {
        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom("this-solution-does-not-exist.sln", new SolutionGenerationOptions()))
             .Throws<FileNotFoundException>();
    }

    [Fact(DisplayName = "GetErrorDocumentationFrom rejects a path that is not a .sln file.")]
    public void GetErrorDocumentationFromRejectsANonSolutionFile() {
        // Setup: a real file whose extension is not .sln.
        string path = Path.GetTempFileName();

        try {
            // Exercise & verify
            Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(path, new SolutionGenerationOptions()))
                 .Throws<ArgumentException>();
        } finally {
            File.Delete(path);
        }
    }

    [Fact(DisplayName = "GetErrorDocumentationFromAssemblies rejects a null path list.")]
    public void GetErrorDocumentationFromAssembliesRejectsANullPathList() {
        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(null!, new SolutionGenerationOptions()))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetErrorDocumentationFromAssemblies rejects null options.")]
    public void GetErrorDocumentationFromAssembliesRejectsNullOptions() {
        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { "app.dll" }, null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A missing assembly is skipped (empty result) when the failure behavior is Continue.")]
    public void AMissingAssemblyIsSkippedWhenContinuing() {
        // Setup
        SolutionGenerationOptions options = new() { FailureBehavior = FailureBehavior.Continue };

        // Exercise
        IEnumerable<ErrorDocumentation> result =
            SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { "this-assembly-does-not-exist.dll" }, options);

        // Verify: no worker is launched because no assembly resolved; the result is empty.
        Check.That(result).IsEmpty();
    }

    [Fact(DisplayName = "A skipped assembly is logged as a warning (never silent) when the failure behavior is Continue.")]
    public void ASkippedAssemblyIsLoggedWhenContinuing() {
        // Setup
        RecordingGenerationLogger logger = new();
        SolutionGenerationOptions options = new() {
            FailureBehavior = FailureBehavior.Continue,
            Logger          = logger
        };

        // Exercise
        SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { "this-assembly-does-not-exist.dll" }, options);

        // Verify: the skipped assembly leaves a warning trace mentioning the missing file.
        Check.That(logger.Warnings).HasSize(1);
        Check.That(logger.Warnings[0]).Contains("this-assembly-does-not-exist.dll");
    }

    private sealed class RecordingGenerationLogger : IGenerationLogger {

        public List<string> Warnings { get; } = new();

        public void Info(string    message) { }
        public void Warning(string message) { Warnings.Add(message); }
        public void Error(string   message) { }
        public void Debug(string   message) { }

    }

    [Fact(DisplayName = "A missing assembly aborts when the failure behavior is Stop.")]
    public void AMissingAssemblyAbortsWhenStopping() {
        // Setup
        SolutionGenerationOptions options = new() { FailureBehavior = FailureBehavior.Stop };

        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { "this-assembly-does-not-exist.dll" }, options))
             .Throws<SolutionDocumentationGenerationException>();
    }

    [Fact(DisplayName = "GetErrorDocumentationFromAssemblies returns an empty catalog for an empty path list.")]
    public void GetErrorDocumentationFromAssembliesReturnsEmptyForAnEmptyList() {
        // Exercise: no assembly path at all, so no worker is ever launched.
        IEnumerable<ErrorDocumentation> result =
            SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies([], new SolutionGenerationOptions());

        // Verify
        Check.That(result).IsEmpty();
    }

    [Fact(DisplayName = "Generation fails fast when the configured documentation worker cannot be found.")]
    public void GenerationFailsWhenTheConfiguredWorkerCannotBeFound() {
        // Setup: a real (resolvable) assembly path, but a worker path that does not exist. Resolving the worker throws
        // before any process is launched, so this stays a pure, SDK-free test.
        string assemblyPath = Path.GetTempFileName();
        SolutionGenerationOptions options = new() {
            WorkerAssemblyPath = Path.Combine(Path.GetTempPath(), "this-worker-does-not-exist.dll")
        };

        try {
            // Exercise & verify
            Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { assemblyPath }, options))
                 .Throws<SolutionDocumentationGenerationException>();
        } finally {
            File.Delete(assemblyPath);
        }
    }

}
