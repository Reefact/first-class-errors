#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Binding;

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.UnitTests;

/// <summary>Tests for the framework-agnostic <see cref="BookingEndpoint" />: the Outcome pipeline it rejoins on success and failure.</summary>
public sealed class BookingEndpointTests {

    [Fact(DisplayName = "A valid request is handled into a success outcome and confirmed by the pipeline.")]
    public void ValidRequestIsConfirmed() {
        BookingEndpoint endpoint = new();

        Check.That(endpoint.Handle(BookingRequests.Valid()).IsSuccess).IsTrue();
        Check.That(endpoint.Place(BookingRequests.Valid())).IsEqualTo("confirmed:REF-42");
    }

    [Fact(DisplayName = "An invalid request is rejected by the pipeline with the envelope code, without throwing.")]
    public void InvalidRequestIsRejected() {
        BookingEndpoint endpoint = new();

        Check.That(endpoint.Place(BookingRequests.InvalidEverywhere())).IsEqualTo("rejected:BOOKING_COMMAND_INVALID");
    }

}
