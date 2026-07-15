#region Usings declarations

using System.Linq.Expressions;
using System.Reflection;

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

/// <summary>
///     Pins the edges of the binding contract: the error families a converter may fail with, the wrapping of bare
///     nested failures, the selector plumbing, the guard clauses, the handles' inspection seam, and the binder's own
///     error documentation.
/// </summary>
public sealed class BindingContractTests {

    private static BookingRequest Request(string? email = "alice@example.org") {
        return new BookingRequest(email, "REF-1", null, null,
                                  new StayDto("2026-08-10", "2026-08-14"),
                                  Tags: null,
                                  Guests: [new GuestDto("Alice", null)]);
    }

    #region Error families a converter may fail with

    [Fact(DisplayName = "A converter may fail with a PrimaryPortError: it is wrapped like a domain failure.")]
    public void ConverterMayFailWithAPrimaryPortError() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.GuestEmail).AsRequired(_ => Outcome<EmailAddress>.Failure(
            PrimaryPortError.Create(ErrorCode.Create("TEST_PORT_LEVEL_REJECTION"), "Rejected at the port.", Transience.NonTransient)
                            .WithPublicMessage("Rejected.")));

        Outcome<string> outcome = bind.Build(() => "never");
        Error invalid = outcome.Error!.InnerErrors.Single();
        Check.That(invalid.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(invalid.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_PORT_LEVEL_REJECTION");
    }

    [Fact(DisplayName = "A converter failing with any other error family is a contract violation, reported by throwing.")]
    public void ConverterFailingWithAnotherFamilyIsABug() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        InfrastructureError foreignFamily =
            InfrastructureError.Create(ErrorCode.Create("TEST_FOREIGN_FAMILY"), "A family the port envelope cannot hold.",
                                       InteractionDirection.Outgoing, Transience.Unknown)
                               .WithPublicMessage("Foreign.");

        Check.ThatCode(() => bind.SimpleProperty(r => r.GuestEmail)
                                 .AsRequired(_ => Outcome<EmailAddress>.Failure(foreignFamily)))
             .Throws<InvalidOperationException>();
    }

    #endregion

    #region Bare nested failures are wrapped so the path survives

    [Fact(DisplayName = "A nested binding that fails without building its envelope is wrapped, so the argument path survives.")]
    public void BareNestedFailureIsWrapped() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid)
            .AsRequired(_ => Outcome<Stay>.Failure(BookingDomainError.DateInvalid("raw")));

        Outcome<string> outcome = bind.Build(() => "never");
        Error wrapped = outcome.Error!.InnerErrors.Single();
        Check.That(wrapped.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(wrapped)).IsEqualTo("Stay");
        Check.That(wrapped.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_DATE_INVALID");
    }

    [Fact(DisplayName = "A list element whose nested binding fails without building its envelope is wrapped under its indexed path.")]
    public void BareNestedElementFailureIsWrapped() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid)
            .AsRequired(_ => Outcome<Guest>.Failure(BookingDomainError.EmailInvalid("raw")));

        Outcome<string> outcome = bind.Build(() => "never");
        Error wrapped = outcome.Error!.InnerErrors.Single();
        Check.That(wrapped.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(wrapped)).IsEqualTo("Guests[0]");
    }

    #endregion

    #region Remaining list shapes

    [Fact(DisplayName = "A required list of complex properties that is missing records REQUEST_ARGUMENT_REQUIRED; its envelope is never invoked.")]
    public void RequiredComplexListMissing() {
        var bind = Bind.PropertiesOf(new BookingRequest("a@b.c", null, null, null, null, null, Guests: null))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid)
            .AsRequired(g => g.Build(() => new Guest("never", null)));

        Outcome<string> outcome = bind.Build(() => "never");
        Error required = outcome.Error!.InnerErrors.Single();
        Check.That(required.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(required)).IsEqualTo("Guests");
    }

    [Fact(DisplayName = "A null element that is the FIRST failure of a simple list seeds the handle's failure like any other.")]
    public void NullElementAsFirstFailureOfASimpleList() {
        var bind = Bind.PropertiesOf(new BookingRequest("a@b.c", null, null, null, null, Tags: ["ok", null, "not ok"], null))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfSimpleProperties(r => r.Tags).AsRequired(Tag.Parse);

        Outcome<string> outcome = bind.Build(() => "never");
        Check.That(outcome.Error!.InnerErrors.Select(e => e.Code.ToString()))
             .ContainsExactly("REQUEST_ARGUMENT_REQUIRED", "REQUEST_ARGUMENT_INVALID");
        Check.That(outcome.Error!.InnerErrors.Select(BindingAssertions.ArgumentPathOf))
             .ContainsExactly("Tags[1]", "Tags[2]");
    }

    [Fact(DisplayName = "An optional list of complex properties that is present still collects its failing elements.")]
    public void OptionalComplexListPresentCollectsFailures() {
        var bind = Bind.PropertiesOf(new BookingRequest("a@b.c", null, null, null, null, null, Guests: [new GuestDto(null, null)]))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid)
            .AsOptional(g => {
                RequiredProperty<string> firstName = g.SimpleProperty(x => x.FirstName).AsRequired();

                return g.Build(() => new Guest(firstName.Value, null));
            });

        Outcome<string> outcome = bind.Build(() => "never");
        Check.That(outcome.Error!.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_GUEST_INVALID");
    }

    #endregion

    #region Selector plumbing

    private sealed record Counted(int Count);

    [Fact(DisplayName = "The selector resolver unwraps the Convert node a boxing selector produces.")]
    public void SelectorResolverUnwrapsConvertNodes() {
        // Boxing a value-type property forces the compiler to emit a Convert node around the member access.
        Expression<Func<Counted, object?>> boxing = c => c.Count;

        PropertyInfo property = PropertySelectors.GetProperty(boxing);

        Check.That(property.Name).IsEqualTo("Count");
    }

    [Fact(DisplayName = "A null selector is a programming error and throws.")]
    public void NullSelectorThrows() {
        Check.ThatCode(() => PropertySelectors.GetProperty<BookingRequest, string?>(null!))
             .Throws<ArgumentNullException>();
    }

    #endregion

    #region Guard clauses: every entry point rejects null collaborators

    [Fact(DisplayName = "Every entry point rejects a null collaborator with ArgumentNullException.")]
    public void GuardClauses() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        Check.ThatCode(() => Bind.PropertiesOf<BookingRequest>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Bind.PropertiesOf(Request()).FailWith(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.WithOptions(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.Build<string>(null!)).Throws<ArgumentNullException>();

        Check.ThatCode(() => bind.SimpleProperty(r => r.GuestEmail).AsRequired<EmailAddress>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.SimpleProperty(r => r.GuestEmail).AsOptional<EmailAddress>(null!, "x")).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.SimpleProperty(r => r.GuestEmail).AsOptionalReference<EmailAddress>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.SimpleProperty(r => r.MaxNights).AsOptionalValue<int>(null!)).Throws<ArgumentNullException>();

        Check.ThatCode(() => bind.ComplexProperty(r => r.Stay).FailWith(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired<Stay>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsOptional<Stay>(null!)).Throws<ArgumentNullException>();

        Check.ThatCode(() => bind.ListOfSimpleProperties(r => r.Tags).AsRequired<Tag>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.ListOfSimpleProperties(r => r.Tags).AsOptional<Tag>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.ListOfComplexProperties(r => r.Guests).FailWith(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsRequired<Guest>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsOptional<Guest>(null!)).Throws<ArgumentNullException>();

        Check.ThatCode(() => new RequestBinderOptions(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => RequestBinderOptions.Default.ArgumentNameProvider.GetArgumentNameFrom(null!)).Throws<ArgumentNullException>();
    }

    #endregion

    #region Handle inspection seam

    [Fact(DisplayName = "A handle exposes its recorded failure for inspection — and null when the binding succeeded or the argument was absent.")]
    public void HandlesExposeTheirFailure() {
        var bind = Bind.PropertiesOf(new BookingRequest(null, "REF-1", null, null, null, null, null))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredProperty<EmailAddress>          failed  = bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        RequiredProperty<string>                bound   = bind.SimpleProperty(r => r.Reference).AsRequired();
        OptionalReferenceProperty<EmailAddress> absent  = bind.SimpleProperty(r => r.GuestEmail).AsOptionalReference(EmailAddress.Parse);
        OptionalValueProperty<int>              nothing = bind.SimpleProperty(r => r.MaxNights).AsOptionalValue(PositiveInt.Parse);

        Check.That(failed.Failure!.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(bound.Failure).IsNull();
        Check.That(absent.Failure).IsNull();
        Check.That(nothing.Failure).IsNull();
    }

    #endregion

    #region The binder's own errors are documented

    [Fact(DisplayName = "Every binder-owned error factory is documented: its documentation method yields a titled documentation with a live example.")]
    public void BinderOwnedErrorsAreDocumented() {
        MethodInfo[] documented = typeof(RequestBindingError)
                                  .GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                                  .Where(m => m.GetCustomAttribute<DocumentedByAttribute>() is not null)
                                  .ToArray();

        Check.That(documented).HasSize(2);

        foreach (MethodInfo factory in documented) {
            string documentationMethodName = factory.GetCustomAttribute<DocumentedByAttribute>()!.MethodName;
            MethodInfo documentationMethod = typeof(RequestBindingError)
                .GetMethod(documentationMethodName, BindingFlags.Static | BindingFlags.NonPublic)!;

            var documentation = (ErrorDocumentation)documentationMethod.Invoke(null, null)!;

            Check.That(documentation.Title).IsNotEmpty();
            Check.That(documentation.Examples).Not.IsEmpty();
        }
    }

    #endregion

}
