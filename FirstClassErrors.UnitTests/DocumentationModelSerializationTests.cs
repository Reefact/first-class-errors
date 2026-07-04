#region Usings declarations

using System.Text.Json;

using FirstClassErrors.GenDoc;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

/// <summary>
///     Guards the "Option A" contract: the documentation model is plain, serializable data. It must round-trip through
///     System.Text.Json <b>by convention alone</b> — no serialization attributes in the base library — so the
///     out-of-process worker can emit it as JSON and the generator can read it back unchanged.
/// </summary>
[TestSubject(typeof(ErrorDocumentationExtractionResult))]
public sealed class DocumentationModelSerializationTests {

    [Fact(DisplayName = "An extraction result round-trips through JSON, preserving the whole documentation model.")]
    public void AnExtractionResultRoundTripsThroughJsonPreservingTheDocumentationModel() {
        // Setup
        ErrorDocumentation documentation = new() {
            Code         = "TEMPERATURE_BELOW_ABSOLUTE_ZERO",
            Title        = "Temperature below absolute zero",
            Explanation  = "A temperature was instantiated below absolute zero.",
            BusinessRule = "Temperature cannot go below absolute zero.",
            Source       = "Temperature",
            Diagnostics = new[] {
                new ErrorDiagnostic("A value entered by a user is invalid.", ErrorOrigin.External, "Verify the user input."),
                new ErrorDiagnostic("A value computed internally is invalid.", ErrorOrigin.Internal, "Inspect the computation.")
            },
            Examples = new[] {
                new ErrorDescription("Temperature is below absolute zero.", "Failed to instantiate temperature: -300 is below absolute zero.", "The provided temperature is below the minimum allowed value.")
            },
            Context = new[] {
                new ErrorContextEntryDocumentation {
                    Key           = "AttemptedValue",
                    ValueType     = "System.Double",
                    Description   = "The rejected value.",
                    ExampleValues = new[] { "-300", "-500" }
                }
            }
        };

        ErrorDocumentationExtractionFailure failure = new(
            "Some.Broken.Factory",
            "Create",
            "The documentation factory threw while being executed.",
            "System.InvalidOperationException: boom");

        ErrorDocumentationExtractionResult result = new(new[] { documentation }, new[] { failure });

        // Exercise
        string                              json  = JsonSerializer.Serialize(result);
        ErrorDocumentationExtractionResult? round = JsonSerializer.Deserialize<ErrorDocumentationExtractionResult>(json);

        // Verify
        Check.That(round).IsNotNull();
        Check.That(round!.Documentation).CountIs(1);
        Check.That(round.Failures).CountIs(1);
        Check.That(round.HasFailures).IsTrue();

        ErrorDocumentation restored = round.Documentation[0];
        Check.That(restored.Code).IsEqualTo("TEMPERATURE_BELOW_ABSOLUTE_ZERO");
        Check.That(restored.Title).IsEqualTo("Temperature below absolute zero");
        Check.That(restored.Explanation).IsEqualTo("A temperature was instantiated below absolute zero.");
        Check.That(restored.BusinessRule).IsEqualTo("Temperature cannot go below absolute zero.");
        Check.That(restored.Source).IsEqualTo("Temperature");

        Check.That(restored.Diagnostics).CountIs(2);
        Check.That(restored.Diagnostics[0].PossibleCause).IsEqualTo("A value entered by a user is invalid.");
        Check.That(restored.Diagnostics[0].Origin).IsEqualTo(ErrorOrigin.External);
        Check.That(restored.Diagnostics[0].AnalysisHint).IsEqualTo("Verify the user input.");
        Check.That(restored.Diagnostics[1].Origin).IsEqualTo(ErrorOrigin.Internal);

        Check.That(restored.Examples).CountIs(1);
        Check.That(restored.Examples[0].ShortMessage).IsEqualTo("Temperature is below absolute zero.");
        Check.That(restored.Examples[0].DiagnosticMessage).IsEqualTo("Failed to instantiate temperature: -300 is below absolute zero.");
        Check.That(restored.Examples[0].DetailedMessage).IsEqualTo("The provided temperature is below the minimum allowed value.");

        ErrorContextEntryDocumentation contextEntry = restored.Context.Single();
        Check.That(contextEntry.Key).IsEqualTo("AttemptedValue");
        Check.That(contextEntry.ValueType).IsEqualTo("System.Double");
        Check.That(contextEntry.Description).IsEqualTo("The rejected value.");
        Check.That(contextEntry.ExampleValues).ContainsExactly("-300", "-500");

        ErrorDocumentationExtractionFailure restoredFailure = round.Failures[0];
        Check.That(restoredFailure.TypeName).IsEqualTo("Some.Broken.Factory");
        Check.That(restoredFailure.MemberName).IsEqualTo("Create");
        Check.That(restoredFailure.Message).IsEqualTo("The documentation factory threw while being executed.");
        Check.That(restoredFailure.ExceptionDetail).IsEqualTo("System.InvalidOperationException: boom");
    }

    [Fact(DisplayName = "A failure without an underlying exception round-trips with a null ExceptionDetail.")]
    public void AFailureWithoutAnUnderlyingExceptionRoundTripsWithANullExceptionDetail() {
        // Setup
        ErrorDocumentationExtractionResult result = new(
            [],
            new[] { new ErrorDocumentationExtractionFailure("Some.Type", null, "No parameterless static method was found.", null) });

        // Exercise
        string                              json  = JsonSerializer.Serialize(result);
        ErrorDocumentationExtractionResult? round = JsonSerializer.Deserialize<ErrorDocumentationExtractionResult>(json);

        // Verify
        Check.That(round).IsNotNull();
        Check.That(round!.Documentation).IsEmpty();
        Check.That(round.Failures).CountIs(1);
        Check.That(round.Failures[0].MemberName).IsNull();
        Check.That(round.Failures[0].ExceptionDetail).IsNull();
    }

}
