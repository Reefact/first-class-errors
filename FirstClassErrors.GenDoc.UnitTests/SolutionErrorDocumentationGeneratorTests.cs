#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

[TestSubject(typeof(SolutionErrorDocumentationGenerator))]
public sealed class SolutionErrorDocumentationGeneratorTests {

    [Fact(DisplayName = "GetErrorDocumentationFrom rejects a null solution path.")]
    public void GetErrorDocumentationFromRejectsANullSolutionPath() {
        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetErrorDocumentationFrom rejects null options.")]
    public void GetErrorDocumentationFromRejectsNullOptions() {
        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom("app.sln", null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetErrorDocumentationFrom fails when the solution file does not exist.")]
    public void GetErrorDocumentationFromFailsWhenTheSolutionDoesNotExist() {
        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom("this-solution-does-not-exist.sln", new SolutionGenerationOptions()))
             .Throws<FileNotFoundException>();
    }

    [Fact(DisplayName = "GetErrorDocumentationFrom rejects a path that is not a .sln file.")]
    public void GetErrorDocumentationFromRejectsANonSolutionFile() {
        // Setup: a real file whose extension is not .sln.
        string path = Path.GetTempFileName();

        try {
            // Exercise & verify
            Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(path, new SolutionGenerationOptions()))
                 .Throws<ArgumentException>();
        } finally {
            File.Delete(path);
        }
    }

    [Theory(DisplayName = "GetErrorDocumentationFrom accepts the .sln and .slnx solution formats.")]
    [InlineData(".sln")]
    [InlineData(".slnx")]
    public void GetErrorDocumentationFromAcceptsSolutionFormats(string extension) {
        // Setup: a real file carrying a solution extension. It is not a valid solution, so the enumeration further
        // down the pipeline will ultimately fail — but only *after* the extension validation, which is what this test
        // guards. The rejected-extension path is the one that throws ArgumentException (see the sibling test); proving
        // the format is accepted therefore means the call throws anything *but* an ArgumentException.
        string path = Path.ChangeExtension(Path.GetTempFileName(), extension);
        File.WriteAllText(path, string.Empty);

        try {
            // Exercise
            Exception? caught = Record.Exception(
                () => SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(path, new SolutionGenerationOptions { BuildSolution = false }).ToList());

            // Verify: the extension was accepted, so no ArgumentException was raised for it.
            Check.That(caught).IsNotInstanceOf<ArgumentException>();
        } finally {
            File.Delete(path);
        }
    }

    [Fact(DisplayName = "GetErrorDocumentationFromAssemblies rejects a null path list.")]
    public void GetErrorDocumentationFromAssembliesRejectsANullPathList() {
        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(null!, new SolutionGenerationOptions()))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetErrorDocumentationFromAssemblies rejects null options.")]
    public void GetErrorDocumentationFromAssembliesRejectsNullOptions() {
        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { "app.dll" }, null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A missing assembly is skipped (empty result) when the failure behavior is Continue.")]
    public void AMissingAssemblyIsSkippedWhenContinuing() {
        // Setup
        SolutionGenerationOptions options = new() { FailureBehavior = FailureBehavior.Continue };

        // Exercise
        IEnumerable<ErrorDocumentation> result =
            SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { "this-assembly-does-not-exist.dll" }, options);

        // Verify: no worker is launched because no assembly resolved; the result is empty.
        Check.That(result).IsEmpty();
    }

    [Fact(DisplayName = "A skipped assembly is logged as a warning (never silent) when the failure behavior is Continue.")]
    public void ASkippedAssemblyIsLoggedWhenContinuing() {
        // Setup
        RecordingGenerationLogger logger = new();
        SolutionGenerationOptions options = new() {
            FailureBehavior = FailureBehavior.Continue,
            Logger          = logger
        };

        // Exercise
        SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { "this-assembly-does-not-exist.dll" }, options);

        // Verify: the skipped assembly leaves a warning trace mentioning the missing file.
        Check.That(logger.Warnings).HasSize(1);
        Check.That(logger.Warnings[0]).Contains("this-assembly-does-not-exist.dll");
    }

    private sealed class RecordingGenerationLogger : IGenerationLogger {

        public List<string> Warnings { get; } = new();

        public void Info(string    message) { }
        public void Warning(string message) { Warnings.Add(message); }
        public void Error(string   message) { }
        public void Debug(string   message) { }

    }

