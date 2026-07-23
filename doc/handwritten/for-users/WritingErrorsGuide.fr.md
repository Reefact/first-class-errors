# Écrire la documentation d’une erreur

🌍 **Langues :**  
🇬🇧 [English](./WritingErrorsGuide.en.md) | 🇫🇷 Français (ce fichier)

Une erreur documentée doit permettre de comprendre une défaillance sans commencer par ouvrir le code source.

Ce guide explique comment décrire le **sens stable** d’une erreur : son code, son titre, sa description, sa règle, ses diagnostics et ses exemples. Les messages portés à l’exécution sont traités séparément dans [Écrire les messages d’une erreur](WritingErrorMessages.fr.md).

> Le texte de documentation peut être écrit en dur ou lu depuis des ressources localisées. Voir [Internationalisation](Internationalisation.fr.md).

## Le modèle en une minute

Chaque factory documentée répond à six questions :

| Élément | Question à laquelle il répond |
| --- | --- |
| Code d’erreur | Comment le logiciel identifie-t-il cette situation ? |
| Titre | Que s’est-il passé, en quelques mots ? |
| Description | Que signifie cette erreur ? |
| Règle | Qu’est-ce qui devrait normalement être vrai ? |
| Diagnostics | Qu’est-ce qui peut l’expliquer et où commencer l’analyse ? |
| Exemples | À quoi ressemble une occurrence réelle ? |

L’objectif n’est pas de décrire tous les détails techniques. Il est de capturer une connaissance qui reste utile dans les logs, les investigations du support, les releases et les refactorings.

## 1. Partir d’une situation d’erreur précise

Une factory doit représenter une situation unique dans laquelle le système ne peut pas continuer comme prévu.

Évitez les catégories trop larges :

- `INVALID_OPERATION`
- `PROCESSING_ERROR`
- `UNEXPECTED_FAILURE`

Préférez des situations qu’un développeur ou le support peut reconnaître immédiatement :

- `AMOUNT_CURRENCY_MISMATCH`
- `TEMPERATURE_BELOW_ABSOLUTE_ZERO`
- `TRANSACTION_DATE_OUTSIDE_STATEMENT_PERIOD`

Un bon test consiste à demander :

> Deux occurrences de sens réellement différent pourraient-elles partager cette documentation sans la rendre vague ?

Si la réponse est non, elles nécessitent probablement des factories et des codes distincts.

## 2. Choisir un code d’erreur stable

Le code est l’identité de l’erreur, lisible par machine.

Utilisez `UPPER_SNAKE_CASE`, incluez suffisamment de contexte métier pour éviter l’ambiguïté et rendez le code indépendant des noms de classes ou des détails d’implémentation.

Bon :

```text
AMOUNT_CURRENCY_MISMATCH
```

À éviter :

```text
INVALID_AMOUNT_OPERATION_ERROR
ADD_METHOD_FAILED
```

Considérez le code comme un contrat. Des clients, dashboards, alertes ou procédures de support peuvent en dépendre. Le renommer ou le supprimer constitue donc un changement cassant ; voir [Versionnage du catalogue](CatalogVersioning.fr.md).

## 3. Écrire un titre qui nomme la situation

Le titre est un libellé humain court.

Bons titres :

- « Incohérence de devise des montants »
- « Température sous le zéro absolu »
- « Date de transaction hors période du relevé »

Évitez les titres qui annoncent seulement un échec :

- « L’opération a échoué »
- « Valeur invalide »
- « Erreur inattendue »

Le titre doit rester compréhensible lorsqu’il apparaît seul dans l’index d’un catalogue.

## 4. Expliquer le sens en langage simple

La description explique quand l’erreur survient et ce que la situation signifie.

Un schéma fiable est :

> « Cette erreur survient lorsque… »

Écrivez pour une personne qui comprend le système métier sans nécessairement connaître son implémentation. Décrivez la situation, pas la stack trace, la classe, la méthode ou le mécanisme d’exception.

Bon :

> « Cette erreur survient lorsqu’une opération combine des montants exprimés dans des devises différentes. »

À éviter :

> « Cette exception est levée par `Amount.AddOrThrow` lorsque les champs de devise sont différents. »

## 5. Énoncer la règle violée

La règle exprime l’invariant ou la contrainte qui devrait normalement être respecté.

Formulez-la comme une vérité générale :

> « Toutes les opérations monétaires doivent impliquer des montants exprimés dans la même devise. »

La règle ne doit pas répéter la description. La description dit ce qui s’est passé ; la règle dit ce qui doit être vrai.

Lorsqu’aucun invariant pertinent n’existe, omettez la règle plutôt que d’en inventer une.

## 6. Écrire les diagnostics comme des hypothèses

