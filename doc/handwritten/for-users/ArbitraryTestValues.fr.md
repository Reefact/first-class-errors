# Valeurs de test arbitraires

🌍 **Langues :**  
🇫🇷 Français (ce fichier) | 🇬🇧 [English](./ArbitraryTestValues.en.md)

Une grande partie de l’`Arrange` d’un test est d’ordinaire faite de valeurs qu’il ne vérifie jamais — un code d’erreur, un message de diagnostic, un instant de survenue. Écrites en dur, elles se lisent comme si elles comptaient, et une constante réutilisée dans toute une suite peut faire passer un test pour une mauvaise raison. Une valeur *arbitraire* fournit à la place une entrée valide mais accessoire : la seule entrée qui compte ressort, et les autres s’annoncent comme accessoires.

Deux sources couvrent ce besoin, et toutes deux tirent de la même source aléatoire ambiante :

- **[`JustDummies`](https://github.com/Reefact/first-class-errors)** — un générateur fluide de primitives arbitraires (`JustDummies.Any.Int32()`, `JustDummies.Any.String()`, ...). Un appel `JustDummies.Any.*` renvoie une *recette* ; appelez `.Generate()` pour en tirer la valeur.
- Les **fabriques métier** de **`FirstClassErrors.Testing`** — `ErrorCodeFactory.Any()`, `DiagnosticMessageFactory.Any()`, et consorts — pour le vocabulaire d’erreur qu’une primitive brute ne peut pas exprimer. Chacune renvoie directement la valeur.

Comme les deux passent par la même source, un unique `JustDummies.Any.Reproducibly(...)` rend tout un test rejouable ; et — comme les overrides d’horloge et d’identifiants — la source est bornée, locale au contexte et sûre en tests parallèles. Pour figer les valeurs qu’un test *assertit*, voir [Tests d’erreur déterministes](DeterministicTesting.fr.md).

## Fournir une valeur arbitraire

Comparez un test qui code en dur chaque entrée à un test qui ne garde explicite que la valeur assertée :

```csharp
// 😐 Avant — laquelle de ces valeurs le test vérifie-t-il réellement ?
DomainError error = DomainError
    .Create(ErrorCode.Create("ORDER_NOT_FOUND"), "La commande 42 est introuvable.")
    .WithPublicMessage("La commande n’existe pas.");

Outcome<Order>.Failure(error).ShouldFail().WithCode("ORDER_NOT_FOUND");
```

```csharp
// 🙂 Après — le code est le sujet ; les messages sont arbitraires.
DomainError error = DomainError
    .Create(ErrorCode.Create("ORDER_NOT_FOUND"), DiagnosticMessageFactory.Any())
    .WithPublicMessage(ShortMessageFactory.Any());

Outcome<Order>.Failure(error).ShouldFail().WithCode("ORDER_NOT_FOUND");
```

Une valeur n’est accessoire que si elle ne peut pas orienter le code testé. Si elle alimente une branche, une validation, une sérialisation ou un classement, elle façonne le comportement même si le test ne l’assertit jamais — et elle ne peut alors pas être laissée arbitraire sans risque. Réservez les valeurs arbitraires aux entrées que le test transporte mais sur lesquelles il n’agit pas.

## Le vocabulaire d’erreur : les fabriques métier

Pour les parties d’une erreur qu’un test doit fournir sans jamais les asserter, `FirstClassErrors.Testing` livre une fabrique par concept. Chaque `Any()` renvoie une valeur **valide pour son type** — non vide, et reconnaissable comme arbitraire — tirée de la source ambiante :

| Fabrique | Renvoie |
| --- | --- |
| `ErrorCodeFactory.Any()` | un `ErrorCode` valide non vide, de la forme `ANY_CODE_` + 6 caractères alphanumériques majuscules |
| `DiagnosticMessageFactory.Any()` / `ShortMessageFactory.Any()` / `DetailedMessageFactory.Any()` | un message non vide, reconnaissable comme arbitraire |
| `TransienceFactory.Any()` / `InteractionDirectionFactory.Any()` | une valeur *significative* — jamais la sentinelle `Unknown` |
| `ErrorOriginFactory.Any()` | un `ErrorOrigin` quelconque ; toutes ses valeurs sont significatives, il n’y a donc pas de sentinelle à exclure |

Une fabrique renvoie directement la valeur — le cas courant ne demande aucun `.Generate()`. Utilisez les fabriques d’enum significatif (`TransienceFactory`, `InteractionDirectionFactory`) quand le test a besoin d’une valeur qui déclenche réellement le comportement ; ne recourez à un tirage `JustDummies.Any.Enum<TEnum>()` brut que lorsque n’importe quel membre — sentinelle comprise — convient.

## Les primitives : JustDummies

Pour les primitives arbitraires, utilisez **`JustDummies`** directement. Un appel `JustDummies.Any.*` renvoie un *générateur* — une recette immuable — et `.Generate()` en tire une valeur :

```csharp
int    quantity  = JustDummies.Any.Int32().Generate();
string reference = JustDummies.Any.String().NonEmpty().Generate();
Guid   id        = JustDummies.Any.Guid().Generate();
```

Les contraintes chaînées sur le générateur expriment ce que le code environnant *exige* de la valeur — une longueur, un intervalle, un préfixe — jamais ce que le test assertit. La surface complète des générateurs (contraintes, collections, composition via `As`/`Combine`, `.OrNull()`) est documentée avec `JustDummies` lui-même.

Les garanties s’arrêtent à la validité du type. Un générateur ne vise aucune précondition métier — `JustDummies.Any.Int32()` peut être négatif, `JustDummies.Any.String()` n’est pas un e-mail bien formé — donc un value object au contrat plus strict se construit en transformant une primitive sous contrainte : `JustDummies.Any.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create).Generate()`.

## Reproduire une exécution en échec

La source n’est pas seedée par défaut : les valeurs diffèrent donc d’une exécution à l’autre. C’est délibéré : un test qui ne passe que pour une valeur particulière dépend de quelque chose qu’il n’énonce pas, et faire varier la valeur révèle ce couplage.

Quand une exécution mérite d’être reproduite, enveloppez le corps du test dans `JustDummies.Any.Reproducibly`. La méthode épingle une graine fraîche pour l’exécution et, si le corps lève une exception, **rapporte cette graine** avant de laisser l’échec se propager — un test rouge te dit ainsi exactement comment le rejouer :

```csharp
[Fact]
public void Some_value_sensitive_test() =>
    JustDummies.Any.Reproducibly(() => {
        // ... arrange avec les fabriques et JustDummies.Any, act, assert ...
    });
```

En cas d’échec, la graine est écrite sur `Console.Error` par défaut ; passe le writer de ton framework (par exemple l’`ITestOutputHelper.WriteLine` de xUnit) pour l’y router. Rejoue l’exécution en redonnant la graine rapportée :

```csharp
JustDummies.Any.Reproducibly(1234, () => {
    // ... le même corps ...
});
```

Reproduire une exécution nécessite la **même séquence** de tirages : un corps dont l’ordre dépend d’un état externe non déterministe n’est pas entièrement rejouable à partir de la seule graine. Une surcharge asynchrone, `JustDummies.Any.Reproducibly(Func<Task>)`, existe pour les corps de test `async`. Comme les fabriques, les primitives et les seams d’horloge et d’identifiants ci-dessous tirent tous de la même source ambiante, un seul `Reproducibly` les rejoue ensemble.

### Fixer la graine sans corps à envelopper

`Reproducibly` exige un délégué. Un appelant qui observe un test depuis l’extérieur — un adaptateur de framework de test exécutant du code *avant* et *après* la méthode de test — n’en possède aucun : il fixe donc la source ambiante avec une portée qu’il ouvre et dispose lui-même :

```csharp
IDisposable scope = JustDummies.Any.UseSeed(1234);
// ... le test s’exécute ...
scope.Dispose();
```

La portée suit le contexte d’exécution et s’imbrique exactement comme `Reproducibly`, et la disposer restaure ce qui était fixé auparavant. Ce qu’elle ne fait **pas**, c’est rapporter la graine quand le test échoue : c’est à celui qui ouvre la portée de dire au lecteur quelle graine rejouer.

Cette responsabilité s’étend à l’extrait de rejeu. Lorsqu’un générateur échoue lui-même, le message de l’`AnyGenerationException` nomme la façon de rejouer l’exécution — par défaut `Any.Reproducibly(1234, ...)`, ce qui est la mauvaise instruction pour un test ne contenant aucun appel de ce genre. Un appelant qui fixe la graine depuis l’extérieur l’énonce, et son instruction est citée telle quelle à la place :

```csharp
JustDummies.Any.UseSeed(1234, "[Reproducible(Seed = 1234)]");
```

Dans un corps de test, préférez `Reproducibly` : il rapporte la graine pour vous. Ne recourez à `UseSeed` que lorsqu’il n’y a aucun corps à envelopper.

### Sur xUnit v3 : `[Reproducible]`

Le package compagnon `JustDummies.Xunit` fait l’enveloppement pour vous. Marquez un test, une classe ou l’assembly entier, et ses valeurs arbitraires sont tirées d’une graine fixée, rapportée **uniquement lorsque le test échoue** :

```csharp
[Fact, Reproducible]
public void Some_value_sensitive_test() {
    // ... arrange avec les fabriques et JustDummies.Any, act, assert ...
}
```

Une exécution en échec écrit `Reproduce this run with [Reproducible(Seed = 1234)]` dans la sortie du test ; fixez `[Reproducible(Seed = 1234)]` pour la rejouer. Chaque cas d’une théorie tire sa propre graine, et une déclaration au niveau méthode l’emporte sur une déclaration au niveau classe ou assembly. Ce n’est qu’une commodité : `Reproducibly` reste la forme portable et fonctionne sur tous les frameworks.

## `OccurredAt` et `InstanceId` arbitraires

Les données d’occurrence sont arbitraires au même sens : un test a souvent besoin qu’elles soient stables sans en vérifier l’instant ou l’identifiant exact. Les seams de l’horloge et des identifiants proposent donc un `UseAny` en pendant de leur `UseFixed`. `Clock.UseAny()` fige un unique instant arbitraire pour la portée, tandis que `InstanceIds.UseAny()` attribue à chaque erreur son propre identifiant arbitraire distinct :

```csharp
DomainError NewError() =>
    DomainError.Create(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any()).WithPublicMessage(ShortMessageFactory.Any());

using (Clock.UseAny())
using (InstanceIds.UseAny()) {
    DomainError first  = NewError();
    DomainError second = NewError();

    Check.That(second.OccurredAt).IsEqualTo(first.OccurredAt);    // un instant arbitraire, partagé
    Check.That(second.InstanceId).IsNotEqualTo(first.InstanceId); // des identifiants arbitraires distincts
}
```

Les deux tirent de la même source ambiante que `JustDummies.Any` : les exécuter à l’intérieur d’un `JustDummies.Any.Reproducibly` rend donc leur instant et leurs identifiants reproductibles eux aussi. Pour épingler un instant ou un identifiant *précis*, utilisez `UseFixed` — voir [Tests d’erreur déterministes](DeterministicTesting.fr.md).

## Portée et tests parallèles

`JustDummies.Any.Reproducibly`, `Clock.UseAny` et `InstanceIds.UseAny` ne prennent effet que pour l’exécution ou le bloc `using` qu’ils enveloppent, et la source arbitraire est restaurée à leur sortie. Cette source est stockée dans un `AsyncLocal` : elle suit le flux d’exécution du test lui-même et ne fuit jamais dans d’autres tests s’exécutant en même temps.

### À l’intérieur d’un test qui parallélise

Suivre le flux d’exécution du test signifie aussi que la source atteint les threads que le test démarre lui-même — un `Parallel.For`, un `Task.WhenAll`. Y puiser depuis plusieurs d’entre eux à la fois est sûr : les valeurs restent arbitraires et bien formées, quel que soit le nombre de threads qui les tirent.

Ce que le parallélisme coûte, c’est le *rejeu*. Les tirages concurrents s’entrelacent : une graine ne fixe plus quelle valeur atterrit dans quel appel, et une exécution parallélisée ne se reproduit pas à partir de sa seule graine. Si vous n’avez besoin que de dummies, il n’y a rien à faire. Si vous avez besoin que l’exécution rejoue, donnez à chaque unité de travail sa propre portée et dérivez sa graine de celle de l’exécution :

```csharp
Any.Reproducibly(() => {
    Parallel.For(0, 64, index => {
        using (Any.UseSeed(HashCode.Combine(runSeed, index))) {
            sut.Handle(Any.String().NonEmpty().Generate());
        }
    });
});
```

Chaque itération possède alors sa propre séquence, et l’exécution entière rejoue pour un `runSeed` donné.

## Checklist de revue

Avant de recourir à une valeur arbitraire, vérifiez que :

- la valeur ne **modifie pas** le chemin fonctionnel exercé par le test — elle ne doit alimenter ni une branche, ni une validation, ni une sérialisation, ni un classement, même indirectement ;
- la valeur n’est réellement pas vérifiée par le test — sinon utilisez un littéral ;
- une fabrique d’enum significatif (`TransienceFactory`, `InteractionDirectionFactory`) est utilisée quand le test a besoin d’une valeur significative, plutôt qu’un tirage `JustDummies.Any.Enum<TEnum>()` brut ;
- un test sensible aux valeurs est enveloppé dans `JustDummies.Any.Reproducibly`, pour qu’une exécution en échec rapporte la graine à rejouer ;
- `Clock.UseAny` / `InstanceIds.UseAny` servent pour des données d’occurrence stables mais sans importance, et `UseFixed` lorsque la valeur exacte est assertée.

---

<div align="center">
<a href="DeterministicTesting.fr.md">← Tests d’erreur déterministes</a> · <a href="README.fr.md#-documentation">↑ Table des matières</a> · <a href="OperationalIntegration.fr.md">Générer et publier le catalogue →</a>
</div>

---
