# Architecture du pipeline de documentation

FirstClassErrors ne considère pas la documentation comme un artefact externe.  
La documentation est dérivée directement du code et circule à travers un pipeline structuré.

Le pipeline sépare la **définition de la connaissance**, **l’extraction** et le **rendu**.

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

Cet attribut est le point d’ancrage principal du modèle de documentation : il marque la classe comme source d’erreurs et fournit `ErrorDocumentation.Source` (le nom du modèle passé via `nameof(...)`).

À l’intérieur de cette classe, chaque méthode factory est liée à sa méthode de documentation via :

```csharp
[DocumentedBy(nameof(BelowAbsoluteZeroDocumentation))]
```

Cela crée une connexion explicite entre :

* la manière dont une erreur est créée
* la manière dont elle est décrite

## 🔎 3. Analyse des assemblies

`AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly)` analyse un assembly et :

* trouve toute classe annotée avec `[ProvidesErrorsFor(...)]` (ce sont de simples classes statiques, pas des types d’exception)
* trouve les méthodes factory marquées avec `[DocumentedBy]`
* invoque les méthodes de documentation liées
* construit une collection d’objets `ErrorDocumentation` (dédupliquée par `Code`, ordonnée par `Code`)

À ce stade, la documentation devient un modèle structuré en mémoire.

## 🧩 4. Agrégation au niveau de la solution

`SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(solutionPath[, options])` travaille à un niveau plus élevé et :

* compile une solution
* charge tous les assemblies
* agrège tous les `ErrorDocumentation` extraits (dédupliqués par `Code`, ordonnés par `Code`)

Cela produit un **catalogue global des erreurs** pour l’application ou le système.

## 🖨️ 5. Transformation vers des formats de sortie

L’exporteur transforme le modèle structuré en mémoire en documentation publiée. Le modèle étant une simple donnée, l’exporteur le transforme en :

* Markdown
* HTML
* JSON
* ou tout autre format

Cette couche de transformation est indépendante du modèle central.

## 🧰 6. Orchestration via CLI

La CLI `errdocgen` orchestre l’ensemble du processus, par exemple :

```bash
errdocgen --solution ./MyApp.sln --export html
```

La CLI gère :

* la compilation de la solution
* le chargement des assemblies
* l’extraction
* la transformation
* l’export

## 🔁 Pourquoi cette architecture est importante

Cette séparation garantit :

| Couche             | Responsabilité                       |
| ------------------ | ------------------------------------ |
| Code               | Définir la connaissance des erreurs  |
| Reader             | Extraire la documentation structurée |
| Builder            | Agréger à travers les assemblies     |
| Exporter           | Générer la documentation             |
| CLI                | Orchestrer le processus              |

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

Section précédente: [Intégration CI/CD et exploitation](OperationalIntegration.fr.md) | Section suivante: [FAQ](FAQ.fr.md)

---