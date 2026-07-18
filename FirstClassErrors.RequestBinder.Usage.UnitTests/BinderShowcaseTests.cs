#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Binding;
using FirstClassErrors.RequestBinder.Usage.Boundary;
using FirstClassErrors.RequestBinder.Usage.Model;

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.UnitTests;

/// <summary>Behavioural tests for the <see cref="BinderShowcase" /> variations (the overloads and options the canonical binder does not use).</summary>
public sealed class BinderShowcaseTests {

    #region Statics members declarations

    private static BookingRequest With(IReadOnlyList<string?>? tags = null,
                                       StayDto? stay = null,
                                       IReadOnlyList<GuestDto?>? guests = null,
                                       string? guestEmail = null) {
        return new BookingRequest(guestEmail, Reference: null, Currency: null, Nights: null, MaxNights: null,
                                  stay, tags, RoomNumbers: null, guests);
    }

    #endregion

    [Fact(DisplayName = "A required simple list records REQUEST_ARGUMENT_REQUIRED when the list is absent.")]
    public void RequiredListAbsentRecords() {
        Outcome<IReadOnlyList<Tag>> outcome = BinderShowcase.BindTagsAsRequired(With(tags: null));

        Check.That(outcome.IsFailure).IsTrue();
        Error inner = outcome.Error!.InnerErrors.Single();
        Check.That(inner.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BookingRequests.ArgumentPathOf(inner)).IsEqualTo("Tags");
    }

    [Fact(DisplayName = "A required simple list that is present but empty binds an empty list.")]
    public void RequiredListPresentButEmptyBindsEmpty() {
        Outcome<IReadOnlyList<Tag>> outcome = BinderShowcase.BindTagsAsRequired(With(tags: []));

        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEmpty();
    }

    [Fact(DisplayName = "Optional complex and optional complex-list properties yield null / empty when absent, recording nothing.")]
    public void OptionalSectionsAbsentBindToNullAndEmpty() {
        Outcome<BinderShowcase.OptionalSections> outcome = BinderShowcase.BindOptionalSections(With(stay: null, guests: null));

        Check.That(outcome.IsSuccess).IsTrue();
        BinderShowcase.OptionalSections sections = outcome.GetResultOrThrow();
        Check.That(sections.Stay).IsNull();
        Check.That(sections.Guests).IsEmpty();
    }

    [Fact(DisplayName = "A custom IArgumentNameProvider reports the failing argument under its snake_case path.")]
    public void SnakeCaseNamesReportSnakeCasePath() {
        Outcome<EmailAddress> outcome = BinderShowcase.BindGuestEmailWithSnakeCaseNames(With(guestEmail: null));

        Error inner = outcome.Error!.InnerErrors.Single();
        Check.That(BookingRequests.ArgumentPathOf(inner)).IsEqualTo("guest_email");
    }

    [Fact(DisplayName = "Custom structural codes replace the defaults, and consumers branch on them symbolically.")]
    public void CustomStructuralCodesAreRaisedAndMatchedSymbolically() {
        Outcome<EmailAddress> outcome = BinderShowcase.BindGuestEmailWithCustomStructuralCodes(With(guestEmail: null));

        Error inner = outcome.Error!.InnerErrors.Single();
        Check.That(inner.Code.ToString()).IsEqualTo("HOTELAPI_ARGUMENT_REQUIRED");
        Check.That(BinderShowcase.IsMissingArgument(inner, BinderShowcase.HotelApiArgumentRequired.Code)).IsTrue();
    }

    [Fact(DisplayName = "Selecting a non-nullable value-type property trips the binder's programming-error guard.")]
    public void NonNullableValueTypeGuardTrips() {
        Check.That(BinderShowcase.NonNullableValueTypeGuardTrips()).IsTrue();
    }

}
