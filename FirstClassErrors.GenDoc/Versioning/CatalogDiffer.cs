namespace FirstClassErrors.GenDoc.Versioning;

/// <summary>
///     Compares two <see cref="CatalogSnapshot" /> instances — a committed baseline and the freshly generated
///     catalog — and classifies every difference by its impact on the consumers of the error contract.
/// </summary>
/// <remarks>
///     <para>Classification rules:</para>
///     <list type="bullet">
///         <item>
///             <b>Breaking</b> — an error code was removed (a rename is a removal plus an addition; when a removed and
///             an added error share the same title, the removal carries a "possibly renamed" hint), a context key was
///             removed, or a context key changed its value type.
///         </item>
///         <item><b>Compatible</b> — an error code or a context key was added.</item>
///         <item><b>Informational</b> — the title or the source of an existing error changed (documentation identity).</item>
///     </list>
///     <para>
///         Identities are compared normalized (trimmed, ordinal), so a hand-edited or programmatically built snapshot
///         with padded codes or key names never produces phantom differences.
///     </para>
/// </remarks>
public static class CatalogDiffer {

    #region Statics members declarations

    /// <summary>
    ///     Computes the differences between <paramref name="baseline" /> and <paramref name="current" />.
    /// </summary>
    /// <param name="baseline">The committed reference snapshot (the accepted contract).</param>
    /// <param name="current">The snapshot of the catalog as it stands now.</param>
    /// <returns>The classified, deterministically ordered differences.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="baseline" /> or <paramref name="current" /> is <c>null</c>.
    /// </exception>
    public static CatalogDiff Diff(CatalogSnapshot baseline, CatalogSnapshot current) {
        if (baseline is null) { throw new ArgumentNullException(nameof(baseline)); }
        if (current is null) { throw new ArgumentNullException(nameof(current)); }

        // Every comparison below goes through the indexes, whose keys are the normalized identities — never through
        // the raw entry properties — so trimming rules apply symmetrically to both sides.
        Dictionary<string, CatalogSnapshotEntry> baselineByCode = IndexByCode(baseline);
        Dictionary<string, CatalogSnapshotEntry> currentByCode  = IndexByCode(current);

        List<CatalogChange> changes = [];

        List<KeyValuePair<string, CatalogSnapshotEntry>> removed = baselineByCode.Where(pair => currentByCode.ContainsKey(pair.Key) is false).ToList();
        List<KeyValuePair<string, CatalogSnapshotEntry>> added   = currentByCode.Where(pair => baselineByCode.ContainsKey(pair.Key) is false).ToList();

        foreach (KeyValuePair<string, CatalogSnapshotEntry> pair in removed) {
            changes.Add(new CatalogChange(CatalogChangeKind.ErrorRemoved, CatalogChangeImpact.Breaking, pair.Key, DescribeRemoval(pair.Value, added)));
        }

        foreach (KeyValuePair<string, CatalogSnapshotEntry> pair in added) {
            changes.Add(new CatalogChange(CatalogChangeKind.ErrorAdded, CatalogChangeImpact.Compatible, pair.Key, DescribeAddition(pair.Value)));
        }

        foreach (KeyValuePair<string, CatalogSnapshotEntry> pair in baselineByCode) {
            if (currentByCode.TryGetValue(pair.Key, out CatalogSnapshotEntry? after) is false) { continue; }

            CompareEntry(pair.Key, pair.Value, after, changes);
        }

        List<CatalogChange> ordered = changes
                                     .OrderBy(change => change.Code, StringComparer.Ordinal)
                                     .ThenBy(change => change.Kind)
                                     .ToList();

        return new CatalogDiff(ordered);
    }

    private static Dictionary<string, CatalogSnapshotEntry> IndexByCode(CatalogSnapshot snapshot) {
        Dictionary<string, CatalogSnapshotEntry> byCode = new(StringComparer.Ordinal);

        // Same identity rules as CatalogSnapshot.FromCatalog: entries without a code are skipped (nothing to track),
        // codes are trimmed, and the first entry wins on a (defensive) duplicate.
        foreach (CatalogSnapshotEntry entry in snapshot.Errors) {
            if (string.IsNullOrWhiteSpace(entry.Code)) { continue; }

            string code = entry.Code!.Trim();
            if (byCode.ContainsKey(code) is false) { byCode.Add(code, entry); }
        }

        return byCode;
    }

