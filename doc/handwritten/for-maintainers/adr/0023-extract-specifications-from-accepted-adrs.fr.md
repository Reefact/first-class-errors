# ADR-0023 | Extraire les spécifications des ADR acceptés

🌍 🇬🇧 [English](0023-extract-specifications-from-accepted-adrs.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Les ADR acceptés sont des archives historiques immuables : une décision modifiée
est représentée par un successeur, pas par la réécriture de l'original. Le corpus
contient néanmoins des mécanismes d'implémentation, configurations exactes,
procédures de maintenance et parcours d'API qui évoluent plus souvent que les
décisions qu'ils expliquent.

Ce mélange contredit la règle propre au dépôt : si l'implémentation change alors
que la décision tient, l'ADR ne devrait pas nécessiter d'édition. Il rend aussi la
maintenance ambiguë, car un lecteur ne sait pas si un flag, une forme de méthode,
un budget de tirage ou une étape de workflow constitue la décision durable ou la
seule implémentation courante.

Une revue complète de la base d'ADR a identifié un ensemble borné de documents
acceptés normalisables sans changer leur décision, justification, alternatives,
conséquences ni sens historique. Reefact autorise explicitement cette
normalisation éditoriale ainsi que la correction, dans la même pull request, des
statuts `Proposed` dont les décisions sont déjà implémentées.

## Décision

Le corpus d'ADR acceptés peut subir une migration éditoriale unique qui extrait les spécifications d'implémentation vers des références mainteneur évolutives, ajoute des relations non sémantiques et corrige les statuts déjà implémentés sans modifier matériellement aucune décision consignée ni son raisonnement.

## Justification

La migration restaure la séparation voulue : les ADR restent des explications
stables des choix, tandis que les spécifications deviennent la source maintenue
des mécanismes courants d'API, workflows, algorithmes et compatibilité. Créer des
ADR de remplacement présenterait à tort une correction éditoriale comme une
nouvelle architecture ; copier les mécanismes sans les retirer conserverait
l'ambiguïté.

L'exception est volontairement unique et bornée. Elle ne s'applique qu'au corpus
revu dans la pull request de migration, exige l'alignement anglais/français et se
vérifie comme un diff documentaire pur. Les futurs ADR acceptés redeviennent
immuables selon la règle ordinaire.

La correction d'un statut `Proposed` n'est incluse que lorsque la décision
correspondante est entièrement implémentée sur `main` et que le mainteneur a
explicitement accepté la régularisation. Aucun agent ne déduit une acceptation de
la seule implémentation.

## Alternatives envisagées

### Laisser le corpus inchangé

Envisagé parce que cela préserve l'immuabilité littérale. Rejeté parce que les
fuites de spécification connues resteraient définitivement dans les archives.

### Remplacer chaque ADR concerné

Envisagé parce que c'est le mécanisme normal lorsqu'une décision acceptée change.
Rejeté parce qu'aucune décision ne change ; des successeurs feraient croire à tort
à une nouvelle architecture et fragmenteraient un choix pour une correction
purement éditoriale.

### Copier les mécanismes dans des références sans alléger les ADR

Envisagé parce que cela ajoute une documentation évolutive sans toucher aux ADR
acceptés. Rejeté parce que le texte dupliqué créerait immédiatement deux sources
de vérité concurrentes.

## Conséquences

### Positives

* Les ADR acceptés deviennent courts et stables face aux refactorings.
* Les mécanismes courants disposent de références mainteneur bilingues et évolutives.
* Les relations de précision ou de réexamen entre ADR deviennent visibles.
* Les statuts reflètent les décisions acceptées et déjà implémentées.

### Négatives

* La migration produit un diff documentaire important sur des fichiers historiques.
* Les reviewers doivent vérifier la préservation sémantique plutôt que s'appuyer
  sur la règle habituelle d'absence de modification.

### Risques

* Une réécriture éditoriale pourrait altérer une décision ou sa justification.
  Atténuation : la pull request lie chaque spécification extraite, conserve les
  phrases de décision et fait l'objet d'une revue exceptionnelle.
* L'exception pourrait servir de précédent à de futurs édits sur place.
  Atténuation : cet ADR limite l'autorisation à la pull request de migration ; les
  changements suivants reprennent le processus normal de remplacement.

## Actions de suivi

* Maintenir l'index des spécifications sous
  [`../specifications/`](../specifications/README.fr.md).
* Réappliquer la règle ordinaire d'immuabilité après le merge de cette migration.

## Références

* [Format et index des ADR](README.md).
* [Spécifications mainteneur](../specifications/README.fr.md).
