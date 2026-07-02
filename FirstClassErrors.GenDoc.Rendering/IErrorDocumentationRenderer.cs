namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     Renders an aggregated error-documentation catalog into one or more textual output files (JSON, Markdown, …).
/// </summary>
/// <remarks>
///     A renderer transforms the documentation model into a target representation. It depends only on the model, not
///     on how the catalog was produced, so a catalog obtained from any source can be rendered. A renderer may produce
///     a single document or several (e.g. a Markdown index plus one file per error).
/// </remarks>
public interface IErrorDocumentationRenderer {

    /// <summary>
    ///     Renders the given catalog and returns the produced document(s).
    /// </summary>
    /// <param name="catalog">The aggregated, deduplicated error documentation to render.</param>
    /// <returns>The rendered documents. Always at least one; single-file formats return exactly one.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog" /> is <c>null</c>.</exception>
    IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog);

}
