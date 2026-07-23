namespace Dummies;

/// <summary>
///     A generator of arbitrary <c>mailto</c> URIs — <c>mailto:local@domain</c>, optionally with headers. The
///     local-part and domain are drawn from ASCII characters, so the value is an arbitrary well-formed address, never
///     a realistic one (this library does not fabricate plausible data). A mailto URI is not authority-based, so this
///     builder exposes no host/port/user-info in the authority sense — only the address parts and headers.
/// </summary>
public sealed class AnyMailtoUri : IAny<Uri>, IHasRandomSource {

    #region Fields declarations

    private readonly RandomSource _source;
    private readonly UriSpec      _spec;

    #endregion

    internal AnyMailtoUri(RandomSource source, UriSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Pins the local-part (the text before <c>@</c>).</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="localPart" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="localPart" /> contains a non-unreserved character.</exception>
    public AnyMailtoUri WithLocalPart(string localPart) {
        return new AnyMailtoUri(_source, _spec.WithUserInfo(UriSpec.RequireUserInfoPart(localPart, nameof(localPart)), null));
    }

    /// <summary>Pins the domain (the text after <c>@</c>). Must be an ASCII host name.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domain" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="domain" /> is empty, non-ASCII or not a valid host name.</exception>
    public AnyMailtoUri WithDomain(string domain) {
        return new AnyMailtoUri(_source, _spec.WithHost(UriSpec.RequireHost(domain, nameof(domain))));
    }

    /// <summary>Includes an arbitrary header (e.g. <c>?subject=...</c>).</summary>
    public AnyMailtoUri WithHeaders() {
        return new AnyMailtoUri(_source, _spec.WithQuery());
    }

    /// <inheritdoc />
    public Uri Generate() {
        return _spec.Generate(_source);
    }

}
