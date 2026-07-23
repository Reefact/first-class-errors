# FAQ

🌍 **Langues :**  
🇬🇧 [English](./FAQ.en.md) | 🇫🇷 Français (ce fichier)

## Pourquoi ne pas simplement utiliser des exceptions normales ?

C’est possible. FirstClassErrors utilise toujours les exceptions standard .NET comme mécanisme de signalement et de propagation des défaillances.

FirstClassErrors associe à cette exception une `Error` structurée — porteuse d’un code stable, de contexte structuré, de diagnostics et d’une documentation liée. Voir [Concepts fondamentaux](CoreConcepts.fr.md).

## Pourquoi ne pas utiliser un `Result<T, Error>` générique ?

Vous pourriez. Transporter l’`Error` de la bibliothèque dans un type résultat générique — par exemple le `Result<T, E>` de [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions) — conserve la structure qu’un simple `Result<T, string>` perdrait.

`Outcome` est cette idée, spécialisée pour ce modèle d’erreur. Son côté échec est toujours une `Error` — et non un second paramètre de type à propager dans chaque signature — et sa petite API (`Then`, `Recover`, `Finally`) est nommée par intention plutôt qu’avec la mécanique de la programmation fonctionnelle (`Map`, `Bind`, `Match`). L’objectif : du code de domaine et des use cases qui se lisent comme un flux métier, pas comme de la tuyauterie de résultat générique.

Voir [Cas d’usage](UsagePatterns.fr.md) et [Comparaison avec les bibliothèques de gestion d’erreurs](ComparisonWithOtherLibraries.fr.md).

## Est-ce que FirstClassErrors remplace les logs ?

Non. Il structure les erreurs et leur documentation ; votre système de logs enregistre leurs occurrences et leur contexte d’exécution. Une erreur de première classe donne à chaque occurrence journalisée un code stable et une explication partagée, mais elle ne stocke ni ne remplace le log lui-même.

Voir [Intégration au logging structuré](LoggingIntegration.fr.md).

## Est-ce trop lourd pour une application simple ?

Cela peut l’être. Les petits scripts, prototypes et systèmes sans besoin durable de support peuvent être mieux servis par des exceptions standard.

Voir [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md) pour les critères de décision.

## Puis-je adopter FirstClassErrors progressivement ?

Oui. Aucune migration globale n’est nécessaire. Introduisez-le là où les erreurs méritent d’être modélisées — un domaine, un module ou un use case à la fois — et laissez le reste sur des exceptions standard jusqu’à ce qu’il justifie une erreur de première classe. Comme une factory n’est qu’une façon typée de construire une `Error`, le code existant continue de fonctionner pendant que les chemins nouveaux ou repris adoptent le modèle.

Voir [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md).

## Quand utiliser `Outcome<T>` ?

Utilisez-le lorsque l’échec est une branche attendue du flux normal : validation, parsing, traitement par lots ou succès partiel.

Utilisez une exception lorsque l’échec doit interrompre l’opération à ce niveau. Les deux chemins peuvent porter la même `Error`, créée par la même factory.

Voir [Cas d’usage](UsagePatterns.fr.md).

## `Outcome<T>` conserve-t-il une stack trace ?

Aucune exception n’est créée ou levée tant que l’erreur est transportée comme donnée. Si l’échec est ensuite escaladé avec `GetResultOrThrow()` ou `error.ToException()`, l’exception et sa stack trace commencent à ce point d’escalade.

## Toute exception doit-elle devenir une erreur de première classe ?

Non. Modélisez les erreurs applicatives porteuses de sens : situations reconnues, règles, contraintes ou échecs de frontière qui bénéficient d’une identité stable et d’une explication partagée. Les exceptions du framework, crashes accidentels et fautes d’implémentation bas niveau restent généralement de simples exceptions techniques.

Voir [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md).

## Comment gérer une exception provenant d’une bibliothèque tierce ?

Attrapez-la à la frontière — en général un adaptateur ou un port secondaire — et décidez si elle représente une défaillance applicative stable. Si oui, modélisez-la via une factory sous la forme d’une `InfrastructureError` (ou une `SecondaryPortError` pour une dépendance sortante, une `PrimaryPortError` pour une entrante), placez le détail technique dans le `DiagnosticMessage` et ajoutez les faits sûrs via l’`ErrorContext`.

L’erreur de première classe capture le *sens* de la défaillance ; elle ne stocke pas l’objet exception attrapé. Gardez l’exception d’origine à sa place — enregistrée par votre pipeline de logs au point de capture. Ne transformez pas chaque exception technique en erreur de première classe : seulement les défaillances de frontière qui méritent une identité stable.

Voir [Taxonomie et composition des erreurs](ErrorTaxonomy.fr.md) et [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md).

## Pourquoi utiliser des factories plutôt que `new` ?

