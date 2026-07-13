namespace FirstClassErrors.GenDoc.Versioning;

/// <summary>
///     The impact of a single catalog change on the consumers of the error contract.
/// </summary>
public enum CatalogChangeImpact {

    /// <summary>
    ///     The change can break a consumer of the contract: a code or a context key that external systems may rely on
    ///     (client branching, dashboards, log pipelines, support procedures) disappeared or changed shape.
    /// </summary>
    Breaking,

    /// <summary>
    ///     The change is additive: existing consumers keep working, new capability appears (a new error code, a new
    ///     context key).
    /// </summary>
    Compatible,

    /// <summary>
    ///     The change only affects the documentation identity (title, source); no consumer contract is involved. The
    ///     machine-readable JSON report surfaces this as the enum name (<c>informational</c>); the human-readable text
    ///     and Markdown reports label the same group "Documentation changes".
    /// </summary>
    Informational

}

/// <summary>
///     The kind of a single catalog change.
/// </summary>
public enum CatalogChangeKind {

    /// <summary>A new error code appeared in the catalog.</summary>
    ErrorAdded,

    /// <summary>An error code disappeared from the catalog.</summary>
    ErrorRemoved,

    /// <summary>An existing error declares a new context key.</summary>
    ContextKeyAdded,

    /// <summary>An existing error no longer declares a context key it used to.</summary>
    ContextKeyRemoved,

    /// <summary>A context key of an existing error changed its value type.</summary>
    ContextKeyValueTypeChanged,

    /// <summary>The title of an existing error changed.</summary>
    TitleChanged,

    /// <summary>The source of an existing error changed.</summary>
    SourceChanged

}

/// <summary>
///     A single difference between two catalog snapshots: what changed (<see cref="Kind" />), how much it matters
///     (<see cref="Impact" />), on which error (<see cref="Code" />), and a human-readable <see cref="Description" />.
/// </summary>
public sealed class CatalogChange {

    #region Constructors declarations

    internal CatalogChange(CatalogChangeKind kind, CatalogChangeImpact impact, string code, string description) {
        Kind        = kind;
        Impact      = impact;
        Code        = code;
        Description = description;
    }

    #endregion

    /// <summary>Gets the kind of the change.</summary>
    public CatalogChangeKind Kind { get; }

    /// <summary>Gets the impact of the change on the consumers of the contract.</summary>
    public CatalogChangeImpact Impact { get; }

    /// <summary>Gets the error code the change applies to.</summary>
    public string Code { get; }

    /// <summary>Gets the human-readable description of the change.</summary>
    public string Description { get; }

    /// <inheritdoc />
    public override string ToString() {
        return $"[{Kind}] {Code} — {Description}";
    }

}
