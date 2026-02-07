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

Les factories d’exception doivent éviter d’introduire la construction d’erreur directement dans la logique métier.

Préférez :

```csharp
throw InvalidAmountOperationException.CurrencyMismatch(a1, a2);
````

Plutôt que :

```csharp
throw new InvalidAmountOperationException(...);
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

## 🔁 7. Utiliser TryOutcome quand l’échec est attendu

Utilisez des exceptions pour :

* les violations d’invariants
* les états inattendus

Utilisez `TryOutcome<T>` lorsque :

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

Les méthodes de documentation doivent vivre dans la même classe d’exception que la factory.

Cela garde :

* l’intention
* la création de l’erreur
* la documentation

au même endroit conceptuel.

**Pourquoi :**
Garder la documentation à côté de la factory garantit qu’elle évolue avec le code. Cela évite les dérives et préserve l’idée centrale de documentation vivante : la connaissance reste là où le comportement est défini.

## 🧩 11. Sceller les exceptions applicatives

Les exceptions spécifiques à l’application devraient être déclarées `sealed`.

```csharp
public sealed class InvalidAmountOperationException : DomainException
```

**Pourquoi :**
Chaque type d’exception représente une catégorie d’erreur bien définie. Autoriser l’héritage tend à brouiller la sémantique, créer des hiérarchies floues et rendre les diagnostics plus difficiles à raisonner. Sceller le type garantit que le sens de l’exception reste stable et explicite.

## 🏭 12. Utiliser des constructeurs privés et des méthodes factory

Les constructeurs d’exception devraient être `private` et seuls ceux strictement nécessaires devraient être implémentés.

```csharp
private InvalidAmountOperationException(string errorCode, string errorMessage)
    : base(errorCode, errorMessage) { }
```

Les instances doivent toujours être créées via des méthodes factory :

```csharp
throw InvalidAmountOperationException.CurrencyMismatch(a1, a2);
```

**Pourquoi :**
En restreignant les constructeurs, vous vous assurez que toutes les exceptions de ce type sont créées de manière contrôlée, documentée et sémantiquement cohérente.

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