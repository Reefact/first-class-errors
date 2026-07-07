#region Usings declarations

using System.Text.Json;
using System.Text.Json.Serialization;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Reads and writes the CLI configuration file (<c>fce.json</c>). The file is human-editable; the
///     <c>fce config</c> commands are conveniences over the same file.
/// </summary>
internal static class ConfigurationStore {

    #region Statics members declarations

    public const string DefaultFileName = "fce.json";

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        WriteIndented               = true,
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Resolves the configuration path: the given path, or <c>fce.json</c> in the current directory.</summary>
    public static string Resolve(string? configPath) {
        return string.IsNullOrWhiteSpace(configPath)
                   ? Path.Combine(Directory.GetCurrentDirectory(), DefaultFileName)
                   : Path.GetFullPath(configPath);
    }

    /// <summary>Determines whether a configuration file exists at the given (already resolved) path.</summary>
    public static bool Exists(string path) {
        return File.Exists(path);
    }

    /// <summary>Loads the configuration, returning an empty one when the file is missing or blank.</summary>
    public static CliConfiguration Load(string path) {
        if (File.Exists(path) is false) { return new CliConfiguration(); }

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) { return new CliConfiguration(); }

        CliConfiguration configuration = JsonSerializer.Deserialize<CliConfiguration>(json, SerializerOptions) ?? new CliConfiguration();

        // A hand-edited "renderers": null would deserialize to a null list; keep the never-null invariant.
        if (configuration.Renderers is null) { configuration.Renderers = []; }

        return configuration;
    }

    /// <summary>
    ///     Writes the configuration to the given path (creating parent directories as needed). The write is atomic:
    ///     the JSON is written to a temporary file in the same directory and then renamed over the target, so a crash
    ///     mid-write leaves the previous <c>fce.json</c> intact rather than a truncated one.
    /// </summary>
    public static void Save(string path, CliConfiguration configuration) {
        string? directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory) is false) { Directory.CreateDirectory(directory); }

        string json     = JsonSerializer.Serialize(configuration, SerializerOptions);
        string tempPath = path + ".tmp";

        // Write the whole payload to a sibling temp file first, then rename it over the target. The rename stays on a
        // single volume, so a reader only ever sees either the old file or the fully written new one, never a partial write.
        File.WriteAllText(tempPath, json);

        try {
            File.Move(tempPath, path, overwrite: true);
        } catch {
            TryDelete(tempPath);

            throw;
        }
    }

    /// <summary>Best-effort deletion used to clean up a temporary file; a leftover temp is harmless and reused.</summary>
    private static void TryDelete(string path) {
        try {
            File.Delete(path);
        } catch {
            // Ignore: cleanup is best-effort, and the next Save overwrites the temp file anyway.
        }
    }

    #endregion

}
