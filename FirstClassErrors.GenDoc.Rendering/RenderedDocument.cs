namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     A single rendered output file produced by a renderer: its <see cref="RelativePath" /> (used as the file name
///     when writing to a directory) and its textual <see cref="Content" />.
/// </summary>
/// <remarks>
///     Single-file renderers (JSON, single-file Markdown) return exactly one document. Multi-file renderers (the
///     split Markdown layout) return an index document plus one document per error.
/// </remarks>
/// <param name="RelativePath">The suggested file name (may contain sub-directories), relative to the output folder.</param>
/// <param name="Content">The rendered text of the file.</param>
public sealed record RenderedDocument(string RelativePath, string Content);
