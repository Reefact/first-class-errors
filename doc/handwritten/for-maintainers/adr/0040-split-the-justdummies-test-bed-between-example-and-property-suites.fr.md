# ADR-0040 | Répartir le banc de test de JustDummies entre une suite par l'exemple et une suite par propriétés

🌍 🇬🇧 [English](0040-split-the-justdummies-test-bed-between-example-and-property-suites.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-26
**Décideurs :** Reefact

## Contexte

`JustDummies` construit des valeurs arbitraires qui satisfont les contraintes
déclarées. Son affirmation de correction est donc quantifiée universellement :
*chaque* valeur produite par un générateur satisfait *chacune* des contraintes
déclarées sur lui, pour *chaque* combinaison légale d'arguments de contrainte.

Jusqu'ici cette affirmation était prouvée par une seule suite,
`JustDummies.UnitTests`, qui l'établit par échantillonnage. Un test fixe une
contrainte — `Between(10, 20)`, `WithLength(12)`, `WithCount(4)` — puis tire
quelques centaines de valeurs et vérifie l'invariant sur chacune. Les valeurs
tirées varient ; les **arguments de contrainte, non**. La suite instancie donc
l'affirmation universelle en une poignée de points choisis à la main et ne prouve
rien sur le reste de l'espace des contraintes.

Des défauts ont déjà été trouvés dans cet espace non prouvé. L'issue #206 était un
générateur d'intervalle décimal dont les tirages ne franchissaient jamais le milieu
de la plage demandée : tous les candidats tombaient dans la moitié basse. Il a été
trouvé à la main et figé en régression sur un seul intervalle, `[0, 100]`. Le bug
vivait dans la relation entre des bornes arbitraires et la valeur produite —
précisément la dimension qu'un argument fixe ne peut pas faire varier.

La suite tire par ailleurs ses propres valeurs échantillonnées depuis
`JustDummies`, si bien que le composant sous test participe à décider avec quoi il
est testé.

Le dépôt exploite déjà des suites par propriétés. `FirstClassErrors.PropertyTests`
et `FirstClassErrors.RequestBinder.PropertyTests` utilisent FsCheck, portent le
segment du plancher .NET Framework 4.7.2, et sont ce que le contrôle *Fuzzing* de
l'OpenSSF Scorecard lit pour créditer le projet. Aucune des deux n'a été introduite
par un ADR : un projet frère `*.PropertyTests` est ici une pratique établie, non un
mouvement architectural nouveau.

Tous les contrats de la bibliothèque ne sont pas quantifiés universellement. Un
conflit doit lever `ConflictingAnyConstraintException` avec un message nommant *les
deux* contraintes fautives ; un argument nul doit lever `ArgumentNullException` ; le
miroir entre `Any` et `AnyContext`, la convention de nommage des fabriques et la
frontière d'assemblage autonome de la bibliothèque sont des faits structurels
vérifiés par réflexion. Ce sont des cas spécifiques et nommés, et leur formulation
est délibérément sensible au sens de l'application — une propriété qui les
quantifierait affirmerait moins, et moins lisiblement.

L'audit d'architecture de juillet 2026 a relevé que l'ADR-0025 cite « a property
test » contre le vrai moteur d'expressions régulières, alors que ce qui existe est
un test-oracle à graine fixe et corpus fixe dans le projet de tests unitaires, et a
demandé que le texte dise ce qu'est réellement le filet de sécurité.

## Décision

`JustDummies` est testé par deux suites sœurs sous une frontière unique :
`JustDummies.PropertyTests` porte tout invariant quantifiable sur des **arguments
de contrainte générés**, et `JustDummies.UnitTests` porte tout contrat dont le sujet
est un cas spécifique et nommé — contenu des messages, validation des arguments,
conventions structurelles et régressions datées.

## Justification

L'affirmation de la bibliothèque est une quantification universelle : le test dont
la forme lui correspond est donc celui qui quantifie. Générer les arguments de
contrainte — bornes, longueurs, cardinalités, viviers et graines qu'un appelant
déclare — fait passer chaque test du statut d'instance de l'affirmation à celui de
l'affirmation elle-même, et déplace la recherche vers l'espace où #206 vivait
réellement. Tirer davantage de valeurs derrière un `Between(10, 20)` fixe n'en
explore rien.

Un cadre de propriétés rapporte aussi les échecs différemment. Le rétrécissement
réduit un contre-exemple à sa forme minimale : un défaut arrive donc sous la forme
du plus petit intervalle et de la plus petite valeur qui le déclenchent, plutôt que
comme un tirage opaque parmi quelques centaines. Pour un composant dont les échecs
sont des cas limites arithmétiques, c'est la différence entre un diagnostic et un
point de départ.

Tirer les contraintes depuis un générateur indépendant brise la circularité relevée
dans le Contexte : la suite n'utilise plus `JustDummies` pour décider avec quoi
tester `JustDummies`. Le biais propre de FsCheck vers les petites valeurs est
compensé en y mêlant explicitement les bords du domaine, sans quoi un décalage d'une
unité à `int.MaxValue` ne serait pour ainsi dire jamais tiré.

La frontière est tracée là où chaque style est réellement le plus fort, non par
nature de code. Le contenu des messages, le traitement des nuls et les gardes de
convention par réflexion ne sont pas des affirmations universelles ; les exprimer en
propriétés ajouterait une quantification sur des entrées qui ne varient pas,
brouillerait ce qui est affirmé, et rendrait les assertions sur le libellé exact plus
difficiles à lire. Les garder dans la suite par l'exemple laisse chaque suite dire ce
qu'elle dit le mieux, et fait que la localisation d'un échec indique déjà quelle
classe de contrat a cédé.

Les régressions datées restent avec les exemples pour la même raison. Une régression
fige un défaut qui a réellement eu lieu, aux coordonnées où il a eu lieu ; cette
spécificité est sa valeur, et une propriété couvrant le même terrain ne la retire pas.

Deux projets frères plutôt qu'un projet mixte suit la convention que le dépôt applique
déjà deux fois, garde la dépendance FsCheck hors de la suite qui n'en a pas besoin, et
laisse chaque projet énoncer sa propre histoire de plancher applicatif.

## Alternatives considérées

### Élargir les boucles d'échantillonnage de la suite existante

Augmenter le nombre d'échantillons et ajouter davantage d'intervalles choisis à la
main est le changement le moins coûteux et ne demande aucun nouveau projet.

Rejeté : cela multiplie les tirages à l'intérieur des mêmes arguments de contrainte
fixes. La dimension laissée inexplorée — la relation entre une borne arbitraire et la
valeur produite — le reste, de sorte que la classe de défaut à laquelle #206
appartenait demeure invisible. On achète du temps d'exécution, pas de l'information.

### Convertir tout le banc de test en propriétés

Une suite unique est plus simple à expliquer, et les tests de forme invariante
gagneraient tous à être quantifiés.

Rejeté : les contrats décrits dans le Contexte ne sont pas quantifiés
universellement. Une propriété affirmant qu'un message de conflit nomme les deux
contraintes est un moins bon test par l'exemple — elle ne quantifie sur rien tout en
rendant l'assertion plus difficile à lire — et les gardes de convention par réflexion
n'ont aucun espace d'entrée.

### Héberger les propriétés dans `JustDummies.UnitTests`

Un seul projet, c'est une seule chose à construire, exécuter et configurer.

Rejeté : cela place deux styles d'assertion et deux jeux de dépendances dans un même
assemblage, et perd le signal que le nom d'un projet en échec porte déjà. Cela
s'écarterait aussi de la convention de projet frère que le dépôt a appliquée à
`FirstClassErrors` et `FirstClassErrors.RequestBinder`.

### Générer les arguments de contrainte avec `JustDummies` lui-même

La bibliothèque est un générateur de valeurs : elle pourrait fournir ses propres
entrées de test et éviter une dépendance.

Rejeté : cela approfondit la circularité au lieu de la briser. Un défaut du générateur
serait alors libre de biaiser les entrées mêmes censées l'exposer, et aucun échec ne
pourrait être attribué avec confiance.

## Conséquences

### Positives

* L'affirmation universelle de la bibliothèque est prouvée sur un espace de contraintes
  plutôt qu'en une poignée de points, et les défauts de la classe de #206 deviennent
  atteignables par la suite.
* Une propriété en échec arrive rétrécie à un contre-exemple minimal.
* Chaque suite énonce une seule sorte de contrat : la localisation d'un échec le classe
  déjà.
* La suite par propriétés porte le segment du plancher .NET Framework 4.7.2 comme ses
  sœurs, de sorte que les invariants sont prouvés contre l'asset `netstandard2.0` que
  les consommateurs chargent réellement.
* L'aller-retour sur les expressions régulières est prouvé par une véritable propriété,
  ce qui permet de reformuler exactement l'affirmation de l'ADR-0025 : le constat
  d'audit qui a motivé ce point est clos par construction plutôt que par une réécriture.

### Négatives

* Deux suites sont à garder en tête quand un générateur change, et la frontière doit
  être appliquée délibérément plutôt que par habitude.
* Un invariant déjà prouvé par une propriété peut malgré tout être réaffirmé par un
  exemple qui paraît redondant lu isolément ; la redondance est voulue quand l'exemple
  est une régression datée, et non voulue sinon.
* Les générateurs par défaut de FsCheck demandent un biaisage explicite vers les bords
  pour être utiles ici, ce qui fait du code de support de test supplémentaire à maintenir.

### Risques

* Une propriété dont les arguments générés chevauchent une frontière de légalité
  dépendante de la valeur (ADR-0035) peut être écrite de façon à échouer par
  intermittence plutôt que de manière déterministe. Une propriété doit décider du
  résultat attendu à partir de la valeur générée, non de la forme de l'appel.
* Les propriétés statistiques — qu'une plage soit atteinte, que les deux branches d'un
  tirage à pile ou face soient observées — sont probabilistes, non universelles. Écrites
  sans soin elles deviennent instables ; elles relèvent d'une graine figée et doivent
  être signalées comme gardes statistiques.
* Élaguer la suite par l'exemple à mesure que les propriétés arrivent peut réduire
  silencieusement la couverture si l'on retire un exemple dont la propriété ne subsume
  pas réellement l'invariant.

## Actions de suivi

* Publier la frontière sous forme de guide mainteneur, afin qu'un contributeur ajoutant
  une contrainte ou corrigeant un défaut puisse placer son test sans re-dériver cette
  décision.
* Réexaminer la formulation « property test » de l'ADR-0025, que l'audit a signalée comme
  inexacte, maintenant qu'une propriété d'aller-retour existe. Seul `@reefact` peut
  amender ou remplacer un ADR accepté.

## Références

* [ADR-0025](0025-generate-strings-from-a-home-grown-regular-subset.fr.md) — le sous-ensemble régulier dont cette suite prouve l'aller-retour
* [ADR-0035](0035-enforce-structural-any-conflicts-at-compile-time.fr.md) — conflits structurels contre conflits dépendants de la valeur, qui décident de la façon dont une propriété doit se ramifier
* [ADR-0036](0036-draw-lattice-constrained-scalars-on-the-grid.fr.md) — les contraintes de treillis, dont l'invariant de grille est quantifié par la suite par propriétés
* [Audit d'architecture et de conception de JustDummies, 2026-07-20](../audit/2026-07-20-dummies-architecture-and-design-audit.fr.md) — le constat « non property-based » sur l'ADR-0025
* Issue #206 — le défaut d'intervalle décimal qui a motivé la quantification sur les bornes
