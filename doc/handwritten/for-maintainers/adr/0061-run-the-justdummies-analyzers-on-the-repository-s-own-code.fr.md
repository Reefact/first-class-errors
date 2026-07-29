# ADR-0061 | Faire tourner les analyseurs JustDummies sur le code du dépôt lui-même

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0061-run-the-justdummies-analyzers-on-the-repository-s-own-code.md)

**Statut :** Accepté
**Proposé :** 2026-07-29
**Accepté :** 2026-07-29
**Décideurs :** Reefact

## Contexte

Ce dépôt livre deux paquets d'analyseurs. `FirstClassErrors.Analyzers` porte
`FCE001`–`FCE022` ; `JustDummies.Analyzers`, créé sous
[ADR-0044](0044-ship-justdummies-analyzers.md), porte `JD001`–`JD028`.

Les deux sont vérifiés de façon très différente.

Les analyseurs FirstClassErrors sont chargés par `FirstClassErrors.Usage`, que le flux
`analyzers` construit sur chaque pull request. Les règles tournent donc, à chaque
changement, sur du code écrit pour utiliser la bibliothèque et non pour exercer les
règles.

Les analyseurs JustDummies ne sont chargés par aucun projet de ce dépôt. Leur seule
vérification permanente est leur propre suite unitaire — 246 tests, chacun compilant une
snippet écrite par l'auteur de la règle. Au-delà, un agent injecte l'analyseur construit
dans les suites du dépôt à la main, via une propriété MSBuild, et lit les avertissements.

Ce balayage manuel n'est pas une formalité. Il a contredit le modèle de l'auteur au moins
une fois à chacune des quatre dernières vagues de règles, et chaque contradiction portait
sur une règle qui serait partie fausse :

* `JD015` modélisait la casse des lettres comme un vivier de caractères, ce qui aurait
  condamné le légal `UpperCase().StartingWith("ORD-")`.
* `JD016` comptait les membres déclarés d'un enum alors qu'`AllowingCombinations()`
  élargit l'univers à leur clôture par OU.
* `JD023` tenait `LessThanOrEqualTo(long.MinValue)` pour insatisfiable, alors que la
  suite de la bibliothèque asserte que cette chaîne est légale.
* `JD028` supposait que chaque tirage est une instance neuve, ce qui est faux pour un
  réservoir : `OneOf` rend les références mêmes qu'on lui a données.
* Le retrait de `JD027` devant un composeur qui lève ne se déclenchait pas, parce qu'un
  `=> throw` en corps d'expression est un retour qui *porte* le throw, sous une
  conversion.

Aucune de ces erreurs n'a été trouvée par la suite unitaire, et aucune ne pouvait l'être :
l'auteur écrit à la fois la règle et la snippet qui la teste, donc une idée fausse
partagée passe des deux côtés. Toutes ont été trouvées sur le code de la bibliothèque
elle-même, écrit pour d'autres raisons et qui ne partage donc pas l'idée fausse.

Les règles couvrent quatre sévérités. `JD001`–`JD005` sont `Error` ; la plupart sont
`Warning` ; `JD020`, `JD022` et `JD024` sont `Info` ; `JD011` et `JD019` sont livrées
désactivées, sur adhésion explicite. Roslyn ne fait pas remonter les diagnostics `Info` à
la verbosité de compilation par défaut — un balayage précoce a paru propre exactement
pour cette raison, et n'a rien signalé alors que deux règles étaient actives et
silencieuses.

Les suites de tests de la bibliothèque écrivent délibérément les formes que les règles
signalent. Ce n'est pas accessoire : un test d'un comportement doit l'exercer. Sept sites
de ce genre existent aujourd'hui. Cinq portent un `SuppressMessage` qui nomme leur règle
et dit pourquoi la forme est délibérée ; les deux tests d'écrasement des doublons n'en
portent pas.

Le chargement des analyseurs dans `JustDummies.UnitTests` a été mesuré avant l'écriture
de ce document. La compilation réussit. Exactement deux diagnostics sont signalés — les
deux tests d'écrasement des doublons. Les cinq suppressions existantes font taire leur
règle : le mécanisme fonctionne donc sur un vrai test. Deux attributs de plus ramènent la
surface à zéro. Les autres projets consommant JustDummies ne signalent rien du tout. Une
compilation à froid, non incrémentale, de ce projet est passée d'environ six secondes et
demie à environ neuf.

