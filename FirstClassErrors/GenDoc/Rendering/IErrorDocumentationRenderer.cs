namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     Renders an aggregated error-documentation catalog into one or more textual output files (JSON, Markdown, …).
/// </summary>
/// <remarks>
///     <para>
///         A renderer transforms the documentation model into a target representation. It depends only on the model,
///         not on how the catalog was produced, so a catalog obtained from any source can be rendered. A renderer may
///         produce a single document or several (e.g. a Markdown index plus one file per error).
///     </para>
///     <para>
///         To add a format, implement this interface: declare a unique <see cref="Format" /> (the value selected on
///         the command line), the <see cref="SupportedLayouts" /> the renderer can produce, and return the rendered
///         <see cref="RenderedDocument" />(s) for a given <see cref="RenderRequest" />. A tool can then discover the
///         renderer by its self-declared format without any hard-coded mapping.
///     </para>
/// </remarks>
public interface IErrorDocumentationRenderer {

    /// <summary>
    ///     Gets the unique format identifier this renderer produces (e.g. <c>"json"</c>, <c>"markdown"</c>). This is
    ///     the value a caller uses to select the renderer.
    /// </summary>
    string Format { get; }

    /// <summary>
    ///     Gets the layouts this renderer can produce (see <see cref="RenderLayouts" />). A renderer must reject, via
    ///     <see cref="LayoutNotSupportedException" />, any <see cref="RenderRequest.Layout" /> that is not listed here.
    ///     At least one layout is always declared.
    /// </summary>
    IReadOnlyCollection<string> SupportedLayouts { get; }

    /// <summary>
    ///     Renders the given catalog for the given request and returns the produced document(s).
    /// </summary>
    /// <param name="catalog">The aggregated, deduplicated error documentation to render.</param>
    /// <param name="request">The layout and culture to render for.</param>
    /// <returns>The rendered documents. Always at least one; single-file layouts return exactly one.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog" /> or <paramref name="request" /> is <c>null</c>.</exception>
    /// <exception cref="LayoutNotSupportedException">Thrown when the requested layout is not one of <see cref="SupportedLayouts" />.</exception>
    IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog, RenderRequest request);

}
