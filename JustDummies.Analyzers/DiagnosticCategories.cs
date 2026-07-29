namespace JustDummies.Analyzers;

/// <summary>
///     Categories used to group JustDummies diagnostics in the IDE and in <c>.editorconfig</c>.
/// </summary>
internal static class DiagnosticCategories {

    public const string Reproducibility = "JustDummies.Reproducibility";

    /// <summary>
    ///     Rules about the recipe-versus-value distinction the library teaches: a generator is an immutable recipe,
    ///     and <c>Generate()</c> is the only thing that materializes a value from it.
    /// </summary>
    public const string Usage = "JustDummies.Usage";

    /// <summary>
    ///     Rules that front-load, to build time, the subset of the library's run-time constraint checks that is
    ///     decidable from compile-time constants. The run-time checks stay: they cover every argument these cannot see.
    /// </summary>
    public const string Constraints = "JustDummies.Constraints";

    /// <summary>
    ///     Rules about assembling generators into bigger ones — <c>Combine</c>'s operands, and the element contract a
    ///     collection generator relies on. What they share is that nothing goes wrong: the composed generator builds,
    ///     draws and returns a value. It is simply not the value the call site describes.
    /// </summary>
    public const string Composition = "JustDummies.Composition";

}
