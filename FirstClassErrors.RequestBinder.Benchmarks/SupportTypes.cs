// ReSharper disable All
#region Usings declarations

using FirstClassErrors;

#endregion

namespace FirstClassErrors.RequestBinder.Benchmarks;

// ---------------------------------------------------------------------------------------------------------------------
// Request DTOs — the shapes an adapter deserializes an incoming request into. Modeled on the canonical BookingBinder
// example so the measured path is the documented, realistic one.
// ---------------------------------------------------------------------------------------------------------------------

public sealed class BookingRequest {

    public string?         GuestEmail  { get; set; }
    public string?         Reference   { get; set; }
    public string?         Currency    { get; set; }
    public int?            Nights      { get; set; }
    public int?            MaxNights   { get; set; }
    public StayDto?        Stay        { get; set; }
    public List<string?>?  Tags        { get; set; }
    public List<int?>?     RoomNumbers { get; set; }
    public List<GuestDto?>? Guests     { get; set; }

}

public sealed class StayDto {

    public string? CheckIn  { get; set; }
    public string? CheckOut { get; set; }

}

public sealed class GuestDto {

    public string? FirstName { get; set; }
    public string? Email     { get; set; }

}

public sealed class FiveScalarsDto {

    public string? First  { get; set; }
    public string? Second { get; set; }
    public string? Third  { get; set; }
    public string? Fourth { get; set; }
    public string? Fifth  { get; set; }

}

public sealed class OneScalarDto {

    public string? First { get; set; }

}

public sealed class OneNullableIntDto {

    public int? Count { get; set; }

}

public sealed class ListOnlyDto {

    public List<string?>? Items { get; set; }

}

// ---------------------------------------------------------------------------------------------------------------------
// Value objects — deliberately minimal converters so the benchmark measures BINDER overhead, not domain-rule cost.
// Every converter succeeds on the happy-path inputs used by the benchmarks. Error codes are cached in static fields
// so the failure path does not pay ErrorCode.Create either.
// ---------------------------------------------------------------------------------------------------------------------

public sealed class EmailAddress {

    private static readonly ErrorCode InvalidCode = ErrorCode.Create("EMAIL_ADDRESS_INVALID");

    private EmailAddress(string value) {
        Value = value;
    }

    public string Value { get; }

    public static Outcome<EmailAddress> Parse(string raw) {
        if (string.IsNullOrEmpty(raw) || raw.IndexOf('@') < 0) {
            return Outcome<EmailAddress>.Failure(
                DomainError.Create(InvalidCode, "The value is not a valid email address.")
                           .WithPublicMessage("The email address is invalid."));
        }

        return Outcome<EmailAddress>.Success(new EmailAddress(raw));
    }

}

public sealed class Currency {

    private static readonly ErrorCode InvalidCode = ErrorCode.Create("CURRENCY_INVALID");

    private Currency(string value) {
        Value = value;
    }

    public string Value { get; }

    public static Outcome<Currency> Parse(string raw) {
        if (string.IsNullOrEmpty(raw) || raw.Length != 3) {
            return Outcome<Currency>.Failure(
                DomainError.Create(InvalidCode, "The value is not a valid ISO currency code.")
                           .WithPublicMessage("The currency is invalid."));
        }

        return Outcome<Currency>.Success(new Currency(raw));
    }

}

// A readonly struct, mirroring the Usage project's NightCount: consumer value objects may be structs — the
// class-only rule applies to the library's own invariant-carrying types.
public readonly struct NightCount {

    private static readonly ErrorCode InvalidCode = ErrorCode.Create("NIGHT_COUNT_INVALID");

    private NightCount(int value) {
        Value = value;
    }

    public int Value { get; }

    public static Outcome<NightCount> From(int raw) {
        if (raw <= 0) {
            return Outcome<NightCount>.Failure(
                DomainError.Create(InvalidCode, "A night count must be strictly positive.")
                           .WithPublicMessage("The night count is invalid."));
        }

        return Outcome<NightCount>.Success(new NightCount(raw));
    }

}

public sealed class BookingDate {

    private static readonly ErrorCode InvalidCode = ErrorCode.Create("BOOKING_DATE_INVALID");

    private BookingDate(string value) {
        Value = value;
    }

    public string Value { get; }

    public static Outcome<BookingDate> Parse(string raw) {
        if (string.IsNullOrEmpty(raw)) {
            return Outcome<BookingDate>.Failure(
                DomainError.Create(InvalidCode, "The value is not a valid booking date.")
                           .WithPublicMessage("The date is invalid."));
        }

        return Outcome<BookingDate>.Success(new BookingDate(raw));
    }

}

public sealed class Tag {

    private static readonly ErrorCode InvalidCode = ErrorCode.Create("TAG_INVALID");

    private Tag(string value) {
        Value = value;
    }

    public string Value { get; }

