namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     A single rendered output file produced by a renderer: its <see cref="RelativePath" /> (used as the file name
///     when writing to a directory) and its textual <see cref="Content" />.
/// </summary>
/// <remarks>
///     Single-file renderers (JSON, single-file Markdown) return exactly one document. Multi-file renderers (the
///     split Markdown layout) return an index document plus one document per error.
/// </remarks>
public sealed class RenderedDocument {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="RenderedDocument" /> class.
    /// </summary>
    /// <param name="relativePath">The suggested file name (may contain sub-directories), relative to the output folder.</param>
    /// <param name="content">The rendered text of the file.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="relativePath" /> or <paramref name="content" /> is <c>null</c>.</exception>
    public RenderedDocument(string relativePath, string content) {
        if (relativePath is null) { throw new ArgumentNullException(nameof(relativePath)); }
        if (content is null) { throw new ArgumentNullException(nameof(content)); }

        RelativePath = relativePath;
        Content      = content;
    }

    #endregion

    /// <summary>
    ///     Gets the suggested file name (possibly including sub-directories), relative to the output folder.
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    ///     Gets the rendered text of the file.
    /// </summary>
    public string Content { get; }

}
