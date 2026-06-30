namespace DiagnosableExceptions.GenDoc;

public sealed class SolutionGenerationOptions {

    public bool            BuildSolution   { get; init; } = true;
    public string          Configuration   { get; init; } = "Debug";
    public string?         TargetFramework { get; init; }
    public FailureBehavior FailureBehavior { get; init; } = FailureBehavior.Stop;

    /// <summary>
    ///     If true, projects without explicit GenerateErrorDocumentation=true will be included.
    ///     Default: false (opt-in).
    /// </summary>
    public bool IncludeProjectsWithoutOptIn { get; init; } = false;

    /// <summary>
    ///     MSBuild property name used for opt-in.
    ///     Default: "GenerateErrorDocumentation"
    /// </summary>
    public string OptInPropertyName { get; init; } = "GenerateErrorDocumentation";

    /// <summary>
    ///     Additional arguments appended verbatim to the "dotnet build" command line (e.g. "--no-restore").
    ///     Defaults to "--nologo".
    /// </summary>
    public string? DotNetBuildAdditionalArguments { get; init; } = "--nologo";

    public IGenerationLogger Logger { get; init; } = new NullGenerationLogger();

}