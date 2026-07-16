#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

#region Value-type request DTO and value objects

internal sealed record ValueTypeRequest(
    int?                    Quantity,
    bool?                   Flag,
    string?                 Note,
    IReadOnlyList<int?>?    Quantities,
    IReadOnlyList<string?>? Notes,
    int                     Count = 0);

/// <summary>A struct value object built from the underlying <see cref="int" /> — the shape a converter binds against.</summary>
internal readonly struct PositiveQty {

    public int Value { get; private init; }

    public static Outcome<PositiveQty> From(int n) {
        return n > 0
                   ? Outcome<PositiveQty>.Success(new PositiveQty { Value = n })
                   : Outcome<PositiveQty>.Failure(BookingDomainError.NotAPositiveNumber(n.ToString()));
    }

}

/// <summary>A second struct value object over a different underlying value type (<see cref="bool" />).</summary>
internal readonly struct Consent {

    public bool Given { get; private init; }

    public static Outcome<Consent> From(bool given) {
        return Outcome<Consent>.Success(new Consent { Given = given });
    }

}

#endregion

public sealed class ValueTypePropertyBindingTests {

    private static ValueTypeRequest Request(int? quantity = 5, bool? flag = true, string? note = "hello",
                                            IReadOnlyList<int?>? quantities = null, IReadOnlyList<string?>? notes = null) {
        return new ValueTypeRequest(quantity, flag, note, quantities, notes);
    }

    // ── Scalar value-type property ────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "A nullable value-type property binds via a method-group converter over its underlying type.")]
    public void ValueTypeRequiredBindsViaMethodGroup() {
        var bind = Bind.PropertiesOf(Request(quantity: 5)).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<PositiveQty> qty = bind.SimpleProperty(r => r.Quantity).AsRequired(PositiveQty.From);

        Outcome<int> outcome = bind.New(s => s.Get(qty).Value);
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEqualTo(5);
    }

    [Fact(DisplayName = "A required value-type property that is absent records REQUEST_ARGUMENT_REQUIRED with its path.")]
    public void ValueTypeRequiredMissingRecords() {
        var bind = Bind.PropertiesOf(Request(quantity: null)).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.Quantity).AsRequired(PositiveQty.From);

