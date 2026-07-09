namespace FirstClassErrors.Cli.UnitTests;

/// <summary>Shared helpers for the CLI unit tests.</summary>
internal static class CliTestHelpers {

    /// <summary>
    ///     Returns a path that is guaranteed not to exist, so <c>ConfigurationStore.Load</c> returns an empty
    ///     configuration and the test stays independent of any <c>fce.json</c> in the working directory.
    /// </summary>
    public static string NonExistentConfigPath() {
        return Path.Combine(Path.GetTempPath(), $"fce-absent-config-{Guid.NewGuid():N}.json");
    }

}
