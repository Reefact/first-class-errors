# ADR-0001 | Verrouiller le plancher Roslyn de l'analyseur

🌍 🇬🇧 [English](0001-lock-the-analyzer-roslyn-floor.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-10
**Décideurs :** Reefact

## Contexte

`FirstClassErrors.Analyzers` est intégré au package NuGet `FirstClassErrors` et chargé par le compilateur de chaque consommateur. La version de Roslyn contre laquelle l'analyseur est compilé devient donc la version minimale du compilateur capable de le charger.

Une mise à jour ordinaire de la dépendance Roslyn peut relever silencieusement ce minimum. La CI moderne et l'IDE du mainteneur ne révèlent pas la régression puisqu'ils satisfont déjà le nouveau plancher. Le dépôt a déjà subi cette dérive lorsque l'analyseur a fini par exiger Roslyn 5.6.

Le plus ancien hôte d'analyseur pris en charge est le SDK .NET 8.0.100 / Visual Studio 2022 17.8, qui embarque Roslyn 4.8. Une garantie complète de compatibilité doit couvrir à la fois les références de l'analyseur et l'artefact empaqueté réellement chargé par les consommateurs.

## Décision

Le plancher Roslyn de l'analyseur est fixé à **4.8.0**, la version de Roslyn du plus ancien hôte pris en charge, et protégé par des garde-fous indépendants lors du build, des tests, de l'empaquetage et de la gestion des dépendances.

## Justification

Le plancher découle directement de la promesse de compatibilité : une version supérieure exclurait silencieusement un hôte pris en charge, tandis qu'une version inférieure n'ajouterait aucun environnement supporté.

Un simple épinglage de version ne suffit pas, car la dérive des références, le chargement de l'analyseur et son emplacement dans le package sont des modes de panne distincts. Des garde-fous indépendants apportent une défense en profondeur et vérifient l'artefact réellement livré sur le plus ancien compilateur pris en charge, pas seulement dans un environnement de développement moderne.

Le coût de maintenance supplémentaire est justifié, car les échecs de chargement de l'analyseur restent autrement invisibles au mainteneur et n'apparaissent chez les consommateurs qu'après publication.

La réalisation technique actuelle et la procédure de relèvement du plancher sont documentées dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#plancher-de-compatibilité-de-lanalyseur) et la [référence du workflow `analyzers`](../workflows/analyzers.fr.md).

## Alternatives envisagées

### Suivre la version courante de Roslyn

Envisagé car il s'agit du chemin normal de maintenance des dépendances. Rejeté parce que chaque mise à jour relève silencieusement la version minimale du SDK et de l'IDE exigée des consommateurs.

### Épingler la version sans garde-fous supplémentaires

Envisagé comme correction minimale. Rejeté parce que cela ne prouve ni que l'analyseur empaqueté se charge sur l'hôte plancher, ni qu'une future mise à jour bien intentionnée ne réintroduira pas la régression.

### S'appuyer uniquement sur un test des références d'assembly

Envisagé car il est rapide et déterministe. Rejeté parce qu'il ne peut prouver ni que l'analyseur est empaqueté à l'emplacement attendu, ni que l'artefact livré se charge sur le plus ancien compilateur.

## Conséquences

### Positives

* Le compilateur minimal de l'analyseur ne peut plus dériver au gré de la maintenance courante des dépendances.
* La promesse de compatibilité est vérifiée sur l'artefact empaqueté et sur le plus ancien hôte pris en charge.
* Un relèvement du plancher reste une décision architecturale explicite.

### Négatives

* Plusieurs garde-fous complémentaires doivent être maintenus.
* Relever le plancher exige des modifications coordonnées du build, des tests, de l'empaquetage et de la documentation.

### Risques

* Un futur mainteneur pourrait supprimer un garde-fou dont l'utilité n'est pas évidente. Mesure : les mécanismes sont documentés dans les références d'implémentation et de workflow.
* Un garde-fou pourrait continuer à valider un ancien plancher après un changement volontaire. Mesure : tout changement de plancher doit remplacer cet ADR et suivre la procédure documentée.

## Actions de suivi

* Aucune action immédiate.
* Remplacer cet ADR lorsque le plancher Roslyn pris en charge change.

## Références

* [Référence d'implémentation des ADR — Plancher de compatibilité de l'analyseur](../specifications/adr-implementation-reference.fr.md#plancher-de-compatibilité-de-lanalyseur)
* [Référence du workflow `analyzers`](../workflows/analyzers.fr.md)
* [ADR-0002](0002-floor-the-tooling-runtime.fr.md) — la décision équivalente pour le runtime.
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