Un diagnostic contient trois éléments :

| Élément | Rôle |
| --- | --- |
| Cause | Un état ou une condition plausible pouvant expliquer l’erreur |
| Origine | Une cause interne, externe ou potentiellement les deux |
| Piste d’analyse | L’endroit où commencer l’investigation |

Les causes sont des hypothèses, pas des causes racines prouvées. Décrivez des conditions sans attribuer de faute.

Bonne cause :

> « Des montants ont été utilisés avant d’avoir été convertis dans une devise commune. »

À éviter :

> « Le développeur a oublié de convertir les montants. »

Une bonne piste d’analyse commence par un verbe neutre comme *Vérifier*, *Contrôler* ou *Examiner* :

> « Vérifier que chaque montant a été converti dans la devise de l’opération avant le calcul. »

N’encodez pas de procédures organisationnelles comme « ouvrir un ticket » ou « contacter l’équipe X ». Ces processus évoluent indépendamment de l’application.

## 7. Utiliser les exemples pour rendre la règle visible

Les exemples doivent utiliser des valeurs simples et réalistes qui rendent la violation évidente.

```csharp
.WithExamples(
    () => CurrencyMismatch(
        new Amount(127.33m, Currency.EUR),
        new Amount(84.10m, Currency.USD)))
```

Les exemples sont du contenu pédagogique destiné au catalogue, pas des tests de valeurs limites. Préférez une ou deux occurrences représentatives à des valeurs pathologiques.

Comme l’exemple appelle la vraie factory, la documentation générée reste également reliée à l’erreur réellement produite à l’exécution.

## Exemple complet

```csharp
[ProvidesErrorsFor(nameof(Amount))]
public static class InvalidAmountOperationError {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static DomainError CurrencyMismatch(Amount left, Amount right) {
        return DomainError.Create(
                Code.CurrencyMismatch,
                diagnosticMessage: $"Impossible de combiner des montants en {left.Currency} et {right.Currency} : {left} et {right}.")
            .WithPublicMessage(
                shortMessage: "Les montants utilisent des devises différentes.",
                detailedMessage: "Tous les montants de cette opération doivent utiliser la même devise.");
    }

    private static ErrorDocumentation CurrencyMismatchDocumentation() {
        return DescribeError
            .WithTitle("Incohérence de devise des montants")
            .WithDescription("Cette erreur survient lorsqu’une opération combine des montants exprimés dans des devises différentes.")
            .WithRule("Toutes les opérations monétaires doivent impliquer des montants exprimés dans la même devise.")
            .WithDiagnostic(
                "Des montants ont été utilisés avant d’avoir été convertis dans une devise commune.",
                ErrorOrigin.Internal,
                "Vérifier que chaque montant a été converti dans la devise de l’opération avant le calcul.")
            .AndDiagnostic(
                "Une requête externe a fourni des montants dans des devises incompatibles.",
                ErrorOrigin.External,
                "Contrôler les devises fournies par l’appelant et celle attendue par l’opération.")
            .WithExamples(() => CurrencyMismatch(
                new Amount(127.33m, Currency.EUR),
                new Amount(84.10m, Currency.USD)));
    }

    private static class Code {
        public static readonly ErrorCode CurrencyMismatch =
            ErrorCode.Create("AMOUNT_CURRENCY_MISMATCH");
    }
}
```

La documentation décrit la situation d’erreur stable. La factory crée séparément une occurrence concrète avec ses messages et ses valeurs d’exécution.

## Checklist de revue

Avant d’accepter une nouvelle documentation d’erreur, vérifiez que :

- la factory représente une situation précise ;
- le code est spécifique, stable et écrit en `UPPER_SNAKE_CASE` ;
- le titre nomme la situation au lieu d’annoncer un échec ;
- la description est compréhensible sans lire l’implémentation ;
- la règle est un véritable invariant, ou est omise ;
- les causes de diagnostic sont des conditions plausibles et non des accusations ;
- les pistes d’analyse orientent l’investigation sans prescrire le workflow du support ;
- les exemples sont simples, réalistes et appellent la factory documentée ;
- aucun bruit technique ni donnée sensible n’apparaît.

Pour choisir les textes publics et internes portés à l’exécution, continuez avec [Écrire les messages d’une erreur](WritingErrorMessages.fr.md). Pour une liste de revue compacte à l’échelle du projet, consultez [Bonnes pratiques](BestPractices.fr.md).

---

<div align="center">
<a href="ErrorContext.fr.md">← Guide du contexte d’erreur</a> · <a href="README.fr.md#-documentation">↑ Table des matières</a> · <a href="WritingErrorMessages.fr.md">Écrire les messages d’une erreur →</a>
</div>

---