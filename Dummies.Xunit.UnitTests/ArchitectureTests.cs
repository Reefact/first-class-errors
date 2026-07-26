#region Usings declarations

using System.Reflection;

using NFluent;

#endregion

namespace Dummies.Xunit.UnitTests;

/// <summary>
///     Guards the boundary of the companion package. Dummies itself may depend on nothing beyond the standard
///     library (ADR-0011), which is precisely why the xUnit adapter is a separate package (ADR-0036): it exists to
///     carry the one dependency Dummies cannot. What it must never carry is a FirstClassErrors dependency — the
///     error-agnostic promise applies to the whole Dummies line, not just its core assembly.
/// </summary>
public sealed class ArchitectureTests {

    [Fact(DisplayName = "Dummies.Xunit references no FirstClassErrors assembly.")]
    public void DummiesXunitReferencesNoFirstClassErrorsAssembly() {
        AssemblyName[] references = typeof(ReproducibleAttribute).Assembly.GetReferencedAssemblies();

        foreach (AssemblyName reference in references) {
            Check.WithCustomMessage($"Unexpected assembly reference: {reference.Name}")
                 .That(reference.Name!.StartsWith("FirstClassErrors", StringComparison.Ordinal)).IsFalse();
        }
    }

    [Fact(DisplayName = "Dummies.Xunit depends on nothing beyond the standard library, Dummies and xUnit.")]
    public void DummiesXunitDependsOnlyOnDummiesAndXunit() {
        AssemblyName[] references = typeof(ReproducibleAttribute).Assembly.GetReferencedAssemblies();

        foreach (AssemblyName reference in references) {
            // The exact facade split varies with the SDK, so the guard checks the intent — the standard
            // library, the library being adapted, and the framework it is adapted to — not a fixed list.
            bool expected = reference.Name is "netstandard" or "mscorlib" or "Dummies"
                         || reference.Name!.StartsWith("System.", StringComparison.Ordinal)
                         || reference.Name.StartsWith("xunit.", StringComparison.Ordinal);

            Check.WithCustomMessage($"Unexpected assembly reference: {reference.Name}").That(expected).IsTrue();
        }
    }

}
