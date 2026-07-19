#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(PrimaryPortInnerErrors))]
public sealed class PortInnerErrorsTests {

    [Fact(DisplayName = "ComputeTransience of an empty collection is Unknown.")]
    public void ComputeTransienceOfAnEmptyCollectionIsUnknown() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new();

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.Unknown);
    }

    [Fact(DisplayName = "ComputeTransience is NonTransient when any entry is non-transient, even mixed with a transient one.")]
    public void ComputeTransienceIsNonTransientWhenAnyEntryIsNonTransient() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add(ErrorFactory.Primary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), Transience.Transient))
                                            .Add(ErrorFactory.Primary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), Transience.NonTransient));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.NonTransient);
    }

    [Fact(DisplayName = "ComputeTransience is Transient when at least one entry is transient and none is non-transient.")]
    public void ComputeTransienceIsTransientWhenAtLeastOneEntryIsTransientAndNoneIsNonTransient() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add(ErrorFactory.Primary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), Transience.Unknown))
                                            .Add(ErrorFactory.Primary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), Transience.Transient));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.Transient);
    }

    [Fact(DisplayName = "ComputeTransience is Unknown when the collection contains only domain errors.")]
    public void ComputeTransienceIsUnknownWhenTheCollectionContainsOnlyDomainErrors() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add(ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any()))
                                            .Add(ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any()));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.Unknown);
    }

    [Fact(DisplayName = "Add ignores null domain and primary port errors.")]
    public void AddIgnoresNullDomainAndPrimaryPortErrors() {
        // Setup
        DomainError      domainError      = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        PrimaryPortError primaryPortError = ErrorFactory.Primary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), TransienceFactory.Any());

        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add((DomainError)null!)
                                            .Add(domainError)
                                            .Add((PrimaryPortError)null!)
                                            .Add(primaryPortError);

        // Exercise
        PrimaryPortError root = ErrorFactory.Primary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), innerErrors);

        // Verify
        Check.That(root.InnerErrors).CountIs(2);
    }

    [Fact(DisplayName = "Add returns the same instance to allow fluent chaining.")]
    public void AddReturnsTheSameInstanceToAllowFluentChaining() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new();
        DomainError            domainError = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        PrimaryPortError       portError   = ErrorFactory.Primary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), TransienceFactory.Any());

        // Exercise & verify
        Check.That(innerErrors.Add(domainError)).IsSameReferenceAs(innerErrors);
        Check.That(innerErrors.Add(portError)).IsSameReferenceAs(innerErrors);
    }

    [Fact(DisplayName = "A primary port error built from inner errors exposes the aggregated transience and the inner errors.")]
    public void APrimaryPortErrorBuiltFromInnerErrorsExposesTheAggregatedTransienceAndTheInnerErrors() {
        // Setup
        DomainError      firstInnerError  = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        PrimaryPortError secondInnerError = ErrorFactory.Primary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), Transience.Transient);

        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add(firstInnerError)
                                            .Add(secondInnerError);

        // Exercise
        PrimaryPortError root = ErrorFactory.Primary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), innerErrors);

        // Verify
        Check.That(root.Transience).IsEqualTo(innerErrors.ComputeTransience());
        Check.That(root.Transience).IsEqualTo(Transience.Transient);
        Check.That(root.InnerErrors).CountIs(2);
        Check.That(root.InnerErrors[0]).IsSameReferenceAs(firstInnerError);
        Check.That(root.InnerErrors[1]).IsSameReferenceAs(secondInnerError);
    }

    [Fact(DisplayName = "ToString renders the pluralized count of aggregated errors.")]
    public void ToStringRendersThePluralizedCountOfAggregatedErrors() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add(ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any()))
                                            .Add(ErrorFactory.Primary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), TransienceFactory.Any()));

        // Exercise & verify
        Check.That(innerErrors.ToString()).IsEqualTo("2 inner errors");
    }

    [Fact(DisplayName = "ToString renders the singular form for a single aggregated error.")]
    public void ToStringRendersTheSingularFormForASingleAggregatedError() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
            .Add(ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any()));

        // Exercise & verify
        Check.That(innerErrors.ToString()).IsEqualTo("1 inner error");
    }

    [Fact(DisplayName = "ToString renders the plural form for an empty collection.")]
    public void ToStringRendersThePluralFormForAnEmptyCollection() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new();

        // Exercise & verify
        Check.That(innerErrors.ToString()).IsEqualTo("0 inner errors");
    }

}

