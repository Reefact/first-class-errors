# Concepts clés

FirstClassErrors n’est pas simplement une bibliothèque utilitaire.  Il introduit une autre manière de penser les erreurs applicatives.

Au lieu de considérer les exceptions comme des incidents techniques, elles sont vues comme **une connaissance structurée sur ce qui s’est mal passé**.

## 🧠 Une erreur n’est pas juste un message

Dans de nombreux systèmes, les exceptions se résument à :

> un type + un message texte

Avec FirstClassErrors, une **erreur** représente :

* une **situation d’erreur spécifique**  
* identifiée par un **code d’erreur stable**  
* décrite avec trois messages dédiés (un résumé public, un détail public optionnel et un message de diagnostic interne)  
* éventuellement enrichie de contexte  
* associée à des diagnostics structurés  

Une **erreur** devient un objet sémantique, pas seulement un signal d’exécution.

### Trois messages, trois publics

Une erreur sépare délibérément ce qui peut atteindre un appelant de ce qui est destiné aux développeurs et au support :

* **`ShortMessage`** (obligatoire) — un résumé public court, exposable sans risque à un utilisateur final ou à un client d’API (par ex. le `title` d’un problem detail RFC 9457).
* **`DetailedMessage`** (optionnel) — un détail public maîtrisé, exposable **uniquement** si l’application le décide explicitement (par ex. le `detail` d’un problem detail RFC 9457). Il ne doit jamais contenir d’information sensible ou interne.
* **`DiagnosticMessage`** (obligatoire) — le message de diagnostic interne destiné aux logs, au support et aux développeurs. Il peut contenir des détails techniques/opérationnels (identifiants, valeurs fautives, état interne) et n’est **jamais** exposé aux clients externes par défaut. `error.ToException()` l’utilise comme `Message` de l’exception.

Le cœur du modèle reste agnostique vis-à-vis d’HTTP : le message de diagnostic n’est jamais un corps de réponse HTTP par défaut.

## 🧩 Une factory représente une situation d’erreur

Les factories d’**erreur** sont au cœur du modèle.

Une méthode factory :

* représente un scénario d’erreur précis  
* lui donne un **nom** dans le code  
* centralise la création de l’erreur  
* devient le point d’ancrage de la documentation  

Cela signifie :

> Chaque factory = un cas d’erreur documenté.

Les factories améliorent la lisibilité et rendent explicites les situations d’erreur, tout en gardant les détails de construction en dehors de la logique métier.

## 📘 La documentation vit avec le code

La documentation des erreurs est écrite avec le DSL `DescribeError` et liée directement aux factories d’**erreur**.

Cela permet de définir :

* des descriptions structurées  
* les règles violées  
* des diagnostics  
* des exemples réalistes  

Comme la documentation est du code :

* elle évolue avec le système  
* elle ne dérive pas  
* elle peut être extraite automatiquement  

C’est de la **documentation vivante**.

## 🔎 Les diagnostics décrivent des hypothèses, pas des fautes

Les diagnostics répondent à :

* Qu’est-ce qui pourrait avoir causé cette erreur ?  
* Est-ce probablement lié aux données d’entrée, au système, ou aux deux ?  
* Par où commencer l’investigation ?  

Les diagnostics sont :

* structurés  
* orientés humains  
* des guides pour l’analyse  

Ils n’encodent pas de processus opérationnels. Ils donnent une **direction**, pas des procédures.

## 🧭 Taxonomie des erreurs

Les erreurs sont modélisées sous forme de hiérarchie ayant pour racine le type abstrait `Error` :

* **`DomainError`** — une violation d’une règle métier (la couche domaine).
* **`InfrastructureError`** — une défaillance à une frontière technique. Elle porte une `Transience` (`Unknown` / `NonTransient` / `Transient`) et une `InteractionDirection`.
  * **`PrimaryPortError`** — frontière entrante (`Direction` fixée à `Incoming`).
  * **`SecondaryPortError`** — frontière sortante (`Direction` fixée à `Outgoing`).

Les erreurs de Port remplacent les anciennes exceptions d’Adapter. Lorsqu’une défaillance de port enveloppe plusieurs causes, `PrimaryPortInnerErrors` / `SecondaryPortInnerErrors` agrègent les erreurs internes et calculent la transience globale.

Chaque erreur possède une exception associée, obtenue via `error.ToException()` : `DomainException`, `InfrastructureException`, `PrimaryPortException`, `SecondaryPortException`. On ne les instancie jamais directement avec `new` ; l’exception expose son `Error` (et, à travers lui, le contexte et les erreurs internes).

## 🔁 Erreur ou donnée ? Les deux sont possibles

Traditionnellement, les exceptions sont toujours levées.  
FirstClassErrors supporte deux modèles complémentaires :

* **L’exception comme flux de contrôle** (throw classique)  
* **L’erreur comme donnée** (`Outcome<T>`, ou `Outcome` non générique lorsqu’il n’y a pas de valeur)  

Cela permet aux erreurs d’être :

* levées immédiatement  
* transportées dans des pipelines de validation  
* escaladées plus tard  

La même situation d’erreur peut servir ces deux rôles.

Le modèle sans levée d’exception est `Outcome` / `Outcome<T>` : l’`Error` est portée comme donnée (`IsSuccess` / `IsFailure` / `Error`) et peut être convertie en exception à la demande via `error.ToException()`.

## 🎯 De l’échec à la connaissance

Avec ce modèle, les erreurs ne sont plus :

> des défaillances techniques isolées

Elles deviennent :

> une connaissance partagée et structurée sur la manière dont le système peut échouer.

Cela crée un pont entre :

* le développement  
* le support  
* la documentation  
* l’exploitation  

Le tout basé sur une même source de vérité : le code.

---

Section précédente: [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md) | Section suivante: [Guide du contexte d’erreur](ErrorContext.fr.md)

---