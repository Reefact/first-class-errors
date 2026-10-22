# ADR-0072 | Autoriser les sous-types d'erreur personnalisés

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0072-allow-custom-error-subtypes.md)

**Statut :** Accepté
**Proposé :** 2026-10-22
**Accepté :** 2026-10-22
**Décideurs :** Reefact

## Contexte

FirstClassErrors fournit une hiérarchie typée d'erreurs de domaine et d'infrastructure, avec les ports primaires et secondaires.
Les équipes de socle peuvent définir des catégories d'erreurs réutilisables, par exemple les erreurs de persistance, pour harmoniser le support et le diagnostic.

Les classes existantes disposent de méthodes de fabrique en deux étapes, mais leurs constructeurs ne sont pas accessibles aux classes dérivées définies dans d’autres projets .NET.
Une catégorie personnalisée ne peut donc pas étendre cette taxonomie sans remplacer ou dupliquer son fonctionnement.
Les consommateurs ne peuvent pas non plus créer une méthode de fabrique en deux étapes renvoyant un type spécialisé.

## Décision

FirstClassErrors permettra aux applications de dériver des catégories d'erreurs spécialisées de chaque type concret intégré, tout en conservant le modèle de construction à étapes des messages publics.

## Justification

Une hiérarchie extensible permet aux bibliothèques de socle de proposer des catégories utiles au support sans créer un type par code d'erreur.
Le maintien dans la hiérarchie existante préserve la direction architecturale, le caractère transitoire ou non de l’erreur et les diagnostics.
Les méthodes de fabrique en deux étapes conservent une façon uniforme de fournir les messages publics.

## Alternatives étudiées

### Garder la hiérarchie fermée

Cela limiterait l'API publique, mais empêcherait les équipes de socle de définir des familles d'erreurs par héritage.

### Utiliser exclusivement les codes d'erreur

Les codes identifient bien les situations particulières, mais ne permettent pas de regrouper les erreurs dans des catégories de types transverses.

## Conséquences

### Positives

* Les consommateurs peuvent ajouter des catégories sémantiques sans modifier FirstClassErrors.
* Les méthodes de fabrique et les codes d’erreur existants restent utilisables.
* Les types personnalisés bénéficient du même modèle de messages publics à deux étapes.

### Négatives

* L'API publique expose de nouveaux points d'extension et de nouvelles obligations de compatibilité.
* Les types dérivés doivent fournir leurs propres méthodes de fabrique.

### Risques

* Les classes dérivées peuvent s'écarter des conventions de construction ; les exemples et les tests doivent promouvoir les méthodes de fabrique en deux étapes.
* Une conversion en exception plus spécialisée nécessite une adaptation explicite.

## Actions de suivi

* Aucune en attente. Le modèle d'extension est documenté dans le [guide de la taxonomie des erreurs](../../for-users/ErrorTaxonomy.fr.md), et l'héritage côté consommateur ainsi que l'extraction GenDoc sont couverts par `CustomErrorTypesTests` dans `FirstClassErrors.GenDoc.UnitTests`, un assembly sans accès aux membres internes de la bibliothèque.

## Références

* [Hiérarchie des erreurs](../../../../FirstClassErrors/Error.cs)
* [Messages publics à étapes](../../../../FirstClassErrors/PublicMessageStage.cs)
