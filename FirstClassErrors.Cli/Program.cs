#region Usings declarations

using FirstClassErrors.Cli;

using Spectre.Console.Cli;

#endregion

CommandApp app = new();

app.Configure(config => {
    config.SetApplicationName("fce");

    config.AddCommand<GenerateCommand>("generate")
          .WithDescription("Generate error documentation from a solution or from assemblies.");

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
});

// Spectre handles argument parsing, validation errors and --help. Runtime failures are handled inside each command
// so the tool reports them as a terse "error: …" line rather than a stack trace.
return app.Run(args);
