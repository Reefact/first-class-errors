#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace Dummies.UnitTests;

[TestSubject(typeof(AnyExtensions))]
public sealed class CompositionTests {

    #region Statics members declarations

    private static T Materialize<T>(IAny<T> generator) {
        return generator.Generate();
    }

    #endregion

    [Fact(DisplayName = "As bridges a constrained string to a value object through its own factory.")]
    public void AsBuildsAStringValueObject() {
        IAny<OrderReference> generator = Any.String()
                                            .StartingWith("ORD-")
                                            .WithLength(12)
                                            .As(OrderReference.Create);

        OrderReference reference = generator.Generate();

        Check.That(reference.Value).StartsWith("ORD-");
        Check.That(reference.Value.Length).IsEqualTo(12);
    }

    [Fact(DisplayName = "As bridges a constrained integer to a value object through its own factory.")]
    public void AsBuildsANumericValueObject() {
        IAny<Percentage> generator = Any.Int32().Between(0, 100).As(Percentage.Create);

        Percentage percentage = generator.Generate();

        Check.That(percentage.Value).IsGreaterOrEqualThan(0);
        Check.That(percentage.Value).IsLessOrEqualThan(100);
    }

    [Fact(DisplayName = "A factory rejecting the generated value surfaces as AnyGenerationException naming the value and the seed.")]
    public void AsWrapsFactoryFailures() {
        IAny<Percentage> tooWeaklyConstrained = Any.Int32().Between(200, 300).As(Percentage.Create);

        AnyGenerationException? caught = null;
        Assert.Throws<AnyGenerationException>(
            () => Any.Reproducibly(9876, () => {
                try {
                    tooWeaklyConstrained.Generate();
                } catch (AnyGenerationException exception) {
                    caught = exception;

                    throw;
                }
            }, _ => { }));

        Check.That(caught).IsNotNull();
        Check.That(caught!.Seed).IsEqualTo(9876);
        Check.That(caught.Message).Contains("As(...)");
        Check.That(caught.Message).Contains("9876");
        Check.That(caught.InnerException).IsInstanceOf<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Combine assembles two constrained parts through a constructor lambda.")]
    public void CombineAssemblesTwoParts() {
        IAny<Customer> generator = Any.Combine(
            Any.String().NonEmpty().WithMaxLength(50),
            Any.String().StartingWith("ORD-").WithLength(12),
            (name, reference) => new Customer(name, OrderReference.Create(reference)));

        Customer customer = generator.Generate();

        Check.That(customer.Name).IsNotEmpty();
        Check.That(customer.LastOrder.Value).StartsWith("ORD-");
    }

    [Fact(DisplayName = "Combine assembles three parts through a constructor lambda.")]
    public void CombineAssemblesThreeParts() {
        IAny<string> generator = Any.Combine(
            Any.String().WithLength(2).UpperCase(),
            Any.Int32().Between(10, 99),
            Any.String().WithLength(2).LowerCase(),
            (head, middle, tail) => $"{head}{middle}{tail}");

        string value = generator.Generate();

        Check.That(value.Length).IsEqualTo(6);
    }

    [Fact(DisplayName = "A composer failure surfaces as AnyGenerationException naming the generated values.")]
    public void CombineWrapsComposerFailures() {
        IAny<string> generator = Any.Combine<int, int, string>(
            Any.Int32().Between(1, 3),
            Any.Int32().Between(4, 6),
            (first, second) => throw new InvalidOperationException($"rejected {first}/{second}"));

        AnyGenerationException caught = Assert.Throws<AnyGenerationException>(() => generator.Generate());

        Check.That(caught.Message).Contains("Combine(...)");
        Check.That(caught.InnerException).IsInstanceOf<InvalidOperationException>();
    }

    [Fact(DisplayName = "Generic inference flows through IAny<T> without relying on implicit conversions.")]
    public void GenericInferenceFlowsThroughIAny() {
        string text  = Materialize(Any.String().NonEmpty().WithMaxLength(50));
        int    value = Materialize(Any.Int32().Positive());

        Check.That(text).IsNotEmpty();
        Check.That(value).IsStrictlyGreaterThan(0);
    }

    [Fact(DisplayName = "As and Combine validate their arguments.")]
    public void CompositionValidatesArguments() {
        Check.ThatCode(() => Any.String().As<string, OrderReference>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => AnyExtensions.As(null!, (string value) => value)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.Combine(null!, Any.Int32(), (int a, int b) => a + b)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.Combine(Any.Int32(), Any.Int32(), (Func<int, int, int>)null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A derived generator draws fresh values on every generation.")]
    public void DerivedGeneratorsDrawFreshValues() {
        IAny<Percentage> generator = Any.Int32().Between(0, 100).As(Percentage.Create);

        HashSet<int> seen = new();
        for (int i = 0; i < 100; i++) {
            seen.Add(generator.Generate().Value);
        }

        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

}
