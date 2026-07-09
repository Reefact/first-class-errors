namespace FirstClassErrors.Cli;

/// <summary>
///     The effective generate options once precedence has been applied — command line first, then the configuration
///     file, then the built-in default — and the source has been selected. It carries only resolved values; culture
///     parsing, renderer resolution, validation and the construction of the generation options stay in the command,
///     which owns their ordering and error reporting.
/// </summary>
internal sealed record ResolvedGenerateOptions(
    string?               Solution,
    IReadOnlyList<string> Assemblies,
    bool                  HasSolution,
    bool                  HasAssemblies,
    string                Format,
    string                Layout,
    string?               Output,
    string?               ServiceName,
    string                Language,
    string                BuildConfiguration,
    string?               Framework,
    string?               WorkerPath,
    bool                  NoBuild,
    bool                  Strict);

/// <summary>
///     Resolves the effective <see cref="ResolvedGenerateOptions" /> for a run: a command-line value overrides the
///     corresponding configuration value, which overrides the built-in default. A command-line source (a solution or
///     assemblies) replaces the configured one wholesale, so the two are never mixed.
/// </summary>
internal static class GenerateOptionsResolver {

    #region Statics members declarations

    public static ResolvedGenerateOptions Resolve(GenerateSettings settings, CliConfiguration configuration) {
        // Effective source: a command-line source overrides the configured one wholesale (passing --assemblies does not
        // combine with a configured 'solution').
        string?  solution;
        string[] assemblies;
        if (string.IsNullOrWhiteSpace(settings.SolutionPath) is false || settings.AssemblyPaths.Length > 0) {
            solution   = settings.SolutionPath;
            assemblies = settings.AssemblyPaths;
        } else {
            solution   = configuration.Solution;
            assemblies = configuration.Assemblies?.ToArray() ?? [];
        }

        return new ResolvedGenerateOptions(
            Solution:           solution,
            Assemblies:         assemblies,
            HasSolution:        string.IsNullOrWhiteSpace(solution) is false,
            HasAssemblies:      assemblies.Length > 0,
            Format:             NormalizeFormat(FirstNonEmpty(settings.Format, configuration.Format) ?? "json"),
            Layout:             (FirstNonEmpty(settings.Layout, configuration.Layout) ?? "single").Trim().ToLowerInvariant(),
            Output:             FirstNonEmpty(settings.OutputPath, configuration.Output),
            ServiceName:        FirstNonEmpty(settings.ServiceName, configuration.ServiceName),
            Language:           FirstNonEmpty(settings.Language, configuration.Language) ?? "en",
            BuildConfiguration: FirstNonEmpty(settings.Configuration, configuration.Configuration) ?? "Debug",
            Framework:          FirstNonEmpty(settings.Framework, configuration.Framework),
            WorkerPath:         FirstNonEmpty(settings.WorkerPath, configuration.Worker),
            NoBuild:            settings.NoBuild || (configuration.NoBuild ?? false),
            Strict:             settings.Strict  || (configuration.Strict ?? false));
    }

    internal static string? FirstNonEmpty(string? primary, string? fallback) {
        if (string.IsNullOrWhiteSpace(primary) is false) { return primary; }
        if (string.IsNullOrWhiteSpace(fallback) is false) { return fallback; }

        return null;
    }

    internal static string NormalizeFormat(string format) {
        string normalized = format.Trim().ToLowerInvariant();

        return normalized == "md" ? "markdown" : normalized;
    }

    #endregion

}
