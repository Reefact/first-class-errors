#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace Dummies.UnitTests;

[TestSubject(typeof(Any))]
public sealed class MaterializationTests {

    #region Statics members declarations

    private static T Materialize<T>(IAny<T> generator) {
        return generator.Generate();
    }

    #endregion

    [Fact(DisplayName = "Generate materializes a valid string value.")]
    public void GenerateMaterializesAString() {
        string value = Any.String().NonEmpty().Generate();

        Check.That(value).IsNotEmpty();
    }

    [Fact(DisplayName = "Generate materializes a valid int value.")]
    public void GenerateMaterializesAnInt() {
        int value = Any.Int32().Positive().Generate();

        Check.That(value).IsStrictlyGreaterThan(0);
    }

    [Fact(DisplayName = "A materialized value flows into a method expecting the generated type.")]
    public void GeneratedValueFlowsIntoACallSite() {
        static int Measure(string text) {
            return text.Length;
        }

        int length = Measure(Any.String().WithLength(9).Generate());

        Check.That(length).IsEqualTo(9);
    }

    [Fact(DisplayName = "Each Generate call draws a fresh value.")]
    public void EachGenerateDrawsAFreshValue() {
        AnyInt32 generator = Any.Int32().Between(0, int.MaxValue);

        HashSet<int> seen = new();
        for (int i = 0; i < 20; i++) {
            seen.Add(generator.Generate());
        }

        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "Generic inference flows through IAny<T>, materializing without any implicit conversion.")]
    public void GenericInferenceMaterializesThroughIAny() {
        string text  = Materialize(Any.String().NonEmpty());
        int    value = Materialize(Any.Int32().Positive());

        Check.That(text).IsNotEmpty();
        Check.That(value).IsStrictlyGreaterThan(0);
    }

}
