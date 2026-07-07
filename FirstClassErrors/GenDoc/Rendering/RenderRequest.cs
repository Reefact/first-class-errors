#region Usings declarations

using System.Globalization;

#endregion

namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     The parameters of a single render: the requested <see cref="Layout" /> and the target <see cref="Culture" />
///     (used to localize any boilerplate the renderer emits). It is passed to
///     <see cref="IErrorDocumentationRenderer.Render" /> so that layout and language are chosen per call rather than
///     baked into the renderer instance.
/// </summary>
public sealed class RenderRequest {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new <see cref="RenderRequest" /> for the given layout, rendered for the invariant culture.
    /// </summary>
    /// <param name="layout">The requested layout (see <see cref="RenderLayouts" />).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="layout" /> is <c>null</c>.</exception>
    public RenderRequest(string layout)
        : this(layout, CultureInfo.InvariantCulture) { }

    /// <summary>
    ///     Initializes a new <see cref="RenderRequest" /> for the given layout and culture.
    /// </summary>
    /// <param name="layout">The requested layout (see <see cref="RenderLayouts" />).</param>
    /// <param name="culture">The culture to localize the rendered boilerplate for.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="layout" /> or <paramref name="culture" /> is <c>null</c>.</exception>
    public RenderRequest(string layout, CultureInfo culture)
        : this(layout, culture, null) { }

    /// <summary>
    ///     Initializes a new <see cref="RenderRequest" /> for the given layout, culture and service name.
    /// </summary>
    /// <param name="layout">The requested layout (see <see cref="RenderLayouts" />).</param>
    /// <param name="culture">The culture to localize the rendered boilerplate for.</param>
    /// <param name="serviceName">
    ///     The service name the documented errors are exposed under, used to build the RFC 9457 problem <c>type</c> of the
    ///     rendered examples (<c>urn:problem:{service}:{code}</c>), or <c>null</c> when none is configured.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="layout" /> or <paramref name="culture" /> is <c>null</c>.</exception>
    public RenderRequest(string layout, CultureInfo culture, string? serviceName) {
        Layout      = layout  ?? throw new ArgumentNullException(nameof(layout));
        Culture     = culture ?? throw new ArgumentNullException(nameof(culture));
        ServiceName = serviceName;
    }

    #endregion

    /// <summary>Gets the requested layout.</summary>
    public string Layout { get; }

    /// <summary>Gets the culture the rendered boilerplate should be localized for.</summary>
    public CultureInfo Culture { get; }

    /// <summary>
    ///     Gets the service name used to build the RFC 9457 problem <c>type</c> of the rendered examples, or <c>null</c>
    ///     when none is configured (no <c>type</c> is then emitted).
    /// </summary>
    public string? ServiceName { get; }

}
