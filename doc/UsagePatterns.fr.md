# Patterns d’utilisation

DiagnosableExceptions est particulièrement utile lorsque les erreurs ne sont pas de simples défaillances techniques, mais des **événements porteurs de sens dans la vie du système**.  
Voici des patterns courants où la bibliothèque apporte clarté et structure.

## 🧱 1. Invariants de Value Object

Lors de la création d’un value object, les états invalides doivent être rejetés.

```csharp
public static Amount From(decimal value, Currency currency) {
    if (value < 0) { throw InvalidAmountException.NegativeValue(value, currency); }

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
public TryOutcome<Amount> TryCreateAmount(decimal value, string currencyCode){
    if (!Currency.TryParse(currencyCode, out var currency))    {
        return TryOutcome<Amount>.Failure(InvalidAmountException.UnknownCurrency(currencyCode)); }

    return TryOutcome<Amount>.Success(new Amount(value, currency));
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
    if (Currency != other.Currency) { throw InvalidAmountOperationException.CurrencyMismatch(this, other); }

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
        Log(result.Exception);
        
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
             .Bind(CheckCurrency)
             .Bind(CheckLimits);
```

Chaque échec peut porter une exception diagnostiquable, ce qui garde un modèle cohérent tout en évitant des levées d’exception incontrôlées.

## 🧩 7. Logging orienté support

Comme les exceptions portent des diagnostics structurés, les logs deviennent plus exploitables :

* codes d’erreur stables
* messages courts porteurs de sens
* causes documentées

Les équipes support peuvent relier les événements runtime à des cas d’erreur documentés.

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