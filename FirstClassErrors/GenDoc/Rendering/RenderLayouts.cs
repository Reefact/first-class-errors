namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     The layout identifiers understood by the built-in renderers. A layout is a free-form string in the renderer
///     contract (a custom renderer may declare its own), but these are the values the shipped renderers use and the
///     CLI exposes through <c>--layout</c>.
/// </summary>
public static class RenderLayouts {

    /// <summary>A single output document (e.g. one Markdown file, or the JSON catalog).</summary>
    public const string Single = "single";

    /// <summary>Several output documents (e.g. a Markdown index plus one file per source group and per error).</summary>
    public const string Split = "split";

}
