#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

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

        HashSet<int> seen = [];
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

    [Fact(DisplayName = "Building a generator draws nothing at all, which is why a chain left unmaterialized is silent.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("JustDummies.Usage", "JD006:The generator returned by a constraint is discarded",
                                                     Justification =
                                                         "The discarded generator IS the subject. This pins the behaviour JD006 reports: the arrange line reads like it did something, " +
                                                         "and drew nothing.")]
    public void BuildingAGeneratorDrawsNothing() {
        int draws = 0;

        Any.Int32().As(value => {
            draws++;

            return value;
        });

        Check.That(draws).IsEqualTo(0);
    }

    [Fact(DisplayName = "A generator interpolated into text renders its type name, never a value it could draw.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("JustDummies.Usage", "JD005:A generator is rendered as text instead of the value it would draw",
                                                     Justification =
                                                         "The rendered generator IS the subject. This pins the behaviour JD005 reports, and the deliberate absence of a ToString override " +
                                                         "that would mask it — an override returning a drawn value would make this test red, which is the point.")]
    public void AGeneratorRendersAsItsTypeName() {
        string rendered = $"{Any.Int32()}";

        Check.That(rendered).IsEqualTo(typeof(AnyInt32).ToString());
        Check.That(int.TryParse(rendered, out int _)).IsFalse();
    }

}
