namespace Dummies;

/// <summary>
///     A generator of arbitrary <c>http</c>/<c>https</c> URIs. Exposes the full authority surface — user-info, host,
///     port, path, query and fragment. Pin the TLS variant with <see cref="UsingHttp" />/<see cref="UsingHttps" />;
///     unpinned, the scheme is drawn from both. Every component is drawn from ASCII-unreserved characters, so a value
///     is valid by construction and reproducible under a seed.
/// </summary>
public sealed class AnyWebUri : IAny<Uri>, IHasRandomSource {

    #region Fields declarations

    private readonly RandomSource _source;
    private readonly UriSpec      _spec;

    #endregion

    internal AnyWebUri(RandomSource source, UriSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Pins the scheme to <c>http</c>. Declared once per generator.</summary>
    public AnyWebUri UsingHttp() {
        return new AnyWebUri(_source, _spec.WithScheme("http", "UsingHttp()"));
    }

    /// <summary>Pins the scheme to <c>https</c>. Declared once per generator.</summary>
    public AnyWebUri UsingHttps() {
        return new AnyWebUri(_source, _spec.WithScheme("https", "UsingHttps()"));
    }

    /// <summary>Pins the host. Must be an ASCII host name (pass the punycode form for internationalized hosts).</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="host" /> is empty, non-ASCII or not a valid host name.</exception>
    public AnyWebUri WithHost(string host) {
        return new AnyWebUri(_source, _spec.WithHost(UriSpec.RequireHost(host, nameof(host))));
    }

    /// <summary>Includes arbitrary <c>user:password</c> user-info.</summary>
    public AnyWebUri WithUserInfo() {
        return new AnyWebUri(_source, _spec.WithUserInfo(null, null));
    }

    /// <summary>Includes user-info with the given <paramref name="user" /> and an arbitrary password.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="user" /> contains a non-unreserved character.</exception>
    public AnyWebUri WithUserInfo(string user) {
        return new AnyWebUri(_source, _spec.WithUserInfo(UriSpec.RequireUserInfoPart(user, nameof(user)), null));
    }

    /// <summary>Includes the given <paramref name="user" /> and <paramref name="password" /> user-info.</summary>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument contains a non-unreserved character.</exception>
    public AnyWebUri WithUserInfo(string user, string password) {
        return new AnyWebUri(_source, _spec.WithUserInfo(UriSpec.RequireUserInfoPart(user, nameof(user)), UriSpec.RequireUserInfoPart(password, nameof(password))));
    }

    /// <summary>Includes an arbitrary non-default port.</summary>
    public AnyWebUri WithPort() {
        return new AnyWebUri(_source, _spec.WithPort(null));
    }

    /// <summary>Includes the given <paramref name="port" />.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="port" /> is outside 1..65535.</exception>
    public AnyWebUri WithPort(int port) {
        return new AnyWebUri(_source, _spec.WithPort(UriSpec.RequirePort(port, nameof(port))));
    }

    /// <summary>Fixes the path to exactly <paramref name="count" /> segments. Declared once per generator.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a path constraint is already declared.</exception>
    public AnyWebUri WithPathSegments(int count) {
        return new AnyWebUri(_source, _spec.WithPath(UriPathMode.Exact, UriSpec.RequireSegmentCount(count, nameof(count)), UriSpec.SegmentsLabel(count)));
    }

    /// <summary>Renders the root path (<c>/</c>) with no segments. Declared once per generator.</summary>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a path constraint is already declared.</exception>
    public AnyWebUri WithoutPath() {
        return new AnyWebUri(_source, _spec.WithPath(UriPathMode.Root, 0, "WithoutPath()"));
    }

    /// <summary>Includes an arbitrary query string.</summary>
    public AnyWebUri WithQuery() {
        return new AnyWebUri(_source, _spec.WithQuery());
    }

    /// <summary>Includes an arbitrary fragment.</summary>
    public AnyWebUri WithFragment() {
        return new AnyWebUri(_source, _spec.WithFragment());
    }

    /// <inheritdoc />
    public Uri Generate() {
        return _spec.Generate(_source);
    }

}