    [Fact(DisplayName = "A missing assembly aborts when the failure behavior is Stop.")]
    public void AMissingAssemblyAbortsWhenStopping() {
        // Setup
        SolutionGenerationOptions options = new() { FailureBehavior = FailureBehavior.Stop };

        // Exercise & verify
        Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { "this-assembly-does-not-exist.dll" }, options))
             .Throws<SolutionDocumentationGenerationException>();
    }

    [Fact(DisplayName = "GetErrorDocumentationFromAssemblies returns an empty catalog for an empty path list.")]
    public void GetErrorDocumentationFromAssembliesReturnsEmptyForAnEmptyList() {
        // Exercise: no assembly path at all, so no worker is ever launched.
        IEnumerable<ErrorDocumentation> result =
            SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies([], new SolutionGenerationOptions());

        // Verify
        Check.That(result).IsEmpty();
    }

    [Fact(DisplayName = "Generation fails fast when the configured documentation worker cannot be found.")]
    public void GenerationFailsWhenTheConfiguredWorkerCannotBeFound() {
        // Setup: a real (resolvable) assembly path, but a worker path that does not exist. Resolving the worker throws
        // before any process is launched, so this stays a pure, SDK-free test.
        string assemblyPath = Path.GetTempFileName();
        SolutionGenerationOptions options = new() {
            WorkerAssemblyPath = Path.Combine(Path.GetTempPath(), "this-worker-does-not-exist.dll")
        };

        try {
            // Exercise & verify
            Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { assemblyPath }, options))
                 .Throws<SolutionDocumentationGenerationException>();
        } finally {
            File.Delete(assemblyPath);
        }
    }

    [Fact(DisplayName = "Generation is abandoned when cancellation is already requested.")]
    public void GenerationIsAbandonedWhenCancellationIsRequested() {
        // Setup: a resolvable assembly and a resolvable worker, so nothing fails on its own — the only reason to stop is
        // the cancellation. The token is already cancelled, so the per-assembly loop must abandon before launching any
        // worker process and surface an OperationCanceledException.
        string assemblyPath = Path.GetTempFileName();
        string workerPath   = Path.GetTempFileName();

        using CancellationTokenSource cts = new();
        cts.Cancel();

        SolutionGenerationOptions options = new() {
            WorkerAssemblyPath = workerPath,
            CancellationToken  = cts.Token
        };

        try {
            // Exercise & verify
            Check.ThatCode(() => SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(new[] { assemblyPath }, options))
                 .Throws<OperationCanceledException>();
        } finally {
            File.Delete(assemblyPath);
            File.Delete(workerPath);
        }
    }

    [Fact(DisplayName = "Cross-assembly deduplication keeps a single entry per code and warns about the collision.")]
    public void CrossAssemblyDeduplicationWarnsAboutTheCollision() {
        // Setup: the same code declared by two different sources (i.e. two different assemblies' workers).
        RecordingLogger logger = new();
        List<ErrorDocumentation> documentation = new() {
            new ErrorDocumentation { Code = "SHARED", Title = "From A", Source = "AssemblyA" },
            new ErrorDocumentation { Code = "SHARED", Title = "From B", Source = "AssemblyB" },
            new ErrorDocumentation { Code = "UNIQUE", Title = "Alone",  Source = "AssemblyA" }
        };

        // Exercise
        IReadOnlyList<ErrorDocumentation> catalog =
            SolutionErrorDocumentationGenerator.DeduplicateAcrossAssemblies(documentation, logger);

        // Verify: the code collapses to a single entry (the first-seen survives), the unique code is untouched...
        Check.That(catalog.Select(doc => doc.Code)).ContainsExactly("SHARED", "UNIQUE");
        Check.That(catalog.Single(doc => doc.Code == "SHARED").Source).IsEqualTo("AssemblyA");

        // ...and the drop is not silent: a warning names the code and the dropped source.
        Check.That(logger.Warnings).HasSize(1);
        Check.That(logger.Warnings[0]).Contains("SHARED");
        Check.That(logger.Warnings[0]).Contains("AssemblyB");
    }

    [Fact(DisplayName = "Cross-assembly deduplication stays silent when there is no duplicate code.")]
    public void CrossAssemblyDeduplicationStaysSilentWithoutDuplicates() {
        // Setup
        RecordingLogger logger = new();
        List<ErrorDocumentation> documentation = new() {
            new ErrorDocumentation { Code = "A", Source = "AssemblyA" },
            new ErrorDocumentation { Code = "B", Source = "AssemblyB" }
        };

        // Exercise
        IReadOnlyList<ErrorDocumentation> catalog =
            SolutionErrorDocumentationGenerator.DeduplicateAcrossAssemblies(documentation, logger);

        // Verify
        Check.That(catalog.Select(doc => doc.Code)).ContainsExactly("A", "B");
        Check.That(logger.Warnings).IsEmpty();
    }

    [Fact(DisplayName = "A project opted in with a single literal property is included.")]
    public void ASingleLiteralOptInIsIncluded() {
        // Setup
        string project = WriteTempProject("<PropertyGroup><GenerateErrorDocumentation>true</GenerateErrorDocumentation></PropertyGroup>");

        try {
            // Exercise & verify
            Check.That(SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, new SolutionGenerationOptions()))
                 .IsTrue();
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "A project with an explicit falsy opt-in is excluded, even under the include-everything policy.")]
    public void AnExplicitOptOutIsExcluded() {
        // Setup
        string project = WriteTempProject("<PropertyGroup><GenerateErrorDocumentation>false</GenerateErrorDocumentation></PropertyGroup>");

        try {
            // Exercise & verify
            Check.That(SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, new SolutionGenerationOptions { IncludeProjectsWithoutOptIn = true }))
                 .IsFalse();
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "A project without the opt-in is excluded by default.")]
    public void AnAbsentOptInIsExcludedByDefault() {
        // Setup
        string project = WriteTempProject("<PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>");

        try {
            // Exercise & verify
            Check.That(SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, new SolutionGenerationOptions()))
                 .IsFalse();
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "A project without the opt-in is included under the include-everything policy.")]
    public void AnAbsentOptInIsIncludedUnderTheIncludeEverythingPolicy() {
        // Setup
        string project = WriteTempProject("<PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>");

        try {
            // Exercise & verify
            Check.That(SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, new SolutionGenerationOptions { IncludeProjectsWithoutOptIn = true }))
                 .IsTrue();
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "An opt-in defined more than once is ambiguous: the project is skipped and the skip is logged (Continue).")]
    public void ADuplicatedOptInIsSkippedAndLoggedWhenContinuing() {
        // Setup: two definitions of the marker — its effective value cannot be known without evaluating MSBuild.
        RecordingGenerationLogger logger = new();
        SolutionGenerationOptions options = new() {
            FailureBehavior = FailureBehavior.Continue,
            Logger          = logger
        };
        string project = WriteTempProject(
            "<PropertyGroup><GenerateErrorDocumentation>false</GenerateErrorDocumentation></PropertyGroup>" +
            "<PropertyGroup><GenerateErrorDocumentation>true</GenerateErrorDocumentation></PropertyGroup>");

        try {
            // Exercise
            bool included = SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, options);

            // Verify: the project is left out, and the skip is traced (never silent) with the property named.
            Check.That(included).IsFalse();
            Check.That(logger.Warnings).HasSize(1);
            Check.That(logger.Warnings[0]).Contains("GenerateErrorDocumentation");
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "An opt-in gated behind a Condition is ambiguous: the project is skipped and the skip is logged (Continue).")]
    public void AConditionedOptInIsSkippedAndLoggedWhenContinuing() {
        // Setup: the marker sits under a Condition, so the raw XML value is not necessarily the effective one.
        RecordingGenerationLogger logger = new();
        SolutionGenerationOptions options = new() {
            FailureBehavior = FailureBehavior.Continue,
            Logger          = logger
        };
        string project = WriteTempProject(
            "<PropertyGroup Condition=\" '$(Configuration)' == 'Release' \"><GenerateErrorDocumentation>true</GenerateErrorDocumentation></PropertyGroup>");

        try {
            // Exercise
            bool included = SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, options);

            // Verify
            Check.That(included).IsFalse();
            Check.That(logger.Warnings).HasSize(1);
            Check.That(logger.Warnings[0]).Contains("Condition");
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "An ambiguous opt-in aborts when the failure behavior is Stop.")]
    public void AnAmbiguousOptInAbortsWhenStopping() {
        // Setup: default options use FailureBehavior.Stop.
        string project = WriteTempProject(
            "<PropertyGroup><GenerateErrorDocumentation>true</GenerateErrorDocumentation></PropertyGroup>" +
            "<PropertyGroup><GenerateErrorDocumentation>true</GenerateErrorDocumentation></PropertyGroup>");

        try {
            // Exercise & verify
            Check.ThatCode(() => SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, new SolutionGenerationOptions()))
                 .Throws<SolutionDocumentationGenerationException>();
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "A solution where no project opts in warns with the opt-in property name instead of staying silent.")]
    public void AnEmptyOptInResultIsWarnedNotSilent() {
        // Setup: two projects, neither carrying the marker — the exact signature of an opt-in declared only in a shared
        // Directory.Build.props (invisible to the literal .csproj read) or of a misspelled property name.
        RecordingGenerationLogger logger = new();
        SolutionGenerationOptions options = new() { Logger = logger };
        string                    projectA = WriteTempProject("<PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>");
        string                    projectB = WriteTempProject("<PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>");

        try {
            // Exercise
            IReadOnlyList<string> included = SolutionErrorDocumentationGenerator.FilterProjects(new[] { projectA, projectB }, options);

            // Verify: nothing is retained, and the empty result is traced with the property name and the read limitation.
            Check.That(included).IsEmpty();
            Check.That(logger.Warnings).HasSize(1);
            Check.That(logger.Warnings[0]).Contains("GenerateErrorDocumentation");
            Check.That(logger.Warnings[0]).Contains("Directory.Build.props");
        } finally {
            File.Delete(projectA);
            File.Delete(projectB);
        }
    }

    [Fact(DisplayName = "No warning is emitted for an empty opt-in when the include-everything policy makes the filter inactive.")]
    public void AnEmptyOptInResultStaysSilentWhenTheFilterIsInactive() {
        // Setup
        RecordingGenerationLogger logger = new();
        SolutionGenerationOptions options = new() {
            IncludeProjectsWithoutOptIn = true,
            Logger                      = logger
        };
        string project = WriteTempProject("<PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>");

        try {
            // Exercise
            IReadOnlyList<string> included = SolutionErrorDocumentationGenerator.FilterProjects(new[] { project }, options);

            // Verify: the project is included by the global policy, so the silence is correct.
            Check.That(included).ContainsExactly(project);
            Check.That(logger.Warnings).IsEmpty();
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "No warning is emitted when at least one project opts in.")]
    public void APartialOptInStaysSilent() {
        // Setup
        RecordingGenerationLogger logger  = new();
        SolutionGenerationOptions options = new() { Logger = logger };
        string                    optedIn = WriteTempProject("<PropertyGroup><GenerateErrorDocumentation>true</GenerateErrorDocumentation></PropertyGroup>");
        string                    plain   = WriteTempProject("<PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>");

        try {
            // Exercise
            IReadOnlyList<string> included = SolutionErrorDocumentationGenerator.FilterProjects(new[] { optedIn, plain }, options);

            // Verify
            Check.That(included).ContainsExactly(optedIn);
            Check.That(logger.Warnings).IsEmpty();
        } finally {
            File.Delete(optedIn);
            File.Delete(plain);
        }
    }

    [Fact(DisplayName = "No warning is emitted for a solution without any project to examine.")]
    public void AnEmptySolutionStaysSilent() {
        // Setup
        RecordingGenerationLogger logger  = new();
        SolutionGenerationOptions options = new() { Logger = logger };

        // Exercise
        IReadOnlyList<string> included = SolutionErrorDocumentationGenerator.FilterProjects([], options);

        // Verify: with nothing to examine there is nothing to diagnose.
        Check.That(included).IsEmpty();
        Check.That(logger.Warnings).IsEmpty();
    }

    [Fact(DisplayName = "An opt-in under a Choose/When branch is ambiguous: the project is skipped and the skip is logged (Continue).")]
    public void AChooseWhenOptInIsSkippedAndLoggedWhenContinuing() {
        // Setup: the Condition sits on the <When> grandparent — neither the property nor its PropertyGroup carries it.
        RecordingGenerationLogger logger = new();
        SolutionGenerationOptions options = new() {
            FailureBehavior = FailureBehavior.Continue,
            Logger          = logger
        };
        string project = WriteTempProject(
            "<Choose><When Condition=\" '$(Configuration)' == 'Release' \">" +
            "<PropertyGroup><GenerateErrorDocumentation>true</GenerateErrorDocumentation></PropertyGroup>" +
            "</When></Choose>");

        try {
            // Exercise
            bool included = SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, options);

            // Verify
            Check.That(included).IsFalse();
            Check.That(logger.Warnings).HasSize(1);
            Check.That(logger.Warnings[0]).Contains("Condition");
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "An opt-in under a Choose/When branch aborts when the failure behavior is Stop.")]
    public void AChooseWhenOptInAbortsWhenStopping() {
        // Setup: default options use FailureBehavior.Stop.
        string project = WriteTempProject(
            "<Choose><When Condition=\" '$(Configuration)' == 'Release' \">" +
            "<PropertyGroup><GenerateErrorDocumentation>true</GenerateErrorDocumentation></PropertyGroup>" +
            "</When></Choose>");

        try {
            // Exercise & verify
            Check.ThatCode(() => SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, new SolutionGenerationOptions()))
                 .Throws<SolutionDocumentationGenerationException>();
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "An opt-in under a Choose/Otherwise branch is ambiguous even though it carries no Condition attribute.")]
    public void AChooseOtherwiseOptInIsAmbiguous() {
        // Setup: the <Otherwise> branch bears no Condition attribute anywhere on the property's ancestor chain, yet it
        // only applies when no <When> matched — conditional by construction.
        RecordingGenerationLogger logger = new();
        SolutionGenerationOptions options = new() {
            FailureBehavior = FailureBehavior.Continue,
            Logger          = logger
        };
        string project = WriteTempProject(
            "<Choose><When Condition=\" '$(Configuration)' == 'Release' \">" +
            "<PropertyGroup><SomeOtherProperty>x</SomeOtherProperty></PropertyGroup>" +
            "</When><Otherwise>" +
            "<PropertyGroup><GenerateErrorDocumentation>true</GenerateErrorDocumentation></PropertyGroup>" +
            "</Otherwise></Choose>");

        try {
            // Exercise
            bool included = SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, options);

            // Verify
            Check.That(included).IsFalse();
            Check.That(logger.Warnings).HasSize(1);
            Check.That(logger.Warnings[0]).Contains("Condition");
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "A target-local assignment does not turn a legitimate opt-in into a false duplicate.")]
    public void ATargetLocalAssignmentIsNotAFalseDuplicate() {
        // Setup: a clean evaluation-time opt-in, plus an occurrence inside a <Target> — assigned when the target runs,
        // nonexistent at evaluation time. It must count neither as a duplicate nor as a value.
        RecordingGenerationLogger logger = new();
        SolutionGenerationOptions options = new() {
            FailureBehavior = FailureBehavior.Continue,
            Logger          = logger
        };
        string project = WriteTempProject(
            "<PropertyGroup><GenerateErrorDocumentation>true</GenerateErrorDocumentation></PropertyGroup>" +
            "<Target Name=\"AfterBuildTweak\">" +
            "<PropertyGroup><GenerateErrorDocumentation>false</GenerateErrorDocumentation></PropertyGroup>" +
            "</Target>");

        try {
            // Exercise
            bool included = SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, options);

            // Verify: the evaluation-time opt-in wins, without any ambiguity diagnostic.
            Check.That(included).IsTrue();
            Check.That(logger.Warnings).IsEmpty();
        } finally {
            File.Delete(project);
        }
    }

    [Fact(DisplayName = "An opt-in that only exists inside a Target is absent at evaluation time.")]
    public void ATargetOnlyOptInIsAbsent() {
        // Setup: the only occurrence is target-local. Read as absent, the project must follow the global policy —
        // which distinguishes absence from an explicit opt-out (the latter is honored even under include-everything).
        string project = WriteTempProject(
            "<Target Name=\"AfterBuildTweak\">" +
            "<PropertyGroup><GenerateErrorDocumentation>false</GenerateErrorDocumentation></PropertyGroup>" +
            "</Target>");

        try {
            // Exercise & verify: excluded by the default opt-in policy...
            Check.That(SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, new SolutionGenerationOptions()))
                 .IsFalse();

            // ...but included under include-everything, proving the read was Absent and not an explicit "false".
            Check.That(SolutionErrorDocumentationGenerator.ShouldIncludeProject(project, new SolutionGenerationOptions { IncludeProjectsWithoutOptIn = true }))
                 .IsTrue();
        } finally {
            File.Delete(project);
        }
    }

    private static string WriteTempProject(string body) {
        string path = Path.ChangeExtension(Path.GetTempFileName(), ".csproj");
        File.WriteAllText(path, $"<Project Sdk=\"Microsoft.NET.Sdk\">{body}</Project>");

        return path;
    }

    private sealed class RecordingLogger : IGenerationLogger {

        public List<string> Warnings { get; } = new();

        public void Info(string    message) { }
        public void Warning(string message) { Warnings.Add(message); }
        public void Error(string   message) { }
        public void Debug(string   message) { }

    }

}
