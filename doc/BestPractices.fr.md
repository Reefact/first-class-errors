# Bonnes pratiques

DiagnosableExceptions est le plus efficace lorsqu’il est utilisé de manière cohérente et intentionnelle.  
Ces pratiques aident à garder des erreurs significatives, lisibles et réellement utiles.

## 🧠 1. Une situation d’erreur par factory

Chaque méthode factory doit représenter **une situation d’erreur précise**.

Évitez :

* les factories qui couvrent plusieurs causes différentes  
* les factories génériques de type “InvalidOperation”  

Une factory doit répondre à :

> « Qu’est-ce qui s’est exactement mal passé ? »

**Pourquoi :**  
Des frontières claires entre les situations d’erreur rendent les diagnostics précis et la documentation fiable.

## 🏷️ 2. Garder les codes d’erreur stables

Les codes d’erreur font partie du contrat.

* Ne changez pas les codes à la légère  
* Ne réutilisez pas un code pour une autre situation  
* Traitez-les comme des identifiants durables  

**Pourquoi :**  
Les codes d’erreur sont utilisés dans les logs, la documentation et les processus de support. Leur stabilité préserve la traçabilité dans le temps.

## ✂️ 3. Garder le happy path propre

Les factories d’erreur doivent éviter d’introduire la construction d’erreur directement dans la logique métier.

Préférez :

```csharp
throw InvalidAmountOperationError.CurrencyMismatch(a1, a2).ToException();
````

Plutôt que :

```csharp
throw new DomainException(/* Error assemblée manuellement */);
```

**Pourquoi :**
Cela garde la logique métier lisible et sépare l’intention métier des détails de construction de l’erreur.

## 📘 4. Écrire la documentation pour des humains

La documentation des erreurs n’est pas destinée au compilateur — elle est destinée :

* aux développeurs
* au support
* aux opérateurs

Évitez le bruit technique. Concentrez-vous sur :

* le sens
* la règle
* les causes plausibles

## 🔎 5. Les diagnostics sont des hypothèses, pas des accusations

Les diagnostics doivent décrire des états possibles, pas accuser des acteurs.

Préférez :

> « Des montants ont été utilisés sans conversion. »

Évitez :

> « Le développeur a oublié de convertir. »

**Pourquoi :**
Les diagnostics guident l’investigation. Un langage accusateur nuit à la collaboration et n’aide pas au dépannage.

## 🧭 6. Les pistes d’analyse guident, elles ne prescrivent pas

N’incluez pas de processus opérationnels ou de procédures de support.

Évitez :

* « Ouvrir un ticket »
* « Contacter l’équipe X »

Concentrez-vous sur la direction de l’investigation, pas sur le workflow.

**Pourquoi :**
Les processus opérationnels dépendent du contexte organisationnel, pas de l’application elle-même. Les encoder dans la documentation des erreurs couple votre code à des procédures externes et rend la documentation fragile lorsque ces processus changent.

## 🔁 7. Utiliser Outcome quand l’échec est attendu

Utilisez des exceptions pour :

* les violations d’invariants
* les états inattendus

Utilisez `Outcome<T>` lorsque :

* vous validez des entrées
* vous traitez des lots
* les échecs partiels sont normaux

**Pourquoi :**
Cela maintient le flux d’exceptions significatif tout en permettant de transmettre des informations d’erreur riches dans des scénarios non exceptionnels.

## 🧩 8. Ne pas documenter les accidents techniques

Évitez de documenter :

* les NullReferenceExceptions
* les exceptions du framework
* les défaillances techniques bas niveau

Le DSL est destiné aux **erreurs applicatives porteuses de sens**, pas aux crashes accidentels.

**Pourquoi :**
L’objectif est de documenter le comportement et les règles du système, pas des incidents techniques imprévisibles.

## 🧪 9. Les exemples doivent éduquer, pas tester les limites

Les exemples ne sont pas des tests unitaires.

Utilisez des valeurs :

* simples
* réalistes
* claires

Évitez les cas extrêmes ou les données pathologiques.

## 🧱 10. Garder la documentation proche de la factory

Les méthodes de documentation doivent vivre dans la même classe factory d’erreur que la factory.

Cela garde :

* l’intention
* la création de l’erreur
* la documentation

au même endroit conceptuel.

**Pourquoi :**
Garder la documentation à côté de la factory garantit qu’elle évolue avec le code. Cela évite les dérives et préserve l’idée centrale de documentation vivante : la connaissance reste là où le comportement est défini.

## 🧩 11. Regrouper les erreurs dans une classe factory dédiée

Les erreurs spécifiques à l’application devraient être regroupées dans une classe `static` annotée avec `[ProvidesErrorsFor(...)]`, avec une méthode factory `internal static` par situation d’erreur.

```csharp
[ProvidesErrorsFor(nameof(Amount))]
public static class InvalidAmountOperationError {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static DomainError CurrencyMismatch(Amount left, Amount right) {
        return new DomainError(
            Code.CurrencyMismatch,
            $"Impossible d’opérer sur des montants de devises différentes : {left.Currency} et {right.Currency}.",
            "Les montants utilisent des devises différentes.");
    }

    // ... méthode de documentation et codes d’erreur ...
}
```

**Pourquoi :**
Chaque méthode factory représente une catégorie d’erreur bien définie. Les regrouper dans une classe dédiée garde au même endroit les situations d’erreur liées, leurs codes et leur documentation. Notez que les types du cœur (`DomainError`, `DomainException`, …) ne sont **pas** `sealed` — l’héritage est intentionnellement autorisé afin de pouvoir modéliser vos propres hiérarchies d’erreur — mais en pratique vous décrivez les situations d’erreur via ces classes factory plutôt qu’en dérivant des sous-classes.

## 🏭 12. Construire les erreurs via des factories, lever avec `ToException()`

Vous ne faites jamais `new` sur une `DiagnosableException` dans votre code, et il n’existe pas de constructeur à deux chaînes : le seul constructeur d’une exception prend une `Error`. Construisez l’erreur via une méthode factory puis transformez-la en exception avec `ToException()`.

```csharp
// Construit une Error via la factory, puis la lève en tant qu’exception :
throw InvalidAmountOperationError.CurrencyMismatch(a1, a2).ToException();
```

Lorsque l’échec est attendu plutôt qu’exceptionnel, retournez l’`Error` de la même factory dans un `Outcome<T>` :

```csharp
return Outcome<Amount>.Failure(InvalidAmountOperationError.NegativeAmount(value));
```

**Pourquoi :**
Faire passer chaque erreur par une factory garantit que toutes les erreurs d’une catégorie donnée sont créées de manière contrôlée, documentée et sémantiquement cohérente, qu’elles soient levées en tant qu’exceptions ou portées comme échecs d’`Outcome`.

## 🎯 Pensée finale

DiagnosableExceptions vise à **exprimer de la connaissance**, pas seulement à gérer des erreurs.

Des erreurs bien écrites améliorent :

* la lisibilité du code
* le dépannage
* la documentation
* la compréhension partagée du système

---

Section précédente: [Cas d’usage](UsagePatterns.fr.md) | Section suivante: [Intégration CI/CD et exploitation](OperationalIntegration.fr.md)

---