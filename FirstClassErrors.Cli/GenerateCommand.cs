#region Usings declarations

using System.Globalization;

using FirstClassErrors;
using FirstClassErrors.GenDoc;
using FirstClassErrors.GenDoc.Rendering;
using FirstClassErrors.GenDoc.Versioning;

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

            // Effective options: command line first, then configuration, then the built-in default.
            ResolvedGenerateOptions resolved = GenerateOptionsResolver.Resolve(settings, configuration);

            if (resolved.HasSolution && resolved.HasAssemblies) {
                logger.Error("Specify either a solution or assemblies, not both.");

                return 1;
            }

            if (!resolved.HasSolution && !resolved.HasAssemblies) {
                logger.Error("No source: pass --solution/--assemblies, or set 'solution'/'assemblies' in the configuration.");

                return 1;
            }

            // The language drives both the extraction (localized error descriptions) and the rendering (localized
            // template boilerplate). It defaults to English.
            CultureInfo culture = ResolveCulture(resolved.Language);

            // Resolve the renderer and validate the requested layout against what it actually supports, before the
            // (expensive) extraction runs — so a bad --format/--layout fails fast. Custom renderers referenced by the
            // configuration are loaded and offered alongside the built-in ones.
            IReadOnlyList<IErrorDocumentationRenderer> customRenderers = RendererLoader.Load(configuration.Renderers, configDir, logger);
            IErrorDocumentationRenderer                renderer        = RendererCatalog.Create(resolved.Format, customRenderers);

            if (!renderer.SupportedLayouts.Contains(resolved.Layout, StringComparer.OrdinalIgnoreCase)) {
                logger.Error($"The '{resolved.Format}' format does not support the '{resolved.Layout}' layout. Supported layouts: {string.Join(", ", renderer.SupportedLayouts)}.");

                return 1;
            }

            // The markdown/html formats embed RFC 9457 examples whose problem type is urn:problem:{service}:{code}. The
            // service segment cannot be invented, so require it (from --service-name or the configuration) rather than
            // emit a type-less example. The json format carries no such example and is exempt.
            if ((resolved.Format is "markdown" or "html") && string.IsNullOrWhiteSpace(resolved.ServiceName)) {
                logger.Error($"No service name: the '{resolved.Format}' format embeds RFC 9457 examples whose problem type is urn:problem:{{service}}:{{code}}. Pass --service-name <name> (for example --service-name temperature-simulator), or set 'serviceName' in the configuration.");

                return 1;
            }

            SolutionGenerationOptions options = new() {
                BuildSolution      = !resolved.NoBuild,
                Configuration      = resolved.BuildConfiguration,
                TargetFramework    = resolved.Framework,
                FailureBehavior    = resolved.Strict ? FailureBehavior.Stop : FailureBehavior.Continue,
                WorkerAssemblyPath = resolved.WorkerPath,
                Culture            = culture,
                Logger             = logger,
                CancellationToken  = cancellationToken
            };

            // The catalog is materialized here, so generation failures surface as a clean error — and the same
            // catalog can feed both the renderer and the optional contract snapshot below.
            List<ErrorDocumentation> catalog =
                (resolved.HasSolution
                     ? _generator.GetErrorDocumentationFrom(resolved.Solution!, options)
                     : _generator.GetErrorDocumentationFromAssemblies(resolved.Assemblies, options))
               .ToList();

            RenderRequest                   request   = new(resolved.Layout, culture, resolved.ServiceName);
            IReadOnlyList<RenderedDocument> documents = renderer.Render(catalog, request);

            _outputWriter.Write(documents, renderer.Format, resolved.Output, logger);

            // The canonical snapshot is renderer-independent: whatever format is published for humans, the same
            // contract file can be produced for `fce catalog diff` and CI drift detection. A configured `snapshot`
            // path resolves relative to fce.json (like the baseline and the renderer references), while a --snapshot
            // command-line value resolves against the current directory. It reflects the render language; a
            // culture-independent baseline should come from `fce catalog update` (which always pins `en`), so warn
            // when the two would diverge.
            string? snapshotPath = ConfigRelativePath.Resolve(settings.SnapshotPath, configuration.Snapshot, configDir);
            if (snapshotPath is not null) {
                if (!IsEnglish(culture)) {
                    logger.Warning($"The snapshot reflects the '{culture.Name}' language (localized titles/sources). For a culture-independent baseline, use 'fce catalog update' or generate with --language en.");
                }

                WriteSnapshotFile(snapshotPath, CatalogSnapshotSerializer.Serialize(CatalogSnapshot.FromCatalog(catalog)));
                logger.Info($"Catalog snapshot written to '{snapshotPath}'.");
            }

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

    internal static CultureInfo ResolveCulture(string language) {
        try {
            return CultureInfo.GetCultureInfo(language.Trim());
        } catch (CultureNotFoundException) {
            throw new InvalidOperationException($"Unknown language '{language}'. Use a culture name such as en, fr, es, de or sv.");
        }
    }

    private static bool IsEnglish(CultureInfo culture) {
        // The canonical baseline (fce catalog update) pins "en"; a snapshot produced under English (or the invariant
        // culture) matches it, so no warning is needed. Any other language localizes titles/sources.
        return string.IsNullOrEmpty(culture.Name) || string.Equals(culture.TwoLetterISOLanguageName, "en", StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteSnapshotFile(string path, string content) {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) { Directory.CreateDirectory(directory); }

        File.WriteAllText(path, content);
    }

    #endregion

}
