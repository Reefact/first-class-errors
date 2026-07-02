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

    // One entry per built-in renderer. Each factory receives the parsed settings so a renderer can honour its own
    // options (e.g. the Markdown layout). Adding a built-in format is a single line here; custom formats are added
    // through the configuration instead (fce renderer add).
    private static readonly IReadOnlyList<Func<GenerateSettings, IErrorDocumentationRenderer>> BuiltInFactories = [
        _ => new JsonErrorDocumentationRenderer(),
        settings => new MarkdownErrorDocumentationRenderer(settings.NormalizedLayout() == "split"
                                                               ? MarkdownLayout.Split
                                                               : MarkdownLayout.Single)
    ];

    /// <summary>Gets the built-in format identifiers, as declared by the built-in renderers.</summary>
    public static IReadOnlyList<string> BuiltInFormats {
        get { return BuiltInFactories.Select(factory => factory(new GenerateSettings()).Format).ToList(); }
    }

    /// <summary>
    ///     Creates the renderer whose declared format matches <see cref="GenerateSettings.NormalizedFormat" />,
    ///     preferring a built-in over a custom one when both declare the same format.
    /// </summary>
    /// <param name="settings">The parsed generation settings (carries the requested format and layout).</param>
    /// <param name="customRenderers">Renderers loaded from the configuration for this run.</param>
    /// <exception cref="InvalidOperationException">Thrown when no renderer declares the requested format.</exception>
    public static IErrorDocumentationRenderer Create(GenerateSettings settings, IReadOnlyList<IErrorDocumentationRenderer> customRenderers) {
        string requested = settings.NormalizedFormat();

        foreach (Func<GenerateSettings, IErrorDocumentationRenderer> factory in BuiltInFactories) {
            IErrorDocumentationRenderer renderer = factory(settings);
            if (string.Equals(renderer.Format, requested, StringComparison.OrdinalIgnoreCase)) {
                return renderer;
            }
        }

        foreach (IErrorDocumentationRenderer renderer in customRenderers) {
            if (string.Equals(renderer.Format, requested, StringComparison.OrdinalIgnoreCase)) {
                return renderer;
            }
        }

        IEnumerable<string> available = BuiltInFormats.Concat(customRenderers.Select(renderer => renderer.Format))
                                                      .Distinct(StringComparer.OrdinalIgnoreCase);

        throw new InvalidOperationException($"Unsupported format '{settings.Format}'. Available formats: {string.Join(", ", available)}.");
    }

    #endregion

}
