#region Usings declarations

using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;

using FirstClassErrors.GenDoc;

#endregion

// Extracts the documentation model from a single target assembly, in a dedicated process.
//
// Usage:  FirstClassErrors.GenDoc.Worker <assembly-path> [output-json-path] [--culture <name>]
//
// The worker is meant to be launched by the generator against the target's own dependency closure
// (dotnet exec --depsfile <target>.deps.json ... worker.dll <target>.dll), so it binds to the target's
// FirstClassErrors version and starts from a fresh static registry. It writes the ErrorDocumentationExtractionResult
// as JSON to the output file when provided, otherwise to stdout; diagnostics and fatal errors go to stderr.
//
// When --culture is given, the extraction runs under that culture, so documentation factories that read localized
// resources produce their text in that language.
//
// Exit codes: 0 = success, 1 = fatal extraction error, 2 = bad usage.

(string? assemblyPath, string? outputPath, string? cultureName, string? parseError) = ParseArguments(args);

if (parseError is not null) {
    await Console.Error.WriteLineAsync(parseError);

    return 2;
}

if (string.IsNullOrWhiteSpace(assemblyPath)) {
    await Console.Error.WriteLineAsync("Usage: FirstClassErrors.GenDoc.Worker <assembly-path> [output-json-path] [--culture <name>]");

    return 2;
}

if (cultureName is not null) {
    try {
        CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture                = culture;
        CultureInfo.CurrentUICulture              = culture;
        CultureInfo.DefaultThreadCurrentCulture   = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    } catch (CultureNotFoundException) {
        await Console.Error.WriteLineAsync($"Unknown culture '{cultureName}'.");

        return 2;
    }
}

try {
    // Load into the default context (not Assembly.LoadFrom): the worker already runs under the target's
    // deps.json, so its dependency closure — the target's own FirstClassErrors included — resolves there.
    // LoadFromAssemblyPath requires an absolute path (LoadFrom resolved relative paths against the cwd itself).
    Assembly                           assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
    ErrorDocumentationExtractionResult result   = AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly);

    JsonSerializerOptions options = new() {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented          = false
    };

    string json = JsonSerializer.Serialize(result, options);

    if (outputPath is null) {
        await Console.Out.WriteAsync(json);
    } else {
        await File.WriteAllTextAsync(outputPath, json);
    }

    return 0;
} catch (Exception ex) {
    await Console.Error.WriteLineAsync($"Fatal error while extracting documentation from '{assemblyPath}': {ex}");

    return 1;
}

// Parses the positional <assembly-path> [output-json-path] and the optional --culture <name>. Returns the parsed
// values plus a non-null error message when --culture is given without a value. Kept as a local function so the
// top-level flow stays a straight-line sequence of guards.
static (string? assemblyPath, string? outputPath, string? cultureName, string? error) ParseArguments(string[] arguments) {
    string? assemblyPath = null;
    string? outputPath   = null;
    string? cultureName  = null;

    int index = 0;
    while (index < arguments.Length) {
        string arg = arguments[index];

        if (string.Equals(arg, "--culture", StringComparison.Ordinal)) {
            if (index + 1 >= arguments.Length) {
                return (null, null, null, "Missing value for --culture.");
            }

            cultureName = arguments[index + 1];
            index += 2;

            continue;
        }

        if (assemblyPath is null) {
            assemblyPath = arg;
        } else if (outputPath is null) {
            outputPath = arg;
        }

        index++;
    }

    return (assemblyPath, outputPath, cultureName, null);
}
