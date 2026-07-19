// ReSharper disable All
#region Usings declarations

using System.Linq.Expressions;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

using FirstClassErrors;

#endregion

namespace FirstClassErrors.RequestBinder.Benchmarks;

/// <summary>
///     Measures the per-request cost of the binder's happy path (the concern of issue #151): time and allocated
///     bytes when every argument is valid, against a hand-written baseline, plus the failure path and two micro
///     benchmarks isolating the irreducible call-site cost of expression-tree selectors.
/// </summary>
/// <remarks>
///     Run with <c>dotnet run -c Release --project FirstClassErrors.RequestBinder.Benchmarks -- --filter '*'</c>.
///     Allocated bytes are exact (MemoryDiagnoser); timings in a container are indicative, comparisons are what matter.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 3, iterationCount: 7)]
public class BinderBenchmarks {

    private BookingRequest    _fullRequest    = null!;
    private FiveScalarsDto    _fiveScalars    = null!;
    private FiveScalarsDto    _fiveScalarsOneMissing = null!;
    private OneScalarDto      _oneScalar      = null!;
    private OneNullableIntDto _oneNullableInt = null!;
    private ListOnlyDto       _listOfTen      = null!;
    private StayDto           _stay           = null!;
    private string            _routeBookingId = null!;

    // Hoisted selectors: the SAME public API, but the Expression<> instances are allocated once — isolates the
    // call-site expression-tree allocation from the binder's internal work.
    private static readonly Expression<Func<FiveScalarsDto, string?>> FirstSelector  = d => d.First;
    private static readonly Expression<Func<FiveScalarsDto, string?>> SecondSelector = d => d.Second;
    private static readonly Expression<Func<FiveScalarsDto, string?>> ThirdSelector  = d => d.Third;
    private static readonly Expression<Func<FiveScalarsDto, string?>> FourthSelector = d => d.Fourth;
    private static readonly Expression<Func<FiveScalarsDto, string?>> FifthSelector  = d => d.Fifth;

