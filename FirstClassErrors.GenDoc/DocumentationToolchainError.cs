namespace FirstClassErrors.GenDoc;

/// <summary>
///     Provides the secondary-port (outgoing) errors raised when the toolchain GenDoc drives fails: the .NET SDK
///     commands it spawns to enumerate and build solutions, and the extraction worker process it launches per
///     assembly.
/// </summary>
/// <remarks>
///     These errors are GenDoc documenting itself with FirstClassErrors: each failure of the generator carries a
///     stable code, structured context, and generated documentation — exactly what the library asks of application
///     errors. Factories only assemble <see cref="Error" /> instances from already-computed facts (exit codes,
///     captured output, paths); they never touch the file system or spawn processes, so the documentation examples
///     stay side-effect free.
/// </remarks>
[ProvidesErrorsFor("DocumentationToolchain",
                   Description = "The toolchain the documentation generator drives: the .NET SDK commands it spawns and the extraction worker process it launches per assembly.")]
internal static class DocumentationToolchainError {

    #region Statics members declarations

    /// <summary>'dotnet sln list' failed, so the solution's projects could not be enumerated.</summary>
    [DocumentedBy(nameof(ProjectEnumerationFailedDocumentation))]
    internal static SecondaryPortError ProjectEnumerationFailed(string solutionPath, int exitCode, string errorOutput) {
        return SecondaryPortError.Create(
                                     Code.ProjectEnumerationFailed,
                                     FormattableString.Invariant($"Failed to list the projects of solution '{solutionPath}' (exit code {exitCode}).\n{errorOutput}"),
                                     Transience.NonTransient,
                                     ctx => {
                                         ctx.Add(ErrCtxKey.SolutionPath, solutionPath);
                                         ctx.Add(ErrCtxKey.ExitCode, exitCode);
                                     })
                                 .WithPublicMessage(
                                     "The solution's projects could not be listed.",
                                     "The 'dotnet sln list' command failed for the requested solution.");
    }

    /// <summary>'dotnet build' failed for the solution under documentation.</summary>
    [DocumentedBy(nameof(SolutionBuildFailedDocumentation))]
    internal static SecondaryPortError SolutionBuildFailed(string solutionPath, int exitCode, string buildOutput) {
        return SecondaryPortError.Create(
                                     Code.SolutionBuildFailed,
                                     FormattableString.Invariant($"dotnet build failed for solution '{solutionPath}' (exit code {exitCode}).\n{buildOutput}"),
                                     Transience.NonTransient,
                                     ctx => {
                                         ctx.Add(ErrCtxKey.SolutionPath, solutionPath);
                                         ctx.Add(ErrCtxKey.ExitCode, exitCode);
                                     })
                                 .WithPublicMessage(
                                     "The solution build failed.",
                                     "The solution under documentation did not build; the build output is in the diagnostic message.");
    }

    /// <summary>A child process required by the generation (the 'dotnet' host) could not be started at all.</summary>
    [DocumentedBy(nameof(ProcessStartFailedDocumentation))]
    internal static SecondaryPortError ProcessStartFailed(string fileName) {
        return SecondaryPortError.Create(
                                     Code.ProcessStartFailed,
                                     $"Failed to start process '{fileName}'.",
                                     Transience.NonTransient,
                                     ctx => ctx.Add(ErrCtxKey.ProcessFileName, fileName))
                                 .WithPublicMessage(
                                     "A required process could not be started.",
                                     "A child process required by the documentation generation could not be started.");
    }

    /// <summary>A child process exceeded its configured timeout and was killed.</summary>
    /// <param name="command">Short description of the command that was running.</param>
    /// <param name="target">Path of the solution, project or assembly the command was operating on.</param>
    /// <param name="timeout">The configured timeout that was exceeded.</param>
    /// <param name="capturedOutput">The output captured before the kill; often the only trace of where the process stalled.</param>
    [DocumentedBy(nameof(ProcessTimedOutDocumentation))]
    internal static SecondaryPortError ProcessTimedOut(string command, string target, TimeSpan timeout, string capturedOutput) {
        return SecondaryPortError.Create(
                                     Code.ProcessTimedOut,
                                     FormattableString.Invariant($"Process '{command}' timed out after {timeout} and was killed.\n{capturedOutput}"),
                                     Transience.Transient,
                                     ctx => {
                                         ctx.Add(ErrCtxKey.Command, command);
                                         ctx.Add(ErrCtxKey.Target, target);
                                         ctx.Add(ErrCtxKey.Timeout, timeout);
                                     })
                                 .WithPublicMessage(
                                     "A documentation process timed out.",
                                     "The operation exceeded its configured timeout and was killed; it can be retried.");
    }

