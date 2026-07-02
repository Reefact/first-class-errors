#region Usings declarations

using System.ComponentModel;

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Options for the <c>generate</c> command. Every value is optional here: when omitted, the command falls back
///     to the configuration file (<c>fce.json</c>) and then to a built-in default. Spectre.Console.Cli binds the
///     command line to these properties and generates the <c>--help</c> output from the attributes.
/// </summary>
internal sealed class GenerateSettings : ConfigScopedSettings {

    [CommandOption("-s|--solution <PATH>")]
    [Description("Path to the .sln file to document (overrides 'solution' in the configuration).")]
    public string? SolutionPath { get; set; }

    [CommandOption("-a|--assemblies <PATH>")]
    [Description("A built assembly (.dll) to document; repeat to document several (overrides 'assemblies').")]
    public string[] AssemblyPaths { get; set; } = [];

    [CommandOption("-o|--output <PATH>")]
    [Description("Write the rendered document to this file or directory (falls back to the configuration, then standard output).")]
    public string? OutputPath { get; set; }

    [CommandOption("-f|--format <FORMAT>")]
    [Description("Output format: json or markdown (alias: md). Falls back to the configuration, then json.")]
    public string? Format { get; set; }

    [CommandOption("--layout <LAYOUT>")]
    [Description("Markdown layout: single or split. Falls back to the configuration, then single.")]
    public string? Layout { get; set; }

    [CommandOption("-c|--configuration <NAME>")]
    [Description("Build configuration used when building a solution. Falls back to the configuration, then Debug.")]
    public string? Configuration { get; set; }

    [CommandOption("--framework <TFM>")]
    [Description("Restrict a multi-target solution to a single target framework.")]
    public string? Framework { get; set; }

    [CommandOption("--no-build")]
    [Description("Do not build the solution; document the existing binaries.")]
    public bool NoBuild { get; set; }

    [CommandOption("--strict")]
    [Description("Abort on the first extraction failure (default: continue and report).")]
    public bool Strict { get; set; }

    [CommandOption("--worker <PATH>")]
    [Description("Path to FirstClassErrors.GenDoc.Worker.dll (default: next to this tool).")]
    public string? WorkerPath { get; set; }

    [CommandOption("--verbose")] // Long-only to stay clear of Spectre's built-in --version/-h help options.
    [Description("Emit diagnostic logging to standard error.")]
    public bool Verbose { get; set; }

}
