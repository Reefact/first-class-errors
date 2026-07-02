#region Usings declarations

using System.Reflection;

using FirstClassErrors.GenDoc.Rendering;

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>Settings for <c>fce renderer add &lt;path&gt;</c>.</summary>
internal sealed class RendererReferenceSettings : ConfigScopedSettings {

    [CommandArgument(0, "<PATH>")]
    public string LibraryPath { get; set; } = string.Empty;

}

/// <summary>
///     Registers a renderer library in the configuration. The library is validated (it must load and expose at
///     least one renderer) before it is added; the path is stored as given, so relative paths stay portable.
/// </summary>
internal sealed class RendererAddCommand : Command<RendererReferenceSettings> {

    protected override int Execute(CommandContext context, RendererReferenceSettings settings, CancellationToken cancellationToken) {
        string path      = ConfigurationStore.Resolve(settings.ConfigPath);
        string configDir = Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory();
        string library   = RendererLoader.Resolve(settings.LibraryPath, configDir);

        if (File.Exists(library) is false) {
            Console.Error.WriteLine($"error: renderer library not found: '{library}'.");

            return 1;
        }

        IReadOnlyList<IErrorDocumentationRenderer> renderers;
        try {
            renderers = RendererLoader.InstantiateRenderers(Assembly.LoadFrom(library));
        } catch (Exception exception) {
            Console.Error.WriteLine($"error: could not load '{library}': {exception.Message}");

            return 1;
        }

        if (renderers.Count == 0) {
            Console.Error.WriteLine($"error: no IErrorDocumentationRenderer found in '{library}'.");

            return 1;
        }

        RendererConfiguration configuration = ConfigurationStore.Load(path);
        bool alreadyReferenced = configuration.Renderers.Any(existing =>
            string.Equals(RendererLoader.Resolve(existing, configDir), library, StringComparison.OrdinalIgnoreCase));

        if (alreadyReferenced) {
            Console.Out.WriteLine($"'{settings.LibraryPath}' is already referenced.");

            return 0;
        }

        configuration.Renderers.Add(settings.LibraryPath);
        ConfigurationStore.Save(path, configuration);

        string formats = string.Join(", ", renderers.Select(renderer => renderer.Format));
        Console.Out.WriteLine($"Added '{settings.LibraryPath}' (formats: {formats}).");

        return 0;
    }

}