    [GlobalSetup]
    public void Setup() {
        _fullRequest = new BookingRequest {
            GuestEmail  = "guest@example.org",
            Reference   = "BK-2026-000123",
            Currency    = "EUR",
            Nights      = 3,
            MaxNights   = 10,
            Stay        = new StayDto { CheckIn = "2026-08-01", CheckOut = "2026-08-04" },
            Tags        = new List<string?> { "beach", "family", "late-checkout" },
            RoomNumbers = new List<int?> { 101, 102, 210 },
            Guests      = new List<GuestDto?> {
                new GuestDto { FirstName = "Ada", Email = "ada@example.org" },
                new GuestDto { FirstName = "Blaise", Email = null },
            },
        };
        _fiveScalars = new FiveScalarsDto {
            First  = "guest@example.org",
            Second = "BK-2026-000123",
            Third  = "EUR",
            Fourth = "beach",
            Fifth  = "2026-08-01",
        };
        _fiveScalarsOneMissing = new FiveScalarsDto {
            First  = "guest@example.org",
            Second = null, // the missing required argument
            Third  = "EUR",
            Fourth = "beach",
            Fifth  = "2026-08-01",
        };
        _oneScalar      = new OneScalarDto { First      = "guest@example.org" };
        _oneNullableInt = new OneNullableIntDto { Count = 3 };
        _listOfTen      = new ListOnlyDto { Items       = new List<string?> { "a1", "b2", "c3", "d4", "e5", "f6", "g7", "h8", "i9", "j10" } };
        _stay           = new StayDto { CheckIn         = "2026-08-01", CheckOut = "2026-08-04" };
        _routeBookingId = "bk_0123456789";
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Full realistic request — the canonical BookingBinder shape (9 bound inputs, nested binder, three lists).
    // -----------------------------------------------------------------------------------------------------------------

    [Benchmark]
    public Outcome<PlaceBookingCommand> FullBooking_HappyPath() {
        RequestBinder                  binder = Bind.Request(BenchmarkErrors.CommandInvalid);
        PropertySource<BookingRequest> body   = binder.PropertiesOf(_fullRequest);

        RequiredField<EmailAddress>              email     = body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        RequiredField<string>                    reference = body.SimpleProperty(r => r.Reference).AsRequired();
        RequiredField<Currency>                  currency  = body.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");
        RequiredField<NightCount>                nights    = body.SimpleProperty(r => r.Nights).AsRequired(NightCount.From);
        OptionalValueField<NightCount>           maxNights = body.SimpleProperty(r => r.MaxNights).AsOptionalValue(NightCount.From);
        RequiredField<Stay>                      stay      = body.ComplexProperty(r => r.Stay).FailWith(BenchmarkErrors.StayInvalid).AsRequired(BindStay);
        RequiredField<IReadOnlyList<Tag>>        tags      = body.ListOfSimpleProperties(r => r.Tags).AsOptional(Tag.Parse);
        RequiredField<IReadOnlyList<RoomNumber>> rooms     = body.ListOfSimpleProperties(r => r.RoomNumbers).AsRequired(RoomNumber.From);
        RequiredField<IReadOnlyList<Guest>>      guests    = body.ListOfComplexProperties(r => r.Guests).FailWith(BenchmarkErrors.GuestInvalid).AsRequired(BindGuest);

        return binder.New(s => new PlaceBookingCommand(
                              s.Get(email),
                              s.Get(reference),
                              s.Get(currency),
                              s.Get(nights),
                              s.Get(maxNights),
                              s.Get(stay),
                              s.Get(tags),
                              s.Get(rooms),
                              s.Get(guests)));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Scalar scaling — 5 properties vs 1 property gives the marginal per-property cost.
    // -----------------------------------------------------------------------------------------------------------------

    [Benchmark]
    public Outcome<FiveScalarsCommand> Scalars5_HappyPath() {
        RequestBinder                  binder = Bind.Request(BenchmarkErrors.CommandInvalid);
        PropertySource<FiveScalarsDto> body   = binder.PropertiesOf(_fiveScalars);

        RequiredField<EmailAddress> first  = body.SimpleProperty(d => d.First).AsRequired(EmailAddress.Parse);
        RequiredField<string>       second = body.SimpleProperty(d => d.Second).AsRequired();
        RequiredField<Currency>     third  = body.SimpleProperty(d => d.Third).AsRequired(Currency.Parse);
        RequiredField<Tag>          fourth = body.SimpleProperty(d => d.Fourth).AsRequired(Tag.Parse);
        RequiredField<BookingDate>  fifth  = body.SimpleProperty(d => d.Fifth).AsRequired(BookingDate.Parse);

        return binder.New(s => new FiveScalarsCommand(s.Get(first), s.Get(second), s.Get(third), s.Get(fourth), s.Get(fifth)));
    }

    [Benchmark]
    public Outcome<FiveScalarsCommand> Scalars5_HoistedSelectors_HappyPath() {
        RequestBinder                  binder = Bind.Request(BenchmarkErrors.CommandInvalid);
        PropertySource<FiveScalarsDto> body   = binder.PropertiesOf(_fiveScalars);

        RequiredField<EmailAddress> first  = body.SimpleProperty(FirstSelector).AsRequired(EmailAddress.Parse);
        RequiredField<string>       second = body.SimpleProperty(SecondSelector).AsRequired();
        RequiredField<Currency>     third  = body.SimpleProperty(ThirdSelector).AsRequired(Currency.Parse);
        RequiredField<Tag>          fourth = body.SimpleProperty(FourthSelector).AsRequired(Tag.Parse);
        RequiredField<BookingDate>  fifth  = body.SimpleProperty(FifthSelector).AsRequired(BookingDate.Parse);

        return binder.New(s => new FiveScalarsCommand(s.Get(first), s.Get(second), s.Get(third), s.Get(fourth), s.Get(fifth)));
    }

    [Benchmark]
    public Outcome<EmailAddress> Scalar1_String_HappyPath() {
        RequestBinder                binder = Bind.Request(BenchmarkErrors.CommandInvalid);
        PropertySource<OneScalarDto> body   = binder.PropertiesOf(_oneScalar);

        RequiredField<EmailAddress> first = body.SimpleProperty(d => d.First).AsRequired(EmailAddress.Parse);

        return binder.New(s => s.Get(first));
    }

    [Benchmark]
    public Outcome<NightCount> Scalar1_NullableInt_HappyPath() {
        RequestBinder                     binder = Bind.Request(BenchmarkErrors.CommandInvalid);
        PropertySource<OneNullableIntDto> body   = binder.PropertiesOf(_oneNullableInt);

        RequiredField<NightCount> count = body.SimpleProperty(d => d.Count).AsRequired(NightCount.From);

        return binder.New(s => s.Get(count));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Lists and nesting.
    // -----------------------------------------------------------------------------------------------------------------

    [Benchmark]
    public Outcome<IReadOnlyList<Tag>> ListOfStrings10_HappyPath() {
        RequestBinder               binder = Bind.Request(BenchmarkErrors.CommandInvalid);
        PropertySource<ListOnlyDto> body   = binder.PropertiesOf(_listOfTen);

        RequiredField<IReadOnlyList<Tag>> items = body.ListOfSimpleProperties(d => d.Items).AsRequired(Tag.Parse);

        return binder.New(s => s.Get(items));
    }

    [Benchmark]
    public Outcome<Stay> Nested_Stay_HappyPath() {
        RequestBinder binder = Bind.Request(BenchmarkErrors.CommandInvalid);

        return BindStay(binder, _stay);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Out-of-DTO argument — no expression tree, no reflection: the floor the DTO-property path could approach.
    // -----------------------------------------------------------------------------------------------------------------

    [Benchmark]
    public Outcome<string> OutOfDtoArgument_HappyPath() {
        RequestBinder binder = Bind.Request(BenchmarkErrors.CommandInvalid);

        RequiredField<string> id = binder.Argument("bookingId").FromRoute(_routeBookingId).AsRequired();

        return binder.New(s => s.Get(id));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Failure path — must not regress when the happy path gets cheaper.
    // -----------------------------------------------------------------------------------------------------------------

    [Benchmark]
    public Outcome<FiveScalarsCommand> Scalars5_OneMissing_FailurePath() {
        RequestBinder                  binder = Bind.Request(BenchmarkErrors.CommandInvalid);
        PropertySource<FiveScalarsDto> body   = binder.PropertiesOf(_fiveScalarsOneMissing);

        RequiredField<EmailAddress> first  = body.SimpleProperty(d => d.First).AsRequired(EmailAddress.Parse);
        RequiredField<string>       second = body.SimpleProperty(d => d.Second).AsRequired();
        RequiredField<Currency>     third  = body.SimpleProperty(d => d.Third).AsRequired(Currency.Parse);
        RequiredField<Tag>          fourth = body.SimpleProperty(d => d.Fourth).AsRequired(Tag.Parse);
        RequiredField<BookingDate>  fifth  = body.SimpleProperty(d => d.Fifth).AsRequired(BookingDate.Parse);

        return binder.New(s => new FiveScalarsCommand(s.Get(first), s.Get(second), s.Get(third), s.Get(fourth), s.Get(fifth)));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Hand-written baseline — the absolute floor: null checks and direct construction, no binder.
    // -----------------------------------------------------------------------------------------------------------------

    [Benchmark(Baseline = true)]
    public Outcome<FiveScalarsCommand> Manual_Scalars5_HappyPath() {
        FiveScalarsDto dto = _fiveScalars;

        if (dto.First is null || dto.Second is null || dto.Third is null || dto.Fourth is null || dto.Fifth is null) {
            return Outcome<FiveScalarsCommand>.Failure(BenchmarkErrors.CommandInvalid(new PrimaryPortInnerErrors()));
        }

        Outcome<EmailAddress> first  = EmailAddress.Parse(dto.First);
        Outcome<Currency>     third  = Currency.Parse(dto.Third);
        Outcome<Tag>          fourth = Tag.Parse(dto.Fourth);
        Outcome<BookingDate>  fifth  = BookingDate.Parse(dto.Fifth);

        if (first.IsFailure || third.IsFailure || fourth.IsFailure || fifth.IsFailure) {
            return Outcome<FiveScalarsCommand>.Failure(BenchmarkErrors.CommandInvalid(new PrimaryPortInnerErrors()));
        }

        return Outcome<FiveScalarsCommand>.Success(new FiveScalarsCommand(
                                                       first.GetResultOrThrow(),
                                                       dto.Second,
                                                       third.GetResultOrThrow(),
                                                       fourth.GetResultOrThrow(),
                                                       fifth.GetResultOrThrow()));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Micro — the irreducible call-site cost of an expression-tree selector vs a compiler-cached delegate.
    // This is the part of the per-property cost that NO internal optimization can remove: it quantifies exactly
    // what an API alternative (delegate + name) would buy, for the v1 API-freeze decision.
    // -----------------------------------------------------------------------------------------------------------------

    [Benchmark]
    public Expression<Func<OneScalarDto, string?>> Micro_ExpressionTreeSelector_Allocation() {
        Expression<Func<OneScalarDto, string?>> selector = d => d.First;

        return selector;
    }

    [Benchmark]
    public Func<OneScalarDto, string?> Micro_CachedDelegateSelector_Allocation() {
        Func<OneScalarDto, string?> selector = static d => d.First;

        return selector;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Nested binders shared by the full-booking scenario.
    // -----------------------------------------------------------------------------------------------------------------

    private static Outcome<Stay> BindStay(RequestBinder binder, StayDto dto) {
        PropertySource<StayDto> stay = binder.PropertiesOf(dto);

        RequiredField<BookingDate> checkIn  = stay.SimpleProperty(s => s.CheckIn).AsRequired(BookingDate.Parse);
        RequiredField<BookingDate> checkOut = stay.SimpleProperty(s => s.CheckOut).AsRequired(BookingDate.Parse);

        return binder.New(s => new Stay(s.Get(checkIn), s.Get(checkOut)));
    }

    private static Outcome<Guest> BindGuest(RequestBinder binder, GuestDto dto) {
        PropertySource<GuestDto> guest = binder.PropertiesOf(dto);

        RequiredField<string>                firstName = guest.SimpleProperty(g => g.FirstName).AsRequired();
        OptionalReferenceField<EmailAddress> email     = guest.SimpleProperty(g => g.Email).AsOptionalReference(EmailAddress.Parse);

        return binder.New(s => new Guest(s.Get(firstName), s.Get(email)));
    }

}
