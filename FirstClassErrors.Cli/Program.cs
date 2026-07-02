#region Usings declarations

using FirstClassErrors.Cli;

using Spectre.Console.Cli;

#endregion

CommandApp<GenerateCommand> app = new();

app.Configure(config => config.SetApplicationName("fce-gendoc"));

// Spectre handles argument parsing, validation errors and --help. Runtime (generation) failures are handled
// inside the command so the tool reports them as a terse "error: …" line rather than a stack trace.
return app.Run(args);
