namespace FirstClassErrors.Cli;

/// <summary>
///     Parsed command-line arguments for the documentation generator. Hand-rolled so the tool carries no extra
///     dependency: the surface is small and stable, and a bespoke parser keeps the error messages precise.
/// </summary>
internal sealed class CliArguments {

    #region Statics members declarations

    public const string HelpText =
        """
        fce-gendoc — generate error documentation from a .NET solution or from built assemblies.

        Usage:
          fce-gendoc --solution <path> [options]
          fce-gendoc --assemblies <path> [<path>...] [options]

        Source (exactly one is required):
          -s, --solution <path>        Path to the .sln file to document.
          -a, --assemblies <path...>   One or more built assemblies (.dll) to document.

        Options:
          -o, --output <path>          Write the rendered document to this file (default: standard output).
          -f, --format <json>          Output format. Supported: json (default).
          -c, --configuration <name>   Build configuration used when building a solution (default: Debug).
              --framework <tfm>        Restrict a multi-target solution to a single target framework.
              --no-build               Do not build the solution; document the existing binaries.
              --strict                 Abort on the first extraction failure (default: continue and report).
              --worker <path>          Path to FirstClassErrors.GenDoc.Worker.dll (default: next to this tool).
          -v, --verbose                Emit diagnostic logging to standard error.
          -h, --help                   Show this help and exit.
        """;

    #endregion

    public string?      SolutionPath  { get; private set; }
    public List<string> AssemblyPaths { get; } = [];
    public string?      OutputPath    { get; private set; }
    public string       Format        { get; private set; } = "json";
    public string       Configuration { get; private set; } = "Debug";
    public string?      Framework     { get; private set; }
    public bool         Build         { get; private set; } = true;
    public bool         Strict        { get; private set; }
    public string?      WorkerPath    { get; private set; }
    public bool         Verbose       { get; private set; }
    public bool         ShowHelp      { get; private set; }

    /// <summary>
    ///     Parses the raw argument vector. Throws <see cref="ArgumentException" /> on an unknown option or a missing
    ///     value. When <c>--help</c> is present it short-circuits so callers can print usage without validating.
    /// </summary>
    public static CliArguments Parse(string[] args) {
        ArgumentNullException.ThrowIfNull(args);

        CliArguments parsed = new();

        for (int index = 0; index < args.Length; index++) {
            string argument = args[index];
            switch (argument) {
                case "--help" or "-h" or "-?":
                    parsed.ShowHelp = true;
                    return parsed; // Help wins over everything else, including otherwise-invalid arguments.
                case "--solution" or "-s":
                    parsed.SolutionPath = RequireValue(args, ref index, argument);
                    break;
                case "--assemblies" or "-a":
                    ReadAssemblyList(args, ref index, parsed.AssemblyPaths, argument);
                    break;
                case "--output" or "-o":
                    parsed.OutputPath = RequireValue(args, ref index, argument);
                    break;
                case "--format" or "-f":
                    parsed.Format = RequireValue(args, ref index, argument).ToLowerInvariant();
                    break;
                case "--configuration" or "-c":
                    parsed.Configuration = RequireValue(args, ref index, argument);
                    break;
                case "--framework":
                    parsed.Framework = RequireValue(args, ref index, argument);
                    break;
                case "--worker":
                    parsed.WorkerPath = RequireValue(args, ref index, argument);
                    break;
                case "--no-build":
                    parsed.Build = false;
                    break;
                case "--strict":
                    parsed.Strict = true;
                    break;
                case "--verbose" or "-v":
                    parsed.Verbose = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{argument}'. Run with --help for usage.");
            }
        }

        return parsed;
    }

    /// <summary>
    ///     Validates cross-argument rules: exactly one source (solution or assemblies) and a supported format.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the combination of arguments is invalid.</exception>
    public void Validate() {
        bool hasSolution   = string.IsNullOrWhiteSpace(SolutionPath) is false;
        bool hasAssemblies = AssemblyPaths.Count > 0;

        if (hasSolution && hasAssemblies) {
            throw new ArgumentException("Specify either --solution or --assemblies, not both.");
        }

        if (hasSolution is false && hasAssemblies is false) {
            throw new ArgumentException("A source is required: pass --solution <path> or --assemblies <path...>.");
        }

        if (Format != "json") {
            throw new ArgumentException($"Unsupported --format '{Format}'. Supported formats: json.");
        }
    }

    private static void ReadAssemblyList(string[] args, ref int index, List<string> destination, string flag) {
        int start = index;
        while (index + 1 < args.Length && IsValue(args[index + 1])) {
            destination.Add(args[++index]);
        }

        if (index == start) {
            throw new ArgumentException($"Option '{flag}' requires at least one assembly path.");
        }
    }

    private static string RequireValue(string[] args, ref int index, string flag) {
        if (index + 1 >= args.Length || IsValue(args[index + 1]) is false) {
            throw new ArgumentException($"Option '{flag}' requires a value.");
        }

        return args[++index];
    }

    private static bool IsValue(string token) {
        // A value is any token that is not an option. Options start with '-' (e.g. "--output", "-o").
        return token.StartsWith('-') is false;
    }

}
