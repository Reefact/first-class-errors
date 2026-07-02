#region Usings declarations

using FirstClassErrors.GenDoc.Rendering;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     The set of renderers the CLI can produce, each selected by the <see cref="IErrorDocumentationRenderer.Format" />
///     it declares. To expose a new format, add one factory to <see cref="Factories" /> — no other code changes.
/// </summary>
internal static class RendererCatalog {

    #region Statics members declarations

    // One entry per renderer. Each factory receives the parsed settings so a renderer can honour its own options
    // (e.g. the Markdown layout). Adding a format is a single line here.
    private static readonly IReadOnlyList<Func<GenerateSettings, IErrorDocumentationRenderer>> Factories = [
        _ => new JsonErrorDocumentationRenderer(),
        settings => new MarkdownErrorDocumentationRenderer(settings.NormalizedLayout() == "split"
                                                               ? MarkdownLayout.Split
                                                               : MarkdownLayout.Single)
    ];

    /// <summary>Gets the format identifiers the CLI supports, as declared by the registered renderers.</summary>
    public static IReadOnlyList<string> Formats {
        get { return Factories.Select(factory => factory(new GenerateSettings()).Format).ToList(); }
    }

    /// <summary>Determines whether a renderer is registered for the given (already normalized) format.</summary>
    public static bool Supports(string format) {
        return Formats.Any(known => string.Equals(known, format, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Creates the renderer whose declared format matches <see cref="GenerateSettings.NormalizedFormat" />.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no renderer declares the requested format.</exception>
    public static IErrorDocumentationRenderer Create(GenerateSettings settings) {
        string requested = settings.NormalizedFormat();

        foreach (Func<GenerateSettings, IErrorDocumentationRenderer> factory in Factories) {
            IErrorDocumentationRenderer renderer = factory(settings);
            if (string.Equals(renderer.Format, requested, StringComparison.OrdinalIgnoreCase)) {
                return renderer;
            }
        }

        throw new InvalidOperationException($"Unsupported format '{settings.Format}'. Available formats: {string.Join(", ", Formats)}.");
    }

    #endregion

}
