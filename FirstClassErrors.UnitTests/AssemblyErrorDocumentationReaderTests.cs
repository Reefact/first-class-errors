#region Usings declarations

using System.Reflection;

using FirstClassErrors.GenDoc;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

// The reader scans a whole assembly, so the fixtures below (declared at the bottom of this file) act as its input:
// running it against this test assembly exercises the happy path, the deduplication, and every failure branch in one
// place. Assertions target the fixtures by their unique source / member names rather than by absolute counts, so they
// stay stable regardless of the other fixtures present in the assembly.
[TestSubject(typeof(AssemblyErrorDocumentationReader))]
public sealed class AssemblyErrorDocumentationReaderTests {

    #region Statics members declarations

    private static ErrorDocumentationExtractionResult Extract() {
        return AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(Assembly.GetExecutingAssembly());
    }

    private static ErrorDocumentation? DocumentationWithCode(ErrorDocumentationExtractionResult result, string code) {
        return result.Documentation.FirstOrDefault(doc => string.Equals(doc.Code, code, StringComparison.Ordinal));
    }

    #endregion

    [Fact(DisplayName = "The reader rejects a null assembly.")]
    public void TheReaderRejectsANullAssembly() {
        // Exercise & verify
        Check.ThatCode(() => AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The reader wires the source and its description onto each extracted document.")]
    public void TheReaderWiresTheSourceAndItsDescriptionOntoEachDocument() {
        // Exercise
        ErrorDocumentationExtractionResult result = Extract();

        // Verify
        ErrorDocumentation? happy = DocumentationWithCode(result, "READER_HAPPY");
        Check.That(happy).IsNotNull();
        Check.That(happy!.Title).IsEqualTo("Happy");
        Check.That(happy.Source).IsEqualTo("ReaderHappySource");
        Check.That(happy.SourceDescription).IsEqualTo("Errors from the happy reader fixture.");
    }

    [Fact(DisplayName = "A documentation factory that throws is recorded as a failure instead of aborting the scan.")]
    public void ADocumentationFactoryThatThrowsIsRecordedAsAFailure() {
        // Exercise
        ErrorDocumentationExtractionResult result = Extract();

        // Verify: the throwing fixture is surfaced as a failure...
        ErrorDocumentationExtractionFailure? failure =
            result.Failures.FirstOrDefault(f => f.MemberName == "ThrowingDoc");

        Check.That(failure).IsNotNull();
        Check.That(failure!.TypeName).Contains("ReaderThrowingFixture");
        Check.That(failure.Message).Contains("threw");
        Check.That(failure.ExceptionDetail).Contains("reader-boom");

        // ...and the reader still reads the healthy fixtures, i.e. the scan is not aborted.
        Check.That(result.HasFailures).IsTrue();
        Check.That(DocumentationWithCode(result, "READER_HAPPY")).IsNotNull();
    }

    [Fact(DisplayName = "An unresolvable [DocumentedBy] reference is recorded as a failure.")]
    public void AnUnresolvableDocumentedByReferenceIsRecordedAsAFailure() {
        // Exercise
        ErrorDocumentationExtractionResult result = Extract();

        // Verify
        ErrorDocumentationExtractionFailure? failure =
            result.Failures.FirstOrDefault(f => f.MemberName == "NoSuchReaderMethod");

        Check.That(failure).IsNotNull();
        Check.That(failure!.TypeName).Contains("ReaderUnresolvedFixture");
        Check.That(failure.Message).Contains("No parameterless static method");
    }

    [Fact(DisplayName = "A [DocumentedBy] reference binds to the concrete factory even when a parameterless generic overload shares its name.")]
    public void ADocumentedByReferenceIgnoresAParameterlessGenericOverload() {
        // Exercise
        ErrorDocumentationExtractionResult result = Extract();

        // Verify: `Foo()` and `Foo<T>()` are legal same-name overloads that both look parameterless via reflection.
        // The concrete OverloadedDoc() must be selected (the generic overload would have thrown), so the documentation
        // is produced and no ambiguity failure is recorded for it.
        ErrorDocumentation? doc = DocumentationWithCode(result, "READER_GENERIC_OVERLOAD");
        Check.That(doc).IsNotNull();
        Check.That(doc!.Title).IsEqualTo("Overloaded");

        ErrorDocumentationExtractionFailure? failure =
            result.Failures.FirstOrDefault(f => f.MemberName == "OverloadedDoc");
        Check.That(failure).IsNull();
    }

    [Fact(DisplayName = "A [DocumentedBy] reference whose only match is a generic method definition is reported as unresolved, not invoked.")]
    public void ADocumentedByReferenceMatchingOnlyAGenericMethodIsUnresolved() {
        // Exercise
        ErrorDocumentationExtractionResult result = Extract();

        // Verify: an open generic definition cannot be invoked without a type argument, so it must never be treated as a
        // candidate. The reference resolves to "No parameterless static method" rather than being invoked (which would
        // otherwise surface as a misleading "the factory threw" failure).
        ErrorDocumentationExtractionFailure? failure =
            result.Failures.FirstOrDefault(f => f.MemberName == "GenericOnlyDoc");

        Check.That(failure).IsNotNull();
        Check.That(failure!.TypeName).Contains("ReaderGenericOnlyFixture");
        Check.That(failure.Message).Contains("No parameterless static method");
    }

    [Fact(DisplayName = "A documentation factory that returns the wrong type is recorded as a failure.")]
    public void ADocumentationFactoryReturningTheWrongTypeIsRecordedAsAFailure() {
        // Exercise
        ErrorDocumentationExtractionResult result = Extract();

        // Verify
        ErrorDocumentationExtractionFailure? failure =
            result.Failures.FirstOrDefault(f => f.MemberName == "WrongReturnDoc");

        Check.That(failure).IsNotNull();
        Check.That(failure!.TypeName).Contains("ReaderWrongReturnFixture");
        Check.That(failure.Message).Contains("did not return an ErrorDocumentation");
    }

    [Fact(DisplayName = "Several factories sharing one error code collapse to a single catalog entry.")]
    public void SeveralFactoriesSharingOneErrorCodeCollapseToASingleEntry() {
        // Exercise
        ErrorDocumentationExtractionResult result = Extract();

        // Verify
        int count = result.Documentation.Count(doc => string.Equals(doc.Code, "READER_DUP", StringComparison.Ordinal));
        Check.That(count).IsEqualTo(1);
    }

    [Fact(DisplayName = "A duplicate error code is not dropped silently: the collision is recorded as a failure.")]
    public void ADuplicateErrorCodeIsRecordedAsAFailure() {
        // Exercise
        ErrorDocumentationExtractionResult result = Extract();

        // Verify: the collision on READER_DUP surfaces as a failure that names the code and the sources involved, rather
        // than vanishing without a trace.
        ErrorDocumentationExtractionFailure? failure =
            result.Failures.FirstOrDefault(f => f.Message.Contains("READER_DUP"));

        Check.That(failure).IsNotNull();
        Check.That(failure!.Message).Contains("Duplicate error code");
        Check.That(failure.Message).Contains("ReaderDupSource");
    }

    [Fact(DisplayName = "The extracted documentation is ordered by error code, case-insensitively.")]
    public void TheExtractedDocumentationIsOrderedByErrorCode() {
        // Exercise
        ErrorDocumentationExtractionResult result = Extract();

        // Verify: the surviving documents are sorted by Code (the reader orders the deduplicated catalog).
        string[] codes = result.Documentation.Select(doc => doc.Code ?? string.Empty).ToArray();
        string[] sorted = codes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToArray();
        Check.That(codes).ContainsExactly(sorted);
    }

    [Fact(DisplayName = "A source description keyed to a resource that cannot be found falls back to the key text.")]
    public void AResourceKeyedSourceDescriptionFallsBackToTheKeyWhenUnresolved() {
        // The fixture points DescriptionResourceType at a type with no embedded resources, so the key cannot be
        // resolved; the reader must fall back to the key text rather than throwing.
        ErrorDocumentationExtractionResult result = Extract();

        ErrorDocumentation? keyed = DocumentationWithCode(result, "READER_RESKEY");
        Check.That(keyed).IsNotNull();
        Check.That(keyed!.SourceDescription).IsEqualTo("reader.missing.key");
    }

}

// ---------------------------------------------------------------------------
// Fixtures consumed by the reader (see the note on the test class above).
// ---------------------------------------------------------------------------

[ProvidesErrorsFor("ReaderHappySource", Description = "Errors from the happy reader fixture.")]
[UsedImplicitly]
public static class ReaderHappyFixture {

    [DocumentedBy(nameof(HappyDoc))]
    public static object HappyFactory() {
        return new object();
    }

    public static ErrorDocumentation HappyDoc() {
        return new ErrorDocumentation { Code = "READER_HAPPY", Title = "Happy" };
    }

}

[ProvidesErrorsFor("ReaderThrowingSource")]
[UsedImplicitly]
public static class ReaderThrowingFixture {

    [DocumentedBy(nameof(ThrowingDoc))]
    public static object ThrowingFactory() {
        return new object();
    }

    public static ErrorDocumentation ThrowingDoc() {
        throw new InvalidOperationException("reader-boom");
    }

}

[ProvidesErrorsFor("ReaderUnresolvedSource")]
[UsedImplicitly]
public static class ReaderUnresolvedFixture {

    [DocumentedBy("NoSuchReaderMethod")]
    public static object UnresolvedFactory() {
        return new object();
    }

}

[ProvidesErrorsFor("ReaderWrongReturnSource")]
[UsedImplicitly]
public static class ReaderWrongReturnFixture {

    [DocumentedBy(nameof(WrongReturnDoc))]
    public static object WrongReturnFactory() {
        return new object();
    }

    public static string WrongReturnDoc() {
        return "not an ErrorDocumentation";
    }

}

[ProvidesErrorsFor("ReaderResourceKeySource",
                   Description = "reader.missing.key",
                   DescriptionResourceType = typeof(ReaderResourceKeyFixture))]
[UsedImplicitly]
public static class ReaderResourceKeyFixture {

    [DocumentedBy(nameof(ResourceKeyDoc))]
    public static object ResourceKeyFactory() {
        return new object();
    }

    public static ErrorDocumentation ResourceKeyDoc() {
        return new ErrorDocumentation { Code = "READER_RESKEY", Title = "Resource keyed" };
    }

}

[ProvidesErrorsFor("ReaderGenericOverloadSource")]
[UsedImplicitly]
public static class ReaderGenericOverloadFixture {

    [DocumentedBy(nameof(OverloadedDoc))]
    public static object OverloadedFactory() {
        return new object();
    }

    // A parameterless generic overload legally shares the name with the real factory. The resolver must ignore the open
    // generic definition (it cannot be invoked without a type argument) and bind to the concrete method below.
    public static ErrorDocumentation OverloadedDoc() {
        return new ErrorDocumentation { Code = "READER_GENERIC_OVERLOAD", Title = "Overloaded" };
    }

    [UsedImplicitly]
    public static ErrorDocumentation OverloadedDoc<T>() {
        throw new InvalidOperationException("the generic overload must never be selected");
    }

}

[ProvidesErrorsFor("ReaderGenericOnlySource")]
[UsedImplicitly]
public static class ReaderGenericOnlyFixture {

    [DocumentedBy("GenericOnlyDoc")]
    public static object GenericOnlyFactory() {
        return new object();
    }

    // The only method with this name is an open generic definition, which is not a usable parameterless factory. The
    // reference must resolve to "not found" rather than being invoked (invoking an open generic definition throws).
    [UsedImplicitly]
    public static ErrorDocumentation GenericOnlyDoc<T>() {
        return new ErrorDocumentation { Code = "READER_GENERIC_ONLY", Title = "Generic only" };
    }

}

[ProvidesErrorsFor("ReaderDupSource")]
[UsedImplicitly]
public static class ReaderDuplicateCodeFixture {

    [DocumentedBy(nameof(DupDocA))]
    public static object DupFactoryA() {
        return new object();
    }

    [DocumentedBy(nameof(DupDocB))]
    public static object DupFactoryB() {
        return new object();
    }

    public static ErrorDocumentation DupDocA() {
        return new ErrorDocumentation { Code = "READER_DUP", Title = "First" };
    }

    public static ErrorDocumentation DupDocB() {
        return new ErrorDocumentation { Code = "READER_DUP", Title = "Second" };
    }

}
