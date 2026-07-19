# ADR-0013 | Contrôler les collections distinctes par la cardinalité, sinon par un tirage borné

🌍 🇬🇧 [English](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Dummies rejette les contraintes contradictoires à leur déclaration et construit
sinon les valeurs depuis la spécification complète, sans boucle de retry non
bornée. Les collections distinctes ajoutent une exigence prouvable précocement
seulement lorsque la taille du domaine du générateur est connue.

Certains générateurs internes ont un petit domaine dénombrable ; les implémentations
arbitraires ou étrangères d'`IAny<T>`, les générateurs dérivés et les comparers
personnalisés peuvent avoir un nombre inconnu ou dépendant de leur distribution de
classes d'équivalence. La cardinalité ne peut donc pas être imposée à l'interface
publique.

## Décision

Une collection distincte rejette immédiatement un compte demandé lorsqu'il dépasse une cardinalité annoncée et utilise sinon un tirage dédupliquant borné qui échoue de manière reproductible à la génération s'il n'obtient pas assez de valeurs distinctes.

## Justification

Lorsqu'une borne supérieure fiable est connue, la dépasser est un conflit de
déclaration qui mérite le même diagnostic que les contradictions scalaires. Quand
le domaine est inconnu, tirer puis dédupliquer est le seul mécanisme général ; il
doit être borné afin qu'une demande impossible ne suspende jamais le test.

La différence de moment d'échec reflète l'information disponible. Un comparer peut
réduire le domaine effectif, de sorte que le chemin borné reste nécessaire après
le contrôle précoce. Le modèle de hint, le budget de collisions et les diagnostics
de replay sont maintenus dans la
[spécification de génération Dummies](../specifications/dummies-generation.fr.md).

## Alternatives envisagées

### Toujours échouer à la génération

Envisagé pour un canal unique. Rejeté car cela reporte des contradictions certaines
et peu coûteuses pour booléens, enums et autres domaines finis connus.

### Exiger une cardinalité de chaque `IAny<T>`

Envisagé pour tout valider immédiatement. Rejeté car les générateurs étrangers ou
dérivés ne peuvent souvent pas fournir une borne fiable.

### Tirer jusqu'à obtenir assez de valeurs

Envisagé car une demande satisfaisable finit par réussir. Rejeté car une demande
insatisfaisable ne termine jamais.

## Conséquences

### Positives

* Les demandes distinctes impossibles et connues échouent dans l'arrangement.
* Les domaines inconnus échouent sans suspendre et avec possibilité de replay.
* `IAny<T>` public reste implémentable sans métadonnée de domaine.

### Négatives

* Le moment de l'échec varie entre domaines connus et inconnus.
* Un budget fini peut rejeter un générateur satisfaisable mais très biaisé.

### Risques

* Un hint peut être trop généreux ou un comparer fusionner davantage de valeurs ;
  le tirage borné détecte alors le manque à la génération.
* Un budget mal ajusté peut provoquer un faux épuisement sur une distribution
  pathologique. Atténuation : rapporter la graine et réexaminer le budget documenté
  si l'usage le démontre ; aucune garantie probabiliste universelle n'est annoncée.

## Actions de suivi

* Expliquer les deux canaux d'échec dans la documentation utilisateur.
* Réexaminer le budget ou le modèle de capacité en cas de faux épuisement rejouable.

## Références

* [Spécification de génération Dummies](../specifications/dummies-generation.fr.md).
* ADR-0011 — package Dummies autonome.
* `Dummies/CollectionState.cs` et `Dummies/ICardinalityHint.cs`.
