# ADR-0012 | Fixer les options du binder avant le début de la liaison

🌍 🇬🇧 [English](0012-fix-the-binder-options-before-binding-begins.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

Le Request Binder résout les chemins d'arguments au moyen d'un `IArgumentNameProvider` porté par `RequestBinderOptions`.

La surface fluent précédente permettait de modifier les options après que certaines propriétés avaient déjà été liées. Chaque binding lisant les options actives à cet instant, une même enveloppe d'échec pouvait contenir des chemins produits par des politiques de nommage différentes.

Un consommateur dépend d'une politique unique pour faire correspondre chaque chemin d'erreur à l'entrée qu'il a envoyée. La documentation seule ne pouvait pas empêcher l'ordre d'appel invalide.

`RequestBinderOptions` ne porte aucun état propre à une requête et peut donc être configuré une fois puis réutilisé entre les requêtes.

## Décision

Les options d'un binder sont fixées à son point d'entrée avant qu'une source ne soit liée, et l'API publique ne permet pas de les modifier après le début du binding.

## Justification

Fixer les options avant l'existence du binder rend les enveloppes mélangeant plusieurs politiques impossibles à représenter plutôt que de seulement les détecter plus tard.

Conserver les options hors du binder spécifique à la requête reflète également leur véritable portée : une politique de nommage et d'erreurs structurelles est une configuration applicative, pas une donnée de requête, et un point d'entrée configuré peut être réutilisé en toute sécurité.

La décision porte sur le moment où les options deviennent fixes, pas sur l'existence éventuelle d'une valeur par défaut applicative. L'[ADR-0017](0017-provide-a-configurable-application-wide-default-for-the-binder-options.fr.md) réexamine plus tard l'alternative du défaut global avec des garde-fous supplémentaires de gel et d'isolation des tests, tout en préservant l'immutabilité au point d'entrée définie ici.

Le type exact du point d'entrée configuré, la forme fluent, l'héritage par les binders imbriqués et les exemples sont documentés dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#contrats-dimplémentation-du-request-binder) et le guide du Request Binder.

## Alternatives envisagées

### Verrouiller les options à l'exécution lors du premier binding

Envisagé pour préserver davantage la surface précédente. Rejeté parce que l'ordre invalide continuerait à compiler et n'échouerait qu'à l'exécution.

### Fournir uniquement une valeur par défaut globale au processus

Envisagé pour supprimer la configuration des points d'appel. Rejeté à l'époque parce qu'un défaut global non contraint pouvait dériver pendant l'exécution et fuiter entre tests parallèles. L'ADR-0017 adopte ensuite une forme contrainte, gelée à la première utilisation, sans modifier la décision de cet ADR.

### Conserver le setter d'instance mutable et renforcer la documentation

Envisagé pour minimiser les changements. Rejeté parce que la documentation ne peut pas rendre une enveloppe incohérente impossible.

## Conséquences

### Positives

* Chaque enveloppe d'échec utilise une politique unique de nommage et d'erreurs structurelles.
* Une configuration tardive invalide devient impossible par la forme publique.
* Les points d'entrée configurés peuvent être réutilisés entre les requêtes.

### Négatives

* La configuration possède un chemin d'entrée distinct que les consommateurs doivent apprendre.
* Le code existant utilisant un setter tardif doit déplacer la configuration avant le binding.

### Risques

* Un futur besoin d'options volontairement différentes dans un binder imbriqué ne rentrerait pas dans ce modèle. Mesure : exiger une nouvelle décision explicite plutôt que de réintroduire une mutation tardive.

## Actions de suivi

* Maintenir les recommandations d'injection de dépendances et de valeur par défaut applicative dans la documentation d'intégration et utilisateur.

## Références

* [Référence d'implémentation des ADR — Contrats d'implémentation du Request Binder](../specifications/adr-implementation-reference.fr.md#contrats-dimplémentation-du-request-binder)
* [ADR-0017](0017-provide-a-configurable-application-wide-default-for-the-binder-options.fr.md) — réexamine l'alternative du défaut global tout en préservant des options fixes par binder.
* [ADR-0006](0006-supply-arbitrary-test-values-from-a-seedable-source.fr.md)
* [ADR-0007](0007-name-the-binder-terminals-new-and-create.fr.md)
* Issue #145 et pull request #126.
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
