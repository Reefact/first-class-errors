# FAQ

🌍 **Langues :**  
🇬🇧 [English](./FAQ.en.md) | 🇫🇷 Français (ce fichier)

## Pourquoi ne pas simplement utiliser des exceptions normales ?

C’est possible. FirstClassErrors utilise toujours les exceptions standard .NET comme mécanisme de signalement et de propagation des défaillances.

La bibliothèque enrichit l’`Error` portée par l’exception avec un code stable, du contexte structuré, des diagnostics et une documentation liée. Voir [Concepts fondamentaux](CoreConcepts.fr.md).

## Pourquoi ne pas utiliser un `Result<T, Error>` générique ?

Vous pourriez. Transporter l’`Error` de la bibliothèque dans un type résultat générique — par exemple le `Result<T, E>` de [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions) — conserve la structure qu’un simple `Result<T, string>` perdrait.

`Outcome` est cette idée, spécialisée pour ce modèle d’erreur. Son côté échec est toujours une `Error` — et non un second paramètre de type à propager dans chaque signature — et sa petite API (`Then`, `Recover`, `Finally`) est nommée par intention plutôt qu’avec la mécanique de la programmation fonctionnelle (`Map`, `Bind`, `Match`). L’objectif : du code de domaine et des use cases qui se lisent comme un flux métier, pas comme de la tuyauterie de résultat générique.

Voir [Cas d’usage](UsagePatterns.fr.md) et [Comparaison avec les bibliothèques de gestion d’erreurs](ComparisonWithOtherLibraries.fr.md).

## Est-ce trop lourd pour une application simple ?

Cela peut l’être. Les petits scripts, prototypes et systèmes sans besoin durable de support peuvent être mieux servis par des exceptions standard.

Voir [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md) pour les critères de décision.

## Pourquoi utiliser des factories plutôt que `new` ?

Une factory donne un nom à une situation d’erreur, centralise son code et ses messages, garde la construction hors du happy path et sert de point d’ancrage à la documentation vivante.

Voir [Premiers pas](GettingStarted.fr.md).

## Quelle différence entre la documentation d’erreur et les messages runtime ?

La documentation est la partie d’une erreur qui ne dépend d’aucune occurrence particulière : elle est identique pour chaque instance et ne change jamais à l’exécution. Elle sert à *comprendre* l’erreur — titre, sens, règle, hypothèses de diagnostic et exemples représentatifs.

Les messages runtime sont portés par l’instance d’erreur elle-même et servent à *investiguer* cette occurrence :

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

## Dois-je documenter toutes les erreurs ?

Oui. Toute erreur que vous modélisez est destinée à être documentée : une erreur non documentée n’apparaît jamais dans le catalogue généré, et l’analyseur [FCE009](analyzers/FCE009.fr.md) signale toute factory d’erreur laissée sans documentation.

Voir [Écrire la documentation d’une erreur](WritingErrorsGuide.fr.md).

## Toute exception doit-elle devenir une erreur de première classe ?

Non. Modélisez les erreurs applicatives porteuses de sens : situations reconnues, règles, contraintes ou échecs de frontière qui bénéficient d’une identité stable et d’une explication partagée. Les exceptions du framework, crashes accidentels et fautes d’implémentation bas niveau restent généralement de simples exceptions techniques.

Voir [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md).

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