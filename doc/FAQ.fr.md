# FAQ

🌍 **Langues :**  
🇬🇧 [English](./FAQ.en.md) | 🇫🇷 Français (ce fichier)

## Pourquoi ne pas simplement utiliser des exceptions normales ?

C’est possible. FirstClassErrors utilise toujours les exceptions standard .NET comme mécanisme de signalement et de propagation des défaillances.

La bibliothèque enrichit l’`Error` portée par l’exception avec un code stable, du contexte structuré, des diagnostics et une documentation liée. Voir [Concepts fondamentaux](CoreConcepts.fr.md).

## Pourquoi ne pas utiliser `Result<T, string>` ?

Une chaîne perd la structure. `Outcome<T>` transporte le même modèle riche d’`Error` que le chemin par exception : code, messages, contexte, diagnostics et identité documentaire.

Voir [Cas d’usage](UsagePatterns.fr.md) et [Comparaison avec les librairies de gestion d’erreurs](ComparisonWithOtherLibraries.fr.md).

## Est-ce trop lourd pour une application simple ?

Cela peut l’être. Les petits scripts, prototypes et systèmes sans besoin durable de support peuvent être mieux servis par des exceptions standard.

Voir [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md) pour les critères de décision.

## Pourquoi utiliser des factories plutôt que `new` ?

Une factory donne un nom à une situation d’erreur, centralise son code et ses messages, garde la construction hors du happy path et sert de point d’ancrage à la documentation vivante.

Voir [Premiers pas](GettingStarted.fr.md).

## Quelle différence entre la documentation d’erreur et les messages runtime ?

La documentation décrit la catégorie d’erreur stable : titre, sens, règle, hypothèses de diagnostic et exemples représentatifs.

Les messages runtime décrivent ou exposent une occurrence concrète :

- `ShortMessage` est le résumé public sûr ;
- `DetailedMessage` est un détail public optionnel et maîtrisé ;
- `DiagnosticMessage` est le détail interne destiné aux logs et au support.

Voir [Écrire la documentation d’une erreur](WritingErrorsGuide.fr.md) et [Écrire les messages d’une erreur](WritingErrorMessages.fr.md).

## Les diagnostics sont-ils des causes racines ?

Non. Ce sont des hypothèses plausibles et des points de départ pour l’investigation. Ils décrivent ce qui peut expliquer l’erreur sans prétendre à la certitude ni attribuer une faute.

## Les diagnostics doivent-ils contenir les procédures du support ?

Non. Gardez le ticketing, l’escalade et les consignes de contact d’équipe hors de la documentation applicative. Une piste d’analyse indique où chercher ; elle ne prescrit pas un workflow organisationnel.

Voir [Écrire la documentation d’une erreur](WritingErrorsGuide.fr.md#6-écrire-les-diagnostics-comme-des-hypothèses).

## Pourquoi écrire la documentation dans le code ?

Parce qu’elle est liée aux mêmes factories qui créent les erreurs. Elle peut être extraite automatiquement et évolue à côté du comportement qu’elle décrit, ce qui réduit la dérive.

Voir [Architecture du pipeline de documentation](ArchitectureOfTheDocumentationPipeline.fr.md).

## Quand ajouter un `ErrorContext` ?

Utilisez-le pour des faits sûrs, propres à une occurrence, qui améliorent réellement le diagnostic ou l’observabilité : identifiant métier, valeur mesurée ou borne pertinente.

N’y placez ni secret, ni payload volumineux, ni documentation générique, ni procédure opérationnelle. Voir [Contexte d’erreur](ErrorContext.fr.md).

## Quand utiliser `Outcome<T>` ?

Utilisez-le lorsque l’échec est une branche attendue du flux normal : validation, parsing, traitement par lots ou succès partiel.

Utilisez une exception lorsque l’échec doit interrompre l’opération à ce niveau. Les deux chemins peuvent porter la même `Error`, créée par la même factory.

Voir [Cas d’usage](UsagePatterns.fr.md).

## `Outcome<T>` conserve-t-il une stack trace ?

Aucune exception n’est créée ou levée tant que l’erreur est transportée comme donnée. Si l’échec est ensuite escaladé avec `GetResultOrThrow()` ou `error.ToException()`, l’exception et sa stack trace commencent à ce point d’escalade.

## Puis-je documenter toutes les exceptions ?

Non. Documentez les erreurs applicatives porteuses de sens : situations reconnues, règles, contraintes ou échecs de frontière qui bénéficient d’une identité stable et d’une explication partagée.

Les exceptions du framework, crashes accidentels et fautes d’implémentation bas niveau restent généralement des exceptions techniques. Voir [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md).

## FirstClassErrors est-il lié au Domain-Driven Design ?

Non. Son vocabulaire s’accorde bien avec le DDD et l’architecture hexagonale, mais tout système durable qui a besoin d’une sémantique d’erreur explicite, de supportabilité et de documentation vivante peut l’utiliser.

## Pourquoi une erreur de domaine ne contient-elle que des erreurs de domaine ?

Une `DomainError` affirme qu’une règle métier a été violée. Imbriquer une défaillance d’infrastructure à l’intérieur décrirait une panne technique comme une partie du vocabulaire métier.

Une erreur de port ou d’infrastructure peut contenir une `DomainError` lorsqu’un échec à la frontière est causé par un rejet métier, par exemple une requête entrante impossible à convertir en value object valide. Les deux faits sont ainsi conservés sans rendre le domaine dépendant de HTTP, de la messagerie ou d’une technologie d’adapter.

Voir la taxonomie et les règles d’imbrication dans [Concepts fondamentaux](CoreConcepts.fr.md#-taxonomie-des-erreurs).

---

<div align="center">
<a href="Internationalisation.fr.md">← Internationalisation</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a>
</div>

---