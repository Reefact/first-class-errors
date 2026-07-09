using System;
using System.Linq;
using System.Reflection;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

/// <summary>
///     Enforces the analyzer's Roslyn load contract. The analyzer ships inside the FirstClassErrors package
///     and is loaded by each consumer's host compiler, so the Microsoft.CodeAnalysis version it is compiled
///     against is the minimum Roslyn able to load it; a higher one makes it fail to load (CS8032) on older
///     SDKs/IDEs. The floor is defined once in Directory.Build.props and surfaced here through assembly
///     metadata, so the csproj pin and this test can never diverge.
/// </summary>
public sealed class RoslynFloorTests {

    [Fact]
    public void Analyzer_stays_on_the_supported_Roslyn_floor() {
        Assembly analyzerAssembly = typeof(DuplicateErrorCodeAnalyzer).Assembly;
        Version  floor            = ReadFloor(analyzerAssembly);

        // The analyzers use only the language-agnostic API (IOperation), so the compiler emits a reference
        // to Microsoft.CodeAnalysis but not necessarily to Microsoft.CodeAnalysis.CSharp — only *used*
        // references are recorded. Bound the whole family rather than one assembly name.
        AssemblyName[] roslynReferences = analyzerAssembly
           .GetReferencedAssemblies()
           .Where(reference => reference.Name is not null
                            && reference.Name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
           .ToArray();

        // If the family ever disappears from the metadata this test proves nothing: fail loudly rather
        // than pass vacuously.
        Check.That(roslynReferences).Not.IsEmpty();

        string[] offenders = roslynReferences
           .Where(reference => OnMajorMinorBuild(reference.Version) > floor)
           .Select(reference => $"{reference.Name} {reference.Version}")
           .ToArray();

        Check.That(offenders).IsEmpty();
    }

    private static Version ReadFloor(Assembly analyzerAssembly) {
        AssemblyMetadataAttribute floor = analyzerAssembly
           .GetCustomAttributes<AssemblyMetadataAttribute>()
           .Single(metadata => metadata.Key == "RoslynFloorVersion");

        return OnMajorMinorBuild(Version.Parse(floor.Value!));
    }

    // Roslyn assemblies carry a four-part version (x.y.z.0) while the floor is written as x.y.z; comparing on
    // major.minor.build only keeps a raw 4.8.0.0 from reading as newer than the 4.8.0 floor.
    private static Version OnMajorMinorBuild(Version? version) =>
        new(version?.Major ?? 0, version?.Minor ?? 0, version is { Build: >= 0 } ? version.Build : 0);
}
