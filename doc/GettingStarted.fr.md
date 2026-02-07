# Premier pas

DiagnosableExceptions vous aide à considérer les erreurs comme une **connaissance structurée et diagnostiquable**, plutôt que comme de simples messages d’exception.

En quelques minutes, vous allez voir comment :

* définir une exception diagnostiquable  
* utiliser des factories d’exception (indispensables pour la documentation vivante)  
* documenter l’erreur de manière structurée  
* attacher des diagnostics  
* utiliser éventuellement l’erreur sans la lever  

## 1. Définir une exception diagnostiquable (avec une factory)

Pour bénéficier de la **documentation vivante**, les exceptions ne sont pas créées directement avec `new`. Elles sont créées via des **méthodes factory statiques** dans la classe d’exception.

Ce pattern est essentiel car :

* chaque méthode factory représente une **situation d’erreur spécifique**  
* c’est le point d’ancrage du DSL de documentation  
* le générateur de documentation relie les factories à la documentation  

Remarque :

*L’utilisation de méthodes factory pour créer des exceptions est un pattern .NET bien établi pour centraliser et standardiser la création d’exceptions. DiagnosableExceptions s’appuie sur cette idée et fait des factories le point d’ancrage de la documentation d’erreurs structurée et vivante. Au-delà de la documentation, les factories améliorent fortement la lisibilité du code : elles sortent la construction de l’erreur (codes, messages, formatage, formulation) du “happy path”, ce qui permet à la logique métier de rester centrée sur les règles métier plutôt que sur des détails techniques. Un appel comme `throw InvalidAmountOperationException.CurrencyMismatch(a1, a2);` exprime l’intention bien plus clairement qu’une construction d’exception inline. Cette approche s’aligne avec les principes du clean code en séparant les responsabilités, en réduisant la duplication et en donnant à chaque situation d’erreur une représentation explicite et nommée dans le code — tout en fournissant un point unique et cohérent pour attacher diagnostics et documentation.*  

Exemple :

```csharp
[ProvidesErrorsFor(typeof(Amount))]
public sealed class InvalidAmountOperationException : DomainException {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    public static InvalidAmountOperationException CurrencyMismatch(Amount amount1, Amount amount2) {
        return new InvalidAmountOperationException(
            "AMOUNT_CURRENCY_MISMATCH",
            $"Impossible d’effectuer l’opération monétaire car les montants impliqués sont exprimés dans des devises différentes : {amount1} et {amount2}.",
            "Devise différente"
        );
    }

    private InvalidAmountOperationException(string errorCode, string errorMessage, string shortMessage)
        : base(errorCode, errorMessage, shortMessage) { }

}
````

Ici :

* Le **type d’exception** représente une catégorie d’erreurs métier.
* La **méthode factory** représente un cas d’erreur précis.
* Le **code d’erreur** est stable et lisible par machine.
* C’est la méthode factory qui sera documentée.

## 2. Lier la factory à une documentation structurée

Chaque méthode factory est liée à sa documentation via `[DocumentedBy]`.

```csharp
private static ErrorDocumentation CurrencyMismatchDocumentation() {
    return DescribeError.WithTitle("Incohérence de devise des montants")
                        .WithDescription("Cette erreur survient lorsqu’on tente d’utiliser plusieurs montants dans une opération alors qu’ils sont exprimés dans des devises différentes.")
                        .WithRule("Toutes les opérations monétaires doivent impliquer des montants exprimés dans la même devise.")
                        .WithDiagnostic(
                            "Des montants ont été utilisés dans une opération monétaire sans avoir été convertis dans une devise commune.",
                            ErrorCauseType.System,
                            "Vérifiez si tous les montants impliqués ont été convertis dans une devise commune avant d’être utilisés ensemble."
                        )
                        .AndDiagnostic(
                            "Des montants censés être exprimés dans la même devise ont été fournis avec des devises différentes.",
                            ErrorCauseType.SystemOrInput,
                            "Vérifiez les devises associées à chaque montant et confirmez si une devise commune était attendue pour cette opération."
                        )
                        .WithExamples(() => CurrencyMismatch(new Amount(127.33m, Currency.EUR), new Amount(57689.00m, Currency.USD)));
}
```

Cette documentation :

* explique la signification de l’erreur
* énonce la règle violée
* fournit des hypothèses de diagnostic
* donne des exemples réalistes de messages

Il s’agit de connaissance structurée, pas d’un commentaire.

## 3. Utiliser l’exception dans le code métier

```csharp
public Amount Add(Amount other) {
    if (Currency != other.Currency) { throw InvalidAmountOperationException.CurrencyMismatch(this, other); }

    return new Amount(Value + other.Value, Currency);
}
```

La logique métier reste propre et expressive.

## 4. Ou l’utiliser sans lever d’exception (`TryOutcome<T>`)

Pour les scénarios de validation ou de traitement par lots :

```csharp
public static TryOutcome<Amount> TryAdd(Amount a1, Amount a2) {
    if (a1.Currency != a2.Currency) { 
        return TryOutcome<Amount>.Failure(InvalidAmountOperationException.CurrencyMismatch(a1, a2)); 
    }

    return TryOutcome<Amount>.Success(new Amount(a1.Value + a2.Value, a1.Currency));
}
```

Vous pouvez inspecter :

```csharp
if (result.IsFailure) {
    Log(result.Exception);
}
```

Ou escalader :

```csharp
var amount = result.GetOrThrow();
```

## 5. Générer la documentation

Comme les factories sont liées à une documentation structurée :

* les erreurs peuvent être extraites des assemblies
* la documentation peut être générée automatiquement
* le support et les développeurs partagent la même source de vérité

## ✅ Ce que vous y gagnez

Avec DiagnosableExceptions :

* les erreurs sont cohérentes
* la documentation est proche du code
* les diagnostics guident le dépannage
* la connaissance ne dérive pas

Vous passez de :

> « Une exception s’est produite »

à

> « Cette erreur précise et documentée s’est produite, voici ce qu’elle signifie et où chercher. »

---

Section suivante: [Principes de conception](DesignPrinciples.fr.md)

---