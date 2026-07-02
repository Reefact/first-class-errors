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

    // One entry per built-in renderer. Each factory receives the effective layout so a renderer can honour it (only
    // the Markdown renderer does). Adding a built-in format is a single line here; custom formats are added through
    // the configuration instead (fce config renderer add).
    private static readonly IReadOnlyList<Func<string, IErrorDocumentationRenderer>> BuiltInFactories = [
        _ => new JsonErrorDocumentationRenderer(),
        layout => new MarkdownErrorDocumentationRenderer(string.Equals(layout, "split", StringComparison.OrdinalIgnoreCase)
                                                             ? MarkdownLayout.Split
                                                             : MarkdownLayout.Single)
    ];

    /// <summary>Gets the built-in format identifiers, as declared by the built-in renderers.</summary>
    public static IReadOnlyList<string> BuiltInFormats {
        get { return BuiltInFactories.Select(factory => factory("single").Format).ToList(); }
    }

    /// <summary>
    ///     Creates the renderer whose declared format matches <paramref name="format" />, preferring a built-in over
    ///     a custom one when both declare the same format.
    /// </summary>
    /// <param name="format">The requested (already normalized) format.</param>
    /// <param name="layout">The effective Markdown layout, forwarded to renderers that honour it.</param>
    /// <param name="customRenderers">Renderers loaded from the configuration for this run.</param>
    /// <exception cref="InvalidOperationException">Thrown when no renderer declares the requested format.</exception>
    public static IErrorDocumentationRenderer Create(string format, string layout, IReadOnlyList<IErrorDocumentationRenderer> customRenderers) {
        foreach (Func<string, IErrorDocumentationRenderer> factory in BuiltInFactories) {
            IErrorDocumentationRenderer renderer = factory(layout);
            if (string.Equals(renderer.Format, format, StringComparison.OrdinalIgnoreCase)) {
                return renderer;
            }
        }

        foreach (IErrorDocumentationRenderer renderer in customRenderers) {
            if (string.Equals(renderer.Format, format, StringComparison.OrdinalIgnoreCase)) {
                return renderer;
            }
        }

        IEnumerable<string> available = BuiltInFormats.Concat(customRenderers.Select(renderer => renderer.Format))
                                                      .Distinct(StringComparer.OrdinalIgnoreCase);

        throw new InvalidOperationException($"Unsupported format '{format}'. Available formats: {string.Join(", ", available)}.");
    }

    #endregion

}
