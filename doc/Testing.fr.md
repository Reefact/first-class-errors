# Guide des tests

🌍 **Langues :**  
🇫🇷 Français (ce fichier) | 🇬🇧 [English](./Testing.en.md)

Les erreurs et les outcomes sont des valeurs : vos tests devraient donc se lire
comme des tests sur des valeurs — pas comme de la tuyauterie. Le package
compagnon **`FirstClassErrors.Testing`** apporte trois petites choses qui rendent
cela facile :

* des **assertions fluentes** sur `Outcome`, `Outcome<T>` et `Error` ;
* une **horloge figeable**, pour que `OccurredAt` soit déterministe ;
* des **identifiants d'instance figeables**, pour que `InstanceId` soit déterministe.

Deux promesses tenues par le package :

* **Il n'impose aucun framework de test ni d'assertion.** Les assertions lèvent
  une simple exception que xUnit, NUnit, MSTest — n'importe lequel — rapporte
  comme un échec.
* **Il n'ajoute rien à vos dépendances de production.** Tout vit dans un package
  séparé réservé aux tests, et les overrides n'affectent jamais que le code qui
  s'exécute à l'intérieur de leur portée `using`.

```xml
<!-- dans votre projet de test -->
<PackageReference Include="FirstClassErrors.Testing" Version="..." />
```

```csharp
using FirstClassErrors;
using FirstClassErrors.Testing;
```

> Les exemples ci-dessous utilisent xUnit pour l'échafaudage (`[Fact]`,
> `Assert`), mais les assertions de FirstClassErrors fonctionnent de la même
> manière dans n'importe quel framework.

---

## ✅ Asserter sur les outcomes

C'est la partie que vous utiliserez tous les jours. Tester du code qui renvoie un
`Outcome` revient d'ordinaire à le déballer à la main et à sortir l'opérateur
`!` qui écrase le nullable :

```csharp
// 😐 Avant
Outcome<Receipt> outcome = checkout.Pay(order);

Assert.True(outcome.IsFailure);
Assert.Equal("PAYMENT.DECLINED", outcome.Error!.Code.ToString());
```

Avec le package de test, l'intention passe en premier et le boilerplate disparaît :

```csharp
// 🙂 Après
checkout.Pay(order)
        .ShouldFail()
        .WithCode("PAYMENT.DECLINED");
```

### Les succès renvoient leur valeur

`ShouldSucceed()` vérifie que l'outcome a réussi et vous rend la valeur portée,
prête à être asssertée :

```csharp
[Fact]
public void Payer_une_commande_valide_produit_un_recu()
{
    Outcome<Receipt> outcome = checkout.Pay(order);

    Receipt receipt = outcome.ShouldSucceed();

    Assert.Equal(order.Total, receipt.AmountCharged);
}
```

Pour l'`Outcome` non générique, `ShouldSucceed()` vérifie simplement le succès :

```csharp
inventory.Reserve(sku).ShouldSucceed();
```

### Les échecs renvoient un handle fluent

`ShouldFail()` vérifie que l'outcome a échoué et renvoie une `ErrorAssertion` sur
laquelle enchaîner des attentes. Chaque étape vérifie une facette de l'erreur :

```csharp
[Fact]
public void Un_paiement_refuse_remonte_une_erreur_diagnosable()
{
    Outcome<Receipt> outcome = checkout.Pay(declinedCard);

    outcome.ShouldFail()
           .WithCode("PAYMENT.DECLINED")
           .WithShortMessage("Votre paiement a été refusé.")
           .WithDiagnosticMessage("L'émetteur a refusé l'autorisation (code 51).")
           .WithContextEntry("CardNetwork", "VISA");
}
```

Vérifications disponibles sur `ErrorAssertion` :

| Méthode | Vérifie |
| --- | --- |
| `WithCode("...")` / `WithCode(errorCode)` | le `Code` de l'erreur |
| `WithShortMessage("...")` | le `ShortMessage` public |
| `WithDiagnosticMessage("...")` | le `DiagnosticMessage` interne |
| `WithContextEntry("clé")` | la présence d'une entrée de contexte |
| `WithContextEntry("clé", valeur)` | la présence d'une entrée **et** son égalité à `valeur` |

Besoin de quelque chose que la surface fluente ne couvre pas ? `Subject` vous
rend l'`Error` brute :

```csharp
Error error = outcome.ShouldFail().WithCode("ORDER.NOT_FOUND").Subject;

Assert.Empty(error.InnerErrors);
```

### Les messages d'échec se lisent bien

Quand une attente n'est pas satisfaite, les assertions lèvent une
`OutcomeAssertionException` dont le message dit ce qui s'est réellement passé —
et non l'exception propre au domaine :

```text
Expected the outcome to succeed, but it failed with [PAYMENT.DECLINED]: L'émetteur a refusé l'autorisation (code 51).
```

```text
Expected the error to have code "ORDER.NOT_FOUND", but it was "ORDER.LOCKED".
```

