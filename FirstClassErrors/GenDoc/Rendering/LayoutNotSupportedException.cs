namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     Thrown by a renderer when it is asked to produce a layout it does not support (see
///     <see cref="IErrorDocumentationRenderer.SupportedLayouts" />).
/// </summary>
public sealed class LayoutNotSupportedException : Exception {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new <see cref="LayoutNotSupportedException" /> describing the unsupported request.
    /// </summary>
    /// <param name="format">The format of the renderer that rejected the request.</param>
    /// <param name="requestedLayout">The layout that was requested.</param>
    /// <param name="supportedLayouts">The layouts the renderer actually supports.</param>
    public LayoutNotSupportedException(string format, string requestedLayout, IEnumerable<string> supportedLayouts)
        : base($"The '{format}' renderer does not support the '{requestedLayout}' layout. Supported layouts: {string.Join(", ", supportedLayouts)}.") {
        Format           = format;
        RequestedLayout  = requestedLayout;
        SupportedLayouts = supportedLayouts is null ? [] : new List<string>(supportedLayouts);
    }

    #endregion

    /// <summary>Gets the format of the renderer that rejected the request.</summary>
    public string Format { get; }

    /// <summary>Gets the layout that was requested.</summary>
    public string RequestedLayout { get; }

    /// <summary>Gets the layouts the renderer supports.</summary>
    public IReadOnlyList<string> SupportedLayouts { get; }

}
