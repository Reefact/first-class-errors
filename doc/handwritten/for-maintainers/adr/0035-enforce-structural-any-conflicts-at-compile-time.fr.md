# ADR-0035 | Détecter les conflits structurels de Any à la compilation, ceux dépendant de la valeur à l'exécution

🌍 🇬🇧 [English](0035-enforce-structural-any-conflicts-at-compile-time.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-26
**Décideurs :** Reefact

## Contexte

* Chaque générateur du point d'entrée `Any` de `Dummies` — et son miroir `AnyContext` — a été jusqu'ici un
  **builder plat** : un type unique expose toutes les méthodes de contrainte, les méthodes s'enchaînent dans
  n'importe quel ordre, et une combinaison incompatible est signalée à l'**exécution** par une
  `ConflictingAnyConstraintException` dont le message nomme les deux côtés (« Cannot apply X because Y is already
  defined »). Une spécification qui ne se révèle insatisfiable que pendant la production d'une valeur lève une
  `AnyGenerationException`, qui porte la graine. Le système de types n'est jamais utilisé pour empêcher une
  combinaison.
* Deux sortes d'incompatibilité surviennent sur cette surface. L'une est **structurelle** : elle vaut pour la
  combinaison elle-même, pour toute valeur d'argument — sur `Any.String()`, un second jeu de caractères après un
  premier est toujours fautif. L'autre **dépend de la valeur** : le même appel de méthode est licite ou illicite
  selon la valeur d'exécution de son argument — `Any.String().Numeric().StartingWith("ORD-")` est en conflit
  parce que les lettres du préfixe tombent hors du jeu numérique, tandis que
  `Any.String().Numeric().StartingWith("123")` est valide ; le point d'appel et les types statiques sont
  identiques dans les deux cas.
* `Any.Uri()` (issue #226) est le premier générateur dont l'espace se partitionne en **formes** structurellement
  différentes : une URI web absolue, WebSocket, FTP ou mailto, ou une référence relative. Chaque forme admet un
  ensemble de composants différent et fixé par la RFC — un mailto n'a ni port ni autorité (RFC 6068), une URI
  WebSocket ni user-info ni fragment (RFC 6455), une URI FTP ni requête ni fragment, une référence relative ni
  schéma ni autorité. Quels composants sont licites est fixé par la forme, non par une valeur.
* Une erreur de catégorie entre ces formes — un port sur un mailto, un fragment sur une URI WebSocket — est donc
  structurelle au sens ci-dessus, et connue avant qu'aucune valeur ne soit tirée.
* C# sait rendre un membre indisponible sur un type. Un générateur qui retourne un **type différent par forme**,
  chacun n'exposant que les composants de sa forme, transforme une erreur de catégorie en du code qui ne compile
  pas, là où un unique `AnyUri` plat exposant tous les composants ne pourrait rejeter la même erreur qu'à
  l'exécution.
* `Dummies` est en pré-publication : aucun tag `dum-v*`, aucun consommateur externe, une section *Unreleased* de
  changelog vide. La forme de sa surface de générateurs publique peut encore être fixée sans coût de migration.
* Le dépôt consigne sous forme d'ADR les décisions qui façonnent la surface publique `Any` — ADR-0020
  (matérialiser uniquement via `Generate()`), ADR-0031 (nommer les fabriques d'après leur type CLR), ADR-0006
  (une seule source graine). Une règle nouvelle et transverse sur la *manière* dont la surface signale une
  combinaison illicite est une décision de cette même classe.

## Décision

Une combinaison de contraintes illicite sur la surface `Any` est rendue impossible à écrire à la compilation —
au moyen d'une progression typée qui retourne un builder propre à la forme n'exposant que les membres de cette
forme — lorsque l'illicéité est structurelle, et est sinon laissée au chemin d'exécution
`ConflictingAnyConstraintException` / `AnyGenerationException` lorsqu'elle dépend d'une valeur générée.

## Justification

* La ligne de partage est la décidabilité par le compilateur, et elle tombe exactement là où tombent les deux
  sortes d'incompatibilité du Contexte. Une erreur structurelle est une propriété de la combinaison, donc le
  système de types *peut* la porter ; une erreur dépendant de la valeur est une propriété d'un argument que le
  compilateur ne voit jamais, donc le système de types *ne peut pas* la porter et une vérification à l'exécution
  est la seule option. La règle suit le grain de ce que chaque point d'application est capable de savoir.
* Appliquer la progression typée au cas dépendant de la valeur n'est pas seulement inutile, c'est impossible :
  aucun agencement de types ne distingue `StartingWith("ORD-")` de `StartingWith("123")`, puisqu'ils ne
  diffèrent que par une valeur. Le patron plat à l'exécution n'y est donc pas un repli plus faible — c'est le
  seul mécanisme capable d'exprimer la contrainte tout court.
* Inversement, laisser une erreur structurelle d'URI à l'exécution jette une garantie disponible gratuitement.
  `Mailto().WithPort(...)` est fautif pour tout argument possible ; l'exposer comme une génération en échec, ou
  même comme une `ConflictingAnyConstraintException` levée, reporte à l'exécution une erreur que le compilateur
  attraperait sinon à la frappe, sans aucun gain.
* Rendre les erreurs de catégorie impossibles à écrire les retire aussi de la surface qu'un lecteur doit
  apprendre : un builder propre à la forme qui n'offre jamais `WithPort` ne peut pas être mal employé ainsi, si
  bien que la règle RFC « un mailto n'a pas de port » est enseignée par l'API plutôt que par un message
  d'exécution. C'est le même raisonnement « rendre la règle impossible à enfreindre plutôt que seulement
  vérifiée » que l'ADR-0031 a appliqué au nommage des fabriques.
* Le coût du chemin typé — plusieurs types builder publics pour une famille au lieu d'un seul — est le genre de
  décision de surface unique que la fenêtre de pré-publication absorbe sans frais, et il est confiné aux
  générateurs dont l'espace se scinde réellement en formes fixes ; le patron plat reste le défaut partout
  ailleurs, si bien que la surface ne se fragmente pas builder par builder.

## Alternatives envisagées

### Garder chaque générateur plat et signaler tous les conflits à l'exécution

Envisagée parce que c'est le patron établi de la bibliothèque, qu'elle donne un modèle mental uniforme
(« enchaîner librement, apprendre les conflits par les exceptions ») et qu'elle garde le plus petit nombre de
types publics — un unique `AnyUri` au lieu d'une famille.

Rejetée parce qu'elle dépense une garantie qu'elle n'a pas à dépenser : une erreur de catégorie comme un port
sur un mailto est connaissable à la compilation, et une surface uniquement d'exécution peut au mieux lever pour
elle une fois que le code compile et tourne déjà. L'uniformité serait préservée au mauvais endroit — faisant se
comporter l'erreur décidable par le compilateur comme celle dépendant de la valeur, alors que seule la seconde
est réellement contrainte à l'exécution.

### Faire de chaque générateur une progression typée

Envisagée par symétrie — un seul modèle d'application sur toute la surface `Any` — et parce qu'elle déplacerait
davantage d'erreurs vers la compilation en général.

Rejetée parce que la plupart des conflits de la surface dépendent de la valeur (préfixes, valeurs contenues,
exclusions, jeu des longueurs), ce qu'aucun agencement de types ne peut décider ; leur imposer des types ne peut
pas fonctionner, et multiplierait soit des types builder sans retirer une seule vérification d'exécution, soit
rétrécirait en silence la surface en deçà de ce que le générateur est censé exprimer. La progression typée ne
mérite son coût que là où un espace se scinde en formes fixes.

### Appliquer les règles de catégorie d'URI par un analyseur Roslyn au-dessus d'un builder plat

Envisagée parce que la bibliothèque livre déjà des analyseurs, si bien qu'un diagnostic pourrait signaler
`Mailto().WithPort()` sur un unique `AnyUri` plat tout en gardant un seul type.

Rejetée parce qu'elle réintroduit, comme vérification externe, un invariant que le système de types peut tenir
intrinsèquement : un analyseur peut être supprimé, accuse un retard sur le compilateur, et doit être documenté et
testé comme sa propre surface, là où un membre absent ne peut tout simplement pas être écrit. Un analyseur est
le bon outil pour une odeur *dépendant de la valeur* que les types ne peuvent pas attraper, pas pour une règle
structurelle qu'ils peuvent porter.

## Conséquences

### Positives

* Les erreurs de catégorie dans un générateur partitionné par forme deviennent des erreurs de compilation :
  `Mailto().WithPort(...)` et `WebSocket().WithFragment(...)` ne compilent pas, au lieu d'échouer à l'exécution.
* L'ensemble des composants licites de chaque forme d'URI est enseigné par le builder de cette forme — l'API est
  auto-documentée là où elle s'appuyait sur un message d'exécution.
* La règle énonce clairement quel point d'application un nouveau générateur doit employer, indexé sur une
  propriété (structurelle vs dépendant de la valeur) qui est déjà la distinction pertinente sur la surface.

### Négatives

* La surface `Any` n'est plus à modèle unique : un contributeur doit reconnaître lequel des deux patrons appelle
  un nouveau générateur, au lieu de toujours se tourner vers le builder plat.
* Un générateur partitionné par forme porte plusieurs types builder publics au lieu d'un seul, augmentant le
  nombre de types et la référence d'API publique pour cette famille.

### Risques

* La ligne « structurel vs dépendant de la valeur » peut être mal jugée pour un générateur futur — typer quelque
  chose dont les conflits dépendent en fait de la valeur (surface de type morte), ou laisser une scission
  réellement structurelle à l'exécution (une garantie de compilation manquée) ; atténué en gardant le patron
  plat à l'exécution comme défaut et en réservant la progression typée à un espace qui se scinde
  démonstrativement en formes fixes.
* La progression typée pourrait être sur-appliquée par nouveauté, fragmentant la surface ; atténué en consignant
  ici qu'elle est l'exception — justifiée par une partition en formes fixes — et non le nouveau défaut.

## Actions de suivi

* Aucune requise. `Any.Uri()` (issue #226, première application) réalise déjà le côté progression typée, et la
  surface `AnyString` existante réalise déjà le côté exécution ; cet ADR consigne la règle qu'ils établissent
  conjointement.
* Appliquer la règle lorsque l'espace d'un générateur futur se scinde en formes fixes ; sinon, garder le patron
  plat à l'exécution.

## Références

* ADR-0020 — matérialiser les dummies uniquement via `Generate()` ; partage le sujet « forme de la surface
  `Any` ».
* ADR-0031 — nommer les fabriques de Any d'après leur type CLR ; précédent du « rendre la règle impossible à
  enfreindre plutôt que seulement vérifiée », et de la consignation des décisions de surface `Any` comme ADR.
* ADR-0006 — fournir les valeurs arbitraires depuis une seule source graine ; la graine portée par
  `AnyGenerationException` sur le chemin d'exécution.
* PR #295 — ajouter la famille `Any.Uri()`, la première progression typée.
* Issue #226 — le backlog Nice-to-Have de Dummies qui a motivé `Any.Uri()`.
