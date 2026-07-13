#region Usings declarations

using System.Text.Json;

using FirstClassErrors.GenDoc.Versioning;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

/// <summary>
///     Drives the <c>catalog diff</c> command end to end through the real configuration store, baseline store and
///     differ, but with a fake snapshot source (no process spawned) and a string writer for standard output. This
///     exercises the command wiring — option validation, exit-code policy, report routing, source-vs-`--against`
///     selection and cancellation — without touching the console or the build pipeline.
/// </summary>
[TestSubject(typeof(CatalogDiffCommand))]
public sealed class CatalogDiffCommandEndToEndTests {

    [Fact(DisplayName = "A missing baseline is an execution error (exit 1), not drift.")]
    public void AMissingBaselineIsAnExecutionError() {
        using TempDir dir = new();
        RecordingSnapshotSource source = new(Snapshot("A"));
        (int exit, string _, RecordingLogger logger) = RunDiff(source, new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = dir.File("errors-baseline.json")
        });

        Check.That(exit).IsEqualTo(1);
        Check.That(source.WasInvoked).IsFalse();
        Check.That(string.Join("\n", logger.Errors)).Contains("No baseline");
    }

    [Fact(DisplayName = "No change against the baseline reports nothing and exits 0.")]
    public void NoChangeExitsZero() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A"));

        (int exit, string output, RecordingLogger _) = RunDiff(new RecordingSnapshotSource(Snapshot("A")), new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline
        });

        Check.That(exit).IsEqualTo(0);
        Check.That(output).Contains("No catalog changes.");
    }

    [Fact(DisplayName = "A removed code is a breaking change: exit 2 with a report.")]
    public void ARemovedCodeExitsTwo() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A", "B"));

        (int exit, string output, RecordingLogger logger) = RunDiff(new RecordingSnapshotSource(Snapshot("A")), new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline
        });

        Check.That(exit).IsEqualTo(2);
        Check.That(output).Contains("[removed] B");
        Check.That(string.Join("\n", logger.Errors)).Contains("1 breaking change(s)");
    }

    [Fact(DisplayName = "--fail-on any turns a compatible-only change into a failure (exit 2).")]
    public void FailOnAnyFailsOnCompatibleChange() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A"));

        (int exit, string _, RecordingLogger __) = RunDiff(new RecordingSnapshotSource(Snapshot("A", "B")), new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline,
            FailOn       = "any"
        });

        Check.That(exit).IsEqualTo(2);
    }

    [Fact(DisplayName = "The default --fail-on breaking passes a compatible-only change (exit 0).")]
    public void DefaultFailOnBreakingPassesCompatibleChange() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A"));

        (int exit, string output, RecordingLogger _) = RunDiff(new RecordingSnapshotSource(Snapshot("A", "B")), new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline
        });

        Check.That(exit).IsEqualTo(0);
        Check.That(output).Contains("[added] B");
    }

    [Fact(DisplayName = "--fail-on none never fails, even on a breaking change (exit 0).")]
    public void FailOnNoneNeverFails() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A", "B"));

        (int exit, string _, RecordingLogger __) = RunDiff(new RecordingSnapshotSource(Snapshot("A")), new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline,
            FailOn       = "none"
        });

        Check.That(exit).IsEqualTo(0);
    }

    [Fact(DisplayName = "--report json writes a machine-readable document.")]
    public void ReportJsonWritesJson() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A", "B"));

        (int exit, string output, RecordingLogger _) = RunDiff(new RecordingSnapshotSource(Snapshot("A")), new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline,
            Report       = "json"
        });

        Check.That(exit).IsEqualTo(2);
        using JsonDocument parsed = JsonDocument.Parse(output);
        Check.That(parsed.RootElement.GetProperty("hasBreakingChanges").GetBoolean()).IsTrue();
    }

    [Fact(DisplayName = "--report markdown writes a PR-comment-ready document.")]
    public void ReportMarkdownWritesMarkdown() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A", "B"));

        (int exit, string output, RecordingLogger _) = RunDiff(new RecordingSnapshotSource(Snapshot("A")), new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline,
            Report       = "md"
        });

        Check.That(exit).IsEqualTo(2);
        Check.That(output).StartsWith("## Error catalog changes");
    }

    [Fact(DisplayName = "An invalid --fail-on fails fast (exit 1) before any extraction.")]
    public void InvalidFailOnFailsFast() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A"));
        RecordingSnapshotSource source = new(Snapshot("A"));

        (int exit, string _, RecordingLogger logger) = RunDiff(source, new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline,
            FailOn       = "bogus"
        });

        Check.That(exit).IsEqualTo(1);
        Check.That(source.WasInvoked).IsFalse();
        Check.That(string.Join("\n", logger.Errors)).Contains("--fail-on");
    }

    [Fact(DisplayName = "--against compares a snapshot file instead of extracting from the source.")]
    public void AgainstComparesAFileNotTheSource() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        string against  = dir.File("current.json");
        BaselineStore.Save(baseline, Snapshot("A"));
        BaselineStore.Save(against, Snapshot("A"));
        RecordingSnapshotSource source = new(Snapshot("A"));

        (int exit, string output, RecordingLogger _) = RunDiff(source, new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline,
            AgainstPath  = against
        });

        Check.That(exit).IsEqualTo(0);
        Check.That(output).Contains("No catalog changes.");
        Check.That(source.WasInvoked).IsFalse();
    }

    [Fact(DisplayName = "Cancellation during extraction exits 130.")]
    public void CancellationExitsOneThirty() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A"));

        (int exit, string _, RecordingLogger __) = RunDiff(new CancellingSnapshotSource(), new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline
        });

        Check.That(exit).IsEqualTo(130);
    }

    [Fact(DisplayName = "An extraction failure is reported tersely (exit 1).")]
    public void ExtractionFailureExitsOne() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A"));

        (int exit, string _, RecordingLogger logger) = RunDiff(new FailingSnapshotSource(new InvalidOperationException("boom")), new CatalogDiffSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline
        });

        Check.That(exit).IsEqualTo(1);
        Check.That(string.Join("\n", logger.Errors)).Contains("boom");
    }

    private static (int Exit, string Output, RecordingLogger Logger) RunDiff(ICatalogSnapshotSource source, CatalogDiffSettings settings) {
        StringWriter    output = new();
        RecordingLogger logger = new();
        int             exit   = new CatalogDiffCommand(source, _ => logger, output).Run(settings, CancellationToken.None);

        return (exit, output.ToString(), logger);
    }

    private static CatalogSnapshot Snapshot(params string[] codes) {
        return new CatalogSnapshot {
            Errors = codes.Select(code => new CatalogSnapshotEntry { Code = code, Title = code }).ToList()
        };
    }

}

