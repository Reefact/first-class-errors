#region Usings declarations

using FirstClassErrors.GenDoc;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

/// <summary>
///     Drives GenerateCommand.Run end to end with a fake generator and a recording sink, but through the real
///     configuration store and the real renderers. This exercises the command's wiring — source routing, option
///     mapping, format/layout validation and output routing — without spawning any process or touching the console.
/// </summary>
[TestSubject(typeof(GenerateCommand))]
public sealed class GenerateCommandEndToEndTests {

    [Fact(DisplayName = "A JSON generation from a solution routes to the solution source and writes to standard output.")]
    public void JsonFromSolutionRoutesToTheSolutionSource() {
        // Setup
        RecordingGenerator generator = new(Sample());
        RecordingOutputSink sink     = new();
        GenerateCommand command       = new(generator, sink, _ => new RecordingLogger());
        GenerateSettings settings = new() {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            SolutionPath = "app.sln",
            Format       = "json"
        };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify
        Check.That(exitCode).IsEqualTo(0);
        Check.That(generator.SolutionPath).IsEqualTo("app.sln");
        Check.That(generator.AssemblyPaths).IsNull();
        Check.That(sink.StandardOutput).HasSize(1);
    }

    [Fact(DisplayName = "A JSON generation from assemblies routes to the assemblies source.")]
    public void JsonFromAssembliesRoutesToTheAssembliesSource() {
        // Setup
        RecordingGenerator generator = new(Sample());
        GenerateCommand    command   = new(generator, new RecordingOutputSink(), _ => new RecordingLogger());
        GenerateSettings settings = new() {
            ConfigPath    = CliTestHelpers.NonExistentConfigPath(),
            AssemblyPaths = ["a.dll", "b.dll"],
            Format        = "json"
        };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify
        Check.That(exitCode).IsEqualTo(0);
        Check.That(generator.AssemblyPaths).ContainsExactly("a.dll", "b.dll");
        Check.That(generator.SolutionPath).IsNull();
    }

    [Fact(DisplayName = "Command-line options are mapped onto the generation options.")]
    public void CommandLineOptionsAreMappedOntoTheGenerationOptions() {
        // Setup
        RecordingGenerator generator = new(Sample());
        GenerateCommand    command   = new(generator, new RecordingOutputSink(), _ => new RecordingLogger());
        GenerateSettings settings = new() {
            ConfigPath    = CliTestHelpers.NonExistentConfigPath(),
            SolutionPath  = "app.sln",
            Format        = "json",
            NoBuild       = true,
            Strict        = true,
            Configuration = "Release",
            Language      = "fr"
        };

        using CancellationTokenSource cts = new();

        // Exercise
        command.Run(settings, cts.Token);

        // Verify
        SolutionGenerationOptions options = generator.Options!;
        Check.That(options.BuildSolution).IsFalse();                        // --no-build
        Check.That(options.FailureBehavior).IsEqualTo(FailureBehavior.Stop); // --strict
        Check.That(options.Configuration).IsEqualTo("Release");
        Check.That(options.Culture!.Name).IsEqualTo("fr");
        Check.That(options.CancellationToken).IsEqualTo(cts.Token);
    }

    [Fact(DisplayName = "A single document with an output file is written to that file, not to standard output.")]
    public void ASingleDocumentWithAnOutputFileIsWrittenToDisk() {
        // Setup
        RecordingGenerator  generator  = new(Sample());
        RecordingOutputSink sink       = new();
        GenerateCommand     command    = new(generator, sink, _ => new RecordingLogger());
        string              outputPath = Path.Combine(Path.GetTempPath(), $"fce-e2e-{Guid.NewGuid():N}.json");
        GenerateSettings settings = new() {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            SolutionPath = "app.sln",
            Format       = "json",
            OutputPath   = outputPath
        };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify
        Check.That(exitCode).IsEqualTo(0);
        Check.That(sink.Files.Keys).ContainsExactly(Path.GetFullPath(outputPath));
        Check.That(sink.StandardOutput).IsEmpty();
    }

    [Fact(DisplayName = "The markdown format requires a service name, and rejects the run before generating when it is missing.")]
    public void MarkdownWithoutAServiceNameIsRejectedBeforeGenerating() {
        // Setup
        RecordingGenerator generator = new(Sample());
        GenerateCommand    command   = new(generator, new RecordingOutputSink(), _ => new RecordingLogger());
        GenerateSettings settings = new() {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            SolutionPath = "app.sln",
            Format       = "markdown"
        };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify
        Check.That(exitCode).IsEqualTo(1);
        Check.That(generator.SolutionPath).IsNull();
    }

    [Fact(DisplayName = "The 'md' alias resolves to markdown and renders when a service name is supplied.")]
    public void TheMdAliasRendersMarkdown() {
        // Setup
        RecordingGenerator  generator = new(Sample());
        RecordingOutputSink sink      = new();
        GenerateCommand     command   = new(generator, sink, _ => new RecordingLogger());
        GenerateSettings settings = new() {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            SolutionPath = "app.sln",
            Format       = "md",
            ServiceName  = "temperature-simulator"
        };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify: the single-layout markdown produces one document, sent to standard output.
        Check.That(exitCode).IsEqualTo(0);
        Check.That(sink.StandardOutput).HasSize(1);
    }

    [Fact(DisplayName = "An unsupported layout is rejected before generating.")]
    public void AnUnsupportedLayoutIsRejectedBeforeGenerating() {
        // Setup: json only supports the 'single' layout.
        RecordingGenerator generator = new(Sample());
        GenerateCommand    command   = new(generator, new RecordingOutputSink(), _ => new RecordingLogger());
        GenerateSettings settings = new() {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            SolutionPath = "app.sln",
            Format       = "json",
            Layout       = "split"
        };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify
        Check.That(exitCode).IsEqualTo(1);
        Check.That(generator.SolutionPath).IsNull();
    }

    [Fact(DisplayName = "A failing generation is reported tersely, with the full exception on the debug channel.")]
    public void AFailingGenerationIsReportedWithFullDetailOnDebug() {
        // Setup
        RecordingLogger logger  = new();
        GenerateCommand command = new(
            new FailingGenerator(new InvalidOperationException("boom")),
            new RecordingOutputSink(),
            _ => logger);
        GenerateSettings settings = new() {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            SolutionPath = "app.sln",
            Format       = "json"
        };

        // Exercise
        int exitCode = command.Run(settings, CancellationToken.None);

        // Verify: terse message on the error channel, full exception on the (verbose-only) debug channel.
        Check.That(exitCode).IsEqualTo(1);
        Check.That(logger.Errors).Contains("boom");
        Check.That(logger.Debugs).HasSize(1);
    }

    #region Helpers

    private static ErrorDocumentation Sample() {
        return new ErrorDocumentation { Code = "SAMPLE_CODE", Title = "Sample error" };
    }

    #endregion

}
