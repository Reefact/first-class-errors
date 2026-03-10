# Contexte d’erreur : quand et pourquoi l’utiliser

`ErrorContext` permet d’attacher des métadonnées **structurées, typées et stables** à une instance de `DiagnosableException`.

Il complète le code d’erreur et les messages en répondant à :

> Qu’est-ce qui était vrai au moment où cette erreur précise s’est produite ?

## ✅ Quand utiliser `ErrorContext`

Utilisez-le lorsque l’information est utile pour le diagnostic et l’observabilité, et qu’elle varie selon les occurrences.

Exemples typiques :

* une date de transaction hors période autorisée
* un identifiant métier utile à l’investigation (`OrderId`, `StatementId`, `CustomerId`)
* une valeur mesurée qui viole une règle (`ProvidedTemperature`, `DeclaredAmount`)

En bref : utilisez le contexte pour des **faits liés à l’occurrence**, pas pour la sémantique générale de l’erreur.

## ❌ Quand ne pas l’utiliser

N’ajoutez pas dans le contexte :

* des informations qui appartiennent déjà à la définition stable de l’erreur (titre, règle, diagnostics)
* des payloads volumineux (fichiers complets, objets massifs, corps de requête/réponse complets)
* des secrets ou données sensibles (mots de passe, tokens, données personnelles complètes)
* des instructions opérationnelles (« ouvrir un ticket », « contacter l’équipe X »)

Si l’information est instable, bruyante, sensible ou non actionnable, ne l’ajoutez pas.

## 🎯 Pourquoi cela améliore l’observabilité

Avec `ErrorCode`, vous regroupez les erreurs par type.
Avec `ErrorContext`, vous comprenez **pourquoi cette occurrence** est arrivée.

Cela permet :

* un triage plus rapide dans les logs
* une corrélation plus simple entre systèmes
* des dashboards et filtres plus précis (par clé de contexte)
* moins d’allers-retours entre dev et support

À retenir :

* `ErrorCode` = *de quelle catégorie d’erreur parle-t-on ?*
* `ErrorContext` = *quels sont les faits clés de cette occurrence ?*

## 🧱 Règles de conception

### 1) Utiliser des clés nommées et réutilisables

Définissez les clés de contexte une fois dans un emplacement central :

```csharp
internal static class ErrCtxKey {
    public static readonly ErrorContextKey<DateOnly> TransactionDate =
        ErrorContextKey.Create<DateOnly>("TRANSACTION_DATE", "Date de la transaction en cours de traitement.");
}
```

### 2) Ajouter le contexte au niveau des factories

Attachez le contexte là où l’exception est créée, pour garantir la cohérence de chaque occurrence :

```csharp
return new NonCompliantBankTransactionFileException(
    Code.DateOutOfStatementPeriod,
    $"Transaction datée du {transactionDate} hors période [{periodStart};{periodEnd}].",
    "La date de transaction est hors période du relevé.",
    ctx => ctx.Add(ErrCtxKey.TransactionDate, transactionDate));
```

### 3) Garder des valeurs simples et sérialisables

Privilégiez les primitives et petits value objects faciles à logger.

### 4) Garder des noms de clés stables

Les clés de contexte deviennent une partie de votre vocabulaire opérationnel. Les renommer souvent casse les requêtes et dashboards.

## 📌 Checklist pratique

Avant d’ajouter une entrée de contexte, demandez-vous :

* Est-ce que cela aide à diagnostiquer plus vite ?
* Est-ce sûr à exposer dans les logs ?
* Est-ce spécifique à cette occurrence ?
* Est-ce actionnable pour le support / l’exploitation ?

Si la majorité des réponses est oui, c’est un bon candidat.

---

Section précédente: [Concepts fondamentaux](CoreConcepts.fr.md) | Section suivante: [Guide d’écriture des erreurs](WritingErrorsGuide.fr.md)

---
