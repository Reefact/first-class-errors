#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

public sealed class ListBindingTests {

    private static Outcome<Guest> BindGuest(RequestBinder<GuestDto> guest) {
        RequiredField<string>                firstName = guest.SimpleProperty(g => g.FirstName).AsRequired();
        OptionalReferenceField<EmailAddress> email     = guest.SimpleProperty(g => g.Email).AsOptionalReference(EmailAddress.Parse);

        return guest.New(s => new Guest(s.Get(firstName), s.Get(email)));
    }

    private static BookingRequest RequestWith(IReadOnlyList<string?>? tags = null, IReadOnlyList<GuestDto?>? guests = null) {
        return new BookingRequest("alice@example.org", "REF-1", "EUR", null, Stay: null, tags, guests);
    }

    [Fact(DisplayName = "A required list of simple properties binds every element, in order.")]
    public void RequiredSimpleListBinds() {
        var bind = Bind.PropertiesOf(RequestWith(tags: ["vip", "late-checkout"])).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<IReadOnlyList<Tag>> tags = bind.ListOfSimpleProperties(r => r.Tags).AsRequired(Tag.Parse);

        Outcome<IReadOnlyList<Tag>> outcome = bind.New(s => s.Get(tags));
        Check.That(outcome.GetResultOrThrow().Select(t => t.Value)).ContainsExactly("vip", "late-checkout");
    }

    [Fact(DisplayName = "A required list that is missing records REQUEST_ARGUMENT_REQUIRED.")]
    public void RequiredSimpleListMissing() {
        var bind = Bind.PropertiesOf(RequestWith(tags: null)).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfSimpleProperties(r => r.Tags).AsRequired(Tag.Parse);

        Outcome<string> outcome = bind.New(_ => "never");
        Error required = outcome.Error!.InnerErrors.Single();
        Check.That(required.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(required)).IsEqualTo("Tags");
    }

    [Fact(DisplayName = "A required list that is present but empty is valid: it binds an empty list and records nothing — required constrains presence, not element count.")]
    public void RequiredSimpleListPresentButEmptyBindsEmpty() {
        var bind = Bind.PropertiesOf(RequestWith(tags: [])).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<IReadOnlyList<Tag>> tags = bind.ListOfSimpleProperties(r => r.Tags).AsRequired(Tag.Parse);

        Outcome<IReadOnlyList<Tag>> outcome = bind.New(s => s.Get(tags));
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEmpty();
    }

