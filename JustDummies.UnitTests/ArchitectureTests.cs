#region Usings declarations

using System.Reflection;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Guards the standalone boundary of the library: JustDummies is error-agnostic and must never gain a dependency on
///     FirstClassErrors (or any other project of this repository). If this test fails, the boundary was crossed —
///     see the ADR that records the decision before touching it.
/// </summary>
public sealed class ArchitectureTests {

    [Fact(DisplayName = "JustDummies references no FirstClassErrors assembly.")]
    public void JustDummiesReferencesNoFirstClassErrorsAssembly() {
        AssemblyName[] references = typeof(Any).Assembly.GetReferencedAssemblies();

        foreach (AssemblyName reference in references) {
            Check.That(reference.Name!.StartsWith("FirstClassErrors", StringComparison.Ordinal)).IsFalse();
        }
    }

    [Fact(DisplayName = "JustDummies depends on nothing beyond the standard library.")]
    public void JustDummiesDependsOnNothingBeyondTheStandardLibrary() {
        AssemblyName[] references = typeof(Any).Assembly.GetReferencedAssemblies();

        foreach (AssemblyName reference in references) {
            // The exact facade split (System.Runtime, System.Threading, ...) varies with the SDK and build
            // configuration, so the guard checks the intent — standard library only — not a fixed list.
            bool standard = reference.Name is "netstandard" or "mscorlib" || reference.Name!.StartsWith("System.", StringComparison.Ordinal);
            Check.WithCustomMessage($"Unexpected assembly reference: {reference.Name}").That(standard).IsTrue();
        }
    }

}