[TestSubject(typeof(SecondaryPortInnerErrors))]
public sealed class SecondaryPortInnerErrorsTests {

    [Fact(DisplayName = "ComputeTransience is NonTransient when any entry is non-transient, even mixed with a transient one.")]
    public void ComputeTransienceIsNonTransientWhenAnyEntryIsNonTransient() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add(ErrorFactory.Secondary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), Transience.Transient))
                                              .Add(ErrorFactory.Secondary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), Transience.NonTransient));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.NonTransient);
    }

    [Fact(DisplayName = "ComputeTransience is Transient when at least one entry is transient and none is non-transient.")]
    public void ComputeTransienceIsTransientWhenAtLeastOneEntryIsTransientAndNoneIsNonTransient() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add(ErrorFactory.Secondary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), Transience.Unknown))
                                              .Add(ErrorFactory.Secondary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), Transience.Transient));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.Transient);
    }

    [Fact(DisplayName = "ComputeTransience is Unknown when the collection contains only domain errors.")]
    public void ComputeTransienceIsUnknownWhenTheCollectionContainsOnlyDomainErrors() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add(ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any()))
                                              .Add(ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any()));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.Unknown);
    }

    [Fact(DisplayName = "Add ignores null domain and secondary port errors.")]
    public void AddIgnoresNullDomainAndSecondaryPortErrors() {
        // Setup
        DomainError        domainError        = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        SecondaryPortError secondaryPortError = ErrorFactory.Secondary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), TransienceFactory.Any());

        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add((DomainError)null!)
                                              .Add(domainError)
                                              .Add((SecondaryPortError)null!)
                                              .Add(secondaryPortError);

        // Exercise
        SecondaryPortError root = ErrorFactory.Secondary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), innerErrors);

        // Verify
        Check.That(root.InnerErrors).CountIs(2);
    }

    [Fact(DisplayName = "Add returns the same instance to allow fluent chaining.")]
    public void AddReturnsTheSameInstanceToAllowFluentChaining() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new();
        DomainError              domainError = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        SecondaryPortError       portError   = ErrorFactory.Secondary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), TransienceFactory.Any());

        // Exercise & verify
        Check.That(innerErrors.Add(domainError)).IsSameReferenceAs(innerErrors);
        Check.That(innerErrors.Add(portError)).IsSameReferenceAs(innerErrors);
    }

    [Fact(DisplayName = "ToString renders the pluralized count of aggregated errors.")]
    public void ToStringRendersThePluralizedCountOfAggregatedErrors() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add(ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any()))
                                              .Add(ErrorFactory.Secondary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), TransienceFactory.Any()));

        // Exercise & verify
        Check.That(innerErrors.ToString()).IsEqualTo("2 inner errors");
    }

    [Fact(DisplayName = "ToString renders the singular form for a single aggregated error.")]
    public void ToStringRendersTheSingularFormForASingleAggregatedError() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
            .Add(ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any()));

        // Exercise & verify
        Check.That(innerErrors.ToString()).IsEqualTo("1 inner error");
    }

    [Fact(DisplayName = "ToString renders the plural form for an empty collection.")]
    public void ToStringRendersThePluralFormForAnEmptyCollection() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new();

        // Exercise & verify
        Check.That(innerErrors.ToString()).IsEqualTo("0 inner errors");
    }

    [Fact(DisplayName = "ComputeTransience of an empty collection is Unknown.")]
    public void ComputeTransienceOfAnEmptyCollectionIsUnknown() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new();

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.Unknown);
    }

    [Fact(DisplayName = "A secondary port error built from inner errors exposes the aggregated transience and the inner errors.")]
    public void ASecondaryPortErrorBuiltFromInnerErrorsExposesTheAggregatedTransienceAndTheInnerErrors() {
        // Setup
        DomainError        firstInnerError  = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        SecondaryPortError secondInnerError = ErrorFactory.Secondary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), Transience.NonTransient);

        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add(firstInnerError)
                                              .Add(secondInnerError);

        // Exercise
        SecondaryPortError root = ErrorFactory.Secondary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), innerErrors);

        // Verify
        Check.That(root.Transience).IsEqualTo(innerErrors.ComputeTransience());
        Check.That(root.Transience).IsEqualTo(Transience.NonTransient);
        Check.That(root.InnerErrors).CountIs(2);
        Check.That(root.InnerErrors[0]).IsSameReferenceAs(firstInnerError);
        Check.That(root.InnerErrors[1]).IsSameReferenceAs(secondInnerError);
    }

}
