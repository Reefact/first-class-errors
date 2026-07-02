#region Usings declarations

using System.Globalization;

#endregion

namespace FirstClassErrors.GenDoc;

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

    /// <summary>
    ///     Absolute path to the documentation worker assembly (<c>FirstClassErrors.GenDoc.Worker.dll</c>). When
    ///     <c>null</c>, the worker is located next to the running tool (<see cref="AppContext.BaseDirectory" />).
    /// </summary>
    public string? WorkerAssemblyPath { get; init; }

    /// <summary>
    ///     Maximum time to wait for a single worker process (one per documented assembly) before it is killed and
    ///     treated as a failure. Defaults to two minutes.
    /// </summary>
    public TimeSpan WorkerTimeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     The culture the extraction runs under, so documentation factories that read localized resources produce
    ///     their text in that language. When <c>null</c>, the worker keeps its ambient culture.
    /// </summary>
    public CultureInfo? Culture { get; init; }

    public IGenerationLogger Logger { get; init; } = new NullGenerationLogger();

}