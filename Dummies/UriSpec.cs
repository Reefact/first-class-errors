#region Usings declarations

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

#endregion

namespace Dummies;

/// <summary>The URI families the builder can produce, one per distinct valid shape.</summary>
internal enum UriFamily {

    Web,       // http / https  — full authority: host, userinfo, port, path, query, fragment
    WebSocket, // ws   / wss    — authority without userinfo or fragment (RFC 6455)
    Ftp,       // ftp           — authority without query or fragment
    Mailto,    // mailto        — local@domain (+ optional headers)
    Relative   // no scheme     — path (+ optional query, fragment)

}

/// <summary>How the path component is drawn.</summary>
internal enum UriPathMode {

    Auto, // 0 to 2 arbitrary segments
    Root, // no segments (an authority family still renders "/")
    Exact // a fixed number of segments

}

/// <summary>
///     The immutable specification behind every <c>AnyUri</c> family builder. It records the constrained family and
///     scheme plus the per-component pins, and assembles a <see cref="Uri" /> <b>directly</b> — every component is
///     drawn from ASCII-unreserved characters, so a generated URI is valid by construction (never a throw, never a
///     retry) and reproducible under a seed on every target framework. Contradictory constraints fail eagerly with a
///     <see cref="ConflictingAnyConstraintException" /> naming both sides; an invalid pinned component fails as an
///     argument at the call site.
/// </summary>
internal sealed class UriSpec {

    #region Constants

    private const string LowerLetters   = "abcdefghijklmnopqrstuvwxyz";
    private const string LowerAlphaNum  = "abcdefghijklmnopqrstuvwxyz0123456789";
    private const string Unreserved     = "abcdefghijklmnopqrstuvwxyz0123456789-._~";
    private const int    MinDynamicPort = 1025; // above every default we emit (http 80, https 443, ftp 21, ws 80, wss 443)

    #endregion

    #region Statics members declarations

    internal static readonly UriSpec Unconstrained = new(null, null, null, null,
                                                         false, null, null,
                                                         false, null,
                                                         UriPathMode.Auto, 0,
                                                         false, false, false);

    private static readonly UriFamily[] DefaultFamilies = { UriFamily.Web, UriFamily.WebSocket, UriFamily.Ftp, UriFamily.Mailto, UriFamily.Relative };

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string SegmentsLabel(int count) {
        return $"WithPathSegments({V(count)})";
    }

    #endregion

    #region Fields declarations

    private readonly UriFamily?   _family;
    private readonly string?      _scheme;           // pinned concrete scheme within the family (e.g. "https")
    private readonly string?      _schemeConstraint; // the call that pinned it, for a conflict message
    private readonly string?      _host;             // pinned host / mailto domain
    private readonly bool         _hasUserInfo;
    private readonly string?      _user;
    private readonly string?      _password;
    private readonly bool         _hasPort;
    private readonly int?         _port;
    private readonly UriPathMode  _pathMode;
    private readonly string?      _pathConstraint;
    private readonly int          _pathSegments;
    private readonly bool         _hasQuery;
    private readonly bool         _hasFragment;
    private readonly bool         _rooted;

    #endregion

    private UriSpec(UriFamily? family, string? scheme, string? schemeConstraint, string? host,
                    bool hasUserInfo, string? user, string? password,
                    bool hasPort, int? port,
                    UriPathMode pathMode, int pathSegments,
                    bool hasQuery, bool hasFragment, bool rooted,
                    string? pathConstraint = null) {
        _family           = family;
        _scheme           = scheme;
        _schemeConstraint = schemeConstraint;
        _host             = host;
        _hasUserInfo      = hasUserInfo;
        _user             = user;
        _password         = password;
        _hasPort          = hasPort;
        _port             = port;
        _pathMode         = pathMode;
        _pathConstraint   = pathConstraint;
        _pathSegments     = pathSegments;
        _hasQuery         = hasQuery;
        _hasFragment      = hasFragment;
        _rooted           = rooted;
    }

    #region Family narrowing

    internal UriSpec WithFamily(UriFamily family) {
        return new UriSpec(family, _scheme, _schemeConstraint, _host, _hasUserInfo, _user, _password,
                           _hasPort, _port, _pathMode, _pathSegments, _hasQuery, _hasFragment, _rooted, _pathConstraint);
    }

    internal UriSpec WithScheme(string scheme, string applying) {
        if (_schemeConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_schemeConstraint} is already defined."); }

        return new UriSpec(_family, scheme, applying, _host, _hasUserInfo, _user, _password,
                           _hasPort, _port, _pathMode, _pathSegments, _hasQuery, _hasFragment, _rooted, _pathConstraint);
    }

    #endregion

    #region Component pins

    internal UriSpec WithHost(string host) {
        return new UriSpec(_family, _scheme, _schemeConstraint, host, _hasUserInfo, _user, _password,
                           _hasPort, _port, _pathMode, _pathSegments, _hasQuery, _hasFragment, _rooted, _pathConstraint);
    }

