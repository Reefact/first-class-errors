#region Usings declarations

using FirstClassErrors.GenDoc;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

[TestSubject(typeof(GenerateCommand))]
public sealed class GenerateCommandExecutionTests {

    [Fact(DisplayName = "Specifying both a solution and assemblies fails, is reported, and skips generation.")]
    public void SpecifyingBothSourcesFailsWithoutInvokingTheGenerator() {
        // Setup: a generator that fails the test if it is ever reached.
        ThrowingGenerator generator = new();
        RecordingLogger   logger    = new();
        GenerateCommand   command   = new(generator, new RecordingOutputSink(), _ => logger);
        GenerateSettings settings = new() {
            ConfigPath    = CliTestHelpers.NonExistentConfigPath(),
            SolutionPath  = "app.sln",
            AssemblyPaths = ["lib.dll"]
        };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify: the mutually-exclusive sources are rejected (with a message) before any generation is attempted.
        Check.That(exitCode).IsEqualTo(1);
        Check.That(generator.WasInvoked).IsFalse();
        Check.That(logger.Errors).HasSize(1);
    }

    [Fact(DisplayName = "Specifying no source fails without invoking the generator.")]
    public void SpecifyingNoSourceFailsWithoutInvokingTheGenerator() {
        // Setup
        ThrowingGenerator generator = new();
        GenerateCommand   command   = new(generator, new RecordingOutputSink(), _ => new RecordingLogger());
        GenerateSettings  settings  = new() { ConfigPath = CliTestHelpers.NonExistentConfigPath() };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify
        Check.That(exitCode).IsEqualTo(1);
        Check.That(generator.WasInvoked).IsFalse();
    }

    [Theory(DisplayName = "The command builds its logger from the --verbose flag.")]
    [InlineData(true)]
    [InlineData(false)]
    public void TheCommandBuildsItsLoggerFromTheVerboseFlag(bool verbose) {
        // Setup: capture the flag the logger factory is invoked with. The no-source path still builds the logger first.
        bool?           capturedVerbose = null;
        GenerateCommand command = new(
            new StubGenerator(),
            new RecordingOutputSink(),
            flag => {
                capturedVerbose = flag;

                return new RecordingLogger();
            });
        GenerateSettings settings = new() { ConfigPath = CliTestHelpers.NonExistentConfigPath(), Verbose = verbose };

        // Exercise
        command.Run(settings, CancellationToken.None);

        // Verify
        Check.That(capturedVerbose).IsEqualTo(verbose);
    }

    [Fact(DisplayName = "A cancelled generation is reported and returns the SIGINT exit code (130).")]
    public void ACancelledGenerationReturnsTheSigintExitCode() {
        // Setup: a JSON generation from a solution reaches the generator (no service name needed for json), which
        // reports cancellation.
        RecordingLogger logger  = new();
        GenerateCommand command = new(new CancellingGenerator(), new RecordingOutputSink(), _ => logger);
        GenerateSettings settings = new() {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            SolutionPath = "app.sln",
            Format       = "json"
        };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify
        Check.That(exitCode).IsEqualTo(130);
        Check.That(logger.Errors).Contains("Generation canceled.");
    }

}
