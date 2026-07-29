#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for the <see cref="AnyUri" /> family. The example-based suite pins one host
///     (<c>api.example.com</c>), one port (<c>8443</c>) and one segment count (<c>3</c>), and can only prove the
///     builder right for those; these quantify over the whole option space of each family — every host the library
///     accepts, every port in 1..65535, every small segment count, and every on/off combination of the optional
///     components — so a shape that renders an unparsable URI for one combination is found and shrunk to its minimal
///     counter-example rather than missed.
/// </summary>
/// <remarks>
///     The family narrowings are a <b>typed</b> progression: a category error such as a port on a mailto or a fragment
///     on a WebSocket is a compile error, not an exception, so there is nothing here to assert about it. Only two
///     conflicts survive to run time — a second scheme constraint and a second path constraint — and both are proven
///     below over arbitrary arguments.
/// </remarks>
[TestSubject(typeof(AnyUri))]
public sealed class UriProperties {

    private const string LowerLetters  = "abcdefghijklmnopqrstuvwxyz";
    private const string LowerAlphaNum = "abcdefghijklmnopqrstuvwxyz0123456789";
    private const string Unreserved    = "abcdefghijklmnopqrstuvwxyz0123456789-_";

    /// <summary>
    ///     Which path constraint a case declares. <c>Unconstrained</c> declares none at all — the third state a
    ///     nullable segment count cannot express, and the only one that leaves the segment count to the draw.
    /// </summary>
    private enum PathChoice {

        Unconstrained,
        Root,
        Exact

    }

    /// <summary>Which of the three <c>WithUserInfo</c> overloads a case calls.</summary>
    private enum UserInfoChoice {

        Arbitrary,      // WithUserInfo()               — both parts drawn
        UserOnly,       // WithUserInfo(user)           — user pinned, password drawn
        UserAndPassword // WithUserInfo(user, password) — both parts pinned

    }

    #region Statics members declarations

    /// <summary>
    ///     The schemes the library is allowed to emit. <c>file</c> is deliberately absent: a file path does not
    ///     round-trip identically across target frameworks, so the unconstrained draw must never reach it.
    /// </summary>
    private static readonly HashSet<string> EmittableSchemes = ["http", "https", "ws", "wss", "ftp", "mailto"];

    /// <summary>
    ///     Arbitrary hosts the library accepts, drawn from the very alphabet <c>UriSpec</c> draws its own hosts from: a
    ///     DNS label, optionally followed by a second one. Staying inside that alphabet keeps every generated host on
    ///     the legal side of <c>WithHost</c>, so the property exercises the pinning rather than the validation.
    /// </summary>
    private static Gen<string> Hosts() {
        return from first in Labels()
               from second in Gen.OneOf(Gen.Constant(string.Empty), Labels().Select(label => "." + label))
               select first + second;
    }

    /// <summary>
    ///     A DNS-safe label: one letter, then up to seven letters or digits. Capping the tail matters — an unbounded
    ///     FsCheck list would eventually exceed the 63-character label ceiling and turn a pinning property into an
    ///     argument-validation one.
    /// </summary>
    private static Gen<string> Labels() {
        return from head in Gen.Elements(LowerLetters.ToCharArray())
               from tail in Gen.ListOf(Gen.Elements(LowerAlphaNum.ToCharArray()))
               select head.ToString() + new string(tail.Take(7).ToArray());
    }

    /// <summary>
    ///     Arbitrary user-info parts and mailto local-parts: non-empty, starting with a letter or a digit, and drawn
    ///     from the unreserved characters <c>RequireUserInfoPart</c> accepts.
    /// </summary>
    private static Gen<string> UnreservedParts() {
        return from head in Gen.Elements(LowerAlphaNum.ToCharArray())
               from tail in Gen.ListOf(Gen.Elements(Unreserved.ToCharArray()))
               select head.ToString() + new string(tail.Take(7).ToArray());
    }

    /// <summary>Arbitrary legal ports — the whole 1..65535 range, not the two or three a hand-written test would pick.</summary>
    private static Gen<int> Ports() {
        return Gen.Choose(1, 65535);
    }

    /// <summary>An option the caller may leave undeclared: <c>null</c> stands for "the call was never made".</summary>
    private static Gen<T?> Optional<T>(Gen<T> values)
        where T : struct {
        return Gen.OneOf(Gen.Constant((T?)null), values.Select(value => (T?)value));
    }

