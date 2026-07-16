#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

/// <summary>
///     The dogfooding contract of the GenDoc project itself: its own failure surface is modeled with FirstClassErrors,
///     so the very pipeline it implements must extract a complete, failure-free catalog of GENDOC_-prefixed codes from
///     its assembly. Extraction executes every documentation method and every example factory in-process — this is the
///     end-to-end proof that GenDoc's own errors are documented, and that their factories are pure (no process spawn,
///     no file-system access).
/// </summary>
public sealed class GenDocSelfDocumentationTests {

    // Extraction reflects over the whole assembly and executes every documentation method and example factory; one
    // shared run serves the three assertions below instead of paying that cost per test.
    private static readonly Lazy<ErrorDocumentationExtractionResult> SelfExtraction =
        new(() => AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(typeof(SolutionErrorDocumentationGenerator).Assembly));

    private static ErrorDocumentationExtractionResult ExtractSelf() {
        return SelfExtraction.Value;
    }

    [Fact(DisplayName = "GenDoc's own assembly extracts without a single failure.")]
    public void GenDocExtractsWithoutFailures() {
        // Exercise
        ErrorDocumentationExtractionResult extraction = ExtractSelf();

        // Verify: every [DocumentedBy] wiring resolves and every example factory runs — a failure here means a
        // documentation method or example of GenDoc's own errors is broken.
        Check.That(extraction.Failures).IsEmpty();
        Check.That(extraction.HasFailures).IsFalse();
    }

    [Fact(DisplayName = "GenDoc documents its complete failure surface under the GENDOC_ prefix.")]
    public void GenDocDocumentsItsCompleteFailureSurface() {
        // Exercise
        ErrorDocumentationExtractionResult extraction = ExtractSelf();

        // Verify: the catalog holds exactly the declared failure surface — set equality, so a missing, extra, or
        // duplicated code all fail by name. A code added or removed here is a contract change of the tool and must
        // be deliberate (this list is the reviewable record of it).
        Check.That(extraction.Documentation.Select(doc => doc.Code)).IsEquivalentTo(
            "GENDOC_SOLUTION_NOT_FOUND",
            "GENDOC_SOLUTION_PATH_UNSUPPORTED",
            "GENDOC_ASSEMBLY_NOT_FOUND",
            "GENDOC_TARGET_ASSEMBLY_NOT_FOUND",
            "GENDOC_OPT_IN_AMBIGUOUS",
            "GENDOC_WORKER_PATH_INVALID",
            "GENDOC_PROJECT_ENUMERATION_FAILED",
            "GENDOC_SOLUTION_BUILD_FAILED",
            "GENDOC_PROCESS_START_FAILED",
            "GENDOC_PROCESS_TIMED_OUT",
            "GENDOC_TARGET_PATH_RESOLUTION_FAILED",
            "GENDOC_WORKER_NOT_DEPLOYED",
            "GENDOC_WORKER_FAILED",
            "GENDOC_WORKER_OUTPUT_MISSING",
            "GENDOC_WORKER_OUTPUT_UNREADABLE",
            "GENDOC_WORKER_RUN_FAILED");
    }

    [Fact(DisplayName = "Every documented GenDoc error carries a title, a description, a diagnostic and an example.")]
    public void EveryGenDocErrorIsFullyDocumented() {
        // Exercise
        ErrorDocumentationExtractionResult extraction = ExtractSelf();

        // Verify: the catalog entries are usable by support and operations, not just present.
        foreach (ErrorDocumentation doc in extraction.Documentation) {
            Check.WithCustomMessage($"'{doc.Code}' must have a title.").That(doc.Title).IsNotEmpty();
            Check.WithCustomMessage($"'{doc.Code}' must have an explanation.").That(doc.Explanation).IsNotEmpty();
            Check.WithCustomMessage($"'{doc.Code}' must have at least one diagnostic.").That(doc.Diagnostics).Not.IsEmpty();
            Check.WithCustomMessage($"'{doc.Code}' must have at least one example.").That(doc.Examples).Not.IsEmpty();
            Check.WithCustomMessage($"'{doc.Code}' must document its context entries.").That(doc.Context).Not.IsEmpty();
        }
    }

}
