# FAQ

## ❓ Pourquoi ne pas simplement utiliser des exceptions normales ?

C’est possible — et DiagnosableExceptions repose toujours sur les exceptions standard .NET.

La différence est que cette bibliothèque ajoute :

* des codes d’erreur stables  
* des diagnostics structurés  
* une documentation liée  
* un modèle cohérent  

Elle transforme les exceptions de *signaux techniques* en *unités de connaissance documentées*.

## ❓ Pourquoi ne pas utiliser `Result<T, string>` à la place ?

Une string perd la structure.

DiagnosableExceptions conserve :

* les codes d’erreur  
* des messages riches  
* des diagnostics  
* du contexte  

tout en permettant de transporter les erreurs sans lever d’exception via `TryOutcome<T>`.

Vous obtenez les avantages d’un flux basé sur les résultats sans perdre la puissance des exceptions.

## ❓ N’est-ce pas trop lourd pour des applications simples ?

Pour de petits scripts ou des prototypes, oui, cela peut être superflu.

Cette bibliothèque prend tout son sens dans des systèmes qui sont :

* riches en domaine  
* conçus pour durer  
* critiques pour le support  
* utilisés par plusieurs équipes  

C’est un investissement dans la clarté et la supportabilité.

## ❓ Pourquoi utiliser des factories d’exception plutôt que `new` ?

Les factories :

* rendent explicites les situations d’erreur  
* gardent la construction en dehors du happy path  
* centralisent messages et codes  
* servent de points d’ancrage pour la documentation  

Elles améliorent la lisibilité et rendent possible la documentation vivante.

## ❓ Les diagnostics sont-ils équivalents aux causes racines ?

Non.

Les diagnostics décrivent des **explications plausibles** et guident l’investigation.  
Ce sont des hypothèses, pas des certitudes.

## ❓ Les diagnostics accusent-ils les développeurs ou les utilisateurs ?

Non.

Les diagnostics doivent décrire des états ou des conditions, pas attribuer de responsabilité.

L’objectif est d’aider l’analyse, pas de désigner des fautifs.

## ❓ Pourquoi la documentation est-elle écrite dans le code ?

Parce que la documentation dans le code :

* évolue avec le système  
* reste proche du comportement  
* peut être extraite automatiquement  

Cela évite la dérive entre le code et la documentation.

## ❓ Quand dois-je utiliser `TryOutcome<T>` ?

Utilisez-le lorsque l’échec est attendu et fait partie du flux normal :

* validation d’entrées  
* parsing  
* traitement par lots  

Utilisez directement des exceptions lorsque :

* des invariants sont violés  
* le système ne peut pas continuer  

## ❓ `TryOutcome<T>` fait-il perdre la stack trace ?

Oui — volontairement.

Avec `TryOutcome<T>`, l’exception est traitée comme une information d’erreur structurée, pas comme un crash runtime.  
Si vous appelez ensuite `GetOrThrow()`, l’exception est levée à ce moment-là.

## ❓ Puis-je documenter toutes les exceptions ?

Non.

Concentrez-vous sur les erreurs porteuses de sens au niveau métier.  
Ne documentez pas :

* les exceptions du framework  
* les crashes accidentels  
* les fautes techniques bas niveau  

Le DSL est destiné aux erreurs qui ont une signification sémantique dans votre système.

## ❓ Est-ce lié au Domain-Driven Design ?

Cela s’aligne très bien avec le DDD, mais ce n’est pas limité à ce cadre.

Tout système qui bénéficie de :

* règles claires  
* sémantique d’erreur explicite  
* supportabilité  

peut utiliser cette bibliothèque.

---

Section précédente: [Architecture du pipeline de documentation](ArchitectureOfTheDocumentationPipeline.fr.md)

---