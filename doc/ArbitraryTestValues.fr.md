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

## Reproduire une exécution avec une graine

La source n’est pas seedée par défaut : les valeurs diffèrent donc d’une exécution à l’autre. C’est délibéré : un test qui ne passe que pour une valeur particulière dépend de quelque chose qu’il n’énonce pas, et faire varier la valeur révèle ce couplage.

Le coût, c’est la reproductibilité. La graine par défaut **n’est pas exposée aujourd’hui** : une exécution ayant échoué sur une valeur non seedée précise ne peut donc pas être rejouée à partir de sa seule sortie. Gardez la valeur arbitraire hors de ce qui décide du succès ou de l’échec — ou, lorsqu’une exécution précise doit être reproductible, épinglez une graine sur la portée la plus étroite utile ; une graine commune à toute la suite convient lorsque la stabilité complète est préférable à la variation entre les exécutions. Chaque appel à `Any` dans la portée devient alors déterministe :

```csharp
using (Any.UseSeed(1234)) {
    ErrorCode first  = Any.ErrorCode();
    ErrorCode second = Any.ErrorCode(); // les deux mêmes valeurs à chaque exécution
}
```

Les graines s’imbriquent : un `Any.UseSeed(...)` interne utilise sa propre séquence et restaure celle de l’extérieur à sa sortie. Hors de toute portée, la source n’est pas seedée et chaque exécution diffère.

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

Les deux acceptent une graine optionnelle (`Clock.UseAny(1234)`, `InstanceIds.UseAny(1234)`) pour rendre les valeurs choisies reproductibles. Pour épingler un instant ou un identifiant *précis*, utilisez `UseFixed` — voir [Tests d’erreur déterministes](DeterministicTesting.fr.md).

## Portée et tests parallèles

`Any.UseSeed`, `Clock.UseAny` et `InstanceIds.UseAny` ne prennent effet qu’à l’intérieur de leur bloc `using` et sont restaurés à la sortie de la portée. La source seedée est stockée dans un `AsyncLocal` : elle suit le flux d’exécution du test lui-même et ne fuit jamais dans d’autres tests s’exécutant en même temps.

## Checklist de revue

Avant de recourir à une valeur arbitraire, vérifiez que :

- la valeur ne **modifie pas** le chemin fonctionnel exercé par le test — elle ne doit alimenter ni une branche, ni une validation, ni une sérialisation, ni un classement, même indirectement ;
- la valeur n’est réellement pas vérifiée par le test — sinon utilisez un littéral ;
- un helper d’enum nommé est utilisé quand le test a besoin d’une valeur significative, plutôt que `Any.Enum<TEnum>()` ;
- une graine est épinglée dès qu’une exécution en échec serait sinon irreproductible ;
- `Clock.UseAny` / `InstanceIds.UseAny` servent pour des données d’occurrence stables mais sans importance, et `UseFixed` lorsque la valeur exacte est assertée.

---

<div align="center">
<a href="DeterministicTesting.fr.md">← Tests d’erreur déterministes</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="OperationalIntegration.fr.md">Générer et publier le catalogue →</a>
</div>

---
