# Concepts clés

DiagnosableExceptions n’est pas simplement une bibliothèque utilitaire.  Il introduit une autre manière de penser les erreurs applicatives.

Au lieu de considérer les exceptions comme des incidents techniques, elles sont vues comme **une connaissance structurée sur ce qui s’est mal passé**.

## 🧠 Une exception n’est pas juste un message

Dans de nombreux systèmes, les exceptions se résument à :

> un type + un message texte

Avec DiagnosableExceptions, une exception représente :

* une **situation d’erreur spécifique**  
* identifiée par un **code d’erreur stable**  
* décrite avec des messages porteurs de sens  
* éventuellement enrichie de contexte  
* associée à des diagnostics structurés  

Une exception devient un **objet sémantique**, pas seulement un signal d’exécution.

## 🧩 Une factory représente une situation d’erreur

Les factories d’exception sont au cœur du modèle.

Une méthode factory :

* représente un scénario d’erreur précis  
* lui donne un **nom** dans le code  
* centralise la création de l’erreur  
* devient le point d’ancrage de la documentation  

Cela signifie :

> Chaque factory = un cas d’erreur documenté.

Les factories améliorent la lisibilité et rendent explicites les situations d’erreur, tout en gardant les détails de construction en dehors de la logique métier.

## 📘 La documentation vit avec le code

La documentation des erreurs est écrite avec le DSL `DescribeError` et liée directement aux factories d’exception.

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

## 🔁 Exception ou donnée ? Les deux sont possibles

Traditionnellement, les exceptions sont toujours levées.  
DiagnosableExceptions supporte deux modèles complémentaires :

* **L’exception comme flux de contrôle** (throw classique)  
* **L’exception comme donnée** (`TryOutcome<T>`)  

Cela permet aux erreurs d’être :

* levées immédiatement  
* transportées dans des pipelines de validation  
* escaladées plus tard  

Le même type d’exception peut servir ces deux rôles.

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

Section précédente: [Quand ne pas utiliser DiagnosableExceptions](WhenNotToUseDiagnosableExceptions.fr.md) | Section suivante: [Guide du contexte d’erreur](ErrorContext.fr.md)

---