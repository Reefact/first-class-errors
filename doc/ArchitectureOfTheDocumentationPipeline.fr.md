# Architecture du pipeline de documentation

🌍 **Langues:**  
🇬🇧 [English](./ArchitectureOfTheDocumentationPipeline.en.md) | 🇫🇷 Français (ce fichier)

FirstClassErrors ne considère pas la documentation comme un artefact externe.  
La documentation est dérivée directement du code et circule à travers un pipeline structuré.

Le pipeline sépare la **définition de la connaissance**, l’**extraction** et le **rendu**.

## 🧱 1. La connaissance vit dans le code

La connaissance liée aux erreurs est écrite à l’endroit où les erreurs sont définies :

* Une classe statique annotée avec `[ProvidesErrorsFor(...)]` regroupe les erreurs liées à un modèle donné  
* Les sous-types d’`Error` (`DomainError`, `PrimaryPortError`, `SecondaryPortError`, ...) représentent des catégories d’erreurs  
* Les méthodes factory représentent des situations d’erreur spécifiques  
* Le DSL `DescribeError` décrit le sens, les règles, les diagnostics et les exemples  

À ce stade, la documentation est une **donnée structurée**, pas des fichiers texte.

## 🔗 2. Les erreurs sont ancrées et liées à la documentation

Une classe statique déclare qu’elle possède les erreurs d’un modèle donné :

```csharp
[ProvidesErrorsFor(nameof(Temperature))]
public static class InvalidTemperatureError { ... }
```

Cet attribut est le point d’ancrage principal du modèle de documentation : il marque la classe comme source d’erreurs et fournit `ErrorDocumentation.Source` (le nom du modèle passé via `nameof(...)`). Il peut aussi porter une `Description` optionnelle, rendue comme introduction au groupe de cette source dans la documentation générée :

```csharp
[ProvidesErrorsFor(nameof(Temperature),
                   Description = "Errors raised when constructing a Temperature value from an out-of-range input.")]
```

La `Description` est un texte littéral par défaut ; renseignez `DescriptionResourceType` pour qu’elle soit résolue comme une clé de ressource à la place, en vue de la localisation (voir [Internationalisation](Internationalisation.fr.md)).

À l’intérieur de cette classe, chaque méthode factory est liée à sa méthode de documentation via :

```csharp
[DocumentedBy(nameof(BelowAbsoluteZeroDocumentation))]
```

Cela crée une connexion explicite entre :

* la manière dont une erreur est créée
* la manière dont elle est décrite

## 🔎 3. Extraction

`AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly)` analyse un assembly et :

* trouve toute classe annotée avec `[ProvidesErrorsFor(...)]` (ce sont de simples classes statiques, pas des types d’exception)
* trouve les méthodes factory marquées avec `[DocumentedBy]`
* **invoque** les méthodes de documentation liées — ainsi que les factories d’exemples qu’elles référencent. La documentation est *exécutable* : les exemples reflètent le vrai code, et non une copie qui pourrait dériver. Une factory qui lève une exception, ou une référence `[DocumentedBy]` non résolvable, est enregistrée comme une *failure* au lieu d’interrompre toute l’analyse.
* renvoie un `ErrorDocumentationExtractionResult` : la collection d’`ErrorDocumentation` (dédupliquée par `Code`, ordonnée par `Code`) avec la liste des *failures* d’extraction

À ce stade, la documentation devient un modèle structuré en mémoire.

## 🧪 4. L’extraction s’exécute hors-processus

Parce que l’extraction **exécute** le code de la cible, chaque assembly est documenté par un **processus worker** éphémère, lancé par le générateur (`dotnet exec`, en s’appuyant sur le fichier de dépendances de la cible). On y gagne :

* des **exemples vivants** — les factories d’exemples s’exécutent contre le vrai code, pas une description figée
* un **registre statique neuf** par assembly — aucun état ne fuit d’un assembly à l’autre
* l’**isolation des versions** — chaque cible lie sa propre version de FirstClassErrors
* l’**isolation des pannes** — un assembly qui plante ou se bloque est tué sur *timeout* et enregistré comme une *failure*, sans faire tomber toute la génération

Le worker écrit son `ErrorDocumentationExtractionResult` en JSON ; le générateur le relit et passe à l’assembly suivant.

## 🧩 5. Agrégation au niveau de la solution

`SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(solutionPath[, options])` — ou `GetErrorDocumentationFromAssemblies(paths, options)` pour des binaires déjà compilés — travaille à un niveau plus élevé et :

* découvre les projets (via `dotnet sln list`), ne garde que ceux qui ont opté (voir plus bas) et, sauf indication contraire, les compile
* lance un worker pour chaque assembly de sortie
* agrège tous les `ErrorDocumentation` extraits (dédupliqués par `Code`, ordonnés par `Code`)

Cela produit un **catalogue global des erreurs** pour l’application ou le système.

### Activer un projet (opt-in)

La génération au niveau de la solution est **opt-in par projet** : un projet n’est documenté que si son fichier projet (`.csproj`) définit la propriété MSBuild

