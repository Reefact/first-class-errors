# Valeurs de test arbitraires

🌍 **Langues :**  
🇫🇷 Français (ce fichier) | 🇬🇧 [English](./ArbitraryTestValues.en.md)

Une grande partie de l’`Arrange` d’un test est d’ordinaire faite de valeurs qu’il ne vérifie jamais — un code d’erreur, un message de diagnostic, un instant de survenue. Écrites en dur, elles se lisent comme si elles comptaient, et une constante réutilisée dans toute une suite peut faire passer un test pour une mauvaise raison. `Any` fournit à la place une valeur valide mais arbitraire : la seule entrée qui compte ressort, et les autres s’annoncent comme accessoires.

`Any` vit dans **`FirstClassErrors.Testing`** ; il n’ajoute aucune dépendance et, comme les overrides d’horloge et d’identifiants, il est borné, local au contexte et sûr en tests parallèles. Pour figer les valeurs qu’un test *assertit*, voir [Tests d’erreur déterministes](DeterministicTesting.fr.md).

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
// 🙂 Après — le code est le sujet ; les messages sont fournis par Any.
DomainError error = DomainError
    .Create(ErrorCode.Create("ORDER_NOT_FOUND"), Any.DiagnosticMessage())
    .WithPublicMessage(Any.ShortMessage());

Outcome<Order>.Failure(error).ShouldFail().WithCode("ORDER_NOT_FOUND");
```

Une valeur n’est accessoire que si elle ne peut pas orienter le code testé. Si elle alimente une branche, une validation, une sérialisation ou un classement, elle façonne le comportement même si le test ne l’assertit jamais — et elle ne peut alors pas être laissée arbitraire sans risque. Réservez `Any` aux entrées que le test transporte mais sur lesquelles il n’agit pas.

## Ce que `Any` propose

Chaque helper renvoie une valeur **valide pour son type** — des chaînes et messages non vides, un instant UTC réel, un code d’erreur jamais vide :

| Helper | Renvoie |
| --- | --- |
| `Any.ErrorCode()` | un code valide non vide, de la forme `ANY_CODE_` + 6 caractères alphanumériques majuscules |
| `Any.DiagnosticMessage()` / `Any.ShortMessage()` / `Any.DetailedMessage()` | un message non vide, de longueur bornée |
| `Any.String()` | une chaîne non vide de longueur bornée (`any-` + 8 caractères alphanumériques minuscules) ; sans espace |
| `Any.Guid()` | un `Guid` arbitraire |
| `Any.Instant()` | un instant UTC arbitraire (offset zéro), entre le 1er janvier 2000 et environ 2068 |
| `Any.Int()` | un `int` arbitraire — éventuellement négatif ou nul |
| `Any.Bool()` | `true` ou `false` |
| `Any.Enum<TEnum>()` | un membre quelconque de l’enum — une sentinelle comme `Unknown` comprise |
| `Any.Transience()` / `Any.InteractionDirection()` | une valeur *significative* — jamais la sentinelle `Unknown` |
| `Any.ErrorOrigin()` | un `ErrorOrigin` quelconque ; les trois valeurs sont significatives, il n’y a donc pas de sentinelle à exclure |

Les garanties s’arrêtent à la validité du type. Un helper ne vise aucune précondition métier — `Any.Int()` peut être négatif, `Any.String()` n’est pas un e-mail bien formé — donc un value object au contrat plus strict a besoin de sa propre factory arbitraire, pas d’une primitive brute.

Utilisez `Any.Enum<TEnum>()` quand n’importe quel membre convient — sentinelle comprise — et les helpers d’enum nommés quand le test a besoin d’une valeur qui déclenche réellement le comportement concerné.

## Reproduire une exécution en échec

La source n’est pas seedée par défaut : les valeurs diffèrent donc d’une exécution à l’autre. C’est délibéré : un test qui ne passe que pour une valeur particulière dépend de quelque chose qu’il n’énonce pas, et faire varier la valeur révèle ce couplage.

Quand une exécution mérite d’être reproduite, enveloppez le corps du test dans `Any.Reproducibly`. La méthode épingle une graine fraîche pour l’exécution et, si le corps lève une exception, **rapporte cette graine** avant de laisser l’échec se propager — un test rouge te dit ainsi exactement comment le rejouer :

```csharp
[Fact]
public void Some_value_sensitive_test() =>
    Any.Reproducibly(() => {
        // ... arrange avec Any, act, assert ...
    });
