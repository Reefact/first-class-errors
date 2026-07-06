# FAQ

🌍 **Langues:**  
🇬🇧 [English](./FAQ.en.md) | 🇫🇷 Français (ce fichier)

## ❓ Pourquoi ne pas simplement utiliser des exceptions normales ?

C’est possible — et FirstClassErrors repose toujours sur les exceptions standard .NET.

La différence est que cette bibliothèque ajoute :

* des codes d’erreur stables  
* des diagnostics structurés  
* une documentation liée  
* un modèle cohérent  

Elle transforme les exceptions de *signaux techniques* en *unités de connaissance documentées*.

## ❓ Pourquoi ne pas utiliser `Result<T, string>` à la place ?

Une string perd la structure.

FirstClassErrors conserve :

* les codes d’erreur  
* des messages riches  
* des diagnostics  
* du contexte  

tout en permettant de transporter les erreurs sans lever d’exception via `Outcome<T>`.

Vous obtenez les avantages d’un flux basé sur les résultats sans perdre la puissance des exceptions.

## ❓ N’est-ce pas trop lourd pour des applications simples ?

Pour de petits scripts ou des prototypes, oui, cela peut être superflu.

Cette bibliothèque prend tout son sens dans des systèmes qui sont :

* riches en domaine  
* conçus pour durer  
* critiques pour le support  
* utilisés par plusieurs équipes  

C’est un investissement dans la clarté et la supportabilité.

## ❓ Pourquoi utiliser des factories d’erreur plutôt que `new` ?

Les factories d’erreur retournent des objets `Error` (levés via `.ToException()` lorsqu’une exception est nécessaire). Elles :

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

## ❓ Quand dois-je ajouter un `ErrorContext` à une erreur ?

Utilisez `ErrorContext` pour des **faits spécifiques à l’occurrence** qui améliorent le diagnostic et l’observabilité. Le contexte vit sur l’`Error`, si bien qu’il voyage avec l’erreur, qu’elle soit transportée via `Outcome<T>` ou levée en exception.

Bons candidats :

* identifiants métier utiles à l’investigation
* valeurs ayant violé une règle
* dates ou bornes pertinentes pour l’échec

Évitez d’y mettre :

* des données sensibles
* des payloads volumineux
* des informations déjà présentes dans la documentation stable de l’erreur

Règle simple : si la donnée aide à expliquer cette occurrence dans les logs, et qu’elle est sûre à exposer, ajoutez-la.

## ❓ Quand dois-je utiliser `Outcome<T>` ?

Utilisez-le lorsque l’échec est attendu et fait partie du flux normal :

* validation d’entrées  
* parsing  
* traitement par lots  

Utilisez directement des exceptions lorsque :

* des invariants sont violés  
* le système ne peut pas continuer  

## ❓ `Outcome<T>` fait-il perdre la stack trace ?

Oui — volontairement.

Avec `Outcome<T>`, l’exception est traitée comme une information d’erreur structurée, pas comme un crash runtime.  
Si vous appelez ensuite `GetResultOrThrow()`, l’exception est levée à ce moment-là.

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

## ❓ Pourquoi une erreur de domaine ne peut-elle imbriquer que des erreurs de domaine, alors qu’une erreur d’infrastructure peut aussi imbriquer une erreur de domaine ?

Parce que le type d’une erreur **suit la nature de la défaillance — quelle règle a été violée — et non l’endroit où la défaillance est détectée.**

Une `DomainError` signifie qu’une règle métier a été enfreinte ; sa cause n’est jamais qu’une autre défaillance métier, donc elle n’imbrique que des `DomainError`. Lui donner une cause d’infrastructure ferait fuiter une préoccupation technique dans le vocabulaire du domaine — et étiquetterait une panne technique comme une faute métier.

Une erreur d’infrastructure / de port *peut*, elle, imbriquer une `DomainError`, parce qu’une frontière signale légitimement une défaillance *au niveau de la requête* dont la *cause* est une règle métier. Le cas d’école : un adapter entrant (port primaire) mappe un DTO en value objects, et un value object refuse d’être construit parce qu’un invariant est violé. Deux faits distincts coexistent :

* la **cause** — « cette valeur est invalide » — appartient au domaine (le value object a produit une `DomainError`) ;
* la **condition de frontière** — « cette requête est rejetée au bord » — appartient à l’adapter (une `PrimaryPortError`).

Imbriquer l’erreur de domaine dans l’erreur de port conserve les deux. C’est une *erreur d’infrastructure **causée par** une erreur de domaine* — pas l’une ou l’autre.

Attention au sens : le value object reste du code de domaine et émet la `DomainError` ; c’est l’adapter qui classe la condition de frontière comme infrastructure. Un value object ne doit jamais émettre lui-même une erreur d’infrastructure — cela ferait dépendre le domaine de l’infrastructure.

Pourquoi cette distinction en vaut-elle la peine ?

* **Indépendance au transport** — la même `DomainError` se rend en HTTP 400/422, en gRPC `INVALID_ARGUMENT`, ou en code de sortie CLI. Le statut HTTP est un *rendu* de l’erreur, pas son identité.
* **Exploitation** — on alerte et on rejoue sur `InteractionDirection` et `Transience`, pas sur le seul type. Un utilisateur qui saisit un mauvais email produit une erreur de port *entrante* non-transitoire qui ne doit jamais réveiller personne ; un timeout de base de données produit une erreur *sortante* transitoire qui, elle, le peut. Mettre une saisie invalide dans le même panier que de vraies pannes empoisonnerait ce signal.

---

Section précédente: [Internationalisation](Internationalisation.fr.md)

---