    /// <summary>Counts the non-empty segments of a path, the way a reader counts the slashes.</summary>
    private static int SegmentCount(string path) {
        return path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>Applies one of the two web scheme pins — the pair that conflicts with itself in either order.</summary>
    private static AnyWebUri PinScheme(AnyWebUri generator, bool secure) {
        return secure ? generator.UsingHttps() : generator.UsingHttp();
    }

    /// <summary>Applies one of the two WebSocket scheme pins — the pair that conflicts with itself in either order.</summary>
    private static AnyWebSocketUri PinScheme(AnyWebSocketUri generator, bool secure) {
        return secure ? generator.UsingWss() : generator.UsingWs();
    }

    /// <summary>Declares a path constraint: a segment count, or the root path when <paramref name="segments" /> is <c>null</c>.</summary>
    private static AnyWebUri PinPath(AnyWebUri generator, int? segments) {
        return segments.HasValue ? generator.WithPathSegments(segments.Value) : generator.WithoutPath();
    }

    /// <summary>
    ///     Projects a path declaration onto a positive segment count for the relative leg, which has no
    ///     <c>WithoutPath()</c>. Injective on purpose — <c>null</c> and <c>0</c> are two different declarations and must
    ///     stay two different counts — and never zero, because a relative reference with no segment, query, fragment or
    ///     root is the empty string, which is not a valid URI reference.
    /// </summary>
    private static int RelativeSegments(int? declared) {
        return declared is null ? 1 : declared.Value + 2;
    }

    #endregion

    [Fact(DisplayName = "Every unconstrained draw is a valid URI of an emittable family, whatever the seed.")]
    public void UnconstrainedDrawsAreValidUris() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => Expect.EveryDraw(Any.WithSeed(seed).Uri(),
                                             value => {
                                                 UriKind kind = value.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative;

                                                 return value.OriginalString.Length > 0
                                                        // Every component is ASCII by construction: an internationalized host
                                                        // would not round-trip identically across target frameworks.
                                                        && value.OriginalString.All(character => character < 128)
                                                        && (!value.IsAbsoluteUri || EmittableSchemes.Contains(value.Scheme))
                                                        && Uri.TryCreate(value.OriginalString, kind, out _);
                                             },
                                             16))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "The unconstrained draw reaches all five families, whatever the seed.")]
    public void UnconstrainedReachesEveryFamily() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        // One family in five per draw, so 120 draws leave a miss far below any rate that could make
                        // this flaky, while a hand-written test can only ever assert it for the one seed it picked.
                        HashSet<string> seen = [];
                        foreach (Uri value in Expect.Draws(Any.WithSeed(seed).Uri(), 120)) {
                            seen.Add(value.IsAbsoluteUri ? value.Scheme : "relative");
                        }

                        return (seen.Contains("http") || seen.Contains("https"))
                               && (seen.Contains("ws") || seen.Contains("wss"))
                               && seen.Contains("ftp")
                               && seen.Contains("mailto")
                               && seen.Contains("relative");
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A web draw carries exactly the components declared on it, in every combination.")]
    public void WebDrawsCarryTheDeclaredComponents() {
        Gen<(string Host, int? Port, PathChoice Path, int Segments, bool? Secure, bool Query, bool Fragment)> cases =
            from host in Hosts()
            from port in Optional(Ports())
            from path in Gen.Elements(PathChoice.Unconstrained, PathChoice.Root, PathChoice.Exact)
            from segments in Generators.Count(6)
            from secure in Optional(Gen.Elements(true, false))
            from query in Gen.Elements(true, false)
            from fragment in Gen.Elements(true, false)
            select (host, port, path, segments, secure, query, fragment);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        AnyWebUri generator = Any.Uri().Web().WithHost(testCase.Host);
                        if (testCase.Secure.HasValue) { generator = PinScheme(generator, testCase.Secure.Value); }
                        if (testCase.Port.HasValue) { generator = generator.WithPort(testCase.Port.Value); }
                        if (testCase.Path == PathChoice.Root) { generator = generator.WithoutPath(); }
                        if (testCase.Path == PathChoice.Exact) { generator = generator.WithPathSegments(testCase.Segments); }
                        if (testCase.Query) { generator = generator.WithQuery(); }
                        if (testCase.Fragment) { generator = generator.WithFragment(); }

                        return Expect.EveryDraw(generator,
                                                value => {
                                                    bool pathHolds = testCase.Path switch {
                                                        PathChoice.Root  => value.AbsolutePath == "/",
                                                        PathChoice.Exact => SegmentCount(value.AbsolutePath) == testCase.Segments,
                                                        // An undeclared path draws 0 to 2 segments.
                                                        _ => SegmentCount(value.AbsolutePath) <= 2
                                                    };

                                                    return value.IsAbsoluteUri
                                                           && (testCase.Secure.HasValue
                                                                   ? value.Scheme == (testCase.Secure.Value ? "https" : "http")
                                                                   : value.Scheme is "http" or "https")
                                                           && value.Host == testCase.Host
                                                           && (!testCase.Port.HasValue || value.Port == testCase.Port.Value)
                                                           && pathHolds
                                                           && value.UserInfo.Length == 0
                                                           && (value.Query.Length > 0) == testCase.Query
                                                           && (value.Fragment.Length > 0) == testCase.Fragment;
                                                });
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Declared user-info reaches the URI, whichever of the three overloads declared it.")]
    public void UserInfoShapesReachTheUri() {
        Gen<(UserInfoChoice Choice, string User, string Password)> cases =
            from choice in Gen.Elements(UserInfoChoice.Arbitrary, UserInfoChoice.UserOnly, UserInfoChoice.UserAndPassword)
            from user in UnreservedParts()
            from password in UnreservedParts()
            select (choice, user, password);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        AnyWebUri generator = testCase.Choice switch {
                            UserInfoChoice.UserOnly        => Any.Uri().Web().WithUserInfo(testCase.User),
                            UserInfoChoice.UserAndPassword => Any.Uri().Web().WithUserInfo(testCase.User, testCase.Password),
                            _                              => Any.Uri().Web().WithUserInfo()
                        };

                        return Expect.EveryDraw(generator,
                                                value => testCase.Choice switch {
                                                    // Only the user is pinned, so the password is merely required to be there.
                                                    UserInfoChoice.UserOnly => value.UserInfo.StartsWith(testCase.User + ":", StringComparison.Ordinal)
                                                                               && value.UserInfo.Length > testCase.User.Length + 1,
                                                    UserInfoChoice.UserAndPassword => value.UserInfo == testCase.User + ":" + testCase.Password,
                                                    _                              => value.UserInfo.IndexOf(':') > 0
                                                });
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A WebSocket draw uses ws or wss and never carries user-info or a fragment.")]
    public void WebSocketDrawsAreWebSocketUris() {
        Gen<(string Host, PathChoice Path, int Segments, bool? Secure, bool Query)> cases =
            from host in Hosts()
            from path in Gen.Elements(PathChoice.Unconstrained, PathChoice.Root, PathChoice.Exact)
            from segments in Generators.Count(6)
            from secure in Optional(Gen.Elements(true, false))
            from query in Gen.Elements(true, false)
            select (host, path, segments, secure, query);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        AnyWebSocketUri generator = Any.Uri().WebSocket().WithHost(testCase.Host);
                        if (testCase.Secure.HasValue) { generator = PinScheme(generator, testCase.Secure.Value); }
                        if (testCase.Path == PathChoice.Root) { generator = generator.WithoutPath(); }
                        if (testCase.Path == PathChoice.Exact) { generator = generator.WithPathSegments(testCase.Segments); }
                        if (testCase.Query) { generator = generator.WithQuery(); }

                        return Expect.EveryDraw(generator,
                                                value => {
                                                    // Asserted on the rendered string rather than on the parsed components:
                                                    // ws and wss are not authority-parsed identically on every framework, and
                                                    // the rendering is what the library actually promises.
                                                    string rendered = value.OriginalString;

                                                    return value.IsAbsoluteUri
                                                           && (testCase.Secure.HasValue
                                                                   ? value.Scheme == (testCase.Secure.Value ? "wss" : "ws")
                                                                   : value.Scheme is "ws" or "wss")
                                                           && rendered.StartsWith(value.Scheme + "://" + testCase.Host, StringComparison.Ordinal)
                                                           && (rendered.IndexOf('?') >= 0) == testCase.Query
                                                           && rendered.IndexOf('#') < 0
                                                           && rendered.IndexOf('@') < 0;
                                                });
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "An FTP draw uses the ftp scheme with its user-info, and never a query or a fragment.")]
    public void FtpDrawsAreFtpUris() {
        Gen<(string Host, int? Port, PathChoice Path, int Segments, bool Credentials, string User, string Password)> cases =
            from host in Hosts()
            from port in Optional(Ports())
            from path in Gen.Elements(PathChoice.Unconstrained, PathChoice.Root, PathChoice.Exact)
            from segments in Generators.Count(6)
            from credentials in Gen.Elements(true, false)
            from user in UnreservedParts()
            from password in UnreservedParts()
            select (host, port, path, segments, credentials, user, password);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        AnyFtpUri generator = Any.Uri().Ftp().WithHost(testCase.Host);
                        if (testCase.Port.HasValue) { generator = generator.WithPort(testCase.Port.Value); }
                        if (testCase.Path == PathChoice.Root) { generator = generator.WithoutPath(); }
                        if (testCase.Path == PathChoice.Exact) { generator = generator.WithPathSegments(testCase.Segments); }
                        if (testCase.Credentials) { generator = generator.WithUserInfo(testCase.User, testCase.Password); }

                        return Expect.EveryDraw(generator,
                                                value => value.IsAbsoluteUri
                                                         && value.Scheme == "ftp"
                                                         && value.Host == testCase.Host
                                                         && (!testCase.Port.HasValue || value.Port == testCase.Port.Value)
                                                         && value.UserInfo == (testCase.Credentials ? testCase.User + ":" + testCase.Password : string.Empty)
                                                         // An FTP URI has neither, and the builder does not even expose them.
                                                         && value.OriginalString.IndexOf('?') < 0
                                                         && value.OriginalString.IndexOf('#') < 0);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A mailto draw renders local@domain, honouring whichever part is pinned.")]
    public void MailtoDrawsRenderTheDeclaredAddress() {
        Gen<(string Local, string Domain, bool PinLocal, bool PinDomain, bool Headers)> cases =
            from local in UnreservedParts()
            from domain in Hosts()
            from pinLocal in Gen.Elements(true, false)
            from pinDomain in Gen.Elements(true, false)
            from headers in Gen.Elements(true, false)
            select (local, domain, pinLocal, pinDomain, headers);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        AnyMailtoUri generator = Any.Uri().Mailto();
                        if (testCase.PinLocal) { generator = generator.WithLocalPart(testCase.Local); }
                        if (testCase.PinDomain) { generator = generator.WithDomain(testCase.Domain); }
                        if (testCase.Headers) { generator = generator.WithHeaders(); }

                        return Expect.EveryDraw(generator,
                                                value => {
                                                    if (!value.IsAbsoluteUri || value.Scheme != "mailto") { return false; }
                                                    if (!value.OriginalString.StartsWith("mailto:", StringComparison.Ordinal)) { return false; }

                                                    string address     = value.OriginalString.Substring("mailto:".Length);
                                                    int    headerStart = address.IndexOf('?');
                                                    if ((headerStart >= 0) != testCase.Headers) { return false; }
                                                    if (headerStart >= 0) { address = address.Substring(0, headerStart); }

                                                    string[] parts = address.Split(new[] { '@' });

                                                    return parts.Length == 2
                                                           && parts[0].Length > 0
                                                           && parts[1].Length > 0
                                                           && (!testCase.PinLocal || parts[0] == testCase.Local)
                                                           && (!testCase.PinDomain || parts[1] == testCase.Domain);
                                                });
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A relative draw is a relative reference carrying exactly the declared path, query and fragment.")]
    public void RelativeDrawsAreRelativeReferences() {
        Gen<(int? Segments, bool Rooted, bool Query, bool Fragment)> cases =
            from segments in Optional(Generators.Count(6))
            from rooted in Gen.Elements(true, false)
            from query in Gen.Elements(true, false)
            from fragment in Gen.Elements(true, false)
            select (segments, rooted, query, fragment);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        AnyRelativeUri generator = Any.Uri().Relative();
                        if (testCase.Rooted) { generator = generator.Rooted(); }
                        if (testCase.Segments.HasValue) { generator = generator.WithPathSegments(testCase.Segments.Value); }
                        if (testCase.Query) { generator = generator.WithQuery(); }
                        if (testCase.Fragment) { generator = generator.WithFragment(); }

                        // An explicit zero-segment path with nothing else to carry it renders the empty string, which is
                        // not a valid reference: the one shape of this family that cannot generate.
                        if (testCase.Segments == 0 && !testCase.Rooted && !testCase.Query && !testCase.Fragment) {
                            return Expect.Throws<AnyGenerationException>(() => generator.Generate());
                        }

                        return Expect.EveryDraw(generator,
                                                value => {
                                                    string reference = value.OriginalString;
                                                    int    cut       = reference.IndexOfAny(new[] { '?', '#' });
                                                    int    segments  = SegmentCount(cut < 0 ? reference : reference.Substring(0, cut));

                                                    return !value.IsAbsoluteUri
                                                           && reference.Length > 0
                                                           && (!testCase.Rooted || reference.StartsWith("/", StringComparison.Ordinal))
                                                           && (testCase.Segments.HasValue ? segments == testCase.Segments.Value : segments <= 2)
                                                           && (reference.IndexOf('?') >= 0) == testCase.Query
                                                           && (reference.IndexOf('#') >= 0) == testCase.Fragment;
                                                });
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "An undeclared relative path never renders empty: a zero-segment draw is resolved to one segment.")]
    public void UnconstrainedRelativeNeverRendersEmpty() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    // The draw picks 0, 1 or 2 segments; the 0 that would render the empty string is silently resolved
                    // to a single arbitrary segment, so the count never leaves 1..2 and generation never fails.
                    seed => Expect.EveryDraw(Any.WithSeed(seed).Uri().Relative(),
                                             value => !value.IsAbsoluteUri
                                                      && value.OriginalString.Length > 0
                                                      && SegmentCount(value.OriginalString) is 1 or 2,
                                             24))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "An explicit zero-segment relative path with nothing else fails at generation, carrying the seed.")]
    public void EmptyRelativeFailsAtGenerationCarryingTheSeed() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        try {
                            Any.WithSeed(seed).Uri().Relative().WithPathSegments(0).Generate();

                            return false;
                        } catch (AnyGenerationException error) {
                            return error.Seed == seed;
                        }
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithPort pins the port over the whole legal range, and rejects anything outside it as an argument.")]
    public void PortsArePinnedOrRejectedAsArguments() {
        Gen<int> candidates = Generators.WithEdges(Gen.OneOf(Ports(), Generators.Int32()),
                                                   -1, 0, 1, 65535, 65536, int.MinValue, int.MaxValue);

        Prop.ForAll(candidates.ToArbitrary(),
                    port => port is < 1 or > 65535
                                ? Expect.Throws<ArgumentOutOfRangeException>(() => Any.Uri().Web().WithPort(port))
                                : Expect.EveryDraw(Any.Uri().Web().WithPort(port), value => value.Port == port))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "The argument-less WithPort yields an explicit, non-default port on every draw.")]
    public void ArbitraryPortsAreExplicitAndNonDefault() {
        Prop.ForAll(Hosts().ToArbitrary(),
                    host => Expect.EveryDraw(Any.Uri().Web().WithHost(host).WithPort(),
                                             // Drawn above every default the library emits, so the port is always visible.
                                             value => value.Port >= 1025 && value.Port <= 65535 && !value.IsDefaultPort))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A second scheme pin conflicts unless it repeats the first, whichever pair and whichever order.")]
    public void SecondSchemePinConflictsUnlessItRepeatsTheFirst() {
        Gen<(bool First, bool Second)> cases = from first in Gen.Elements(true, false)
                                               from second in Gen.Elements(true, false)
                                               select (first, second);

        Prop.ForAll(cases.ToArbitrary(),
                    // Pinning the same scheme twice asks for the scheme already in force, so it is a no-op and the
                    // generator still produces it; pinning the other one contradicts it.
                    testCase => testCase.First == testCase.Second
                                    ? Expect.EveryDraw(PinScheme(PinScheme(Any.Uri().Web(), testCase.First), testCase.Second), uri => uri.IsAbsoluteUri)
                                      && Expect.EveryDraw(PinScheme(PinScheme(Any.Uri().WebSocket(), testCase.First), testCase.Second), uri => uri.IsAbsoluteUri)
                                    : Expect.Throws<ConflictingAnyConstraintException>(
                                          () => PinScheme(PinScheme(Any.Uri().Web(), testCase.First), testCase.Second))
                                      && Expect.Throws<ConflictingAnyConstraintException>(
                                          () => PinScheme(PinScheme(Any.Uri().WebSocket(), testCase.First), testCase.Second)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A second path constraint conflicts unless it repeats the first, whichever pair and whichever segment counts.")]
    public void SecondPathConstraintConflictsUnlessItRepeatsTheFirst() {
        // null stands for WithoutPath(), a count for WithPathSegments(count): all four ordered pairs conflict.
        Gen<(int? First, int? Second)> cases = from first in Optional(Generators.Count(6))
                                               from second in Optional(Generators.Count(6))
                                               select (first, second);

        Prop.ForAll(cases.ToArbitrary(),
                    // Repeating the SAME path declaration is a no-op; any other pair contradicts. The relative leg has
                    // no WithoutPath(), so it only ever exercises the doubled segment count.
                    testCase => testCase.First == testCase.Second
                                    ? Expect.EveryDraw(PinPath(PinPath(Any.Uri().Web(), testCase.First), testCase.Second), uri => uri.IsAbsoluteUri)
                                      // Shifted off zero: a relative reference with no segment, query, fragment or root
                                      // is the empty string, which is not a valid URI reference — a pre-existing refusal
                                      // this property is not about.
                                      && Expect.EveryDraw(Any.Uri().Relative().WithPathSegments(RelativeSegments(testCase.First)).WithPathSegments(RelativeSegments(testCase.Second)),
                                                          uri => !uri.IsAbsoluteUri)
                                    : Expect.Throws<ConflictingAnyConstraintException>(
                                          () => PinPath(PinPath(Any.Uri().Web(), testCase.First), testCase.Second))
                                      && Expect.Throws<ConflictingAnyConstraintException>(
                                          () => Any.Uri().Relative().WithPathSegments(RelativeSegments(testCase.First)).WithPathSegments(RelativeSegments(testCase.Second))))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A negative segment count is an argument error even when a path constraint already conflicts with it.")]
    public void NegativeSegmentCountsAreArgumentErrorsBeforeConflicts() {
        Gen<int> negatives = Generators.WithEdges(Generators.Count(8).Select(offset => -1 - offset), -1, int.MinValue);

        Gen<(int? Declared, int Count)> cases = from declared in Optional(Generators.Count(6))
                                                from count in negatives
                                                select (declared, count);

        Prop.ForAll(cases.ToArbitrary(),
                    // Argument validation runs before conflict checking, so the argument error wins over the conflict
                    // the second path constraint would otherwise raise.
                    testCase => Expect.Throws<ArgumentOutOfRangeException>(
                        () => PinPath(Any.Uri().Web(), testCase.Declared).WithPathSegments(testCase.Count)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A host carrying a non-ASCII character is rejected as an argument, wherever the character sits.")]
    public void NonAsciiHostsAreRejectedAsArguments() {
        Gen<string> cases = from host in Hosts()
                            from character in Gen.Elements('é', 'ü', 'ñ', 'ß', 'д', '中')
                            from index in Gen.Choose(0, host.Length)
                            select host.Insert(index, character.ToString());

        Prop.ForAll(cases.ToArbitrary(),
                    // An internationalized host is refused at the call site, pointing at punycode — never silently
                    // accepted, because it would not round-trip identically across target frameworks.
                    spoiled => Expect.Throws<ArgumentException>(() => Any.Uri().Web().WithHost(spoiled)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A user-info part carrying a reserved character is rejected as an argument, wherever it sits.")]
    public void ReservedUserInfoCharactersAreRejectedAsArguments() {
        Gen<string> cases = from part in UnreservedParts()
                            from character in Gen.Elements(':', '/', '?', '#', '[', ']', '@', '!', '$', '&', '(', ')', '*', '+', ',', ';', '=', '%', ' ')
                            from index in Gen.Choose(0, part.Length)
                            select part.Insert(index, character.ToString());

        Prop.ForAll(cases.ToArbitrary(),
                    spoiled => Expect.Throws<ArgumentException>(() => Any.Uri().Web().WithUserInfo(spoiled))
                               && Expect.Throws<ArgumentException>(() => Any.Uri().Mailto().WithLocalPart(spoiled)))
            .QuickCheckThrowOnFailure();
    }

}
