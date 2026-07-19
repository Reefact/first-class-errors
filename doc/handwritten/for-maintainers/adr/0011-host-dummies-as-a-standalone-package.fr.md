# ADR-0011 | Héberger Dummies comme package autonome dans ce dépôt

🌍 🇬🇧 [English](0011-host-dummies-as-a-standalone-package.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Le moteur générique de valeurs arbitraires anticipé par l'ADR-0006 sert les tests orientés domaine en général, pas spécifiquement la gestion des erreurs.

Une bibliothèque autonome nommée `Dummies` fournit désormais des générateurs typés portant leurs contraintes, sans connaissance de FirstClassErrors. Son public visé dépasse les consommateurs du package principal de ce dépôt.

L'identité d'un package est coûteuse à renommer après adoption, tandis que ce dépôt fournit déjà la CI, l'empaquetage, les releases, le SBOM, SourceLink et la gouvernance nécessaires à une publication sûre.

L'API de la bibliothèque est appelée à évoluer rapidement pendant ses premières itérations et ses premiers consommateurs sont présents dans ce dépôt.

## Décision

`Dummies` est livré comme package NuGet indépendant nommé `Dummies`, hébergé dans ce dépôt sous la forme d'un projet autonome qui ne doit référencer aucun projet FirstClassErrors.

## Justification

Le nom du package reflète la portée réelle de la bibliothèque et évite de suggérer une dépendance à la gestion d'erreurs qui n'existe pas.

La colocalisation dans le dépôt réutilise une infrastructure de livraison mature et maintient un coût d'itération faible, tandis que la frontière du package, le namespace et la règle de dépendance préservent une identité produit distincte.

La règle d'absence de référence rend l'indépendance vérifiable et conserve une future extraction vers un dépôt distinct comme opération mécanique plutôt qu'architecturale.

Les mécanismes actuels du train de release et des tests d'architecture sont documentés dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#contrats-de-génération-de-dummies) et la documentation d'empaquetage du dépôt.

## Alternatives envisagées

### Le nommer comme une extension de FirstClassErrors.Testing

Envisagé parce que le moteur a été conçu à proximité de ce package. Rejeté parce que ce nom limiterait le public, décrirait mal la bibliothèque et suggérerait une dépendance interdite par l'architecture.

### Créer immédiatement un dépôt séparé

Envisagé car cela donne la séparation organisationnelle la plus forte. Rejeté parce que la frontière du package fournit déjà l'identité, tandis qu'un nouveau dépôt dupliquerait l'infrastructure de livraison pendant la période où l'API évolue le plus vite.

### Étendre la façade existante de FirstClassErrors.Testing

Envisagé parce qu'elle est déjà publiée. Rejeté parce que cela couplerait un DSL générique de génération à un package spécifique aux erreurs et empêcherait le public indépendant recherché.

## Conséquences

### Positives

* Dummies possède une identité de package et un public indépendants dès sa première release.
* L'infrastructure de livraison est réutilisée plutôt que dupliquée.
* La frontière de dépendance est vérifiable et une future extraction reste peu coûteuse.

### Négatives

* Le dépôt maintient un package, un train de release et une documentation supplémentaires.
* Les contributeurs doivent comprendre que ce projet est volontairement extérieur au graphe de dépendances FirstClassErrors.

### Risques

* La frontière pourrait s'éroder par l'ajout opportuniste d'une référence de projet. Mesure : imposer la règle par des tests d'architecture.
* Le rythme de release du package pourrait diverger de celui du dépôt. Mesure : considérer les conflits récurrents de cadence, l'arrivée de contributeurs indépendants ou un flux d'issues propre comme déclencheurs d'extraction.

## Actions de suivi

* Réexaminer l'extraction vers un dépôt séparé lorsque le package développe sa propre gouvernance ou une pression de release indépendante.
* Décider séparément si FirstClassErrors.Testing doit consommer Dummies en interne.

## Références

* [Référence d'implémentation des ADR — Contrats de génération de Dummies](../specifications/adr-implementation-reference.fr.md#contrats-de-génération-de-dummies)
* [ADR-0006](0006-supply-arbitrary-test-values-from-a-seedable-source.fr.md)
* Tests d'architecture dans `Dummies.UnitTests`.
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
