#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

[TestSubject(typeof(DocumentationToolchainError))]
public sealed class DocumentationToolchainErrorTests {

    [Fact(DisplayName = "ProjectEnumerationFailed carries the solution path and the exit code, and appends the error output.")]
    public void ProjectEnumerationFailedCarriesItsFacts() {
        // Exercise
        SecondaryPortError error = DocumentationToolchainError.ProjectEnumerationFailed("/src/app/Application.sln", 1, "invalid solution");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_PROJECT_ENUMERATION_FAILED");
        Check.That(error.Transience).IsEqualTo(Transience.NonTransient);
        Check.That(error.Direction).IsEqualTo(InteractionDirection.Outgoing);
        Check.That(error.DiagnosticMessage).Contains("invalid solution");
        Check.That(error.Context.ToNameDictionary()["SolutionPath"]).IsEqualTo("/src/app/Application.sln");
        Check.That(error.Context.ToNameDictionary()["ExitCode"]).IsEqualTo(1);
    }

    [Fact(DisplayName = "SolutionBuildFailed carries the solution path and the exit code, and appends the build output.")]
    public void SolutionBuildFailedCarriesItsFacts() {
        // Exercise
        SecondaryPortError error = DocumentationToolchainError.SolutionBuildFailed("/src/app/Application.sln", 1, "CS1002: ; expected");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_SOLUTION_BUILD_FAILED");
        Check.That(error.Transience).IsEqualTo(Transience.NonTransient);
        Check.That(error.DiagnosticMessage).Contains("CS1002");
        Check.That(error.Context.ToNameDictionary()["ExitCode"]).IsEqualTo(1);
    }

    [Fact(DisplayName = "ProcessStartFailed carries the executable name.")]
    public void ProcessStartFailedCarriesItsFacts() {
        // Exercise
        SecondaryPortError error = DocumentationToolchainError.ProcessStartFailed("dotnet");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_PROCESS_START_FAILED");
        Check.That(error.Context.ToNameDictionary()["ProcessFileName"]).IsEqualTo("dotnet");
    }

    [Fact(DisplayName = "ProcessTimedOut is the transient toolchain error, carrying the command, its target, the timeout and the captured output.")]
    public void ProcessTimedOutIsTransientAndCarriesItsFacts() {
        // Exercise
        SecondaryPortError error = DocumentationToolchainError.ProcessTimedOut("dotnet build /src/app/Application.sln", "/src/app/Application.sln", TimeSpan.FromMinutes(10), "Determining projects to restore...");

        // Verify: a timeout is the one toolchain failure a plain retry can fix — it must say so. The output captured
        // before the kill is often the only trace of where the process stalled, so it must survive in the message.
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_PROCESS_TIMED_OUT");
        Check.That(error.Transience).IsEqualTo(Transience.Transient);
        Check.That(error.DiagnosticMessage).Contains("Determining projects to restore...");
        Check.That(error.Context.ToNameDictionary()["Command"]).IsEqualTo("dotnet build /src/app/Application.sln");
        Check.That(error.Context.ToNameDictionary()["Target"]).IsEqualTo("/src/app/Application.sln");
        Check.That(error.Context.ToNameDictionary()["Timeout"]).IsEqualTo(TimeSpan.FromMinutes(10));
    }

    [Fact(DisplayName = "TargetPathResolutionFailed carries the project path.")]
    public void TargetPathResolutionFailedCarriesItsFacts() {
        // Exercise
        SecondaryPortError error = DocumentationToolchainError.TargetPathResolutionFailed("/src/app/App.csproj");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_TARGET_PATH_RESOLUTION_FAILED");
        Check.That(error.Context.ToNameDictionary()["ProjectPath"]).IsEqualTo("/src/app/App.csproj");
    }

    [Fact(DisplayName = "WorkerNotDeployed carries the probed directory.")]
    public void WorkerNotDeployedCarriesItsFacts() {
        // Exercise
        SecondaryPortError error = DocumentationToolchainError.WorkerNotDeployed("/tools/fce");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_WORKER_NOT_DEPLOYED");
        Check.That(error.Context.ToNameDictionary()["ProbedDirectory"]).IsEqualTo("/tools/fce");
    }

    [Fact(DisplayName = "WorkerFailed carries the assembly path and the exit code, and appends the worker's error output.")]
    public void WorkerFailedCarriesItsFacts() {
        // Exercise
        SecondaryPortError error = DocumentationToolchainError.WorkerFailed("/src/app/bin/App.dll", 1, "load failure");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_WORKER_FAILED");
        Check.That(error.DiagnosticMessage).Contains("load failure");
        Check.That(error.Context.ToNameDictionary()["AssemblyPath"]).IsEqualTo("/src/app/bin/App.dll");
        Check.That(error.Context.ToNameDictionary()["ExitCode"]).IsEqualTo(1);
    }

    [Fact(DisplayName = "WorkerOutputMissing carries the assembly path.")]
    public void WorkerOutputMissingCarriesItsFacts() {
        // Exercise
        SecondaryPortError error = DocumentationToolchainError.WorkerOutputMissing("/src/app/bin/App.dll");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_WORKER_OUTPUT_MISSING");
        Check.That(error.Context.ToNameDictionary()["AssemblyPath"]).IsEqualTo("/src/app/bin/App.dll");
    }

    [Fact(DisplayName = "WorkerOutputUnreadable carries the assembly path.")]
    public void WorkerOutputUnreadableCarriesItsFacts() {
        // Exercise
        SecondaryPortError error = DocumentationToolchainError.WorkerOutputUnreadable("/src/app/bin/App.dll");

        // Verify
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_WORKER_OUTPUT_UNREADABLE");
        Check.That(error.Context.ToNameDictionary()["AssemblyPath"]).IsEqualTo("/src/app/bin/App.dll");
    }

    [Fact(DisplayName = "WorkerRunFailed has unknown transience and carries the assembly path.")]
    public void WorkerRunFailedCarriesItsFacts() {
        // Exercise
        SecondaryPortError error = DocumentationToolchainError.WorkerRunFailed("/src/app/bin/App.dll");

        // Verify: the cause is an arbitrary runtime exception, so no transience claim can be made.
        Check.That(error.Code.ToString()).IsEqualTo("GENDOC_WORKER_RUN_FAILED");
        Check.That(error.Transience).IsEqualTo(Transience.Unknown);
        Check.That(error.Context.ToNameDictionary()["AssemblyPath"]).IsEqualTo("/src/app/bin/App.dll");
    }

}