    /// <summary>Resolving a project's build output path (MSBuild TargetPath) through the SDK failed.</summary>
    [DocumentedBy(nameof(TargetPathResolutionFailedDocumentation))]
    internal static SecondaryPortError TargetPathResolutionFailed(string projectPath) {
        return SecondaryPortError.Create(
                                     Code.TargetPathResolutionFailed,
                                     $"Failed to resolve target path for project '{projectPath}'.",
                                     Transience.NonTransient,
                                     ctx => ctx.Add(ErrCtxKey.ProjectPath, projectPath))
                                 .WithPublicMessage(
                                     "A project's target path could not be resolved.",
                                     "The build output path of a project could not be resolved through the .NET SDK.");
    }

    /// <summary>The extraction worker was not found next to the tool (and no explicit path was configured).</summary>
    [DocumentedBy(nameof(WorkerNotDeployedDocumentation))]
    internal static SecondaryPortError WorkerNotDeployed(string probedDirectory) {
        return SecondaryPortError.Create(
                                     Code.WorkerNotDeployed,
                                     $"The documentation worker 'FirstClassErrors.GenDoc.Worker.dll' could not be located in '{probedDirectory}'. " +
                                     $"Set {nameof(SolutionGenerationOptions)}.{nameof(SolutionGenerationOptions.WorkerAssemblyPath)}, or ensure the worker is deployed next to the tool.",
                                     Transience.NonTransient,
                                     ctx => ctx.Add(ErrCtxKey.ProbedDirectory, probedDirectory))
                                 .WithPublicMessage(
                                     "The documentation worker is missing.",
                                     "The extraction worker was not found next to the tool; the installation is incomplete.");
    }

    /// <summary>The extraction worker ran but exited with a non-zero exit code.</summary>
    [DocumentedBy(nameof(WorkerFailedDocumentation))]
    internal static SecondaryPortError WorkerFailed(string assemblyPath, int exitCode, string errorOutput) {
        return SecondaryPortError.Create(
                                     Code.WorkerFailed,
                                     FormattableString.Invariant($"The documentation worker failed (exit code {exitCode}) for '{assemblyPath}'.\n{errorOutput}"),
                                     Transience.NonTransient,
                                     ctx => {
                                         ctx.Add(ErrCtxKey.AssemblyPath, assemblyPath);
                                         ctx.Add(ErrCtxKey.ExitCode, exitCode);
                                     })
                                 .WithPublicMessage(
                                     "The documentation worker failed.",
                                     "The extraction worker exited with an error for one of the documented assemblies.");
    }

    /// <summary>The extraction worker exited successfully but its output file is missing.</summary>
    [DocumentedBy(nameof(WorkerOutputMissingDocumentation))]
    internal static SecondaryPortError WorkerOutputMissing(string assemblyPath) {
        return SecondaryPortError.Create(
                                     Code.WorkerOutputMissing,
                                     $"The documentation worker produced no output for '{assemblyPath}'.",
                                     Transience.NonTransient,
                                     ctx => ctx.Add(ErrCtxKey.AssemblyPath, assemblyPath))
                                 .WithPublicMessage(
                                     "The documentation worker produced no output.",
                                     "The extraction worker exited successfully but its result file is missing.");
    }

    /// <summary>The extraction worker's output file exists but does not deserialize into an extraction result.</summary>
    [DocumentedBy(nameof(WorkerOutputUnreadableDocumentation))]
    internal static SecondaryPortError WorkerOutputUnreadable(string assemblyPath) {
        return SecondaryPortError.Create(
                                     Code.WorkerOutputUnreadable,
                                     $"The documentation worker produced unreadable output for '{assemblyPath}'.",
                                     Transience.NonTransient,
                                     ctx => ctx.Add(ErrCtxKey.AssemblyPath, assemblyPath))
                                 .WithPublicMessage(
                                     "The documentation worker produced unreadable output.",
                                     "The extraction worker's result file could not be read as an extraction result.");
    }

    /// <summary>Launching or harvesting the extraction worker threw an unexpected runtime exception.</summary>
    [DocumentedBy(nameof(WorkerRunFailedDocumentation))]
    internal static SecondaryPortError WorkerRunFailed(string assemblyPath) {
        return SecondaryPortError.Create(
                                     Code.WorkerRunFailed,
                                     $"Failed to run the documentation worker for '{assemblyPath}'.",
                                     Transience.Unknown,
                                     ctx => ctx.Add(ErrCtxKey.AssemblyPath, assemblyPath))
                                 .WithPublicMessage(
                                     "The documentation worker could not be run.",
                                     "Running the extraction worker failed unexpectedly for one of the documented assemblies.");
    }

