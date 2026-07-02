#region Usings declarations

using System.Globalization;
using System.Reflection;
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

string? assemblyPath = null;
string? outputPath   = null;
string? cultureName  = null;

for (int index = 0; index < args.Length; index++) {
    string arg = args[index];

    if (string.Equals(arg, "--culture", StringComparison.Ordinal)) {
        if (index + 1 >= args.Length) {
            Console.Error.WriteLine("Missing value for --culture.");

            return 2;
        }

        cultureName = args[++index];

        continue;
    }

    if (assemblyPath is null) {
        assemblyPath = arg;
    } else if (outputPath is null) {
        outputPath = arg;
    }
}

if (string.IsNullOrWhiteSpace(assemblyPath)) {
    Console.Error.WriteLine("Usage: FirstClassErrors.GenDoc.Worker <assembly-path> [output-json-path] [--culture <name>]");

    return 2;
}

if (cultureName is not null) {
    try {
        CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture              = culture;
        CultureInfo.CurrentUICulture            = culture;
        CultureInfo.DefaultThreadCurrentCulture   = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    } catch (CultureNotFoundException) {
        Console.Error.WriteLine($"Unknown culture '{cultureName}'.");

        return 2;
    }
}

try {
    Assembly                           assembly = Assembly.LoadFrom(assemblyPath);
    ErrorDocumentationExtractionResult result   = AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly);

    JsonSerializerOptions options = new() {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented          = false
    };

    string json = JsonSerializer.Serialize(result, options);

    if (outputPath is null) {
        Console.Out.Write(json);
    } else {
        File.WriteAllText(outputPath, json);
    }

    return 0;
} catch (Exception ex) {
    Console.Error.WriteLine($"Fatal error while extracting documentation from '{assemblyPath}': {ex}");

    return 1;
}
