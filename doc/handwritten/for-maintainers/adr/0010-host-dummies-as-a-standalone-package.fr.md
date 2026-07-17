# ADR-0010 | Héberger Dummies comme package autonome dans ce dépôt

🌍 🇬🇧 [English](0010-host-dummies-as-a-standalone-package.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-17
**Décideurs :** Reefact

## Contexte

`FirstClassErrors.Testing` fournit des valeurs de test arbitraires via une façade
`Any` orientée erreurs, adossée à une source unique à graine (ADR-0006). Cet ADR
listait, en action de suivi, l'extraction du moteur générique de valeurs vers un
utilitaire autonome et agnostique des erreurs, et le moteur avait été gardé
séparable en interne à cette fin.

Une nouvelle bibliothèque, `Dummies`, fournit désormais une DSL fluide de
générateurs typés porteurs de contraintes (`IAny<T>`) pour des valeurs de test
arbitraires mais valides. Ses contraintes expriment les invariants qu'une valeur
doit satisfaire — le format d'un value object, une précondition de contrat — ce
qui vise les tests orientés domaine en général, pas la gestion d'erreurs : la
bibliothèque ne connaît rien de FirstClassErrors, cible `netstandard2.0` et n'a
aucune dépendance. Son audience visée dépasse les utilisateurs de
FirstClassErrors.

Deux faits contraignent où et sous quel nom elle est publiée :

* Un identifiant de package NuGet est de fait permanent : le renommer après
  adoption signifie publier un nouveau package et imposer une migration aux
  consommateurs.
* Ce dépôt porte déjà l'appareillage de publication qu'un package publié
  requiert — CI avec cliquet zéro warning, SBOM embarqué, SourceLink, trains de
  release pilotés par tag et sélectionnés par liste explicite de projets,
  conventions de commit, et cette base d'ADR. Un dépôt séparé devrait tout
  dupliquer.

L'API de la bibliothèque évoluera le plus vite dans ses premières itérations,
alors que ses premiers consommateurs probables (les projets de test de ce dépôt,
et peut-être `FirstClassErrors.Testing` plus tard) vivent ici.

## Décision

La bibliothèque `Dummies` est publiée comme package NuGet propre, nommé
`Dummies`, hébergé dans ce dépôt comme projet autonome ne référençant aucun
projet FirstClassErrors — frontière gardée par un test d'architecture.

## Justification

* **Le nom ne doit pas restreindre l'audience.** La bibliothèque est un
  générateur générique de valeurs de test ; un nom `FirstClassErrors.Testing.*`
  la décrirait comme un outillage de gestion d'erreurs, plafonnerait son
  audience aux utilisateurs de FirstClassErrors et suggérerait une dépendance
  qui n'existe pas. Un identifiant de package étant permanent, ce choix devait
  être tranché avant la première publication, pas après.
* **L'identité tient à la frontière du package, pas à celle du dépôt.** Un
  identifiant autonome, son propre namespace et une règle de zéro référence
  livrent l'identité indépendante ; héberger les sources ici réutilise
  l'appareillage existant et garde la friction d'itération basse précisément
  quand l'API bouge le plus.
* **La frontière est vérifiée, pas espérée.** Un test d'architecture fait
  échouer tout build où `Dummies` gagnerait une référence FirstClassErrors : la
  promesse d'autonomie ne peut pas s'éroder silencieusement, et une extraction
  ultérieure vers son propre dépôt reste une opération mécanique.
* **La décision réalise le suivi d'ADR-0006 tel qu'anticipé.** L'utilitaire
  autonome et agnostique des erreurs que cet ADR envisageait existe désormais
  comme package de premier rang plutôt que comme moteur interne.

## Alternatives considérées

### Le nommer `FirstClassErrors.Testing.Dummies`

Considéré parce que la bibliothèque est née en scindant le moteur générique de
`FirstClassErrors.Testing`, et qu'un nom de famille hérite de l'audience de ce
package. Rejeté parce que le nom décrit mal le contenu (la bibliothèque ne
parle pas d'erreurs), plafonne l'audience visée et suggère un couplage que le
code interdit délibérément.

### Créer un dépôt séparé dès maintenant

Considéré parce qu'un produit autonome dans son propre dépôt est l'identité la
plus propre à long terme. Rejeté pour l'instant parce que cela duplique tout
l'appareillage de publication sans gain d'identité que la frontière du package
ne livre déjà, et ajoute une friction inter-dépôts au moment où l'API évolue le
plus vite. L'extraction reste peu coûteuse tant que la frontière de zéro
référence tient ; les déclencheurs de réexamen sont listés en actions de suivi.

### Étendre la façade `Any` de `FirstClassErrors.Testing` sur place

Considéré parce que cette façade existe et est publiée. Rejeté parce que cela
soude le moteur générique à la surface spécifique aux erreurs — l'inverse de
l'ambition d'autonomie — et que faire grandir une DSL de contraintes complète
dans un package de support de test dédié aux erreurs en déplacerait le centre
de gravité. `FirstClassErrors.Testing` garde sa façade inchangée.

## Conséquences

### Positives

* La bibliothèque porte une identité et une audience propres, indépendantes de
  FirstClassErrors, dès sa première release.
* Aucune infrastructure de publication n'est dupliquée ; le package bénéficie
  de la CI, du durcissement de packaging et des conventions existants.
* La frontière de zéro référence est vérifiée par la machine, et l'extraction
  vers un dépôt dédié reste une option mécanique et peu coûteuse.

### Négatives

* Un package publié de plus à maintenir depuis ce dépôt : son propre train de
  release, sa documentation, sa cadence de versions.
* Le nom du dépôt ne met pas le package en avant ; sa découvrabilité repose sur
  le package lui-même et sa documentation.
* La liste des scopes de commit grandit d'un élément (`dummies`), et les
  contributeurs doivent savoir qu'un projet de ce dépôt ne fait délibérément
  pas partie du graphe de dépendances FirstClassErrors.

### Risques

* **Érosion de la frontière** — un raccourci commode ajoute une référence
  FirstClassErrors. Atténué par le test d'architecture et par cet ADR qui
  consigne la règle.
* **Conflit de cadence** — le rythme de release de Dummies peut finir par se
  heurter aux trains du dépôt. Cette pression est un déclencheur d'extraction,
  pas une raison de coupler le package davantage.

## Actions de suivi

* Donner à `Dummies` son propre train de release dans l'outillage de packaging
  avant sa première publication ; d'ici là, aucune release ne le publie.
* Extraire vers un dépôt dédié (en conservant l'identifiant du package) quand
  un déclencheur se présente : arrivée de contributeurs externes, cadence de
  release divergente, ou flux d'issues propre au package.
* Écrire la documentation utilisateur (anglais et français) une fois la surface
  V1 stabilisée.
* Décider séparément si `FirstClassErrors.Testing` rebase plus tard son moteur
  interne de valeurs sur `Dummies` ; rien dans cette décision ne l'impose.

## Références

* ADR-0006 — Fournir les valeurs de test arbitraires depuis une source unique à
  graine (le suivi que cette décision réalise).
* Le test d'architecture gardant la frontière, dans `Dummies.UnitTests`.
