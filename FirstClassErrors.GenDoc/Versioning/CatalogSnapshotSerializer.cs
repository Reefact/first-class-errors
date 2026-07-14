#region Usings declarations

using System.Text.Json;

#endregion

namespace FirstClassErrors.GenDoc.Versioning;

/// <summary>
///     Reads and writes <see cref="CatalogSnapshot" /> documents as deterministic JSON, suitable for committing as a
///     baseline file and diffing in source control.
/// </summary>
/// <remarks>
///     Serialization is pinned so the same snapshot always produces the same bytes on every platform: camelCase
///     property names, two-space indentation, <c>\n</c> line endings and a single trailing newline. Parsing is
///     tolerant of property-name casing and normalizes identities (codes and context-key names are trimmed, missing
///     lists become empty), but strict about the contract: a document that does not declare a <c>schema</c>, declares
///     an invalid one, or declares one newer than <see cref="CatalogSnapshot.CurrentSchema" /> is rejected rather
///     than silently misread.
/// </remarks>
public static class CatalogSnapshotSerializer {

    #region Statics members declarations

    private static readonly JsonSerializerOptions WriteOptions = new() {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions ReadOptions = new() {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    ///     Serializes the snapshot to its canonical JSON form.
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <returns>The canonical JSON text, ending with a single newline.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot" /> is <c>null</c>.</exception>
    public static string Serialize(CatalogSnapshot snapshot) {
        if (snapshot is null) { throw new ArgumentNullException(nameof(snapshot)); }

        // Normalize line endings to \n rather than relying on JsonSerializerOptions.NewLine, which is .NET 9+ only:
        // the documentation tooling must also build on the .NET 8 floor, and the committed baseline must stay
        // byte-stable across platforms (a CRLF host would otherwise churn the file).
        string json = JsonSerializer.Serialize(snapshot, WriteOptions).Replace("\r\n", "\n");

        return json + "\n";
    }

    /// <summary>
    ///     Parses a snapshot from its JSON form.
    /// </summary>
    /// <param name="json">The JSON text of the snapshot.</param>
    /// <returns>
    ///     The parsed snapshot, with trimmed codes and context-key names and a never-null
    ///     <see cref="CatalogSnapshot.Errors" /> list.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the text is not valid JSON, when the document does not declare a valid <c>schema</c> version,
    ///     or when an entry has no code.
    /// </exception>
    /// <exception cref="CatalogSchemaTooNewException">
    ///     Thrown when the document declares a schema newer than <see cref="CatalogSnapshot.CurrentSchema" /> (a
    ///     subtype of <see cref="InvalidOperationException" />).
    /// </exception>
    public static CatalogSnapshot Deserialize(string json) {
        if (json is null) { throw new ArgumentNullException(nameof(json)); }

        SnapshotDocument? document;
        try {
            document = JsonSerializer.Deserialize<SnapshotDocument>(json, ReadOptions);
        } catch (JsonException exception) {
            throw new InvalidOperationException($"The snapshot is not valid JSON: {exception.Message}", exception);
        }

        if (document is null) {
            throw new InvalidOperationException("The snapshot is empty.");
        }

        // The schema is bound through a nullable so a document that omits it entirely is detected, instead of
        // silently inheriting the model's default and bypassing this guard.
        if (document.Schema is null || document.Schema < 1) {
            throw new InvalidOperationException("The snapshot does not declare a valid 'schema' version.");
        }

        if (document.Schema > CatalogSnapshot.CurrentSchema) {
            // A distinct type (not a plain InvalidOperationException) so a caller can refuse a too-new baseline
            // instead of mistaking it for a corrupt one — see CatalogSchemaTooNewException.
            throw new CatalogSchemaTooNewException(document.Schema.Value, CatalogSnapshot.CurrentSchema);
        }

        // A hand-edited "errors": null (or a missing property) deserializes to null; keep the never-null invariant.
        List<CatalogSnapshotEntry> errors = document.Errors ?? [];

        foreach (CatalogSnapshotEntry entry in errors) {
            if (string.IsNullOrWhiteSpace(entry.Code)) {
                throw new InvalidOperationException("The snapshot contains an entry without a 'code'; every tracked error must have one.");
            }

            // Identities are normalized here, once, so the differ can rely on trimmed codes and key names even for
            // hand-edited baselines.
            entry.Code = entry.Code!.Trim();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (entry.Context is null) { entry.Context = []; }

            foreach (CatalogSnapshotContextKey key in entry.Context) {
                if (key.Key is not null) { key.Key = key.Key.Trim(); }
            }
        }

        return new CatalogSnapshot { Schema = document.Schema.Value, Errors = errors };
    }

    #endregion

    #region Nested types declarations

    /// <summary>
    ///     The raw shape of a snapshot document, with a nullable schema so its absence is detectable at parse time.
    /// </summary>
    private sealed class SnapshotDocument {

        public int? Schema { get; set; }

        public List<CatalogSnapshotEntry>? Errors { get; set; }

    }

    #endregion

}
