#region Usings declarations

using System.Xml.Linq;

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

/// <summary>
///     Guards the build-time wiring that makes the FCExxx analyzers run on the binder. The binder consumes
///     <c>FirstClassErrors</c> through a <c>ProjectReference</c>, which — unlike the NuGet package — does not carry
///     the bundled analyzers, so they must be referenced explicitly (issue #153). Dropping that reference would
///     silently stop enforcing the binder's own error documentation at compile time; these tests fail if it goes away.
/// </summary>
public sealed class AnalyzerDogfoodingTests {

    [Fact(DisplayName = "The binder dogfoods the FCExxx analyzers: its project file references FirstClassErrors.Analyzers as an analyzer.")]
    public void BinderReferencesTheAnalyzersAsAnalyzer() {
        XElement analyzerReference = AnalyzerProjectReference();

        // OutputItemType=Analyzer is what actually runs the analyzers at build/IDE time; without it the reference
        // would be inert and the binder's error documentation would go unchecked, which is exactly issue #153.
        Check.That(MetadataOf(analyzerReference, "OutputItemType")).IsEqualTo("Analyzer");
    }

    [Fact(DisplayName = "The binder's analyzer reference stays build-only: off the runtime graph and out of the package's dependencies.")]
    public void BinderAnalyzerReferenceStaysBuildOnly() {
        XElement analyzerReference = AnalyzerProjectReference();

        Check.That(MetadataOf(analyzerReference, "ReferenceOutputAssembly")).IsEqualTo("false");
        Check.That(MetadataOf(analyzerReference, "PrivateAssets")).IsEqualTo("all");
    }

    #region Locating the binder project file and its analyzer reference

    private static XElement AnalyzerProjectReference() {
        XDocument project = XDocument.Load(BinderProjectFilePath());

        List<XElement> analyzerReferences = project.Descendants()
                                                   .Where(element => element.Name.LocalName == "ProjectReference")
                                                   .Where(ReferencesTheAnalyzers)
                                                   .ToList();

        // Exactly one: zero means the wiring was removed (issue #153 regressed), more than one means a duplicate.
        Check.That(analyzerReferences).HasSize(1);

        return analyzerReferences[0];
    }

    private static bool ReferencesTheAnalyzers(XElement projectReference) {
        string include  = (string?)projectReference.Attribute("Include") ?? string.Empty;
        string fileName = include.Replace('\\', '/').Split('/').Last();

        return string.Equals(fileName, "FirstClassErrors.Analyzers.csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static string? MetadataOf(XElement projectReference, string name) {
        // MSBuild metadata may be written as an attribute (OutputItemType="Analyzer") or a nested element
        // (<OutputItemType>Analyzer</OutputItemType>). The two are equivalent, so accept either form.
        string? attribute = (string?)projectReference.Attribute(name);
        if (attribute is not null) { return attribute; }

        return projectReference.Elements()
                               .FirstOrDefault(element => element.Name.LocalName == name)
                               ?.Value;
    }

    private static string BinderProjectFilePath() {
        // Walk up from the test assembly's runtime location to the binder project. AppContext.BaseDirectory is
        // resolved at run time, so it survives a deterministic CI build (ContinuousIntegrationBuild=true), which
        // path-maps [CallerFilePath] to a normalized '/_/…' value that does not exist on disk.
        for (DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent) {
            string candidate = Path.Combine(directory.FullName,
                                            "FirstClassErrors.RequestBinder",
                                            "FirstClassErrors.RequestBinder.csproj");
            if (File.Exists(candidate)) { return candidate; }
        }

        throw new FileNotFoundException(
            $"Could not locate FirstClassErrors.RequestBinder.csproj by walking up from '{AppContext.BaseDirectory}'.");
    }

    #endregion

}
