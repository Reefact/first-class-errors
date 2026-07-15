# Tests d’erreur déterministes

🌍 **Langues :**  
🇫🇷 Français (ce fichier) | 🇬🇧 [English](./DeterministicTesting.en.md)

Chaque occurrence d’erreur enregistre deux valeurs qui doivent varier en production :

- `OccurredAt`, l’instant UTC de création de l’erreur ;
- `InstanceId`, un identifiant unique de cette occurrence.

Ces valeurs améliorent l’observabilité, mais rendent instables les assertions sur l’objet complet et les snapshots (tests qui comparent un objet sérialisé à un fichier de référence approuvé). `FirstClassErrors.Testing` permet de remplacer temporairement les deux générateurs qui les produisent — l’horloge et la source d’identifiants — uniquement dans la portée d’un test qui a délibérément besoin de valeurs déterministes.

Pour les assertions fluentes sur les outcomes et les erreurs, commencez par [Tester les outcomes et les erreurs](Testing.fr.md).

## Setup commun aux exemples

Chaque exemple ci-dessous raconte la même petite histoire — la recherche d’une commande absente — via une factory et deux stubs, afin que chaque extrait reste court :

```csharp
// L’erreur documentée que chaque exemple rapporte.
private static DomainError MakeError() =>
    DomainError
        .Create(ErrorCode.Create("ORDER_NOT_FOUND"), "La commande 42 est introuvable.")
        .WithPublicMessage("La commande n’existe pas.");

private static readonly DateTimeOffset start = new(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);

// Un repository dont la recherche échoue pour missingOrderId, retournant un Outcome<Order> en échec.
private readonly IOrderRepository orders = /* test double */;
private readonly OrderId missingOrderId  = /* un id sans correspondance */;
```

`MakeError()` retourne le `DomainError` concret. Lorsqu’un exemple récupère plutôt l’erreur depuis un outcome en échec via `ShouldFail().Subject`, elle revient typée comme l’`Error` de base — c’est pourquoi certains extraits ci-dessous montrent `DomainError` et d’autres `Error`. C’est le même objet vu à deux niveaux de sa hiérarchie de types, pas deux types interchangeables.

## Figer `OccurredAt`

Tester avec l’horloge réelle impose généralement une fenêtre de temps imprécise :

```csharp
DateTimeOffset before = DateTimeOffset.UtcNow;
DomainError error = MakeError();
DateTimeOffset after = DateTimeOffset.UtcNow;

Check.That(error.OccurredAt >= before && error.OccurredAt <= after).IsTrue();
```

`Clock.UseFixed(...)` rend l’instant attendu explicite :

```csharp
[Fact]
public void Une_erreur_enregistre_l_instant_fixe()
{
    var instant = new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);

    using (Clock.UseFixed(instant))
    {
        DomainError error = MakeError();

        Check.That(error.OccurredAt).IsEqualTo(instant);
    }
}
```

L’override ne s’applique qu’à l’intérieur de la portée `using`. La fin de la portée restaure l’horloge précédente.

## Utiliser une horloge personnalisée

Lorsqu’un test nécessite plusieurs instants contrôlés, implémentez `IClock` et passez-la à `Clock.Use(...)` :

```csharp
sealed class StepClock : IClock
{
    private DateTimeOffset _now;

    public StepClock(DateTimeOffset start) => _now = start;

    public DateTimeOffset UtcNow
    {
        get
        {
            DateTimeOffset current = _now;
            _now = _now.AddSeconds(1);
            return current;
        }
    }
}
```

```csharp
using (Clock.Use(new StepClock(start)))
{
    DomainError first = MakeError();
    DomainError second = MakeError();

    Check.That(first.OccurredAt).IsEqualTo(start);
    Check.That(second.OccurredAt).IsEqualTo(start.AddSeconds(1));
}
```

N’utilisez une horloge personnalisée que lorsque la progression elle-même compte. Une horloge fixe est plus simple dans la plupart des tests.

## Figer `InstanceId`

Un `Guid` aléatoire est correct en production, mais instable dans un snapshot :

```csharp
[Fact]
public void Une_erreur_de_commande_absente_a_un_identifiant_stable()
{
    var id = new Guid("11111111-1111-1111-1111-111111111111");

    using (InstanceIds.UseFixed(id))
    {
        Error error = orders.Find(missingOrderId).ShouldFail().Subject;

        Check.That(error.InstanceId).IsEqualTo(id);
    }
}
```

