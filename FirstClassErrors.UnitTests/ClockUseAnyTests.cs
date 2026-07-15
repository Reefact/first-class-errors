#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(Clock))]
public sealed class ClockUseAnyTests {

    #region Statics members declarations

    private static DomainError AnError() {
        return DomainError.Create(ErrorCode.Create("ANY"), "diagnostic").WithPublicMessage("short");
    }

    #endregion

    [Fact(DisplayName = "Clock.UseAny freezes OccurredAt to a single arbitrary instant for the whole scope.")]
    public void UseAnyFreezesASingleInstant() {
        using (Clock.UseAny()) {
            DomainError first  = AnError();
            DomainError second = AnError();

            Check.That(first.OccurredAt).IsEqualTo(second.OccurredAt);
        }
    }

    [Fact(DisplayName = "Clock.UseAny(seed) picks the same instant for a given seed.")]
    public void UseAnyWithSeedIsReproducible() {
        DateTimeOffset first;
        DateTimeOffset second;

        using (Clock.UseAny(42)) { first = AnError().OccurredAt; }
        using (Clock.UseAny(42)) { second = AnError().OccurredAt; }

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "Clock.UseAny only affects code inside the scope; the real clock resumes after disposal.")]
    public void UseAnyIsScoped() {
        using (Clock.UseAny(1)) { AnError(); }

        DateTimeOffset before = DateTimeOffset.UtcNow;
        DateTimeOffset live   = AnError().OccurredAt;
        DateTimeOffset after  = DateTimeOffset.UtcNow;

        Check.That(live >= before && live <= after).IsTrue();
    }

}
