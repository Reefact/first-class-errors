# Patterns d’utilisation

🌍 **Langues :**  
🇬🇧 [English](./UsagePatterns.en.md) | 🇫🇷 Français (ce fichier)

FirstClassErrors permet aussi bien de lever une exception que de transporter une erreur comme une donnée. La décision importante ne dépend pas de la syntaxe préférée, mais de **si l’échec appartient ou non au contrat normal de l’opération**.

Ce guide aide à choisir le bon pattern. Pour l’API complète d’`Outcome` et la composition des pipelines, consultez [Composer avec Outcome](OutcomeGuide.fr.md).

## 🧭 L’échec fait-il partie du contrat de l’opération ?

Un même échec peut légitimement être représenté par une exception ou par un `Outcome` ; la catégorie de l’erreur ne détermine pas, à elle seule, laquelle choisir :

- **Utilisez `Outcome`** lorsque l’échec fait partie du contrat normal de l’opération et doit être traité ou propagé explicitement par l’appelant.
- **Levez une exception** lorsque l’opération ne peut pas remplir son contrat et qu’aucune branche d’échec locale n’est normalement attendue à ce niveau.

Lever une exception ne retire pas la liberté de l’appelant — il peut encore la capturer, la traduire, la retenter ou la journaliser. Cela retire seulement l’échec du type de retour de l’opération. Une fois la question du contrat tranchée, une seconde question reste utile : qui possède la prochaine décision utile, cette méthode ou son appelant ? Cette question affine le choix ; elle ne doit pas remplacer la question du contrat.

Les deux chemins peuvent partir de la même factory documentée. La factory décrit **ce qui s’est produit** ; la lever ou la retourner décrit **comment cet appelant précis choisit de la propager**.

Les sections suivantes parcourent des contextes courants. Là où les deux contrats sont réellement utiles dans un contexte, les deux sont montrés côte à côte ; là où l’un domine clairement, seul celui-ci l’est.

## 🧱 Construire un value object valide

Un value object ne doit jamais entrer dans un état invalide. Que l’échec appartienne au contrat de la méthode de construction, ou en soit entièrement exclu, est un choix de propagation, pas une propriété de l’invariant lui-même — la même vérification dans `Temperature` supporte les deux :

```csharp
// Contrat : renvoie une Temperature valide ou échoue — l'échec n'a pas sa place dans le type de retour de cette méthode.
public static Temperature FromKelvin(decimal kelvin) {
    return TryFromKelvin(kelvin).GetResultOrThrow();
}
```

```csharp
// Contrat : une valeur hors limite fait partie du contrat normal — l'appelant doit la traiter explicitement.
public static Outcome<Temperature> TryFromKelvin(decimal kelvin) {
    if (kelvin < 0) { return Outcome<Temperature>.Failure(InvalidTemperatureError.BelowAbsoluteZero(kelvin, TemperatureUnit.Kelvin)); }

    return Outcome<Temperature>.Success(new Temperature(kelvin));
}
```

Les deux rapportent exactement la même erreur documentée, `InvalidTemperatureError.BelowAbsoluteZero`. `FromKelvin` ne répète pas la vérification : elle escalade l’échec de `TryFromKelvin` avec `GetResultOrThrow()`. Lorsque les deux contrats sont réellement utiles, centralisez la validation dans la version retournant un `Outcome`, puis dérivez-en la version levante — pas l’inverse.

## 🧮 Opérations métier

Des objets métier valides peuvent néanmoins participer à une opération qui viole une règle. Le choix ici est une question de contrat d’API, pas de qui serait théoriquement en mesure de réagir — un appelant d’`Add` pourrait tout à fait capturer et traiter un écart levé, tout comme un appelant de `TryAdd` pourrait tout à fait ignorer un échec retourné. Ce qui diffère, c’est ce que chaque méthode promet à son appelant :

```csharp
// Contrat : les montants fournis doivent déjà être exprimés dans la même devise.
public Amount Add(Amount other) {
    if (Currency != other.Currency) { throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException(); }

    return new Amount(Value + other.Value, Currency);
}
```

```csharp
// Contrat : une incompatibilité de devises est une issue attendue que l'appelant doit traiter.
public Outcome<Amount> TryAdd(Amount other) {
    if (Currency != other.Currency) { return Outcome<Amount>.Failure(InvalidAmountOperationError.CurrencyMismatch(this, other)); }

    return Outcome<Amount>.Success(new Amount(Value + other.Value, Currency));
}
```

Les deux rapportent la même `InvalidAmountOperationError.CurrencyMismatch`. Préférez une factory précise à une exception générique comme `InvalidOperationException` : le code doit indiquer quelle situation métier s’est produite, quelle que soit la méthode qui la porte.

## 📦 Traitement par lots et fichiers

Chaque élément d’un lot peut échouer indépendamment — mais le fait que cet échec appartienne au contrat propre de la boucle, ou invalide tout le fichier, est une décision de politique, pas une propriété du traitement par lots en tant que catégorie.

```csharp
// Contrat : un échec par ligne est attendu et traité localement — le loguer et continuer.
foreach (string line in file) {
    TryParseAmount(line).Finally(
        onSuccess: Process,
        onFailure: Log);
}
```

