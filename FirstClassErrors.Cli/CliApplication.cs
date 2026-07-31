#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using Spectre.Console.Cli;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Builds and runs the <c>fce</c> command tree. It exists as a type rather than as the body of
///     <c>Program.cs</c> so the entry point can be exercised by a test: a top-level program's statements are
///     reachable only by launching a process, and the exit code a bad command line produces is part of the tool's
///     published contract (decision: ADR-0067).
/// </summary>
/// <remarks>
///     The command tree's own failures are reported inside each command, which returns
///     <see cref="ExitCodes.Failure" /> after a terse line. What reaches the handler here is what the commands never
///     see: a command line the parser refused, so no command ever ran.
/// </remarks>
internal static class CliApplication {

    #region Statics members declarations

    /// <summary>
    ///     Runs the command tree over <paramref name="args" />.
    /// </summary>
    /// <param name="args">The process arguments, as given on the command line.</param>
    /// <returns>The exit code, always one of <see cref="ExitCodes" />.</returns>
    internal static Task<int> RunAsync(string[] args) {
        if (args is null) { throw new ArgumentNullException(nameof(args)); }

        CommandApp app = new();
        app.Configure(Configure);

        return app.RunAsync(args);
    }

    private static void Configure(IConfigurator config) {
        config.SetApplicationName("fce");
        config.SetExceptionHandler(HandleUncaught);

        // An argument the command tree does not declare is refused rather than collected. The parser gathers such a
        // token into the remaining arguments, which this tool never reads: a mistyped flag was accepted, ignored, and
        // reported as a success, so a pipeline asking for something the tool does not do was told it had it.
        //
        // The parser's own strict mode (UseStrictParsing) would say the same thing, and cannot be used: in
        // Spectre.Console.Cli 0.55 it makes an option declared without a value swallow the internal
        // "__default_command" token as that value, so `fce generate --solution` looks for a file by that name instead
        // of reporting a usage error. Refusing the leftovers ourselves keeps the diagnosis and leaves the parser's
        // handling of a missing value intact.
        config.SetInterceptor(new RefuseUndeclaredArguments());

        config.AddCommand<GenerateCommand>("generate")
              .WithDescription("Generate error documentation from a solution or from assemblies.");

        config.AddBranch<CommandSettings>("catalog", catalog => {
            catalog.SetDescription("Track the error catalog as a versioned contract (baseline + diff).");
            catalog.AddCommand<CatalogUpdateCommand>("update").WithDescription("Create or refresh the catalog baseline (deliberately accept the current contract).");
            catalog.AddCommand<CatalogDiffCommand>("diff").WithDescription("Compare the current catalog against the baseline and report the changes.");
        });

        config.AddBranch<CommandSettings>("config", configuration => {
            configuration.SetDescription("Manage the configuration file (fce.json).");
            configuration.AddCommand<InitCommand>("init").WithDescription("Create the configuration file.");
            configuration.AddCommand<ConfigShowCommand>("show").WithDescription("Print the current configuration.");

            configuration.AddBranch<CommandSettings>("renderer", renderer => {
                renderer.SetDescription("Manage the custom renderer libraries referenced by the configuration.");
                renderer.AddCommand<RendererAddCommand>("add").WithDescription("Register a renderer library.");
                renderer.AddCommand<RendererRemoveCommand>("remove").WithDescription("Unregister a renderer library.");
                renderer.AddCommand<RendererListCommand>("list").WithDescription("List available renderers (built-in and configured).");
            });
        });
    }

    /// <summary>
    ///     Answers for whatever escapes the command tree, and keeps that answer inside <see cref="ExitCodes" />.
    /// </summary>
    /// <remarks>
    ///     Without a handler the parser's own failure path returned <c>-1</c> — a value in no exit-code table, and
    ///     silent on both streams, so a mistyped command produced nothing at all to read. A wrong command line is a
    ///     usage error, which <see cref="ExitCodes.UsageError" /> names; anything else reaching here is a failure the
    ///     commands did not catch, and it reports as one rather than borrowing the usage code.
    /// </remarks>
    private static int HandleUncaught(Exception exception, ITypeResolver? resolver) {
        _ = resolver;
        bool usage = exception is CommandParseException or CommandTemplateException or CommandConfigurationException
                                  or UndeclaredArgumentException;

        Console.Error.WriteLine($"error: {exception.Message}");
        if (usage) { Console.Error.WriteLine("Run 'fce --help' to see the available commands."); }

        return usage ? ExitCodes.UsageError : ExitCodes.Failure;
    }

    #endregion

    /// <summary>
    ///     Refuses a command line carrying an argument no command declares, before the command runs.
    /// </summary>
    private sealed class RefuseUndeclaredArguments : ICommandInterceptor {

        /// <inheritdoc />
        public void Intercept(CommandContext context, CommandSettings settings) {
            if (context is null) { throw new ArgumentNullException(nameof(context)); }

            IReadOnlyList<string> undeclared = [.. context.Remaining.Raw, .. context.Remaining.Parsed.Select(pair => pair.Key)];
            if (undeclared.Count == 0) { return; }

            throw new UndeclaredArgumentException(undeclared[0]);
        }

    }

}

/// <summary>
///     Raised when the command line carries an argument the command tree does not declare. It is the tool's own
///     usage refusal rather than the parser's, so it names the offending argument and nothing else.
/// </summary>
[SuppressMessage("Minor Code Smell", "S3871:Exception types should be \"public\"",
                 Justification =
                     "The rule exists so a caller outside the assembly can catch the exception. This assembly is an " +
                     "executable: nothing references it, and the only code that catches this is the exit-code handler " +
                     "a few lines above. Making it public would advertise a type to callers that cannot exist.")]
internal sealed class UndeclaredArgumentException : Exception {

    internal UndeclaredArgumentException(string argument) : base($"Unknown argument '{argument}'.") { }

}
