# Patterns d’utilisation

🌍 **Langues :**  
🇬🇧 [English](./UsagePatterns.en.md) | 🇫🇷 Français (ce fichier)

FirstClassErrors permet aussi bien de lever une exception que de transporter une erreur comme une donnée. La décision importante ne dépend pas de la syntaxe préférée, mais de **ce que signifie l’échec dans le flux courant**.

Ce guide aide à choisir le bon pattern. Pour l’API complète d’`Outcome` et la composition des pipelines, consultez [Composer avec Outcome](OutcomeGuide.fr.md).

## 🧭 Choisir selon le sens de l’échec

| Situation | Représentation conseillée | Pourquoi |
| --- | --- | --- |
| Un invariant métier est violé et l’opération ne peut pas continuer | lever une `DomainError` via `.ToException()` | l’échec interrompt l’opération et appartient au domaine |
| Une entrée invalide est un résultat attendu de validation ou de parsing | retourner `Outcome<T>.Failure(...)` | l’appelant peut traiter l’échec sans utiliser les exceptions comme contrôle de flux |
| Un élément échoue dans un batch | retourner un `Outcome<T>` par élément | un échec ne doit pas forcément interrompre tout le lot |
| Une frontière entrante rejette une interaction | utiliser une `PrimaryPortError` | elle conserve la direction entrante et l’éventuelle cause métier |
| Une dépendance sortante échoue | utiliser une `SecondaryPortError` | elle conserve la direction sortante et la transience |
| Un échec doit franchir une frontière applicative sous forme d’exception | appeler `ThrowIfFailure()` ou `GetResultOrThrow()` à cette frontière | l’erreur reste une donnée jusqu’au point d’escalade choisi |

La même factory documentée peut servir à plusieurs transports. L’erreur décrit **ce qui s’est produit** ; la lever ou la retourner décrit **comment cet appelant choisit de la propager**.

## 🧱 Invariants métier

Un value object ou une entité ne doit jamais entrer dans un état invalide. Lorsque la construction ou l’opération ne peut pas continuer, levez l’erreur métier documentée.

```csharp
public static Amount From(decimal value, Currency currency) {
    if (value < 0) {
        throw InvalidAmountOperationError.NegativeAmount(value).ToException();
    }

    return new Amount(value, currency);
}
```

Le happy path reste lisible tandis que la factory centralise le code, les messages, le contexte et la documentation.

## 🧮 Opérations métier

Des objets métier valides peuvent néanmoins participer à une opération qui viole une règle.

```csharp
public Amount Add(Amount other) {
    if (Currency != other.Currency) {
        throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException();
    }

    return new Amount(Value + other.Value, Currency);
}
```

Préférez une factory précise à une exception générique comme `InvalidOperationException`. Le code doit indiquer quelle situation métier s’est produite.

## 📥 Échecs de validation attendus

Les entrées utilisateur, le parsing et la validation métier échouent souvent dans le cadre du flux normal. Retournez l’erreur comme donnée lorsque l’appelant est censé décider de la suite.

```csharp
public Outcome<Amount> TryCreateAmount(decimal value, string currencyCode) {
    if (!Currency.TryParse(currencyCode, out Currency currency)) {
        return Outcome<Amount>.Failure(
            InvalidAmountOperationError.UnknownCurrency(currencyCode));
    }

    if (value < 0) {
        return Outcome<Amount>.Failure(
            InvalidAmountOperationError.NegativeAmount(value));
    }

    return Outcome<Amount>.Success(new Amount(value, currency));
}
```

L’échec transporte toujours la même `Error` structurée ; elle n’est simplement pas levée à ce stade.

## 📦 Traitement par lots et fichiers

Dans un batch, chaque élément peut échouer indépendamment. Traitez chaque `Outcome<T>` et poursuivez lorsque cela correspond à la règle métier.

