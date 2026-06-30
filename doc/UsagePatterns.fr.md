# Patterns d’utilisation

DiagnosableExceptions est particulièrement utile lorsque les erreurs ne sont pas de simples défaillances techniques, mais des **événements porteurs de sens dans la vie du système**.  
Voici des patterns courants où la bibliothèque apporte clarté et structure.

## 🧱 1. Invariants de Value Object

Lors de la création d’un value object, les états invalides doivent être rejetés.

```csharp
public static Amount From(decimal value, Currency currency) {
    if (value < 0) { throw InvalidAmountOperationError.NegativeAmount(value).ToException(); }

    return new Amount(value, currency);
}
````

Ici :

* la règle métier est explicite
* l’exception représente une violation précise d’invariant
* la documentation décrit la règle et les diagnostics

Le code métier reste expressif et auto-explicatif.

## 📥 2. Validation d’entrée (API / UI)

Les entrées utilisateur ou externes peuvent être invalides, sans être exceptionnelles au sens technique.

```csharp
public Outcome<Amount> TryCreateAmount(decimal value, string currencyCode){
    if (!Currency.TryParse(currencyCode, out var currency))    {
        return Outcome<Amount>.Failure(InvalidAmountOperationError.UnknownCurrency(currencyCode)); }

    return Outcome<Amount>.Success(new Amount(value, currency));
}
```

Les erreurs sont :

* capturées
* transportables
* diagnostiquables

sans interrompre le flux.

## 🧮 3. Opérations métier

Les opérations entre objets métier comportent souvent des contraintes sémantiques.

```csharp
public Amount Add(Amount other) {
    if (Currency != other.Currency) { throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException(); }

    return new Amount(Value + other.Value, Currency);
}
```

Le code se lit comme un langage métier, tandis que l’erreur reste structurée et documentée.

## 📦 4. Traitement par lots ou fichiers

En traitement batch, de nombreux éléments peuvent échouer indépendamment.

```csharp
foreach (var line in file) {
    var result = TryParseAmount(line);

    if (result.IsFailure) {
        Log(result.Error);
        
        continue;
    }

    Process(result.Value);
}
```

Les erreurs sont :

* collectées
* loguées avec diagnostics complets
* non bloquantes pour l’ensemble du traitement

## 🌐 5. Frontières d’intégration

Lors d’interactions avec des systèmes externes :

* les données peuvent être incohérentes
* les formats peuvent évoluer
* les hypothèses peuvent être invalides

Les exceptions diagnostiquables aident à distinguer :

* les problèmes métier
* les problèmes d’entrée
* les problèmes système ou de transformation

Les diagnostics orientent l’investigation.

## 🔁 6. Pipelines de validation

Les validations complexes impliquent souvent plusieurs contrôles.

```csharp
var result = ValidateAmount(amount)
             .Then(CheckCurrency)
             .Then(CheckLimits);
```

Chaque échec peut porter une exception diagnostiquable, ce qui garde un modèle cohérent tout en évitant des levées d’exception incontrôlées.

## 🧩 7. Logging orienté support

Comme les exceptions portent des diagnostics structurés, les logs deviennent plus exploitables :

* codes d’erreur stables
* messages courts porteurs de sens
* causes documentées

Les équipes support peuvent relier les événements runtime à des cas d’erreur documentés.

## 🛠️ 8. Composer avec le pipeline `Outcome`

`Outcome` et `Outcome<T>` permettent de composer les chemins de succès et d’échec sans lever d’exception.
Un échec porte une `Error` (jamais une `Exception`), si bien que toute la chaîne reste diagnostiquable.

* **`Then(...)`** — enchaîne l’étape suivante uniquement si la précédente a réussi (court-circuite en cas d’échec).
* **`To(...)`** — transforme la valeur portée en une autre valeur (`Outcome<T>` uniquement), en préservant un éventuel échec.
* **`Recover(...)`** — fournit une valeur de repli lorsque la chaîne a échoué.
* **`Finally(...)`** — exécute un traitement terminal pour le succès comme pour l’échec.

```csharp
Outcome<Receipt> outcome =
    TryCreateAmount(value, currencyCode)         // Outcome<Amount>
        .Then(amount => CheckLimits(amount))     // Outcome<Amount>, exécuté seulement en cas de succès
        .To(amount => amount.WithVat())          // transforme la valeur, les échecs passent au travers
        .Recover(error => Amount.Zero)           // valeur de repli si la chaîne a échoué
        .Then(amount => Charge(amount))          // Outcome<Receipt>
        .Finally(
            onSuccess: receipt => Log($"Charged {receipt}"),
            onFailure: error => Log(error));      // error est une Error, pleinement diagnostiquable
```

### Échappatoires

Lorsqu’il faut sortir du monde `Outcome` (par exemple à une frontière applicative), deux échappatoires retransforment un échec en levée d’exception :

* **`ThrowIfFailure()`** — lève l’exception de l’échec (via `error.ToException()`) lorsque l’outcome a échoué ; sinon ne fait rien.
* **`GetResultOrThrow()`** — retourne la valeur portée en cas de succès, ou lève l’exception de l’échec (`Outcome<T>` uniquement).

```csharp
Outcome<Amount> outcome = TryCreateAmount(value, currencyCode);

outcome.ThrowIfFailure();            // lève error.ToException() en cas d’échec
Amount amount = outcome.GetResultOrThrow(); // valeur en cas de succès, sinon lève
```

### Composition asynchrone

Pour les flux asynchrones, `OutcomeTaskExtensions` fournit des surcharges `Then` / `To` / `Recover` / `Finally`
sur `Task<Outcome>` et `Task<Outcome<T>>`. Chaque surcharge accepte un `CancellationToken` optionnel,
ce qui permet d’attendre l’ensemble du pipeline :

```csharp
Outcome<Receipt> outcome =
    await TryLoadAmountAsync(orderId, cancellationToken)   // Task<Outcome<Amount>>
        .Then(amount => CheckLimitsAsync(amount), cancellationToken)
        .To(amount => amount.WithVat())
        .Recover(error => Amount.Zero)
        .Then(amount => ChargeAsync(amount), cancellationToken)
        .Finally(
            onSuccess: receipt => LogAsync(receipt),
            onFailure: error => LogAsync(error),
            cancellationToken);
```

## 🎯 Résumé

DiagnosableExceptions brille lorsque :

| Situation         | Bénéfice                        |
| ----------------- | ------------------------------- |
| Invariants métier | Violations sémantiques claires  |
| Validation        | Erreurs comme données           |
| Opérations        | Code métier lisible             |
| Traitement batch  | Gestion d’erreurs non bloquante |
| Intégration       | Meilleur dépannage              |
| Support           | Connaissance structurée         |

La bibliothèque vous aide à exprimer non seulement qu’un échec s’est produit — mais **ce que cela signifie, pourquoi cela a pu arriver et où chercher**.

---

Section précédente: [Guide d’écriture des erreurs](WritingErrorsGuide.fr.md) | Section suivante: [Bonnes pratiques](BestPractices.fr.md)

---