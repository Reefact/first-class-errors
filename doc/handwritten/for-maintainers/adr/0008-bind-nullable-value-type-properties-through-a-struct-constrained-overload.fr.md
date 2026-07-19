# ADR-0008 | Lier les propriétés de type valeur nullable via une surcharge contrainte à struct

🌍 🇬🇧 [English](0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Le Request Binder permet de sélectionner une propriété de DTO puis de la convertir au moyen d'une factory de value object, souvent fournie sous forme de groupe de méthodes.

Pour les propriétés de type valeur nullable, le sélecteur générique non contraint d'origine inférait `Nullable<T>` comme entrée du convertisseur. Une factory opérant sur le type valeur sous-jacent ne pouvait donc pas être liée comme groupe de méthodes, alors même que le DTO utilisait correctement une propriété nullable pour distinguer l'absence d'une valeur par défaut effectivement fournie.

C# ne permet pas de surcharger des méthodes en ne changeant que les contraintes génériques, mais un sélecteur contraint à `struct` peut exposer `Nullable<T>` comme forme de paramètre distincte tout en conservant un convertisseur sur le type sous-jacent non nullable.

Les listes de types valeur nullables exigent également une gestion explicite des éléments `null` avant conversion.

## Décision

Le Request Binder lie les propriétés de DTO et les éléments de liste de type valeur nullable au moyen de chemins de sélection dédiés `where TArgument : struct`, dont les convertisseurs opèrent sur le type valeur sous-jacent non nullable.

## Justification

Le chemin dédié rétablit la même ergonomie de groupe de méthodes que pour les propriétés de type référence, tout en préservant la distinction sémantique entre un argument absent et une valeur par défaut fournie.

La surcharge doit se situer à la frontière du sélecteur, car c'est là que l'inférence de types de C# choisit sinon `Nullable<T>` et produit pour le consommateur une erreur de compilation peu explicite.

Les éléments de liste de type valeur nullable ne peuvent pas réutiliser sans risque l'implémentation destinée aux références, puisqu'ils exigent une gestion propre de l'absence et un déballage avant conversion.

Stabiliser cette API additive avant la première version stable évite d'introduire plus tard un risque de compatibilité source lié à la résolution des surcharges.

Les signatures exactes, les types de convertisseurs, le comportement des éléments `null` et les exemples sont documentés dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#contrats-dimplémentation-du-request-binder) et la documentation utilisateur du Request Binder.

## Alternatives envisagées

### Exiger une lambda adaptatrice à chaque appel

Envisagé car cela ne nécessite aucune nouvelle API publique. Rejeté parce que cela expose au consommateur une erreur d'inférence peu intuitive et duplique dans le code applicatif une logique de déballage qui appartient au binder.

### Utiliser un sélecteur unique pour les types référence et valeur

Envisagé pour minimiser la surface. Rejeté parce que C# ne peut pas inférer le type valeur sous-jacent depuis une annotation nullable non contrainte tout en conservant le comportement des types référence.

### Réutiliser le convertisseur de liste destiné aux références

Envisagé car le flux de conversion est autrement similaire. Rejeté parce que les éléments de type valeur nullable exigent une gestion distincte des éléments manquants et un déballage.

## Conséquences

### Positives

* Les propriétés de type valeur nullable se lient avec la même ergonomie de groupe de méthodes que les propriétés de type référence.
* L'absence reste observable tandis que les convertisseurs reçoivent la valeur sous-jacente pertinente.
* La forme publique est stabilisée avant le gel de l'API stable.

### Négatives

* Le binder expose des sélecteurs et convertisseurs supplémentaires.
* Le mécanisme de résolution des surcharges est subtil et nécessite des tests de régression.

### Risques

* De futures formes de sélecteurs pourraient réintroduire une ambiguïté. Mesure : conserver une couverture de compilation ciblée pour les références, les chaînes, les valeurs scalaires et les listes.

## Actions de suivi

* Maintenir la documentation bilingue du Request Binder alignée sur le comportement accepté.

## Références

* [Référence d'implémentation des ADR — Contrats d'implémentation du Request Binder](../specifications/adr-implementation-reference.fr.md#contrats-dimplémentation-du-request-binder)
* [ADR-0007](0007-name-the-binder-terminals-new-and-create.fr.md)
* Issue #144 et pull requests #126 et #141.
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
