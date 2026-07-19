# ADR-0017 | Fournir un défaut d'options configurable à l'échelle de l'application

🌍 🇬🇧 [English](0017-provide-a-configurable-application-wide-default-for-the-binder-options.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

L'ADR-0012 a fixé les options d'un binder avant le début du binding et conservé un point d'entrée configuré explicite.

Les applications sans conteneur d'injection de dépendances peuvent néanmoins avoir besoin d'une politique unique de nommage et d'erreurs structurelles à l'échelle de l'application tout en utilisant le point d'entrée de binding simple. Répéter ou transmettre manuellement un point d'entrée configuré à chaque appel reste possible mais moins pratique.

Une valeur par défaut globale librement mutable introduirait une dérive à l'exécution et des interférences entre tests parallèles. `RequestBinderOptions` est lui-même immuable ; le risque restant est la réaffectation de la référence partagée après utilisation.

## Décision

`RequestBinderOptions.Default`, utilisé par le point d'entrée de binding simple, est configurable une seule fois pendant la composition de l'application puis devient immuable après sa première utilisation par un binding.

## Justification

Une valeur par défaut configurable offre aux hôtes sans injection de dépendances un moyen agnostique d'établir une politique applicative unique, tout en préservant le chemin explicite `Bind.WithOptions` pour la configuration injectée ou propre à un appel.

Geler la référence lors de la première utilisation limite l'état global à la phase de démarrage et empêche la dérive d'exécution rejetée par l'ADR-0012. L'objet partagé étant lui-même immuable, les consommateurs ne peuvent pas modifier une instance de configuration déjà utilisée.

Un override local aux tests et limité à leur scope préserve l'isolation parallèle sans rendre la valeur de production librement réinitialisable.

Cette décision réexamine volontairement une alternative rejetée par l'ADR-0012 avec des contraintes plus fortes ; elle ne modifie pas la décision de l'ADR-0012 selon laquelle chaque binder reçoit des options fixes avant le début du binding.

La sémantique exacte du gel, le comportement d'exception, le seam de test et l'interaction avec les points d'entrée sont documentés dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#contrats-dimplémentation-du-request-binder) et la documentation du Request Binder.

## Alternatives envisagées

### Conserver uniquement les points d'entrée configurés explicitement

Envisagé car cela évite tout état global au processus. Rejeté parce que les hôtes sans conteneur devraient transmettre la configuration à chaque appel de binding, même lorsqu'elle est unique à l'échelle de l'application.

### Utiliser une valeur par défaut globale librement réinitialisable

Envisagé car il s'agit du modèle global le plus simple. Rejeté parce qu'elle pourrait changer pendant le traitement des requêtes et rendrait l'isolation des tests dangereuse.

### Exiger l'injection de dépendances

Envisagé car elle est sûre pour les tests et idiomatique lorsqu'un conteneur existe. Rejeté comme mécanisme unique parce que la bibliothèque est agnostique de l'hôte et prend en charge les CLI, workers et petits outils sans DI.

## Conséquences

### Positives

* Les hôtes avec ou sans injection de dépendances peuvent configurer une politique applicative unique du binder.
* La valeur par défaut ne peut plus dériver après le début du binding.
* Les points d'entrée explicitement configurés restent disponibles et peuvent remplacer le défaut.

### Négatives

* La bibliothèque accepte une référence de configuration globale au processus.
* Lire le défaut trop tôt peut le figer avant la configuration applicative prévue.
* Les tests nécessitent un override dédié et scoped plutôt qu'une réinitialisation de l'état de production.

### Risques

* Un ordre d'initialisation caché peut faire échouer la configuration si un autre composant lie avant elle. Mesure : documenter l'ordre de démarrage et échouer bruyamment en cas d'affectation tardive.
* Les consommateurs peuvent utiliser le défaut global alors qu'une configuration explicite serait plus claire. Mesure : conserver `Bind.WithOptions` visible et le recommander pour les bibliothèques, les tests et les composition roots avec DI.

## Actions de suivi

* N'exposer un seam de test aux consommateurs que si la demande justifie son ajout dans un package de test dédié.
* Réexaminer le défaut global avant la version stable si l'usage réel montre que les points d'entrée configurés et réutilisables suffisent.

## Références

* [Référence d'implémentation des ADR — Contrats d'implémentation du Request Binder](../specifications/adr-implementation-reference.fr.md#contrats-dimplémentation-du-request-binder)
* [ADR-0012](0012-fix-the-binder-options-before-binding-begins.fr.md) — cet ADR réexamine une alternative rejetée tout en préservant des options fixes par binder.
* [ADR-0006](0006-supply-arbitrary-test-values-from-a-seedable-source.fr.md)
* Issue #181.
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
