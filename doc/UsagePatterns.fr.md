# Patterns d’utilisation

🌍 **Langues :**  
🇬🇧 [English](./UsagePatterns.en.md) | 🇫🇷 Français (ce fichier)

FirstClassErrors permet aussi bien de lever une exception que de transporter une erreur comme une donnée. La décision importante ne dépend pas de la syntaxe préférée, mais de **qui est censé décider de la suite : cette méthode, ou son appelant**.

Ce guide aide à choisir le bon pattern. Pour l’API complète d’`Outcome` et la composition des pipelines, consultez [Composer avec Outcome](OutcomeGuide.fr.md).

## 🧭 Qui décide : cette méthode, ou son appelant ?

Le même type d’échec — une valeur invalide, une règle métier violée, une dépendance injoignable — supporte honnêtement les deux représentations : lever une exception ou retourner un `Outcome`. Ce qui décide n’est pas la catégorie de situation, mais qui est censé agir ensuite :

- **Cette méthode décide, ici même.** Un état invalide ne doit tout simplement pas exister, et l’appelant n’a rien d’utile à ajouter. Levez l’erreur documentée via `.ToException()` ; l’exception interrompt immédiatement l’opération en cours.
- **L’appelant décide.** L’échec est une branche normale que l’appelant doit pouvoir inspecter, loguer, retenter, agréger ou dont il doit pouvoir se remettre. Retournez l’erreur documentée sous forme d’`Outcome<T>.Failure(...)` (ou `Outcome.Failure(...)`) ; rien n’est levé, l’échec voyage comme une donnée.

Les deux chemins peuvent partir de la même factory documentée. La factory décrit **ce qui s’est produit** ; la lever ou la retourner décrit **comment cet appelant précis choisit de la propager**.

Les sections suivantes parcourent des contextes courants. Là où les deux intentions sont réellement utiles dans un contexte, les deux sont montrées côte à côte ; là où l’une domine clairement, seule celle-ci l’est.

## 🧱 Construire un value object valide

Un value object ne doit jamais entrer dans un état invalide. Que cela interrompe immédiatement la méthode courante ou devienne une décision pour l’appelant est un choix de propagation, pas une propriété de l’invariant lui-même — la même vérification dans `Temperature` supporte les deux :

```csharp
// Intention : l’appelant n’a rien à décider — une température sous le zéro absolu ne doit pas exister.
public static Temperature FromKelvin(decimal kelvin) {
    return TryFromKelvin(kelvin).GetResultOrThrow();
}
```

```csharp
// Intention : l’appelant — un capteur, un fichier parsé, un champ de formulaire — doit réagir lui-même.
public static Outcome<Temperature> TryFromKelvin(decimal kelvin) {
    if (kelvin < 0) { return Outcome<Temperature>.Failure(InvalidTemperatureError.BelowAbsoluteZero(kelvin, TemperatureUnit.Kelvin)); }

    return Outcome<Temperature>.Success(new Temperature(kelvin));
}
```

Les deux rapportent exactement la même erreur documentée, `InvalidTemperatureError.BelowAbsoluteZero`. `FromKelvin` ne répète pas la vérification : elle escalade l’échec de `TryFromKelvin` avec `GetResultOrThrow()`. Écrivez d’abord la version qui retourne un `Outcome`, puis dérivez-en la version levante — pas l’inverse.

## 🧮 Opérations métier

Des objets métier valides peuvent néanmoins participer à une opération qui viole une règle — et là encore, lever ou retourner est un choix sur qui doit réagir, pas sur la règle elle-même.

```csharp
// Intention : l’appelant n’a rien à décider — mélanger des devises dans un même Amount ne doit pas arriver.
public Amount Add(Amount other) {
    if (Currency != other.Currency) { throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException(); }

    return new Amount(Value + other.Value, Currency);
}
```

