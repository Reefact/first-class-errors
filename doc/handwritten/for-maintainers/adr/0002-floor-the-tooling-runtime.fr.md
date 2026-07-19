# ADR-0002 | Fixer le plancher du runtime de l'outillage à la plus ancienne LTS prise en charge

🌍 🇬🇧 [English](0002-floor-the-tooling-runtime.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-13
**Décideurs :** Reefact

## Contexte

FirstClassErrors livre à la fois des bibliothèques largement consommables et des outils exécutables. Les bibliothèques ciblent `netstandard2.0` ; l'outil en ligne de commande, le générateur de documentation et le worker sont des applications dépendantes d'un framework dont le TFM constitue un runtime minimum strict.

L'outillage ciblait auparavant la dernière version de .NET. Cela empêchait les consommateurs utilisant une LTS plus ancienne mais encore prise en charge d'exécuter `fce`, même lorsque leur application pouvait consommer les bibliothèques.

Le worker charge également les assemblies des consommateurs. Son processus doit donc pouvoir s'exécuter sur un runtime compatible avec l'assembly cible qu'il inspecte. Il s'agit d'une question de sélection du runtime, pas d'une raison de publier un binaire par version de .NET.

Au moment de la décision, .NET 8 était la plus ancienne LTS prise en charge et correspondait au plancher de l'hôte de l'analyseur. Le plancher distinct de prise en charge de .NET Framework par la bibliothèque est défini par l'[ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.fr.md), qui raffine la mention incidente auparavant présente ici.

## Décision

L'outillage (`FirstClassErrors.Cli`, `FirstClassErrors.GenDoc` et `FirstClassErrors.GenDoc.Worker`) cible uniquement **`net8.0`**, la plus ancienne LTS .NET prise en charge au moment de cette décision, et prend en charge les runtimes plus récents par roll-forward plutôt que par une matrice de frameworks cibles.

## Justification

Un build unique sur le plancher permet à tous les consommateurs de la plage supportée d'utiliser l'outillage sans créer une matrice à maintenir à chaque version. Il aligne la déclaration de compatibilité sur l'hôte de l'analyseur tout en évitant des reconstructions sans valeur fonctionnelle.

Le roll-forward est le bon mécanisme, car l'outillage doit s'exécuter sur les runtimes plus récents installés et le worker doit sélectionner un runtime capable de charger l'assembly cible. Publier plusieurs frameworks cibles ne supprimerait pas cette contrainte et créerait une maintenance continue des releases.

Le plancher reste vérifiable selon deux axes indépendants : la compilation empêche l'utilisation accidentelle d'API supérieures au framework cible, tandis que des vérifications d'exécution dédiées exercent l'outillage livré sur le plancher et sur les runtimes à venir.

Les politiques exactes de runtime, les jobs de CI, les réglages de projets et la procédure de maintenance sont documentés dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#plancher-dexécution-des-outils) et la [référence du workflow `ci`](../workflows/ci.fr.md).

## Alternatives envisagées

### Conserver l'outillage sur le dernier runtime

Envisagé car il s'agit de la configuration de projet la plus simple. Rejeté parce que le framework cible constitue un minimum strict et exclurait les consommateurs utilisant une LTS plus ancienne mais encore prise en charge.

### Multi-cibler l'outillage

Envisagé comme stratégie classique de compatibilité. Rejeté parce qu'un build sur le plancher atteint déjà les runtimes plus récents par roll-forward, tandis qu'une matrice ajoute une maintenance de release et ne résout pas le besoin du worker de charger des assemblies ciblant une version supérieure.

## Conséquences

### Positives

* Les consommateurs utilisant la plus ancienne LTS prise en charge ou un runtime plus récent peuvent exécuter l'outillage.
* Le dépôt livre un seul artefact d'outillage plutôt qu'une matrice par version.
* La compatibilité d'exécution est vérifiée sur le plancher et surveillée avant les nouvelles versions de .NET.

### Négatives

* L'outillage ne peut pas s'exécuter sur des runtimes antérieurs au plancher LTS choisi.
* Les politiques de sélection du runtime et les vérifications de compatibilité dédiées doivent être maintenues.

### Risques

* Un futur runtime pourrait modifier le roll-forward ou casser l'outillage. Mesure : exercer le plancher actuel en CI et le prochain runtime via le workflow canary.
* La déclaration de support pourrait devenir incohérente lorsque la LTS plancher arrive en fin de support. Mesure : remplacer cet ADR et mettre à jour ensemble la documentation de support de l'analyseur et de l'outillage.

## Actions de suivi

* Remplacer cet ADR lorsque le plancher du runtime de l'outillage change.
* Maintenir le canary sur la prochaine version de .NET.

## Références

* [Référence d'implémentation des ADR — Plancher d'exécution des outils](../specifications/adr-implementation-reference.fr.md#plancher-dexécution-des-outils)
* [Référence du workflow `ci`](../workflows/ci.fr.md)
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.fr.md) — la décision correspondante pour l'hôte de l'analyseur.
* [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.fr.md) — raffine le plancher .NET Framework de la bibliothèque et remplace la mention incidente de 4.6.1 auparavant présente dans cet ADR.
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
