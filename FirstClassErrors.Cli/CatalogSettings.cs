#region Usings declarations

using System.ComponentModel;

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Common options for the <c>catalog</c> commands (<c>update</c>, <c>diff</c>): the documentation source and
///     build options shared with <c>generate</c>, plus the location of the baseline file. Every value is optional:
///     when omitted, the command falls back to the configuration file (<c>fce.json</c>) and then to a built-in
///     default.
/// </summary>
/// <remarks>
///     The catalog commands have no <c>--format</c>, <c>--layout</c> or <c>--language</c>: they operate on the
///     canonical contract snapshot, which is renderer-independent and always extracted under the <c>en</c> culture so
///     the baseline does not depend on the machine that produces it.
/// </remarks>
internal class CatalogSettings : ConfigScopedSettings {

    [CommandOption("-s|--solution <PATH>")]
    [Description("Path to the .sln file to snapshot (overrides 'solution' in the configuration).")]
    public string? SolutionPath { get; set; }

    [CommandOption("-a|--assemblies <PATH>")]
    [Description("A built assembly (.dll) to snapshot; repeat to snapshot several (overrides 'assemblies').")]
    public string[] AssemblyPaths { get; set; } = [];

    [CommandOption("--baseline <PATH>")]
    [Description("Path of the baseline file (falls back to 'baseline' in the configuration, then errors-baseline.json).")]
    public string? BaselinePath { get; set; }

    [CommandOption("-c|--configuration <NAME>")]
    [Description("Build configuration used when building a solution. Falls back to the configuration, then Debug.")]
    public string? Configuration { get; set; }

    [CommandOption("--framework <TFM>")]
    [Description("Restrict a multi-target solution to a single target framework.")]
    public string? Framework { get; set; }

    [CommandOption("--no-build")]
    [Description("Do not build the solution; snapshot the existing binaries.")]
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
