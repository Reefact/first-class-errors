namespace FirstClassErrors.GenDoc.Versioning;

/// <summary>
///     The result of comparing two catalog snapshots: the ordered list of <see cref="Changes" />, plus convenience
///     views per <see cref="CatalogChangeImpact" /> and the policy helper <see cref="HasChangesAtOrAbove" /> used to
///     turn a diff into a CI verdict.
/// </summary>
public sealed class CatalogDiff {

    #region Statics members declarations

    private static int Rank(CatalogChangeImpact impact) {
        // Breaking is the most severe; the enum declaration order already encodes the ranking, but the mapping is
        // explicit here so reordering the enum can never silently change the policy semantics.
        return impact switch {
            CatalogChangeImpact.Breaking      => 2,
            CatalogChangeImpact.Compatible    => 1,
            CatalogChangeImpact.Informational => 0,
            _                                 => 0
        };
    }

    #endregion

    #region Constructors declarations

    internal CatalogDiff(IReadOnlyList<CatalogChange> changes) {
        Changes = changes;
    }

    #endregion

    /// <summary>
    ///     Gets every change, ordered deterministically (by error code, then by kind).
    /// </summary>
    public IReadOnlyList<CatalogChange> Changes { get; }

    /// <summary>Gets a value indicating whether the two snapshots are identical.</summary>
    public bool IsEmpty => Changes.Count == 0;

    /// <summary>Gets the changes that can break a consumer of the contract.</summary>
    public IReadOnlyList<CatalogChange> BreakingChanges => Of(CatalogChangeImpact.Breaking);

    /// <summary>Gets the additive changes.</summary>
    public IReadOnlyList<CatalogChange> CompatibleChanges => Of(CatalogChangeImpact.Compatible);

    /// <summary>Gets the documentation-only changes.</summary>
    public IReadOnlyList<CatalogChange> InformationalChanges => Of(CatalogChangeImpact.Informational);

    /// <summary>
    ///     Determines whether the diff contains at least one change whose impact is at least as severe as
    ///     <paramref name="impact" /> (severity order: informational &lt; compatible &lt; breaking).
    /// </summary>
    /// <param name="impact">The impact threshold.</param>
    /// <returns><c>true</c> when at least one change reaches the threshold; otherwise <c>false</c>.</returns>
    public bool HasChangesAtOrAbove(CatalogChangeImpact impact) {
        int threshold = Rank(impact);

        return Changes.Any(change => Rank(change.Impact) >= threshold);
    }

    private IReadOnlyList<CatalogChange> Of(CatalogChangeImpact impact) {
        return Changes.Where(change => change.Impact == impact).ToList();
    }

}
