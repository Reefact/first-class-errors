#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Testing.UnitTests;

[TestSubject(typeof(InstanceIds))]
public sealed class InstanceIdsUseAnyTests {

    #region Statics members declarations

    private static DomainError AnError() {
        return DomainError.Create(ErrorCode.Create("ANY"), "diagnostic").WithPublicMessage("short");
    }

    #endregion

    [Fact(DisplayName = "InstanceIds.UseAny assigns a distinct arbitrary id to each error.")]
    public void UseAnyAssignsDistinctIds() {
        using (InstanceIds.UseAny()) {
            Guid first  = AnError().InstanceId;
            Guid second = AnError().InstanceId;

            Check.That(first).IsNotEqualTo(second);
        }
    }

    [Fact(DisplayName = "Inside Dummies.Any.Reproducibly, InstanceIds.UseAny reproduces the same id sequence for a given seed.")]
    public void UseAnyIsReproducibleUnderAReproduciblyScope() {
        Guid firstRunA  = Guid.Empty;
        Guid firstRunB  = Guid.Empty;
        Guid secondRunA = Guid.Empty;
        Guid secondRunB = Guid.Empty;

        Dummies.Any.Reproducibly(7, () => {
            using (InstanceIds.UseAny()) {
                firstRunA = AnError().InstanceId;
                firstRunB = AnError().InstanceId;
            }
        });
        Dummies.Any.Reproducibly(7, () => {
            using (InstanceIds.UseAny()) {
                secondRunA = AnError().InstanceId;
                secondRunB = AnError().InstanceId;
            }
        });

        Check.That(secondRunA).IsEqualTo(firstRunA);
        Check.That(secondRunB).IsEqualTo(firstRunB);
    }

    [Fact(DisplayName = "InstanceIds.UseAny only affects code inside the scope.")]
    public void UseAnyIsScoped() {
        Guid fixedId = new("11111111-1111-1111-1111-111111111111");

        using (InstanceIds.UseAny()) {
            using (InstanceIds.UseFixed(fixedId)) {
                Check.That(AnError().InstanceId).IsEqualTo(fixedId);
            }

            // The inner UseFixed scope is disposed: UseAny is restored, so ids are arbitrary again.
            Check.That(AnError().InstanceId).IsNotEqualTo(fixedId);
        }
    }

}
