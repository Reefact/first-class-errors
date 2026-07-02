#region Usings declarations

using System.Text.Json;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Reads and writes the CLI configuration file (<c>fce.json</c>). The file is human-editable; the
///     <c>fce init</c> and <c>fce renderer</c> commands are conveniences over the same file.
/// </summary>
internal static class ConfigurationStore {

    #region Statics members declarations

    public const string DefaultFileName = "fce.json";

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        WriteIndented          = true,
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
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
    public static RendererConfiguration Load(string path) {
        if (File.Exists(path) is false) { return new RendererConfiguration(); }

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) { return new RendererConfiguration(); }

        return JsonSerializer.Deserialize<RendererConfiguration>(json, SerializerOptions) ?? new RendererConfiguration();
    }

    /// <summary>Writes the configuration to the given path (creating parent directories as needed).</summary>
    public static void Save(string path, RendererConfiguration configuration) {
        string? directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory) is false) { Directory.CreateDirectory(directory); }

        File.WriteAllText(path, JsonSerializer.Serialize(configuration, SerializerOptions));
    }

    #endregion

}
