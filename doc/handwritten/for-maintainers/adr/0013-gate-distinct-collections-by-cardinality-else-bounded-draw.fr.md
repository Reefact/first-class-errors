# ADR-0013 | Contrôler les collections distinctes par la cardinalité, sinon par un tirage borné

🌍 🇬🇧 [English](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Dummies traite les contraintes contradictoires comme des erreurs d'arrangement et évite les boucles de nouvelles tentatives cachées et non bornées.

Une collection distincte de `N` éléments n'est satisfaisable que si au moins `N` valeurs distinctes peuvent être assemblées depuis son domaine effectif : le domaine propre du générateur d'éléments, élargi par les valeurs fixées en dehors de celui-ci et par les valeurs opaques fournies de l'extérieur que le générateur lui-même ne pourrait jamais tirer. La cardinalité propre du générateur ne borne donc que les éléments qui doivent venir de lui, non la demande entière.

Certains générateurs exposent un domaine que la bibliothèque sait compter à bas coût — un petit ensemble fixe, ou une valeur fixée sur l'un de ses membres. D'autres ne peuvent pas annoncer honnêtement la taille de leur domaine, soit parce que la compter est disproportionnément coûteux (une plage flottante, par exemple), soit parce qu'il est véritablement non borné ou inconnaissable, notamment les implémentations externes de `IAny<T>` et les générateurs composés.

Un comparateur d'égalité personnalisé peut réduire le nombre de classes d'équivalence effectives même lorsque le domaine nominal du générateur est plus grand.

## Décision

Une collection distincte rejette immédiatement un nombre demandé supérieur à une cardinalité effective connue du domaine des éléments, et utilise sinon un tirage dédupliqué borné qui échoue explicitement et de manière reproductible lorsqu'il n'est pas possible d'obtenir assez de valeurs distinctes.

## Justification

Lorsque la taille du domaine est connue, la contradiction est certaine et doit être signalée au moment de la déclaration, comme les autres validations de contraintes de Dummies.

Ne compter que la cardinalité propre du générateur rejetterait par anticipation des demandes en réalité satisfaisables une fois prises en compte les valeurs déjà couvertes ; le contrôle anticipé compare donc à la taille du domaine diminuée des valeurs déjà fixées ou fournies de façon opaque en dehors de lui, ce qui le garde correct : il ne rejette jamais une demande réellement satisfaisable, et un comparateur qui réduit le domaine effectif sous le nombre demandé reste rattrapé par le tirage borné.

Lorsque la taille du domaine est inconnue, tirer puis dédupliquer est la seule stratégie générale disponible. Borner le travail garantit la terminaison et transforme une demande impossible ou pratiquement inaccessible en échec de génération diagnostiquable plutôt qu'en blocage.

La capacité de cardinalité reste optionnelle afin de ne pas imposer aux générateurs publics ou externes une information qu'ils ne peuvent pas connaître. Une réduction induite par le comparateur est alors prise en charge par la borne à la génération.

L'interface d'indication exacte, l'état de collection, le budget de tirage, le contenu de l'exception et la propagation de la seed sont documentés dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#contrats-de-génération-de-dummies) et la documentation utilisateur de Dummies.

## Alternatives envisagées

### Toujours échouer à la génération

Envisagé car un point d'échec unique est plus simple. Rejeté parce que cela supprime un diagnostic exact au moment de la déclaration pour les générateurs dont la taille du domaine est connue.

### Exiger une cardinalité de chaque générateur

Envisagé pour rendre chaque demande décidable immédiatement. Rejeté parce que de nombreux générateurs valides ne peuvent pas fournir une borne fiable et que l'interface publique accepte des implémentations externes.

### Tirer sans borne

Envisagé car une demande satisfaisable finirait par aboutir. Rejeté parce qu'une demande insatisfaisable pourrait boucler indéfiniment.

## Conséquences

### Positives

* Les contradictions connues échouent tôt et clairement.
* Les domaines inconnus échouent tout de même de manière sûre, reproductible et sans blocage.
* Les générateurs externes restent compatibles sans implémenter de métadonnées de cardinalité.

### Négatives

* Le moment de l'échec diffère entre domaines connus et inconnus.
* Un tirage borné peut échouer pour un générateur théoriquement satisfaisable mais fortement biaisé.

### Risques

* Un générateur peut annoncer une borne supérieure inexacte. Mesure : le tirage borné reste le filet de sécurité final.
* Un budget mal calibré peut provoquer des échecs indus. Mesure : documenter le budget, tester des générateurs biaisés représentatifs et le réviser sur la base de faits plutôt que de présenter l'échec comme impossible.

## Actions de suivi

* Documenter les deux canaux d'échec et la seed de rejeu dans le guide Dummies.
* Réexaminer le budget si l'usage réel révèle des épuisements indus.

## Références

* [Référence d'implémentation des ADR — Contrats de génération de Dummies](../specifications/adr-implementation-reference.fr.md#contrats-de-génération-de-dummies)
* [ADR-0011](0011-host-dummies-as-a-standalone-package.fr.md)
* `CollectionState` et `ICardinalityHint` dans le projet `Dummies`.
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