```

En cas d’échec, la graine est écrite sur `Console.Error` par défaut ; passe le writer de ton framework (par exemple l’`ITestOutputHelper.WriteLine` de xUnit) pour l’y router. Rejoue l’exécution en redonnant la graine rapportée :

```csharp
Any.Reproducibly(1234, () => {
    // ... le même corps ...
});
```

Reproduire une exécution nécessite la **même séquence** d’appels à `Any` : un corps dont l’ordre des tirages dépend d’un état externe non déterministe n’est pas entièrement rejouable à partir de la seule graine. Une surcharge asynchrone, `Any.Reproducibly(Func<Task>)`, existe pour les corps de test `async`.

## `OccurredAt` et `InstanceId` arbitraires

Les données d’occurrence sont arbitraires au même sens : un test a souvent besoin qu’elles soient stables sans en vérifier l’instant ou l’identifiant exact. Les seams de l’horloge et des identifiants proposent donc un `UseAny` en pendant de leur `UseFixed`. `Clock.UseAny()` fige un unique instant arbitraire pour la portée, tandis que `InstanceIds.UseAny()` attribue à chaque erreur son propre identifiant arbitraire distinct :

```csharp
DomainError NewError() =>
    DomainError.Create(Any.ErrorCode(), Any.DiagnosticMessage()).WithPublicMessage(Any.ShortMessage());

using (Clock.UseAny())
using (InstanceIds.UseAny()) {
    DomainError first  = NewError();
    DomainError second = NewError();

    Check.That(second.OccurredAt).IsEqualTo(first.OccurredAt);    // un instant arbitraire, partagé
    Check.That(second.InstanceId).IsNotEqualTo(first.InstanceId); // des identifiants arbitraires distincts
}
```

Les deux tirent de la même source qu’`Any` : les exécuter à l’intérieur d’un `Any.Reproducibly` rend donc leur instant et leurs identifiants reproductibles eux aussi. Pour épingler un instant ou un identifiant *précis*, utilisez `UseFixed` — voir [Tests d’erreur déterministes](DeterministicTesting.fr.md).

## Portée et tests parallèles

`Any.Reproducibly`, `Clock.UseAny` et `InstanceIds.UseAny` ne prennent effet que pour l’exécution ou le bloc `using` qu’ils enveloppent, et la source arbitraire est restaurée à leur sortie. Cette source est stockée dans un `AsyncLocal` : elle suit le flux d’exécution du test lui-même et ne fuit jamais dans d’autres tests s’exécutant en même temps.

## Checklist de revue

Avant de recourir à une valeur arbitraire, vérifiez que :

- la valeur ne **modifie pas** le chemin fonctionnel exercé par le test — elle ne doit alimenter ni une branche, ni une validation, ni une sérialisation, ni un classement, même indirectement ;
- la valeur n’est réellement pas vérifiée par le test — sinon utilisez un littéral ;
- un helper d’enum nommé est utilisé quand le test a besoin d’une valeur significative, plutôt que `Any.Enum<TEnum>()` ;
- un test sensible aux valeurs est enveloppé dans `Any.Reproducibly`, pour qu’une exécution en échec rapporte la graine à rejouer ;
- `Clock.UseAny` / `InstanceIds.UseAny` servent pour des données d’occurrence stables mais sans importance, et `UseFixed` lorsque la valeur exacte est assertée.

---

<div align="center">
<a href="DeterministicTesting.fr.md">← Tests d’erreur déterministes</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="OperationalIntegration.fr.md">Générer et publier le catalogue →</a>
</div>

---
