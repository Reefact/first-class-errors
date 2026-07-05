#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

[TestSubject(typeof(SolutionGenerationOptions))]
public sealed class SolutionGenerationOptionsTests {

    [Fact(DisplayName = "SolutionGenerationOptions exposes safe defaults.")]
    public void ExposesSafeDefaults() {
        // Exercise
        SolutionGenerationOptions options = new();

        // Verify
        Check.That(options.BuildSolution).IsTrue();
        Check.That(options.Configuration).IsEqualTo("Debug");
        Check.That(options.TargetFramework).IsNull();
        Check.That(options.FailureBehavior).IsEqualTo(FailureBehavior.Stop);
        Check.That(options.IncludeProjectsWithoutOptIn).IsFalse();
        Check.That(options.OptInPropertyName).IsEqualTo("GenerateErrorDocumentation");
        Check.That(options.DotNetBuildAdditionalArguments).ContainsExactly("--nologo");
        Check.That(options.WorkerAssemblyPath).IsNull();
        Check.That(options.WorkerTimeout).IsEqualTo(TimeSpan.FromMinutes(2));
        Check.That(options.BuildTimeout).IsEqualTo(TimeSpan.FromMinutes(10));
        Check.That(options.SdkQueryTimeout).IsEqualTo(TimeSpan.FromMinutes(2));
        Check.That(options.Culture).IsNull();
        Check.That(options.Logger).IsNotNull();
    }

    [Fact(DisplayName = "The default logger accepts every level without throwing.")]
    public void TheDefaultLoggerDoesNotThrow() {
        // Setup
        IGenerationLogger logger = new SolutionGenerationOptions().Logger;

        // Exercise & verify
        Check.ThatCode(() => {
                  logger.Info("info");
                  logger.Warning("warning");
                  logger.Error("error");
                  logger.Debug("debug");
              })
             .DoesNotThrow();
    }

}
