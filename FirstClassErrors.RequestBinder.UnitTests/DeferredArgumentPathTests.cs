#region Usings declarations

using System.Reflection;

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

/// <summary>
///     Pins the deferred-path contract: an argument's path — and the <see cref="IArgumentNameProvider" /> call behind
///     it — only materializes when a failure is recorded. The observable bound: zero provider calls on an all-valid
///     bind of scalar and list properties; exactly one per bound complex property (its prefix segment); and never
///     more than the historical one-call-per-selected-property upper bound.
/// </summary>
public sealed class DeferredArgumentPathTests {

    [Fact(DisplayName = "An all-valid bind of scalar and list properties never consults the name provider.")]
    public void AllValidScalarsAndListsNeverConsultTheProvider() {
        CountingNameProvider provider = new();
        var bind = Bind.WithOptions(new RequestBinderOptions(provider)).Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest("alice@example.org", "REF-1", "EUR", "3", Stay: null, Tags: new[] { "sea", "spa" }, Guests: null));

        RequiredField<EmailAddress>       email    = body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        RequiredField<string>             refField = body.SimpleProperty(r => r.Reference).AsRequired();
        RequiredField<int>                nights   = body.SimpleProperty(r => r.MaxNights).AsRequired(PositiveInt.Parse);
        RequiredField<IReadOnlyList<Tag>> tags     = body.ListOfSimpleProperties(r => r.Tags).AsRequired(Tag.Parse);

