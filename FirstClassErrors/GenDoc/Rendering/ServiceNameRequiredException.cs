namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     Thrown by a renderer that embeds RFC 9457 examples when it is asked to render without a service name in the
///     <see cref="RenderRequest" />. The problem <c>type</c> of an example is <c>urn:problem:{service}:{code}</c>, so
///     the service segment cannot be built without a service name — the renderer refuses rather than emit a type-less
///     example. This invariant is enforced here, in the component that produces the examples, not only at the CLI edge.
/// </summary>
public sealed class ServiceNameRequiredException : Exception {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new <see cref="ServiceNameRequiredException" /> for the given renderer format.
    /// </summary>
    /// <param name="format">The format of the renderer that requires a service name.</param>
    public ServiceNameRequiredException(string format)
        : base($"The '{format}' renderer requires a service name to build the RFC 9457 problem type (urn:problem:{{service}}:{{code}}) of its examples. Provide it through RenderRequest.ServiceName (the CLI exposes it as --service-name, or the 'serviceName' configuration value).") {
        Format = format;
    }

    #endregion

    /// <summary>Gets the format of the renderer that requires a service name.</summary>
    public string Format { get; }

}
