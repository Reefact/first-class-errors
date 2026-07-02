#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(ErrorDocumentationBuilder))]
public sealed class ErrorDocumentationBuilderTests : IDisposable {

    #region Constructors declarations

    public ErrorDocumentationBuilderTests() {
        ErrorContextKey.ResetForTests();
        ErrorCode.ResetForTests();
    }

    #endregion

    [SuppressMessage("Usage", "CA1816", Justification = "IDisposable is used as an xUnit teardown hook. The class has no finalizer and does not own unmanaged resources.")]
    public void Dispose() {
        ErrorContextKey.ResetForTests();
        ErrorCode.ResetForTests();
    }

    [Fact(DisplayName = "An error documentation title cannot be null.")]
    public void AnErrorDocumentationTitleCannotBeNull() {
        // Setup
        ErrorDocumentationBuilder builder = new();

        // Exercise & verify
        Check.ThatCode(() => builder.WithTitle(null!))
             .Throws<ArgumentNullException>();
    }

    [Theory(DisplayName = "An error documentation title cannot be empty or whitespace.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("         ")]
    public void AnErrorDocumentationTitleCannotBeEmptyOrWhitespace(string title) {
        // Setup
        ErrorDocumentationBuilder builder = new();

        // Exercise & verify
        Check.ThatCode(() => builder.WithTitle(title))
             .Throws<ArgumentException>();
    }

    [Fact(DisplayName = "An error documentation title is trimmed.")]
    public void AnErrorDocumentationTitleIsTrimmed() {
        // Setup
        ErrorDocumentationBuilder builder             = new();
        ErrorCode                 anyErrorCode        = ErrorCodeFactory.CreateAny();
        string                    anyErrorLongMessage = StringFactory.AnyErrorLongMessage();

        // Exercise
        ErrorDocumentation doc = builder
                                .WithTitle("  My title  ")
                                .WithDescription(StringFactory.AnyDescription())
                                .WithoutRule()
                                .WithoutDiagnostic()
                                .WithExamples(() => new DomainError(anyErrorCode, anyErrorLongMessage));

        // Verify
        Check.That(doc.Title).IsEqualTo("My title");
    }

    [Fact(DisplayName = "An error documentation description cannot be null.")]
    public void AnErrorDocumentationDescriptionCannotBeNull() {
        // Setup
        ErrorDocumentationBuilder builder = new();

        // Exercise & verify
        Check.ThatCode(() => builder.WithDescription(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "An error documentation description is trimmed.")]
    public void AnErrorDocumentationDescriptionIsTrimmed() {
        // Setup
        ErrorDocumentationBuilder builder             = new();
        ErrorCode                 anyErrorCode        = ErrorCodeFactory.CreateAny();
        string                    anyTitle            = StringFactory.AnyTitle();
        string                    anyErrorLongMessage = StringFactory.AnyErrorLongMessage();

        // Exercise
        ErrorDocumentation doc = builder
                                .WithTitle(anyTitle)
                                .WithDescription("  Explanation  ")
                                .WithoutRule()
                                .WithoutDiagnostic()
                                .WithExamples(() => new DomainError(anyErrorCode, anyErrorLongMessage));

        // Verify
        Check.That(doc.Explanation).IsEqualTo("Explanation");
    }

    [Fact(DisplayName = "An error documentation rule cannot be null.")]
    public void AnErrorDocumentationRuleCannotBeNull() {
        // Setup
        ErrorDocumentationBuilder builder = new();

        // Exercise & verify
        Check.ThatCode(() => builder.WithRule(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "An error documentation rule is trimmed.")]
    public void AnErrorDocumentationRuleIsTrimmed() {
        // Setup
        ErrorDocumentationBuilder builder             = new();
        ErrorCode                 anyErrorCode        = ErrorCodeFactory.CreateAny();
        string                    anyTitle            = StringFactory.AnyTitle();
        string                    anyExplanation      = StringFactory.AnyExplanation();
        string                    anyErrorLongMessage = StringFactory.AnyErrorLongMessage();

        // Exercise
        ErrorDocumentation doc = builder
                                .WithTitle(anyTitle)
                                .WithDescription(anyExplanation)
                                .WithRule("  Rule  ")
                                .WithoutDiagnostic()
                                .WithExamples(() => new DomainError(anyErrorCode, anyErrorLongMessage));

        // Verify
        Check.That(doc.BusinessRule).IsEqualTo("Rule");
    }

    [Fact(DisplayName = "An error documentation can be built without a rule neither diagnostic.")]
    public void AnErrorDocumentationCanBeBuiltWithoutARule() {
        // Setup
        ErrorDocumentationBuilder builder             = new();
        ErrorCode                 anyErrorCode        = ErrorCodeFactory.CreateAny();
        string                    anyTitle            = StringFactory.AnyTitle();
        string                    anyExplanation      = StringFactory.AnyExplanation();
        string                    anyErrorLongMessage = StringFactory.AnyErrorLongMessage();

        // Exercise
        ErrorDocumentation doc = builder
                                .WithTitle(anyTitle)
                                .WithDescription(anyExplanation)
                                .WithoutRule()
                                .WithoutDiagnostic()
                                .WithExamples(() => new DomainError(anyErrorCode, anyErrorLongMessage));

        // Verify
        Check.That(doc.BusinessRule).IsNull();
        Check.That(doc.Diagnostics).IsNotNull();
        Check.That(doc.Diagnostics).CountIs(0);
    }

    [Fact(DisplayName = "An error documentation builder cannot accept a null examples collection.")]
    public void AnErrorDocumentationBuilderCannotAcceptANullExamplesCollection() {
        // Setup
        ErrorDocumentationBuilder builder = new();

        // Exercise & verify
        Check.ThatCode(() => builder.WithExamples<DomainError>(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "An error documentation builder requires at least one example.")]
    public void AnErrorDocumentationBuilderRequiresAtLeastOneExample() {
        // Setup
        ErrorDocumentationBuilder builder = new();

        // Exercise & verify
        Check.ThatCode(() => builder.WithExamples<DomainError>())
             .Throws<ErrorDocumentationException>()
             .WithMessage("At least one example factory must be provided to build documentation examples.");
    }

    [Fact(DisplayName = "An error documentation builder rejects a null example factory.")]
    public void AnErrorDocumentationBuilderRejectsANullExampleFactory() {
        // Setup
        ErrorDocumentationBuilder builder = new();

        // Exercise & verify
        Check.ThatCode(() => builder.WithExamples<DomainError>(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "An error documentation builder rejects a null example factory among the provided factories.")]
    public void AnErrorDocumentationBuilderRejectsANullExampleFactoryAmongTheProvidedFactories() {
        // Setup
        ErrorDocumentationBuilder builder = new();

        ErrorCode         code  = ErrorCode.Create("ANY_CODE");
        Func<DomainError> valid = () => new DomainError(code, "boom");

        // Exercise & verify
        Check.ThatCode(() => builder.WithExamples(valid, null!))
             .Throws<ErrorDocumentationException>()
             .WithMessage("Example factory at index 1 is null. All factories must be valid delegates.");
    }

    [Fact(DisplayName = "An error documentation builder rejects an example factory that throws.")]
    public void AnErrorDocumentationBuilderRejectsAnExampleFactoryThatThrows() {
        // Setup
        ErrorDocumentationBuilder builder = new();
        Func<DomainError>         factory = () => throw new InvalidOperationException("boom");

        // Exercise & verify
        Check.ThatCode(() => builder.WithExamples(factory))
             .Throws<ErrorDocumentationException>()
             .WithMessage("Example factory at index 0 threw an exception. Factories must be deterministic and side-effect free.");
    }

    [Fact(DisplayName = "An error documentation builder rejects a null example.")]
    public void AnErrorDocumentationBuilderRejectsANullExample() {
        // Setup
        ErrorDocumentationBuilder builder = new();
        Func<DomainError>         factory = () => null!;

        // Exercise & verify
        Check.ThatCode(() => builder.WithExamples(factory))
             .Throws<ErrorDocumentationException>()
             .WithMessage("Example factory at index 0 returned null. Factories must return a valid error instance.");
    }

    [Fact(DisplayName = "An error documentation builder rejects inconsistent error codes across examples.")]
    public void AnErrorDocumentationBuilderRejectsInconsistentErrorCodesAcrossExamples() {
        // Setup
        ErrorDocumentationBuilder builder = new();

        ErrorCode codeA = ErrorCode.Create("CODE_A");
        ErrorCode codeB = ErrorCode.Create("CODE_B");

        Func<DomainError> first  = () => new DomainError(codeA, "m1");
        Func<DomainError> second = () => new DomainError(codeB, "m2");

        // Exercise & verify
        Check.ThatCode(() => builder.WithExamples(first, second))
             .Throws<ErrorDocumentationException>()
             .WithMessage("All example factories must produce errors with the same ErrorCode. Example at index 1 produced a different ErrorCode. Expected 'CODE_A', but received 'CODE_B'.");
    }

    [Fact(DisplayName = "An error documentation builder uses the examples error code as documentation code.")]
    public void AnErrorDocumentationBuilderUsesTheExamplesErrorCodeAsDocumentationCode() {
        // Setup
        ErrorDocumentationBuilder builder = new();
        ErrorCode                 code    = ErrorCode.Create("TEMPERATURE_BELOW_ABSOLUTE_ZERO");

        Func<DomainError> example = () => new DomainError(code, "boom", "short");

        // Exercise
        ErrorDocumentation doc = builder.WithExamples(example);

        // Verify
        Check.That(doc.Code).IsEqualTo("TEMPERATURE_BELOW_ABSOLUTE_ZERO");
        Check.That(doc.Examples).IsNotNull();
        Check.That(doc.Examples).CountIs(1);

        Check.That(doc.Examples[0].DetailedMessage).IsEqualTo("boom");
        Check.That(doc.Examples[0].ShortMessage).IsEqualTo("short");
    }

    [Fact(DisplayName = "An error documentation builder includes the provided diagnostics.")]
    public void AnErrorDocumentationBuilderIncludesTheProvidedDiagnostics() {
        // Setup
        ErrorDocumentationBuilder builder = new();

        ErrorDiagnostic first  = new("cause-1", ErrorOrigin.External, "lead-1");
        ErrorDiagnostic second = new("cause-2", ErrorOrigin.Internal, "lead-2");

        ErrorCode         code    = ErrorCode.Create("ANY_CODE");
        Func<DomainError> example = () => new DomainError(code, "boom");

        // Exercise
        ErrorDocumentation doc = builder
                                .WithDiagnostics(first, second)
                                .WithExamples(example);

        // Verify
        Check.That(doc.Diagnostics).IsNotNull();
        Check.That(doc.Diagnostics).CountIs(2);

        Check.That(doc.Diagnostics[0].PossibleCause).IsEqualTo("cause-1");
        Check.That(doc.Diagnostics[0].Origin).IsEqualTo(ErrorOrigin.External);
        Check.That(doc.Diagnostics[0].AnalysisHint).IsEqualTo("lead-1");

        Check.That(doc.Diagnostics[1].PossibleCause).IsEqualTo("cause-2");
        Check.That(doc.Diagnostics[1].Origin).IsEqualTo(ErrorOrigin.Internal);
        Check.That(doc.Diagnostics[1].AnalysisHint).IsEqualTo("lead-2");
    }

    [Fact(DisplayName = "An error documentation builder includes diagnostics added incrementally.")]
    public void AnErrorDocumentationBuilderIncludesDiagnosticsAddedIncrementally() {
        // Setup
        ErrorDocumentationBuilder builder = new();

        ErrorCode         code    = ErrorCode.Create("ANY_CODE");
        Func<DomainError> example = () => new DomainError(code, "boom");

        // Exercise
        ErrorDocumentation doc = builder
                                .AndDiagnostic("cause-1", ErrorOrigin.InternalOrExternal, "lead-1")
                                .AndDiagnostic("cause-2", ErrorOrigin.Internal, "lead-2")
                                .WithExamples(example);

        // Verify
        Check.That(doc.Diagnostics).IsNotNull();
        Check.That(doc.Diagnostics).CountIs(2);
    }

    [Fact(DisplayName = "An error documentation builder aggregates context entries by key name.")]
    public void AnErrorDocumentationBuilderAggregatesContextEntriesByKeyName() {
        // Setup
        ErrorContextKey<string> userId        = ErrorContextKey.Create<string>("UserId", "User identifier.");
        ErrorContextKey<string> correlationId = ErrorContextKey.Create<string>("CorrelationId", "Correlation identifier.");

        ErrorCode code = ErrorCode.Create("ANY_CODE");

        Func<DomainError> ex1 = () => new DomainError(
            code,
            "m1",
            configureContext: ctx => {
                ctx.Add(userId, "u-1");
                ctx.Add(correlationId, "c-1");
            });

        Func<DomainError> ex2 = () => new DomainError(
            code,
            "m2",
            configureContext: ctx => {
                ctx.Add(userId, "u-2");
                ctx.Add(correlationId, "c-1");
            });

        // Exercise
        ErrorDocumentation doc = new ErrorDocumentationBuilder()
           .WithExamples(ex1, ex2);

        // Verify
        Check.That(doc.Context).IsNotNull();
        Check.That(doc.Context).CountIs(2);

        ErrorContextEntryDocumentation userIdEntry = doc.Context.Single(x => x.Key == "UserId");
        Check.That(userIdEntry.Description).IsEqualTo("User identifier.");
        Check.That(userIdEntry.ValueType).IsEqualTo("System.String");
        Check.That(userIdEntry.ExampleValues.OrderBy(x => x)).ContainsExactly("u-1", "u-2");

        ErrorContextEntryDocumentation correlationEntry = doc.Context.Single(x => x.Key == "CorrelationId");
        Check.That(correlationEntry.Description).IsEqualTo("Correlation identifier.");
        Check.That(correlationEntry.ValueType).IsEqualTo("System.String");
        Check.That(correlationEntry.ExampleValues).ContainsExactly("c-1");
    }

    [Fact(DisplayName = "An error documentation builder excludes null context example values.")]
    public void AnErrorDocumentationBuilderExcludesNullContextExampleValues() {
        // Setup
        ErrorContextKey<string> optionalInfo = ErrorContextKey.Create<string>("OptionalInfo", "Optional info.");
        ErrorCode               code         = ErrorCode.Create("ANY_CODE");

        Func<DomainError> ex1 = () => new DomainError(
            code,
            "m1",
            configureContext: ctx => ctx.Add(optionalInfo, null));

        Func<DomainError> ex2 = () => new DomainError(
            code,
            "m2",
            configureContext: ctx => ctx.Add(optionalInfo, "x"));

        // Exercise
        ErrorDocumentation doc = new ErrorDocumentationBuilder()
           .WithExamples(ex1, ex2);

        // Verify
        ErrorContextEntryDocumentation entry = doc.Context.Single(x => x.Key == "OptionalInfo");
        Check.That(entry.ExampleValues).ContainsExactly("x");
    }

    [Fact(DisplayName = "An error documentation builder cannot accept a null diagnostics collection.")]
    public void AnErrorDocumentationBuilderCannotAcceptANullDiagnosticsCollection() {
        // Setup
        ErrorDocumentationBuilder builder          = new();
        ErrorDiagnostic[]         errorDiagnostics = null!;

        // Exercise & verify
        Check.ThatCode(() => builder.WithDiagnostics(errorDiagnostics)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Diagnostics can be added through the diagnostics stage interface.")]
    public void DiagnosticsCanBeAddedThroughTheDiagnosticsStageInterface() {
        // Setup
        ErrorDocumentationBuilder builder = new();
        IErrorDiagnosticsStage    stage   = builder;

        ErrorCode code = ErrorCode.Create("ANY_CODE");
        Func<DomainError> example =
            () => new DomainError(code, "boom");

        // Exercise
        ErrorDocumentation doc = stage
                                .WithDiagnostic("cause", ErrorOrigin.Internal, "lead")
                                .WithExamples(example);

        // Verify
        Check.That(doc.Diagnostics).CountIs(1);
        Check.That(doc.Diagnostics[0].PossibleCause).IsEqualTo("cause");
    }

    #region Nested types declarations

    private static class StringFactory {

        #region Statics members declarations

        public static string AnyDescription() {
            return "any description";
        }

        public static string AnyErrorLongMessage() {
            return "any error detailed message";
        }

        public static string AnyTitle() {
            return "any title";
        }

        public static string AnyExplanation() {
            return "any explanation";
        }

        #endregion

    }

    #endregion

}