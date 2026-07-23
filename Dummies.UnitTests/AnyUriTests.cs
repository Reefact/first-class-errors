#region Usings declarations

using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using NFluent;

using Xunit;

#endregion

namespace Dummies.UnitTests;

[TestSubject(typeof(AnyUri))]
public sealed class AnyUriTests {

    private const int FuzzSeeds   = 500;
    private const int SampleCount = 300;

    #region Statics members declarations

    // Valid by construction: across many seeds a family generator never throws and yields the expected URI kind.
    private static void GeneratesAcross(Func<AnyContext, IAny<Uri>> build, UriKind? kind) {
        for (int seed = 0; seed < FuzzSeeds; seed++) {
            Uri value;
            try { value = build(Any.WithSeed(seed)).Generate(); }
            catch (Exception error) { Assert.Fail($"seed {seed}: {error.GetType().Name}: {error.Message}"); return; }

            if (kind.HasValue) {
                Check.WithCustomMessage($"seed {seed}: '{value.OriginalString}'")
                     .That(value.IsAbsoluteUri).IsEqualTo(kind.Value == UriKind.Absolute);
            }
        }
    }

    private static IEnumerable<Uri> Sample(IAny<Uri> generator) {
        for (int i = 0; i < SampleCount; i++) { yield return generator.Generate(); }
    }

    private static IAny<Uri> Seeded(Func<AnyContext, IAny<Uri>> build) {
        return build(Any.WithSeed(20260723));
    }

    #endregion

    [Fact(DisplayName = "Every family is valid by construction: it never throws and yields the expected URI kind.")]
    public void EveryFamilyGeneratesValidUris() {
        GeneratesAcross(context => context.Uri(), null);
        GeneratesAcross(context => context.Uri().Web(), UriKind.Absolute);
        GeneratesAcross(context => context.Uri().WebSocket(), UriKind.Absolute);
        GeneratesAcross(context => context.Uri().Ftp(), UriKind.Absolute);
        GeneratesAcross(context => context.Uri().Mailto(), UriKind.Absolute);
        GeneratesAcross(context => context.Uri().Relative(), UriKind.Relative);
    }

    [Fact(DisplayName = "The unconstrained generator reaches every family.")]
    public void UnconstrainedReachesEveryFamily() {
        HashSet<string> seen = new();
        foreach (Uri value in Sample(Seeded(context => context.Uri()))) {
            seen.Add(value.IsAbsoluteUri ? value.Scheme : "relative");
        }

        Check.That(seen.Contains("http") || seen.Contains("https")).IsTrue();
        Check.That(seen.Contains("ws") || seen.Contains("wss")).IsTrue();
        Check.That(seen.Contains("ftp")).IsTrue();
        Check.That(seen.Contains("mailto")).IsTrue();
        Check.That(seen.Contains("relative")).IsTrue();
    }

    [Fact(DisplayName = "Web reaches both http and https; each generated value is one of them.")]
    public void WebReachesBothSchemes() {
        HashSet<string> seen = new();
        foreach (Uri value in Sample(Seeded(context => context.Uri().Web()))) {
            seen.Add(value.Scheme);
            Check.That(value.Scheme is "http" or "https").IsTrue();
        }

        Check.That(seen.Contains("http")).IsTrue();
        Check.That(seen.Contains("https")).IsTrue();
    }

    [Fact(DisplayName = "WebSocket reaches both ws and wss.")]
    public void WebSocketReachesBothSchemes() {
        HashSet<string> seen = new();
        foreach (Uri value in Sample(Seeded(context => context.Uri().WebSocket()))) {
            seen.Add(value.Scheme);
            Check.That(value.Scheme is "ws" or "wss").IsTrue();
        }

        Check.That(seen.Contains("ws")).IsTrue();
        Check.That(seen.Contains("wss")).IsTrue();
    }

