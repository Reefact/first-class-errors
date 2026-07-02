#region Usings declarations

using FirstClassErrors;
using FirstClassErrors.Cli;
using FirstClassErrors.GenDoc;
using FirstClassErrors.GenDoc.Rendering;

#endregion

try {
    CliArguments cli = CliArguments.Parse(args);

    if (cli.ShowHelp) {
        Console.WriteLine(CliArguments.HelpText);
        return 0;
    }

    cli.Validate();

    ConsoleGenerationLogger logger = new(cli.Verbose);

    SolutionGenerationOptions options = new() {
        BuildSolution      = cli.Build,
        Configuration      = cli.Configuration,
        TargetFramework    = cli.Framework,
        FailureBehavior    = cli.Strict ? FailureBehavior.Stop : FailureBehavior.Continue,
        WorkerAssemblyPath = cli.WorkerPath,
        Logger             = logger
    };

    IEnumerable<ErrorDocumentation> catalog =
        string.IsNullOrWhiteSpace(cli.SolutionPath) is false
            ? SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(cli.SolutionPath!, options)
            : SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(cli.AssemblyPaths, options);

    IErrorDocumentationRenderer renderer = cli.Format switch {
        "json" => new JsonErrorDocumentationRenderer(),
        _      => throw new ArgumentException($"Unsupported format '{cli.Format}'.")
    };

    // The catalog is enumerated here, inside the try, so generation failures surface as a clean error message.
    string rendered = renderer.Render(catalog);

    if (string.IsNullOrWhiteSpace(cli.OutputPath)) {
        Console.WriteLine(rendered);
    } else {
        string  outputPath = Path.GetFullPath(cli.OutputPath!);
        string? directory  = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrEmpty(directory) is false) {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, rendered);
        logger.Info($"Documentation written to '{outputPath}'.");
    }

    return 0;
} catch (ArgumentException exception) {
    // Usage errors: point the user back to --help.
    Console.Error.WriteLine($"error: {exception.Message}");
    Console.Error.WriteLine("Run with --help for usage.");
    return 1;
} catch (Exception exception) {
    Console.Error.WriteLine($"error: {exception.Message}");
    return 1;
}
