namespace Dummies;

/// <summary>
///     A generator of arbitrary <c>ws</c>/<c>wss</c> URIs. Per RFC 6455 a WebSocket URI carries <b>no user-info and
///     no fragment</b>, so — unlike <see cref="AnyWebUri" /> — this builder does not expose them. Pin the TLS variant
///     with <see cref="UsingWs" />/<see cref="UsingWss" />; unpinned, the scheme is drawn from both.
/// </summary>
public sealed class AnyWebSocketUri : IAny<Uri>, IHasRandomSource {

    #region Fields declarations

    private readonly RandomSource _source;
    private readonly UriSpec      _spec;

    #endregion

    internal AnyWebSocketUri(RandomSource source, UriSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Pins the scheme to <c>ws</c>. Declared once per generator.</summary>
    public AnyWebSocketUri UsingWs() {
        return new AnyWebSocketUri(_source, _spec.WithScheme("ws", "UsingWs()"));
    }

    /// <summary>Pins the scheme to <c>wss</c>. Declared once per generator.</summary>
    public AnyWebSocketUri UsingWss() {
        return new AnyWebSocketUri(_source, _spec.WithScheme("wss", "UsingWss()"));
    }

    /// <summary>Pins the host. Must be an ASCII host name (pass the punycode form for internationalized hosts).</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="host" /> is empty, non-ASCII or not a valid host name.</exception>
    public AnyWebSocketUri WithHost(string host) {
        return new AnyWebSocketUri(_source, _spec.WithHost(UriSpec.RequireHost(host, nameof(host))));
    }

    /// <summary>Includes an arbitrary non-default port.</summary>
    public AnyWebSocketUri WithPort() {
        return new AnyWebSocketUri(_source, _spec.WithPort(null));
    }

    /// <summary>Includes the given <paramref name="port" />.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="port" /> is outside 1..65535.</exception>
    public AnyWebSocketUri WithPort(int port) {
        return new AnyWebSocketUri(_source, _spec.WithPort(UriSpec.RequirePort(port, nameof(port))));
    }

    /// <summary>Fixes the path to exactly <paramref name="count" /> segments. Declared once per generator.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a path constraint is already declared.</exception>
    public AnyWebSocketUri WithPathSegments(int count) {
        return new AnyWebSocketUri(_source, _spec.WithPath(UriPathMode.Exact, UriSpec.RequireSegmentCount(count, nameof(count)), UriSpec.SegmentsLabel(count)));
    }

    /// <summary>Renders the root path (<c>/</c>) with no segments. Declared once per generator.</summary>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a path constraint is already declared.</exception>
    public AnyWebSocketUri WithoutPath() {
        return new AnyWebSocketUri(_source, _spec.WithPath(UriPathMode.Root, 0, "WithoutPath()"));
    }

    /// <summary>Includes an arbitrary query string.</summary>
    public AnyWebSocketUri WithQuery() {
        return new AnyWebSocketUri(_source, _spec.WithQuery());
    }

    /// <inheritdoc />
    public Uri Generate() {
        return _spec.Generate(_source);
    }

}
