#region Usings declarations

using FirstClassErrors.GenDoc.Rendering;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Resolves the renderer to use from its declared <see cref="IErrorDocumentationRenderer.Format" />. Built-in
///     renderers are baked in; custom renderers are supplied per run, loaded from the configuration.
/// </summary>
internal static class RendererCatalog {

    #region Statics members declarations

    // One entry per built-in renderer. A renderer is layout-agnostic at construction: the layout is chosen per call
    // through the RenderRequest passed to Render. Adding a built-in format is a single line here; custom formats are
    // added through the configuration instead (fce config renderer add).
    private static readonly IReadOnlyList<Func<IErrorDocumentationRenderer>> BuiltInFactories = [
        () => new JsonErrorDocumentationRenderer(),
        () => new MarkdownErrorDocumentationRenderer(),
        () => new HtmlErrorDocumentationRenderer()
    ];

    /// <summary>The built-in format identifiers, computed once (the built-in renderers never change at runtime).</summary>
    private static readonly IReadOnlyList<string> BuiltInFormatList = BuiltInFactories.Select(factory => factory().Format).ToList();

    /// <summary>Gets the built-in format identifiers, as declared by the built-in renderers.</summary>
    public static IReadOnlyList<string> BuiltInFormats => BuiltInFormatList;

    /// <summary>
    ///     Creates the renderer whose declared format matches <paramref name="format" />, preferring a built-in over
    ///     a custom one when both declare the same format.
    /// </summary>
    /// <param name="format">The requested (already normalized) format.</param>
    /// <param name="customRenderers">Renderers loaded from the configuration for this run.</param>
    /// <exception cref="InvalidOperationException">Thrown when no renderer declares the requested format.</exception>
    public static IErrorDocumentationRenderer Create(string format, IReadOnlyList<IErrorDocumentationRenderer> customRenderers) {
        foreach (Func<IErrorDocumentationRenderer> factory in BuiltInFactories) {
            IErrorDocumentationRenderer renderer = factory();
            if (string.Equals(renderer.Format, format, StringComparison.OrdinalIgnoreCase)) {
                return renderer;
            }
        }

        IErrorDocumentationRenderer? customRenderer = customRenderers
           .FirstOrDefault(renderer => string.Equals(renderer.Format, format, StringComparison.OrdinalIgnoreCase));
        if (customRenderer is not null) {
            return customRenderer;
        }

        IEnumerable<string> available = BuiltInFormats.Concat(customRenderers.Select(renderer => renderer.Format))
                                                      .Distinct(StringComparer.OrdinalIgnoreCase);

        throw new InvalidOperationException($"Unsupported format '{format}'. Available formats: {string.Join(", ", available)}.");
    }

    #endregion

}
