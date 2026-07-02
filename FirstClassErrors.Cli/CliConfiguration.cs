namespace FirstClassErrors.Cli;

/// <summary>
///     The persisted CLI configuration (<c>fce.json</c>). It provides defaults for the <c>generate</c> command — so
///     common options need not be repeated on every run — and the list of custom renderer libraries to load.
/// </summary>
/// <remarks>
///     Every scalar is optional. A value passed on the command line overrides the corresponding configuration value;
///     the configuration in turn overrides the built-in default. Paths are absolute or relative to the configuration
///     file, so a configuration is portable with its plugins.
/// </remarks>
internal sealed class CliConfiguration {

    /// <summary>Default solution to document (used when neither --solution nor --assemblies is given).</summary>
    public string? Solution { get; set; }

    /// <summary>Default assemblies to document (used when neither --solution nor --assemblies is given).</summary>
    public List<string>? Assemblies { get; set; }

    /// <summary>Default output format (e.g. <c>json</c>, <c>markdown</c>).</summary>
    public string? Format { get; set; }

    /// <summary>Default Markdown layout (<c>single</c> or <c>split</c>).</summary>
    public string? Layout { get; set; }

    /// <summary>Default output file or directory.</summary>
    public string? Output { get; set; }

    /// <summary>Default build configuration used when building a solution.</summary>
    public string? Configuration { get; set; }

    /// <summary>Default target framework restriction for a multi-target solution.</summary>
    public string? Framework { get; set; }

    /// <summary>When <c>true</c>, do not build the solution (document the existing binaries).</summary>
    public bool? NoBuild { get; set; }

    /// <summary>When <c>true</c>, abort on the first extraction failure.</summary>
    public bool? Strict { get; set; }

    /// <summary>Path to the documentation worker assembly.</summary>
    public string? Worker { get; set; }

    /// <summary>Paths of the custom renderer assemblies to load.</summary>
    public List<string> Renderers { get; set; } = [];

}
