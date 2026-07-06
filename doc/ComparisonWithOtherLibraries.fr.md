# Comparaison avec les librairies de gestion d’erreurs

🌍 **Langues:**  
🇬🇧 [English](./ComparisonWithOtherLibraries.en.md) | 🇫🇷 Français (ce fichier)

[ErrorOr](https://github.com/amantinband/error-or) et [FluentResults](https://github.com/altmann/FluentResults) sont d’excellentes librairies, matures. Si votre objectif est un type *Result* léger — retourner les erreurs comme des valeurs plutôt que de les lever — ce sont des choix ciblés et largement adoptés, faits exactement pour ça.

FirstClassErrors répond à une **question différente**. Ce n’est pas d’abord une librairie *Result* : c’est une manière de faire des erreurs une **connaissance de première classe, documentée et diagnosticable** sur votre système — des erreurs que vous pouvez *transporter* comme valeurs **ou** *lever* comme exceptions, avec un seul et même modèle.

Cette page met en avant ce que FirstClassErrors fait différemment.

## 🎯 Un centre de gravité différent

| Librairie | La question à laquelle elle répond |
|---|---|
| **ErrorOr** | *Comment retourner une ou plusieurs erreurs comme valeur au lieu de les lever ?* |
| **FluentResults** | *Comment retourner un résultat portant erreurs, succès et raisons causales ?* |
| **FirstClassErrors** | *Comment transformer les erreurs en connaissance documentée et diagnosticable — et les faire circuler selon les besoins de chaque couche ?* |

Pour ErrorOr et FluentResults, l’**erreur est une charge utile du type résultat**. Pour FirstClassErrors, l’**erreur est le modèle**, et le type résultat (`Outcome`) n’est que l’une des façons de la transporter.

## 🧩 Un modèle d’erreur, trois transports

Le modèle `Error` est découplé de la manière dont il voyage. La *même* erreur peut être :

- conservée comme **donnée** — une valeur `Error` que vous inspectez, journalisez ou enrichissez ;
- **levée** — transformée en exception typée via `error.ToException()`, puis attrapée et routée par type ;
- **transportée** — encapsulée dans un `Outcome` / `Outcome<T>` et composée sans lever.

Des passerelles relient les trois : vous n’êtes jamais enfermé dans un style. Vous pouvez *transporter* les erreurs dans votre domaine et les *lever* à une frontière — avec **le même objet erreur**, sans re-modélisation entre les deux.

ErrorOr et FluentResults sont, par conception, *uniquement erreurs-comme-valeurs* : l’erreur est soudée au type résultat et le modèle évite délibérément de lever. FirstClassErrors traite le chemin exception comme une **citoyenne de première classe**, au même titre que le chemin valeur.

## 📖 Des erreurs qui portent du sens, pas seulement un identifiant

Une `Error` d’ErrorOr, c’est un code, une description, un `Type` et un sac de métadonnées. Une erreur FluentResults, c’est un message avec métadonnées et raisons imbriquées. C’est suffisant pour *traiter* une erreur au runtime.

Une erreur FirstClassErrors est décrite pour des **humains** : un titre, une explication en langage clair, la **règle métier** violée et des exemples représentatifs. L’erreur cesse d’être un jeton technique pour devenir quelque chose qu’un développeur — ou un ingénieur support — peut réellement *comprendre*.

## 🔎 Des diagnostics faits pour l’investigation

Là où les autres *classifient* une erreur (une enum `ErrorType`, une entrée de métadonnées), FirstClassErrors permet à une erreur de déclarer **comment l’investiguer** :

- une ou plusieurs **causes probables** ;
- l’**origine** probable de chacune (`Internal`, `External`, `InternalOrExternal`) ;
- une **piste d’analyse** — par où commencer.

L’erreur passe ainsi de *« ce qui a échoué »* à *« ce qui a probablement mal tourné, et par où commencer »* — la différence entre un message d’erreur et un runbook d’astreinte.

## 📚 Une documentation générée depuis votre code

Parce que les erreurs sont décrites dans le code avec le DSL `DescribeError`, leur documentation est **générée automatiquement sous forme de catalogue d’erreurs** — une référence vivante qui reste synchronisée avec le code, prête pour les développeurs comme pour les équipes support.

Ni ErrorOr ni FluentResults ne produisent de documentation à partir de vos définitions d’erreurs ; la description vit, au mieux, dans des chaînes et des métadonnées éparpillées. Ici, **documenter une erreur et la définir sont un seul et même geste**.

## 🎚️ Le fluent là où il aide, du code simple là où il gêne

`Outcome` offre un pipeline fluent (`Then`, `To`, `Recover`, `Finally`) pour composer des étapes sans lever — utilisez-le quand il rend vraiment le flux plus clair.

Mais ce pipeline est un **transport optionnel, pas le centre de gravité**. Quand un simple `if` retournant une erreur de domaine bien nommée lit plus près du métier, FirstClassErrors vous encourage à *écrire ça à la place*. Votre gestion d’erreurs reste à **hauteur métier** ; vous n’êtes jamais poussé vers de longues chaînes fluent juste pour rester « idiomatique ».

Les librairies *Result* orientées railway tendent à faire du pipeline fluent l’idiome principal. Ici, le pipeline sert l’erreur — pas l’inverse.

## 🏛️ Consciente de l’architecture et de l’exploitation

Le modèle parle nativement le langage de la conception en couches / hexagonale :

- une taxonomie de `DomainError`, `InfrastructureError`, et d’erreurs de **port** primaire / secondaire ;
- des préoccupations d’infrastructure comme `Transience` (transitoire / non transitoire) et `InteractionDirection` (entrant / sortant) ;
- une **identité d’occurrence** sur chaque erreur — un `InstanceId` unique et un horodatage UTC — pour corréler journaux et évènements de diagnostic.

ErrorOr et FluentResults sont délibérément agnostiques de l’architecture et gardent l’erreur légère ; ces concepts sont simplement hors de leur périmètre.

## 📊 En un coup d’œil

| | FirstClassErrors | ErrorOr | FluentResults |
|---|:---:|:---:|:---:|
| Retourner les erreurs comme valeurs (style railway) | ✅ (optionnel) | ✅ | ✅ |
| Lever la *même* erreur comme exception typée | ✅ | ➖ | ➖ |
| Modèle d’erreur orienté humain (titre, règle métier, explication) | ✅ | ➖ | ➖ |
| Diagnostics : cause + origine + piste d’analyse | ✅ | ➖ | ➖ |
| Documentation générée depuis le code | ✅ | ➖ | ➖ |
| Taxonomie d’architecture (domaine / infrastructure / port) | ✅ | ➖ | ➖ |
| Identité par occurrence (id + horodatage) | ✅ | ➖ | ➖ |
| Pipeline fluent | Optionnel, par conception | Central | Central |

*➖ signifie « pas un objectif de cette librairie », pas « mal fait » : ErrorOr et FluentResults sont concentrées sur le fait d’être des types résultat épurés.*

## 🧭 Laquelle choisir ?

- Prenez **ErrorOr** quand vous voulez un type résultat minimal et ergonomique, avec une catégorisation d’erreurs propre et adaptée au HTTP.
- Prenez **FluentResults** quand vous voulez un résultat portant des chaînes de raisons riches et des métadonnées.
- Prenez **FirstClassErrors** quand vous voulez que vos erreurs soient une **connaissance documentée et diagnosticable** — décrite une fois dans le code, transportée comme valeur ou levée comme exception, et transformée en catalogue sur lequel toute l’équipe peut s’appuyer.

Elles ne se disputent pas vraiment le même rôle : les deux premières rendent les erreurs faciles à *retourner* ; FirstClassErrors les rend faciles à *comprendre, supporter et documenter*.
