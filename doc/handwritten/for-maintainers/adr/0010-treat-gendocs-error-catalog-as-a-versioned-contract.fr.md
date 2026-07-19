# ADR-0010 | Traiter le catalogue d'erreurs de GenDoc comme un contrat versionné

🌍 🇬🇧 [English](0010-treat-gendocs-error-catalog-as-a-versioned-contract.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-17
**Décideurs :** Reefact

## Contexte

L'ADR-0009 a fait des échecs propres à GenDoc des erreurs de première classe avec
des codes `GENDOC_` stables et un contexte typé. Ces identités sont émises par
`fce` et peuvent être utilisées par la CI, les intégrations et le support.

GenDoc est intégré au train de release `cli` plutôt que publié indépendamment.
Supprimer un code ou supprimer/retyper une clé de contexte modifie donc la
compatibilité de ce que le train émet. Le dépôt sait déjà snapshotter et comparer
un catalogue, mais le catalogue propre à GenDoc n'était pas relié à la version qui
le publie.

## Décision

Une rupture du catalogue d'erreurs propre à GenDoc, mesurée par rapport à la baseline de la dernière release, exige un changement majeur du train `cli` et est imposée au moment de la release.

## Justification

Des identités d'erreur stables constituent un contrat publié et doivent respecter
la même promesse de versionnement sémantique que l'outil qui les émet. Le bon point
de contrôle est la release : une rupture est légitime pendant le développement,
mais pas sa publication sous une version apparemment compatible.

La comparaison doit rester ancrée sur la dernière release réussie et non sur un
snapshot de développement mouvant. La classification existante du diff reste
l'unique définition d'une rupture de catalogue. Le cycle de la baseline, l'ordre
des workflows et la récupération sont maintenus dans la
[spécification du contrat GenDoc](../specifications/gendoc-catalog-contract.fr.md).

## Alternatives envisagées

### S'appuyer sur les Conventional Commits et la revue

Envisagé car les marqueurs de rupture existent déjà. Rejeté car une rupture de
catalogue peut être un effet involontaire qu'aucun auteur n'a signalé.

### Bloquer chaque pull request

Envisagé pour un retour plus précoce. Rejeté car une rupture intentionnelle est
valide avant la release et ne doit pas imposer prématurément une version.

### Donner à GenDoc son propre package et train

Envisagé pour un versionnement indépendant. Rejeté car GenDoc n'a pas de
consommateur autonome et est déjà une partie interne de `fce`.

## Conséquences

### Positives

* Une rupture ne peut pas être livrée sous une version `cli` non majeure.
* Les reviewers voient l'impact de compatibilité en attente avant la release.
* La documentation vivante est ancrée sur un contrat publié explicite.

### Négatives

* La release dépend d'une baseline commitée et d'un diff de catalogue.
* Avancer la baseline après publication exige une écriture automatisée sur `main`.

### Risques

* Un échec de mise à jour de la baseline après publication la laisse obsolète.
  Atténuation : la procédure de récupération est explicite et ne doit pas
  republier le package déjà livré.

## Actions de suivi

* Garder le câblage des workflows et la récupération opérationnelle alignés avec
  la spécification du contrat.

## Références

* [Spécification du contrat de catalogue GenDoc](../specifications/gendoc-catalog-contract.fr.md).
* [Référence de versionnement des catalogues](../../for-users/CatalogVersioningReference.fr.md).
* ADR-0009 — échecs GenDoc de première classe.
* ADR-0002 — modèle de release de l'outillage intégré.
* Issue #167.
