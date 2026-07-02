namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     Controls how the Markdown renderer lays its output out on disk.
/// </summary>
public enum MarkdownLayout {

    /// <summary>
    ///     A single Markdown file containing a table of contents followed by every error.
    /// </summary>
    Single,

    /// <summary>
    ///     One Markdown file per error plus an index file (<c>README.md</c>) linking to each of them.
    /// </summary>
    Split

}
