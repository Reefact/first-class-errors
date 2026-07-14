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

    /// <summary>Default output format (<c>json</c>, <c>markdown</c> or <c>html</c>).</summary>
    public string? Format { get; set; }

    /// <summary>Default document layout (<c>single</c> or <c>split</c>); applies to the <c>markdown</c> and <c>html</c> formats.</summary>
    public string? Layout { get; set; }

    /// <summary>Default language of the generated documentation (e.g. <c>en</c>, <c>fr</c>, <c>es</c>, <c>de</c>, <c>sv</c>).</summary>
    public string? Language { get; set; }

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

    /// <summary>
    ///     Default service name used to build the RFC 9457 problem <c>type</c> of the rendered examples
    ///     (<c>urn:problem:{service}:{code}</c>). Required by the <c>markdown</c> and <c>html</c> formats;
    ///     <c>fce generate</c> fails with a clear message when it is missing.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>Default path of the catalog baseline file used by the <c>catalog</c> commands.</summary>
    public string? Baseline { get; set; }

    /// <summary>Default path where <c>generate</c> also writes the canonical catalog snapshot.</summary>
    public string? Snapshot { get; set; }

    /// <summary>Paths of the custom renderer assemblies to load.</summary>
    public List<string> Renderers { get; set; } = [];

}
