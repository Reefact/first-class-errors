# FirstClassErrors

🌍 **Langues :**  
🇬🇧 [English](../README.md) | 🇫🇷 Français (ce fichier)

|  |  |
| :-- | :-- |
| **Build** | [![ci](https://github.com/Reefact/first-class-errors/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Reefact/first-class-errors/actions/workflows/ci.yml) |
| **Qualité** | [![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=reefact_first-class-errors&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=reefact_first-class-errors) [![Couverture](https://sonarcloud.io/api/project_badges/measure?project=reefact_first-class-errors&metric=coverage)](https://sonarcloud.io/summary/new_code?id=reefact_first-class-errors) |
| **Sécurité** | [![codeql](https://github.com/Reefact/first-class-errors/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/Reefact/first-class-errors/actions/workflows/codeql.yml) [![OpenSSF Best Practices](https://www.bestpractices.dev/projects/13567/badge)](https://www.bestpractices.dev/projects/13567) [![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Reefact/first-class-errors/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Reefact/first-class-errors) |
| **Package** | [![NuGet](https://img.shields.io/nuget/vpre/FirstClassErrors?logo=nuget)](https://www.nuget.org/packages/FirstClassErrors) ![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4) |
| **Projet** | [![License](https://img.shields.io/github/license/Reefact/first-class-errors)](../LICENSE) [![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-fe5196?logo=conventionalcommits&logoColor=white)](https://www.conventionalcommits.org) |

---

**Transformez vos erreurs en connaissance structurée et vivante sur votre système.**

![FirstClassErrors](./images/first-class-errors.png "FirstClassErrors")

FirstClassErrors est une bibliothèque .NET destinée aux erreurs applicatives qui doivent être comprises, diagnostiquées, documentées et conservées dans le temps.

Au lieu de disperser les codes et messages d’erreur dans le code, vous définissez chaque situation significative une seule fois, dans une factory nommée. La même `Error` structurée peut ensuite être levée sous forme d’exception, transportée dans un `Outcome<T>`, journalisée et intégrée à un catalogue d’erreurs généré automatiquement.

## 🚨 Le problème

Une erreur de production est rarement utile lorsqu’elle se résume à un type et une chaîne de caractères :

```text
Opération invalide.
```

Les développeurs et le support doivent encore découvrir :

- quelle situation s’est réellement produite ;
- quelle règle a été violée ;
- quels faits appartiennent à cette occurrence ;
- ce qui a pu la provoquer ;
- par où commencer l’investigation.

Lorsque cette connaissance est répartie entre les logs, les tickets, les commentaires et la mémoire des personnes, elle dérive du code.

## 💡 L’approche FirstClassErrors

Une factory donne une identité stable à la situation d’erreur et centralise sa construction :

```csharp
[ProvidesErrorsFor(nameof(Amount))]
public static class InvalidAmountOperationError {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static DomainError CurrencyMismatch(Amount left, Amount right) {
        return DomainError.Create(
                ErrorCode.Create("AMOUNT_CURRENCY_MISMATCH"),
                diagnosticMessage: $"Impossible d’additionner {left} et {right} car leurs devises diffèrent.")
            .WithPublicMessage(
                shortMessage: "Les montants utilisent des devises différentes.",
                detailedMessage: "Les deux montants doivent utiliser la même devise.");
    }

    private static ErrorDocumentation CurrencyMismatchDocumentation() {
        return DescribeError.WithTitle("Incohérence de devise")
                            .WithDescription("Cette erreur survient lorsqu’une opération combine des montants exprimés dans des devises différentes.")
                            .WithRule("Une opération monétaire doit utiliser une devise commune.")
                            .WithDiagnostic(
                                "Les montants ont atteint l’opération sans avoir été convertis dans une devise commune.",
                                ErrorOrigin.Internal,
                                "Vérifiez à quel endroit les montants auraient dû être convertis avant cette opération.")
                            .WithExamples(() => CurrencyMismatch(
                                new Amount(10, Currency.EUR),
                                new Amount(12, Currency.USD)));
    }
}
```

Le code métier reste centré sur l’intention :

```csharp
if (Currency != other.Currency) {
    throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException();
}
```

La factory retourne une `Error`, donc un échec attendu peut employer le même modèle sans lever d’exception :

```csharp
return Outcome<Amount>.Failure(
    InvalidAmountOperationError.CurrencyMismatch(left, right));
```

À partir de ces factories, FirstClassErrors peut générer un catalogue lisible par les développeurs, le support et l’exploitation.

## 📦 Installation

```bash
dotnet add package FirstClassErrors
```

Le package cible **.NET Standard 2.0**. Les analyseurs Roslyn sont inclus automatiquement ; aucun package d’analyse supplémentaire n’est nécessaire.

Pour générer la documentation, installez le CLI :

```bash
dotnet tool install --global FirstClassErrors.Cli
```

Suivez ensuite le guide [Premiers pas](GettingStarted.fr.md) pour créer et générer votre première erreur documentée.

## 🎯 Quand la bibliothèque est adaptée

FirstClassErrors est particulièrement utile dans du code applicatif ou métier durable lorsque :

- les erreurs représentent des règles, des contraintes ou des défaillances de frontière ;
- plusieurs équipes ou systèmes dépendent de codes d’erreur stables ;
- le support et l’exploitation investiguent des incidents de production ;
- la documentation doit rester alignée avec le comportement.

Pour un prototype, un petit utilitaire ou du code technique bas niveau, les exceptions standards peuvent suffire. Consultez [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md).

## 🔍 Analyseurs et chaîne d’approvisionnement

Le package contient des règles Roslyn aux identifiants stables `FCExxx`. Elles détectent notamment les codes dupliqués ou mal formés, les liaisons de documentation invalides, l’absence d’exemples et certains mauvais usages de l’API. Consultez la [référence des analyseurs](analyzers/README.fr.md).

Les packages publiés contiennent une provenance de build signée et un SBOM SPDX embarqué. Les informations de vérification sont disponibles dans [SECURITY.fr.md](SECURITY.fr.md).

## 🐛 Retours et contributions

Vous avez trouvé un bug ou souhaitez proposer une fonctionnalité ? Ouvrez une issue sur le [gestionnaire d’issues GitHub](https://github.com/Reefact/first-class-errors/issues). Les contributions sont les bienvenues ; consultez [CONTRIBUTING.fr.md](CONTRIBUTING.fr.md).

Pour les vulnérabilités de sécurité, suivez le processus privé décrit dans [SECURITY.fr.md](SECURITY.fr.md).

## 📚 Documentation

### Découvrir

- [Premiers pas](GettingStarted.fr.md)
- [Principes de conception](DesignPrinciples.fr.md)
- [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md)

### Comprendre le modèle

- [Concepts fondamentaux](CoreConcepts.fr.md)
- [Taxonomie et composition des erreurs](ErrorTaxonomy.fr.md)
- [Guide du contexte d’erreur](ErrorContext.fr.md)

### Écrire et utiliser des erreurs

- [Guide d’écriture des erreurs](WritingErrorsGuide.fr.md)
- [Cas d’usage](UsagePatterns.fr.md)
- [Bonnes pratiques](BestPractices.fr.md)
- [Guide des tests](Testing.fr.md)

### Générer et exploiter le catalogue

- [Intégration CI/CD et exploitation](OperationalIntegration.fr.md)
- Versionnage du catalogue
  - [Vue d’ensemble et workflow](CatalogVersioning.fr.md)
  - [Référence des commandes](CatalogVersioningReference.fr.md)
  - [Intégration CI/CD](CatalogVersioningCI.fr.md)
- [Architecture du pipeline de documentation](ArchitectureOfTheDocumentationPipeline.fr.md)
- [Écrire son propre renderer](WritingACustomRenderer.fr.md)
- [Internationalisation](Internationalisation.fr.md)

### Évaluer et résoudre les problèmes

- [Comparaison avec les bibliothèques de gestion d’erreurs](ComparisonWithOtherLibraries.fr.md)
- [Règles d’analyse (FCExxx)](analyzers/README.fr.md)
- [FAQ](FAQ.fr.md)
