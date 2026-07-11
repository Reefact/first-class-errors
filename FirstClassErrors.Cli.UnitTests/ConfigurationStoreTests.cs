#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

[TestSubject(typeof(ConfigurationStore))]
public sealed class ConfigurationStoreTests {

    [Fact(DisplayName = "Loading a configuration from a missing file returns an empty configuration.")]
    public void LoadingAMissingFileReturnsAnEmptyConfiguration() {
        // Setup: a path guaranteed not to exist.
        string path = CliTestHelpers.NonExistentConfigPath();

        // Exercise
        CliConfiguration configuration = ConfigurationStore.Load(path);

        // Verify: a fresh configuration carries none of the persisted defaults.
        Check.That(configuration.Solution).IsNull();
        Check.That(configuration.Format).IsNull();
        Check.That(configuration.NoBuild).IsNull();
    }

    [Fact(DisplayName = "Loading a configuration from a blank file returns an empty configuration.")]
    public void LoadingABlankFileReturnsAnEmptyConfiguration() {
        // Setup: an existing but blank file exercises the whitespace short-circuit rather than the missing-file one.
        string path = Path.Combine(Path.GetTempPath(), $"fce-blank-config-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "   \n");

        try {
            // Exercise
            CliConfiguration configuration = ConfigurationStore.Load(path);

            // Verify
            Check.That(configuration.Solution).IsNull();
        } finally {
            File.Delete(path);
        }
    }

}
