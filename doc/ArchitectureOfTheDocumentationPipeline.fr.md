# Architecture du pipeline de documentation

DiagnosableExceptions ne considère pas la documentation comme un artefact externe.  
La documentation est dérivée directement du code et circule à travers un pipeline structuré.

Le pipeline sépare la **définition de la connaissance**, **l’extraction** et le **rendu**.

## 🧱 1. La connaissance vit dans le code

La connaissance liée aux erreurs est écrite à l’endroit où les erreurs sont définies :

* Les types d’exception représentent des catégories d’erreurs  
* Les méthodes factory représentent des situations d’erreur spécifiques  
* Le DSL `DescribeError` décrit le sens, les règles, les diagnostics et les exemples  

À ce stade, la documentation est une **donnée structurée**, pas des fichiers texte.

## 🔗 2. Les factories sont liées à la documentation

Chaque méthode factory est liée à sa documentation via :

```csharp
[DocumentedBy(nameof(CurrencyMismatchDocumentation))]
```

Cela crée une connexion explicite entre :

* la manière dont une erreur est créée
* la manière dont elle est décrite

Les factories deviennent les points d’ancrage du modèle de documentation.

## 🔎 3. Analyse des assemblies

`AssemblyErrorDocumentationReader` analyse les assemblies et :

* trouve les types d’exception dérivant de `DiagnosableException`
* trouve les méthodes factory marquées avec `[DocumentedBy]`
* invoque les méthodes de documentation
* construit une collection d’objets `ErrorDocumentation`

À ce stade, la documentation devient un modèle structuré en mémoire.

## 🧩 4. Agrégation au niveau de la solution

Un outil de plus haut niveau peut :

* compiler une solution
* charger tous les assemblies
* agréger tous les `ErrorDocumentation` extraits

Cela produit un **catalogue global des erreurs** pour l’application ou le système.

## 🖨️ 5. Transformation vers des formats de sortie

Le modèle structuré peut être transformé en :

* Markdown
* HTML
* JSON
* ou tout autre format

La couche de transformation est indépendante du modèle central.

## 🧰 6. Orchestration via CLI

Un outil en ligne de commande peut orchestrer l’ensemble du processus :

```bash
errdocgen --solution ./MyApp.sln --export html
```

Il gère :

* la compilation de la solution
* le chargement des assemblies
* l’extraction
* la transformation
* l’export

## 🔁 Pourquoi cette architecture est importante

Cette séparation garantit :

| Couche   | Responsabilité                       |
| -------- | ------------------------------------ |
| Code     | Définir la connaissance des erreurs  |
| Reader   | Extraire la documentation structurée |
| Builder  | Agréger à travers les assemblies     |
| Exporter | Générer la documentation             |
| CLI      | Orchestrer le processus              |

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