Comme pour l’horloge, l’override prend fin lorsque la portée est disposée.

## Générer plusieurs identifiants stables

Lorsqu’un test crée plusieurs erreurs, fournissez une source déterministe :

```csharp
static Func<Guid> SequentialIds()
{
    int value = 0;
    return () => new Guid(++value, 0, 0, new byte[8]);
}
```

```csharp
using (InstanceIds.Use(SequentialIds()))
{
    DomainError first = MakeError();
    DomainError second = MakeError();

    Check.That(first.InstanceId.ToString()).IsEqualTo("00000001-0000-0000-0000-000000000000");
    Check.That(second.InstanceId.ToString()).IsEqualTo("00000002-0000-0000-0000-000000000000");
}
```

Préférez des identifiants visiblement synthétiques afin qu’ils ne puissent pas être confondus avec des valeurs de production.

## Figer simultanément `OccurredAt` et `InstanceId`

```csharp
[Fact]
public void Une_erreur_de_commande_absente_est_totalement_deterministe()
{
    var instant = new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);
    var id = new Guid("11111111-1111-1111-1111-111111111111");

    using (Clock.UseFixed(instant))
    using (InstanceIds.UseFixed(id))
    {
        Error error = orders.Find(missingOrderId)
                            .ShouldFail()
                            .WithCode("ORDER_NOT_FOUND")
                            .WithContextEntry("OrderId", missingOrderId)
                            .Subject;

        Check.That(error.OccurredAt).IsEqualTo(instant);
        Check.That(error.InstanceId).IsEqualTo(id);
    }
}
```

Figez les deux lorsqu’un même test vérifie les deux valeurs d’occurrence, ou lorsqu’il transmet l’erreur complète à un snapshot fait main comparé à une chaîne de référence fixe. Une bibliothèque de snapshot comme [Verify](https://github.com/VerifyTests/Verify) est un cas différent : elle scrubbe déjà d’elle-même les `Guid` et `DateTime` volatils en placeholders stables (`Guid_1`, `DateTimeOffset_1`), donc vous les laissez généralement tels quels et ne figez que lorsque le snapshot doit montrer la valeur exacte. Dans tous les cas, si un test ne porte que sur le code d’erreur ou le contexte, ne figez pas des champs sans rapport simplement parce que les helpers existent.

## Portée et tests parallèles

Les overrides sont :

- jetables et prévus pour des portées `using` ;
- locaux au contexte d’exécution courant ;
- restaurés à la fin de la portée ;
- inactifs en dehors de cette portée ;
- conçus pour ne pas fuiter vers des tests parallèles sans rapport.

Gardez la portée aussi étroite que possible autour du code qui crée les erreurs.

## Erreurs fréquentes

### Oublier de disposer l’override

Utilisez toujours `using`. Une gestion manuelle de la durée de vie rend les tests plus difficiles à comprendre.

### Figer les valeurs dans tous les tests

La majorité des tests doit vérifier la sémantique stable via `ShouldSucceed()` et `ShouldFail()`. Ne figez les données d’occurrence que lorsqu’elles font partie de l’assertion.

### Traiter les overrides comme de la configuration de production

Les overrides de `Clock` et `InstanceIds` sont des aides de test. La production doit conserver l’horloge UTC réelle et des identifiants uniques.

### Réutiliser le même identifiant fixe lorsque l’identité compte

Si le test vérifie plusieurs occurrences distinctes, utilisez une séquence déterministe plutôt qu’un identifiant répété.

## Checklist de revue

Avant d’approuver un test déterministe, vérifiez que :

- l’horodatage ou l’identifiant est réellement pertinent ;
- chaque override est entouré d’un `using` ;
- la portée est étroite ;
- une valeur fixe est utilisée lorsque la progression est inutile ;
- une séquence déterministe est utilisée lorsque plusieurs erreurs distinctes sont créées ;
- les valeurs synthétiques sont évidentes dans les snapshots ;
- le test ne dépend pas accidentellement du temps réel ou d’identifiants aléatoires.

---

<div align="center">
<a href="Testing.fr.md">← Tester les outcomes et les erreurs</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="OperationalIntegration.fr.md">Générer et publier le catalogue →</a>
</div>

---