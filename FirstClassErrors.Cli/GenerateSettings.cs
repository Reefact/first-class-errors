#region Usings declarations

using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Options for the documentation generation command. Spectre.Console.Cli binds the command line to these
///     properties and generates the <c>--help</c> output from the attributes below.
/// </summary>
internal sealed class GenerateSettings : CommandSettings {

    [CommandOption("-s|--solution <PATH>")]
    [Description("Path to the .sln file to document.")]
    public string? SolutionPath { get; set; }

    [CommandOption("-a|--assemblies <PATH>")]
    [Description("A built assembly (.dll) to document. Repeat the option to document several.")]
    public string[] AssemblyPaths { get; set; } = [];

    [CommandOption("-o|--output <PATH>")]
    [Description("Write the rendered document to this file (default: standard output).")]
    public string? OutputPath { get; set; }

    [CommandOption("-f|--format <FORMAT>")]
    [Description("Output format. Supported: json.")]
    [DefaultValue("json")]
    public string Format { get; set; } = "json";

    [CommandOption("-c|--configuration <NAME>")]
    [Description("Build configuration used when building a solution.")]
    [DefaultValue("Debug")]
    public string Configuration { get; set; } = "Debug";

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

    /// <summary>
    ///     Cross-option rules enforced before the command runs: exactly one source (a solution or one/more
    ///     assemblies) and a supported output format.
    /// </summary>
    public override ValidationResult Validate() {
        bool hasSolution   = string.IsNullOrWhiteSpace(SolutionPath) is false;
        bool hasAssemblies = AssemblyPaths.Length > 0;

        if (hasSolution && hasAssemblies) {
            return ValidationResult.Error("Specify either --solution or --assemblies, not both.");
        }

        if (hasSolution is false && hasAssemblies is false) {
            return ValidationResult.Error("A source is required: pass --solution <path> or --assemblies <path> (repeatable).");
        }

        if (string.Equals(Format, "json", StringComparison.OrdinalIgnoreCase) is false) {
            return ValidationResult.Error($"Unsupported --format '{Format}'. Supported formats: json.");
        }

        return ValidationResult.Success();
    }

}
