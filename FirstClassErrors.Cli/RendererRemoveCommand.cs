#region Usings declarations

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Removes a renderer library from the configuration. Matching is done on the resolved path, so the same
///     library is removed whether it was referenced with a relative or an absolute path.
/// </summary>
internal sealed class RendererRemoveCommand : Command<RendererReferenceSettings> {

    protected override int Execute(CommandContext context, RendererReferenceSettings settings, CancellationToken cancellationToken) {
        string path = ConfigurationStore.Resolve(settings.ConfigPath);

        if (ConfigurationStore.Exists(path) is false) {
            Console.Error.WriteLine($"error: no configuration at '{path}'. Run 'fce init' first.");

            return 1;
        }

        string                configDir     = Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory();
        string                target        = RendererLoader.Resolve(settings.LibraryPath, configDir);
        RendererConfiguration configuration = ConfigurationStore.Load(path);

        int removed = configuration.Renderers.RemoveAll(existing =>
            string.Equals(RendererLoader.Resolve(existing, configDir), target, StringComparison.OrdinalIgnoreCase));

        if (removed == 0) {
            Console.Error.WriteLine($"error: '{settings.LibraryPath}' is not referenced.");

            return 1;
        }

        ConfigurationStore.Save(path, configuration);
        Console.Out.WriteLine($"Removed '{settings.LibraryPath}'.");

        return 0;
    }

}
