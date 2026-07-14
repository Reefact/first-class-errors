# FirstClassErrors

🌍 **Langues:**  
🇬🇧 [English](../README.md) | 🇫🇷 Français (ce fichier)

|  |  |
| :-- | :-- |
| **Build**    | [![ci](https://github.com/Reefact/first-class-errors/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Reefact/first-class-errors/actions/workflows/ci.yml) |
| **Qualité**  | [![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=reefact_first-class-errors&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=reefact_first-class-errors) [![Couverture](https://sonarcloud.io/api/project_badges/measure?project=reefact_first-class-errors&metric=coverage)](https://sonarcloud.io/summary/new_code?id=reefact_first-class-errors) |
| **Sécurité** | [![codeql](https://github.com/Reefact/first-class-errors/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/Reefact/first-class-errors/actions/workflows/codeql.yml) [![OpenSSF Best Practices](https://www.bestpractices.dev/projects/13567/badge)](https://www.bestpractices.dev/projects/13567) [![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Reefact/first-class-errors/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Reefact/first-class-errors) |
| **Package**  | [![NuGet](https://img.shields.io/nuget/vpre/FirstClassErrors?logo=nuget)](https://www.nuget.org/packages/FirstClassErrors) ![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4) |
| **Projet**   | [![License](https://img.shields.io/github/license/Reefact/first-class-errors)](../LICENSE) [![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-fe5196?logo=conventionalcommits&logoColor=white)](https://www.conventionalcommits.org) |

---

**Transformez vos erreurs en connaissance structurée et vivante sur votre système.**

![FirstClassErrors](./images/first-class-errors.png "FirstClassErrors")

FirstClassErrors est une bibliothèque .NET qui considère les erreurs comme des concepts de premier ordre, documentés et diagnostiquables — pas seulement comme des chaînes de caractères lancées à l’exécution.

Elle vous aide à :

* exprimer les erreurs de manière cohérente et structurée
* associer des diagnostics utiles à chaque erreur
* garder la documentation des erreurs proche du code
* générer automatiquement une documentation humaine des erreurs

## 🚨 Le problème

Dans la plupart des systèmes, les erreurs sont :

* dispersées dans le code
* décrites par des messages ad hoc
* peu ou pas documentées
* difficiles à analyser
* déconnectées du support et des opérations

Avec le temps, cela entraîne :

* des investigations répétées
* du savoir implicite (“tribal knowledge”)
* des équipes support qui devinent
* des développeurs qui réexpliquent sans cesse les mêmes erreurs

## 💡 L’idée

Et si :

> **Chaque erreur de votre système était explicitement décrite, structurée et documentée — directement dans le code — et que cette documentation pouvait être générée automatiquement ?**

FirstClassErrors introduit :

* un **modèle d’erreur enrichi**
* un **système de diagnostics structurés**
* un **DSL pour documenter les erreurs**
* un **pipeline d’extraction de documentation**

Les erreurs deviennent :

> non seulement des échecs,
> mais des **unités de connaissance documentées**.

## 🧱 Ce que fournit la bibliothèque

### 1️⃣ Un modèle d’erreur plus riche

Les erreurs portent :

* un code d’erreur stable
* un horodatage
* trois messages distincts — un résumé public court (obligatoire), un détail public optionnel et un message de diagnostic interne (obligatoire)
* des données de contexte
* des diagnostics structurés

L’exception, elle, n’expose que son `.Error`, et est conçue pour être :

* loguées de manière cohérente
* comprises par des humains
* exploitées par des outils

#### Trois messages, trois publics

Une `Error` sépare délibérément ce qui peut être montré à un appelant de ce qui est destiné aux développeurs et au support :

| Message | Obligatoire | Public | Exposition |
| --- | --- | --- | --- |
| `ShortMessage` | Oui | Utilisateurs finaux / clients d’API | Un résumé public court, exposable tel quel (par ex. le `title` d’un problem detail RFC 9457). |
| `DetailedMessage` | Non | Utilisateurs finaux / clients d’API | Un détail public maîtrisé (par ex. le `detail` d’un problem detail RFC 9457), exposé **uniquement** si l’application le décide explicitement. Ne doit contenir aucune information sensible ou interne. |
| `DiagnosticMessage` | Oui | Logs, support, développeurs | Le message de diagnostic interne. Il peut contenir des détails techniques/opérationnels (identifiants, valeurs fautives, état interne) ; il n’est **jamais** exposé aux clients externes par défaut. |

Le cœur du modèle est agnostique vis-à-vis d’HTTP : `DiagnosticMessage` n’est jamais utilisé comme corps de réponse HTTP par défaut, et `type` et `status` restent l’affaire de l’application. Lorsqu’une erreur est exposée en HTTP, son `Code` est le `type` RFC 9457 naturel : exposez-le comme un URI stable tel que `urn:problem:{service}:{code}`, où `{code}` est le code d’erreur en minuscules et en kebab-case — par exemple `urn:problem:temperature-simulator:temperature-below-absolute-zero` ou `urn:problem:banking-api:money-transfer-amount-not-positive` — afin que les clients puissent aiguiller sur le type de problème. Lorsque vous transformez une erreur en exception avec `.ToException()`, le `Exception.Message` obtenu est le `DiagnosticMessage` — le texte destiné aux développeurs et aux logs.

### 2️⃣ Des diagnostics structurés

Chaque erreur peut déclarer des **causes possibles** et des **pistes d’analyse** :

* Qu’est-ce qui a pu provoquer cette erreur ?
* Est-elle plutôt liée aux données d’entrée, au système, ou aux deux ?
* Par où commencer l’investigation ?

Les diagnostics orientent l’analyse sans figer les processus opérationnels.

### 3️⃣ Un DSL pour décrire les erreurs

Les erreurs sont documentées directement dans le code via une API fluide :

```csharp
return DescribeError.WithTitle("Temperature below absolute zero")
                    .WithDescription("This error occurs when trying to instantiate a temperature with a value that is below absolute zero.")
                    .WithRule("Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.")
                    .WithDiagnostics(ValueObjectDiagnostic.Diagnostic)
                    .WithExamples(
                        () => BelowAbsoluteZero(-1, TemperatureUnit.Kelvin),
                        () => BelowAbsoluteZero(-280, TemperatureUnit.Celsius));
```

Il ne s’agit pas de simples commentaires — c’est de la **documentation structurée et exécutable**.

### 4️⃣ Extraction de la documentation

La bibliothèque fournit un mécanisme pour analyser les assemblies et extraire toute la documentation d’erreurs déclarée :

* liée aux classes de factories d’erreur
* liée aux méthodes factory
* enrichie par des exemples
* prête à être rendue

Cela permet de générer :

* des catalogues d’erreurs en Markdown ou JSON (ou tout format personnalisé via un renderer)
* de la documentation orientée support
* une documentation vivante générée depuis le code
* des catalogues multilingues (optionnel) — voir [Internationalisation](Internationalisation.fr.md)

## 🔁 Exception ou pas ? À vous de choisir.

La bibliothèque supporte à la fois :

* **les erreurs levées** (flux classique par exceptions)
* **les erreurs transportées sans être levées** via `Outcome` et `Outcome<T>`

Cela vous permet de traiter la même erreur, au choix :

> comme un signal d’exécution que vous levez
> ou comme une donnée structurée que vous transportez

selon le contexte (domaine, validation, pipelines, etc.).

## 📦 Installation

```bash
dotnet add package FirstClassErrors
```

Cible **.NET Standard 2.0**. Les analyseurs Roslyn sont intégrés au package — aucune installation séparée.

## 🧩 Exemple

Extrait du projet `FirstClassErrors.Usage` :

```csharp
[ProvidesErrorsFor(nameof(Temperature))]
public static class InvalidTemperatureError {

    [DocumentedBy(nameof(BelowAbsoluteZeroDocumentation))]
    internal static DomainError BelowAbsoluteZero(decimal invalidValue, TemperatureUnit invalidValueUnit) {
        return DomainError.Create(
                Code.TemperatureBelowAbsoluteZero,
                diagnosticMessage: $"Failed to instantiate temperature: the value {invalidValue} {invalidValueUnit} is below absolute zero.")
            .WithPublicMessage(
                shortMessage: "Temperature is below absolute zero.",
                detailedMessage: "The provided temperature is below absolute zero, which is not physically valid.");
    }

    private static ErrorDocumentation BelowAbsoluteZeroDocumentation() {
        return DescribeError.WithTitle("Temperature below absolute zero")
                            .WithDescription("This error occurs when trying to instantiate a temperature with a value that is below absolute zero.")
                            .WithRule("Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.")
                            .WithDiagnostics(ValueObjectDiagnostic.Diagnostic)
                            .WithExamples(
                                () => BelowAbsoluteZero(-1, TemperatureUnit.Kelvin),
                                () => BelowAbsoluteZero(-280, TemperatureUnit.Celsius));
    }

    private static class Code {
        public static readonly ErrorCode TemperatureBelowAbsoluteZero = ErrorCode.Create("TEMPERATURE_BELOW_ABSOLUTE_ZERO");
    }
}
```

La factory retourne une `Error` structurée. Lorsque vous devez la lever, vous la transformez en exception avec `.ToException()` :

```csharp
throw InvalidTemperatureError.BelowAbsoluteZero(-1, TemperatureUnit.Kelvin).ToException();
```

Ici, l’erreur, sa signification, sa règle, ses diagnostics et des exemples de messages sont définis ensemble — dans le code.

## 🎯 Pour qui ?

FirstClassErrors est particulièrement utile si :

* vous développez des systèmes métiers complexes
* vous vous souciez de la supportabilité
* vous voulez une gestion d’erreurs cohérente
* vous souhaitez une documentation qui ne dérive pas du code
* vous concevez avec une approche orientée domaine

## 🔍 Analyseurs

FirstClassErrors est livré avec un ensemble d’analyseurs Roslyn (identifiants de règle `FCExxx`) **inclus dans le package NuGet** — référencez le package et ils s’exécutent au build, sans installation supplémentaire. Ils détectent, avant toute exécution, les erreurs que le runtime ou le pipeline de documentation ne feraient sinon apparaître que tard, voire silencieusement : codes d’erreur dupliqués, références `[DocumentedBy]` qui ne résolvent pas, erreurs documentées qui n’atteignent jamais le catalogue, et plus encore.

> **Prérequis compilateur :** les analyseurs inclus sont compilés contre Roslyn 4.8 ; ils se chargent donc dans tout hôte à partir du **SDK .NET 8 / Visual Studio 2022 17.8**. Les SDK/IDE plus anciens ne peuvent pas les charger.

Voir la [référence des règles d’analyse](analyzers/README.fr.md).

## 🔐 Chaîne d’approvisionnement

Chaque package publié est construit et poussé par [`release.yml`](../.github/workflows/release.yml) avec deux garanties vérifiables :

- **Provenance de build signée** ([SLSA](https://slsa.dev)) — chaque package est attesté au moment du build, liant son empreinte à ce dépôt, au commit exact et à l’exécution du workflow. Les octets attestés sont attachés à la [GitHub Release](https://github.com/Reefact/first-class-errors/releases) correspondante ; téléchargez-y le `.nupkg` et vérifiez avec :

  ```bash
  gh attestation verify FirstClassErrors.<version>.nupkg --repo Reefact/first-class-errors
  ```

  (nuget.org sert une copie signée par le dépôt, d’empreinte différente : vérifiez **celle-ci** plutôt avec `dotnet nuget verify`.)

- **SBOM embarqué** — chaque package contient son inventaire logiciel SPDX (*software bill of materials*) à `_manifest/spdx_2.2/manifest.spdx.json`, recensant les fichiers livrés et les composants tiers utilisés pour le construire.

## 🐛 Retours & contributions

Vous avez trouvé un bug ou souhaitez proposer une fonctionnalité ? Ouvrez une issue sur le [gestionnaire d’issues GitHub](https://github.com/Reefact/first-class-errors/issues). Les contributions sont les bienvenues — voir [CONTRIBUTING.fr.md](./CONTRIBUTING.fr.md) pour commencer.

Pour les vulnérabilités de **sécurité**, merci de suivre le processus privé décrit dans [SECURITY.fr.md](./SECURITY.fr.md) plutôt que d’ouvrir une issue publique.

## 📚 Étapes suivantes

Consultez la documentation complète :

- [Premiers pas](GettingStarted.fr.md)
- [Principes de conception](DesignPrinciples.fr.md)
- [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md)
- [Concepts fondamentaux](CoreConcepts.fr.md)
- [Guide du contexte d’erreur](ErrorContext.fr.md)
- [Guide d’écriture des erreurs](WritingErrorsGuide.fr.md)
- [Cas d’usage](UsagePatterns.fr.md)
- [Bonnes pratiques](BestPractices.fr.md)
- [Guide des tests](Testing.fr.md)
- [Intégration CI/CD et exploitation](OperationalIntegration.fr.md)
- Versionnage du catalogue
  - [Vue d'ensemble & workflow](CatalogVersioning.fr.md)
  - [Référence des commandes](CatalogVersioningReference.fr.md)
  - [Intégration CI/CD](CatalogVersioningCI.fr.md)
- [Architecture du pipeline de documentation](ArchitectureOfTheDocumentationPipeline.fr.md)
- [Écrire son propre renderer](WritingACustomRenderer.fr.md)
- [Internationalisation](Internationalisation.fr.md)
- [Comparaison avec les librairies de gestion d’erreurs](ComparisonWithOtherLibraries.fr.md)
- [Règles d’analyse (FCExxx)](analyzers/README.fr.md)
- [FAQ](FAQ.fr.md)
