namespace FirstClassErrors.GenDoc.Versioning;

/// <summary>
///     The canonical, machine-comparable projection of an error catalog: the part of the catalog that constitutes a
///     <b>public contract</b> — error codes and their declared context keys — plus the light documentation identity
///     (title, source) used to explain changes. It is the unit of comparison for catalog versioning, independent of
///     whatever renderer (JSON, Markdown, HTML, custom) is used to publish the human-facing documentation.
/// </summary>
/// <remarks>
///     <para>
///         Error codes leak outside the producing system: client applications branch on them, dashboards alert on
///         them, support procedures reference them. Removing or renaming a code is therefore a breaking change of the
///         same nature as removing a public API member. The snapshot materializes that contract as a small,
///         deterministic JSON document that can be committed as a <b>baseline</b> and diffed in CI (see
///         <see cref="CatalogDiffer" />).
///     </para>
///     <para>
///         The snapshot is deterministic: errors are ordered by code and context keys by name (ordinal), so the same
///         catalog always serializes to the same bytes and the committed baseline diffs cleanly in source control.
///     </para>
/// </remarks>
public sealed class CatalogSnapshot {

    #region Constants declarations

    /// <summary>
    ///     The schema version written by this library. A snapshot declaring a higher schema was produced by a newer
    ///     version of the tooling and is rejected at parse time rather than silently misread.
    /// </summary>
    public const int CurrentSchema = 1;

    #endregion

    #region Statics members declarations

    /// <summary>
    ///     Builds the canonical snapshot of the given catalog.
    /// </summary>
    /// <param name="catalog">The aggregated, deduplicated error documentation to project.</param>
    /// <returns>The deterministic snapshot of the catalog's contract.</returns>
    /// <remarks>
    ///     Entries without a code are skipped: a code is the identity under which a contract can be tracked, so an
    ///     uncoded entry has nothing to version. Should two entries still share a code (the extraction pipeline
    ///     already deduplicates), the first one in code order wins, deterministically. Context keys are deduplicated
    ///     by name within each error, keeping the first value type seen in name order.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog" /> is <c>null</c>.</exception>
    public static CatalogSnapshot FromCatalog(IEnumerable<ErrorDocumentation> catalog) {
        if (catalog is null) { throw new ArgumentNullException(nameof(catalog)); }

        List<CatalogSnapshotEntry> entries = catalog
                                            .Where(error => string.IsNullOrWhiteSpace(error.Code) is false)
                                             // Order by code, then a deterministic tie-breaker (source, then title), so
                                             // that when several factories share a code the surviving entry — group.First()
                                             // below — is chosen independently of the (reflection-driven) input order.
                                            .OrderBy(error => error.Code, StringComparer.Ordinal)
                                            .ThenBy(error => error.Source, StringComparer.Ordinal)
                                            .ThenBy(error => error.Title, StringComparer.Ordinal)
                                            .GroupBy(error => error.Code!.Trim(), StringComparer.Ordinal)
                                            .Select(group => ToEntry(group.Key, group.First()))
                                            .OrderBy(entry => entry.Code, StringComparer.Ordinal)
                                            .ToList();

        return new CatalogSnapshot { Schema = CurrentSchema, Errors = entries };
    }

    private static CatalogSnapshotEntry ToEntry(string code, ErrorDocumentation error) {
        List<CatalogSnapshotContextKey> contextKeys = error.Context
                                                           .Where(entry => string.IsNullOrWhiteSpace(entry.Key) is false)
                                                           .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                                                           .GroupBy(entry => entry.Key!.Trim(), StringComparer.Ordinal)
                                                           .Select(group => new CatalogSnapshotContextKey {
                                                                Key       = group.Key,
                                                                ValueType = group.First().ValueType
                                                            })
                                                           // Re-sort on the trimmed names: a padded key must not
                                                           // break the documented "ordered by Key (ordinal)" invariant.
                                                           .OrderBy(key => key.Key, StringComparer.Ordinal)
                                                           .ToList();

        return new CatalogSnapshotEntry {
            Code    = code,
            Source  = error.Source,
            Title   = error.Title,
            Context = contextKeys
        };
    }

    #endregion

    /// <summary>
    ///     Gets or sets the schema version of the snapshot document (see <see cref="CurrentSchema" />).
    /// </summary>
    public int Schema { get; set; } = CurrentSchema;

    /// <summary>
    ///     Gets or sets the tracked errors, ordered by <see cref="CatalogSnapshotEntry.Code" /> (ordinal).
    /// </summary>
    public IReadOnlyList<CatalogSnapshotEntry> Errors { get; set; } = [];

}

/// <summary>
///     The contract of a single documented error inside a <see cref="CatalogSnapshot" />.
/// </summary>
public sealed class CatalogSnapshotEntry {

    /// <summary>
    ///     Gets or sets the stable error code — the identity under which the error is tracked across versions.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     Gets or sets the source of the error (the <c>[ProvidesErrorsFor]</c> target). Documentation-level identity:
    ///     a change is reported as informational, never as breaking.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    ///     Gets or sets the human-readable title of the error. Documentation-level identity: a change is reported as
    ///     informational, and a matching title is used to hint at probable renames.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    ///     Gets or sets the declared context keys of the error, ordered by <see cref="CatalogSnapshotContextKey.Key" />
    ///     (ordinal). Context keys are part of the contract: log pipelines and dashboards read them by name.
    /// </summary>
    public IReadOnlyList<CatalogSnapshotContextKey> Context { get; set; } = [];

}

/// <summary>
///     A context key declared by a documented error: its name and the (fully qualified) name of its value type.
/// </summary>
public sealed class CatalogSnapshotContextKey {

    /// <summary>
    ///     Gets or sets the unique name of the context key.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    ///     Gets or sets the (fully qualified) name of the value type associated with the key.
    /// </summary>
    public string? ValueType { get; set; }

}
