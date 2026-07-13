#region Usings declarations

using System.Globalization;

using FirstClassErrors.GenDoc;
using FirstClassErrors.GenDoc.Versioning;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Extracts the canonical <see cref="CatalogSnapshot" /> for the <c>catalog</c> commands: resolves the source
///     (command line first, then configuration), runs the documentation extraction, and projects the resulting
///     catalog into its contract snapshot.
/// </summary>
internal static class CatalogSnapshotSource {

    #region Statics members declarations

    // The snapshot is always extracted under the "en" culture (the same built-in default as `generate`) so a
    // committed baseline never depends on the ambient culture of the machine that produced it.
    private static readonly CultureInfo SnapshotCulture = CultureInfo.GetCultureInfo("en");

    /// <summary>
    ///     Extracts the snapshot of the catalog as it currently stands in the configured source.
    /// </summary>
    /// <param name="settings">The catalog command options.</param>
    /// <param name="configuration">The loaded configuration file.</param>
    /// <param name="logger">The logger diagnostics are reported to.</param>
    /// <returns>The canonical snapshot of the current catalog.</returns>
    public static CatalogSnapshot Extract(CatalogSettings settings, CliConfiguration configuration, ConsoleGenerationLogger logger) {
        (string? solution, string[] assemblies) = CatalogSourceResolver.Resolve(settings.SolutionPath, settings.AssemblyPaths, configuration);

        string  buildConfig = CatalogSourceResolver.FirstNonEmpty(settings.Configuration, configuration.Configuration) ?? "Debug";
        string? framework   = CatalogSourceResolver.FirstNonEmpty(settings.Framework, configuration.Framework);
        string? worker      = CatalogSourceResolver.FirstNonEmpty(settings.WorkerPath, configuration.Worker);
        bool    noBuild     = settings.NoBuild || (configuration.NoBuild ?? false);
        bool    strict      = settings.Strict  || (configuration.Strict ?? false);

        SolutionGenerationOptions options = new() {
            BuildSolution      = noBuild is false,
            Configuration      = buildConfig,
            TargetFramework    = framework,
            FailureBehavior    = strict ? FailureBehavior.Stop : FailureBehavior.Continue,
            WorkerAssemblyPath = worker,
            Culture            = SnapshotCulture,
            Logger             = logger
        };

        List<ErrorDocumentation> catalog =
            (solution is not null
                 ? SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(solution, options)
                 : SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(assemblies, options))
           .ToList();

        int uncoded = catalog.Count(error => string.IsNullOrWhiteSpace(error.Code));
        if (uncoded > 0) {
            logger.Warning($"{uncoded} documented error(s) have no code and cannot be tracked by the baseline.");
        }

        return CatalogSnapshot.FromCatalog(catalog);
    }

    #endregion

}
