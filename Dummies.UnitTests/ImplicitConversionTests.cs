#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace Dummies.UnitTests;

[TestSubject(typeof(Any))]
public sealed class ImplicitConversionTests {

    [Fact(DisplayName = "An AnyString flows implicitly into a string variable.")]
    public void AnyStringConvertsImplicitly() {
        string value = Any.String().NonEmpty();

        Check.That(value).IsNotEmpty();
    }

    [Fact(DisplayName = "An AnyInt32 flows implicitly into an int variable.")]
    public void AnyInt32ConvertsImplicitly() {
        int value = Any.Int32().Positive();

        Check.That(value).IsStrictlyGreaterThan(0);
    }

    [Fact(DisplayName = "An AnyString flows implicitly into a method expecting a string.")]
    public void AnyStringConvertsImplicitlyAtACallSite() {
        static int Measure(string text) {
            return text.Length;
        }

        int length = Measure(Any.String().WithLength(9));

        Check.That(length).IsEqualTo(9);
    }

    [Fact(DisplayName = "Each implicit conversion draws a fresh value.")]
    public void EachConversionDrawsAFreshValue() {
        AnyInt32 generator = Any.Int32().Between(0, int.MaxValue);

        HashSet<int> seen = new();
        for (int i = 0; i < 20; i++) {
            int value = generator;
            seen.Add(value);
        }

        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

}
