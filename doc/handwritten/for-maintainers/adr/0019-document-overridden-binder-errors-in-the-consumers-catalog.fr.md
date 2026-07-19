# ADR-0019 | Documenter les erreurs de binder surchargées dans le catalogue du consommateur

🌍 🇬🇧 [English](0019-document-overridden-binder-errors-in-the-consumers-catalog.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

L'ADR-0018 a rendu configurables les erreurs structurelles du Request Binder sous forme de définitions appartenant au consommateur et contenant les codes et messages effectifs.

Le générateur de documentation découvre les erreurs documentées dans les projets consommateurs explicitement inclus, pas dans la configuration d'exécution de packages référencés. Le binaire d'un package ne peut exposer que ses valeurs par défaut, alors qu'un consommateur qui les remplace possède les valeurs réellement émises.

Le sens des erreurs structurelles du binder est stable et appartient au package, mais leurs identités et messages effectifs peuvent appartenir au consommateur.

Le modèle de catalogue du dépôt documente les erreurs là où elles sont définies et évite les liens non typés par chaînes de caractères entre une erreur et sa documentation.

## Décision

Un consommateur qui remplace les définitions d'erreurs structurelles du Request Binder documente ces erreurs effectives dans son propre catalogue généré au moyen de points d'extension documentaires du binder vérifiés par le compilateur, plutôt que par découverte automatique des catalogues de packages référencés.

## Justification

Le consommateur possède les codes et messages effectifs ; son propre catalogue est donc le seul emplacement capable de décrire fidèlement ce que l'application émet à l'exécution.

Les points d'extension fournis par le binder permettent au consommateur de réutiliser la prose stable et de créer des erreurs représentatives avec le comportement du package, sans recopier les descriptions ni reconstruire manuellement leur forme.

Conserver l'entrée de catalogue dans le flux ordinaire `[ProvidesErrorsFor]` / `[DocumentedBy]` du consommateur évite un mécanisme spécial de découverte inter-packages, l'analyse de la fermeture des références, une politique de collisions et le risque de documenter des valeurs par défaut que l'application n'utilise plus.

Des liens exprimés en code et vérifiés par le compilateur préservent la position du dépôt contre les magic strings et restent refactorables par les outils habituels.

Les membres publics exacts, les suppressions d'analyseurs et les exemples consommateurs sont documentés dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#surfaces-publiques-uniquement-destinées-à-la-documentation) et la documentation du Request Binder.

## Alternatives envisagées

### Découvrir automatiquement les codes documentés des packages référencés

Envisagé pour supprimer le glue consommateur pour les valeurs par défaut du package. Rejeté parce que les binaires référencés ne peuvent révéler les surcharges du consommateur et pourraient donc documenter des codes que l'application n'émet pas.

### Relier la documentation via la configuration de build

Envisagé pour conserver le câblage hors du code. Rejeté parce que les noms de membres deviendraient des chaînes non vérifiées, peu navigables et fragiles au refactoring.

## Conséquences

### Positives

* Un consommateur documente exactement les erreurs du binder qu'il émet.
* La prose stable et la construction d'erreurs représentatives sont réutilisées plutôt que dupliquées.
* Le générateur et son modèle de découverte restent inchangés.
* Les liens documentaires restent vérifiés par le compilateur.

### Négatives

* Les consommateurs ajoutent une petite quantité de glue explicite dans leur catalogue.
* Le binder expose une surface publique limitée dont l'objectif principal est le support documentaire.

### Risques

* Les membres publics destinés à la documentation pourraient étendre inutilement l'API d'exécution. Mesure : conserver une surface minimale, stable et liée au contrat du catalogue ; réexaminer les alternatives par métadonnées ou côté générateur avant tout ajout.
* Un consommateur pourrait appeler un point d'exemple hors documentation. Mesure : les exemples sont des valeurs d'erreur inertes et ne modifient pas le comportement du binder.

## Actions de suivi

* Examiner tout futur membre public destiné à la documentation au regard de la règle de minimisation de la référence d'implémentation.

## Références

* [Référence d'implémentation des ADR — Surfaces publiques uniquement destinées à la documentation](../specifications/adr-implementation-reference.fr.md#surfaces-publiques-uniquement-destinées-à-la-documentation)
* [ADR-0018](0018-bundle-the-binders-structural-error-code-and-messages.fr.md)
* [ADR-0016](0016-make-the-binders-structural-error-codes-configurable.fr.md) — origine remplacée de la question différée.
* Issue #140 et analyseur FCE009.
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
