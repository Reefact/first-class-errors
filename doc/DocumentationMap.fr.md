# Carte de la documentation

🌍 **Langues :**  
🇬🇧 [English](./DocumentationMap.en.md) | 🇫🇷 Français (ce fichier)

La documentation de FirstClassErrors est organisée selon **l’intention du lecteur**, et non selon les namespaces d’implémentation.

Partez de la question à laquelle vous cherchez à répondre. Chaque page principale peut ensuite renvoyer vers des guides ou des références plus spécialisés.

## Je découvre la bibliothèque

Suivez ce parcours pour déterminer si FirstClassErrors correspond à votre application.

1. [Premiers pas](GettingStarted.fr.md) — installer la bibliothèque, créer une erreur et générer un premier catalogue d’erreurs lisible par des humains.
2. [Principes de conception](DesignPrinciples.fr.md) — comprendre pourquoi l’erreur est le modèle et pourquoi sa façon de voyager (exception ou valeur de retour) est un choix séparé.
3. [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md) — reconnaître les situations où la bibliothèque apporterait plus de formalisme que de valeur.
4. [Comparaison avec les bibliothèques de gestion d’erreurs](ComparisonWithOtherLibraries.fr.md) — comparer FirstClassErrors, ErrorOr et FluentResults à partir d’un même scénario concret.

## Je dois comprendre le modèle

Lisez ces pages avant de définir des conventions à l’échelle d’un projet.

- [Concepts fondamentaux](CoreConcepts.fr.md) — `Error`, factories, documentation, exceptions et `Outcome`.
- [Guide du contexte d’erreur](ErrorContext.fr.md) — faits structurés propres à une occurrence.
- [Cas d’usage](UsagePatterns.fr.md) — choisir entre exceptions, outcomes, erreurs de domaine et erreurs d’infrastructure.

Les pages principales renvoient vers les éventuels guides dédiés à la taxonomie ou à la composition disponibles dans la version courante de la documentation.

## J’écris une erreur

Utilisez ce parcours lors de l’ajout ou de la revue d’une erreur applicative.

1. [Guide d’écriture des erreurs](WritingErrorsGuide.fr.md) — code, titre, description, règle, diagnostics et exemples.
2. [Bonnes pratiques](BestPractices.fr.md) — checklist de projet et de pull request.
3. [Internationalisation](Internationalisation.fr.md) — localiser le contenu public et documentaire tout en gardant les identifiants stables invariants.
4. [Règles d’analyse](analyzers/README.fr.md) — comprendre les contrôles de compilation qui protègent le modèle et les liens documentaires.

## J’utilise les erreurs dans le code applicatif

- [Cas d’usage](UsagePatterns.fr.md) — choisir la bonne représentation selon la situation.
- [Guide du contexte d’erreur](ErrorContext.fr.md) — attacher des faits utiles, sûrs et propres à l’occurrence.
- [Guide des tests](Testing.fr.md) — vérifier les outcomes et les erreurs sans plomberie manuelle.
- [FAQ](FAQ.fr.md) — résoudre les questions de conception courantes et trouver le guide spécialisé pertinent.

## J’intègre la livraison et l’exploitation

1. [Intégration CI/CD et exploitation](OperationalIntegration.fr.md) — générer et publier le catalogue dans la chaîne de livraison.
2. [Versionnage du catalogue — vue d’ensemble et workflow](CatalogVersioning.fr.md) — comprendre snapshots, baseline et compatibilité.
3. [Versionnage du catalogue — référence des commandes](CatalogVersioningReference.fr.md) — retrouver les options exactes de la CLI et les codes de sortie.
4. [Versionnage du catalogue — intégration CI/CD](CatalogVersioningCI.fr.md) — mettre en place des contrôles de contrat en lecture seule dans les pipelines.

Les pages d’intégration opérationnelle renvoient vers les éventuels guides dédiés au logging disponibles dans la version courante de la documentation.

## J’étends le pipeline documentaire

- [Architecture du pipeline de documentation](ArchitectureOfTheDocumentationPipeline.fr.md) — comprendre le modèle de bout en bout et la responsabilité de chaque composant.
- [Écrire son propre renderer](WritingACustomRenderer.fr.md) — implémenter et enregistrer un nouveau format de sortie.
- [Internationalisation](Internationalisation.fr.md) — comprendre la frontière de culture entre extraction et rendu.

La page d’architecture renvoie vers l’éventuelle référence dédiée à l’extraction et à la découverte des projets disponible dans la version courante de la documentation.

## J’ai besoin d’une référence, pas d’un tutoriel

Utilisez ces pages lorsque vous connaissez déjà le modèle et recherchez un comportement exact.

- [Référence des commandes de versionnage](CatalogVersioningReference.fr.md)
- [Règles d’analyse](analyzers/README.fr.md)
- [FAQ](FAQ.fr.md)

## Ordre de lecture conseillé pour une équipe

Pour une équipe qui adopte FirstClassErrors, un ordre pratique est :

1. Premiers pas ;
2. Principes de conception ;
3. Concepts fondamentaux ;
4. Guide d’écriture des erreurs ;
5. Cas d’usage ;
6. Guide des tests ;
7. Intégration CI/CD et exploitation ;
8. Versionnage du catalogue.

Après ce socle commun, les spécialistes peuvent lire les contenus d’architecture, de renderer, d’internationalisation, de logging, d’analyseurs et de référence CLI utiles à leur travail.

## Garder une seule source de vérité

Évitez de recopier de longues explications entre les conventions du projet et cette documentation.

Les règles propres au projet devraient exprimer la décision locale et renvoyer vers le guide correspondant. Par exemple :

```text
Les erreurs applicatives doivent être créées via des factories nommées.
Voir le Guide d’écriture des erreurs et les Bonnes pratiques.
```

Les conventions locales restent ainsi courtes, tandis que la documentation de la bibliothèque peut évoluer sans créer plusieurs explications contradictoires.

---

<div align="center">
<a href="README.fr.md">← README du projet</a> · <a href="GettingStarted.fr.md">Commencer par Premiers pas →</a>
</div>

---