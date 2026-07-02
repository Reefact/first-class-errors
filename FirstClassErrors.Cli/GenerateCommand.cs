#region Usings declarations

using FirstClassErrors;
using FirstClassErrors.GenDoc;
using FirstClassErrors.GenDoc.Rendering;

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Generates the error-documentation catalog from a solution or from pre-built assemblies and renders it to the
///     requested format. Diagnostic logging goes to standard error; the rendered document goes to the chosen output
///     (a file or standard output), so the tool can be piped.
/// </summary>
internal sealed class GenerateCommand : Command<GenerateSettings> {

    protected override int Execute(CommandContext context, GenerateSettings settings, CancellationToken cancellationToken) {
        ConsoleGenerationLogger logger = new(settings.Verbose);

        try {
            SolutionGenerationOptions options = new() {
                BuildSolution      = settings.NoBuild is false,
                Configuration      = settings.Configuration,
                TargetFramework    = settings.Framework,
                FailureBehavior    = settings.Strict ? FailureBehavior.Stop : FailureBehavior.Continue,
                WorkerAssemblyPath = settings.WorkerPath,
                Logger             = logger
            };

            IEnumerable<ErrorDocumentation> catalog =
                string.IsNullOrWhiteSpace(settings.SolutionPath) is false
                    ? SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(settings.SolutionPath!, options)
                    : SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(settings.AssemblyPaths, options);

            IErrorDocumentationRenderer renderer = CreateRenderer(settings);

            // The catalog is enumerated here (by the renderer), so generation failures surface as a clean error.
            IReadOnlyList<RenderedDocument> documents = renderer.Render(catalog);

            WriteOutput(documents, settings.OutputPath, logger);

            return 0;
        } catch (Exception exception) {
            // Report expected failures (missing solution, worker crash, …) as a terse line, not a stack trace.
            logger.Error(exception.Message);

            return 1;
        }
    }

    #region Helpers

    private static IErrorDocumentationRenderer CreateRenderer(GenerateSettings settings) {
        return settings.NormalizedFormat() switch {
            "json"     => new JsonErrorDocumentationRenderer(),
            "markdown" => new MarkdownErrorDocumentationRenderer(
                              settings.NormalizedLayout() == "split" ? MarkdownLayout.Split : MarkdownLayout.Single),
            _ => throw new InvalidOperationException($"Unsupported format '{settings.Format}'.")
        };
    }

    private static void WriteOutput(IReadOnlyList<RenderedDocument> documents, string? outputPath, ConsoleGenerationLogger logger) {
        bool hasOutput = string.IsNullOrWhiteSpace(outputPath) is false;

        // No target: only a single document can go to standard output.
        if (hasOutput is false) {
            if (documents.Count > 1) {
                throw new InvalidOperationException("This layout produces several files; specify an output directory with --output.");
            }

            Console.Out.WriteLine(documents[0].Content);

            return;
        }

        string fullOutput = Path.GetFullPath(outputPath!);

        // Treat the target as a directory when there are several files, when it already exists as one, or when the
        // path ends with a separator. Otherwise a single document is written to the given file path verbatim.
        bool asDirectory = documents.Count > 1 || Directory.Exists(fullOutput) || EndsWithSeparator(outputPath!);
        if (asDirectory is false) {
            WriteFile(fullOutput, documents[0].Content);
            logger.Info($"Documentation written to '{fullOutput}'.");

            return;
        }

        foreach (RenderedDocument document in documents) {
            WriteFile(Path.Combine(fullOutput, document.RelativePath), document.Content);
        }

        logger.Info($"Documentation written to '{fullOutput}' ({documents.Count} file(s)).");
    }

    private static void WriteFile(string path, string content) {
        string? directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory) is false) {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, content);
    }

    private static bool EndsWithSeparator(string path) {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar);
    }

    #endregion

}
