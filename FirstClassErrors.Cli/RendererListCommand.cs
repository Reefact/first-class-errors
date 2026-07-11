#region Usings declarations

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using FirstClassErrors.GenDoc.Rendering;

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Lists the renderers the CLI can use: the built-in formats and, for each configured library, the formats it
///     contributes (or why it could not be loaded).
/// </summary>
internal sealed class RendererListCommand : Command<ConfigScopedSettings> {

    [SuppressMessage("Major Code Smell", "S3885:\"Assembly.Load\" should be used instead of \"Assembly.LoadFrom\"",
                     Justification =
                         "Each configured library is inspected by loading it from its file path; Assembly.Load resolves by " +
                         "name, not path. LoadFrom is deliberate — it probes the library's own directory for its dependencies.")]
    protected override int Execute(CommandContext context, ConfigScopedSettings settings, CancellationToken cancellationToken) {
        Console.Out.WriteLine("Built-in formats:");
        foreach (string format in RendererCatalog.BuiltInFormats) {
            Console.Out.WriteLine($"  - {format}");
        }

        string                path          = ConfigurationStore.Resolve(settings.ConfigPath);
        CliConfiguration configuration = ConfigurationStore.Load(path);

        Console.Out.WriteLine();
        if (configuration.Renderers.Count == 0) {
            Console.Out.WriteLine($"No custom renderers configured ({path}).");

            return 0;
        }

        string configDir = Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory();
        Console.Out.WriteLine($"Custom renderers ({path}):");
        foreach (string reference in configuration.Renderers) {
            string library = RendererLoader.Resolve(reference, configDir);
            if (File.Exists(library) is false) {
                Console.Out.WriteLine($"  - {reference} (missing: {library})");

                continue;
            }

            try {
                IReadOnlyList<IErrorDocumentationRenderer> found = RendererLoader.InstantiateRenderers(Assembly.LoadFrom(library));
                string formats = found.Count == 0 ? "no renderer found" : string.Join(", ", found.Select(renderer => renderer.Format));
                Console.Out.WriteLine($"  - {reference} ({formats})");
            } catch (Exception exception) {
                Console.Out.WriteLine($"  - {reference} (load error: {exception.Message})");
            }
        }

        return 0;
    }

}
