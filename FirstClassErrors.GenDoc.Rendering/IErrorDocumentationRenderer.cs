namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     Renders an aggregated error-documentation catalog into a single textual output format (JSON, Markdown, …).
/// </summary>
/// <remarks>
///     A renderer transforms the documentation model into a target representation. It depends only on the model, not
///     on how the catalog was produced, so a catalog obtained from any source can be rendered.
/// </remarks>
public interface IErrorDocumentationRenderer {

    /// <summary>
    ///     Renders the given catalog and returns the produced document as text.
    /// </summary>
    /// <param name="catalog">The aggregated, deduplicated error documentation to render.</param>
    /// <returns>The rendered document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog" /> is <c>null</c>.</exception>
    string Render(IEnumerable<ErrorDocumentation> catalog);

}