    public static Outcome<Tag> Parse(string raw) {
        if (string.IsNullOrEmpty(raw)) {
            return Outcome<Tag>.Failure(
                DomainError.Create(InvalidCode, "A tag cannot be empty.")
                           .WithPublicMessage("The tag is invalid."));
        }

        return Outcome<Tag>.Success(new Tag(raw));
    }

}

public sealed class RoomNumber {

    private static readonly ErrorCode InvalidCode = ErrorCode.Create("ROOM_NUMBER_INVALID");

    private RoomNumber(int value) {
        Value = value;
    }

    public int Value { get; }

    public static Outcome<RoomNumber> From(int raw) {
        if (raw <= 0) {
            return Outcome<RoomNumber>.Failure(
                DomainError.Create(InvalidCode, "A room number must be strictly positive.")
                           .WithPublicMessage("The room number is invalid."));
        }

        return Outcome<RoomNumber>.Success(new RoomNumber(raw));
    }

}

// ---------------------------------------------------------------------------------------------------------------------
// Commands — the bound results.
// ---------------------------------------------------------------------------------------------------------------------

public sealed class Stay {

    public Stay(BookingDate checkIn, BookingDate checkOut) {
        CheckIn  = checkIn;
        CheckOut = checkOut;
    }

    public BookingDate CheckIn  { get; }
    public BookingDate CheckOut { get; }

}

public sealed class Guest {

    public Guest(string firstName, EmailAddress? email) {
        FirstName = firstName;
        Email     = email;
    }

    public string        FirstName { get; }
    public EmailAddress? Email     { get; }

}

public sealed class PlaceBookingCommand {

    public PlaceBookingCommand(EmailAddress               guestEmail,
                               string                     reference,
                               Currency                   currency,
                               NightCount                 nights,
                               NightCount?                maxNights,
                               Stay                       stay,
                               IReadOnlyList<Tag>         tags,
                               IReadOnlyList<RoomNumber>  rooms,
                               IReadOnlyList<Guest>       guests) {
        GuestEmail = guestEmail;
        Reference  = reference;
        Currency   = currency;
        Nights     = nights;
        MaxNights  = maxNights;
        Stay       = stay;
        Tags       = tags;
        Rooms      = rooms;
        Guests     = guests;
    }

    public EmailAddress              GuestEmail { get; }
    public string                    Reference  { get; }
    public Currency                  Currency   { get; }
    public NightCount                Nights     { get; }
    public NightCount?               MaxNights  { get; }
    public Stay                      Stay       { get; }
    public IReadOnlyList<Tag>        Tags       { get; }
    public IReadOnlyList<RoomNumber> Rooms      { get; }
    public IReadOnlyList<Guest>      Guests     { get; }

}

public sealed class FiveScalarsCommand {

    public FiveScalarsCommand(EmailAddress first, string second, Currency third, Tag fourth, BookingDate fifth) {
        First  = first;
        Second = second;
        Third  = third;
        Fourth = fourth;
        Fifth  = fifth;
    }

    public EmailAddress First  { get; }
    public string       Second { get; }
    public Currency     Third  { get; }
    public Tag          Fourth { get; }
    public BookingDate  Fifth  { get; }

}

// ---------------------------------------------------------------------------------------------------------------------
// Envelope factories — cached codes; the envelope itself is only ever built on the failure path.
// ---------------------------------------------------------------------------------------------------------------------

public static class BenchmarkErrors {

    private static readonly ErrorCode CommandInvalidCode = ErrorCode.Create("BOOKING_COMMAND_INVALID");
    private static readonly ErrorCode StayInvalidCode    = ErrorCode.Create("BOOKING_STAY_INVALID");
    private static readonly ErrorCode GuestInvalidCode   = ErrorCode.Create("BOOKING_GUEST_INVALID");

    public static PrimaryPortError CommandInvalid(PrimaryPortInnerErrors violations) {
        return PrimaryPortError.Create(
                                   CommandInvalidCode,
                                   "The booking command is invalid: one or more request arguments failed to bind.",
                                   violations)
                               .WithPublicMessage("The booking request is invalid.", "One or more fields of the booking request are invalid.");
    }

    public static PrimaryPortError StayInvalid(PrimaryPortInnerErrors violations) {
        return PrimaryPortError.Create(
                                   StayInvalidCode,
                                   "The stay is invalid: one or more of its dates failed to bind.",
                                   violations)
                               .WithPublicMessage("The stay is invalid.", "One or more dates of the stay are invalid.");
    }

    public static PrimaryPortError GuestInvalid(PrimaryPortInnerErrors violations) {
        return PrimaryPortError.Create(
                                   GuestInvalidCode,
                                   "A guest is invalid: one or more of its fields failed to bind.",
                                   violations)
                               .WithPublicMessage("A guest is invalid.", "One or more fields of a guest are invalid.");
    }

}
