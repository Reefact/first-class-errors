# ADR-0011 | Héberger Dummies comme package autonome dans ce dépôt

🌍 🇬🇧 [English](0011-host-dummies-as-a-standalone-package.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
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
aucune dépendance. Son audience visée dépasse les utilisateurs de FirstClassErrors.

Deux faits contraignent où et sous quel nom elle est publiée :

* Un identifiant de package NuGet est de fait permanent : le renommer après
  adoption signifie publier un nouveau package et imposer une migration aux
  consommateurs.
* Ce dépôt porte déjà l'appareillage de publication qu'un package publié
  requiert — CI avec cliquet zéro warning, SBOM embarqué, SourceLink, trains de
  release pilotés par tag et sélectionnés par liste explicite de projets,
  conventions de commit, et cette base d'ADR. Un dépôt séparé devrait tout dupliquer.

L'API de la bibliothèque évoluera le plus vite dans ses premières itérations,
alors que ses premiers consommateurs probables vivent ici.

## Décision

La bibliothèque `Dummies` est publiée comme package NuGet propre, nommé `Dummies`,
hébergé dans ce dépôt comme projet autonome ne référençant aucun projet
FirstClassErrors — frontière gardée par un test d'architecture.

## Justification

* **Le nom ne doit pas restreindre l'audience.** La bibliothèque est un générateur
  générique de valeurs de test ; un nom `FirstClassErrors.Testing.*` la décrirait
  comme un outillage de gestion d'erreurs et suggérerait une dépendance inexistante.
* **L'identité tient à la frontière du package, pas à celle du dépôt.** Un
  identifiant autonome, son namespace et la règle de zéro référence livrent
  l'identité indépendante tout en réutilisant l'appareillage existant.
* **La frontière est vérifiée, pas espérée.** Un test d'architecture fait échouer
  tout build où `Dummies` gagnerait une référence FirstClassErrors.
* **La décision réalise le suivi d'ADR-0006.** L'utilitaire autonome anticipé
  existe comme package de premier rang.

## Alternatives considérées

### Le nommer `FirstClassErrors.Testing.Dummies`

Rejeté parce que le nom décrit mal le contenu, plafonne l'audience et suggère un
couplage interdit.

### Créer un dépôt séparé dès maintenant

Rejeté pour l'instant parce que cela duplique l'appareillage de publication sans
gain d'identité que la frontière du package ne fournisse déjà.

### Étendre la façade `Any` de `FirstClassErrors.Testing` sur place

Rejeté parce que cela soude le moteur générique à la surface spécifique aux erreurs
et déplace le centre de gravité du package existant.

## Conséquences

### Positives

* La bibliothèque porte une identité et une audience propres.
* Aucune infrastructure de publication n'est dupliquée.
* La frontière de zéro référence est vérifiée par la machine.

### Négatives

* Un package publié de plus possède son train, sa documentation et sa cadence.
* Le nom du dépôt ne met pas directement le package en avant.
* Le scope de commit `dummies` et sa frontière doivent être connus des contributeurs.

### Risques

* **Érosion de la frontière** — atténuée par le test d'architecture.
* **Conflit de cadence** — considéré comme déclencheur d'une extraction future.

## Actions de suivi

* Maintenir le train de release propre à Dummies.
* Extraire vers un dépôt dédié si les contributeurs, la cadence ou le flux d'issues
  deviennent indépendants.
* Décider séparément d'un éventuel rebasage de `FirstClassErrors.Testing` sur Dummies.

## Références

* ADR-0006 — source arbitraire à graine.
* Test d'architecture dans `Dummies.UnitTests`.
* [Spécification de génération Dummies](../specifications/dummies-generation.fr.md).