```csharp
foreach (string line in file) {
    Outcome<Amount> result = TryParseAmount(line);

    if (result.IsFailure) {
        Log(result.Error!);
        continue;
    }

    Process(result.Value);
}
```

Ce pattern n’est pertinent que si continuer est intentionnel. Si un élément invalide rend tout le fichier invalide, retournez ou levez plutôt une erreur au niveau du fichier.

## 🌐 Frontières entrantes

Un adapter entrant peut rejeter une interaction parce que le mapping, la validation ou la construction métier a échoué.

```csharp
DomainError invalidAmount = InvalidAmountOperationError.NegativeAmount(request.Amount);

return PrimaryPortError.Create(
        Code.RequestRejected,
        diagnosticMessage: $"La requête {request.Id} contient un montant invalide.",
        new PrimaryPortInnerErrors().Add(invalidAmount),
        configureContext: ctx => ctx.Add(ErrCtxKey.RequestId, request.Id))
    .WithPublicMessage(
        shortMessage: "La requête contient des données invalides.");
```

L’erreur de domaine explique la règle violée. L’erreur de port primaire explique le résultat à la frontière. Consultez [Taxonomie et composition des erreurs](ErrorTaxonomy.fr.md) pour les règles d’imbrication.

## 🔌 Dépendances sortantes

Une panne de base de données, broker, système de fichiers ou API distante est une interaction sortante.

```csharp
return SecondaryPortError.Create(
        Code.PaymentProviderUnavailable,
        diagnosticMessage: "Le fournisseur de paiement a dépassé le délai de 5 secondes.",
        transience: Transience.Transient)
    .WithPublicMessage(
        shortMessage: "Le service de paiement est temporairement indisponible.");
```

`Transience` indique si retenter plus tard peut être utile. Elle n’implémente pas elle-même une politique de retry.

## 🔁 Flux applicatifs en plusieurs étapes

Lorsque plusieurs opérations susceptibles d’échouer doivent s’enchaîner, composez leurs outcomes au lieu de vérifier et déballer chaque résultat manuellement.

```csharp
Outcome<Receipt> result =
    TryCreateAmount(value, currencyCode)
        .Then(CheckLimits)
        .Then(Charge);
```

Le premier échec court-circuite les étapes suivantes et est propagé sans modification. Utilisez une chaîne fluide uniquement lorsqu’elle est plus lisible qu’un branchement explicite.

Le comportement complet de `Then`, `To`, `Recover`, `Finally`, des surcharges async et des échappatoires est documenté dans [Composer avec Outcome](OutcomeGuide.fr.md).

## 🧩 Logging et support

Au point où l’échec est traité, loguez l’erreur structurée plutôt que seulement un message public.

Les champs utiles comprennent :

- `Code` pour les regroupements et dashboards ;
- `InstanceId` pour corréler une occurrence ;
- `OccurredAt` pour l’horodatage ;
- `DiagnosticMessage` pour l’analyse interne ;
- `Context` pour les faits propres à l’occurrence ;
- `InnerErrors` pour la chaîne causale.

Les messages publics sont destinés aux appelants. Les informations de diagnostic sont destinées aux logs, au support et aux développeurs.

## 📌 Checklist de décision

Avant de choisir un pattern, demandez-vous :

1. Cet échec est-il attendu dans le flux normal ?
2. L’opération courante doit-elle s’arrêter immédiatement ?
3. S’agit-il d’une règle métier, d’une condition de frontière entrante ou d’une défaillance de dépendance sortante ?
4. Qui doit prendre la prochaine décision : cette méthode ou son appelant ?
5. Une chaîne `Outcome` serait-elle plus lisible qu’un branchement explicite ?

Choisissez la représentation à partir de ces réponses, et non d’une règle absolue imposant de toujours lever ou toujours retourner les erreurs.

---

<div align="center">
<a href="WritingErrorsGuide.fr.md">← Guide d’écriture des erreurs</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="OutcomeGuide.fr.md">Composer avec Outcome →</a>
</div>

---