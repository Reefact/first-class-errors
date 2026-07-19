#region Usings declarations

using FirstClassErrors.Testing;

using NFluent;

#endregion

namespace FirstClassErrors.Testing.UnitTests;

/// <summary>
///     Contract tests for the meaningful-enum factories: they must never yield the <c>Unknown</c> sentinel, which is
///     the whole reason they exist next to a plain <c>Dummies.Any.Enum&lt;T&gt;()</c> draw.
/// </summary>
public sealed class MeaningfulEnumFactoryTests {

    [Fact(DisplayName = "TransienceFactory.Any never returns the Unknown sentinel.")]
    public void TransienceExcludesUnknown() {
        for (int i = 0; i < 200; i++) {
            Check.That(TransienceFactory.Any()).IsNotEqualTo(Transience.Unknown);
        }
    }

    [Fact(DisplayName = "InteractionDirectionFactory.Any never returns the Unknown sentinel.")]
    public void InteractionDirectionExcludesUnknown() {
        for (int i = 0; i < 200; i++) {
            Check.That(InteractionDirectionFactory.Any()).IsNotEqualTo(InteractionDirection.Unknown);
        }
    }

}