    [Fact(DisplayName = "Every failing element of a list is collected, each under its indexed path — one bad element never hides the others.")]
    public void EveryFailingElementIsCollected() {
        var bind = Bind.PropertiesOf(RequestWith(tags: ["ok", "not ok", null, "fine"])).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfSimpleProperties(r => r.Tags).AsRequired(Tag.Parse);

        Outcome<string> outcome = bind.New(_ => "never");
        Check.That(outcome.Error!.InnerErrors).HasSize(2);
        Check.That(outcome.Error!.InnerErrors.Select(BindingAssertions.ArgumentPathOf))
             .ContainsExactly("Tags[1]", "Tags[2]");
        Check.That(outcome.Error!.InnerErrors[0].Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(outcome.Error!.InnerErrors[1].Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
    }

    [Fact(DisplayName = "An optional list that is absent binds an empty list — never null — and records nothing.")]
    public void OptionalListAbsentBindsEmpty() {
        var bind = Bind.PropertiesOf(RequestWith(tags: null)).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<IReadOnlyList<Tag>> tags = bind.ListOfSimpleProperties(r => r.Tags).AsOptional(Tag.Parse);

        Outcome<IReadOnlyList<Tag>> outcome = bind.New(s => s.Get(tags));
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEmpty();
    }

    [Fact(DisplayName = "A required list of complex properties binds every element through its own nested binder.")]
    public void RequiredComplexListBinds() {
        var bind = Bind.PropertiesOf(RequestWith(guests: [new GuestDto("Alice", "alice@example.org"), new GuestDto("Bob", null)]))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<IReadOnlyList<Guest>> guests =
            bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsRequired(BindGuest);

        Outcome<IReadOnlyList<Guest>> outcome = bind.New(s => s.Get(guests));
        Check.That(outcome.GetResultOrThrow().Select(g => g.FirstName)).ContainsExactly("Alice", "Bob");
        Check.That(outcome.GetResultOrThrow()[1].Email).IsNull();
    }

    [Fact(DisplayName = "A required complex list that is present but empty is valid: it binds an empty list and records nothing.")]
    public void RequiredComplexListPresentButEmptyBindsEmpty() {
        var bind = Bind.PropertiesOf(RequestWith(guests: [])).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<IReadOnlyList<Guest>> guests =
            bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsRequired(BindGuest);

        Outcome<IReadOnlyList<Guest>> outcome = bind.New(s => s.Get(guests));
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEmpty();
    }

    [Fact(DisplayName = "Each failing element of a complex list records its own envelope, whose inner paths carry the indexed prefix.")]
    public void FailingComplexElementsRecordTheirEnvelopes() {
        var bind = Bind.PropertiesOf(RequestWith(guests: [new GuestDto("Alice", null), new GuestDto(null, "nope"), new GuestDto(null, null)]))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsRequired(BindGuest);

        Outcome<string> outcome = bind.New(_ => "never");
        Check.That(outcome.Error!.InnerErrors).HasSize(2);

        Error second = outcome.Error!.InnerErrors[0];
        Check.That(second.Code.ToString()).IsEqualTo("TEST_GUEST_INVALID");
        Check.That(second.InnerErrors.Select(BindingAssertions.ArgumentPathOf))
             .ContainsExactly("Guests[1].FirstName", "Guests[1].Email");

        Error third = outcome.Error!.InnerErrors[1];
        Check.That(third.InnerErrors.Select(BindingAssertions.ArgumentPathOf)).ContainsExactly("Guests[2].FirstName");
    }

    [Fact(DisplayName = "A null element of a complex list records REQUEST_ARGUMENT_REQUIRED under its indexed path.")]
    public void NullComplexElementIsRequired() {
        var bind = Bind.PropertiesOf(RequestWith(guests: [new GuestDto("Alice", null), null]))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsRequired(BindGuest);

        Outcome<string> outcome = bind.New(_ => "never");
        Error required = outcome.Error!.InnerErrors.Single();
        Check.That(required.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(required)).IsEqualTo("Guests[1]");
    }

    [Fact(DisplayName = "A null element does not hide the failing elements that follow it — each is still collected under its own indexed path.")]
    public void NullComplexElementDoesNotHideLaterFailures() {
        var bind = Bind.PropertiesOf(RequestWith(guests: [null, new GuestDto(null, "nope")]))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsRequired(BindGuest);

        Outcome<string> outcome = bind.New(_ => "never");

        // Both the null element AND the invalid element after it must be collected — the null must not short-circuit.
        Check.That(outcome.Error!.InnerErrors).HasSize(2);

        Error nullElement = outcome.Error!.InnerErrors[0];
        Check.That(nullElement.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(nullElement)).IsEqualTo("Guests[0]");

        Error secondEnvelope = outcome.Error!.InnerErrors[1];
        Check.That(secondEnvelope.Code.ToString()).IsEqualTo("TEST_GUEST_INVALID");
        Check.That(secondEnvelope.InnerErrors.Select(BindingAssertions.ArgumentPathOf))
             .ContainsExactly("Guests[1].FirstName", "Guests[1].Email");
    }

    [Fact(DisplayName = "An optional complex list that is absent binds an empty list and records nothing.")]
    public void OptionalComplexListAbsentBindsEmpty() {
        var bind = Bind.PropertiesOf(RequestWith(guests: null)).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<IReadOnlyList<Guest>> guests =
            bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsOptional(BindGuest);

        Outcome<IReadOnlyList<Guest>> outcome = bind.New(s => s.Get(guests));
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEmpty();
    }

    [Fact(DisplayName = "A custom argument-name provider renames the inner paths of complex-list elements: the element binder inherits the parent options.")]
    public void CustomNameProviderRenamesComplexListElementPaths() {
        var bind = Bind.WithOptions(new RequestBinderOptions(new SnakeCaseNameProvider()))
                       .PropertiesOf(new BookingRequest("a@b.c", "REF-1", null, null, null, null, Guests: [new GuestDto(null, "nope")]))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsRequired(BindGuest);

        Outcome<string> outcome = bind.New(_ => "never");
        Error guestEnvelope = outcome.Error!.InnerErrors.Single(e => e.Code.ToString() == "TEST_GUEST_INVALID");
        // FirstName (required, absent) and Email ("nope", invalid) are both collected; their names must be
        // snake_cased by the INHERITED provider, not the default PascalCase — proving the element binder inherits options.
        Check.That(guestEnvelope.InnerErrors.Select(BindingAssertions.ArgumentPathOf))
             .ContainsExactly("guests[0].first_name", "guests[0].email");
    }

    private sealed class SnakeCaseNameProvider : IArgumentNameProvider {

        public string GetArgumentNameFrom(System.Reflection.PropertyInfo property) {
            return string.Concat(property.Name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
        }

    }

}
