# ADR-0002 | Fixer le floor du runtime de l'outillage à la plus ancienne LTS supportée

🌍 🇬🇧 [English](0002-floor-the-tooling-runtime.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-13
**Décideurs :** Reefact

## Contexte

Les bibliothèques livrées sont des assemblies `netstandard2.0`, tandis que `fce`,
GenDoc et son worker sont des applications exécutables dépendantes du framework.
Le TFM d'une application exécutable est un minimum strict : le roll-forward peut
monter vers un runtime plus récent, mais ne peut pas faire exécuter une cible
récente sur un runtime plus ancien.

L'outillage ciblait auparavant le dernier runtime, empêchant un consommateur sur
la plus ancienne LTS supportée d'exécuter le générateur alors qu'il pouvait
utiliser les bibliothèques. Le worker charge aussi l'assembly cible dans son propre
processus et doit donc s'exécuter sur un runtime installé capable de la charger.

.NET 8 est la plus ancienne LTS supportée au moment de la décision et correspond
au floor d'hôte de l'analyzer défini par l'ADR-0001. Le floor .NET Framework
pratique des bibliothèques est défini séparément à 4.7.2 par l'ADR-0022.

## Décision

L'outillage (`FirstClassErrors.Cli`, `FirstClassErrors.GenDoc` et `FirstClassErrors.GenDoc.Worker`) cible uniquement **`net8.0`**, la plus ancienne LTS supportée, et atteint les runtimes plus récents par roll-forward plutôt que par une matrice de TFM.

## Justification

Un unique artefact au floor offre la portée supportée la plus large sans ajouter
une cible à chaque release .NET. Il aligne le support de l'outillage et de
l'analyzer sur .NET 8 tout en permettant au worker de choisir un runtime capable
de charger l'assembly cible du consommateur.

Les politiques runtime, l'exécution CI sur le floor, le canary preview et les
procédures de maintenance sont des mécanismes consignés dans la
[spécification de compatibilité](../specifications/platform-compatibility.fr.md)
et la [référence du workflow `ci`](../workflows/ci.fr.md). Ils peuvent évoluer
sans changer la décision de floor unique avec roll-forward.

## Alternatives envisagées

### Garder l'outillage sur le dernier runtime

Envisagé parce que cela n'exige aucun réglage de roll-forward. Rejeté car cela
exclut les consommateurs dont le runtime le plus récent est la plus ancienne LTS
supportée.

### Multi-cibler l'outillage

Envisagé comme matrice de compatibilité conventionnelle. Rejeté car un build au
floor monte déjà vers les runtimes plus récents, tandis qu'une matrice ajoute de
la maintenance et ne résout pas le chargement d'une cible supérieure par le worker.

## Conséquences

### Positives

* Les consommateurs sur .NET 8 ou plus récent peuvent exécuter `fce`.
* L'outillage livre un seul TFM au lieu d'une matrice par release.
* Une nouvelle majeure .NET ne requiert normalement ni rebuild ni republication.
* La frontière peut être testée sur le véritable runtime du floor.

### Négatives

* L'outillage ne peut pas s'exécuter si le runtime le plus récent précède .NET 8.
* Le roll-forward doit être configuré et testé délibérément.

### Risques

* Un futur runtime peut régresser sur le roll-forward ou le chargement d'assembly.
  Atténuation : les exécutions floor et preview décrites par la spécification.

## Actions de suivi

* Lorsque la LTS du floor atteint son EOL, décider d'un successeur dans un nouvel
  ADR et mettre à jour la spécification, les tests runtime et la documentation.

## Références

* **Précisé par l'[ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.fr.md) :**
  le floor .NET Framework des bibliothèques est 4.7.2 ; cet ADR ne gouverne que
  le runtime de l'outillage.
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.fr.md) — floor d'hôte de l'analyzer.
* [Spécification de compatibilité](../specifications/platform-compatibility.fr.md).
* [Référence du workflow `ci`](../workflows/ci.fr.md).