## Décision

Charger `JustDummies.Analyzers` dans chaque projet de ce dépôt qui consomme JustDummies,
de sorte que les règles tournent à la compilation et dans l'IDE, et consigner chaque
violation délibérée par une suppression nommant la règle à laquelle elle répond.

## Justification

Une règle de ce catalogue est une affirmation sur le comportement de JustDummies. Sa
suite unitaire prouve quelque chose de plus faible : que la règle se déclenche sur une
snippet que son auteur a écrite pour elle. Quand le modèle que l'auteur se fait de la
bibliothèque est faux, les deux côtés de ce test sont faux ensemble et il passe. Chacune
des erreurs de modèle listées dans le contexte a passé sa suite unitaire.

Ce qui les a attrapées, c'est du code réaliste — les suites de la bibliothèque, écrites
pour exercer JustDummies et non pour exercer les règles. Ce corpus est le seul de ce genre
que le dépôt maîtrise, et y faire tourner les règles est la seule vérification qui puisse
échouer pour une raison à laquelle l'auteur de la règle n'a pas pensé. Rendre cela continu
plutôt que manuel est toute la décision.

L'arrangement actuel a le signal sans la garantie. Il dépend de la mémoire de qui
travaille, pour un balayage que rien dans le dépôt ne réclame — et l'histoire montre ce
que cela vaut : le balayage n'a pas été fait du tout avant la quatrième vague, et les
règles parties avant lui sont celles qu'il a fallu corriger ensuite. Un contrôle qui a
attrapé cinq règles fausses et qui repose sur la mémoire est un contrôle qui finira par ne
pas être fait.

La surface de suppression mesurée — deux sites, sur un mécanisme déjà éprouvé sur cinq
autres — est assez petite pour que la décision soit adoptable aujourd'hui plutôt qu'après
un nettoyage. Plus important : ces suppressions ne sont pas un coût toléré. Un test qui
écrit une forme signalée est le test *de* cette forme, et l'attribut est l'endroit où un
lecteur futur apprend que la forme est le sujet et non une erreur. Le dépôt allait déjà
dans ce sens : les cinq suppressions existantes ont été écrites avant ce document,
précisément parce que l'annotation se lit mieux que la forme nue.

Que les règles de sévérité `Error` cassent la compilation si elles se déclenchent un jour
sur le code du dépôt est le comportement correct, non un inconvénient à contourner. Une
règle `Error` qui se déclenche sur du code réaliste signifie soit un vrai défaut, soit une
règle fausse, et les deux doivent être tranchés avant la fusion.

Le coût de compilation est connu, borné, et payé sur un projet de test plutôt que sur quoi
que ce soit qui est livré.

## Alternatives envisagées

### Continuer le dogfooding à la main

C'est ce qui a trouvé toutes les erreurs de modèle : ce n'est donc pas inefficace — mais
son efficacité n'est pas la question. Rien dans le dépôt n'énonce que le balayage existe,
quand il doit être fait, ni ce que signifie un résultat propre ; le contrôle ne survit donc
qu'aussi longtemps que celui qui travaille s'en souvient. L'histoire montre déjà la
défaillance : trois vagues de règles sont parties avant que le balayage ne devienne une
habitude, et ce sont les vagues dont les règles ont eu besoin d'être corrigées.

Rejetée parce qu'une vérification qui dépend de la mémoire n'est pas une vérification.

### Ajouter un projet d'exemple JustDummies, en miroir de FirstClassErrors.Usage

Symétrique de l'arrangement qui fonctionne déjà pour les analyseurs FirstClassErrors, et
il ne porterait aucune pression de suppression, puisqu'un exemple écrit pour démontrer la
bibliothèque n'a aucune raison d'écrire une forme signalée exprès.

Rejetée comme *la* réponse, parce qu'un exemple n'exerce que ce que quelqu'un a pensé à y
mettre. Aucune des erreurs de modèle ne vient d'un exemple ; elles viennent de suites
écrites bien avant l'existence des règles, pour des raisons étrangères à elles, ce qui est
exactement pourquoi elles ne partageaient pas l'idée fausse de l'auteur. Un exemple n'en
aurait trouvé aucune.

