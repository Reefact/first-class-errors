#region Usings declarations

using System.ComponentModel;

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Base settings for every command that reads or writes the configuration file.
/// </summary>
internal class ConfigScopedSettings : CommandSettings {

    [CommandOption("--config <PATH>")]
    [Description("Path to the configuration file (default: fce.json in the current directory).")]
    public string? ConfigPath { get; set; }

}