    internal UriSpec WithUserInfo(string? user, string? password) {
        return new UriSpec(_family, _scheme, _schemeConstraint, _host, true, user, password,
                           _hasPort, _port, _pathMode, _pathSegments, _hasQuery, _hasFragment, _rooted, _pathConstraint);
    }

    internal UriSpec WithPort(int? port) {
        return new UriSpec(_family, _scheme, _schemeConstraint, _host, _hasUserInfo, _user, _password,
                           true, port, _pathMode, _pathSegments, _hasQuery, _hasFragment, _rooted, _pathConstraint);
    }

    internal UriSpec WithPath(UriPathMode mode, int segments, string applying) {
        if (_pathConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_pathConstraint} is already defined."); }

        return new UriSpec(_family, _scheme, _schemeConstraint, _host, _hasUserInfo, _user, _password,
                           _hasPort, _port, mode, segments, _hasQuery, _hasFragment, _rooted, applying);
    }

    internal UriSpec WithQuery() {
        return new UriSpec(_family, _scheme, _schemeConstraint, _host, _hasUserInfo, _user, _password,
                           _hasPort, _port, _pathMode, _pathSegments, true, _hasFragment, _rooted, _pathConstraint);
    }

    internal UriSpec WithFragment() {
        return new UriSpec(_family, _scheme, _schemeConstraint, _host, _hasUserInfo, _user, _password,
                           _hasPort, _port, _pathMode, _pathSegments, _hasQuery, true, _rooted, _pathConstraint);
    }

    internal UriSpec Rooted() {
        return new UriSpec(_family, _scheme, _schemeConstraint, _host, _hasUserInfo, _user, _password,
                           _hasPort, _port, _pathMode, _pathSegments, _hasQuery, _hasFragment, true, _pathConstraint);
    }

    #endregion

    #region Generation

    internal Uri Generate(RandomSource source) {
        Random    random = source.Current.Random;
        UriFamily family = _family ?? DefaultFamilies[random.Next(DefaultFamilies.Length)];

        return family == UriFamily.Relative
                   ? new Uri(BuildRelative(source), UriKind.Relative)
                   : new Uri(BuildAbsolute(family, random), UriKind.Absolute);
    }

    private string BuildAbsolute(UriFamily family, Random random) {
        string        scheme  = ResolveScheme(family, random);
        StringBuilder builder = new();
        builder.Append(scheme).Append(':');

        if (family == UriFamily.Mailto) {
            builder.Append(_user ?? Draw(random, LowerAlphaNum, 3, 8));
            builder.Append('@');
            builder.Append(_host ?? Host(random));
            if (_hasQuery) { builder.Append("?subject=").Append(Draw(random, LowerAlphaNum, 3, 8)); }

            return builder.ToString();
        }

        builder.Append("//");
        if (AllowsUserInfo(family) && _hasUserInfo) {
            builder.Append(_user ?? Draw(random, LowerAlphaNum, 3, 8));
            builder.Append(':').Append(_password ?? Draw(random, LowerAlphaNum, 3, 8));
            builder.Append('@');
        }

        builder.Append(_host ?? Host(random));
        if (_hasPort) { builder.Append(':').Append(V(_port ?? random.Next(MinDynamicPort, 65536))); }

        builder.Append(Path(random, leadingSlash: true));
        if (AllowsQuery(family) && _hasQuery) { builder.Append(Query(random)); }
        if (AllowsFragment(family) && _hasFragment) { builder.Append('#').Append(Draw(random, LowerAlphaNum, 1, 8)); }

        return builder.ToString();
    }

    private string BuildRelative(RandomSource source) {
        Random        random  = source.Current.Random;
        StringBuilder builder = new();
        builder.Append(Path(random, leadingSlash: _rooted));
        if (_hasQuery) { builder.Append(Query(random)); }
        if (_hasFragment) { builder.Append('#').Append(Draw(random, LowerAlphaNum, 1, 8)); }

        string result = builder.ToString();
        if (result.Length > 0) { return result; }

        // The reference rendered empty, which is not a valid URI. An unconstrained (Auto) path incidentally drew zero
        // segments — resolve it to an arbitrary segment. An explicit WithPathSegments(0) with no query, fragment or
        // root asked for the empty reference, which cannot generate: surface it with the seed to replay, like the
        // library's other unsatisfiable specs.
        if (_pathMode == UriPathMode.Exact) {
            int seed = source.Current.Seed;
            throw new AnyGenerationException(
                "A relative URI with exactly 0 path segments and no query, fragment or root is empty, which is not a valid URI reference. Add a query, a fragment, Rooted(), or a positive segment count. " + source.ReplayHint(seed),
                seed);
        }

        return Draw(random, LowerAlphaNum, 1, 8);
    }

    private string ResolveScheme(UriFamily family, Random random) {
        if (_scheme is not null) { return _scheme; }

        return family switch {
            UriFamily.Web       => random.Next(2) == 0 ? "http" : "https",
            UriFamily.WebSocket => random.Next(2) == 0 ? "ws" : "wss",
            UriFamily.Ftp       => "ftp",
            UriFamily.Mailto    => "mailto",
            _                   => throw new InvalidOperationException("Relative URIs have no scheme.")
        };
    }

