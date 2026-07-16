namespace FirstClassErrors.GenDoc;

/// <summary>
///     Context keys carried by the errors GenDoc raises about its own pipeline (see
///     <see cref="DocumentationRequestError" /> and <see cref="DocumentationToolchainError" />). Each key names one
///     fact of the failing occurrence, so the failing element is identifiable without parsing messages. The value
///     types below all stringify culture-invariantly (string, int, TimeSpan), so the rendered context stays
///     deterministic.
/// </summary>
internal static class ErrCtxKey {

    #region Statics members declarations

    public static readonly ErrorContextKey<string> SolutionPath =
        ErrorContextKey.Create<string>("SolutionPath", "Full path of the solution file the generation request designates.");

    public static readonly ErrorContextKey<string> ProjectPath =
        ErrorContextKey.Create<string>("ProjectPath", "Full path of the project file being processed.");

    public static readonly ErrorContextKey<string> AssemblyPath =
        ErrorContextKey.Create<string>("AssemblyPath", "Full path of the assembly being documented.");

    public static readonly ErrorContextKey<string> TargetPath =
        ErrorContextKey.Create<string>("TargetPath", "Build output path resolved for the project (MSBuild TargetPath).");

    public static readonly ErrorContextKey<string> WorkerPath =
        ErrorContextKey.Create<string>("WorkerPath", "Configured path of the documentation worker assembly.");

    public static readonly ErrorContextKey<string> ProbedDirectory =
        ErrorContextKey.Create<string>("ProbedDirectory", "Directory probed for the documentation worker assembly.");

    public static readonly ErrorContextKey<string> OptInProperty =
        ErrorContextKey.Create<string>("OptInProperty", "Name of the MSBuild property read as the documentation opt-in marker.");

    public static readonly ErrorContextKey<string> AmbiguityReason =
        ErrorContextKey.Create<string>("AmbiguityReason", "Why the opt-in property's effective value cannot be determined from the project XML.");

    public static readonly ErrorContextKey<string> ProcessFileName =
        ErrorContextKey.Create<string>("ProcessFileName", "Executable name of the child process the generator tried to run.");

    public static readonly ErrorContextKey<string> Command =
        ErrorContextKey.Create<string>("Command", "Short description of the child-process command that was running.");

    public static readonly ErrorContextKey<string> Target =
        ErrorContextKey.Create<string>("Target", "Path of the solution, project or assembly the timed-out command was operating on.");

    public static readonly ErrorContextKey<int> ExitCode =
        ErrorContextKey.Create<int>("ExitCode", "Exit code returned by the child process.");

    public static readonly ErrorContextKey<TimeSpan> Timeout =
        ErrorContextKey.Create<TimeSpan>("Timeout", "Configured timeout the child process exceeded.");

    #endregion

}
