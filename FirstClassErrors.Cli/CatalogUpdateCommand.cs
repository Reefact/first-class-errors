#region Usings declarations

using FirstClassErrors.GenDoc;
using FirstClassErrors.GenDoc.Versioning;

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>Settings for <c>fce catalog update</c>.</summary>
internal sealed class CatalogUpdateSettings : CatalogSettings { }

/// <summary>
///     Creates or refreshes the catalog baseline: extracts the current catalog, projects it into its canonical
///     contract snapshot, and writes it to the baseline file. Updating the baseline is the <b>deliberate act</b> of
///     accepting the current contract — including any breaking change — so the file is meant to be committed and the
///     change reviewed like any other contract change.
/// </summary>
internal sealed class CatalogUpdateCommand : Command<CatalogUpdateSettings> {

    #region Fields

    private readonly ICatalogSnapshotSource        _snapshotSource;
    private readonly Func<bool, IGenerationLogger> _loggerFactory;
    private readonly TextWriter                    _output;

    #endregion

    #region Constructors & Destructor

    /// <summary>Production constructor used by the CLI host: wires the real extraction pipeline, console logger and stdout.</summary>
    public CatalogUpdateCommand() : this(
        new SolutionCatalogSnapshotSource(),
        verbose => new ConsoleGenerationLogger(verbose),
        Console.Out) { }

    /// <summary>Test seam: injects the collaborators so they can be substituted by fakes.</summary>
    internal CatalogUpdateCommand(ICatalogSnapshotSource snapshotSource, Func<bool, IGenerationLogger> loggerFactory, TextWriter output) {
        _snapshotSource = snapshotSource;
        _loggerFactory  = loggerFactory;
        _output         = output;
    }

    #endregion

    protected override int Execute(CommandContext context, CatalogUpdateSettings settings, CancellationToken cancellationToken) {
        // The command body uses no CommandContext state, so it lives in a context-free seam that tests drive directly.
        return Run(settings, cancellationToken);
    }

    internal int Run(CatalogUpdateSettings settings, CancellationToken cancellationToken) {
        IGenerationLogger logger = _loggerFactory(settings.Verbose);

        try {
            string           configPath    = ConfigurationStore.Resolve(settings.ConfigPath);
            CliConfiguration configuration = ConfigurationStore.Load(configPath);
            string           configDir     = Path.GetDirectoryName(configPath) ?? Directory.GetCurrentDirectory();

            CatalogSnapshot current      = _snapshotSource.Extract(settings, configuration, logger, cancellationToken);
            string          baselinePath = BaselineStore.Resolve(settings.BaselinePath, configuration.Baseline, configDir);

            if (BaselineStore.Exists(baselinePath) is false) {
                BaselineStore.Save(baselinePath, current);
                _output.WriteLine($"Baseline created at '{baselinePath}', tracking {current.Errors.Count} error(s).");

                return 0;
            }

            string existingText = File.ReadAllText(baselinePath);
            string canonical    = CatalogSnapshotSerializer.Serialize(current);
            if (string.Equals(existingText, canonical, StringComparison.Ordinal)) {
                _output.WriteLine($"Baseline at '{baselinePath}' is already up to date ({current.Errors.Count} error(s)).");

                return 0;
            }

            // Summarize what the refresh absorbs, so accepting a breaking change is a visible, reviewable act. An
            // existing baseline that cannot be read (corrupt, or written by a newer schema) must not block the
            // rewrite — updating is precisely how you regenerate it — so fall back to a plain rewrite with a warning.
            CatalogDiff? diff = null;
            try {
                diff = CatalogDiffer.Diff(CatalogSnapshotSerializer.Deserialize(existingText), current);
            } catch (InvalidOperationException exception) {
                logger.Warning($"The existing baseline at '{baselinePath}' could not be read ({exception.Message}); rewriting it.");
            }

            BaselineStore.Save(baselinePath, current);

            if (diff is null) {
                _output.WriteLine($"Baseline rewritten at '{baselinePath}' ({current.Errors.Count} error(s)); the previous file was unreadable.");
            } else if (diff.IsEmpty) {
                _output.WriteLine($"Baseline rewritten in canonical form at '{baselinePath}' ({current.Errors.Count} error(s)).");
            } else {
                _output.WriteLine($"Baseline updated at '{baselinePath}': {diff.BreakingChanges.Count} breaking, {diff.CompatibleChanges.Count} compatible and {diff.InformationalChanges.Count} documentation change(s) accepted.");
            }

            return 0;
        } catch (OperationCanceledException) {
            // Cancellation (Ctrl+C) is an abort, not a failure: the child processes are already killed through the
            // token, so report it with the conventional SIGINT exit code (128 + 2) rather than a generic error.
            logger.Error("Catalog update canceled.");

            return 130;
        } catch (Exception exception) {
            // Report expected failures (missing solution, worker crash, …) as a terse line, not a stack trace.
            logger.Error(exception.Message);
            logger.Debug(exception.ToString());

            return 1;
        }
    }

}
