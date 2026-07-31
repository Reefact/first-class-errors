#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

/// <summary>
///     The exit codes the entry point answers with for a command line that never reaches a command. They are part of
///     the tool's published contract (decision: ADR-0067), and the parse path is the one a command's own tests cannot
///     reach — it fails before any command is constructed.
/// </summary>
[TestSubject(typeof(CliApplication))]
public sealed class CliApplicationExitCodeTests {

    #region Statics members declarations

    private static (int exitCode, string error) Run(params string[] args) {
        TextWriter   original = Console.Error;
        StringWriter captured = new();
        try {
            Console.SetError(captured);

            return (CliApplication.RunAsync(args).GetAwaiter().GetResult(), captured.ToString());
        } finally {
            Console.SetError(original);
        }
    }

    #endregion

    [Fact(DisplayName = "An unknown command is a usage error (64), reported on standard error.")]
    public void AnUnknownCommandIsAUsageError() {
        // Exercise
        (int exitCode, string error) = Run("frobnicate");

        // Verify: the code says "this invocation is wrong", and the run says so rather than exiting silently.
        Check.That(exitCode).IsEqualTo(64);
        Check.That(error).Contains("frobnicate");
        Check.That(error).Contains("fce --help");
    }

    [Fact(DisplayName = "An unknown command inside a branch is a usage error (64) too.")]
    public void AnUnknownCommandInsideABranchIsAUsageError() {
        // Exercise
        (int exitCode, string error) = Run("catalog", "frobnicate");

        // Verify
        Check.That(exitCode).IsEqualTo(64);
        Check.That(error).Contains("frobnicate");
    }

    [Fact(DisplayName = "An option given without its value is a usage error (64).")]
    public void AnOptionWithoutItsValueIsAUsageError() {
        // Exercise
        (int exitCode, string error) = Run("generate", "--solution");

        // Verify
        Check.That(exitCode).IsEqualTo(64);
        Check.That(error).Contains("solution");
    }

    // The regression this pins: before the handler existed, the parser's own failure path returned -1 — a value in no
    // exit-code table — and wrote nothing to either stream, so a mistyped command produced nothing at all to read.
    [Fact(DisplayName = "A refused command line never exits with the parser's own -1, silently.")]
    public void ARefusedCommandLineNeverExitsWithMinusOne() {
        // Exercise
        (int exitCode, string error) = Run("frobnicate");

        // Verify
        Check.That(exitCode).IsNotEqualTo(-1);
        Check.That(error).IsNotEmpty();
    }

    [Fact(DisplayName = "Asking for help is not a usage error: it succeeds (0).")]
    public void AskingForHelpSucceeds() {
        // Exercise
        (int exitCode, string _) = Run("--help");

        // Verify: --help is what the usage error tells the caller to run, so it must not itself be an error.
        Check.That(exitCode).IsEqualTo(0);
    }

    [Fact(DisplayName = "The entry point rejects a null argument array.")]
    public void RejectsANullArgumentArray() {
        Check.ThatCode(() => CliApplication.RunAsync(null!)).Throws<ArgumentNullException>();
    }

}
