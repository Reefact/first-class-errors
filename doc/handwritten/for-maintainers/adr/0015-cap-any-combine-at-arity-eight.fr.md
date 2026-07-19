# ADR-0015 | Plafonner Any.Combine à l'arité huit

🌍 🇬🇧 [English](0015-cap-any-combine-at-arity-eight.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Dummies compose des générateurs de types différents en objets plus larges au moyen de `Any.Combine`, en préservant la validation du domaine par les constructeurs sans recourir à la réflexion.

C# ne dispose pas de génériques variadiques hétérogènes ; chaque arité supportée exige donc une surcharge publique distincte. Des arités trop faibles imposent des compositions imbriquées ou des tuples positionnels pour les constructeurs plus larges, tandis qu'une surface illimitée créerait une API et une documentation répétitives pour une valeur décroissante.

Des constructeurs très larges peuvent également signaler l'absence de concepts intermédiaires dans le domaine.

## Décision

`Any.Combine` fournit des surcharges hétérogènes plates de l'arité deux à l'arité huit et s'arrête volontairement à ce seuil.

## Justification

Un appel plat avec des paramètres de lambda nommés est nettement plus lisible qu'une composition imbriquée ou l'accès positionnel à un tuple pour les tailles d'objets courantes dans le code métier.

Huit est un plafond pragmatique de confort, pas une propriété mathématique du DDD. Il couvre les cas visés de construction d'objets larges tout en maintenant une surface manuelle bornée et en laissant les constructeurs encore plus larges jouer leur rôle de signal de conception.

Les avertissements de nombre de paramètres sur les plus grandes surcharges constituent un compromis local explicite, pas un relâchement général des règles de qualité du dépôt.

Les signatures exactes, la documentation et les suppressions d'analyseurs sont des détails d'implémentation décrits dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#contrats-de-génération-de-dummies) et la référence d'API de Dummies.

## Alternatives envisagées

### Conserver uniquement les plus petites surcharges

Envisagé pour minimiser la surface d'API. Rejeté parce que les compositions plus larges deviennent nettement moins lisibles avec des lambdas imbriquées ou des membres positionnels de tuples.

### Utiliser un builder fluent accumulant un tuple

Envisagé pour éviter une surcharge par arité. Rejeté parce que cela déplace la même complexité dans le builder et continue d'exposer une structure positionnelle au point d'appel.

### Étendre jusqu'à l'arité maximale de `Func`

Envisagé pour la complétude. Rejeté parce que le coût de maintenance et la normalisation de constructeurs extrêmement larges dépassent le gain marginal de confort.

### Accepter uniquement des générateurs homogènes via `params`

Envisagé car cette forme est naturellement variadique. Rejeté parce qu'elle ne couvre pas les paramètres de constructeur de types différents pour lesquels `Combine` existe.

## Conséquences

### Positives

* Les objets larges courants se composent en un appel lisible et sans réflexion.
* L'API de confort reste volontairement bornée.
* Les constructions extrêmement larges restent visibles comme problème potentiel de conception.

### Négatives

* Plusieurs surcharges maintenues à la main font partie de la surface publique.
* Les plus grandes surcharges exigent des suppressions localisées d'analyseurs.
* Le plafond est heuristique et peut ne pas convenir à tous les domaines.

### Risques

* Un besoin légitime récurrent au-delà de l'arité huit peut apparaître. Mesure : des arités supérieures peuvent être ajoutées de manière compatible par une nouvelle décision si des faits montrent que le plafond actuel est trop bas.

## Actions de suivi

* Rendre explicite la plage d'arités supportée dans la documentation de Dummies.
* Étudier séparément une composition variadique homogène si un cas réel apparaît.

## Références

* [Référence d'implémentation des ADR — Contrats de génération de Dummies](../specifications/adr-implementation-reference.fr.md#contrats-de-génération-de-dummies)
* [ADR-0011](0011-host-dummies-as-a-standalone-package.fr.md)
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
