namespace FirstClassErrors.GenDoc;

/// <summary>
///     Provides the primary-port (incoming) errors raised when a documentation-generation request is invalid: the
///     solution, assemblies, worker path, or opt-in markers it designates cannot be used as requested.
/// </summary>
/// <remarks>
///     These errors are GenDoc documenting itself with FirstClassErrors: each failure of the generator carries a
///     stable code, structured context, and generated documentation — exactly what the library asks of application
///     errors. Factories only assemble <see cref="Error" /> instances from already-computed facts; they never touch
///     the file system or spawn processes, so the documentation examples stay side-effect free.
/// </remarks>
[ProvidesErrorsFor("DocumentationRequest",
                   Description = "Validation of a documentation-generation request: the solution, assemblies, worker path and opt-in markers it designates.")]
internal static class DocumentationRequestError {

    #region Statics members declarations

    /// <summary>The solution file designated by the request does not exist on disk.</summary>
    [DocumentedBy(nameof(SolutionNotFoundDocumentation))]
    internal static PrimaryPortError SolutionNotFound(string solutionPath) {
        return PrimaryPortError.Create(
                                   Code.SolutionNotFound,
                                   $"Solution file not found: '{solutionPath}'.",
                                   Transience.NonTransient,
                                   ctx => ctx.Add(ErrCtxKey.SolutionPath, solutionPath))
                               .WithPublicMessage(
                                   "The solution file was not found.",
                                   "The path passed to the documentation generator does not point to an existing solution file.");
    }

    /// <summary>The request designates a path that is not a supported solution format (.sln or .slnx).</summary>
    [DocumentedBy(nameof(SolutionPathUnsupportedDocumentation))]
    internal static PrimaryPortError SolutionPathUnsupported(string solutionPath) {
        return PrimaryPortError.Create(
                                   Code.SolutionPathUnsupported,
                                   $"Expected a .sln or .slnx file path, got: '{solutionPath}'.",
                                   Transience.NonTransient,
                                   ctx => ctx.Add(ErrCtxKey.SolutionPath, solutionPath))
                               .WithPublicMessage(
                                   "The solution path is not supported.",
                                   "The documentation generator accepts .sln and .slnx solution files.");
    }

    /// <summary>An assembly explicitly designated by the request does not exist on disk.</summary>
    [DocumentedBy(nameof(AssemblyNotFoundDocumentation))]
    internal static PrimaryPortError AssemblyNotFound(string assemblyPath) {
        return PrimaryPortError.Create(
                                   Code.AssemblyNotFound,
                                   $"Assembly not found: '{assemblyPath}'.",
                                   Transience.NonTransient,
                                   ctx => ctx.Add(ErrCtxKey.AssemblyPath, assemblyPath))
                               .WithPublicMessage(
                                   "A requested assembly was not found.",
                                   "One of the assemblies passed to the documentation generator does not exist on disk.");
    }

    /// <summary>The build output resolved for a project does not exist on disk.</summary>
    [DocumentedBy(nameof(TargetAssemblyNotFoundDocumentation))]
    internal static PrimaryPortError TargetAssemblyNotFound(string projectPath, string targetPath) {
        return PrimaryPortError.Create(
                                   Code.TargetAssemblyNotFound,
                                   $"Target assembly not found for project '{projectPath}'. Resolved TargetPath='{targetPath}'.",
                                   Transience.NonTransient,
                                   ctx => {
                                       ctx.Add(ErrCtxKey.ProjectPath, projectPath);
                                       ctx.Add(ErrCtxKey.TargetPath, targetPath);
                                   })
                               .WithPublicMessage(
                                   "A project's build output was not found.",
                                   "The project's resolved target assembly does not exist on disk.");
    }

