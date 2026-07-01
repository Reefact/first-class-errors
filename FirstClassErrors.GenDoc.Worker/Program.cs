#region Usings declarations

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using FirstClassErrors.GenDoc;
using FirstClassErrors.GenDoc.Worker;

#endregion

// Extracts the documentation model from a single target assembly, in a dedicated process.
//
// Usage:  FirstClassErrors.GenDoc.Worker <assembly-path> [output-json-path]
//
// The worker is meant to be launched by the generator against the target's own dependency closure
// (dotnet exec --depsfile <target>.deps.json ... worker.dll <target>.dll), so it binds to the target's
// FirstClassErrors version and starts from a fresh static registry. It writes an ExtractionResultDto as JSON to
// the output file when provided, otherwise to stdout; diagnostics and fatal errors go to stderr.
//
// Exit codes: 0 = success, 1 = fatal extraction error, 2 = bad usage.

if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0])) {
    Console.Error.WriteLine("Usage: FirstClassErrors.GenDoc.Worker <assembly-path> [output-json-path]");

    return 2;
}

string  assemblyPath = args[0];
string? outputPath   = args.Length > 1 ? args[1] : null;

try {
    Assembly                           assembly = Assembly.LoadFrom(assemblyPath);
    ErrorDocumentationExtractionResult result   = AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly);
    ExtractionResultDto                dto      = ExtractionResultDto.From(result);

    JsonSerializerOptions options = new() {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented          = false
    };

    string json = JsonSerializer.Serialize(dto, options);

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
