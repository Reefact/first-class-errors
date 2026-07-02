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

    [Fact(DisplayName = "A missing assembly aborts when the failure behavior is Stop.")]
    public void AMissingAssemblyAbortsWhenStopping() {
        // Setup
        SolutionGenerationOptions options = new() { FailureBehavior = FailureBehavior.Stop };

        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { "this-assembly-does-not-exist.dll" }, options))
             .Throws<SolutionDocumentationGenerationException>();
    }

}