---

## 🕒 Figer l'horloge

Chaque `Error` enregistre l'instant où elle est survenue dans `OccurredAt`. Dans
un test, l'horloge réelle vous force à raisonner par fenêtre de temps :

```csharp
// 😐 Avant
DateTimeOffset before = DateTimeOffset.UtcNow;
DomainError    error  = MakeError();
DateTimeOffset after  = DateTimeOffset.UtcNow;

Assert.True(error.OccurredAt >= before && error.OccurredAt <= after);
```

`Clock.UseFixed(...)` épingle le temps à un instant exact pendant la durée d'une
portée `using`, ce qui permet d'asserter une égalité :

```csharp
// 🙂 Après
[Fact]
public void Une_erreur_enregistre_quand_elle_survient()
{
    var instant = new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);

    using (Clock.UseFixed(instant))
    {
        DomainError error = DomainError
            .Create(ErrorCode.Create("ORDER.NOT_FOUND"), "La commande 42 est introuvable.")
            .WithPublicMessage("Cette commande n'existe pas.");

        Assert.Equal(instant, error.OccurredAt);
    }
}
```

Besoin d'une horloge que vous contrôlez sur plusieurs lectures (par exemple une
horloge qui avance) ? Implémentez `IClock` et passez-la à `Clock.Use(...)` :

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
            _now = _now.AddSeconds(1); // chaque lecture avance d'une seconde
            return current;
        }
    }
}

using (Clock.Use(new StepClock(start)))
{
    // les erreurs créées ici obtiennent start, start + 1s, start + 2s, ...
}
```

**Comportement de la portée**

* En dehors d'une portée — c'est-à-dire en production — l'horloge est toujours
  l'horloge système réelle. Ce type n'affecte que le code exécuté dans un bloc
  `using`.
* Disposer la portée restaure l'horloge précédente. Utilisez toujours `using`.
* L'override suit le contexte d'exécution courant : il ne fuit donc jamais dans
  des tests exécutés en parallèle.

---

## 🔢 Figer les identifiants d'instance

Chaque occurrence d'erreur reçoit un `InstanceId` unique (un `Guid` aléatoire).
C'est exactement ce que l'on veut en production, et exactement ce qui casse un
snapshot ou une assertion d'égalité sur une erreur entière. `InstanceIds`
permet de l'épingler.

Épingler un identifiant unique :

```csharp
[Fact]
public void Une_erreur_not_found_se_snapshote_proprement()
{
    var id = new Guid("11111111-1111-1111-1111-111111111111");

    using (InstanceIds.UseFixed(id))
    {
        DomainError error = orders.Find(missingId).ShouldFail().Subject as DomainError;

        Assert.Equal(id, error!.InstanceId);
    }
}
```

Ou fournissez votre propre source — un compteur, par exemple, quand un test crée
plusieurs erreurs et que vous voulez des identifiants stables et distincts :

```csharp
static Func<Guid> Sequential()
{
    int n = 0;
    return () => new Guid(++n, 0, 0, new byte[8]); // 00000001-..., 00000002-...
}

using (InstanceIds.Use(Sequential()))
{
    // première erreur  -> 00000001-0000-0000-0000-000000000000
    // deuxième erreur  -> 00000002-0000-0000-0000-000000000000
}
```

`InstanceIds` suit les mêmes règles de portée que `Clock` : jetable, local au
contexte, et inerte en dehors d'un bloc `using`.

---

## 🧪 Tout mettre ensemble

Figer l'horloge et l'identifiant transforme une erreur entière en une valeur
totalement déterministe — idéale pour une assertion unique et lisible, ou pour
un snapshot :

```csharp
[Fact]
public void Chercher_une_commande_absente_echoue_de_facon_deterministe()
{
    var instant = new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);
    var id      = new Guid("11111111-1111-1111-1111-111111111111");

    using (Clock.UseFixed(instant))
    using (InstanceIds.UseFixed(id))
    {
        Outcome<Order> outcome = orders.Find(missingId);

        ErrorAssertion failure = outcome.ShouldFail()
                                        .WithCode("ORDER.NOT_FOUND")
                                        .WithContextEntry("OrderId", missingId);

        Assert.Equal(instant, failure.Subject.OccurredAt);
        Assert.Equal(id,      failure.Subject.InstanceId);
    }
}
```

---

## 📎 Bon à savoir

* Tout est **borné et jetable** — sortez toujours le `using`.
* Les overrides sont **locaux au contexte** : ils ne s'appliquent qu'au flux
  asynchrone courant, si bien que des tests parallèles ne se marchent pas dessus.
* Rien ici ne change le comportement de production, et le package n'introduit
  **aucun framework de test ni d'assertion** dans votre projet.

---

<div align="center">
<a href="BestPractices.fr.md">← Bonnes pratiques</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="OperationalIntegration.fr.md">Intégration CI/CD et exploitation →</a>
</div>

---