Elle reste un ajout raisonnable pour ses propres mérites — de la documentation qui
compile — et ce document ne plaide pas contre.

### Faire du balayage un job CI consultatif

La mécanique existe déjà ; formaliser l'injection en job de flux mettrait le résultat sur
chaque pull request sans toucher au moindre fichier projet, et le dépôt a un précédent de
contrôle consultatif avec le score de mutation par pull request
([ADR-0046](0046-make-the-per-pull-request-mutation-gate-advisory.md)).

Rejetée pour deux raisons. Cela garde le résultat hors de l'IDE, là où l'auteur se trouve
au moment où l'erreur est commise et où une règle sur une erreur silencieuse vaut le plus ;
et un signal consultatif est un signal que personne n'est tenu de traiter, ce qui
reproduit le mode de défaillance que ce document existe pour fermer. Le précédent de la
mutation ne se transpose pas : un score de mutation est une mesure continue dont le seuil
relève du jugement, alors qu'un diagnostic est une affirmation binaire que quelque chose de
précis ne va pas.

## Conséquences

### Positives

* Les règles sont vérifiées en continu contre du code qui n'a pas été écrit pour leur
  plaire, ce qui est la seule vérification capable de révéler un modèle faux.
* Les cinq erreurs de modèle que cette pratique a déjà attrapées deviennent impossibles à
  réintroduire en silence.
* Les attributs `SuppressMessage` deviennent vivants : une règle qui cesse de se
  déclencher sur un site qui prétend l'exercer est le signe que la règle ou la
  bibliothèque a bougé.
* Le taux de faux positifs d'une nouvelle règle se mesure pendant qu'on l'écrit, dans
  l'IDE, au lieu d'à la fin d'une vague.

### Négatives

* Chaque projet qui consomme JustDummies paie le coût de l'analyseur à chaque
  compilation.
* Une nouvelle règle peut exiger de nouvelles suppressions dans les suites de la
  bibliothèque avant de pouvoir être fusionnée.
* Qui écrit désormais un test JustDummies rencontre les règles, et doit savoir pourquoi
  une suppression est la bonne réponse plutôt qu'un contournement.

### Risques

* **Les règles `Info` restent invisibles.** `JD020`, `JD022` et `JD024` ne remontent pas
  à la verbosité par défaut : cette décision ne les vérifie donc pas — elle vérifie les
  règles bruyantes. Une compilation propre se lira comme une couverture complète alors que
  trois règles ne sont pas exercées, ce qui est précisément le piège qui a fait paraître
  propre un balayage précoce.
* **Les règles sur adhésion restent éteintes.** `JD011` et `JD019` sont livrées
  désactivées et ne tourneraient pas, laissant deux règles sans aucune vérification
  permanente.
* **Une règle `Error` peut casser la compilation sur une forme délibérée.** La réponse est
  une suppression, mais pour une règle introduite dans le même changement l'échec arrive à
  la fusion plutôt qu'à l'écriture.
* Le projet de test de l'analyseur ne doit pas charger l'analyseur qu'il teste.

## Actions de suivi

* Décider si les règles `Info` doivent être escaladées dans la configuration du dépôt
  lui-même. Sans cela, les trois règles dont toute la valeur est que l'exécution ne dit
  rien sont les trois que cette décision n'exerce pas.
* Décider si `JD011` et `JD019` doivent être exercées quelque part, étant livrées
  désactivées par choix.
* Réexaminer si les analyseurs FirstClassErrors, vérifiés aujourd'hui uniquement par
  `FirstClassErrors.Usage`, devraient atteindre les suites de cette bibliothèque au titre
  du même argument.

## Références

* [ADR-0044](0044-ship-justdummies-analyzers.md) — la décision de livrer des analyseurs
  JustDummies de première partie.
* [ADR-0046](0046-make-the-per-pull-request-mutation-gate-advisory.md) — le contrôle
  consultatif par pull request que ce document refuse d'imiter.
* [ADR-0059](0059-guard-the-recipe-versus-value-boundary-with-analyzers.md) — les règles
  recette-contre-valeur, dont le dogfooding a produit une partie des preuves ci-dessus.
* [Les règles d'analyse JustDummies](../../for-users/analyzers/README.md).
