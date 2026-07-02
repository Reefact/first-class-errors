#region Usings declarations

using FirstClassErrors;
using FirstClassErrors.GenDoc;
using FirstClassErrors.GenDoc.Rendering;

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Generates the error-documentation catalog from a solution or from pre-built assemblies and renders it to the
///     requested format. Options fall back to the configuration file (<c>fce.json</c>) and then to built-in defaults;
///     a command-line value overrides the configuration. Diagnostic logging goes to standard error and the rendered
///     document to the chosen output (a file, a directory, or standard output), so the tool can be piped.
/// </summary>
internal sealed class GenerateCommand : Command<GenerateSettings> {

    protected override int Execute(CommandContext context, GenerateSettings settings, CancellationToken cancellationToken) {
        ConsoleGenerationLogger logger = new(settings.Verbose);

        try {
            string           configPath    = ConfigurationStore.Resolve(settings.ConfigPath);
            CliConfiguration configuration = ConfigurationStore.Load(configPath);
            string           configDir     = Path.GetDirectoryName(configPath) ?? Directory.GetCurrentDirectory();

            // Effective source: a command-line source overrides the configured one wholesale, so the two are never
            // mixed (passing --assemblies does not combine with a configured 'solution').
            string?  solution;
            string[] assemblies;
            if (string.IsNullOrWhiteSpace(settings.SolutionPath) is false || settings.AssemblyPaths.Length > 0) {
                solution   = settings.SolutionPath;
                assemblies = settings.AssemblyPaths;
            } else {
                solution   = configuration.Solution;
                assemblies = configuration.Assemblies?.ToArray() ?? [];
            }

            bool hasSolution   = string.IsNullOrWhiteSpace(solution) is false;
            bool hasAssemblies = assemblies.Length > 0;
            if (hasSolution && hasAssemblies) {
                logger.Error("Specify either a solution or assemblies, not both.");

                return 1;
            }

            if (hasSolution is false && hasAssemblies is false) {
                logger.Error("No source: pass --solution/--assemblies, or set 'solution'/'assemblies' in the configuration.");

                return 1;
            }

            // Effective options: command line first, then configuration, then the built-in default.
            string  format      = NormalizeFormat(FirstNonEmpty(settings.Format, configuration.Format) ?? "json");
            string  layout      = (FirstNonEmpty(settings.Layout, configuration.Layout) ?? "single").Trim().ToLowerInvariant();
            string? output      = FirstNonEmpty(settings.OutputPath, configuration.Output);
            string  buildConfig = FirstNonEmpty(settings.Configuration, configuration.Configuration) ?? "Debug";
            string? framework   = FirstNonEmpty(settings.Framework, configuration.Framework);
            string? worker      = FirstNonEmpty(settings.WorkerPath, configuration.Worker);
            bool    noBuild     = settings.NoBuild || (configuration.NoBuild ?? false);
            bool    strict      = settings.Strict  || (configuration.Strict ?? false);

            if (layout is not ("single" or "split")) {
                logger.Error($"Unsupported layout '{layout}'. Supported layouts: single, split.");

                return 1;
            }

            if (layout == "split" && format != "markdown") {
                logger.Error("The 'split' layout is only supported for the markdown format.");

                return 1;
            }

            SolutionGenerationOptions options = new() {
                BuildSolution      = noBuild is false,
                Configuration      = buildConfig,
                TargetFramework    = framework,
                FailureBehavior    = strict ? FailureBehavior.Stop : FailureBehavior.Continue,
                WorkerAssemblyPath = worker,
                Logger             = logger
            };

            IEnumerable<ErrorDocumentation> catalog =
                hasSolution
                    ? SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(solution!, options)
                    : SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(assemblies, options);

            // Custom renderers referenced by the configuration are loaded and offered alongside the built-in ones.
            IReadOnlyList<IErrorDocumentationRenderer> customRenderers = RendererLoader.Load(configuration.Renderers, configDir, logger);
            IErrorDocumentationRenderer                renderer        = RendererCatalog.Create(format, layout, customRenderers);

            // The catalog is enumerated here (by the renderer), so generation failures surface as a clean error.
            IReadOnlyList<RenderedDocument> documents = renderer.Render(catalog);

            WriteOutput(documents, output, logger);

            return 0;
        } catch (Exception exception) {
            // Report expected failures (missing solution, worker crash, …) as a terse line, not a stack trace.
            logger.Error(exception.Message);

            return 1;
        }
    }

    #region Helpers

    private static string? FirstNonEmpty(string? primary, string? fallback) {
        if (string.IsNullOrWhiteSpace(primary) is false) { return primary; }
        if (string.IsNullOrWhiteSpace(fallback) is false) { return fallback; }

        return null;
    }

    private static string NormalizeFormat(string format) {
        string normalized = format.Trim().ToLowerInvariant();

        return normalized == "md" ? "markdown" : normalized;
    }

    private static void WriteOutput(IReadOnlyList<RenderedDocument> documents, string? outputPath, ConsoleGenerationLogger logger) {
        bool hasOutput = string.IsNullOrWhiteSpace(outputPath) is false;

        // No target: only a single document can go to standard output.
        if (hasOutput is false) {
            if (documents.Count > 1) {
                throw new InvalidOperationException("This layout produces several files; specify an output directory with --output (or 'output' in the configuration).");
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