    [Fact(DisplayName = "UsingHttps pins the scheme to https.")]
    public void UsingHttpsPinsTheScheme() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Web().UsingHttps()))) {
            Check.That(value.Scheme).IsEqualTo("https");
        }
    }

    [Fact(DisplayName = "WithHost pins the host.")]
    public void WithHostPinsTheHost() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Web().WithHost("api.example.com")))) {
            Check.That(value.Host).IsEqualTo("api.example.com");
        }
    }

    [Fact(DisplayName = "WithPort pins the port.")]
    public void WithPortPinsThePort() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Web().WithPort(8443)))) {
            Check.That(value.Port).IsEqualTo(8443);
        }
    }

    [Fact(DisplayName = "WithoutPath renders the root path.")]
    public void WithoutPathRendersRoot() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Web().WithoutPath()))) {
            Check.That(value.AbsolutePath).IsEqualTo("/");
        }
    }

    [Fact(DisplayName = "WithPathSegments renders exactly that many segments.")]
    public void WithPathSegmentsRendersThatManySegments() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Web().WithHost("h.test").WithPathSegments(3)))) {
            Check.That(value.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length).IsEqualTo(3);
        }
    }

    [Fact(DisplayName = "WithUserInfo, WithQuery and WithFragment add their components.")]
    public void OptionalComponentsAreIncluded() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Web().WithUserInfo().WithQuery().WithFragment()))) {
            Check.That(value.UserInfo.Length).IsStrictlyGreaterThan(0);
            Check.That(value.Query.Length).IsStrictlyGreaterThan(0);
            Check.That(value.Fragment.Length).IsStrictlyGreaterThan(0);
        }
    }

    [Fact(DisplayName = "WithUserInfo(user, password) pins both parts.")]
    public void WithUserInfoPinsBothParts() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Ftp().WithUserInfo("admin", "s3cret")))) {
            Check.That(value.UserInfo).IsEqualTo("admin:s3cret");
        }
    }

    [Fact(DisplayName = "Ftp yields the ftp scheme with user-info.")]
    public void FtpYieldsFtpScheme() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Ftp().WithUserInfo()))) {
            Check.That(value.Scheme).IsEqualTo("ftp");
            Check.That(value.UserInfo.Length).IsStrictlyGreaterThan(0);
        }
    }

    [Fact(DisplayName = "Mailto yields the mailto scheme and an address.")]
    public void MailtoYieldsAnAddress() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Mailto()))) {
            Check.That(value.Scheme).IsEqualTo("mailto");
            Check.That(value.OriginalString).StartsWith("mailto:");
            Check.That(value.OriginalString).Contains("@");
        }
    }

    [Fact(DisplayName = "Mailto pins the local-part and domain.")]
    public void MailtoPinsLocalPartAndDomain() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Mailto().WithLocalPart("john").WithDomain("example.test")))) {
            Check.That(value.OriginalString).IsEqualTo("mailto:john@example.test");
        }
    }

    [Fact(DisplayName = "Relative yields a relative reference (not absolute).")]
    public void RelativeYieldsARelativeReference() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Relative()))) {
            Check.That(value.IsAbsoluteUri).IsFalse();
        }
    }

    [Fact(DisplayName = "Relative().Rooted() starts the path with a slash.")]
    public void RootedRelativeStartsWithSlash() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Relative().Rooted().WithPathSegments(2)))) {
            Check.That(value.OriginalString).StartsWith("/");
        }
    }

    [Fact(DisplayName = "A second scheme pin conflicts, naming both sides.")]
    public void SecondSchemePinConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Uri().Web().UsingHttp().UsingHttps());

        Check.That(conflict.Message).Contains("UsingHttps()");
        Check.That(conflict.Message).Contains("UsingHttp()");
    }

    [Fact(DisplayName = "A second path constraint conflicts, naming both sides.")]
    public void SecondPathConstraintConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Uri().Web().WithoutPath().WithPathSegments(2));

        Check.That(conflict.Message).Contains("WithPathSegments(2)");
        Check.That(conflict.Message).Contains("WithoutPath()");
    }

    [Fact(DisplayName = "WithHost rejects a non-ASCII (IDN) host, pointing to punycode.")]
    public void WithHostRejectsIdnHost() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Uri().Web().WithHost("münchen.de"));

        Check.That(error.Message).Contains("punycode");
    }

    [Fact(DisplayName = "Host, user-info, port and segment-count arguments are validated as arguments.")]
    public void ArgumentsAreValidated() {
        Check.ThatCode(() => Any.Uri().Web().WithHost(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.Uri().Web().WithHost("")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Uri().Web().WithHost("bad host")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Uri().Web().WithUserInfo("a:b")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Uri().Web().WithPort(0)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.Uri().Web().WithPort(70000)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.Uri().Web().WithPathSegments(-1)).Throws<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "WithUserInfo(user) keeps the user and draws an arbitrary password.")]
    public void WithUserInfoPinsUserAndDrawsPassword() {
        foreach (Uri value in Sample(Seeded(context => context.Uri().Web().WithUserInfo("bob")))) {
            Check.That(value.UserInfo).StartsWith("bob:");
            Check.That(value.UserInfo.Length).IsStrictlyGreaterThan("bob:".Length);
        }
    }

    [Fact(DisplayName = "A relative URI with an explicit 0 segments and nothing else fails at generation with a seed.")]
    public void EmptyRelativeFailsAtGeneration() {
        AnyGenerationException error = Assert.Throws<AnyGenerationException>(
            () => Any.WithSeed(20260723).Uri().Relative().WithPathSegments(0).Generate());

        Check.That(error.Seed).IsEqualTo(20260723);
    }

    [Fact(DisplayName = "A relative URI with 0 segments still generates when it carries a query.")]
    public void ZeroSegmentRelativeWithQueryGenerates() {
        Uri value = Any.WithSeed(1).Uri().Relative().WithPathSegments(0).WithQuery().Generate();

        Check.That(value.IsAbsoluteUri).IsFalse();
        Check.That(value.OriginalString).StartsWith("?");
    }

    [Fact(DisplayName = "WithHost rejects a non-canonical shorthand-IPv4 host that would not round-trip.")]
    public void WithHostRejectsNonCanonicalHost() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Uri().Web().WithHost("123"));
        Check.That(error.Message).Contains("canonical");

        Check.ThatCode(() => Any.Uri().Web().WithHost("1.2")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Uri().Web().WithHost("1.2.3.4")).DoesNotThrow();
        Check.ThatCode(() => Any.Uri().Web().WithHost("api.example.com")).DoesNotThrow();
    }

    [Fact(DisplayName = "A seeded URI draw is reproducible: the same seed yields the same value.")]
    public void SeededDrawIsReproducible() {
        Uri first  = Any.WithSeed(4242).Uri().Web().WithQuery().Generate();
        Uri second = Any.WithSeed(4242).Uri().Web().WithQuery().Generate();

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "A URI generator composes like any other: through As into a domain value.")]
    public void ComposesThroughAs() {
        foreach (string scheme in Sample(Seeded(context => context.Uri().Web()).As(uri => uri.Scheme))) {
            Check.That(scheme is "http" or "https").IsTrue();
        }
    }

    private static IEnumerable<T> Sample<T>(IAny<T> generator) {
        for (int i = 0; i < SampleCount; i++) { yield return generator.Generate(); }
    }

}
