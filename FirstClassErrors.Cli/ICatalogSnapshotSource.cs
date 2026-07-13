#region Usings declarations

using FirstClassErrors.GenDoc;
using FirstClassErrors.GenDoc.Versioning;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     The catalog commands' port over the extraction-plus-projection pipeline: it turns the configured source into
///     the canonical <see cref="CatalogSnapshot" />. The commands depend on this abstraction rather than on the static
///     generator, so tests substitute a fake and exercise the command wiring (exit codes, report routing, baseline
///     handling) without spawning real <c>dotnet</c> processes.
/// </summary>
internal interface ICatalogSnapshotSource {

    /// <summary>
    ///     Extracts the snapshot of the catalog as it currently stands in the configured source.
    /// </summary>
    /// <param name="settings">The catalog command options.</param>
    /// <param name="configuration">The loaded configuration file.</param>
    /// <param name="logger">The logger diagnostics are reported to.</param>
    /// <param name="cancellationToken">A token observed while the (out-of-process) extraction runs.</param>
    /// <returns>The canonical snapshot of the current catalog.</returns>
    CatalogSnapshot Extract(CatalogSettings settings, CliConfiguration configuration, IGenerationLogger logger, CancellationToken cancellationToken);

}
