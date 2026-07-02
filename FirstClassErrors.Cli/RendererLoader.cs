#region Usings declarations

using System.Reflection;

using FirstClassErrors.GenDoc;
using FirstClassErrors.GenDoc.Rendering;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Loads custom renderers from the assemblies referenced by the configuration. A renderer is any public,
///     concrete <see cref="IErrorDocumentationRenderer" /> with a public parameterless constructor.
/// </summary>
/// <remarks>
///     Assemblies are loaded into the CLI process. A plugin must reference the same <c>FirstClassErrors</c> contract
///     assembly as the host (i.e. not ship its own copy) so the renderer type resolves to the host's interface.
/// </remarks>
internal static class RendererLoader {

    #region Statics members declarations

    /// <summary>Resolves a configured path against the configuration folder (absolute paths are returned as-is).</summary>
    public static string Resolve(string configuredPath, string baseDirectory) {
        return Path.IsPathRooted(configuredPath)
                   ? Path.GetFullPath(configuredPath)
                   : Path.GetFullPath(Path.Combine(baseDirectory, configuredPath));
    }

    /// <summary>
    ///     Loads every renderer referenced by <paramref name="assemblyPaths" />. Problems (missing file, load error,
    ///     no renderer inside) are logged as warnings and skipped rather than aborting the run.
    /// </summary>
    public static IReadOnlyList<IErrorDocumentationRenderer> Load(IReadOnlyList<string> assemblyPaths, string baseDirectory, IGenerationLogger logger) {
        List<IErrorDocumentationRenderer> renderers = [];

        foreach (string configured in assemblyPaths) {
            string path = Resolve(configured, baseDirectory);
            if (File.Exists(path) is false) {
                logger.Warning($"Configured renderer library not found: '{path}'.");

                continue;
            }

            try {
                IReadOnlyList<IErrorDocumentationRenderer> found = InstantiateRenderers(Assembly.LoadFrom(path));
                if (found.Count == 0) {
                    logger.Warning($"No renderer found in '{path}'.");

                    continue;
                }

                renderers.AddRange(found);
                logger.Debug($"Loaded {found.Count} renderer(s) from '{path}'.");
            } catch (Exception exception) {
                logger.Warning($"Failed to load renderer library '{path}': {exception.Message}");
            }
        }

        return renderers;
    }

    /// <summary>Instantiates every renderer type defined in <paramref name="assembly" />.</summary>
    public static IReadOnlyList<IErrorDocumentationRenderer> InstantiateRenderers(Assembly assembly) {
        Type?[] types;
        try {
            types = assembly.GetTypes();
        } catch (ReflectionTypeLoadException exception) {
            types = exception.Types; // The loadable subset; unloadable entries are null and filtered below.
        }

        List<IErrorDocumentationRenderer> renderers = [];
        foreach (Type? type in types) {
            if (type is null || type.IsAbstract || type.IsInterface) { continue; }
            if (typeof(IErrorDocumentationRenderer).IsAssignableFrom(type) is false) { continue; }
            if (type.GetConstructor(Type.EmptyTypes) is null) { continue; }

            if (Activator.CreateInstance(type) is IErrorDocumentationRenderer renderer) { renderers.Add(renderer); }
        }

        return renderers;
    }

    #endregion

}
