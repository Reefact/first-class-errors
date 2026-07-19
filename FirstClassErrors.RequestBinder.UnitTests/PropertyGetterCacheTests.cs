#region Usings declarations

using System.Reflection;

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

/// <summary>
///     Pins the contract of the compiled-getter cache (<see cref="PropertyGetters{TDto,TValue}" />): reuse across
///     bindings, isolation across DTO types, the eager mis-declaration guard on every call, and the raw-exception
///     bug channel of a throwing DTO getter.
/// </summary>
public sealed class PropertyGetterCacheTests {

    [Fact(DisplayName = "Binding the same property twice reuses one compiled getter.")]
    public void SamePropertyReusesTheCompiledGetter() {
        PropertyInfo property = typeof(BookingRequest).GetProperty(nameof(BookingRequest.GuestEmail))!;

        Func<BookingRequest, string?> first  = PropertyGetters<BookingRequest, string?>.For(property);
        Func<BookingRequest, string?> second = PropertyGetters<BookingRequest, string?>.For(property);

        Check.That(second).IsSameReferenceAs(first);
        Check.That(first(new BookingRequest("alice@example.org", null, null, null, null, null, null))).IsEqualTo("alice@example.org");
    }

    [Fact(DisplayName = "Two DTO types sharing a property name bind through distinct getters, each reading its own type.")]
    public void SamePropertyNameOnDistinctDtoTypesDoesNotCollide() {
        var bindFirst = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<string> first = bindFirst.PropertiesOf(new FirstDto("from-first")).SimpleProperty(d => d.Name).AsRequired();

        var bindSecond = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<string> second = bindSecond.PropertiesOf(new SecondDto("from-second")).SimpleProperty(d => d.Name).AsRequired();

        Check.That(bindFirst.New(s => s.Get(first)).GetResultOrThrow()).IsEqualTo("from-first");
        Check.That(bindSecond.New(s => s.Get(second)).GetResultOrThrow()).IsEqualTo("from-second");
    }

    [Fact(DisplayName = "A property declared on a base type binds through a derived DTO.")]
    public void InheritedPropertyBindsThroughTheDerivedDto() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new DerivedDto { Inherited = "base-value", Own = "own-value" });

        RequiredField<string> inherited = body.SimpleProperty(d => d.Inherited).AsRequired();
        RequiredField<string> own       = body.SimpleProperty(d => d.Own).AsRequired();

        Outcome<string> outcome = bind.New(s => s.Get(inherited) + "/" + s.Get(own));
        Check.That(outcome.GetResultOrThrow()).IsEqualTo("base-value/own-value");
    }

    [Fact(DisplayName = "A non-nullable value-type property is rejected on every call — the guard is never cached away.")]
    public void NonNullableValueTypePropertyIsRejectedOnEveryCall() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new MisdeclaredDto());

        ArgumentException first = Assert.Throws<ArgumentException>(() => body.SimpleProperty(d => d.Count).AsRequired(PositiveIntFromInt));
        Check.That(first.Message).Contains("non-nullable value type");

        // A second selection must throw again: an invalid property never enters the getter cache.
        ArgumentException second = Assert.Throws<ArgumentException>(() => body.SimpleProperty(d => d.Count).AsRequired(PositiveIntFromInt));
        Check.That(second.Message).Contains("non-nullable value type");
    }

    [Fact(DisplayName = "A DTO getter that throws surfaces its own exception, not a reflection wrapper.")]
    public void ThrowingDtoGetterSurfacesTheRawException() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new ThrowingDto());

        // The binder's bug channel: a throwing property getter is a programming error and propagates as itself —
        // a compiled getter does not wrap it in TargetInvocationException the way PropertyInfo.GetValue did.
        InvalidOperationException raw = Assert.Throws<InvalidOperationException>(() => body.SimpleProperty(d => d.Boom).AsRequired());
        Check.That(raw.Message).IsEqualTo("getter bug");
    }

    [Fact(DisplayName = "Concurrent first binds of one property all succeed with correct values.")]
    public void ConcurrentBindsOfTheSamePropertyAreSafe() {
        string?[] results = new string?[32];

        Parallel.For(0, results.Length, i => {
            var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
            RequiredField<string> value = bind.PropertiesOf(new ConcurrencyDto($"value-{i}")).SimpleProperty(d => d.Value).AsRequired();
            results[i] = bind.New(s => s.Get(value)).GetResultOrThrow();
        });

        for (int i = 0; i < results.Length; i++) {
            Check.That(results[i]).IsEqualTo($"value-{i}");
        }
    }

    [Fact(DisplayName = "A coercion selector between an enum property and its underlying type still binds, both ways.")]
    public void EnumUnderlyingCoercionSelectorStillBinds() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new EnumDto(Channel.Web, 2));

        // The reflection-based reader cast the boxed value to the selector's underlying type, which the CLR
        // accepts across an enum and its underlying integral — the compiled getter must keep accepting it.
        RequiredField<int>     code    = body.SimpleProperty(d => (int?)d.Channel).AsRequired(IntPass);
        RequiredField<Channel> channel = body.SimpleProperty(d => (Channel?)d.ChannelCode).AsRequired(ChannelPass);

        Outcome<string> outcome = bind.New(s => $"{s.Get(code)}/{s.Get(channel)}");
        Check.That(outcome.GetResultOrThrow()).IsEqualTo("1/Phone");
    }

    [Fact(DisplayName = "A nullable value-type property still binds its value, and its absence, through the cached getter.")]
    public void NullableValueTypePropertyBindsThroughTheCache() {
        var present = Bind.Request(BookingEnvelopeError.CommandInvalid);
        RequiredField<int> bound = present.PropertiesOf(new NullableIntDto(41)).SimpleProperty(d => d.Count).AsRequired(NextInt);
        Check.That(present.New(s => s.Get(bound)).GetResultOrThrow()).IsEqualTo(42);

        var missing = Bind.Request(BookingEnvelopeError.CommandInvalid);
        missing.PropertiesOf(new NullableIntDto(null)).SimpleProperty(d => d.Count).AsRequired(NextInt);
        Outcome<int> outcome = missing.New(_ => -1);
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error!.InnerErrors.Single().Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
    }

    #region Helpers & fixtures

    private static Outcome<int> PositiveIntFromInt(int raw) {
        return Outcome<int>.Success(raw);
    }

    private static Outcome<int> NextInt(int raw) {
        return Outcome<int>.Success(raw + 1);
    }

    private static Outcome<int> IntPass(int raw) {
        return Outcome<int>.Success(raw);
    }

    private static Outcome<Channel> ChannelPass(Channel raw) {
        return Outcome<Channel>.Success(raw);
    }

    private sealed record FirstDto(string? Name);

    private sealed record SecondDto(string? Name);

    private class BaseDto {

        public string? Inherited { get; set; }

    }

    private sealed class DerivedDto : BaseDto {

        public string? Own { get; set; }

    }

    private sealed record MisdeclaredDto {

        public int Count { get; init; }

    }

    private sealed class ThrowingDto {

        public string? Boom => throw new InvalidOperationException("getter bug");

    }

    private sealed record ConcurrencyDto(string? Value);

    private sealed record NullableIntDto(int? Count);

    private enum Channel {

        Web   = 1,
        Phone = 2

    }

    private sealed record EnumDto(Channel? Channel, int? ChannelCode);

    #endregion

}
