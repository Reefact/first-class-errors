# ADR-0003 | Unifier la transformation de valeur d'Outcome sous Then

🌍 🇬🇧 [English](0003-unify-outcome-value-mapping-under-then.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-15
**Décideurs :** Reefact

## Contexte

* `Outcome` et `Outcome<T>` sont le type de résultat succès-ou-échec de la bibliothèque ; ils se composent au travers d'une petite surface fluide.
* Deux opérations de composition existaient sous des noms distincts : `Then` enchaînait avec une fonction pouvant elle-même échouer (elle retourne un `Outcome`), et `To` transformait une valeur en cas de succès avec une fonction ne pouvant pas échouer (elle retourne une valeur simple). Ce sont les opérations *bind* et *map* de la programmation fonctionnelle.
* L'objectif affiché de la bibliothèque est une API nommée pour l'**intention**, non pour la mécanique de la programmation fonctionnelle ; la FAQ défend ce choix précis lorsqu'elle oppose `Outcome` à un `Result<T, Error>` générique.
* Le fait qu'une étape de composition puisse échouer est déjà exprimé par le type de retour de la fonction que l'appelant fournit : une fonction retournant un `Outcome` peut échouer ; une fonction retournant une valeur ne le peut pas.
* C# interdit la surcharge sur le seul type de retour, et sa résolution de surcharge sélectionne le type de paramètre le plus spécifique lorsque plusieurs candidats s'appliquent. La distinction map/bind est donc imposée par le système de types à chaque site d'appel, indépendamment du nom de la méthode.
* La bibliothèque est en pré-version : non publiée sur NuGet, sans consommateurs externes, de sorte qu'un renommage cassant n'entraîne aujourd'hui aucun coût de migration en aval.

## Décision

L'opération de transformation de valeur sur `Outcome` est exposée sous forme de surcharge de `Then` plutôt que comme une méthode `To` distincte.

## Justification

* Conserver deux noms fait remonter la mécanique map/bind de la programmation fonctionnelle dans l'API, ce qui contredit l'objectif de nommer les opérations pour l'intention. Un unique `Then` se lit comme « ensuite, fais ceci » — l'intention réelle de l'appelant — quel que soit le type de fonction qui suit.
* L'information que les noms `To`/`Then` encodaient — *cette étape peut-elle échouer ?* — est redondante avec le type de retour de la fonction fournie, que le lecteur voit déjà au site d'appel. Abandonner le second verbe ne perd aucune information que le code ne porte pas déjà.
* Fusionner les deux sous un seul nom est sûr : C# sélectionne la surcharge de transformation de valeur pour une fonction retournant une valeur et la surcharge de liaison pour une fonction retournant un `Outcome`, de sorte qu'un résultat s'aplatit toujours et ne s'imbrique jamais en `Outcome<Outcome<T>>`. Cette résolution est déterministe et a été vérifiée stable à travers les versions du langage C# avec lesquelles les consommateurs compilent (voir la pull request et ses tests de verrouillage de résolution).
* Le statut de pré-version signifie que le coût du changement — un renommage cassant — est payé maintenant, alors qu'il n'y a aucun consommateur, plutôt que plus tard.

## Alternatives envisagées

### Conserver `To` et `Then` comme opérations distinctes (statu quo)

Envisagée parce qu'elle reflète la nomenclature établie `map`/`bind` familière aux praticiens de la programmation fonctionnelle et garde chaque opération sans ambiguïté par elle-même.

Rejetée parce qu'elle expose précisément la mécanique que la bibliothèque masque délibérément, et la distinction échec/sans-échec qu'elle encode est déjà portée par le type de retour de la fonction fournie — le second nom ajoute du vocabulaire sans ajouter d'information.

### Conserver `To` comme alias déprécié de `Then`

Envisagée comme une voie de migration plus douce et non cassante.

Rejetée parce qu'il n'y a aucun consommateur à migrer, et qu'un alias préserverait la dualité map/bind que la décision vise à supprimer.

## Conséquences

### Positives

* La surface de composition est un unique verbe nommé pour l'intention, cohérent avec le reste de l'API.
* Les sites d'appel se lisent comme une intention unique, qu'une étape puisse échouer ou non.
* La distinction map/bind reste imposée là où c'est important — par le système de types à chaque appel — sans risque d'outcomes imbriqués.

### Négatives

* `To` est supprimée : un changement cassant de l'API publique.
* L'opération map ne peut plus être nommée distinctement à un site d'appel ; un lecteur déduit « ne peut pas échouer » du type de retour de la fonction fournie plutôt que du verbe.
* Obtenir délibérément un `Outcome<Outcome<T>>` imbriqué via la surcharge de transformation n'est plus possible (jamais un usage prévu).

### Risques

* Un changement futur dans la résolution de surcharge de C# pourrait, en principe, modifier quelle surcharge est sélectionnée. Atténué par des tests de verrouillage qui vérifient la résolution et échouent — à la compilation et à l'exécution — si jamais elle régresse.

## Actions de suivi

* Mettre à jour la documentation destinée aux utilisateurs (guide Outcome, Concepts fondamentaux, Schémas d'utilisation) pour utiliser `Then` pour la transformation de valeur.

## Références

* Pull request : [#127](https://github.com/Reefact/first-class-errors/pull/127)
* FAQ — « Pourquoi ne pas utiliser un `Result<T, Error>` générique ? » (la justification intention-plutôt-que-mécanique).
