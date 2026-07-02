#region Usings declarations

using System.ComponentModel;

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>Settings for <c>fce init</c>.</summary>
internal sealed class InitSettings : ConfigScopedSettings {

    [CommandOption("--force")]
    [Description("Overwrite an existing configuration file.")]
    public bool Force { get; set; }

}

/// <summary>
///     Creates a configuration file (<c>fce.json</c>) with an empty list of custom renderers.
/// </summary>
internal sealed class InitCommand : Command<InitSettings> {

    protected override int Execute(CommandContext context, InitSettings settings, CancellationToken cancellationToken) {
        string path = ConfigurationStore.Resolve(settings.ConfigPath);

        if (ConfigurationStore.Exists(path) && settings.Force is false) {
            Console.Error.WriteLine($"error: a configuration already exists at '{path}'. Use --force to overwrite.");

            return 1;
        }

        ConfigurationStore.Save(path, new RendererConfiguration());
        Console.Out.WriteLine($"Created configuration at '{path}'.");

        return 0;
    }

}
