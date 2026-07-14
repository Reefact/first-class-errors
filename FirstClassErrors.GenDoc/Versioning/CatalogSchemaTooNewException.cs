namespace FirstClassErrors.GenDoc.Versioning;

/// <summary>
///     Thrown when a snapshot declares a schema newer than <see cref="CatalogSnapshot.CurrentSchema" />: it was
///     produced by a newer version of the tooling and cannot be read safely.
/// </summary>
/// <remarks>
///     It derives from <see cref="InvalidOperationException" />, so callers that treat every invalid snapshot alike
///     keep working. It is nonetheless a distinct type so a caller can tell "your tool is too old" apart from a
///     genuinely corrupt file — for example so that <c>catalog update</c> <b>refuses</b> a newer baseline (rather than
///     silently downgrading it) while still self-healing an unreadable one.
/// </remarks>
public sealed class CatalogSchemaTooNewException : InvalidOperationException {

    #region Constructors declarations

    internal CatalogSchemaTooNewException(int declaredSchema, int supportedSchema)
        : base($"The snapshot declares schema {declaredSchema}, but this tool only understands schema {supportedSchema} or lower. Update the tool to read it.") {
        DeclaredSchema  = declaredSchema;
        SupportedSchema = supportedSchema;
    }

    #endregion

    /// <summary>Gets the schema version the snapshot declares.</summary>
    public int DeclaredSchema { get; }

    /// <summary>Gets the highest schema version this tool understands.</summary>
    public int SupportedSchema { get; }

}
