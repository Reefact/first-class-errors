#region Usings declarations

using System.Reflection;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(DocumentationContractVersionAttribute))]
public sealed class DocumentationContractVersionTests {

    [Fact(DisplayName = "The library assembly declares the current documentation-contract version.")]
    public void TheLibraryAssemblyDeclaresTheCurrentDocumentationContractVersion() {
        // Exercise
        DocumentationContractVersionAttribute? attribute =
            typeof(DocumentationContractVersionAttribute).Assembly.GetCustomAttribute<DocumentationContractVersionAttribute>();

        // Verify
        Check.That(attribute).IsNotNull();
        Check.That(attribute!.Version).IsEqualTo(DocumentationContractVersionAttribute.CurrentVersion);
    }

    [Fact(DisplayName = "The documentation-contract version starts at 1 (an unmarked assembly reads as version 1).")]
    public void TheDocumentationContractVersionStartsAtOne() {
        // Verify
        Check.That(DocumentationContractVersionAttribute.CurrentVersion).IsStrictlyGreaterThan(0);
    }

}
