#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Boundary;
using FirstClassErrors.RequestBinder.Usage.Model;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Binding;

/// <summary>
///     A framework-agnostic primary adapter (incoming port): it depends on nothing from ASP.NET, gRPC or any transport
///     — it takes a plain <see cref="BookingRequest" /> DTO and returns an <see cref="Outcome{T}" />. The same code
///     serves an HTTP controller, a message consumer, a CLI or a gRPC handler; each transport only has to deserialize
///     into the DTO and map the returned <see cref="Outcome{T}" /> onto its own response. This type is also the
///     documentation anchor for the endpoint's envelope errors (see <see cref="Errors.PlaceBookingError" />).
/// </summary>
public sealed class BookingEndpoint {

    /// <summary>
    ///     Binds the request into a command. The returned <see cref="Outcome{T}" /> rejoins the caller's own pipeline —
    ///     see <see cref="Place" /> for a transport-shaped example.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
                                                     Justification =
                                                         "BookingEndpoint exists to show what a transport handler looks like, and a handler is an instance a container resolves — not a " +
                                                         "static entry point. Place calls this on the instance, and the method is public, so making it static would both misrepresent " +
                                                         "the shape the sample teaches and change the sample's public surface.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2325:Methods and properties that do not access instance data should be static",
                                                     Justification =
                                                         "BookingEndpoint exists to show what a transport handler looks like, and a handler is an instance a container resolves — not a " +
                                                         "static entry point. Place calls this on the instance, and the method is public, so making it static would both misrepresent " +
                                                         "the shape the sample teaches and change the sample's public surface.")]
    public Outcome<PlaceBookingCommand> Handle(BookingRequest request) {
        return BookingBinder.BindBooking(request);
    }

    /// <summary>
    ///     Binds the request and rejoins the <see cref="Outcome{T}" /> pipeline with <c>Then</c> / <c>Finally</c> to
    ///     produce a single response value — a confirmation string on success, a rejection string carrying the envelope
    ///     code on failure. Mirrors what a transport handler would do to shape its response, without any framework
    ///     dependency.
    /// </summary>
    public string Place(BookingRequest request) {
        return Handle(request)
              .Then(command => Outcome<string>.Success($"confirmed:{command.Reference}"))
              .Finally(confirmation => confirmation, error => $"rejected:{error.Code}");
    }

}
