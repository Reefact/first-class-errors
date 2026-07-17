#region Usings declarations

using System.Reflection;

using NFluent;

#endregion

namespace Dummies.UnitTests;

/// <summary>
///     Guards the standalone boundary of the library: Dummies is error-agnostic and must never gain a dependency on
///     FirstClassErrors (or any other project of this repository). If this test fails, the boundary was crossed —
///     see the ADR that records the decision before touching it.
/// </summary>
public sealed class ArchitectureTests {

    [Fact(DisplayName = "Dummies references no FirstClassErrors assembly.")]
    public void DummiesReferencesNoFirstClassErrorsAssembly() {
        AssemblyName[] references = typeof(Any).Assembly.GetReferencedAssemblies();

        foreach (AssemblyName reference in references) {
            Check.That(reference.Name!.StartsWith("FirstClassErrors", StringComparison.Ordinal)).IsFalse();
        }
    }

    [Fact(DisplayName = "Dummies depends on nothing beyond the standard library.")]
    public void DummiesDependsOnNothingBeyondTheStandardLibrary() {
        AssemblyName[] references = typeof(Any).Assembly.GetReferencedAssemblies();

        foreach (AssemblyName reference in references) {
            bool standard = reference.Name is "netstandard" or "System.Runtime" or "System.Linq" or "System.Threading" or "System.Runtime.Extensions" or "System.Collections";
            Check.WithCustomMessage($"Unexpected assembly reference: {reference.Name}").That(standard).IsTrue();
        }
    }

}
