#region Usings declarations

using System.ComponentModel;

using FirstClassErrors.GenDoc;
using FirstClassErrors.GenDoc.Versioning;

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>Settings for <c>fce catalog diff</c>.</summary>
internal sealed class CatalogDiffSettings : CatalogSettings {

    [CommandOption("--against <PATH>")]
    [Description("Compare the baseline against this snapshot file instead of extracting the catalog from the source.")]
    public string? AgainstPath { get; set; }

    [CommandOption("--fail-on <IMPACT>")]
    [Description("Exit with code 2 when changes at or above this impact exist: breaking, any or none. Default: breaking.")]
    public string? FailOn { get; set; }

    [CommandOption("--report <FORMAT>")]
    [Description("Report format written to standard output: text, markdown (alias: md) or json. Default: text.")]
    public string? Report { get; set; }

}

/// <summary>
///     Compares the current catalog against the committed baseline and reports every change, classified by impact
///     (breaking, compatible, informational). The report goes to standard output — so it can be piped or posted as a
///     pull-request comment — and diagnostics go to standard error.
/// </summary>
/// <remarks>
///     Exit codes: <c>0</c> when no change reaches the <c>--fail-on</c> threshold, <c>2</c> when at least one does,
///     and <c>1</c> on an execution error (missing baseline, failed extraction, …); <c>130</c> on cancellation. CI
///     pipelines can therefore distinguish "the contract changed" from "the tool failed".
/// </remarks>
internal sealed class CatalogDiffCommand : Command<CatalogDiffSettings> {

    #region Fields

    private readonly ICatalogSnapshotSource        _snapshotSource;
    private readonly Func<bool, IGenerationLogger> _loggerFactory;
    private readonly TextWriter                    _output;

    #endregion

    #region Constructors & Destructor

    /// <summary>Production constructor used by the CLI host: wires the real extraction pipeline, console logger and stdout.</summary>
    public CatalogDiffCommand() : this(
        new SolutionCatalogSnapshotSource(),
        verbose => new ConsoleGenerationLogger(verbose),
        Console.Out) { }

    /// <summary>Test seam: injects the collaborators so they can be substituted by fakes.</summary>
    internal CatalogDiffCommand(ICatalogSnapshotSource snapshotSource, Func<bool, IGenerationLogger> loggerFactory, TextWriter output) {
        _snapshotSource = snapshotSource;
        _loggerFactory  = loggerFactory;
        _output         = output;
    }

    #endregion

    protected override int Execute(CommandContext context, CatalogDiffSettings settings, CancellationToken cancellationToken) {
        // The command body uses no CommandContext state, so it lives in a context-free seam that tests drive directly.
        return Run(settings, cancellationToken);
    }

    internal int Run(CatalogDiffSettings settings, CancellationToken cancellationToken) {
        IGenerationLogger logger = _loggerFactory(settings.Verbose);

        try {
            string           configPath    = ConfigurationStore.Resolve(settings.ConfigPath);
            CliConfiguration configuration = ConfigurationStore.Load(configPath);
            string           configDir     = Path.GetDirectoryName(configPath) ?? Directory.GetCurrentDirectory();

            // Validate the policy and report options before the (expensive) extraction runs, so a typo fails fast.
            string failOn = (settings.FailOn ?? "breaking").Trim().ToLowerInvariant();
            if (failOn is not ("breaking" or "any" or "none")) {
                logger.Error($"Unknown --fail-on value '{settings.FailOn}'. Use breaking, any or none.");

                return 1;
            }

            string report = NormalizeReport(settings.Report ?? "text");
            if (report is not ("text" or "markdown" or "json")) {
                logger.Error($"Unknown --report value '{settings.Report}'. Use text, markdown (alias: md) or json.");

                return 1;
            }

            string baselinePath = BaselineStore.Resolve(settings.BaselinePath, configuration.Baseline, configDir);
            if (BaselineStore.Exists(baselinePath) is false) {
                logger.Error($"No baseline at '{baselinePath}'. Run 'fce catalog update' to create it.");

                return 1;
            }

            CatalogSnapshot baseline = BaselineStore.Load(baselinePath);
            CatalogSnapshot current  = string.IsNullOrWhiteSpace(settings.AgainstPath)
                                           ? _snapshotSource.Extract(settings, configuration, logger, cancellationToken)
                                           : BaselineStore.Load(Path.GetFullPath(settings.AgainstPath));

            CatalogDiff diff = CatalogDiffer.Diff(baseline, current);

            _output.Write(report switch {
                "markdown" => CatalogDiffFormatter.ToMarkdown(diff),
                "json"     => CatalogDiffFormatter.ToJson(diff),
                _          => CatalogDiffFormatter.ToText(diff)
            });

            bool violated = failOn switch {
                "breaking" => diff.HasChangesAtOrAbove(CatalogChangeImpact.Breaking),
                "any"      => diff.IsEmpty is false,
                _          => false
            };

            if (violated) {
                logger.Error(failOn == "breaking"
                                 ? $"The catalog has {diff.BreakingChanges.Count} breaking change(s) against the baseline. Fix them, or accept them deliberately with 'fce catalog update'."
                                 : $"The catalog has {diff.Changes.Count} change(s) against the baseline. Accept them with 'fce catalog update'.");

                return 2;
            }

            return 0;
        } catch (OperationCanceledException) {
            // Cancellation (Ctrl+C) is an abort, not a failure: report it with the conventional SIGINT exit code.
            logger.Error("Catalog diff canceled.");

            return 130;
        } catch (DiagnosableException exception) {
            return FailureReporting.ReportCodedFailure(logger, exception);
        } catch (Exception exception) {
            // Report expected failures (missing solution, worker crash, invalid baseline, …) as a terse line.
            logger.Error(exception.Message);
            logger.Debug(exception.ToString());

            return 1;
        }
    }

    private static string NormalizeReport(string report) {
        string normalized = report.Trim().ToLowerInvariant();

        return normalized == "md" ? "markdown" : normalized;
    }

}
