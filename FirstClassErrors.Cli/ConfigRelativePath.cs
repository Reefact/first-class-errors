namespace FirstClassErrors.Cli;

/// <summary>
///     Resolves an optional path with the repository's portability contract, shared by every option that can be set
///     both on the command line and in <c>fce.json</c> (the catalog baseline, the generate snapshot): a command-line
///     value wins and is resolved against the current directory — the caller's vantage point — while a value read
///     from the configuration file is resolved against the configuration file's directory, so a configuration checked
///     in under, say, <c>ci/fce.json</c> keeps pointing at <c>ci/…</c> wherever the tool is invoked from. This mirrors
///     how renderer references resolve.
/// </summary>
internal static class ConfigRelativePath {

    #region Statics members declarations

    /// <summary>
    ///     Resolves the effective path, or returns <c>null</c> when neither a command-line nor a configured value is
    ///     provided.
    /// </summary>
    /// <param name="optionPath">The command-line value, if any (resolved against the current directory).</param>
    /// <param name="configuredPath">The value from the configuration file, if any (resolved against <paramref name="configDirectory" />).</param>
    /// <param name="configDirectory">The directory containing the configuration file.</param>
    public static string? Resolve(string? optionPath, string? configuredPath, string configDirectory) {
        if (string.IsNullOrWhiteSpace(optionPath) is false) {
            return Path.GetFullPath(optionPath!);
        }

        if (string.IsNullOrWhiteSpace(configuredPath) is false) {
            return Path.IsPathRooted(configuredPath)
                       ? Path.GetFullPath(configuredPath!)
                       : Path.GetFullPath(Path.Combine(configDirectory, configuredPath!));
        }

        return null;
    }

    #endregion

}
