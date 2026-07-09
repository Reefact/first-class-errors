#region Usings declarations

using System.Globalization;

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

    #region Fields

    private readonly IErrorDocumentationGenerator      _generator;
    private readonly DocumentationOutputWriter         _outputWriter;
    private readonly Func<bool, IGenerationLogger>     _loggerFactory;

    #endregion

    #region Constructors & Destructor

    /// <summary>Production constructor used by the CLI host: wires the real pipeline, output sink and console logger.</summary>
    public GenerateCommand() : this(
        new SolutionErrorDocumentationGeneratorAdapter(),
        new ConsoleAndFileOutputSink(),
        verbose => new ConsoleGenerationLogger(verbose)) { }

    /// <summary>Test seam: injects the collaborators so they can be substituted by fakes.</summary>
    internal GenerateCommand(IErrorDocumentationGenerator generator, IOutputSink outputSink, Func<bool, IGenerationLogger> loggerFactory) {
        _generator     = generator;
        _outputWriter  = new DocumentationOutputWriter(outputSink);
        _loggerFactory = loggerFactory;
    }

    #endregion

    protected override int Execute(CommandContext context, GenerateSettings settings, CancellationToken cancellationToken) {
        // The command body uses no CommandContext state, so it lives in a context-free seam that tests can drive
        // directly (they need only build a GenerateSettings, not a Spectre CommandContext).
        return Run(settings, cancellationToken);
    }

    internal int Run(GenerateSettings settings, CancellationToken cancellationToken) {
        IGenerationLogger logger = _loggerFactory(settings.Verbose);

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
            string? serviceName = FirstNonEmpty(settings.ServiceName, configuration.ServiceName);
            bool    noBuild     = settings.NoBuild || (configuration.NoBuild ?? false);
            bool    strict      = settings.Strict  || (configuration.Strict ?? false);

            // The language drives both the extraction (localized error descriptions) and the rendering (localized
            // template boilerplate). It defaults to English.
            CultureInfo culture = ResolveCulture(FirstNonEmpty(settings.Language, configuration.Language) ?? "en");

            // Resolve the renderer and validate the requested layout against what it actually supports, before the
            // (expensive) extraction runs — so a bad --format/--layout fails fast. Custom renderers referenced by the
            // configuration are loaded and offered alongside the built-in ones.
            IReadOnlyList<IErrorDocumentationRenderer> customRenderers = RendererLoader.Load(configuration.Renderers, configDir, logger);
            IErrorDocumentationRenderer                renderer        = RendererCatalog.Create(format, customRenderers);

            if (renderer.SupportedLayouts.Contains(layout, StringComparer.OrdinalIgnoreCase) is false) {
                logger.Error($"The '{format}' format does not support the '{layout}' layout. Supported layouts: {string.Join(", ", renderer.SupportedLayouts)}.");

                return 1;
            }

            // The markdown/html formats embed RFC 9457 examples whose problem type is urn:problem:{service}:{code}. The
            // service segment cannot be invented, so require it (from --service-name or the configuration) rather than
            // emit a type-less example. The json format carries no such example and is exempt.
            if ((format is "markdown" or "html") && string.IsNullOrWhiteSpace(serviceName)) {
                logger.Error($"No service name: the '{format}' format embeds RFC 9457 examples whose problem type is urn:problem:{{service}}:{{code}}. Pass --service-name <name> (for example --service-name temperature-simulator), or set 'serviceName' in the configuration.");

                return 1;
            }

            SolutionGenerationOptions options = new() {
                BuildSolution      = noBuild is false,
                Configuration      = buildConfig,
                TargetFramework    = framework,
                FailureBehavior    = strict ? FailureBehavior.Stop : FailureBehavior.Continue,
                WorkerAssemblyPath = worker,
                Culture            = culture,
                Logger             = logger,
                CancellationToken  = cancellationToken
            };

            IEnumerable<ErrorDocumentation> catalog =
                hasSolution
                    ? _generator.GetErrorDocumentationFrom(solution!, options)
                    : _generator.GetErrorDocumentationFromAssemblies(assemblies, options);

            // The catalog is enumerated here (by the renderer), so generation failures surface as a clean error.
            RenderRequest                   request   = new(layout, culture, serviceName);
            IReadOnlyList<RenderedDocument> documents = renderer.Render(catalog, request);

            _outputWriter.Write(documents, renderer.Format, output, logger);

            return 0;
        } catch (OperationCanceledException) {
            // Cancellation (Ctrl+C) is an abort, not a failure: the child processes are already killed through the
            // token, so report it as its own concise line and the conventional SIGINT exit code (128 + 2) rather than a
            // generic error.
            logger.Error("Generation canceled.");

            return 130;
        } catch (Exception exception) {
            // Report expected failures (missing solution, worker crash, …) as a terse line, not a stack trace. The full
            // exception (type, stack trace, inner exceptions) goes to the debug channel, which surfaces only under
            // --verbose, so it is available for diagnosis without cluttering the default output.
            logger.Error(exception.Message);
            logger.Debug(exception.ToString());

            return 1;
        }
    }

    #region Helpers

    internal static string? FirstNonEmpty(string? primary, string? fallback) {
        if (string.IsNullOrWhiteSpace(primary) is false) { return primary; }
        if (string.IsNullOrWhiteSpace(fallback) is false) { return fallback; }

        return null;
    }

    internal static string NormalizeFormat(string format) {
        string normalized = format.Trim().ToLowerInvariant();

        return normalized == "md" ? "markdown" : normalized;
    }

    internal static CultureInfo ResolveCulture(string language) {
        try {
            return CultureInfo.GetCultureInfo(language.Trim());
        } catch (CultureNotFoundException) {
            throw new InvalidOperationException($"Unknown language '{language}'. Use a culture name such as en, fr, es, de or sv.");
        }
    }

    #endregion

}
