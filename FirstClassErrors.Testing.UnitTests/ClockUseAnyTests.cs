#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Testing.UnitTests;

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

    [Fact(DisplayName = "Inside Dummies.Any.Reproducibly, Clock.UseAny picks the same instant for a given seed.")]
    public void UseAnyIsReproducibleUnderAReproduciblyScope() {
        DateTimeOffset first  = default;
        DateTimeOffset second = default;

        Dummies.Any.Reproducibly(42, () => {
            using (Clock.UseAny()) { first = AnError().OccurredAt; }
        });
        Dummies.Any.Reproducibly(42, () => {
            using (Clock.UseAny()) { second = AnError().OccurredAt; }
        });

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "Clock.UseAny only affects code inside the scope; the real clock resumes after disposal.")]
    public void UseAnyIsScoped() {
        using (Clock.UseAny()) { AnError(); }

        DateTimeOffset before = DateTimeOffset.UtcNow;
        DateTimeOffset live   = AnError().OccurredAt;
        DateTimeOffset after  = DateTimeOffset.UtcNow;

        Check.That(live >= before && live <= after).IsTrue();
    }

}
