#region Usings declarations

using FirstClassErrors.GenDoc;
using FirstClassErrors.GenDoc.Rendering;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Routes the rendered documents to their destination — standard output for a single document, or files on disk —
///     and guards renderer-supplied paths against escaping the output directory. The raw side effects are delegated to
///     an <see cref="IOutputSink" />, so the routing and path-safety logic can be tested without touching the real
///     console or file system.
/// </summary>
internal sealed class DocumentationOutputWriter {

    #region Fields

    private readonly IOutputSink _sink;

    #endregion

    #region Constructors & Destructor

    public DocumentationOutputWriter(IOutputSink sink) {
        _sink = sink;
    }

    #endregion

    public void Write(IReadOnlyList<RenderedDocument> documents, string format, string? outputPath, IGenerationLogger logger) {
        // A renderer must honour its contract of returning at least one document. If a (custom) renderer returns an
        // empty list, fail with a clear message rather than an opaque IndexOutOfRange from documents[0] below.
        if (documents.Count == 0) {
            throw new InvalidOperationException($"The '{format}' renderer produced no documents; a renderer must return at least one document.");
        }

        bool hasOutput = !string.IsNullOrWhiteSpace(outputPath);

        // No target: only a single document can go to standard output.
        if (!hasOutput) {
            if (documents.Count > 1) {
                throw new InvalidOperationException("This layout produces several files; specify an output directory with --output (or 'output' in the configuration).");
            }

            _sink.WriteStandardOutput(documents[0].Content);

            return;
        }

        string fullOutput = Path.GetFullPath(outputPath!);

        // Treat the target as a directory when there are several files, when it already exists as one, or when the
        // path ends with a separator. Otherwise a single document is written to the given file path verbatim.
        bool asDirectory = documents.Count > 1 || Directory.Exists(fullOutput) || EndsWithSeparator(outputPath!);
        if (!asDirectory) {
            _sink.WriteFile(fullOutput, documents[0].Content);
            logger.Info($"Documentation written to '{fullOutput}'.");

            return;
        }

        foreach (RenderedDocument document in documents) {
            _sink.WriteFile(ResolveWithinOutput(fullOutput, document.RelativePath), document.Content);
        }

        logger.Info($"Documentation written to '{fullOutput}' ({documents.Count} file(s)).");
    }

    #region Helpers

    /// <summary>
    ///     Combines the output directory with a renderer-supplied relative path and guarantees the result stays inside
    ///     that directory. Renderers are third-party code (loaded via <c>fce config renderer add</c>) and may hand back,
    ///     by mistake, an absolute path or one containing '..' — <see cref="Path.Combine(string, string)" /> would then
    ///     resolve to a location outside the requested target, silently writing files where they are not expected.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the resolved path escapes <paramref name="outputDirectory" />.</exception>
    internal static string ResolveWithinOutput(string outputDirectory, string relativePath) {
        string target = Path.GetFullPath(Path.Combine(outputDirectory, relativePath));

        // Compare against the directory suffixed with a separator so a sibling such as 'out-evil' is not mistaken for a
        // path inside 'out'. The directory itself is not a valid file target either, so an exact match is rejected too.
        string root = outputDirectory.EndsWith(Path.DirectorySeparatorChar)
                          ? outputDirectory
                          : outputDirectory + Path.DirectorySeparatorChar;

        if (!target.StartsWith(root, StringComparison.Ordinal)) {
            throw new InvalidOperationException(
                $"The renderer produced a document whose path '{relativePath}' escapes the output directory '{outputDirectory}'.");
        }

        return target;
    }

    internal static bool EndsWithSeparator(string path) {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar);
    }

    #endregion

}