    private static ErrorDocumentation ProjectEnumerationFailedDocumentation() {
        return DescribeError.WithTitle("Solution project enumeration failed")
                            .WithDescription("The generator enumerates a solution's projects by running 'dotnet sln list'. That command failed, so no project could be discovered. The solution path and the exit code are carried in the error context; the command's error output is appended to the diagnostic message.")
                            .WithoutRule()
                            .WithDiagnostic("The solution file is malformed or references projects that no longer exist.",
                                            ErrorOrigin.External,
                                            "Run 'dotnet sln <solution> list' by hand and read its error output.")
                            .AndDiagnostic("The .NET SDK on the machine is missing or too old for the solution format (for example .slnx support).",
                                           ErrorOrigin.External,
                                           "Check 'dotnet --version' against the solution format and the repository's SDK requirements.")
                            .WithExamples(() => ProjectEnumerationFailed("/src/app/Application.sln", 1, "The solution file is invalid."));
    }

    private static ErrorDocumentation SolutionBuildFailedDocumentation() {
        return DescribeError.WithTitle("Solution build failed")
                            .WithDescription("The generator builds the solution before documenting it (unless the build step is disabled). The build failed, so no assembly could be documented. The solution path and the exit code are carried in the error context; the build output is appended to the diagnostic message.")
                            .WithoutRule()
                            .WithDiagnostic("The solution under documentation has compile errors.",
                                            ErrorOrigin.External,
                                            "Read the build output in the diagnostic message; build the solution by hand to reproduce.")
                            .AndDiagnostic("Package restore failed (offline machine, feed outage, or authentication).",
                                           ErrorOrigin.InternalOrExternal,
                                           "Check the restore section of the build output and the reachability of the configured NuGet feeds.")
                            .WithExamples(() => SolutionBuildFailed("/src/app/Application.sln", 1, "CS1002: ; expected"));
    }

    private static ErrorDocumentation ProcessStartFailedDocumentation() {
        return DescribeError.WithTitle("Toolchain process failed to start")
                            .WithDescription("The generator drives the .NET SDK and its extraction worker through child processes. One of them could not be started at all. The executable name is carried in the error context.")
                            .WithoutRule()
                            .WithDiagnostic("The 'dotnet' host is not installed or not on the PATH of the process running the generation.",
                                            ErrorOrigin.External,
                                            "Run 'dotnet --info' in the same environment (shell, CI step, service account) as the generation.")
                            .WithExamples(() => ProcessStartFailed("dotnet"));
    }

    private static ErrorDocumentation ProcessTimedOutDocumentation() {
        return DescribeError.WithTitle("Toolchain process timed out")
                            .WithDescription("A child process of the generation (an SDK command or the extraction worker) exceeded its configured timeout and was killed. The command, its target and the timeout are carried in the error context; the output captured before the kill is appended to the diagnostic message. It is transient: the run can be retried.")
                            .WithoutRule()
                            .WithDiagnostic("The machine is under load, or a cold NuGet cache made the first build far slower than usual.",
                                            ErrorOrigin.External,
                                            "Retry the run; compare its duration with the configured build, SDK-query and worker timeouts.")
                            .AndDiagnostic("A documented assembly's example factory hangs (extraction executes the documentation methods of the target).",
                                           ErrorOrigin.InternalOrExternal,
                                           "Check which assembly was being processed when the timeout hit; review its documentation examples for blocking calls.")
                            .WithExamples(() => ProcessTimedOut("dotnet build /src/app/Application.sln", "/src/app/Application.sln", TimeSpan.FromMinutes(10), "Determining projects to restore..."));
    }

    private static ErrorDocumentation TargetPathResolutionFailedDocumentation() {
        return DescribeError.WithTitle("Target path resolution failed")
                            .WithDescription("The generator resolves each project's build output path by querying the .NET SDK ('dotnet msbuild -getProperty:TargetPath'). That query threw, so the project cannot be located and is skipped (or the generation stops, per the configured failure behavior). The project path is carried in the error context.")
                            .WithoutRule()
                            .WithDiagnostic("The project file does not evaluate (broken import, missing SDK workload, malformed XML).",
                                            ErrorOrigin.External,
                                            "Run 'dotnet msbuild <project> -getProperty:TargetPath' by hand and read its error output.")
                            .WithExamples(() => TargetPathResolutionFailed("/src/app/Application/Application.csproj"));
    }

    private static ErrorDocumentation WorkerNotDeployedDocumentation() {
        return DescribeError.WithTitle("Documentation worker not deployed")
                            .WithDescription("No explicit worker path is configured, and the extraction worker was not found next to the tool — the conventional location it is deployed to. The probed directory is carried in the error context.")
                            .WithRule("The extraction worker ships next to the tool; an installation without it cannot extract documentation.")
                            .WithDiagnostic("The tool was copied or repackaged without 'FirstClassErrors.GenDoc.Worker.dll' (an incomplete manual install).",
                                            ErrorOrigin.Internal,
                                            "Inspect the probed directory named in the error context; reinstall the tool, or configure an explicit worker path.")
                            .WithExamples(() => WorkerNotDeployed("/tools/fce"));
    }

