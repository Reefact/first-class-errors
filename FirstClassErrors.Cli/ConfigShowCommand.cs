#region Usings declarations

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>Prints the current configuration file, or notes its absence.</summary>
internal sealed class ConfigShowCommand : Command<ConfigScopedSettings> {

    protected override int Execute(CommandContext context, ConfigScopedSettings settings, CancellationToken cancellationToken) {
        string path = ConfigurationStore.Resolve(settings.ConfigPath);

        if (ConfigurationStore.Exists(path) is false) {
            Console.Out.WriteLine($"No configuration at '{path}'. Run 'fce config init' to create one.");

            return 0;
        }

        Console.Out.WriteLine($"# {path}");
        Console.Out.WriteLine(File.ReadAllText(path));

        return 0;
    }

}
