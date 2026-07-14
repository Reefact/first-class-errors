namespace FirstClassErrors.Cli;

/// <summary>
///     Resolves the effective documentation source — a solution or a set of assemblies — from the command line and
///     the configuration file, applying the same precedence rules for every command that extracts a catalog
///     (<c>generate</c>, <c>catalog update</c>, <c>catalog diff</c>).
/// </summary>
internal static class CatalogSourceResolver {

    #region Statics members declarations

    /// <summary>
    ///     Resolves the effective source. A command-line source overrides the configured one wholesale, so the two
    ///     are never mixed (passing <c>--assemblies</c> does not combine with a configured <c>solution</c>).
    /// </summary>
    /// <param name="solutionOption">The <c>--solution</c> value, if any.</param>
    /// <param name="assemblyOptions">The <c>--assemblies</c> values, possibly empty.</param>
    /// <param name="configuration">The loaded configuration file.</param>
    /// <returns>The effective solution path (or <c>null</c>) and the effective assembly paths (possibly empty).</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when both a solution and assemblies are specified, or when no source is specified at all.
    /// </exception>
    public static (string? Solution, string[] Assemblies) Resolve(string? solutionOption, IReadOnlyList<string> assemblyOptions, CliConfiguration configuration) {
        string?  solution;
        string[] assemblies;
        if (string.IsNullOrWhiteSpace(solutionOption) is false || assemblyOptions.Count > 0) {
            solution   = solutionOption;
            assemblies = assemblyOptions.ToArray();
        } else {
            solution   = configuration.Solution;
            assemblies = configuration.Assemblies?.ToArray() ?? [];
        }

        bool hasSolution   = string.IsNullOrWhiteSpace(solution) is false;
        bool hasAssemblies = assemblies.Length > 0;
        if (hasSolution && hasAssemblies) {
            throw new InvalidOperationException("Specify either a solution or assemblies, not both.");
        }

        if (hasSolution is false && hasAssemblies is false) {
            throw new InvalidOperationException("No source: pass --solution/--assemblies, or set 'solution'/'assemblies' in the configuration.");
        }

        // Normalize a whitespace-only solution to null, so callers can dispatch on `solution is not null` and never
        // route an empty string into the solution path when assemblies were the effective source.
        return (hasSolution ? solution : null, assemblies);
    }

    /// <summary>
    ///     Returns the first value that is not <c>null</c>, empty or whitespace — the "command line first, then
    ///     configuration" precedence applied to a single option.
    /// </summary>
    public static string? FirstNonEmpty(string? primary, string? fallback) {
        if (string.IsNullOrWhiteSpace(primary) is false) { return primary; }
        if (string.IsNullOrWhiteSpace(fallback) is false) { return fallback; }

        return null;
    }

    #endregion

}
