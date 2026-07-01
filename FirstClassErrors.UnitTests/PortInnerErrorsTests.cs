#region Usings declarations

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
                                            .Add(new PrimaryPortError(ErrorCode.Unspecified, "transient", Transience.Transient))
                                            .Add(new PrimaryPortError(ErrorCode.Unspecified, "non-transient", Transience.NonTransient));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.NonTransient);
    }

    [Fact(DisplayName = "ComputeTransience is Transient when at least one entry is transient and none is non-transient.")]
    public void ComputeTransienceIsTransientWhenAtLeastOneEntryIsTransientAndNoneIsNonTransient() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add(new PrimaryPortError(ErrorCode.Unspecified, "unknown", Transience.Unknown))
                                            .Add(new PrimaryPortError(ErrorCode.Unspecified, "transient", Transience.Transient));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.Transient);
    }

    [Fact(DisplayName = "ComputeTransience is Unknown when the collection contains only domain errors.")]
    public void ComputeTransienceIsUnknownWhenTheCollectionContainsOnlyDomainErrors() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add(new DomainError(ErrorCode.Unspecified, "first"))
                                            .Add(new DomainError(ErrorCode.Unspecified, "second"));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.Unknown);
    }

    [Fact(DisplayName = "Add ignores null domain and primary port errors.")]
    public void AddIgnoresNullDomainAndPrimaryPortErrors() {
        // Setup
        DomainError      domainError      = new(ErrorCode.Unspecified, "domain");
        PrimaryPortError primaryPortError = new(ErrorCode.Unspecified, "primary", Transience.NonTransient);

        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add((DomainError)null!)
                                            .Add(domainError)
                                            .Add((PrimaryPortError)null!)
                                            .Add(primaryPortError);

        // Exercise
        PrimaryPortError root = new(ErrorCode.Unspecified, "root", innerErrors);

        // Verify
        Check.That(root.InnerErrors).CountIs(2);
    }

    [Fact(DisplayName = "Add returns the same instance to allow fluent chaining.")]
    public void AddReturnsTheSameInstanceToAllowFluentChaining() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new();
        DomainError            domainError = new(ErrorCode.Unspecified, "domain");
        PrimaryPortError       portError   = new(ErrorCode.Unspecified, "primary", Transience.Transient);

        // Exercise & verify
        Check.That(innerErrors.Add(domainError)).IsSameReferenceAs(innerErrors);
        Check.That(innerErrors.Add(portError)).IsSameReferenceAs(innerErrors);
    }

    [Fact(DisplayName = "A primary port error built from inner errors exposes the aggregated transience and the inner errors.")]
    public void APrimaryPortErrorBuiltFromInnerErrorsExposesTheAggregatedTransienceAndTheInnerErrors() {
        // Setup
        DomainError      firstInnerError  = new(ErrorCode.Unspecified, "first");
        PrimaryPortError secondInnerError = new(ErrorCode.Unspecified, "second", Transience.Transient);

        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add(firstInnerError)
                                            .Add(secondInnerError);

        // Exercise
        PrimaryPortError root = new(ErrorCode.Unspecified, "root", innerErrors);

        // Verify
        Check.That(root.Transience).IsEqualTo(innerErrors.ComputeTransience());
        Check.That(root.Transience).IsEqualTo(Transience.Transient);
        Check.That(root.InnerErrors).CountIs(2);
        Check.That(root.InnerErrors[0]).IsSameReferenceAs(firstInnerError);
        Check.That(root.InnerErrors[1]).IsSameReferenceAs(secondInnerError);
    }

    [Fact(DisplayName = "ToString returns the number of aggregated errors.")]
    public void ToStringReturnsTheNumberOfAggregatedErrors() {
        // Setup
        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add(new DomainError(ErrorCode.Unspecified, "first"))
                                            .Add(new PrimaryPortError(ErrorCode.Unspecified, "second", Transience.Transient));

        // Exercise & verify
        Check.That(innerErrors.ToString()).IsEqualTo("2");
    }

}

