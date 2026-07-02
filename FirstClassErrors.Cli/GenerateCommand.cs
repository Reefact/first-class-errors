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

            IErrorDocumentationRenderer renderer = settings.Format.ToLowerInvariant() switch {
                "json" => new JsonErrorDocumentationRenderer(),
                _      => throw new InvalidOperationException($"Unsupported format '{settings.Format}'.")
            };

            // The catalog is enumerated here (by the renderer), so generation failures surface as a clean error.
            string rendered = renderer.Render(catalog);

            if (string.IsNullOrWhiteSpace(settings.OutputPath)) {
                Console.Out.WriteLine(rendered);
            } else {
                string  outputPath = Path.GetFullPath(settings.OutputPath!);
                string? directory  = Path.GetDirectoryName(outputPath);
                if (string.IsNullOrEmpty(directory) is false) {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(outputPath, rendered);
                logger.Info($"Documentation written to '{outputPath}'.");
            }

            return 0;
        } catch (Exception exception) {
            // Report expected failures (missing solution, worker crash, …) as a terse line, not a stack trace.
            logger.Error(exception.Message);

            return 1;
        }
    }

}
