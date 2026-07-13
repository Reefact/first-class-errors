#region Usings declarations

using FirstClassErrors.GenDoc.Versioning;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Reads and writes the catalog baseline file (<c>errors-baseline.json</c> by default) — the committed snapshot
///     of the error contract that the <c>catalog</c> commands compare against.
/// </summary>
internal static class BaselineStore {

    #region Statics members declarations

    public const string DefaultFileName = "errors-baseline.json";

    /// <summary>
    ///     Resolves the baseline path with the same portability contract as the rest of the configuration: a
    ///     command-line value wins and is resolved against the current directory (the caller's vantage point); a value
    ///     from <c>fce.json</c> is resolved against the configuration file's directory, so a configuration checked in
    ///     under, say, <c>ci/fce.json</c> keeps pointing at <c>ci/errors-baseline.json</c> wherever it is invoked from.
    ///     With neither, <see cref="DefaultFileName" /> in the current directory is used.
    /// </summary>
    /// <param name="optionPath">The <c>--baseline</c> command-line value, if any.</param>
    /// <param name="configuredPath">The <c>baseline</c> value from the configuration file, if any.</param>
    /// <param name="configDirectory">The directory containing the configuration file, configured paths resolve against it.</param>
    public static string Resolve(string? optionPath, string? configuredPath, string configDirectory) {
        return ConfigRelativePath.Resolve(optionPath, configuredPath, configDirectory)
            ?? Path.GetFullPath(DefaultFileName);
    }

    /// <summary>Determines whether a baseline exists at the given (already resolved) path.</summary>
    public static bool Exists(string path) {
        return File.Exists(path);
    }

    /// <summary>Loads and validates the baseline at the given path.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the file is not a valid snapshot.</exception>
    public static CatalogSnapshot Load(string path) {
        return CatalogSnapshotSerializer.Deserialize(File.ReadAllText(path));
    }

    /// <summary>Writes the snapshot to the given path in its canonical form (creating parent directories as needed).</summary>
    public static void Save(string path, CatalogSnapshot snapshot) {
        string? directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory) is false) { Directory.CreateDirectory(directory); }

        File.WriteAllText(path, CatalogSnapshotSerializer.Serialize(snapshot));
    }

    #endregion

}
