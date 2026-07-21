# ADR-0029 | Compléter la surface async d'Outcome.Try avec des surcharges sans token

🌍 🇬🇧 [English](0029-complete-the-outcome-try-async-surface-with-token-less-overloads.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-21
**Décideurs :** Reefact

## Contexte

L'ADR-0028 a introduit `Outcome.Try` avec quatre surcharges : une forme
synchrone à valeur (`Func<T>`), une forme synchrone à effet de bord (`Action`),
et leurs contreparties asynchrones, qui prennent un `CancellationToken`
(`Func<CancellationToken, Task<T>>` et `Func<CancellationToken, Task>`). Sa phrase
de Décision garde le pont étroit « en signalant son mésusage par des analyzers
d'usage plutôt qu'en élargissant l'API », et sa Justification rejette « réduire
la surface de type » — deux arguments cadrés, selon les mots mêmes de l'ADR, aux
mésusages où « appel légitime et illégitime partagent la même signature et ne
diffèrent que par un contexte que le compilateur ne voit pas » (les quatre gardes
sémantiques FCE019–FCE022).

Une revue de code a ensuite trouvé un défaut qui n'est **pas** de cette nature.
Passer un lambda asynchrone sans paramètre à la surcharge synchrone à effet de
bord — `Outcome.Try<E>(async () => { await …; }, map)` — lie le lambda à `Action`
en **`async void`**, faute d'une surcharge sans token retournant une `Task` qu'il
préférerait. L'`Action` rend la main au premier `await`, donc `Try` ne voit
aucune exception, retourne un succès, et toute exception levée après le premier
`await` s'échappe du `try`/`catch` hors bande — une exception `async void` non
observée qui, typiquement, fait crasher le processus. La même forme abandonne en
silence un fire-and-forget `() => ReturnsTask()`. C'est un défaut **silencieux**,
sur la frontière même que `Try` existe pour sécuriser.

Trois faits cadrent la correction :

* Le cas asynchrone **à valeur** ne partage pas le défaut. Un lambda
  `async () => … return T` produit une `Task<T>`, qui ne correspond pas à
  `Func<T>`, donc c'est une **erreur de compilation** — un garde-fou qui oriente
  l'appelant vers la surcharge à token. Seul le cas `Action` à effet de bord se
  lie silencieusement.
* Le BCL .NET a résolu ce footgun exact par le **design des surcharges** :
  `System.Threading.Tasks.Task.Run` expose `Action`, `Func<Task>`, `Func<T>` et
  `Func<Task<T>>` côte à côte, précisément pour qu'un lambda asynchrone se lie à
  la surcharge retournant une `Task` (awaitée) plutôt qu'en `async void`.
* Le défaut est **visible par le système de types** et n'a **aucun usage
  légitime** : personne ne veut passer un lambda async-void à un `Try` synchrone.

## Décision

FirstClassErrors ajoute des surcharges sans token `Func<Task>` et `Func<Task<T>>`
à `Outcome.Try` pour qu'un lambda asynchrone se lie à un délégué retournant une
`Task` awaitée plutôt qu'à l'`Action` synchrone en `async void`, complétant la
surface async — une correction structurelle que le « plutôt qu'en élargissant
l'API » de l'ADR-0028 n'était pas censé interdire.

## Justification

* **Un défaut du système de types mérite une correction du système de types.**
  L'accident de liaison naît à la résolution de surcharges, et la règle de
  « betterness » de C# fait qu'un lambda asynchrone préfère un délégué retournant
  une `Task` à une `Action`. Une fois les surcharges sans token
  `Func<Task>` / `Func<Task<T>>` présentes, le lambda async s'y lie et est awaité
  dans le `try`/`catch` ; le crash devient une impossibilité de compilation plutôt
  qu'un danger d'exécution. C'est la bonne altitude — la même sur laquelle le cas
  async à valeur s'appuie déjà pour échouer sans risque.
* **Cela colle au précédent BCL qui fait autorité.** `Task.Run` a établi cette
  forme exacte à quatre pour ce footgun exact ; la reproduire est le geste
  conventionnel et le moins surprenant pour un auteur de bibliothèque .NET, et
  cela complète la symétrie sync/async × valeur/effet de bord que l'ADR-0028 a
  déjà engagée (les formes async n'existent aujourd'hui que sous forme à token).
* **La préférence-analyzer de l'ADR-0028 ne couvre pas ce cas.** Cette décision
  argumente contre la *réduction* de la surface de type pour policer des mésusages
  *sémantiques* dont les formes légitime et illégitime partagent une signature. Ce
  défaut est l'inverse : la distinction async/sync est visible par le système de
  types, le mésusage n'a aucune contrepartie légitime, et la correction *ajoute*
  une surcharge au lieu de retirer une capacité — elle ne bloque donc aucun usage
  valide et ne déclenche pas l'objection de l'ADR-0028.
* **Un analyzer ne peut pas remplacer la prévention ici.** Un diagnostic
  supprimable, en Warning par défaut, qui ne tourne que là où les analyzers sont
  activés est le mauvais garde pour un crash de processus *silencieux* — le plus
  faible précisément chez les consommateurs .NET Standard 2.0 / .NET Framework
  anciens pour qui `Try` existe. Prévenir vaut mieux que détecter quand la panne
  est silencieuse.

## Alternatives envisagées

### Un analyzer (FCE023) au lieu des surcharges

Envisagé parce qu'il ne change aucune surface publique et prolonge la famille
FCE019–FCE022 que la bibliothèque livre déjà.

Rejeté comme garde *primaire* parce qu'il détecte au lieu de prévenir : un crash
de processus silencieux peut quand même être livré là où le warning est coupé ou
supprimé, et l'analyzer doit poursuivre chaque forme syntaxique (lambda async,
expression fire-and-forget) là où la surcharge ferme toute la famille au niveau
du type. Il reste disponible en défense en profondeur optionnelle pour les cas
résiduels non-accidentels ci-dessous, mais n'est pas nécessaire pour fermer le
défaut.

