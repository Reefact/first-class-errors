#region Usings declarations

using System.Text.Json;
using System.Text.Json.Serialization;

#endregion

namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     Renders the catalog as a curated, stable JSON document — a clean public schema of the documentation itself,
///     distinct from the internal extraction/transport model (no extraction failures, no computed flags).
/// </summary>
/// <remarks>
///     The output is deterministic (no timestamps) so it diffs cleanly in source control and CI. Enum values are
///     written as their names and property names are camelCase.
/// </remarks>
public sealed class JsonErrorDocumentationRenderer : IErrorDocumentationRenderer {

    #region Statics members declarations

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    #endregion

    /// <inheritdoc />
    public string Format => "json";

    /// <inheritdoc />
    public IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog) {
        if (catalog is null) { throw new ArgumentNullException(nameof(catalog)); }

        // A curated projection: the anonymous shape fixes exactly which fields are published, their camelCase names,
        // and enum-as-string, without exposing the internal model or maintaining parallel DTO types.
        var document = new {
            schemaVersion = "1.0",
            errors = catalog.Select(error => new {
                code         = error.Code,
                title        = error.Title,
                explanation  = error.Explanation,
                businessRule = error.BusinessRule,
                source       = error.Source,
                diagnostics = error.Diagnostics.Select(diagnostic => new {
                    possibleCause = diagnostic.PossibleCause,
                    origin        = diagnostic.Origin.ToString(),
                    analysisHint  = diagnostic.AnalysisHint
                }),
                examples = error.Examples.Select(example => new {
                    detailedMessage = example.DetailedMessage,
                    shortMessage    = example.ShortMessage
                }),
                context = error.Context.Select(entry => new {
                    key           = entry.Key,
                    valueType     = entry.ValueType,
                    description   = entry.Description,
                    exampleValues = entry.ExampleValues
                })
            })
        };

        string json = JsonSerializer.Serialize(document, SerializerOptions);

        return [new RenderedDocument("errors.json", json)];
    }

}
