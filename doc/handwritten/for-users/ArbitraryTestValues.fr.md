# Valeurs de test arbitraires

🌍 **Langues :**  
🇫🇷 Français (ce fichier) | 🇬🇧 [English](./ArbitraryTestValues.en.md)

Une grande partie de l’`Arrange` d’un test est d’ordinaire faite de valeurs qu’il ne vérifie jamais — un code d’erreur, un message de diagnostic, un instant de survenue. Écrites en dur, elles se lisent comme si elles comptaient, et une constante réutilisée dans toute une suite peut faire passer un test pour une mauvaise raison. Une valeur *arbitraire* fournit à la place une entrée valide mais accessoire : la seule entrée qui compte ressort, et les autres s’annoncent comme accessoires.

Deux sources couvrent ce besoin, et toutes deux tirent de la même source aléatoire ambiante :

- **[`Dummies`](https://github.com/Reefact/first-class-errors)** — un générateur fluide de primitives arbitraires (`Dummies.Any.Int32()`, `Dummies.Any.String()`, ...). Un appel `Dummies.Any.*` renvoie une *recette* ; appelez `.Generate()` pour en tirer la valeur.
- Les **fabriques métier** de **`FirstClassErrors.Testing`** — `ErrorCodeFactory.Any()`, `DiagnosticMessageFactory.Any()`, et consorts — pour le vocabulaire d’erreur qu’une primitive brute ne peut pas exprimer. Chacune renvoie directement la valeur.

Comme les deux passent par la même source, un unique `Dummies.Any.Reproducibly(...)` rend tout un test rejouable ; et — comme les overrides d’horloge et d’identifiants — la source est bornée, locale au contexte et sûre en tests parallèles. Pour figer les valeurs qu’un test *assertit*, voir [Tests d’erreur déterministes](DeterministicTesting.fr.md).

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

Une fabrique renvoie directement la valeur — le cas courant ne demande aucun `.Generate()`. Utilisez les fabriques d’enum significatif (`TransienceFactory`, `InteractionDirectionFactory`) quand le test a besoin d’une valeur qui déclenche réellement le comportement ; ne recourez à un tirage `Dummies.Any.Enum<TEnum>()` brut que lorsque n’importe quel membre — sentinelle comprise — convient.

## Les primitives : Dummies

Pour les primitives arbitraires, utilisez **`Dummies`** directement. Un appel `Dummies.Any.*` renvoie un *générateur* — une recette immuable — et `.Generate()` en tire une valeur :

```csharp
int    quantity  = Dummies.Any.Int32().Generate();
string reference = Dummies.Any.String().NonEmpty().Generate();
Guid   id        = Dummies.Any.Guid().Generate();
```

Les contraintes chaînées sur le générateur expriment ce que le code environnant *exige* de la valeur — une longueur, un intervalle, un préfixe — jamais ce que le test assertit. La surface complète des générateurs (contraintes, collections, composition via `As`/`Combine`, `.OrNull()`) est documentée avec `Dummies` lui-même.

Les garanties s’arrêtent à la validité du type. Un générateur ne vise aucune précondition métier — `Dummies.Any.Int32()` peut être négatif, `Dummies.Any.String()` n’est pas un e-mail bien formé — donc un value object au contrat plus strict se construit en transformant une primitive sous contrainte : `Dummies.Any.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create).Generate()`.

## Reproduire une exécution en échec

La source n’est pas seedée par défaut : les valeurs diffèrent donc d’une exécution à l’autre. C’est délibéré : un test qui ne passe que pour une valeur particulière dépend de quelque chose qu’il n’énonce pas, et faire varier la valeur révèle ce couplage.

Quand une exécution mérite d’être reproduite, enveloppez le corps du test dans `Dummies.Any.Reproducibly`. La méthode épingle une graine fraîche pour l’exécution et, si le corps lève une exception, **rapporte cette graine** avant de laisser l’échec se propager — un test rouge te dit ainsi exactement comment le rejouer :

```csharp
[Fact]
public void Some_value_sensitive_test() =>
    Dummies.Any.Reproducibly(() => {
        // ... arrange avec les fabriques et Dummies.Any, act, assert ...
    });
```

En cas d’échec, la graine est écrite sur `Console.Error` par défaut ; passe le writer de ton framework (par exemple l’`ITestOutputHelper.WriteLine` de xUnit) pour l’y router. Rejoue l’exécution en redonnant la graine rapportée :

```csharp
Dummies.Any.Reproducibly(1234, () => {
    // ... le même corps ...
});
```

Reproduire une exécution nécessite la **même séquence** de tirages : un corps dont l’ordre dépend d’un état externe non déterministe n’est pas entièrement rejouable à partir de la seule graine. Une surcharge asynchrone, `Dummies.Any.Reproducibly(Func<Task>)`, existe pour les corps de test `async`. Comme les fabriques, les primitives et les seams d’horloge et d’identifiants ci-dessous tirent tous de la même source ambiante, un seul `Reproducibly` les rejoue ensemble.

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

Les deux tirent de la même source ambiante que `Dummies.Any` : les exécuter à l’intérieur d’un `Dummies.Any.Reproducibly` rend donc leur instant et leurs identifiants reproductibles eux aussi. Pour épingler un instant ou un identifiant *précis*, utilisez `UseFixed` — voir [Tests d’erreur déterministes](DeterministicTesting.fr.md).

## Portée et tests parallèles

`Dummies.Any.Reproducibly`, `Clock.UseAny` et `InstanceIds.UseAny` ne prennent effet que pour l’exécution ou le bloc `using` qu’ils enveloppent, et la source arbitraire est restaurée à leur sortie. Cette source est stockée dans un `AsyncLocal` : elle suit le flux d’exécution du test lui-même et ne fuit jamais dans d’autres tests s’exécutant en même temps.

## Checklist de revue

Avant de recourir à une valeur arbitraire, vérifiez que :

- la valeur ne **modifie pas** le chemin fonctionnel exercé par le test — elle ne doit alimenter ni une branche, ni une validation, ni une sérialisation, ni un classement, même indirectement ;
- la valeur n’est réellement pas vérifiée par le test — sinon utilisez un littéral ;
- une fabrique d’enum significatif (`TransienceFactory`, `InteractionDirectionFactory`) est utilisée quand le test a besoin d’une valeur significative, plutôt qu’un tirage `Dummies.Any.Enum<TEnum>()` brut ;
- un test sensible aux valeurs est enveloppé dans `Dummies.Any.Reproducibly`, pour qu’une exécution en échec rapporte la graine à rejouer ;
- `Clock.UseAny` / `InstanceIds.UseAny` servent pour des données d’occurrence stables mais sans importance, et `UseFixed` lorsque la valeur exacte est assertée.

---

<div align="center">
<a href="DeterministicTesting.fr.md">← Tests d’erreur déterministes</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="OperationalIntegration.fr.md">Générer et publier le catalogue →</a>
</div>

---
