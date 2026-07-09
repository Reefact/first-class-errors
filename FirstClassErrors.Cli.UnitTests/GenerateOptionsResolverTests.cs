#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

[TestSubject(typeof(GenerateOptionsResolver))]
public sealed class GenerateOptionsResolverTests {

    #region FirstNonEmpty

    [Theory(DisplayName = "FirstNonEmpty returns the primary value when it is not blank.")]
    [InlineData("primary", "fallback", "primary")]
    [InlineData("primary", null, "primary")]
    public void FirstNonEmptyReturnsThePrimaryWhenPresent(string? primary, string? fallback, string expected) {
        Check.That(GenerateOptionsResolver.FirstNonEmpty(primary, fallback)).IsEqualTo(expected);
    }

    [Theory(DisplayName = "FirstNonEmpty falls back when the primary is null, empty or whitespace.")]
    [InlineData(null, "fallback")]
    [InlineData("", "fallback")]
    [InlineData("   ", "fallback")]
    public void FirstNonEmptyFallsBackWhenThePrimaryIsBlank(string? primary, string fallback) {
        Check.That(GenerateOptionsResolver.FirstNonEmpty(primary, fallback)).IsEqualTo(fallback);
    }

    [Fact(DisplayName = "FirstNonEmpty returns null when both values are blank.")]
    public void FirstNonEmptyReturnsNullWhenBothAreBlank() {
        Check.That(GenerateOptionsResolver.FirstNonEmpty("  ", null)).IsNull();
    }

    #endregion

    #region NormalizeFormat

    [Theory(DisplayName = "NormalizeFormat lowercases, trims, and maps the 'md' alias to 'markdown'.")]
    [InlineData("md", "markdown")]
    [InlineData("  MD  ", "markdown")]
    [InlineData("JSON", "json")]
    [InlineData(" Html ", "html")]
    public void NormalizeFormatNormalizesAndMapsTheAlias(string input, string expected) {
        Check.That(GenerateOptionsResolver.NormalizeFormat(input)).IsEqualTo(expected);
    }

    #endregion

    #region Resolve — precedence

    [Fact(DisplayName = "A command-line value overrides the configuration value.")]
    public void ACommandLineValueOverridesTheConfiguration() {
        GenerateSettings settings      = new() { SolutionPath = "app.sln", Format = "html" };
        CliConfiguration configuration = new() { Format       = "markdown" };

        ResolvedGenerateOptions resolved = GenerateOptionsResolver.Resolve(settings, configuration);

        Check.That(resolved.Format).IsEqualTo("html");
    }

    [Fact(DisplayName = "The configuration value is used when the command line omits it.")]
    public void TheConfigurationValueIsUsedWhenTheCommandLineOmitsIt() {
        GenerateSettings settings      = new() { SolutionPath = "app.sln" };
        CliConfiguration configuration = new() { Format       = "html", ServiceName = "svc" };

        ResolvedGenerateOptions resolved = GenerateOptionsResolver.Resolve(settings, configuration);

        Check.That(resolved.Format).IsEqualTo("html");
        Check.That(resolved.ServiceName).IsEqualTo("svc");
    }

    [Fact(DisplayName = "Built-in defaults apply when neither the command line nor the configuration sets a value.")]
    public void BuiltInDefaultsApplyWhenNothingIsSet() {
        GenerateSettings settings      = new() { SolutionPath = "app.sln" };
        CliConfiguration configuration = new();

        ResolvedGenerateOptions resolved = GenerateOptionsResolver.Resolve(settings, configuration);

        Check.That(resolved.Format).IsEqualTo("json");
        Check.That(resolved.Layout).IsEqualTo("single");
        Check.That(resolved.BuildConfiguration).IsEqualTo("Debug");
        Check.That(resolved.Language).IsEqualTo("en");
    }

    [Fact(DisplayName = "NoBuild and Strict are true when set on either the command line or the configuration.")]
    public void NoBuildAndStrictAreEitherSource() {
        Check.That(GenerateOptionsResolver.Resolve(new GenerateSettings { SolutionPath = "a.sln", NoBuild = true }, new CliConfiguration()).NoBuild).IsTrue();
        Check.That(GenerateOptionsResolver.Resolve(new GenerateSettings { SolutionPath = "a.sln" }, new CliConfiguration { NoBuild = true }).NoBuild).IsTrue();
        Check.That(GenerateOptionsResolver.Resolve(new GenerateSettings { SolutionPath = "a.sln" }, new CliConfiguration()).Strict).IsFalse();
        Check.That(GenerateOptionsResolver.Resolve(new GenerateSettings { SolutionPath = "a.sln" }, new CliConfiguration { Strict = true }).Strict).IsTrue();
    }

    #endregion

    #region Resolve — source selection

    [Fact(DisplayName = "A command-line solution is selected as the source.")]
    public void ACommandLineSolutionIsSelected() {
        ResolvedGenerateOptions resolved = GenerateOptionsResolver.Resolve(new GenerateSettings { SolutionPath = "app.sln" }, new CliConfiguration());

        Check.That(resolved.HasSolution).IsTrue();
        Check.That(resolved.Solution).IsEqualTo("app.sln");
        Check.That(resolved.HasAssemblies).IsFalse();
    }

    [Fact(DisplayName = "Command-line assemblies are selected as the source.")]
    public void CommandLineAssembliesAreSelected() {
        ResolvedGenerateOptions resolved = GenerateOptionsResolver.Resolve(new GenerateSettings { AssemblyPaths = ["a.dll", "b.dll"] }, new CliConfiguration());

        Check.That(resolved.HasAssemblies).IsTrue();
        Check.That(resolved.Assemblies).ContainsExactly("a.dll", "b.dll");
        Check.That(resolved.HasSolution).IsFalse();
    }

    [Fact(DisplayName = "The configured source is used when the command line gives none.")]
    public void TheConfiguredSourceIsUsedWhenTheCommandLineGivesNone() {
        ResolvedGenerateOptions resolved = GenerateOptionsResolver.Resolve(new GenerateSettings(), new CliConfiguration { Solution = "configured.sln" });

        Check.That(resolved.HasSolution).IsTrue();
        Check.That(resolved.Solution).IsEqualTo("configured.sln");
    }

    [Fact(DisplayName = "A command-line source replaces the configured source wholesale.")]
    public void ACommandLineSourceReplacesTheConfiguredSourceWholesale() {
        // Command-line assemblies must not combine with a configured solution.
        ResolvedGenerateOptions resolved = GenerateOptionsResolver.Resolve(
            new GenerateSettings { AssemblyPaths = ["a.dll"] },
            new CliConfiguration { Solution = "configured.sln" });

        Check.That(resolved.HasAssemblies).IsTrue();
        Check.That(resolved.Assemblies).ContainsExactly("a.dll");
        Check.That(resolved.HasSolution).IsFalse();
        Check.That(resolved.Solution).IsNull();
    }

    #endregion

}
