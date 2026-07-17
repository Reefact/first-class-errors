# ADR-0008 | Lier les propriétés de type valeur nullable via une surcharge contrainte à struct

🌍 🇬🇧 [English](0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-16
**Décideurs :** Reefact

## Contexte

* Le request binder sélectionne chaque propriété du DTO avec `SimpleProperty(r => r.X)` ou
  `ListOfSimpleProperties(r => r.X)`, puis un convertisseur la lie — une fabrique de value
  object `Func<TArgument, Outcome<TProperty>>`, typiquement un groupe de méthodes tel que
  `EmailAddress.Parse`.
* Le sélecteur d'origine est générique sur `TArgument`, avec un paramètre
  `Expression<Func<TRequest, TArgument?>>` non contraint.
* Lorsque la propriété du DTO est un type valeur nullable (`int?`), le sélecteur non
  contraint infère `TArgument = Nullable<int>`, car pour un paramètre de type non contraint
  le `?` est une annotation sans effet sur les types valeur. L'étape de conversion attend
  alors `Func<Nullable<int>, Outcome<T>>`, si bien qu'un groupe de méthodes sur le type
  sous-jacent (`int -> Outcome<T>`) ne correspond pas et l'appel échoue à compiler avec
  **CS0411**.
* Une propriété de type valeur non nullable est déjà rejetée au moment du binding (constat
  de revue n°4, livré dans #141) : une telle propriété doit être déclarée nullable pour
  qu'un argument manquant se distingue d'une valeur par défaut légitimement envoyée.
* C# ne traite pas deux méthodes qui ne diffèrent que par une contrainte `class` versus
  `struct` comme des signatures distinctes — une contrainte ne fait pas partie de la
  signature (CS0111).
* Sous une contrainte `where TArgument : struct`, `TArgument?` désigne le type construit
  `Nullable<TArgument>`. C'est un type de paramètre différent du `TArgument` nu du sélecteur
  non contraint et — étant un type construit — il est structurellement plus spécifique pour
  la résolution de surcharge.
* Un élément de liste `Nullable<TArgument>` peut indépendamment être `null` ; un
  convertisseur sur le type sous-jacent non nullable ne peut pas représenter cet élément, et
  le convertisseur de liste de référence déréférence les éléments sans déballer un
  `Nullable`.
* La bibliothèque est en pré-version, non publiée sur NuGet et sans consommateurs externes. Des
  surcharges additives figées avant le gel de la v1 ne peuvent pas déplacer l'inférence sur
  des sites d'appel de consommateurs qui n'existent pas encore ; les mêmes surcharges
  ajoutées après que des consommateurs ont écrit des bindings de type valeur pourraient
  changer la résolution sur ces sites.

## Décision

Le request binder lie une propriété de DTO de type valeur nullable au travers d'une surcharge
de sélecteur dédiée `where TArgument : struct` dont le sélecteur porte `Nullable<TArgument>`
et dont le convertisseur opère sur le type sous-jacent non nullable.

## Justification

* Le paramètre `Nullable<TArgument>` de la surcharge contrainte à `struct` est un type
  véritablement différent — et structurellement plus spécifique — que le `TArgument` nu de la
  surcharge non contrainte, si bien que les deux coexistent sans CS0111 et que la surcharge
  de type valeur l'emporte pour une propriété de type valeur nullable, tandis que les
  propriétés de référence et de type `string` continuent de se résoudre vers la surcharge non
  contrainte. L'échec CS0411 est supprimé au niveau du sélecteur plutôt que reporté sur le
  consommateur sous la forme d'une lambda d'adaptation.
* Faire remonter le type sous-jacent non nullable (`int`, et non `int?`) permet à une fabrique
  de value object de lier sous forme de groupe de méthodes exactement comme elle le fait pour
  une propriété de référence, gardant une même ergonomie fluide pour les deux sortes de
  propriétés : la propriété reste déclarée nullable pour que l'absence demeure observable, et
  le type sous-jacent est ce sur quoi le convertisseur opère concrètement.
* Un convertisseur de liste dédié est requis plutôt qu'une réutilisation, parce qu'un élément
  `Nullable` a besoin de sa propre gestion du `null` — un élément `null` est un argument
  manquant enregistré sous son chemin indexé — et d'un déballage avant conversion, ni l'un ni
  l'autre n'étant réalisés par le convertisseur de référence.
* Décider avant le gel de la v1 fige la forme de l'API tant qu'elle est encore libre : les
  surcharges sont additives maintenant, sans aucun site d'appel à perturber, alors que les
  différer au-delà du gel ferait de la même addition un changement cassant à la source pour
  les bindings de type valeur des consommateurs.

## Alternatives envisagées

### Exiger des consommateurs qu'ils passent une lambda d'adaptation

Envisagée parce qu'elle ne nécessite aucune nouvelle API : `AsRequired(v => PositiveInt.From(v))`
lie là où le groupe de méthodes nu ne le fait pas.

Rejetée parce qu'elle dégrade silencieusement l'ergonomie du groupe de méthodes pour le cas
courant du type valeur nullable, fait surgir une erreur de compilation CS0411 sans cause
évidente, et duplique à chaque site d'appel le déballage que le binder peut effectuer une
seule fois.

### Un sélecteur unique unifiant types référence et types valeur

Envisagée parce qu'une seule surcharge est la surface la plus réduite.

Rejetée parce que C# ne peut pas l'exprimer : un `TArgument?` non contraint ne peut pas à la
fois inférer le type sous-jacent pour une propriété de type valeur et rester une référence
pour une propriété de type référence, et une contrainte ne peut pas varier au sein d'une même
méthode.

### Réutiliser le convertisseur de liste de référence pour les listes de types valeur

Envisagée parce que la logique de binding est par ailleurs identique.

Rejetée parce qu'un élément `Nullable<T>` et un élément de référence nécessitent une gestion
du `null` différente, et que le chemin de type valeur doit déballer chaque élément présent
avant conversion ; fondre les deux dans un seul convertisseur masquerait qu'un élément `null`
est un argument manquant enregistré.

## Conséquences

### Positives

* Une propriété de DTO de type valeur nullable (`int?`, `bool?`, ...) et les listes de
  celles-ci se lient via un groupe de méthodes sur le type sous-jacent, avec la même ergonomie
  que les propriétés de référence.
* Le correctif est additif et figé avant le gel de la v1, si bien qu'il ne devient jamais un
  changement cassant à la source pour le binding de type valeur existant d'un consommateur.
* Les propriétés de référence et de type `string` ne sont pas affectées : elles continuent de
  se résoudre vers la surcharge d'origine.

### Négatives

* Deux surcharges de sélecteur publiques supplémentaires et un type de convertisseur public de
  plus à documenter et à maintenir, doublant la surface de sélecteur de `SimpleProperty` et
  `ListOfSimpleProperties`.
* Le mécanisme — une contrainte `struct` transformant `TArgument?` en un `Nullable<TArgument>`
  plus spécifique — est subtil ; un mainteneur peu familier de la règle de résolution de
  surcharge peut ne pas voir immédiatement pourquoi les deux sélecteurs coexistent.

### Risques

* Une future troisième forme de sélecteur pourrait interagir avec les deux surcharges de
  manière à réintroduire de l'ambiguïté ; atténué par cette ADR et par des tests de régression
  qui épinglent la résolution des types référence et `string` sur la surcharge d'origine.

### Actions de suivi

* Garder le guide RequestBinder (EN et français) synchronisé avec l'ergonomie de binding de
  type valeur.

## Références

* ADR-0007 — nommer les terminaux du binder New et Create, la décision d'API publique sœur sur
  le même binder.
* Pull requests #126 et #141 — la fonctionnalité de request binder et le garde-fou de type
  valeur non nullable que cette décision complète.
* Issue #144 — le constat CS0411 que cette décision résout.
