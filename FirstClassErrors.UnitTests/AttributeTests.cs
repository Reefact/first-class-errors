#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(ProvidesErrorsForAttribute))]
public sealed class ProvidesErrorsForAttributeTests {

    [Fact(DisplayName = "A provides-errors-for attribute preserves the provided source.")]
    public void AProvidesErrorsForAttributePreservesTheProvidedSource() {
        // Exercise
        ProvidesErrorsForAttribute attribute = new("Model");

        // Verify
        Check.That(attribute.Source).IsEqualTo("Model");
    }

    [Fact(DisplayName = "A provides-errors-for attribute cannot be created from a null source.")]
    public void AProvidesErrorsForAttributeCannotBeCreatedFromANullSource() {
        // Exercise & verify
        Check.ThatCode(() => new ProvidesErrorsForAttribute(null!)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "A provides-errors-for attribute cannot be created from an empty source.")]
    public void AProvidesErrorsForAttributeCannotBeCreatedFromAnEmptySource() {
        // Exercise & verify
        Check.ThatCode(() => new ProvidesErrorsForAttribute("")).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "A provides-errors-for attribute cannot be created from a whitespace-only source.")]
    public void AProvidesErrorsForAttributeCannotBeCreatedFromAWhitespaceOnlySource() {
        // Exercise & verify
        Check.ThatCode(() => new ProvidesErrorsForAttribute("   ")).Throws<ArgumentException>();
    }

}

[TestSubject(typeof(DocumentedByAttribute))]
public sealed class DocumentedByAttributeTests {

    [Fact(DisplayName = "A documented-by attribute preserves the provided method name.")]
    public void ADocumentedByAttributePreservesTheProvidedMethodName() {
        // Exercise
        DocumentedByAttribute attribute = new("Method");

        // Verify
        Check.That(attribute.MethodName).IsEqualTo("Method");
    }

    [Fact(DisplayName = "A documented-by attribute cannot be created from a null method name.")]
    public void ADocumentedByAttributeCannotBeCreatedFromANullMethodName() {
        // Exercise & verify
        Check.ThatCode(() => new DocumentedByAttribute(null!)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "A documented-by attribute cannot be created from an empty method name.")]
    public void ADocumentedByAttributeCannotBeCreatedFromAnEmptyMethodName() {
        // Exercise & verify
        Check.ThatCode(() => new DocumentedByAttribute("")).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "A documented-by attribute cannot be created from a whitespace-only method name.")]
    public void ADocumentedByAttributeCannotBeCreatedFromAWhitespaceOnlyMethodName() {
        // Exercise & verify
        Check.ThatCode(() => new DocumentedByAttribute("   ")).Throws<ArgumentException>();
    }

}
