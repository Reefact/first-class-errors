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

}