    private string Path(Random random, bool leadingSlash) {
        int count = _pathMode switch {
            UriPathMode.Root  => 0,
            UriPathMode.Exact => _pathSegments,
            _                 => random.Next(3) // 0..2
        };

        if (count == 0) { return leadingSlash ? "/" : string.Empty; }

        StringBuilder builder = new();
        for (int i = 0; i < count; i++) {
            if (leadingSlash || i > 0) { builder.Append('/'); }
            builder.Append(Draw(random, LowerAlphaNum, 1, 8));
        }

        return builder.ToString();
    }

    private static string Query(Random random) {
        int           pairs   = random.Next(1, 3); // 1..2
        StringBuilder builder = new("?");
        for (int i = 0; i < pairs; i++) {
            if (i > 0) { builder.Append('&'); }
            builder.Append(Draw(random, LowerAlphaNum, 1, 6)).Append('=').Append(Draw(random, LowerAlphaNum, 1, 6));
        }

        return builder.ToString();
    }

    private static string Host(Random random) {
        return Label(random) + "." + Draw(random, LowerLetters, 2, 4);
    }

    private static string Label(Random random) {
        // A DNS-safe label: starts with a letter, then letters/digits — no leading digit, no hyphen edges.
        return LowerLetters[random.Next(LowerLetters.Length)].ToString() + Draw(random, LowerAlphaNum, 0, 7);
    }

    private static string Draw(Random random, string pool, int min, int max) {
        int           length  = min == max ? min : random.Next(min, max + 1);
        StringBuilder builder = new(length);
        for (int i = 0; i < length; i++) { builder.Append(pool[random.Next(pool.Length)]); }

        return builder.ToString();
    }

    private static bool AllowsUserInfo(UriFamily family) {
        return family is UriFamily.Web or UriFamily.Ftp;
    }

    private static bool AllowsQuery(UriFamily family) {
        return family is UriFamily.Web or UriFamily.WebSocket;
    }

    private static bool AllowsFragment(UriFamily family) {
        return family is UriFamily.Web;
    }

    #endregion

    #region Argument validation helpers (shared by the public builders)

    internal static string RequireHost(string host, string parameterName) {
        if (host is null) { throw new ArgumentNullException(parameterName); }
        if (host.Length == 0) { throw new ArgumentException("The host must not be empty.", parameterName); }
        if (host.Any(character => character > 127)) {
            throw new ArgumentException("The host must be ASCII: an internationalized (IDN) host would not round-trip identically across target frameworks. Pass the punycode form instead (e.g. \"xn--mnchen-3ya.de\").", parameterName);
        }
        if (Uri.CheckHostName(host) == UriHostNameType.Unknown) {
            throw new ArgumentException($"\"{host}\" is not a valid host name.", parameterName);
        }

        // A host of only digits and dots is interpreted as an IPv4 literal by System.Uri, and its shorthand forms
        // ("1" -> "0.0.0.1") parse differently across target frameworks — the same determinism hazard as an IDN host.
        // Reject any such host that is not already a canonical four-octet dotted-quad, with a framework-independent
        // check (never through System.Uri, whose parsing is the very thing that differs).
        if (host.All(character => character is >= '0' and <= '9' or '.') && !IsCanonicalIpv4(host)) {
            throw new ArgumentException($"The host \"{host}\" looks like a shorthand IPv4 literal, which System.Uri parses differently across target frameworks. Pass a DNS host name, or a canonical dotted-quad such as \"1.2.3.4\".", parameterName);
        }

        return host;
    }

    private static bool IsCanonicalIpv4(string host) {
        string[] parts = host.Split(new[] { '.' });
        if (parts.Length != 4) { return false; }
        foreach (string part in parts) {
            if (part.Length is 0 or > 3) { return false; }
            if (part.Length > 1 && part[0] == '0') { return false; } // a leading zero is a non-canonical (octal-ish) octet
            if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int octet) || octet > 255) { return false; }
        }

        return true;
    }

    internal static string RequireUserInfoPart(string value, string parameterName) {
        if (value is null) { throw new ArgumentNullException(parameterName); }
        foreach (char character in value) {
            if (Unreserved.IndexOf(char.ToLowerInvariant(character)) < 0) {
                throw new ArgumentException($"The user-info part may only contain unreserved characters (letters, digits, '-', '.', '_', '~'); '{character}' is not allowed.", parameterName);
            }
        }

        return value;
    }

    internal static int RequirePort(int port, string parameterName) {
        if (port is < 1 or > 65535) { throw new ArgumentOutOfRangeException(parameterName, port, "The port must be between 1 and 65535."); }

        return port;
    }

    internal static int RequireSegmentCount(int count, string parameterName) {
        if (count < 0) { throw new ArgumentOutOfRangeException(parameterName, count, "The segment count must not be negative."); }

        return count;
    }

    #endregion

}