```csharp
// Intention : l’appelant — par ex. un job de rapprochement de relevé additionnant de nombreuses lignes — décide ce que signifie un écart.
public Outcome<Amount> TryAdd(Amount other) {
    if (Currency != other.Currency) { return Outcome<Amount>.Failure(InvalidAmountOperationError.CurrencyMismatch(this, other)); }

    return Outcome<Amount>.Success(new Amount(Value + other.Value, Currency));
}
```

Les deux rapportent la même `InvalidAmountOperationError.CurrencyMismatch`. Préférez une factory précise à une exception générique comme `InvalidOperationException` : le code doit indiquer quelle situation métier s’est produite, quelle que soit la méthode qui la porte.

## 📦 Traitement par lots et fichiers

Chaque élément d’un lot peut échouer indépendamment — mais le fait qu’un échec doive ou non arrêter tout le lot est, encore une fois, une intention, pas une propriété du traitement par lots en tant que catégorie.

```csharp
// Intention : l’appelant (le job batch) décide par élément — loguer et continuer.
foreach (string line in file) {
    Outcome<Amount> result = TryParseAmount(line);
    if (result.IsFailure) { Log(result.Error!); continue; }

    Process(result.GetResultOrThrow());
}
```

```csharp
// Intention : une ligne invalide rend tout le fichier non fiable — arrêter immédiatement.
foreach (string line in file) {
    Amount amount = TryParseAmount(line).GetResultOrThrow();
    Process(amount);
}
```

Les deux réutilisent le même `GetResultOrThrow()` : la première version ne l’appelle qu’après avoir écarté `IsFailure` pour cet élément, donc elle ne lève jamais à cet endroit ; la seconde l’appelle directement, donc la première ligne invalide lève et arrête la boucle. Choisissez la récupération par élément quand une ligne invalide ne concerne qu’elle-même ; choisissez le lever immédiat quand c’est l’intégrité du fichier qui est réellement protégée.

## 🌐 Frontières entrantes

Un adapter entrant peut rejeter une interaction parce que le mapping, la validation ou la construction métier a échoué. Ici, c’est la situation elle-même — une interaction entrante — qui décide du type de port ; `PrimaryPortError` s’applique dans les deux cas.

```csharp
DomainError invalidAmount = InvalidMoneyTransferError.AmountNotPositive(request.Amount);

return PrimaryPortError.Create(
        Code.RequestRejected,
        diagnosticMessage: $"La requête {request.Id} contient un montant invalide.",
        new PrimaryPortInnerErrors().Add(invalidAmount),
        configureContext: ctx => ctx.Add(ErrCtxKey.RequestId, request.Id))
    .WithPublicMessage(
        shortMessage: "La requête contient des données invalides.");
```

L’erreur de domaine explique la règle violée. L’erreur de port primaire explique le résultat à la frontière. Consultez [Taxonomie et composition des erreurs](ErrorTaxonomy.fr.md) pour les règles d’imbrication. Que cette `PrimaryPortError` franchisse ensuite la frontière comme une exception levée ou comme un `Outcome` retourné suit toujours la même question que ci-dessus — qui est censé décider ensuite, cet adapter ou son appelant.

## 🔌 Dépendances sortantes

Une panne de base de données, broker, système de fichiers ou API distante est une interaction sortante ; c’est la direction qui en fait une `SecondaryPortError`, pas une intention.

```csharp
return SecondaryPortError.Create(
        Code.PaymentProviderUnavailable,
        diagnosticMessage: "Le fournisseur de paiement a dépassé le délai de 5 secondes.",
        transience: Transience.Transient)
    .WithPublicMessage(
        shortMessage: "Le service de paiement est temporairement indisponible.");
```

`Transience` indique si retenter plus tard peut être utile ; elle n’implémente pas elle-même une politique de retry. Comme ailleurs, lever cette erreur ou la retourner comme `Outcome` en échec est un choix de propagation distinct, indépendant de ce que `Transience` dit de la dépendance.

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