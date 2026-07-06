# Premier pas

🌍 **Langues:**  
🇬🇧 [English](./GettingStarted.en.md) | 🇫🇷 Français (ce fichier)

FirstClassErrors vous aide à considérer les erreurs comme une **connaissance structurée et diagnostiquable**, plutôt que comme de simples messages d’exception.

En quelques minutes, vous allez voir comment :

* définir une erreur (avec une factory)  
* utiliser des factories d’erreur (indispensables pour la documentation vivante)  
* documenter l’erreur de manière structurée  
* attacher des diagnostics  
* utiliser éventuellement l’erreur sans la lever  

## 1️⃣ Définir une erreur (avec une factory)

Pour bénéficier de la **documentation vivante**, les erreurs ne sont pas créées directement avec `new`. Elles sont créées via des **méthodes factory statiques** dans la classe d’erreur.

Ce pattern est essentiel car :

* chaque méthode factory représente une **situation d’erreur spécifique**  
* c’est le point d’ancrage du DSL de documentation  
* le générateur de documentation relie les factories à la documentation  

Remarque :

*L’utilisation de méthodes factory pour créer des erreurs est un pattern .NET bien établi pour centraliser et standardiser la création d’erreurs. FirstClassErrors s’appuie sur cette idée et fait des factories le point d’ancrage de la documentation d’erreurs structurée et vivante. Au-delà de la documentation, les factories améliorent fortement la lisibilité du code : elles sortent la construction de l’erreur (codes, messages, formatage, formulation) du “happy path”, ce qui permet à la logique métier de rester centrée sur les règles métier plutôt que sur des détails techniques. Un appel comme `throw InvalidAmountOperationError.CurrencyMismatch(a1, a2).ToException();` exprime l’intention bien plus clairement qu’une construction d’erreur inline. Cette approche s’aligne avec les principes du clean code en séparant les responsabilités, en réduisant la duplication et en donnant à chaque situation d’erreur une représentation explicite et nommée dans le code — tout en fournissant un point unique et cohérent pour attacher diagnostics et documentation.*  

Exemple :