    private static ErrorDocumentation WorkerFailedDocumentation() {
        return DescribeError.WithTitle("Documentation worker failed")
                            .WithDescription("The extraction worker runs once per documented assembly, in its own process, against that assembly's dependency graph. It exited with a non-zero code for one assembly. The assembly path and the exit code are carried in the error context; the worker's error output is appended to the diagnostic message.")
                            .WithoutRule()
                            .WithDiagnostic("The target assembly failed to load in the worker (missing dependency, mismatched FirstClassErrors version).",
                                            ErrorOrigin.External,
                                            "Read the worker's error output in the diagnostic message; check the target's deps.json next to the assembly.")
                            .AndDiagnostic("A documentation method or example factory of the target threw while the worker executed it.",
                                           ErrorOrigin.External,
                                           "Read the worker's error output; run the target's documentation methods in a unit test to reproduce.")
                            .WithExamples(() => WorkerFailed("/src/app/bin/Release/net8.0/Application.dll", 1, "Fatal error while extracting documentation."));
    }

    private static ErrorDocumentation WorkerOutputMissingDocumentation() {
        return DescribeError.WithTitle("Documentation worker output missing")
                            .WithDescription("The extraction worker exited successfully but the result file it was asked to write does not exist. The assembly path is carried in the error context.")
                            .WithoutRule()
                            .WithDiagnostic("The temporary directory is not writable, or an antivirus or cleanup job removed the file between the worker's exit and its harvesting.",
                                            ErrorOrigin.External,
                                            "Check the permissions and free space of the temporary directory used by the generation.")
                            .WithExamples(() => WorkerOutputMissing("/src/app/bin/Release/net8.0/Application.dll"));
    }

    private static ErrorDocumentation WorkerOutputUnreadableDocumentation() {
        return DescribeError.WithTitle("Documentation worker output unreadable")
                            .WithDescription("The extraction worker exited successfully and wrote a result file, but the file does not deserialize into an extraction result. The assembly path is carried in the error context.")
                            .WithoutRule()
                            .WithDiagnostic("The worker and the generator come from different tool versions and no longer agree on the result format.",
                                            ErrorOrigin.Internal,
                                            "Check that the worker next to the tool belongs to the same installation; reinstall the tool if in doubt.")
                            .WithExamples(() => WorkerOutputUnreadable("/src/app/bin/Release/net8.0/Application.dll"));
    }

    private static ErrorDocumentation WorkerRunFailedDocumentation() {
        return DescribeError.WithTitle("Documentation worker run failed")
                            .WithDescription("Launching the extraction worker, or harvesting its result, threw an unexpected runtime exception (an I/O failure, a permission error…). The assembly path is carried in the error context; the runtime cause travels with the raised exception.")
                            .WithoutRule()
                            .WithDiagnostic("A file-system or permission problem around the temporary result file or the worker binary.",
                                            ErrorOrigin.InternalOrExternal,
                                            "Read the inner exception attached to the failure; check the temporary directory and the tool's installation directory.")
                            .WithExamples(() => WorkerRunFailed("/src/app/bin/Release/net8.0/Application.dll"));
    }

    #endregion

    #region Nested types declarations

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" members",
        Justification = "Each error-code constant deliberately mirrors the name of its factory method; both are always qualified (Code.X versus the X(...) call), so there is no real ambiguity.")]
    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode ProjectEnumerationFailed   = ErrorCode.Create("GENDOC_PROJECT_ENUMERATION_FAILED");
        public static readonly ErrorCode SolutionBuildFailed        = ErrorCode.Create("GENDOC_SOLUTION_BUILD_FAILED");
        public static readonly ErrorCode ProcessStartFailed         = ErrorCode.Create("GENDOC_PROCESS_START_FAILED");
        public static readonly ErrorCode ProcessTimedOut            = ErrorCode.Create("GENDOC_PROCESS_TIMED_OUT");
        public static readonly ErrorCode TargetPathResolutionFailed = ErrorCode.Create("GENDOC_TARGET_PATH_RESOLUTION_FAILED");
        public static readonly ErrorCode WorkerNotDeployed          = ErrorCode.Create("GENDOC_WORKER_NOT_DEPLOYED");
        public static readonly ErrorCode WorkerFailed               = ErrorCode.Create("GENDOC_WORKER_FAILED");
        public static readonly ErrorCode WorkerOutputMissing        = ErrorCode.Create("GENDOC_WORKER_OUTPUT_MISSING");
        public static readonly ErrorCode WorkerOutputUnreadable     = ErrorCode.Create("GENDOC_WORKER_OUTPUT_UNREADABLE");
        public static readonly ErrorCode WorkerRunFailed            = ErrorCode.Create("GENDOC_WORKER_RUN_FAILED");

        #endregion

    }

    #endregion

}
