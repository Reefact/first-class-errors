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

    /// <summary>Writes the configuration to the given path (creating parent directories as needed).</summary>
    public static void Save(string path, CliConfiguration configuration) {
        string? directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory) is false) { Directory.CreateDirectory(directory); }

        File.WriteAllText(path, JsonSerializer.Serialize(configuration, SerializerOptions));
    }

    #endregion

}