Une factory donne un nom à une situation d’erreur, centralise son code et ses messages, et sert de point d’ancrage à la documentation vivante. Par rapport à un `new DomainError(...)` sur chaque site d’appel, elle évite de répéter les codes, les messages et les métadonnées entre les use cases, et garde la construction de l’erreur hors du happy path.

Voir [Premiers pas](GettingStarted.fr.md).

## Dois-je documenter toutes les erreurs de première classe ?

Oui — toute erreur de première classe que vous définissez est destinée à être documentée. Une erreur non documentée n’apparaît jamais dans le catalogue généré, et l’analyseur [FCE009](analyzers/FCE009.fr.md) signale toute factory d’erreur laissée sans documentation.

Voir [Écrire la documentation d’une erreur](WritingErrorsGuide.fr.md).

## Quelle différence entre la documentation d’erreur et les messages runtime ?

La documentation est la partie d’une erreur qui ne dépend d’aucune occurrence particulière : elle est identique pour chaque instance et ne change jamais à l’exécution. Elle sert à *comprendre* l’erreur — titre, sens, règle, hypothèses de diagnostic et exemples représentatifs.

Les messages runtime sont portés par l’instance d’erreur elle-même et servent à *investiguer* cette occurrence :

- `ShortMessage` est le résumé public sûr ;
- `DetailedMessage` est un détail public optionnel et maîtrisé ;
- `DiagnosticMessage` est le détail interne destiné aux logs et au support.

Voir [Écrire la documentation d’une erreur](WritingErrorsGuide.fr.md) et [Écrire les messages d’une erreur](WritingErrorMessages.fr.md).

## Les messages d’erreur peuvent-ils être affichés aux utilisateurs ?

`ShortMessage`, et éventuellement `DetailedMessage`, sont les messages publics maîtrisés : ils peuvent être exposés sans risque aux utilisateurs ou aux clients d’API. `DiagnosticMessage` est interne et ne doit jamais être exposé.

« Public » signifie « sûr à exposer », pas nécessairement la formulation finale de l’interface : si vous avez besoin d’un texte entièrement localisé et aux couleurs du produit, votre couche de présentation en reste responsable. L’erreur garantit un message sûr et stable que vous pouvez afficher ou mapper.

Voir [Écrire les messages d’une erreur](WritingErrorMessages.fr.md).

## Quand ajouter un `ErrorContext` ?

Utilisez-le pour des faits sûrs, propres à une occurrence, qui améliorent réellement le diagnostic ou l’observabilité : identifiant métier, valeur mesurée ou borne pertinente.

N’y placez ni secret, ni payload volumineux, ni documentation générique, ni procédure opérationnelle. Voir [Contexte d’erreur](ErrorContext.fr.md).

## Les diagnostics sont-ils des causes racines ?

Non. Ce sont des hypothèses plausibles et des points de départ pour l’investigation. Ils décrivent ce qui peut expliquer l’erreur sans prétendre à la certitude ni attribuer une faute.

## Les diagnostics doivent-ils contenir les procédures du support ?

Non. Gardez le ticketing, l’escalade et les consignes de contact d’équipe hors de la documentation applicative. Une piste d’analyse indique où chercher ; elle ne prescrit pas un workflow organisationnel.

Voir [Écrire la documentation d’une erreur](WritingErrorsGuide.fr.md#6-écrire-les-diagnostics-comme-des-hypothèses).

## Pourquoi écrire la documentation dans le code ?

Parce qu’elle est liée aux mêmes factories qui créent les erreurs. Elle peut être extraite automatiquement et évolue à côté du comportement qu’elle décrit, ce qui réduit la dérive.

Voir [Architecture du pipeline de documentation](ArchitectureOfTheDocumentationPipeline.fr.md).

## FirstClassErrors est-il lié au Domain-Driven Design ?

Non. Son vocabulaire s’accorde bien avec le DDD et l’architecture hexagonale, mais tout système durable qui a besoin d’une sémantique d’erreur explicite, de supportabilité et de documentation vivante peut l’utiliser.

## Comment imbriquer les différentes catégories d’erreurs ?

Une `DomainError` affirme qu’une règle métier a été violée. Imbriquer une défaillance d’infrastructure à l’intérieur décrirait une panne technique comme une partie du vocabulaire métier.

Une erreur de port ou d’infrastructure peut contenir une `DomainError` lorsqu’un échec à la frontière est causé par un rejet métier, par exemple une requête entrante impossible à convertir en value object valide. Les deux faits sont ainsi conservés sans rendre le domaine dépendant de HTTP, de la messagerie ou d’une technologie d’adapter.

Voir [Taxonomie et composition des erreurs](ErrorTaxonomy.fr.md).

---

<div align="center">
<a href="Internationalisation.fr.md">← Internationalisation</a> · <a href="README.fr.md#-documentation">↑ Table des matières</a>
</div>

---