```xml
<PropertyGroup>
  <GenerateErrorDocumentation>true</GenerateErrorDocumentation>
</PropertyGroup>
```

Chaque projet découvert dans la solution est alors traité ainsi :

| `GenerateErrorDocumentation` | Résultat |
| ---------------------------- | -------- |
| `true`                       | documenté |
| absente                      | ignoré — le défaut est l’opt-in |
| `false`                      | toujours ignoré, même quand la politique « tout inclure » est active |

Cela limite le catalogue — et les workers lancés pour le produire — aux projets qui définissent réellement des erreurs applicatives, plutôt qu’à tous les projets de la solution.

La propriété est un **marqueur lu directement dans le fichier projet**, pas un interrupteur de build MSBuild : rien ne la consomme lors d’un simple `dotnet build`, et passer `-p:GenerateErrorDocumentation=…` sur une ligne de commande de build n’a aucun effet. Parce qu’elle est lue dans le XML du projet plutôt qu’évaluée par MSBuild, elle doit être déclarée littéralement dans le `.csproj` : une valeur héritée d’un `Directory.Build.props` partagé, apportée par un import, ou conditionnée par un `Condition` MSBuild n’est pas prise en compte. Le mode `--assemblies` n’est pas soumis à ce filtre : il documente exactement les binaires que vous nommez.

> Pour les appels programmatiques, `SolutionGenerationOptions` expose `OptInPropertyName` (renommer le marqueur) et `IncludeProjectsWithoutOptIn` (documenter tous les projets sans distinction). La CLI `fce` utilise les valeurs par défaut ci-dessus.

## 🖨️ 6. Rendu vers des formats de sortie

Un *renderer* transforme le catalogue en mémoire en documentation publiée. Le modèle étant une simple donnée, le rendu est découplé derrière un contrat unique :

```csharp
public interface IErrorDocumentationRenderer {
    string Format { get; }
    IReadOnlyCollection<string> SupportedLayouts { get; }
    IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog, RenderRequest request);
}
```

Chaque renderer déclare les layouts qu’il sait produire et on lui en demande un à chaque appel via le `RenderRequest` (qui porte aussi la culture cible) ; un layout non pris en charge est rejeté par une `LayoutNotSupportedException`. Trois renderers sont fournis d’origine :

* **json** — un schéma JSON curé et stable (layout `single` uniquement)
* **markdown** — un fichier unique, ou (avec `--layout split`) un index README plus un fichier par groupe de source et un fichier par erreur (`single`/`split`)
* **html** — un site statique autonome : une table des matières consultable et groupée par source et, en `split`, une page par erreur (`single`/`split`). Voir [Le renderer HTML](TheHtmlRenderer.fr.md).

Tout autre format (CSV, un gabarit maison, …) est un **renderer personnalisé** : implémentez l’interface et enregistrez-le. Voir [Écrire son propre renderer](WritingACustomRenderer.fr.md).

## 🧰 7. Orchestration via CLI

La CLI `fce` orchestre l’ensemble du processus :

```bash
fce generate --solution ./MyApp.sln --format markdown --layout split --output ./docs/errors
```

Elle gère la compilation de la solution, l’extraction (via les workers), l’agrégation et le rendu. Les options courantes peuvent être stockées dans un fichier de configuration (`fce.json`) pour ne pas les répéter, et les renderers personnalisés y sont aussi référencés :

```bash
fce config init
fce config renderer add ./plugins/MyCompany.Renderers.dll
fce generate            # utilise la solution, le format, l’output, les renderers configurés…
```

Une valeur passée en ligne de commande écrase la configuration.

## 🌍 8. Internationalisation

Le pipeline est sensible à la culture à deux niveaux : l’extracteur localise le *contenu* des erreurs (sous la culture UI demandée) et chaque renderer localise ses propres *gabarits* (depuis `RenderRequest.Culture`), tandis que les noms de fichiers et les ancres restent indépendants de la culture, pour que les liens ne cassent jamais. C’est optionnel et piloté par `fce generate --language`.

Voir **[Internationalisation](Internationalisation.fr.md)** pour le détail — choisir la langue, le hook `DescriptionResourceType`, la localisation des gabarits de renderer, et le pilotage sans la CLI.

## 🔁 Pourquoi cette architecture est importante

Cette séparation garantit :

| Couche      | Responsabilité                          |
| ----------- | --------------------------------------- |
| Code        | Définir la connaissance des erreurs     |
| Reader      | Extraire la documentation structurée    |
| Worker      | Exécuter le code de façon isolée        |
| Générateur  | Compiler et agréger à travers les assemblies |
| Renderer    | Transformer le catalogue en un format cible |
| CLI         | Orchestrer le processus                 |

La documentation reste :

* proche du code
* toujours à jour
* structurée
* exploitable par des outils

## 🎯 L’idée clé

> La documentation des erreurs n’est pas écrite *à propos* du système.
> Elle est dérivée *à partir* du système.

Le code est la source de vérité.

---

<div align="center">
<a href="OperationalIntegration.fr.md">← Intégration CI/CD et exploitation</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="WritingACustomRenderer.fr.md">Écrire son propre renderer →</a>
</div>

---