```csharp
[ProvidesErrorsFor(nameof(Amount))]
public static class InvalidAmountOperationError {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static DomainError CurrencyMismatch(Amount amount1, Amount amount2) {
        return DomainError.Create(
                Code.CurrencyMismatch,
                diagnosticMessage: $"Impossible d’effectuer l’opération monétaire car les montants impliqués sont exprimés dans des devises différentes : {amount1} et {amount2}.")
            .WithPublicMessage(
                shortMessage: "Devise différente",
                detailedMessage: "Les montants impliqués dans l’opération sont exprimés dans des devises différentes.");
    }

    private static class Code {
        public static readonly ErrorCode CurrencyMismatch = ErrorCode.Create("AMOUNT_CURRENCY_MISMATCH");
    }

}
````

Ici :

* Le **type d’erreur** représente une catégorie d’erreurs métier.
* La **méthode factory** représente un cas d’erreur précis.
* Le **code d’erreur** est stable et lisible par machine.
* C’est la méthode factory qui sera documentée.

L’erreur est construite en deux étapes : `DomainError.Create(...)` capture l’information interne obligatoire (le `code` d’erreur et le `diagnosticMessage` interne), et `.WithPublicMessage(shortMessage, detailedMessage)` fournit les messages publics et produit l’erreur finale. Le `shortMessage` obligatoire est un résumé public sûr, le `detailedMessage` optionnel est un détail public maîtrisé, et le `diagnosticMessage` est destiné aux logs, au support et aux développeurs — jamais exposé aux clients par défaut. Il n’y a pas de `.Build()`, et les constructeurs publics sont internes, de sorte qu’une erreur ne peut jamais rester sans son message public.

Vous ne faites jamais `new` sur l’erreur ni sur l’exception vous-même : l’erreur est créée via le builder étagé dans la factory, et pour lever, vous appelez `error.ToException()` (voir section 4).

## 2️⃣ Lier la factory à une documentation structurée

Chaque méthode factory est liée à sa documentation via `[DocumentedBy]`.

```csharp
private static ErrorDocumentation CurrencyMismatchDocumentation() {
    return DescribeError.WithTitle("Incohérence de devise des montants")
                        .WithDescription("Cette erreur survient lorsqu’on tente d’utiliser plusieurs montants dans une opération alors qu’ils sont exprimés dans des devises différentes.")
                        .WithRule("Toutes les opérations monétaires doivent impliquer des montants exprimés dans la même devise.")
                        .WithDiagnostic(
                            "Des montants ont été utilisés dans une opération monétaire sans avoir été convertis dans une devise commune.",
                            ErrorOrigin.Internal,
                            "Vérifiez si tous les montants impliqués ont été convertis dans une devise commune avant d’être utilisés ensemble."
                        )
                        .AndDiagnostic(
                            "Des montants censés être exprimés dans la même devise ont été fournis avec des devises différentes.",
                            ErrorOrigin.InternalOrExternal,
                            "Vérifiez les devises associées à chaque montant et confirmez si une devise commune était attendue pour cette opération."
                        )
                        .WithExamples(() => CurrencyMismatch(new Amount(127.33m, Currency.EUR), new Amount(57689.00m, Currency.USD)));
}
```

Chaque diagnostic déclare une **origine** via `ErrorOrigin`, dont les valeurs sont `Internal`, `External` et `InternalOrExternal` — indiquant si la cause se situe à l’intérieur du système, à l’extérieur (entrée) ou peut être l’une ou l’autre.

Cette documentation :

* explique la signification de l’erreur
* énonce la règle violée
* fournit des hypothèses de diagnostic
* donne des exemples réalistes de messages

Il s’agit de connaissance structurée, pas d’un commentaire.

## 3️⃣ Ajouter un contexte d’erreur structuré (`ErrorContext`)

Quand une information est utile pour diagnostiquer **une occurrence précise**, ajoutez-la dans le contexte.

```csharp
return PrimaryPortError.Create(
        Code.DateOutOfStatementPeriod,
        diagnosticMessage: $"Transaction datée du {transactionDate} hors période [{periodStart};{periodEnd}].",
        transience: Transience.NonTransient,
        configureContext: ctx => ctx.Add(ErrCtxKey.TransactionDate, transactionDate))
    .WithPublicMessage(
        shortMessage: "Date de transaction hors période.",
        detailedMessage: "La date de transaction se situe hors de la période du relevé autorisée.");
```

Le contexte est porté par l’`Error` ; lorsqu’une exception est ensuite produite avec `error.ToException()`, on y accède via `exception.Error.Context`.

Bonnes pratiques :

* utilisez des clés nommées et stables (`ErrorContextKey<T>`)
* ajoutez le contexte au niveau des factories
* évitez les données sensibles ou trop volumineuses

## 4️⃣ Utiliser l’exception dans le code métier

```csharp
public Amount Add(Amount other) {
    if (Currency != other.Currency) { throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException(); }

    return new Amount(Value + other.Value, Currency);
}
```

La logique métier reste propre et expressive.

## 5️⃣ Ou l’utiliser sans lever d’exception (`Outcome<T>`)

Pour les scénarios de validation ou de traitement par lots :

```csharp
public static Outcome<Amount> TryAdd(Amount a1, Amount a2) {
    if (a1.Currency != a2.Currency) { 
        return Outcome<Amount>.Failure(InvalidAmountOperationError.CurrencyMismatch(a1, a2)); 
    }

    return Outcome<Amount>.Success(new Amount(a1.Value + a2.Value, a1.Currency));
}
```

Remarque : `Failure(...)` prend une **`Error`** — la factory en renvoie une directement, donc aucune exception n’est impliquée.

Vous pouvez inspecter :

```csharp
if (result.IsFailure) {
    Log(result.Error);
}
```

Ou escalader :

```csharp
var amount = result.GetResultOrThrow();
```

## 6️⃣ Générer la documentation

Comme les factories sont liées à une documentation structurée :

* les erreurs peuvent être extraites des assemblies
* la documentation peut être générée automatiquement
* le support et les développeurs partagent la même source de vérité

## ✅ Ce que vous y gagnez

Avec FirstClassErrors :

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