### N'ajouter que la surcharge à effet de bord `Func<Task>`

Envisagé parce que seul le cas `Action` crashe silencieusement, donc `Func<Task>`
seul ferme le défaut tout en évitant l'ambiguïté côté valeur ci-dessous.

Rejeté parce qu'il laisse la surface async asymétrique — une forme sans token
retournant une `Task` pour le cas à effet de bord mais pas pour le cas à valeur —
sans raison de principe qu'un appelant pourrait prédire. Compléter les deux
reflète `Task.Run` et la grille sync/async × valeur/effet de bord.

### Laisser le défaut reporté à un suivi

Envisagé parce que l'ADR-0028 vient d'être accepté et que le changement touche sa
formulation.

Rejeté parce que livrer une primitive publique de gestion d'erreurs avec un crash
silencieux connu sur sa frontière async n'est pas acceptable ; la bonne réponse
est de consigner le raffinement ici et de laisser le mainteneur l'accepter, pas
de publier le danger.

## Conséquences

### Positives

* Le crash async-void et l'abandon fire-and-forget deviennent structurellement
  impossibles pour un lambda asynchrone ; ils sont empêchés à la compilation, pas
  seulement diagnostiqués.
* La surface async est symétrique et colle à la forme `Task.Run` du BCL, donc les
  lambdas async se comportent de façon prévisible.
* Aucun appel légitime n'est bloqué : les surcharges ajoutent de la capacité au
  lieu d'en retirer.

### Négatives

* Deux surcharges publiques de plus à maintenir et documenter sur le plancher
  netstandard2.0.
* Ajouter `Func<Task<T>>` à côté de `Func<T>` rend un lambda **sans type de retour
  naturel** — un `() => throw …` nu ou `() => null` — ambigu entre les deux, un
  changement de compatibilité source. Il échoue en **erreur de compilation**
  (fail-safe, jamais une surprise à l'exécution) et ne mord que des lambdas
  pathologiques, en pratique réservés aux tests ; une vraie opération renvoie une
  valeur concrète ou une vraie `Task`. C'est le compromis même que `Task.Run`
  assume déjà.

### Risques

* Deux cas résiduels, **non-accidentels**, se lient encore à `Action` par leur
  type statique : une variable explicitement typée `Action`, et un method-group
  `async void` passé par nom. Ni l'un ni l'autre n'est le chemin lambda accidentel
  que vise cette décision, et `async void` est déjà largement découragé dans
  l'écosystème. Mitigation, si souhaitée plus tard : un analyzer optionnel en
  défense en profondeur (voir Alternatives).
* Les surcharges async sans token ne passent aucun `CancellationToken` ; les
  appelants qui en ont besoin ont toujours les surcharges à token, qu'un lambda
  `ct => …` sélectionne proprement. C'est un manque de confort, pas de correction.

## Actions de suivi

* Couvrir les cas lambda-async et fire-and-forget par des tests de non-régression,
  et documenter l'ambiguïté des lambdas sans type naturel dans les remarques XML.
* Garder le guide EN/FR et les pages de règle de `Try` synchronisés avec la
  surface complétée.
* Décider si le cas résiduel method-group justifie un analyzer optionnel ; fermer
  ou reconvertir l'issue de suivi FCE023 en conséquence.

## Références

* ADR-0028 — faire entrer le code levant dans les outcomes via un Try encadré (cet
  ADR raffine sa clause « plutôt qu'en élargissant l'API » ; il ne change pas la
  décision du pont encadré elle-même).
* ADR-0005 — réserver le nom de fabrique simple à la variante retournant un Outcome
  (la conservation d'API face à laquelle cette décision est pesée).
* `System.Threading.Tasks.Task.Run` — le précédent BCL pour la forme de surcharges
  `Action` / `Func<Task>` / `Func<T>` / `Func<Task<T>>`.
* PR #265 ; issue #267 (le suivi async-void reporté que cette décision résout).
* [Guide Outcome](../../for-users/OutcomeGuide.fr.md).
