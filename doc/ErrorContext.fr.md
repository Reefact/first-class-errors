# Contexte d’erreur : quand et pourquoi l’utiliser

🌍 **Langues :**  
🇬🇧 [English](./ErrorContext.en.md) | 🇫🇷 Français (ce fichier)

`ErrorContext` attache des métadonnées **structurées, typées et stables** à une `Error`.

Il répond à une question :

> Qu’est-ce qui était vrai lorsque cette occurrence précise s’est produite ?

Le code d’erreur identifie la situation. Le contexte enregistre les faits qui rendent cette occurrence diagnostiquable.

## ✅ Quand utiliser `ErrorContext`

Utilisez-le lorsque l’information :

- varie d’une occurrence à l’autre ;
- aide le diagnostic, la corrélation ou l’observabilité ;
- peut être enregistrée sans risque dans les logs ;
- peut être représentée par une valeur petite et stable.

Exemples courants :

- un identifiant métier utilisé pendant l’investigation (`OrderId`, `StatementId`, `CustomerId`) ;
- une valeur ayant violé une règle (`ProvidedTemperature`, `DeclaredAmount`) ;
- une date ou une borne pertinente (`TransactionDate`, `PeriodStart`, `PeriodEnd`) ;
- un identifiant de corrélation externe.

En bref : utilisez le contexte pour des **faits liés à l’occurrence**, pas pour la signification stable de l’erreur.

## ❌ Quand ne pas l’utiliser

N’ajoutez pas dans le contexte :

- des informations appartenant à la définition stable de l’erreur, comme son titre, sa règle ou ses diagnostics ;
- des corps de requête ou de réponse complets ;
- de gros objets ou fichiers ;
- des mots de passe, tokens, secrets ou données personnelles inutiles ;
- des instructions opérationnelles comme « ouvrir un ticket » ou « contacter l’équipe X » ;
- des valeurs que personne ne peut exploiter pendant l’investigation.

Si une donnée est bruyante, sensible, instable, volumineuse ou non actionnable, laissez-la de côté.

## 🎯 Le code, les messages et le contexte ont des rôles différents

| Élément | Question à laquelle il répond |
| --- | --- |
| `ErrorCode` | Quelle situation d’erreur reconnue s’est produite ? |
| messages publics | Que peut-on expliquer sans risque à l’appelant ? |
| `DiagnosticMessage` | Quel détail interne explique cette occurrence ? |
| `ErrorContext` | Quels faits structurés les logs et les outils doivent-ils pouvoir interroger ? |

Ne dupliquez pas chaque valeur du message de diagnostic dans le contexte. Ajoutez une entrée lorsque la valeur doit être recherchable, filtrable, corrélable ou consommée par un outil.

## 🧱 Définir des clés nommées et réutilisables

Définissez les clés une seule fois dans un emplacement stable :

```csharp
internal static class ErrCtxKey {
    public static readonly ErrorContextKey<Guid> OrderId =
        ErrorContextKey.Create<Guid>(
            "ORDER_ID",
            "Identifiant de la commande en cours de traitement.");

    public static readonly ErrorContextKey<Guid> StatementId =
        ErrorContextKey.Create<Guid>(
            "STATEMENT_ID",
            "Identifiant du relevé en cours de traitement.");

    public static readonly ErrorContextKey<DateOnly> TransactionDate =
        ErrorContextKey.Create<DateOnly>(
            "TRANSACTION_DATE",
            "Date de la transaction en cours de traitement.");
}
```

Une clé nommée donne à la valeur une identité et un type stables. Les dashboards, requêtes de logs et la documentation générée peuvent s’appuyer sur ce contrat.

## 🏭 Ajouter le contexte dans la factory

Attachez le contexte là où l’erreur est créée afin que chaque occurrence utilise les mêmes clés.

```csharp
return PrimaryPortError.Create(
        Code.DateOutOfStatementPeriod,
        diagnosticMessage: $"Transaction datée du {transactionDate} hors période [{periodStart}; {periodEnd}].",
        transience: Transience.NonTransient,
        configureContext: ctx => ctx
            .Add(ErrCtxKey.TransactionDate, transactionDate)
            .Add(ErrCtxKey.StatementId, statementId))
    .WithPublicMessage(
        shortMessage: "La date de transaction est hors période du relevé.",
        detailedMessage: "La transaction ne peut pas être acceptée pour cette période de relevé.");
```

Ajouter le contexte après coup dans un adapter, un bloc `catch` ou un middleware de logging risque de produire des clés incohérentes et des données manquantes. Préférez la factory lorsque l’information y est disponible.

## 🔁 Le contexte voyage avec l’erreur

Le contexte appartient à l’`Error`, pas à un transport particulier.

```mermaid
flowchart LR
    A[Factory d’erreur] --> B[Error avec Context]
    B --> C[Outcome<T>]
    B --> D[error.ToException()]
    C --> E[result.Error.Context]
    D --> F[exception.Error.Context]
```

Le même contexte est conservé lorsque l’erreur :

- est retournée dans `Outcome` ou `Outcome<T>` ;
- est propagée via `Then`, `To` ou d’autres opérations d’outcome ;
- est transformée en exception via `ToException()` ;
- est imbriquée comme erreur interne.

Le contexte doit donc décrire l’occurrence elle-même, et non un transport comme HTTP ou les exceptions. Consultez [Patterns d’utilisation](UsagePatterns.fr.md) et [Composer avec Outcome](OutcomeGuide.fr.md).

## 📦 Garder des valeurs petites et sérialisables

Privilégiez les primitives, enums, identifiants, dates et petits value objects qui se sérialisent de façon prévisible.

Bon contexte :

```text
ORDER_ID = 7f7a7f30-3b28-44d6-b956-f85ef8f70b03
TRANSACTION_DATE = 2026-07-14
PROVIDED_AMOUNT = 127.33
```

Mauvais contexte :

```text
ORDER = <agrégat complet>
REQUEST_BODY = <document JSON complet>
CUSTOMER = <profil personnel complet>
```

Si plusieurs valeurs décrivent le même échec, utilisez plusieurs clés explicites plutôt qu’un objet opaque.

## 🔒 Traiter le contexte comme une donnée de log

Même lorsque le contexte ne fait pas partie d’une API publique, supposez qu’il peut apparaître dans les logs, traces, outils de support ou exports de télémétrie.

Avant d’ajouter une valeur, considérez :

- les exigences de protection des données ;
- la durée de conservation ;
- les accès aux outils opérationnels ;
- la possibilité d’un hachage ou d’une occultation partielle ;
- la nécessité réelle de cette valeur.

Une valeur techniquement utile n’est pas automatiquement appropriée à enregistrer.

## 📌 Checklist pratique

Avant d’ajouter une entrée de contexte, demandez-vous :

1. Varie-t-elle selon l’occurrence ?
2. Aide-t-elle quelqu’un à investiguer plus vite ?
3. Les logs ou les outils doivent-ils pouvoir l’interroger indépendamment ?
4. Le nom de la clé est-il stable et réutilisable ?
5. La valeur est-elle petite et sérialisable de façon prévisible ?
6. Peut-elle être conservée sans risque dans les systèmes opérationnels ?

Si la réponse est oui aux six questions, c’est une excellente candidate.

---

<div align="center">
<a href="CoreConcepts.fr.md">← Concepts fondamentaux</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="WritingErrorsGuide.fr.md">Guide d’écriture des erreurs →</a>
</div>

---