```csharp
// Contrat : une ligne invalide invalide tout le fichier — arrêter immédiatement.
foreach (string line in file) {
    Amount amount = TryParseAmount(line).GetResultOrThrow();
    Process(amount);
}
```

La première version ne touche jamais au canal d’exception : `Finally` distribue directement vers `Process` ou `Log` à partir de l’`Outcome`, sans vérification intermédiaire d’`IsFailure` ni `Error!` avec l’opérateur de tolérance au null. La seconde appelle `GetResultOrThrow()` directement, donc la première ligne invalide lève et arrête la boucle. Choisissez la récupération par élément quand une ligne invalide ne concerne qu’elle-même ; choisissez le lever immédiat quand c’est l’intégrité du fichier qui est réellement protégée.

## 🌐 Frontières entrantes

Un adapter entrant peut rejeter une interaction parce que le mapping, la validation ou la construction métier a échoué. L’erreur de domaine explique la règle violée ; l’erreur de port primaire explique le résultat à la frontière — consultez [Taxonomie et composition des erreurs](ErrorTaxonomy.fr.md) pour les règles d’imbrication. La direction de l’interaction décide du type de port : `PrimaryPortError` s’applique, que l’échec soit levé ou retourné.

```csharp
DomainError invalidAmount = InvalidMoneyTransferError.AmountNotPositive(request.Amount);

PrimaryPortError rejection = PrimaryPortError.Create(
        Code.RequestRejected,
        diagnosticMessage: $"La requête {request.Id} contient un montant invalide.",
        new PrimaryPortInnerErrors().Add(invalidAmount),
        configureContext: ctx => ctx.Add(ErrCtxKey.RequestId, request.Id))
    .WithPublicMessage(
        shortMessage: "La requête contient des données invalides.");
```

```csharp
// Contrat : l'échec fait partie du type de retour de ce handler — un pipeline sensible aux Outcome le traduit en réponse.
return Outcome<Receipt>.Failure(rejection);
```

```csharp
// Contrat : l'échec franchit cette frontière comme une exception — un filtre ou middleware l'intercepte.
throw rejection.ToException();
```

Les deux propagations transportent la même `rejection` ; seule la façon dont elle quitte cette méthode diffère.

## 🔌 Dépendances sortantes

Une panne de base de données, broker, système de fichiers ou API distante est une interaction sortante ; c’est la direction qui en fait une `SecondaryPortError`, quelle que soit la façon dont l’échec est propagé.

```csharp
SecondaryPortError unavailable = SecondaryPortError.Create(
        Code.PaymentProviderUnavailable,
        diagnosticMessage: "Le fournisseur de paiement a dépassé le délai de 5 secondes.",
        transience: Transience.Transient)
    .WithPublicMessage(
        shortMessage: "Le service de paiement est temporairement indisponible.");
```

```csharp
// Contrat : l'échec fait partie du type de retour de cet appel — l'appelant examine Transience pour décider de retenter ou non.
return Outcome<Receipt>.Failure(unavailable);
```

```csharp
// Contrat : l'échec franchit cette frontière comme une exception — une politique de résilience (ex. un filtre de retry) l'intercepte.
throw unavailable.ToException();
```

`Transience` indique si retenter plus tard peut être utile ; elle n’implémente pas elle-même une politique de retry, et ne dit rien sur la propagation à utiliser — les deux exemples ci-dessus portent la même `Transience.Transient`.

## 🔁 Flux applicatifs en plusieurs étapes

Lorsque plusieurs opérations susceptibles d’échouer doivent s’enchaîner, composez leurs outcomes au lieu de vérifier et déballer chaque résultat manuellement.

```csharp
Outcome<Receipt> result =
    TryCreateAmount(value, currencyCode)
        .Then(CheckLimits)
        .Then(Charge);
```

Le premier échec court-circuite les étapes suivantes et est propagé sans modification. Utilisez une chaîne fluide uniquement lorsqu’elle est plus lisible qu’un branchement explicite.

Le comportement complet de `Then`, `Recover`, `Finally`, des surcharges async et des échappatoires vers les exceptions (`ThrowIfFailure()`, `GetResultOrThrow()`) est documenté dans [Composer avec Outcome](OutcomeGuide.fr.md).

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

### Choisir le contrat

- L’échec fait-il partie des résultats attendus de cette opération ?
- L’appelant doit-il obligatoirement le traiter ou le propager explicitement ?
- L’appelant peut-il raisonnablement agir sur cet échec, ou seulement l’observer ?
- Doit-il être composé avec d’autres opérations, agrégé, ou récupéré localement ?
- Serait-il acceptable qu’il soit ignoré accidentellement par l’appelant ?
- Cette opération est-elle une API `Try...`, un use case, une frontière, ou une primitive interne ?

### Classifier l’erreur

- Une règle métier ?
- Une interaction entrante ?
- Une dépendance sortante ?

### Consommer un `Outcome`

- Branchement explicite pour une décision locale ?
- Chaîne fluide pour une succession linéaire d’étapes ?

Choisissez le contrat à partir de la première liste, le type d’erreur à partir de la deuxième, et le style de consommation à partir de la troisième — pas à partir d’une seule règle absolue imposant de toujours lever ou toujours retourner les erreurs.

---

<div align="center">
<a href="WritingErrorsGuide.fr.md">← Guide d’écriture des erreurs</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="OutcomeGuide.fr.md">Composer avec Outcome →</a>
</div>

---