        Outcome<string> outcome = bind.New(s => $"{s.Get(email).Value}/{s.Get(refField)}/{s.Get(nights)}/{s.Get(tags).Count}");

        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(provider.Calls).IsEqualTo(0);
    }

    [Fact(DisplayName = "A failing bind consults the name provider once per failing argument only.")]
    public void FailingBindConsultsTheProviderOncePerFailingArgument() {
        CountingNameProvider provider = new();
        var bind = Bind.WithOptions(new RequestBinderOptions(provider)).Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest(GuestEmail: null, "REF-1", "not-a-currency", "3", Stay: null, Tags: null, Guests: null));

        body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse); // missing  -> 1 call
        body.SimpleProperty(r => r.Reference).AsRequired();                    // valid    -> 0 calls
        body.SimpleProperty(r => r.Currency).AsRequired(Currency.Parse);       // invalid  -> 1 call
        body.SimpleProperty(r => r.MaxNights).AsRequired(PositiveInt.Parse);   // valid    -> 0 calls

        Outcome<string> outcome = bind.New(_ => "never");

        Check.That(outcome.IsFailure).IsTrue();
        Check.That(provider.Calls).IsEqualTo(2);

        // And the paths are the same the eager code produced.
        Check.That(outcome.Error!.InnerErrors.Select(BindingAssertions.ArgumentPathOf)).ContainsExactly("GuestEmail", "Currency");
    }

    [Fact(DisplayName = "A list with one invalid element resolves the list name once; valid elements never build a path.")]
    public void PartiallyInvalidListResolvesTheListNameOnce() {
        CountingNameProvider provider = new();
        var bind = Bind.WithOptions(new RequestBinderOptions(provider)).Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest("alice@example.org", "REF-1", "EUR", "3", Stay: null, Tags: new[] { "sea", "not a tag", "spa" }, Guests: null));

        body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        body.ListOfSimpleProperties(r => r.Tags).AsRequired(Tag.Parse);

        Outcome<string> outcome = bind.New(_ => "never");

        Check.That(outcome.IsFailure).IsTrue();
        Check.That(provider.Calls).IsEqualTo(1); // the list stem, resolved once, shared by the failing element's path
        Check.That(BindingAssertions.ArgumentPathOf(outcome.Error!.InnerErrors.Single())).IsEqualTo("Tags[1]");
    }

    [Fact(DisplayName = "A bound complex property resolves its prefix segment exactly once, valid or not.")]
    public void ComplexPropertyResolvesItsPrefixOnce() {
        CountingNameProvider provider = new();
        var bind = Bind.WithOptions(new RequestBinderOptions(provider)).Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest("alice@example.org", "REF-1", "EUR", "3", new StayDto("2026-08-01", "2026-08-04"), Tags: null, Guests: null));

        RequiredField<Stay> stay = body.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired(BindStay);

        Outcome<Stay> outcome = bind.New(s => s.Get(stay));

        Check.That(outcome.IsSuccess).IsTrue();
        // The nested binding needs its prefix ("Stay") up front — one provider call, exactly what the eager code
        // paid; the nested properties' own paths ("Stay.CheckIn") stayed unbuilt because both dates bound.
        Check.That(provider.Calls).IsEqualTo(1);
    }

    [Fact(DisplayName = "Failing nested properties still carry their full, prefixed paths.")]
    public void FailingNestedPropertiesCarryPrefixedPaths() {
        CountingNameProvider provider = new();
        var bind = Bind.WithOptions(new RequestBinderOptions(provider)).Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest("alice@example.org", "REF-1", "EUR", "3", new StayDto(CheckIn: null, "not-a-date"), Tags: null, Guests: null));

        body.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired(BindStay);

        Outcome<string> outcome = bind.New(_ => "never");

        Check.That(outcome.IsFailure).IsTrue();
        Error stayEnvelope = outcome.Error!.InnerErrors.Single();
        Check.That(stayEnvelope.InnerErrors.Select(BindingAssertions.ArgumentPathOf)).ContainsExactly("Stay.CheckIn", "Stay.CheckOut");

        // One call per distinct segment — "Stay", "CheckIn", "CheckOut" — completing the exactly-once contract on
        // the failing side: the prefix is resolved once and reused (a write-back regression would show 4 calls).
        Check.That(provider.Calls).IsEqualTo(3);
    }

    [Fact(DisplayName = "A complex-list element failing deep inside carries its indexed, prefixed path.")]
    public void FailingComplexListElementCarriesIndexedPrefixedPath() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest("alice@example.org", "REF-1", "EUR", "3", Stay: null, Tags: null,
                                                        Guests: new GuestDto?[] { new("Ada", "ada@example.org"), new(FirstName: null, "no-at-sign") }));

        body.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsRequired(BindGuest);

        Outcome<string> outcome = bind.New(_ => "never");

        Check.That(outcome.IsFailure).IsTrue();
        Error guestEnvelope = outcome.Error!.InnerErrors.Single();
        Check.That(guestEnvelope.InnerErrors.Select(BindingAssertions.ArgumentPathOf)).ContainsExactly("Guests[1].FirstName", "Guests[1].Email");
    }

    #region Helpers & fixtures

    private static Outcome<Stay> BindStay(RequestBinder binder, StayDto dto) {
        var stay = binder.PropertiesOf(dto);

        RequiredField<BookingDate> checkIn  = stay.SimpleProperty(s => s.CheckIn).AsRequired(BookingDate.Parse);
        RequiredField<BookingDate> checkOut = stay.SimpleProperty(s => s.CheckOut).AsRequired(BookingDate.Parse);

        return binder.New(s => new Stay(s.Get(checkIn), s.Get(checkOut)));
    }

    private static Outcome<Guest> BindGuest(RequestBinder binder, GuestDto dto) {
        var guest = binder.PropertiesOf(dto);

        RequiredField<string>                firstName = guest.SimpleProperty(g => g.FirstName).AsRequired();
        OptionalReferenceField<EmailAddress> email     = guest.SimpleProperty(g => g.Email).AsOptionalReference(EmailAddress.Parse);

        return binder.New(s => new Guest(s.Get(firstName), s.Get(email)));
    }

    /// <summary>Counts every name resolution, to observe exactly when the binder consults the provider.</summary>
    private sealed class CountingNameProvider : IArgumentNameProvider {

        private int _calls;

        public int Calls => _calls;

        public string GetArgumentNameFrom(PropertyInfo property) {
            Interlocked.Increment(ref _calls);

            return property.Name;
        }

    }

    #endregion

}