    /// <summary>
    ///     The opt-in property of a project is declared in a way whose effective value cannot be determined from the raw
    ///     project XML (defined more than once, or gated behind an MSBuild Condition).
    /// </summary>
    [DocumentedBy(nameof(OptInAmbiguousDocumentation))]
    internal static PrimaryPortError OptInAmbiguous(string projectPath, string optInPropertyName, string ambiguityReason) {
        return PrimaryPortError.Create(
                                   Code.OptInAmbiguous,
                                   $"Cannot determine the opt-in for project '{projectPath}': the '{optInPropertyName}' property is " +
                                   $"{ambiguityReason} in the project XML, which GenDoc reads without MSBuild evaluation. Declare it once, " +
                                   "literally and unconditionally in the project file, or document the built assembly explicitly.",
                                   Transience.NonTransient,
                                   ctx => {
                                       ctx.Add(ErrCtxKey.ProjectPath, projectPath);
                                       ctx.Add(ErrCtxKey.OptInProperty, optInPropertyName);
                                       ctx.Add(ErrCtxKey.AmbiguityReason, ambiguityReason);
                                   })
                               .WithPublicMessage(
                                   "A project's documentation opt-in is ambiguous.",
                                   "Declare the opt-in property once, literally and unconditionally, in the project file — or document the assembly explicitly.");
    }

    /// <summary>The worker path explicitly configured on the request does not point to an existing file.</summary>
    [DocumentedBy(nameof(WorkerPathInvalidDocumentation))]
    internal static PrimaryPortError WorkerPathInvalid(string workerPath) {
        return PrimaryPortError.Create(
                                   Code.WorkerPathInvalid,
                                   $"The configured documentation worker was not found at '{workerPath}'.",
                                   Transience.NonTransient,
                                   ctx => ctx.Add(ErrCtxKey.WorkerPath, workerPath))
                               .WithPublicMessage(
                                   "The configured documentation worker was not found.",
                                   "The configured worker path must point to an existing FirstClassErrors.GenDoc.Worker.dll.");
    }

    private static ErrorDocumentation SolutionNotFoundDocumentation() {
        return DescribeError.WithTitle("Solution file not found")
                            .WithDescription("A documentation-generation request designates a solution file that does not exist on disk. The full path, as resolved by the generator, is carried in the error context.")
                            .WithRule("A generation request must designate an existing solution file.")
                            .WithDiagnostic("The path is wrong or relative to an unexpected working directory (a CI step often runs from a different directory than a developer shell).",
                                            ErrorOrigin.External,
                                            "Compare the resolved path in the error context with the actual solution location; check the working directory of the caller.")
                            .WithExamples(() => SolutionNotFound("/src/app/Application.sln"));
    }

    private static ErrorDocumentation SolutionPathUnsupportedDocumentation() {
        return DescribeError.WithTitle("Solution path not supported")
                            .WithDescription("A documentation-generation request designates a file that is not a .sln or .slnx solution. Solution filters (.slnf) are deliberately not supported: the 'dotnet sln' commands the generator relies on do not process them.")
                            .WithRule("A generation request must designate a .sln or .slnx solution file.")
                            .WithDiagnostic("A project file, a solution filter (.slnf), or another artifact was passed instead of the solution.",
                                            ErrorOrigin.External,
                                            "Check the path in the error context; pass the .sln/.slnx file, or document built assemblies directly instead.")
                            .WithExamples(() => SolutionPathUnsupported("/src/app/Application.slnf"));
    }

    private static ErrorDocumentation AssemblyNotFoundDocumentation() {
        return DescribeError.WithTitle("Requested assembly not found")
                            .WithDescription("A documentation-generation request explicitly designates an assembly that does not exist on disk. The full path, as resolved by the generator, is carried in the error context.")
                            .WithRule("Every assembly explicitly designated by a generation request must exist on disk.")
                            .WithDiagnostic("The assembly was never built, or was built to a different configuration or target framework than the path assumes.",
                                            ErrorOrigin.External,
                                            "Build the project first and compare the path in the error context with the actual build output directory.")
                            .WithExamples(() => AssemblyNotFound("/src/app/bin/Release/net8.0/Application.dll"));
    }

