namespace FirstClassErrors.Cli;

/// <summary>
///     The persisted CLI configuration. Currently it holds the list of custom renderer libraries the tool should
///     load, referenced by path (absolute, or relative to the configuration file).
/// </summary>
internal sealed class RendererConfiguration {

    /// <summary>
    ///     Gets or sets the paths of the renderer assemblies to load. Relative paths are resolved against the folder
    ///     containing the configuration file, so a configuration is portable with its plugins.
    /// </summary>
    public List<string> Renderers { get; set; } = [];

}
