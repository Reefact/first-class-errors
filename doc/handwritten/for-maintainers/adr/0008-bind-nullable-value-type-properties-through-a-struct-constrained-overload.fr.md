# ADR-0008 | Lier les propriétés de type valeur nullable via une surcharge contrainte à struct

🌍 🇬🇧 [English](0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Le Request Binder convertit les propriétés DTO nullables via des method groups de
factories de value objects. Avec le sélecteur non contraint initial, une propriété
comme `int?` inférait l'argument générique comme `Nullable<int>`, de sorte qu'une
factory sur le `int` sous-jacent échouait à l'inférence avec `CS0411`.

Une propriété de type valeur non nullable ne peut pas représenter l'absence et
constitue déjà une erreur de programmation. Les contraintes seules ne distinguent
pas les signatures, mais un sélecteur contraint à `struct` portant
`Nullable<TArgument>` possède un type de paramètre différent et plus spécifique.
Les listes ajoutent une contrainte : chaque élément nullable peut lui-même manquer.

Le package était en pré-release, permettant de stabiliser la forme avant
l'existence de call sites consommateurs.

## Décision

Le Request Binder lie les propriétés DTO de type valeur nullable et leurs listes par des chemins de sélection dédiés `where TArgument : struct` dont les convertisseurs reçoivent la valeur sous-jacente non nullable.

## Justification

Le chemin dédié supprime l'échec d'inférence à la frontière de l'API et conserve
la même ergonomie de method group que pour les propriétés référence. Le DTO reste
nullable afin de rendre l'absence observable, tandis qu'une valeur présente est
déballée avant conversion. Les listes exigent la gestion correspondante afin qu'un
élément `null` soit enregistré à son index plutôt que transmis au convertisseur.

Les mécanismes de surcharge et de conversion sont maintenus dans la
[spécification du Request Binder](../specifications/request-binder.fr.md).

## Alternatives envisagées

### Exiger une lambda d'adaptation à chaque call site

Envisagé parce que cela n'ajoute aucune API. Rejeté car cela expose une limite
d'inférence à chaque consommateur et dégrade l'ergonomie des types valeur.

### Utiliser un unique sélecteur pour références et valeurs

Envisagé pour réduire la surface. Rejeté car C# ne peut pas faire inférer par un
même `T?` non contraint le type valeur sous-jacent tout en préservant la sémantique
référence.

### Réutiliser le convertisseur de listes de références

Envisagé car le comportement est proche. Rejeté car les éléments valeur nullables
requièrent un enregistrement d'absence et un déballage explicites.

## Conséquences

### Positives

* Les propriétés et listes de types valeur nullables utilisent des factories sur
  leur type sous-jacent.
* L'ergonomie des method groups rejoint celle des propriétés référence.
* La forme additive a été stabilisée avant le gel de la surface v1.

### Négatives

* La surface publique de sélection et de conversion s'agrandit.
* La résolution de surcharge est subtile et doit être couverte par régression.

### Risques

* De futures formes de sélecteur pourraient créer une ambiguïté. Atténuation : les
  tests figent la résolution pour références, chaînes, valeurs scalaires et listes.

## Actions de suivi

* Garder le guide utilisateur et la spécification alignés sur l'API publique.

## Références

* [Spécification du Request Binder](../specifications/request-binder.fr.md).
* [Guide du Request Binder](../../for-users/RequestBinder.fr.md).
* ADR-0007 — nommage des terminaux.
* Issue #144 et pull requests #126 / #141.
