# Tester les outcomes et les erreurs

🌍 **Langues :**  
🇫🇷 Français (ce fichier) | 🇬🇧 [English](./Testing.en.md)

Les erreurs et les outcomes sont des valeurs. Leurs tests doivent donc décrire directement le succès, l’échec et la sémantique de l’erreur — pas la tuyauterie nécessaire pour les inspecter.

Le package compagnon **`FirstClassErrors.Testing`** fournit des assertions fluentes indépendantes du framework sur `Outcome`, `Outcome<T>` et `Error`.

## Installer le package de test

```xml
<PackageReference Include="FirstClassErrors.Testing" Version="..." />
```

```csharp
using FirstClassErrors;
using FirstClassErrors.Testing;
using NFluent;
```

Les exemples utilisent xUnit pour la structure des tests et NFluent pour les assertions sur les valeurs simples — la convention suivie par la suite de tests de ce dépôt — mais les assertions FirstClassErrors ne dépendent ni de xUnit, ni de NUnit, ni de MSTest, ni de NFluent, ni d’une autre bibliothèque d’assertions.

Le package doit rester réservé aux projets de test et n’ajoute aucune dépendance en production.

## Asserter un `Outcome<T>` réussi

Sans le package de test, un test vérifie généralement l’état puis récupère séparément le résultat :

```csharp
Outcome<Receipt> outcome = checkout.Pay(order);

Check.That(outcome.IsSuccess).IsTrue();
Receipt receipt = outcome.GetResultOrThrow();
Check.That(receipt.AmountCharged).IsEqualTo(order.Total);
```

`ShouldSucceed()` effectue l’assertion d’état et retourne la valeur portée :

```csharp
[Fact]
public void Payer_une_commande_valide_produit_un_recu() {
    Receipt receipt = checkout.Pay(order).ShouldSucceed();

    Check.That(receipt.AmountCharged).IsEqualTo(order.Total);
}
```

Pour l’`Outcome` non générique, la méthode vérifie simplement le succès :

```csharp
inventory.Reserve(sku).ShouldSucceed();
```

## Asserter un échec

`ShouldFail()` vérifie l’échec et retourne une `ErrorAssertion` :

```csharp
[Fact]
public void Un_paiement_refuse_remonte_une_erreur_diagnosable() {
    checkout.Pay(declinedCard)
            .ShouldFail()
            .WithCode("PAYMENT_DECLINED")
            .WithShortMessage("Votre paiement a été refusé.")
            .WithDiagnosticMessage("L’émetteur a refusé l’autorisation (code 51).")
            .WithContextEntry("CardNetwork", "VISA");
}
```

Le contrat attendu reste visible au même endroit : code stable, message public, diagnostic interne et contexte de l’occurrence.

## Vérifications disponibles

| Méthode | Vérifie |
| --- | --- |
| `WithCode("...")` | le code d’erreur sous forme de texte |
| `WithCode(errorCode)` | l’`ErrorCode` typé |
| `WithShortMessage("...")` | le message public court |
| `WithDiagnosticMessage("...")` | le message de diagnostic interne |
| `WithContextEntry("clé")` | la présence d’une entrée de contexte |
| `WithContextEntry("clé", valeur)` | la présence de l’entrée et l’égalité à la valeur attendue |

N’utilisez que les assertions qui expriment le comportement couvert par le test. Évitez de vérifier mécaniquement tous les champs lorsque seuls le code et une valeur de contexte importent.

## Accéder à l’erreur sous-jacente

`Subject` retourne l’`Error` assertée lorsque la surface fluente ne couvre pas une propriété :

```csharp
Error error = outcome.ShouldFail()
                     .WithCode("ORDER_NOT_FOUND")
                     .Subject;

Check.That(error.InnerErrors).IsEmpty();
Check.That(((InfrastructureError)error).Transience).IsEqualTo(Transience.NonTransient);
```

Utilisez `Subject` pour des assertions ciblées, pas pour reconstruire immédiatement toute la tuyauterie manuelle que l’API fluente vient de supprimer.

## Messages d’échec

Lorsqu’une attente échoue, le package lève `OutcomeAssertionException` avec un message décrivant l’écart :

```text
Expected the outcome to succeed, but it failed with [PAYMENT_DECLINED]: L’émetteur a refusé l’autorisation (code 51).
```

```text
Expected the error to have code "ORDER_NOT_FOUND", but it was "ORDER_LOCKED".
```

`OutcomeAssertionException` est un échec du framework de test, pas l’exception que produirait `error.ToException()`. Le test indique donc ce qu’il attendait et ce que l’outcome contenait réellement.

## Que doit vérifier un test ?

Privilégiez les assertions sur le comportement stable de l’erreur :

- le code d’erreur ;
- la catégorie d’erreur lorsqu’elle est pertinente ;
- le texte public uniquement lorsqu’il fait partie du contrat voulu ;
- le diagnostic lorsque sa formulation exacte est volontairement spécifiée ;
- les clés et valeurs de contexte exploitées par des consommateurs ou par l’exploitation ;
- les erreurs internes lorsque la composition est elle-même le comportement testé.

Évitez de coupler chaque test à du texte secondaire, à un horodatage ou à un identifiant lorsque ces valeurs ne sont pas le sujet du test.

## Exemple complet

```csharp
[Fact]
public void Chercher_une_commande_absente_retourne_l_erreur_attendue() {
    Outcome<Order> outcome = orders.Find(missingOrderId);

    Error error = outcome.ShouldFail()
                         .WithCode("ORDER_NOT_FOUND")
                         .WithShortMessage("La commande n’existe pas.")
                         .WithContextEntry("OrderId", missingOrderId)
                         .Subject;

    Check.That(error).IsInstanceOf<DomainError>();
    Check.That(error.InnerErrors).IsEmpty();
}
```

Le test se lit comme une description du contrat d’échec plutôt que comme une suite de vérifications nullable et de casts.

## Horodatages et identifiants déterministes

Chaque occurrence d’erreur contient un horodatage `OccurredAt` et un `InstanceId` unique. Lorsqu’un test ou un snapshot (un test qui compare un objet sérialisé à un fichier de référence approuvé) doit vérifier ces valeurs, utilisez les overrides bornés fournis par le package de test.

Voir [Tests d’erreur déterministes](DeterministicTesting.fr.md) pour `Clock.UseFixed(...)`, `InstanceIds.UseFixed(...)`, les sources personnalisées, le comportement en tests parallèles et les snapshots d’erreurs complètes.

## Checklist de revue

Avant d’approuver un test d’erreur, vérifiez que :

- il teste un comportement et non la tuyauterie d’implémentation ;
- les valeurs de succès sont obtenues via `ShouldSucceed()` ;
- les échecs sont vérifiés via `ShouldFail()` ;
- le code d’erreur est asserté lorsqu’il identifie le scénario ;
- le texte exact n’est vérifié que lorsqu’il est volontairement contractuel ;
- `Subject` est réservé aux propriétés hors de la surface fluente ;
- le temps et les identifiants ne sont figés que lorsque le test en a besoin.

---

<div align="center">
<a href="BestPractices.fr.md">← Bonnes pratiques</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="DeterministicTesting.fr.md">Tests d’erreur déterministes →</a>
</div>

---