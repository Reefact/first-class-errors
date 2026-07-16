namespace FirstClassErrors.RequestBinder.UnitTests;

#region Request DTOs

internal sealed record BookingRequest(
    string?                   GuestEmail,
    string?                   Reference,
    string?                   Currency,
    string?                   MaxNights,
    StayDto?                  Stay,
    IReadOnlyList<string?>?   Tags,
    IReadOnlyList<GuestDto?>? Guests);

internal sealed record StayDto(string? CheckIn, string? CheckOut);

internal sealed record GuestDto(string? FirstName, string? Email);

#endregion

#region Bound command & value objects

internal sealed record BookingCommand(
    EmailAddress             GuestEmail,
    string                   Reference,
    Currency                 Currency,
    int?                     MaxNights,
    Stay?                    Stay,
    IReadOnlyList<Tag>       Tags,
    IReadOnlyList<Guest>     Guests);

internal sealed record Stay(BookingDate CheckIn, BookingDate CheckOut);

internal sealed record Guest(string FirstName, EmailAddress? Email);

internal sealed class EmailAddress {

    private EmailAddress(string value) {
        Value = value;
    }

    public string Value { get; }

    public static Outcome<EmailAddress> Parse(string raw) {
        return raw.Contains('@')
                   ? Outcome<EmailAddress>.Success(new EmailAddress(raw))
                   : Outcome<EmailAddress>.Failure(BookingDomainError.EmailInvalid(raw));
    }

}

internal sealed class BookingDate {

    private BookingDate(DateOnly value) {
        Value = value;
    }

    public DateOnly Value { get; }

    public static Outcome<BookingDate> Parse(string raw) {
        return DateOnly.TryParse(raw, out DateOnly parsed)
                   ? Outcome<BookingDate>.Success(new BookingDate(parsed))
                   : Outcome<BookingDate>.Failure(BookingDomainError.DateInvalid(raw));
    }

}

internal sealed class Currency {

    private Currency(string code) {
        Code = code;
    }

    public string Code { get; }

    public static Outcome<Currency> Parse(string raw) {
        return raw.Length == 3
                   ? Outcome<Currency>.Success(new Currency(raw))
                   : Outcome<Currency>.Failure(BookingDomainError.CurrencyInvalid(raw));
    }

}

internal sealed class Tag {

    private Tag(string value) {
        Value = value;
    }

    public string Value { get; }

    public static Outcome<Tag> Parse(string raw) {
        return raw.Length > 0 && !raw.Contains(' ')
                   ? Outcome<Tag>.Success(new Tag(raw))
                   : Outcome<Tag>.Failure(BookingDomainError.TagInvalid(raw));
    }

}

internal static class PositiveInt {

    public static Outcome<int> Parse(string raw) {
        return int.TryParse(raw, out int parsed) && parsed > 0
                   ? Outcome<int>.Success(parsed)
                   : Outcome<int>.Failure(BookingDomainError.NotAPositiveNumber(raw));
    }

}

#endregion

#region Test error factories

/// <summary>The leaf domain errors the test value objects fail with.</summary>
internal static class BookingDomainError {

    internal static DomainError EmailInvalid(string raw) {
        return DomainError.Create(Code.EmailInvalid, $"'{raw}' is not a valid email address.")
                          .WithPublicMessage("The email address is invalid.");
    }

    internal static DomainError DateInvalid(string raw) {
        return DomainError.Create(Code.DateInvalid, $"'{raw}' is not a valid date.")
                          .WithPublicMessage("The date is invalid.");
    }

    internal static DomainError CurrencyInvalid(string raw) {
        return DomainError.Create(Code.CurrencyInvalid, $"'{raw}' is not a valid ISO currency code.")
                          .WithPublicMessage("The currency is invalid.");
    }

    internal static DomainError TagInvalid(string raw) {
        return DomainError.Create(Code.TagInvalid, $"'{raw}' is not a valid tag.")
                          .WithPublicMessage("The tag is invalid.");
    }

    internal static DomainError NotAPositiveNumber(string raw) {
        return DomainError.Create(Code.NotAPositiveNumber, $"'{raw}' is not a strictly positive number.")
                          .WithPublicMessage("The number must be strictly positive.");
    }

    private static class Code {

        public static readonly ErrorCode EmailInvalid       = ErrorCode.Create("TEST_EMAIL_INVALID");
        public static readonly ErrorCode DateInvalid        = ErrorCode.Create("TEST_DATE_INVALID");
        public static readonly ErrorCode CurrencyInvalid    = ErrorCode.Create("TEST_CURRENCY_INVALID");
        public static readonly ErrorCode TagInvalid         = ErrorCode.Create("TEST_TAG_INVALID");
        public static readonly ErrorCode NotAPositiveNumber = ErrorCode.Create("TEST_NOT_A_POSITIVE_NUMBER");

    }

}

/// <summary>The envelope factories the test binders fail with.</summary>
internal static class BookingEnvelopeError {

    internal static PrimaryPortError CommandInvalid(PrimaryPortInnerErrors violations) {
        return PrimaryPortError.Create(Code.CommandInvalid, "The booking command is invalid.", violations)
                               .WithPublicMessage("We could not accept your booking request.");
    }

    internal static PrimaryPortError StayInvalid(PrimaryPortInnerErrors violations) {
        return PrimaryPortError.Create(Code.StayInvalid, "The stay is invalid.", violations)
                               .WithPublicMessage("The stay dates are invalid.");
    }

    internal static PrimaryPortError GuestInvalid(PrimaryPortInnerErrors violations) {
        return PrimaryPortError.Create(Code.GuestInvalid, "The guest information is invalid.", violations)
                               .WithPublicMessage("A guest's information is invalid.");
    }

    private static class Code {

        public static readonly ErrorCode CommandInvalid = ErrorCode.Create("TEST_BOOKING_COMMAND_INVALID");
        public static readonly ErrorCode StayInvalid    = ErrorCode.Create("TEST_STAY_INVALID");
        public static readonly ErrorCode GuestInvalid   = ErrorCode.Create("TEST_GUEST_INVALID");

    }

}

#endregion

#region Shared assertion helpers

internal static class BindingAssertions {

    /// <summary>The full argument path recorded in a binding error's context ("RequestArgument" entry).</summary>
    internal static string? ArgumentPathOf(Error error) {
        error.Context.ToNameDictionary().TryGetValue("RequestArgument", out object? path);

        return path as string;
    }

}

#endregion
