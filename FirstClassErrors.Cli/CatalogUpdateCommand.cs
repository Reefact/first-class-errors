#region Usings declarations

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

    protected override int Execute(CommandContext context, CatalogUpdateSettings settings, CancellationToken cancellationToken) {
        ConsoleGenerationLogger logger = new(settings.Verbose);

        try {
            string           configPath    = ConfigurationStore.Resolve(settings.ConfigPath);
            CliConfiguration configuration = ConfigurationStore.Load(configPath);
            string           configDir     = Path.GetDirectoryName(configPath) ?? Directory.GetCurrentDirectory();

            CatalogSnapshot current      = CatalogSnapshotSource.Extract(settings, configuration, logger);
            string          baselinePath = BaselineStore.Resolve(settings.BaselinePath, configuration.Baseline, configDir);

            if (BaselineStore.Exists(baselinePath) is false) {
                BaselineStore.Save(baselinePath, current);
                Console.Out.WriteLine($"Baseline created at '{baselinePath}', tracking {current.Errors.Count} error(s).");

                return 0;
            }

            string existingText = File.ReadAllText(baselinePath);
            string canonical    = CatalogSnapshotSerializer.Serialize(current);
            if (string.Equals(existingText, canonical, StringComparison.Ordinal)) {
                Console.Out.WriteLine($"Baseline at '{baselinePath}' is already up to date ({current.Errors.Count} error(s)).");

                return 0;
            }

            // Summarize what the refresh absorbs, so accepting a breaking change is a visible, reviewable act.
            CatalogDiff diff = CatalogDiffer.Diff(CatalogSnapshotSerializer.Deserialize(existingText), current);
            BaselineStore.Save(baselinePath, current);

            Console.Out.WriteLine(diff.IsEmpty
                                      ? $"Baseline rewritten in canonical form at '{baselinePath}' ({current.Errors.Count} error(s))."
                                      : $"Baseline updated at '{baselinePath}': {diff.BreakingChanges.Count} breaking, {diff.CompatibleChanges.Count} compatible and {diff.InformationalChanges.Count} documentation change(s) accepted.");

            return 0;
        } catch (Exception exception) {
            // Report expected failures (missing solution, worker crash, invalid baseline, …) as a terse line.
            logger.Error(exception.Message);

            return 1;
        }
    }

}
