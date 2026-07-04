# FirstClassErrors

🌍 **Langues:**  
🇬🇧 [English](../README.md) | 🇫🇷 Français (ce fichier)

![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4) [![Licence : Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](../LICENSE)

---

**Transformez vos exceptions en connaissance structurée et vivante sur votre système.**

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

Le cœur du modèle est agnostique vis-à-vis d’HTTP : `DiagnosticMessage` n’est jamais utilisé comme corps de réponse HTTP par défaut. Lorsque vous transformez une erreur en exception avec `.ToException()`, le `Exception.Message` obtenu est le `DiagnosticMessage` — le texte destiné aux développeurs et aux logs.

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

Cela vous permet d’utiliser les exceptions :

> comme signaux d’exécution
> ou comme données d’erreur structurées

selon le contexte (domaine, validation, pipelines, etc.).

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
- [Intégration CI/CD et exploitation](OperationalIntegration.fr.md)
- [Architecture du pipeline de documentation](ArchitectureOfTheDocumentationPipeline.fr.md)
- [Écrire son propre renderer](WritingACustomRenderer.fr.md)
- [Internationalisation](Internationalisation.fr.md)
- [Comparaison avec les librairies de gestion d’erreurs](ComparisonWithOtherLibraries.fr.md)
- [FAQ](FAQ.fr.md)
