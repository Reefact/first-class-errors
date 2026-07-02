# Écrire son propre renderer

Les formats intégrés `json` et `markdown` couvrent les cas courants, mais n’importe quel format de sortie — HTML, CSV, un gabarit de documentation maison — peut être ajouté sous forme de **renderer personnalisé**. Un renderer ne dépend que du modèle de documentation, pas de la façon dont le catalogue a été produit : en écrire un est donc court et autonome.

## Le contrat

Un renderer implémente `IErrorDocumentationRenderer` (fourni dans le package `FirstClassErrors`, namespace `FirstClassErrors.GenDoc.Rendering`) :

```csharp
public interface IErrorDocumentationRenderer {
    // La valeur choisie avec `fce generate --format <…>`.
    string Format { get; }

    // Les layouts que ce renderer sait produire, ex. "single", "split" (voir RenderLayouts).
    IReadOnlyCollection<string> SupportedLayouts { get; }

    // Transforme le catalogue en un ou plusieurs fichiers de sortie pour le layout et la culture demandés.
    IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog, RenderRequest request);
}
```

`RenderedDocument` est un couple `(RelativePath, Content)`. Renvoyez un seul document pour un format mono-fichier, ou plusieurs (un index plus un fichier par erreur) pour un format multi-fichiers — le `RelativePath` sert de nom de fichier lorsque la sortie est un dossier.

`RenderRequest` porte les deux choix propres à chaque appel :

* **`Layout`** — la valeur de `fce generate --layout <…>`. Déclarez les layouts pris en charge dans `SupportedLayouts` et rejetez tout autre avec `LayoutNotSupportedException` (le renderer intégré `json` ne gère que `single` ; `markdown` gère `single` et `split`). Un layout est une simple chaîne : un renderer peut donc définir les siens.
* **`Culture`** — la langue cible (`fce generate --language <…>`). Localisez le texte fixe que vous produisez pour `request.Culture` ; le renderer Markdown intégré lit ainsi ses titres et libellés depuis des ressources indexées par cette culture. Le *contenu* des erreurs est déjà localisé en amont par l’extracteur : un renderer ne localise donc que son propre gabarit.

Le contrat et le modèle (`ErrorDocumentation`, `ErrorDiagnostic`, …) sont livrés dans le package `FirstClassErrors`, qui cible **.NET Standard 2.0** — un renderer n’a donc besoin que de cette seule référence, que la plupart des projets possèdent déjà.

## Un exemple minimal

```csharp
using System.Linq;

using FirstClassErrors;
using FirstClassErrors.GenDoc.Rendering;

public sealed class CsvErrorDocumentationRenderer : IErrorDocumentationRenderer {

    public string Format => "csv";

    // Un seul fichier CSV — ce renderer ne gère que le layout « single ».
    public IReadOnlyCollection<string> SupportedLayouts { get; } = new[] { RenderLayouts.Single };

    public IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog, RenderRequest request) {
        if (!SupportedLayouts.Contains(request.Layout, StringComparer.OrdinalIgnoreCase)) {
            throw new LayoutNotSupportedException(Format, request.Layout, SupportedLayouts);
        }

        var rows    = catalog.Select(error => $"{error.Code},{Quote(error.Title)}");
        var content = "code,title\n" + string.Join("\n", rows);

        return new[] { new RenderedDocument("errors.csv", content) };
    }

    private static string Quote(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}
```

C’est un renderer complet. (Ce CSV n’a aucun texte fixe à traduire ; un renderer qui émet des titres ou des libellés les lirait depuis des ressources indexées par `request.Culture`.)

## Le brancher sur la CLI

Compilez votre renderer dans une bibliothèque, puis enregistrez-le :

```bash
fce config renderer add ./plugins/MyCompany.Renderers.dll
fce generate --solution MyApp.sln --format csv --output errors.csv
```

`fce config renderer add` inscrit le chemin de la bibliothèque dans `fce.json` (vous pouvez aussi éditer le fichier à la main). Au moment de la génération, la CLI charge les assemblies référencées, y découvre chaque renderer public doté d’un constructeur sans paramètre, et sélectionne celui dont le `Format` correspond à `--format`. `fce config renderer list` affiche les formats intégrés et configurés, et un `--format` inconnu liste ce qui est disponible.

```json
{
  "renderers": ["./plugins/MyCompany.Renderers.dll"]
}
```

Les chemins sont absolus ou relatifs à `fce.json`, de sorte qu’une configuration est portable avec ses plugins.

### À savoir

* **Constructeur sans paramètre** — la CLI instancie les renderers par réflexion.
* **Contrat partagé** — référencez `FirstClassErrors`, mais ne livrez pas votre propre copie de cet assembly à côté de la CLI : le type du renderer doit se résoudre vers l’assembly de contrat de la CLI. Référencez-le sans le copier (par ex. `<Private>false</Private>` sur la référence), ou appuyez-vous sur la version identique déjà présente à côté de la CLI.
* **Framework cible** — la CLI charge le plugin dans son propre processus ; compilez-le pour un framework qu’elle peut charger.
* **Les intégrés gagnent les égalités** — si un renderer personnalisé déclare `json` ou `markdown`, c’est l’intégré qui est utilisé.
* **Les échecs sont tolérés** — un plugin impossible à charger est signalé par un avertissement et ignoré ; il n’interrompt pas la génération.

## Utiliser un renderer sans la CLI

La CLI est optionnelle — un renderer n’est qu’une classe. Si vous obtenez un catalogue vous-même (par exemple via `SolutionErrorDocumentationGenerator`, dans `FirstClassErrors.GenDoc`), le rendre se résume à :

```csharp
IEnumerable<ErrorDocumentation> catalog =
    SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom("MyApp.sln", new SolutionGenerationOptions());

RenderRequest request = new(RenderLayouts.Single, CultureInfo.GetCultureInfo("fr"));
foreach (RenderedDocument document in new CsvErrorDocumentationRenderer().Render(catalog, request)) {
    File.WriteAllText(document.RelativePath, document.Content);
}
```

---

Section précédente: [Architecture du pipeline de documentation](ArchitectureOfTheDocumentationPipeline.fr.md) | Section suivante: [FAQ](FAQ.fr.md)

---
