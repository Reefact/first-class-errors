namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary yet <b>valid</b> <see cref="Uri" /> values. Unconstrained, it spans the whole
///     safe URI space — an absolute web (<c>http</c>/<c>https</c>), WebSocket (<c>ws</c>/<c>wss</c>), FTP or mailto
///     URI, or a relative reference — drawn from the ambient random context. Narrow it to one <b>family</b> to reach
///     that family's constraints: each narrowing returns a family-specific builder that exposes only the components
///     that family actually has, so an impossible combination (a port on a mailto, a fragment on a WebSocket) cannot
///     even be written.
/// </summary>
/// <remarks>
///     <para>
///         Every component is drawn from ASCII-unreserved characters and the URI is assembled directly, so a value is
///         valid by construction — never generated then filtered — and a run is reproducible under a seed on every
///         target framework. Internationalized (IDN) hosts and the <c>file</c> scheme are deliberately outside the
///         unconstrained draw: an IDN host and a file path do not round-trip identically across frameworks, which
///         would break the determinism contract.
///     </para>
///     <example>
///         <code>
///         Uri any      = Any.Uri().Generate();                                   // any family, absolute or relative
///         Uri endpoint = Any.Uri().Web().UsingHttps().WithHost("api.example.com").Generate();
///         Uri socket   = Any.Uri().WebSocket().Generate();                       // ws:// or wss://
///         Uri relative = Any.Uri().Relative().Rooted().Generate();               // /a/b/c
///         </code>
///     </example>
/// </remarks>
public sealed class AnyUri : IAny<Uri>, IHasRandomSource {

    #region Fields declarations

    private readonly RandomSource _source;
    private readonly UriSpec      _spec;

    #endregion

    internal AnyUri(RandomSource source, UriSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Narrows to a web URI: <c>http</c> or <c>https</c>, with the full authority surface.</summary>
    /// <returns>A web-URI generator.</returns>
    public AnyWebUri Web() {
        return new AnyWebUri(_source, _spec.WithFamily(UriFamily.Web));
    }

    /// <summary>Narrows to a WebSocket URI: <c>ws</c> or <c>wss</c> (no user-info, no fragment — RFC 6455).</summary>
    /// <returns>A WebSocket-URI generator.</returns>
    public AnyWebSocketUri WebSocket() {
        return new AnyWebSocketUri(_source, _spec.WithFamily(UriFamily.WebSocket));
    }

    /// <summary>Narrows to an <c>ftp</c> URI (authority with user-info, no query or fragment).</summary>
    /// <returns>An FTP-URI generator.</returns>
    public AnyFtpUri Ftp() {
        return new AnyFtpUri(_source, _spec.WithFamily(UriFamily.Ftp));
    }

    /// <summary>Narrows to a <c>mailto</c> URI: <c>local@domain</c>, optionally with headers.</summary>
    /// <returns>A mailto-URI generator.</returns>
    public AnyMailtoUri Mailto() {
        return new AnyMailtoUri(_source, _spec.WithFamily(UriFamily.Mailto));
    }

    /// <summary>Narrows to a relative reference: a path with an optional query and fragment, no scheme or authority.</summary>
    /// <returns>A relative-URI generator.</returns>
    public AnyRelativeUri Relative() {
        return new AnyRelativeUri(_source, _spec.WithFamily(UriFamily.Relative));
    }

    /// <inheritdoc />
    public Uri Generate() {
        return _spec.Generate(_source);
    }

}
