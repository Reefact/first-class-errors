#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

[TestSubject(typeof(DocumentationRequestError))]
public sealed class DocumentationRequestErrorTests {

    [Fact(DisplayName = "SolutionNotFound is a coded, non-transient incoming error carrying the solution path.")]
    public void SolutionNotFoundCarriesItsFacts() {
        // Exercise
        PrimaryPortError error = DocumentationRequestError.SolutionNotFound("/src/app/Application.sln");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_SOLUTION_NOT_FOUND");
        Check.That(error.Transience).IsEqualTo(Transience.NonTransient);
        Check.That(error.Direction).IsEqualTo(InteractionDirection.Incoming);
        Check.That(error.DiagnosticMessage).Contains("/src/app/Application.sln");
        Check.That(error.Context.ToNameDictionary()["SolutionPath"]).IsEqualTo("/src/app/Application.sln");
    }

    [Fact(DisplayName = "SolutionPathUnsupported is a coded, non-transient incoming error carrying the path.")]
    public void SolutionPathUnsupportedCarriesItsFacts() {
        // Exercise
        PrimaryPortError error = DocumentationRequestError.SolutionPathUnsupported("/src/app/Application.slnf");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_SOLUTION_PATH_UNSUPPORTED");
        Check.That(error.Transience).IsEqualTo(Transience.NonTransient);
        Check.That(error.Context.ToNameDictionary()["SolutionPath"]).IsEqualTo("/src/app/Application.slnf");
    }

    [Fact(DisplayName = "AssemblyNotFound is a coded, non-transient incoming error carrying the assembly path.")]
    public void AssemblyNotFoundCarriesItsFacts() {
        // Exercise
        PrimaryPortError error = DocumentationRequestError.AssemblyNotFound("/src/app/bin/Application.dll");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_ASSEMBLY_NOT_FOUND");
        Check.That(error.Transience).IsEqualTo(Transience.NonTransient);
        Check.That(error.Context.ToNameDictionary()["AssemblyPath"]).IsEqualTo("/src/app/bin/Application.dll");
    }

    [Fact(DisplayName = "TargetAssemblyNotFound carries both the project path and the resolved target path.")]
    public void TargetAssemblyNotFoundCarriesItsFacts() {
        // Exercise
        PrimaryPortError error = DocumentationRequestError.TargetAssemblyNotFound("/src/app/App.csproj", "/src/app/bin/App.dll");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_TARGET_ASSEMBLY_NOT_FOUND");
        Check.That(error.Context.ToNameDictionary()["ProjectPath"]).IsEqualTo("/src/app/App.csproj");
        Check.That(error.Context.ToNameDictionary()["TargetPath"]).IsEqualTo("/src/app/bin/App.dll");
    }

    [Fact(DisplayName = "OptInAmbiguous carries the project, the property name and the ambiguity reason.")]
    public void OptInAmbiguousCarriesItsFacts() {
        // Exercise
        PrimaryPortError error = DocumentationRequestError.OptInAmbiguous("/src/app/App.csproj", "GenerateErrorDocumentation", "defined 2 times");

        // Verify: the diagnostic message names the property and the reason, so Continue-mode warnings stay diagnosable.
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_OPT_IN_AMBIGUOUS");
        Check.That(error.DiagnosticMessage).Contains("GenerateErrorDocumentation");
        Check.That(error.DiagnosticMessage).Contains("defined 2 times");
        Check.That(error.Context.ToNameDictionary()["ProjectPath"]).IsEqualTo("/src/app/App.csproj");
        Check.That(error.Context.ToNameDictionary()["OptInProperty"]).IsEqualTo("GenerateErrorDocumentation");
        Check.That(error.Context.ToNameDictionary()["AmbiguityReason"]).IsEqualTo("defined 2 times");
    }

    [Fact(DisplayName = "WorkerPathInvalid carries the configured worker path.")]
    public void WorkerPathInvalidCarriesItsFacts() {
        // Exercise
        PrimaryPortError error = DocumentationRequestError.WorkerPathInvalid("/tools/fce/FirstClassErrors.GenDoc.Worker.dll");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_WORKER_PATH_INVALID");
        Check.That(error.Context.ToNameDictionary()["WorkerPath"]).IsEqualTo("/tools/fce/FirstClassErrors.GenDoc.Worker.dll");
    }

}
