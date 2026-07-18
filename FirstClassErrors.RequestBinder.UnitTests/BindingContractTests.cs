#region Usings declarations

using System.Linq.Expressions;
using System.Reflection;

using FirstClassErrors.Testing;

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

/// <summary>
///     Pins the edges of the binding contract: the error families a converter may fail with, the wrapping of bare
///     nested failures, the selector plumbing, the guard clauses, the binding scope's read guards, and the binder's
///     own error documentation.
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
            PrimaryPortError.Create(ErrorCode.Create("TEST_PORT_LEVEL_REJECTION"), Any.DiagnosticMessage(), Any.Transience())
                            .WithPublicMessage(Any.ShortMessage())));

        Outcome<string> outcome = bind.New(_ => "never");
        Error invalid = outcome.Error!.InnerErrors.Single();
        Check.That(invalid.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(invalid.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_PORT_LEVEL_REJECTION");
    }

    [Fact(DisplayName = "A converter failing with any other error family is a contract violation, reported by throwing.")]
    public void ConverterFailingWithAnotherFamilyIsABug() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        InfrastructureError foreignFamily =
            InfrastructureError.Create(Any.ErrorCode(), Any.DiagnosticMessage(),
                                       Any.InteractionDirection(), Transience.Unknown)
                               .WithPublicMessage(Any.ShortMessage());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => { bind.SimpleProperty(r => r.GuestEmail).AsRequired(_ => Outcome<EmailAddress>.Failure(foreignFamily)); });
        // The bug-channel exception must locate the bug: which argument, and which unsupported family.
        Check.That(exception.Message).Contains("GuestEmail");
        Check.That(exception.Message).Contains("InfrastructureError");
    }

    #endregion

    #region Bare nested failures are wrapped so the path survives

    [Fact(DisplayName = "A nested binding that fails without building its envelope is wrapped, so the argument path survives.")]
    public void BareNestedFailureIsWrapped() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid)
            .AsRequired(_ => Outcome<Stay>.Failure(BookingDomainError.DateInvalid("raw")));

        Outcome<string> outcome = bind.New(_ => "never");
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

        Outcome<string> outcome = bind.New(_ => "never");
        Error wrapped = outcome.Error!.InnerErrors.Single();
        Check.That(wrapped.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(wrapped)).IsEqualTo("Guests[0]");
    }

    private static PrimaryPortError LeafPortError() {
        return PrimaryPortError.Create(ErrorCode.Create("TEST_NESTED_PORT_LEAF"), Any.DiagnosticMessage(),
                                       Any.Transience())
                               .WithPublicMessage(Any.ShortMessage());
    }

    [Fact(DisplayName = "A required nested binding that fails with a bare PrimaryPortError leaf (not its build-terminal envelope) is wrapped, so the path survives.")]
    public void BareNestedPrimaryPortErrorLeafIsWrapped() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid)
            .AsRequired(_ => Outcome<Stay>.Failure(LeafPortError()));

        Outcome<string> outcome = bind.New(_ => "never");
        Error wrapped = outcome.Error!.InnerErrors.Single();
        // A leaf PrimaryPortError is NOT the nested build-terminal envelope, so it is wrapped under the path — exactly like a
        // DomainError, and unlike the old by-type check that recorded it as-is and dropped the "Stay" context.
        Check.That(wrapped.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(wrapped)).IsEqualTo("Stay");
        Check.That(wrapped.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_NESTED_PORT_LEAF");
    }

    [Fact(DisplayName = "An optional nested binding that fails with a bare PrimaryPortError leaf is wrapped under the path too.")]
    public void BareOptionalNestedPrimaryPortErrorLeafIsWrapped() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid)
            .AsOptional(_ => Outcome<Stay>.Failure(LeafPortError()));

        Outcome<string> outcome = bind.New(_ => "never");
        Error wrapped = outcome.Error!.InnerErrors.Single();
        Check.That(wrapped.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(wrapped)).IsEqualTo("Stay");
        Check.That(wrapped.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_NESTED_PORT_LEAF");
    }

    [Fact(DisplayName = "A list element whose nested binding fails with a bare PrimaryPortError leaf is wrapped under its indexed path.")]
    public void BareNestedElementPrimaryPortErrorLeafIsWrapped() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid)
            .AsRequired(_ => Outcome<Guest>.Failure(LeafPortError()));

        Outcome<string> outcome = bind.New(_ => "never");
        Error wrapped = outcome.Error!.InnerErrors.Single();
        Check.That(wrapped.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(wrapped)).IsEqualTo("Guests[0]");
        Check.That(wrapped.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_NESTED_PORT_LEAF");
    }

    #endregion

    #region Remaining list shapes

    [Fact(DisplayName = "A required list of complex properties that is missing records REQUEST_ARGUMENT_REQUIRED; its envelope is never invoked.")]
    public void RequiredComplexListMissing() {
        var bind = Bind.PropertiesOf(new BookingRequest("a@b.c", null, null, null, null, null, Guests: null))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid)
            .AsRequired(g => g.New(_ => new Guest("never", null)));

        Outcome<string> outcome = bind.New(_ => "never");
        Error required = outcome.Error!.InnerErrors.Single();
        Check.That(required.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(required)).IsEqualTo("Guests");
    }

    [Fact(DisplayName = "A null element that is the first failing element of a simple list is collected in order, like any other.")]
    public void NullElementAsFirstFailureOfASimpleList() {
        var bind = Bind.PropertiesOf(new BookingRequest("a@b.c", null, null, null, null, Tags: ["ok", null, "not ok"], null))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfSimpleProperties(r => r.Tags).AsRequired(Tag.Parse);

        Outcome<string> outcome = bind.New(_ => "never");
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
                RequiredField<string> firstName = g.SimpleProperty(x => x.FirstName).AsRequired();

                return g.New(s => new Guest(s.Get(firstName), null));
            });

        Outcome<string> outcome = bind.New(_ => "never");
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

    [Fact(DisplayName = "The selector resolver unwraps EVERY stacked Convert node, not just the outermost.")]
    public void SelectorResolverUnwrapsNestedConvertNodes() {
        // Widening then boxing a value-type property stacks two Convert nodes: Convert(Convert(c.Count, long), object).
        Expression<Func<Counted, object?>> doublyBoxed = c => (long)c.Count;

        PropertyInfo property = PropertySelectors.GetProperty(doublyBoxed);

        Check.That(property.Name).IsEqualTo("Count");
    }

    [Fact(DisplayName = "A null selector is a programming error and throws.")]
    public void NullSelectorThrows() {
        Check.ThatCode(() => PropertySelectors.GetProperty<BookingRequest, string?>(null!))
             .Throws<ArgumentNullException>();
    }

    private sealed class WithField {

        internal int Field = 42;

    }

    [Fact(DisplayName = "A selector pointing at a field rather than a property is rejected: an argument path needs a property name.")]
    public void FieldSelectorIsRejected() {
        Expression<Func<WithField, int>> fieldSelector = w => w.Field;

        Check.ThatCode(() => PropertySelectors.GetProperty(fieldSelector)).Throws<ArgumentException>();
    }

    #endregion

    #region Non-nullable value-type DTO properties are rejected

    private sealed record ValueTypeRequest(int Count, int? OptionalCount);

    [Fact(DisplayName = "Selecting a non-nullable value-type property is rejected: a missing value would be indistinguishable from its default.")]
    public void NonNullableValueTypePropertyIsRejected() {
        var bind = Bind.PropertiesOf(new ValueTypeRequest(0, null)).FailWith(BookingEnvelopeError.CommandInvalid);

        // A non-nullable int cannot be null, so "missing" (deserialized to 0) is undetectable -> loud programming error.
        ArgumentException exception = Assert.Throws<ArgumentException>(() => { bind.SimpleProperty(r => r.Count); });
        Check.That(exception.Message).Contains("Count");
        Check.That(exception.Message).Contains("nullable");

        // A nullable value type is fine: an absent argument arrives as null and is detectable.
        Check.ThatCode(() => bind.SimpleProperty(r => r.OptionalCount)).DoesNotThrow();
    }

    #endregion

    #region Guard clauses: every entry point rejects null collaborators

    [Fact(DisplayName = "Every entry point rejects a null collaborator with ArgumentNullException.")]
    public void GuardClauses() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        Check.ThatCode(() => Bind.PropertiesOf<BookingRequest>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Bind.PropertiesOf(Request()).FailWith(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Bind.WithOptions(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.New<string>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.Create<string>(null!)).Throws<ArgumentNullException>();

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

    #region The binding scope guards its reads

    [Fact(DisplayName = "The binding scope rejects a null field and a field owned by a different binder — on every Get overload, programming errors both.")]
    public void BindingScopeGuardsItsReads() {
        var binderA = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);
        RequiredField<EmailAddress>          requiredOfA = binderA.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        OptionalReferenceField<EmailAddress> optRefOfA   = binderA.SimpleProperty(r => r.GuestEmail).AsOptionalReference(EmailAddress.Parse);
        OptionalValueField<int>              optValOfA   = binderA.SimpleProperty(r => r.MaxNights).AsOptionalValue(PositiveInt.Parse);

        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        // A null field, on each of the three Get overloads (the assembler runs because `bind` recorded no failure):
        Check.ThatCode(() => bind.New(s => s.Get((RequiredField<EmailAddress>)null!).Value)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.New(s => s.Get((OptionalReferenceField<EmailAddress>)null!) is null)).Throws<ArgumentNullException>();
        Check.ThatCode(() => bind.New(s => s.Get((OptionalValueField<int>)null!).HasValue)).Throws<ArgumentNullException>();

        // A field owned by a different binder is a cross-binder mix-up — rejected loudly on every overload, not read silently.
        Check.ThatCode(() => bind.New(s => s.Get(requiredOfA).Value)).Throws<InvalidOperationException>();
        Check.ThatCode(() => bind.New(s => s.Get(optRefOfA) is null)).Throws<InvalidOperationException>();
        Check.ThatCode(() => bind.New(s => s.Get(optValOfA).HasValue)).Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "A cross-binder field read is rejected with a message naming the different-binder cause, not a blank exception.")]
    public void CrossBinderReadMessageNamesTheCause() {
        var binderA = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);
        RequiredField<EmailAddress> requiredOfA = binderA.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => { bind.New(s => s.Get(requiredOfA).Value); });
        Check.That(exception.Message).Contains("different binder");
    }

    #endregion

    #region The binder-owned errors carry their coded messages

    [Fact(DisplayName = "REQUEST_ARGUMENT_REQUIRED carries its public summary, and its detailed and diagnostic messages name the argument path.")]
    public void RequiredArgumentErrorCarriesItsMessages() {
        PrimaryPortError error = RequestBindingError.ArgumentRequired(RequestBindingError.DefaultArgumentRequiredCode, "GuestEmail");

        Check.That(error.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(error.ShortMessage).IsEqualTo("A required argument is missing.");
        Check.That(error.DetailedMessage).Contains("GuestEmail");
        Check.That(error.DiagnosticMessage).Contains("GuestEmail");
        Check.That(error.DiagnosticMessage).Contains("required");
    }

    [Fact(DisplayName = "REQUEST_ARGUMENT_INVALID carries its public summary, its detailed message names the path, and it wraps the cause.")]
    public void InvalidArgumentErrorCarriesItsMessages() {
        PrimaryPortError error = RequestBindingError.ArgumentInvalid(RequestBindingError.DefaultArgumentInvalidCode, "GuestEmail", BookingDomainError.EmailInvalid("x"));

        Check.That(error.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(error.ShortMessage).IsEqualTo("An argument is invalid.");
        Check.That(error.DetailedMessage).Contains("GuestEmail");
        Check.That(error.DiagnosticMessage).Contains("GuestEmail");
        Check.That(error.DiagnosticMessage).Contains("invalid");
        Check.That(error.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_EMAIL_INVALID");
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

    [Fact(DisplayName = "The REQUEST_ARGUMENT_REQUIRED documentation lists both diagnoses, classified external then internal.")]
    public void RequiredArgumentDocumentationClassifiesItsDiagnoses() {
        MethodInfo documentationMethod = typeof(RequestBindingError)
            .GetMethod("ArgumentRequiredDocumentation", BindingFlags.Static | BindingFlags.NonPublic)!;
        var documentation = (ErrorDocumentation)documentationMethod.Invoke(null, null)!;

        // Both causes are documented, and a client omitting an argument is an EXTERNAL fault while the name-provider
        // mismatch is INTERNAL. A dropped diagnosis or a flipped origin would mislead whoever triages the error.
        Check.That(documentation.Diagnostics.Select(d => d.Origin))
             .ContainsExactly(ErrorOrigin.External, ErrorOrigin.Internal);
    }

    [Fact(DisplayName = "The REQUEST_ARGUMENT_INVALID documentation lists both diagnoses, classified external then internal.")]
    public void InvalidArgumentDocumentationClassifiesItsDiagnoses() {
        MethodInfo documentationMethod = typeof(RequestBindingError)
            .GetMethod("ArgumentInvalidDocumentation", BindingFlags.Static | BindingFlags.NonPublic)!;
        var documentation = (ErrorDocumentation)documentationMethod.Invoke(null, null)!;

        Check.That(documentation.Diagnostics.Select(d => d.Origin))
             .ContainsExactly(ErrorOrigin.External, ErrorOrigin.Internal);
    }

    #endregion

}
