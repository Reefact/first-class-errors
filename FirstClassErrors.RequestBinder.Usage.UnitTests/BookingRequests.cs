#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Boundary;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.UnitTests;

/// <summary>Canonical valid / invalid request fixtures and a shared argument-path reader, used across the tests.</summary>
internal static class BookingRequests {

    #region Statics members declarations

    /// <summary>A request whose every field is valid: it binds into a complete command.</summary>
    internal static BookingRequest Valid() {
        return new BookingRequest(
            GuestEmail: "alice@example.org",
            Reference: "REF-42",
            Currency: null, // omitted: falls back to EUR
            Nights: 3,
            MaxNights: null, // omitted: a real null, not 0
            Stay: new StayDto("2026-08-10", "2026-08-14"),
            Tags: ["vip"],
            RoomNumbers: [101, 102],
            Guests: [new GuestDto("Alice", "alice@example.org"), new GuestDto("Bob", null)]);
    }

    /// <summary>
    ///     A request that fails at every level, so the binder's collect-all behaviour and the full ordered error tree
    ///     can be asserted at once.
    /// </summary>
    internal static BookingRequest InvalidEverywhere() {
        return new BookingRequest(
            GuestEmail: "not-an-email",                       // invalid
            Reference: null,                                  // missing
            Currency: "EURO",                                 // invalid (4 letters)
            Nights: null,                                     // missing (value-type required)
            MaxNights: -1,                                    // invalid (present, not positive)
            Stay: new StayDto("not-a-date", "2026-08-14"),    // CheckIn invalid -> nested stay envelope
            Tags: ["ok", "bad tag"],                          // Tags[1] invalid (whitespace)
            RoomNumbers: [101, 1000],                         // RoomNumbers[1] out of range
            Guests: [new GuestDto("Alice", "alice@example.org"), new GuestDto(null, "bad-email")]); // Guests[1] envelope
    }

    /// <summary>The full argument path recorded in a binding error's context (the "RequestArgument" entry).</summary>
    internal static string? ArgumentPathOf(Error error) {
        error.Context.ToNameDictionary().TryGetValue("RequestArgument", out object? path);

        return path as string;
    }

    #endregion

}