/// <summary>
///     Drives the <c>catalog update</c> command end to end (real configuration and baseline stores, fake snapshot
///     source, string writer) — baseline creation, idempotency, refresh with a change summary, self-healing over an
///     unreadable baseline, config-relative baseline resolution, and cancellation.
/// </summary>
[TestSubject(typeof(CatalogUpdateCommand))]
public sealed class CatalogUpdateCommandEndToEndTests {

    [Fact(DisplayName = "A first run creates the baseline file and reports it.")]
    public void FirstRunCreatesTheBaseline() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");

        (int exit, string output, RecordingLogger _) = RunUpdate(new RecordingSnapshotSource(Snapshot("A", "B")), new CatalogUpdateSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline
        });

        Check.That(exit).IsEqualTo(0);
        Check.That(output).Contains("Baseline created");
        Check.That(File.Exists(baseline)).IsTrue();
    }

    [Fact(DisplayName = "A second run with the same catalog reports the baseline is already up to date.")]
    public void SecondRunIsUpToDate() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A"));

        (int exit, string output, RecordingLogger _) = RunUpdate(new RecordingSnapshotSource(Snapshot("A")), new CatalogUpdateSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline
        });

        Check.That(exit).IsEqualTo(0);
        Check.That(output).Contains("already up to date");
    }

    [Fact(DisplayName = "A changed catalog rewrites the baseline and summarizes the accepted change.")]
    public void ChangedCatalogRewritesAndSummarizes() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        BaselineStore.Save(baseline, Snapshot("A", "B"));

        (int exit, string output, RecordingLogger _) = RunUpdate(new RecordingSnapshotSource(Snapshot("A")), new CatalogUpdateSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline
        });

        Check.That(exit).IsEqualTo(0);
        Check.That(output).Contains("1 breaking");
        Check.That(File.ReadAllText(baseline)).IsEqualTo(CatalogSnapshotSerializer.Serialize(Snapshot("A")));
    }

    [Fact(DisplayName = "An unreadable existing baseline is rewritten with a warning, not left broken.")]
    public void UnreadableBaselineIsRewritten() {
        using TempDir dir = new();
        string baseline = dir.File("errors-baseline.json");
        File.WriteAllText(baseline, "this is not a valid snapshot");

        (int exit, string output, RecordingLogger logger) = RunUpdate(new RecordingSnapshotSource(Snapshot("A")), new CatalogUpdateSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = baseline
        });

        Check.That(exit).IsEqualTo(0);
        Check.That(string.Join("\n", logger.Warnings)).Contains("could not be read");
        Check.That(output).Contains("rewritten");
        // The file is now a valid, canonical snapshot.
        Check.That(File.ReadAllText(baseline)).IsEqualTo(CatalogSnapshotSerializer.Serialize(Snapshot("A")));
    }

    [Fact(DisplayName = "A configured baseline resolves relative to the configuration file, not the current directory.")]
    public void ConfiguredBaselineResolvesRelativeToConfig() {
        using TempDir dir = new();
        string configPath = dir.File("fce.json");
        File.WriteAllText(configPath, """{ "baseline": "errors-baseline.json" }""");

        (int exit, string _, RecordingLogger __) = RunUpdate(new RecordingSnapshotSource(Snapshot("A")), new CatalogUpdateSettings {
            ConfigPath = configPath
        });

        Check.That(exit).IsEqualTo(0);
        Check.That(File.Exists(dir.File("errors-baseline.json"))).IsTrue();
        Check.That(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "errors-baseline.json"))).IsFalse();
    }

    [Fact(DisplayName = "Cancellation during extraction exits 130.")]
    public void CancellationExitsOneThirty() {
        using TempDir dir = new();

        (int exit, string _, RecordingLogger __) = RunUpdate(new CancellingSnapshotSource(), new CatalogUpdateSettings {
            ConfigPath   = CliTestHelpers.NonExistentConfigPath(),
            BaselinePath = dir.File("errors-baseline.json")
        });

        Check.That(exit).IsEqualTo(130);
    }

    private static (int Exit, string Output, RecordingLogger Logger) RunUpdate(ICatalogSnapshotSource source, CatalogUpdateSettings settings) {
        StringWriter    output = new();
        RecordingLogger logger = new();
        int             exit   = new CatalogUpdateCommand(source, _ => logger, output).Run(settings, CancellationToken.None);

        return (exit, output.ToString(), logger);
    }

    private static CatalogSnapshot Snapshot(params string[] codes) {
        return new CatalogSnapshot {
            Errors = codes.Select(code => new CatalogSnapshotEntry { Code = code, Title = code }).ToList()
        };
    }

}

/// <summary>A throwaway temp directory that cleans itself up, for tests that touch real baseline files.</summary>
internal sealed class TempDir : IDisposable {

    private readonly string _path;

    public TempDir() {
        _path = Path.Combine(Path.GetTempPath(), $"fce-catalog-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_path);
    }

    public string File(string name) {
        return Path.Combine(_path, name);
    }

    public void Dispose() {
        try {
            Directory.Delete(_path, recursive: true);
        } catch (IOException) {
            // Best-effort cleanup; a leaked temp directory must never fail a test.
        }
    }

}