        Outcome<string> outcome = bind.New(_ => "never");
        Check.That(outcome.IsFailure).IsTrue();
        Error required = outcome.Error!.InnerErrors.Single();
        Check.That(required.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(required)).IsEqualTo("Quantity");
    }

    [Fact(DisplayName = "A present-but-invalid value-type property records REQUEST_ARGUMENT_INVALID wrapping the converter error.")]
    public void ValueTypeRequiredInvalidRecords() {
        var bind = Bind.PropertiesOf(Request(quantity: -1)).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.Quantity).AsRequired(PositiveQty.From);

        Outcome<string> outcome = bind.New(_ => "never");
        Error invalid = outcome.Error!.InnerErrors.Single();
        Check.That(invalid.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(invalid.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_NOT_A_POSITIVE_NUMBER");
    }

    [Fact(DisplayName = "AsOptionalValue on a value-type property yields the value when present and a real null when absent.")]
    public void ValueTypeOptionalValue() {
        var present = Bind.PropertiesOf(Request(quantity: 7)).FailWith(BookingEnvelopeError.CommandInvalid);
        OptionalValueField<PositiveQty> some = present.SimpleProperty(r => r.Quantity).AsOptionalValue(PositiveQty.From);
        Check.That(present.New(s => s.Get(some)!.Value.Value).GetResultOrThrow()).IsEqualTo(7);

        var absent = Bind.PropertiesOf(Request(quantity: null)).FailWith(BookingEnvelopeError.CommandInvalid);
        OptionalValueField<PositiveQty> none = absent.SimpleProperty(r => r.Quantity).AsOptionalValue(PositiveQty.From);
        Check.That(absent.New(s => s.Get(none) is null).GetResultOrThrow()).IsTrue();
    }

    [Fact(DisplayName = "AsOptional with a fallback converts the underlying-typed fallback when the value-type property is absent.")]
    public void ValueTypeOptionalWithFallback() {
        var absent = Bind.PropertiesOf(Request(quantity: null)).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<PositiveQty> defaulted = absent.SimpleProperty(r => r.Quantity).AsOptional(PositiveQty.From, 1);

        Check.That(absent.New(s => s.Get(defaulted).Value).GetResultOrThrow()).IsEqualTo(1);
    }

    [Fact(DisplayName = "The underlying-type inference is not int-specific: a bool? property binds the same way.")]
    public void ValueTypeWorksForBool() {
        var bind = Bind.PropertiesOf(Request(flag: true)).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<Consent> consent = bind.SimpleProperty(r => r.Flag).AsRequired(Consent.From);

        Check.That(bind.New(s => s.Get(consent).Given).GetResultOrThrow()).IsTrue();
    }

    // ── List of value-type elements ───────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "A list of nullable value types binds each element via a method-group converter over the underlying type.")]
    public void ValueTypeListRequiredBinds() {
        var bind = Bind.PropertiesOf(Request(quantities: [1, 2, 3])).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<IReadOnlyList<PositiveQty>> qtys = bind.ListOfSimpleProperties(r => r.Quantities).AsRequired(PositiveQty.From);

        Outcome<int> outcome = bind.New(s => s.Get(qtys).Sum(q => q.Value));
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEqualTo(6);
    }

    [Fact(DisplayName = "A null element of a value-type list records REQUEST_ARGUMENT_REQUIRED under its indexed path.")]
    public void ValueTypeListNullElementRecords() {
        var bind = Bind.PropertiesOf(Request(quantities: [1, null, 3])).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfSimpleProperties(r => r.Quantities).AsRequired(PositiveQty.From);

        Error required = bind.New(_ => "never").Error!.InnerErrors.Single();
        Check.That(required.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(required)).IsEqualTo("Quantities[1]");
    }

    [Fact(DisplayName = "An invalid element of a value-type list records REQUEST_ARGUMENT_INVALID under its indexed path.")]
    public void ValueTypeListInvalidElementRecords() {
        var bind = Bind.PropertiesOf(Request(quantities: [1, -2, 3])).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfSimpleProperties(r => r.Quantities).AsRequired(PositiveQty.From);

        Error invalid = bind.New(_ => "never").Error!.InnerErrors.Single();
        Check.That(invalid.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(invalid)).IsEqualTo("Quantities[1]");
    }

    [Fact(DisplayName = "An optional value-type list that is absent yields an empty list and records nothing; a required one records REQUEST_ARGUMENT_REQUIRED.")]
    public void ValueTypeListOptionalVsRequiredWhenAbsent() {
        var optional = Bind.PropertiesOf(Request(quantities: null)).FailWith(BookingEnvelopeError.CommandInvalid);
        RequiredField<IReadOnlyList<PositiveQty>> empty = optional.ListOfSimpleProperties(r => r.Quantities).AsOptional(PositiveQty.From);
        Check.That(optional.New(s => s.Get(empty).Count).GetResultOrThrow()).IsEqualTo(0);

        var required = Bind.PropertiesOf(Request(quantities: null)).FailWith(BookingEnvelopeError.CommandInvalid);
        required.ListOfSimpleProperties(r => r.Quantities).AsRequired(PositiveQty.From);
        Check.That(required.New(_ => "never").Error!.InnerErrors.Single().Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
    }

    // ── Regressions: reference/string paths keep resolving to the original overloads ──────────────────────

    [Fact(DisplayName = "A string property still resolves to the reference overload — no ambiguity introduced by the value-type overload.")]
    public void StringPropertyStillBindsViaReferenceOverload() {
        var bind = Bind.PropertiesOf(Request(note: "a@b.c")).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<EmailAddress> email = bind.SimpleProperty(r => r.Note).AsRequired(EmailAddress.Parse);

        Check.That(bind.New(s => s.Get(email).Value).GetResultOrThrow()).IsEqualTo("a@b.c");
    }

    [Fact(DisplayName = "A list of strings still resolves to the reference list overload.")]
    public void StringListStillBindsViaReferenceOverload() {
        var bind = Bind.PropertiesOf(Request(notes: ["a", "b"])).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<IReadOnlyList<Tag>> tags = bind.ListOfSimpleProperties(r => r.Notes).AsOptional(Tag.Parse);

        Check.That(bind.New(s => s.Get(tags).Count).GetResultOrThrow()).IsEqualTo(2);
    }

    [Fact(DisplayName = "A non-nullable value-type property still throws ArgumentException — the value-type overload does not bypass the guard.")]
    public void NonNullableValueTypeStillThrows() {
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        Check.ThatCode(() => bind.SimpleProperty(r => r.Count)).Throws<ArgumentException>();
    }

}