    private static void CompareEntry(string code, CatalogSnapshotEntry before, CatalogSnapshotEntry after, List<CatalogChange> changes) {
        if (Differs(before.Title, after.Title)) {
            changes.Add(new CatalogChange(CatalogChangeKind.TitleChanged, CatalogChangeImpact.Informational, code,
                                          $"title changed from {Quote(before.Title)} to {Quote(after.Title)}"));
        }

        if (Differs(before.Source, after.Source)) {
            changes.Add(new CatalogChange(CatalogChangeKind.SourceChanged, CatalogChangeImpact.Informational, code,
                                          $"source changed from {Quote(before.Source)} to {Quote(after.Source)}"));
        }

        Dictionary<string, CatalogSnapshotContextKey> beforeKeys = IndexByKey(before);
        Dictionary<string, CatalogSnapshotContextKey> afterKeys  = IndexByKey(after);

        foreach (KeyValuePair<string, CatalogSnapshotContextKey> pair in beforeKeys) {
            if (afterKeys.TryGetValue(pair.Key, out CatalogSnapshotContextKey? counterpart) is false) {
                changes.Add(new CatalogChange(CatalogChangeKind.ContextKeyRemoved, CatalogChangeImpact.Breaking, code,
                                              $"context key '{pair.Key}' removed"));

                continue;
            }

            if (Differs(pair.Value.ValueType, counterpart.ValueType)) {
                changes.Add(new CatalogChange(CatalogChangeKind.ContextKeyValueTypeChanged, CatalogChangeImpact.Breaking, code,
                                              $"context key '{pair.Key}' changed its value type from {Quote(pair.Value.ValueType)} to {Quote(counterpart.ValueType)}"));
            }
        }

        foreach (KeyValuePair<string, CatalogSnapshotContextKey> pair in afterKeys) {
            if (beforeKeys.ContainsKey(pair.Key) is false) {
                changes.Add(new CatalogChange(CatalogChangeKind.ContextKeyAdded, CatalogChangeImpact.Compatible, code,
                                              $"context key '{pair.Key}' added ({pair.Value.ValueType ?? "unknown type"})"));
            }
        }
    }

    private static Dictionary<string, CatalogSnapshotContextKey> IndexByKey(CatalogSnapshotEntry entry) {
        Dictionary<string, CatalogSnapshotContextKey> byKey = new(StringComparer.Ordinal);

        foreach (CatalogSnapshotContextKey key in entry.Context) {
            if (string.IsNullOrWhiteSpace(key.Key)) { continue; }

            string name = key.Key!.Trim();
            if (byKey.ContainsKey(name) is false) { byKey.Add(name, key); }
        }

        return byKey;
    }

    private static string DescribeRemoval(CatalogSnapshotEntry entry, IReadOnlyList<KeyValuePair<string, CatalogSnapshotEntry>> added) {
        // A rename shows up as a removal plus an addition. When exactly one added error shares the removed error's
        // title (compared normalized, like every other comparison), surface it as a hint — the removal stays breaking
        // either way (consumers know the old code).
        string? title = Normalize(entry.Title);
        List<KeyValuePair<string, CatalogSnapshotEntry>> sameTitle = title is null
                                                                         ? []
                                                                         : added.Where(candidate => string.Equals(Normalize(candidate.Value.Title), title, StringComparison.Ordinal)).ToList();

        return sameTitle.Count == 1
                   ? $"error removed (possibly renamed to '{sameTitle[0].Key}', which has the same title)"
                   : "error removed";
    }

    private static string DescribeAddition(CatalogSnapshotEntry entry) {
        string title  = string.IsNullOrWhiteSpace(entry.Title) ? "new error" : $"new error {Quote(entry.Title)}";
        string source = string.IsNullOrWhiteSpace(entry.Source) ? string.Empty : $" (source: {entry.Source.Trim()})";

        return title + source;
    }

    private static bool Differs(string? before, string? after) {
        return string.Equals(Normalize(before), Normalize(after), StringComparison.Ordinal) is false;
    }

    private static string? Normalize(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
    }

    private static string Quote(string? value) {
        string? normalized = Normalize(value);

        return normalized is null ? "(none)" : $"'{normalized}'";
    }

    #endregion

}