    private static ErrorDocumentation TargetAssemblyNotFoundDocumentation() {
        return DescribeError.WithTitle("Project build output not found")
                            .WithDescription("The generator resolved a project's build output path (MSBuild TargetPath), but no assembly exists there. Both the project path and the resolved target path are carried in the error context.")
                            .WithRule("Every documented project must have been built for the requested configuration and target framework.")
                            .WithDiagnostic("The solution was not built before generation (for example the build step was skipped), so the output is missing.",
                                            ErrorOrigin.External,
                                            "Build the solution first, or let the generator build it by enabling its build step.")
                            .AndDiagnostic("The generation request targets a different configuration or framework than the one that was built.",
                                           ErrorOrigin.External,
                                           "Compare the resolved TargetPath in the error context with the directory that was actually built.")
                            .WithExamples(() => TargetAssemblyNotFound("/src/app/Application/Application.csproj", "/src/app/Application/bin/Release/net8.0/Application.dll"));
    }

    private static ErrorDocumentation OptInAmbiguousDocumentation() {
        return DescribeError.WithTitle("Documentation opt-in ambiguous")
                            .WithDescription("The generator reads the documentation opt-in property literally from the project XML, without MSBuild evaluation. When the property is defined more than once, or gated behind an MSBuild Condition (directly or via a Choose/When branch), its effective value cannot be known, so the generator refuses to guess and skips the project. The project path, the property name, and the reason are carried in the error context.")
                            .WithRule("The opt-in property must be declared at most once, literally and unconditionally, in each project file.")
                            .WithDiagnostic("The property is declared in several PropertyGroups, or under a Condition attribute or a Choose/When branch.",
                                            ErrorOrigin.External,
                                            "Inspect the project file named in the error context; keep a single unconditional declaration, or document the built assembly explicitly instead.")
                            .WithExamples(() => OptInAmbiguous("/src/app/Application/Application.csproj", "GenerateErrorDocumentation", "defined 2 times"));
    }

    private static ErrorDocumentation WorkerPathInvalidDocumentation() {
        return DescribeError.WithTitle("Configured worker path invalid")
                            .WithDescription("A documentation-generation request explicitly configures the path of the extraction worker, but no file exists there. The configured path is carried in the error context.")
                            .WithRule("A configured worker path must point to an existing FirstClassErrors.GenDoc.Worker.dll.")
                            .WithDiagnostic("The configured path is stale — the worker was moved, or the path was written for another machine or installation layout.",
                                            ErrorOrigin.External,
                                            "Check the path in the error context; remove the explicit setting to fall back to the worker deployed next to the tool.")
                            .WithExamples(() => WorkerPathInvalid("/tools/fce/FirstClassErrors.GenDoc.Worker.dll"));
    }

    #endregion

    #region Nested types declarations

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" members",
        Justification = "Each error-code constant deliberately mirrors the name of its factory method; both are always qualified (Code.X versus the X(...) call), so there is no real ambiguity.")]
    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode SolutionNotFound        = ErrorCode.Create("GENDOC_SOLUTION_NOT_FOUND");
        public static readonly ErrorCode SolutionPathUnsupported = ErrorCode.Create("GENDOC_SOLUTION_PATH_UNSUPPORTED");
        public static readonly ErrorCode AssemblyNotFound        = ErrorCode.Create("GENDOC_ASSEMBLY_NOT_FOUND");
        public static readonly ErrorCode TargetAssemblyNotFound  = ErrorCode.Create("GENDOC_TARGET_ASSEMBLY_NOT_FOUND");
        public static readonly ErrorCode OptInAmbiguous          = ErrorCode.Create("GENDOC_OPT_IN_AMBIGUOUS");
        public static readonly ErrorCode WorkerPathInvalid       = ErrorCode.Create("GENDOC_WORKER_PATH_INVALID");

        #endregion

    }

    #endregion

}