[TestSubject(typeof(SecondaryPortInnerErrors))]
public sealed class SecondaryPortInnerErrorsTests {

    [Fact(DisplayName = "ComputeTransience is NonTransient when any entry is non-transient, even mixed with a transient one.")]
    public void ComputeTransienceIsNonTransientWhenAnyEntryIsNonTransient() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add(new SecondaryPortError(ErrorCode.Unspecified, "transient", Transience.Transient))
                                              .Add(new SecondaryPortError(ErrorCode.Unspecified, "non-transient", Transience.NonTransient));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.NonTransient);
    }

    [Fact(DisplayName = "ComputeTransience is Transient when at least one entry is transient and none is non-transient.")]
    public void ComputeTransienceIsTransientWhenAtLeastOneEntryIsTransientAndNoneIsNonTransient() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add(new SecondaryPortError(ErrorCode.Unspecified, "unknown", Transience.Unknown))
                                              .Add(new SecondaryPortError(ErrorCode.Unspecified, "transient", Transience.Transient));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.Transient);
    }

    [Fact(DisplayName = "ComputeTransience is Unknown when the collection contains only domain errors.")]
    public void ComputeTransienceIsUnknownWhenTheCollectionContainsOnlyDomainErrors() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add(new DomainError(ErrorCode.Unspecified, "first"))
                                              .Add(new DomainError(ErrorCode.Unspecified, "second"));

        // Exercise
        Transience transience = innerErrors.ComputeTransience();

        // Verify
        Check.That(transience).IsEqualTo(Transience.Unknown);
    }

    [Fact(DisplayName = "Add ignores null domain and secondary port errors.")]
    public void AddIgnoresNullDomainAndSecondaryPortErrors() {
        // Setup
        DomainError        domainError        = new(ErrorCode.Unspecified, "domain");
        SecondaryPortError secondaryPortError = new(ErrorCode.Unspecified, "secondary", Transience.NonTransient);

        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add((DomainError)null!)
                                              .Add(domainError)
                                              .Add((SecondaryPortError)null!)
                                              .Add(secondaryPortError);

        // Exercise
        SecondaryPortError root = new(ErrorCode.Unspecified, "root", innerErrors);

        // Verify
        Check.That(root.InnerErrors).CountIs(2);
    }

    [Fact(DisplayName = "Add returns the same instance to allow fluent chaining.")]
    public void AddReturnsTheSameInstanceToAllowFluentChaining() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new();
        DomainError              domainError = new(ErrorCode.Unspecified, "domain");
        SecondaryPortError       portError   = new(ErrorCode.Unspecified, "secondary", Transience.Transient);

        // Exercise & verify
        Check.That(innerErrors.Add(domainError)).IsSameReferenceAs(innerErrors);
        Check.That(innerErrors.Add(portError)).IsSameReferenceAs(innerErrors);
    }

    [Fact(DisplayName = "ToString returns the number of aggregated errors.")]
    public void ToStringReturnsTheNumberOfAggregatedErrors() {
        // Setup
        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add(new DomainError(ErrorCode.Unspecified, "first"))
                                              .Add(new SecondaryPortError(ErrorCode.Unspecified, "second", Transience.Transient));

        // Exercise & verify
        Check.That(innerErrors.ToString()).IsEqualTo("2");
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
        DomainError        firstInnerError  = new(ErrorCode.Unspecified, "first");
        SecondaryPortError secondInnerError = new(ErrorCode.Unspecified, "second", Transience.NonTransient);

        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors()
                                              .Add(firstInnerError)
                                              .Add(secondInnerError);

        // Exercise
        SecondaryPortError root = new(ErrorCode.Unspecified, "root", innerErrors);

        // Verify
        Check.That(root.Transience).IsEqualTo(innerErrors.ComputeTransience());
        Check.That(root.Transience).IsEqualTo(Transience.NonTransient);
        Check.That(root.InnerErrors).CountIs(2);
        Check.That(root.InnerErrors[0]).IsSameReferenceAs(firstInnerError);
        Check.That(root.InnerErrors[1]).IsSameReferenceAs(secondInnerError);
    }

}
