#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

/// <summary>
///     Binding of out-of-DTO arguments (route, query, header, …): the <c>Bind.Request</c> entry, the
///     <c>Argument</c> / <c>ArgumentList</c> sources, provenance capture, and the mixing of a DTO's properties with
///     arguments into one collect-all envelope.
/// </summary>
public sealed class ArgumentBindingTests {

    #region A present, valid argument binds

    [Fact]
    public void Argument_present_and_valid_binds_its_value() {
        RequestBinder bind  = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<EmailAddress> email = bind.Argument("email").From("query", "guest@example.com").AsRequired(EmailAddress.Parse);

        Outcome<EmailAddress> outcome = bind.New(s => s.Get(email));

        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow().Value).IsEqualTo("guest@example.com");
    }

    [Fact]
    public void Argument_presence_only_binds_the_raw_value() {
        RequestBinder bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<string> raw  = bind.Argument("reference").From("route", "BK-1").AsRequired();

        Outcome<string> outcome = bind.New(s => s.Get(raw));

        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEqualTo("BK-1");
    }

    #endregion

    #region A missing / invalid argument records path and source

    [Fact]
    public void Missing_required_argument_records_required_with_path_and_source() {
        RequestBinder bind  = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<EmailAddress> email = bind.Argument("email").From("query", (string?)null).AsRequired(EmailAddress.Parse);

        Outcome<EmailAddress> outcome = bind.New(s => s.Get(email));

        Error inner = Single(outcome);
        Check.That(inner.Code).IsEqualTo(RequestBindingError.DefaultArgumentRequiredCode);
        Check.That(PathOf(inner)).IsEqualTo("email");
        Check.That(SourceOf(inner)).IsEqualTo("query");
    }

    [Fact]
    public void Invalid_argument_records_invalid_with_path_source_and_inner_cause() {
        RequestBinder bind  = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<EmailAddress> email = bind.Argument("email").From("header", "not-an-email").AsRequired(EmailAddress.Parse);

        Outcome<EmailAddress> outcome = bind.New(s => s.Get(email));

        Error inner = Single(outcome);
        Check.That(inner.Code).IsEqualTo(RequestBindingError.DefaultArgumentInvalidCode);
        Check.That(PathOf(inner)).IsEqualTo("email");
        Check.That(SourceOf(inner)).IsEqualTo("header");
        Check.That(inner.InnerErrors.Single().Code).IsEqualTo(ErrorCode.Create("TEST_EMAIL_INVALID"));
    }

    [Fact]
    public void Optional_reference_argument_absent_yields_null_and_records_nothing() {
        RequestBinder  bind  = Bind.Request(BookingEnvelopeError.CommandInvalid);
        OptionalReferenceField<EmailAddress> email = bind.Argument("email").From("query", (string?)null).AsOptionalReference(EmailAddress.Parse);

        Outcome<Container> outcome = bind.New(s => new Container(s.Get(email)));

        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow().Email).IsNull();
    }

    #endregion

    #region Provenance: the FromXxx sugar labels the source

    [Theory]
    [InlineData("route")]
    [InlineData("query")]
    [InlineData("header")]
    [InlineData("body")]
    [InlineData("form")]
    public void From_labels_the_source_it_is_given(string source) {
        RequestBinder bind  = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<EmailAddress> email = bind.Argument("email").From(source, (string?)null).AsRequired(EmailAddress.Parse);

        Outcome<EmailAddress> outcome = bind.New(s => s.Get(email));

        Check.That(SourceOf(Single(outcome))).IsEqualTo(source);
    }

    [Fact]
    public void FromRoute_sugar_is_from_route() {
        RequestBinder bind  = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<EmailAddress> email = bind.Argument("email").FromRoute((string?)null).AsRequired(EmailAddress.Parse);

        Check.That(SourceOf(Single(bind.New(s => s.Get(email))))).IsEqualTo("route");
    }

    [Fact]
    public void FromQuery_sugar_is_from_query() {
        RequestBinder bind  = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<EmailAddress> email = bind.Argument("email").FromQuery("not-an-email").AsRequired(EmailAddress.Parse);

        Check.That(SourceOf(Single(bind.New(s => s.Get(email))))).IsEqualTo("query");
    }

    [Fact]
    public void A_dto_property_carries_no_source() {
        RequestBinder bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<EmailAddress> email =
            bind.PropertiesOf(new BookingRequest(null, null, null, null, null, null, null))
                .SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Error inner = Single(bind.New(s => s.Get(email)));
        Check.That(PathOf(inner)).IsEqualTo("GuestEmail");
        Check.That(SourceOf(inner)).IsNull();
    }

    #endregion

    #region Mixing a DTO's properties with arguments into one envelope

    [Fact]
    public void A_dto_property_and_an_argument_collect_into_one_envelope() {
        RequestBinder             bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        PropertySource<BookingRequest> body = bind.PropertiesOf(new BookingRequest("bad-email", null, null, null, null, null, null));

        RequiredField<EmailAddress> email = body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse); // body, invalid
        RequiredField<Tag>          tag   = bind.Argument("tag").FromQuery((string?)null).AsRequired(Tag.Parse);    // query, missing

        Outcome<Mix> outcome = bind.New(s => new Mix(s.Get(email), s.Get(tag)));

        Check.That(outcome.IsFailure).IsTrue();

        IReadOnlyList<Error> inners = outcome.Error!.InnerErrors;
        Check.That(inners).HasSize(2);

        Error emailError = inners.Single(e => PathOf(e) == "GuestEmail");
        Error tagError   = inners.Single(e => PathOf(e) == "tag");
        Check.That(SourceOf(emailError)).IsNull();      // DTO property: implicit provenance
        Check.That(SourceOf(tagError)).IsEqualTo("query"); // argument: explicit provenance
    }

    [Fact]
    public void A_command_binds_from_arguments_only_without_any_dto() {
        RequestBinder bind = Bind.Request(BookingEnvelopeError.CommandInvalid);

        RequiredField<EmailAddress> email = bind.Argument("email").FromRoute("guest@example.com").AsRequired(EmailAddress.Parse);
        RequiredField<Tag>          tag   = bind.Argument("tag").FromQuery("vip").AsRequired(Tag.Parse);

        Outcome<Mix> outcome = bind.New(s => new Mix(s.Get(email), s.Get(tag)));

        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow().Email.Value).IsEqualTo("guest@example.com");
        Check.That(outcome.GetResultOrThrow().Tag.Value).IsEqualTo("vip");
    }

    #endregion

    #region Value-type arguments (the struct overload)

    [Fact]
    public void Value_type_argument_binds_over_its_underlying_type() {
        RequestBinder bind  = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<int> count = bind.Argument("count").From("query", (int?)7).AsRequired(n => Outcome<int>.Success(n));

        Outcome<int> outcome = bind.New(s => s.Get(count));

        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEqualTo(7);
    }

    [Fact]
    public void Missing_value_type_argument_records_required() {
        RequestBinder bind  = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<int> count = bind.Argument("count").From("query", (int?)null).AsRequired(n => Outcome<int>.Success(n));

        Error inner = Single(bind.New(s => s.Get(count)));
        Check.That(inner.Code).IsEqualTo(RequestBindingError.DefaultArgumentRequiredCode);
        Check.That(PathOf(inner)).IsEqualTo("count");
        Check.That(SourceOf(inner)).IsEqualTo("query");
    }

    #endregion

    #region List arguments

    [Fact]
    public void List_argument_binds_each_element_and_records_a_bad_one_under_its_indexed_path() {
        RequestBinder            bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<IReadOnlyList<Tag>> tags =
            bind.ArgumentList("tag").FromQuery(new[] { "ok", "bad tag" }).AsOptional(Tag.Parse);

        Outcome<TagList> outcome = bind.New(s => new TagList(s.Get(tags)));

        Error inner = Single(outcome);
        Check.That(inner.Code).IsEqualTo(RequestBindingError.DefaultArgumentInvalidCode);
        Check.That(PathOf(inner)).IsEqualTo("tag[1]");
        Check.That(SourceOf(inner)).IsEqualTo("query");
    }

    [Fact]
    public void List_argument_absent_optional_yields_empty_and_records_nothing() {
        RequestBinder            bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<IReadOnlyList<Tag>> tags = bind.ArgumentList("tag").FromQuery((IEnumerable<string?>?)null).AsOptional(Tag.Parse);

        Outcome<TagList> outcome = bind.New(s => new TagList(s.Get(tags)));

        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow().Tags).IsEmpty();
    }

    #endregion

    #region Guards

    [Fact]
    public void Argument_with_a_null_name_throws() {
        RequestBinder bind = Bind.Request(BookingEnvelopeError.CommandInvalid);

        Assert.Throws<ArgumentNullException>(() => bind.Argument(null!));
    }

    [Fact]
    public void From_with_a_null_source_throws() {
        RequestBinder bind = Bind.Request(BookingEnvelopeError.CommandInvalid);

        Assert.Throws<ArgumentNullException>(() => bind.Argument("email").From(null!, "x"));
    }

    [Fact]
    public void To_with_a_null_envelope_throws() {
        Assert.Throws<ArgumentNullException>(() => Bind.Request(null!));
    }

    #endregion

    #region Helpers & fixtures

    private static Error Single(Outcome<EmailAddress> outcome) => Single(outcome.Error);
    private static Error Single(Outcome<int> outcome)          => Single(outcome.Error);
    private static Error Single(Outcome<TagList> outcome)      => Single(outcome.Error);

    private static Error Single(Error? envelope) {
        Check.That(envelope).IsNotNull();

        return envelope!.InnerErrors.Single();
    }

    private static string? PathOf(Error error) {
        error.Context.TryGet(RequestBindingError.ArgumentPathKey, out string? path);

        return path;
    }

    private static string? SourceOf(Error error) {
        error.Context.TryGet(RequestBindingError.ArgumentSourceKey, out string? source);

        return source;
    }

    private sealed record Container(EmailAddress? Email);

    private sealed record Mix(EmailAddress Email, Tag Tag);

    private sealed record TagList(IReadOnlyList<Tag> Tags);

    #endregion

}
