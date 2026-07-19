# ADR-0010 | Traiter le catalogue d'erreurs de GenDoc comme un contrat versionné

🌍 🇬🇧 [English](0010-treat-gendocs-error-catalog-as-a-versioned-contract.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-17
**Décideurs :** Reefact

## Contexte

GenDoc expose des codes d'erreur first-class stables et un contexte typé que les consommateurs externes peuvent utiliser dans la CI, les intégrations et les outils de support.

GenDoc est livré dans le package en ligne de commande `fce` plutôt que sur un train de release indépendant. Supprimer ou modifier l'un de ses codes documentés ou de ses contrats de contexte constitue donc une modification de compatibilité du package `cli`.

Le dépôt sait déjà prendre un instantané d'un catalogue et classifier ses changements, mais le propre catalogue d'erreurs de l'outil n'était pas relié à la version sémantique publiée par le processus de release.

## Décision

Une modification cassante du propre catalogue d'erreurs de GenDoc exige une version majeure du train de release `cli` et est contrôlée lors de la publication de cette release.

## Justification

Le catalogue est un contrat publié, car les consommateurs dépendent d'identités d'erreur et de contextes stables. Une rupture du catalogue doit donc être signalée par la même promesse de versionnement sémantique que toute autre rupture observable.

Le moment de la release est le bon point de contrôle. Une rupture peut être légitime pendant le développement ; l'erreur consiste à la publier sous une version qui promet la compatibilité.

La comparaison doit rester ancrée sur le dernier catalogue effectivement livré plutôt que sur un instantané de développement mobile, afin que le numéro de version réponde à la question de ce qui a changé depuis la release précédente.

Réutiliser la classification existante du diff de catalogue évite de créer une seconde définition concurrente de la compatibilité.

L'emplacement exact de la baseline, le workflow de release, les commandes de mise à jour et la procédure de reprise sont documentés dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#compatibilité-du-catalogue-gendoc), la référence des workflows et la documentation du versionnement des catalogues.

## Alternatives envisagées

### S'appuyer sur les Conventional Commits et la discipline de revue

Envisagé car le dépôt enregistre déjà les ruptures intentionnelles. Rejeté parce qu'une rupture accidentelle du catalogue peut survenir sans être identifiée par l'auteur du commit, tandis que le catalogue généré fournit une mesure mécanique.

### Bloquer chaque pull request

Envisagé pour remonter les ruptures plus tôt. Rejeté parce qu'une rupture du catalogue est valide pendant le développement dès lors que la release finale porte la bonne version majeure.

### Publier GenDoc sur un train de release indépendant

Envisagé pour donner au catalogue sa propre version. Rejeté parce que GenDoc n'a pas de consommateur autonome et est volontairement livré dans `fce` ; la machinerie supplémentaire de release n'apporterait pas de bénéfice utilisateur correspondant.

## Conséquences

### Positives

* Une rupture du catalogue GenDoc ne peut pas être livrée sous une version `cli` qui semble compatible.
* Les relecteurs peuvent voir l'impact de compatibilité en attente avant la release.
* Le catalogue généré devient un contrat explicite de release plutôt qu'un instantané au mieux.

### Négatives

* La release `cli` dépend désormais d'une baseline de catalogue valide et d'un contrôle de compatibilité.
* Les mainteneurs doivent comprendre qu'accepter une rupture exige une version majeure ou l'abandon de la modification.

### Risques

* Une baseline obsolète ou avancée incorrectement pourrait mal classifier une release. Mesure : conserver les mises à jour de baseline dans la procédure contrôlée de release et documenter la reprise lorsque la publication et l'avancement de la baseline divergent.

## Actions de suivi

* Maintenir la procédure de release et son chemin de reprise dans la référence du workflow plutôt que dans cet ADR.

## Références

* [Référence d'implémentation des ADR — Compatibilité du catalogue GenDoc](../specifications/adr-implementation-reference.fr.md#compatibilité-du-catalogue-gendoc)
* [Référence de versionnement des catalogues](../../for-users/CatalogVersioningReference.fr.md)
* [ADR-0009](0009-report-the-toolings-failures-as-first-class-errors.fr.md)
* [ADR-0002](0002-floor-the-tooling-runtime.fr.md)
* Issue #167.
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
