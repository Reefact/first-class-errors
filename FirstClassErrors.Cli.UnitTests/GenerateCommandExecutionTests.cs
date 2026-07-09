#region Usings declarations

using FirstClassErrors.GenDoc;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

[TestSubject(typeof(GenerateCommand))]
public sealed class GenerateCommandExecutionTests {

    [Fact(DisplayName = "Specifying both a solution and assemblies fails without invoking the generator.")]
    public void SpecifyingBothSourcesFailsWithoutInvokingTheGenerator() {
        // Setup: a generator that fails the test if it is ever reached.
        ThrowingGenerator generator = new();
        GenerateCommand   command   = new(generator, new RecordingOutputSink());
        GenerateSettings settings = new() {
            ConfigPath    = NonExistentConfigPath(),
            SolutionPath  = "app.sln",
            AssemblyPaths = ["lib.dll"]
        };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify: the mutually-exclusive sources are rejected before any generation is attempted.
        Check.That(exitCode).IsEqualTo(1);
        Check.That(generator.WasInvoked).IsFalse();
    }

    [Fact(DisplayName = "Specifying no source fails without invoking the generator.")]
    public void SpecifyingNoSourceFailsWithoutInvokingTheGenerator() {
        // Setup
        ThrowingGenerator generator = new();
        GenerateCommand   command   = new(generator, new RecordingOutputSink());
        GenerateSettings  settings  = new() { ConfigPath = NonExistentConfigPath() };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify
        Check.That(exitCode).IsEqualTo(1);
        Check.That(generator.WasInvoked).IsFalse();
    }

    #region Helpers

    // A path that is guaranteed not to exist, so ConfigurationStore.Load returns an empty configuration and the test
    // is independent of any fce.json in the working directory.
    private static string NonExistentConfigPath() {
        return Path.Combine(Path.GetTempPath(), $"fce-absent-config-{Guid.NewGuid():N}.json");
    }

    #endregion

    #region Nested types declarations

    private sealed class ThrowingGenerator : IErrorDocumentationGenerator {

        public bool WasInvoked { get; private set; }

        public IEnumerable<ErrorDocumentation> GetErrorDocumentationFrom(string solutionPath, SolutionGenerationOptions options) {
            WasInvoked = true;

            throw new InvalidOperationException("The generator must not be invoked on this path.");
        }

        public IEnumerable<ErrorDocumentation> GetErrorDocumentationFromAssemblies(IReadOnlyList<string> assemblyPaths, SolutionGenerationOptions options) {
            WasInvoked = true;

            throw new InvalidOperationException("The generator must not be invoked on this path.");
        }

    }

    #endregion

}
