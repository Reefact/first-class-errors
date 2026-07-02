#region Usings declarations

using System.Text.Json;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.Rendering.UnitTests;

[TestSubject(typeof(JsonErrorDocumentationRenderer))]
public sealed class JsonErrorDocumentationRendererTests {

    [Fact(DisplayName = "The JSON renderer declares the 'json' format.")]
    public void TheJsonRendererDeclaresTheJsonFormat() {
        // Exercise & verify
        Check.That(new JsonErrorDocumentationRenderer().Format).IsEqualTo("json");
    }

    [Fact(DisplayName = "The JSON renderer produces a curated camelCase catalog with enum values as strings.")]
    public void TheJsonRendererProducesACuratedCamelCaseCatalog() {
        // Setup
        ErrorDocumentation documentation = new() {
            Code         = "TEMPERATURE_BELOW_ABSOLUTE_ZERO",
            Title        = "Temperature below absolute zero",
            Explanation  = "A temperature was instantiated below absolute zero.",
            BusinessRule = "Temperature cannot go below absolute zero.",
            Source       = "Temperature",
            Diagnostics  = new[] { new ErrorDiagnostic("A value entered by a user is invalid.", ErrorOrigin.External, "Verify the user input.") },
            Examples     = new[] { new ErrorDescription("Failed to instantiate temperature: -300 is below absolute zero.", "Below absolute zero.") },
            Context = new[] {
                new ErrorContextEntryDocumentation {
                    Key           = "AttemptedValue",
                    ValueType     = "System.Double",
                    Description   = "The rejected value.",
                    ExampleValues = new[] { "-300" }
                }
            }
        };

        // Exercise
        IReadOnlyList<RenderedDocument> documents = new JsonErrorDocumentationRenderer().Render(new[] { documentation }, new RenderRequest(RenderLayouts.Single));

        // Verify
        Check.That(documents).HasSize(1);
        RenderedDocument document = documents[0];
        Check.That(document.RelativePath).IsEqualTo("errors.json");

        using JsonDocument parsed = JsonDocument.Parse(document.Content);
        JsonElement        root   = parsed.RootElement;

        JsonElement errors = root.GetProperty("errors");
        Check.That(errors.GetArrayLength()).IsEqualTo(1);

        JsonElement error = errors[0];
        Check.That(error.GetProperty("code").GetString()).IsEqualTo("TEMPERATURE_BELOW_ABSOLUTE_ZERO");
        Check.That(error.GetProperty("title").GetString()).IsEqualTo("Temperature below absolute zero");
        Check.That(error.GetProperty("source").GetString()).IsEqualTo("Temperature");

        Check.That(error.GetProperty("diagnostics")[0].GetProperty("possibleCause").GetString()).IsEqualTo("A value entered by a user is invalid.");
        Check.That(error.GetProperty("diagnostics")[0].GetProperty("origin").GetString()).IsEqualTo("External");

        Check.That(error.GetProperty("examples")[0].GetProperty("shortMessage").GetString()).IsEqualTo("Below absolute zero.");

        JsonElement contextEntry = error.GetProperty("context")[0];
        Check.That(contextEntry.GetProperty("key").GetString()).IsEqualTo("AttemptedValue");
        Check.That(contextEntry.GetProperty("valueType").GetString()).IsEqualTo("System.Double");
        Check.That(contextEntry.GetProperty("exampleValues")[0].GetString()).IsEqualTo("-300");

        // Curated: the internal extraction/transport members must NOT leak into the public JSON.
        Check.That(root.TryGetProperty("failures", out _)).IsFalse();
        Check.That(root.TryGetProperty("hasFailures", out _)).IsFalse();
        Check.That(error.TryGetProperty("hasFailures", out _)).IsFalse();
    }

    [Fact(DisplayName = "The JSON renderer guards against a null catalog.")]
    public void TheJsonRendererGuardsAgainstANullCatalog() {
        // Exercise & verify
        Check.ThatCode(() => new JsonErrorDocumentationRenderer().Render(null!, new RenderRequest(RenderLayouts.Single)))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The JSON renderer supports only the single layout.")]
    public void TheJsonRendererSupportsOnlyTheSingleLayout() {
        // Exercise & verify
        Check.That(new JsonErrorDocumentationRenderer().SupportedLayouts).ContainsExactly("single");
    }

    [Fact(DisplayName = "The JSON renderer rejects the split layout.")]
    public void TheJsonRendererRejectsTheSplitLayout() {
        // Setup
        ErrorDocumentation documentation = new() { Code = "CODE", Title = "Title" };

        // Exercise & verify
        Check.ThatCode(() => new JsonErrorDocumentationRenderer().Render(new[] { documentation }, new RenderRequest(RenderLayouts.Split)))
             .Throws<LayoutNotSupportedException>();
    }

}
