# ADR-0060 | Faire primer l'intention énoncée sur le conseil générique d'un analyseur

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0060-let-stated-intent-outrank-generic-analyzer-advice.md)

**Statut :** Proposé
**Proposé :** 2026-07-29
**Décideurs :** Reefact

## Contexte

Le rapport SonarQube Cloud du projet porte 255 constats ouverts. Quatre de ses
règles signalent du code qui n'est pas défectueux mais délibéré, et représentent
ensemble 65 de ces constats. Elles se répartissent en deux familles, selon
l'endroit où le constat est produit.

Deux arrivent sous l'espace de noms `external_roslyn`, c'est-à-dire qu'elles ne
relèvent pas de l'analyse propre à SonarQube : ce sont des diagnostics émis par
le compilateur .NET et par les analyseurs de la BCL pendant la compilation, que
le scanner observe via MSBuild et republie. Une règle réglée sur `none` n'est
jamais émise ; le rapport la perd donc à la source. Les deux autres relèvent de
l'analyse shell propre à SonarQube, qu'aucun réglage de compilation n'atteint —
rien de ce que fait le compilateur ne les produit ni ne les supprime.

Les quatre règles, et ce que fait aujourd'hui le code qu'elles signalent :

* **`CA1859` — 22 constats.** Demande que les membres non publics typés
  `IReadOnlyList<T>` ou `IEnumerable<T>` soient retypés vers la collection
  concrète qu'on les observe retourner, afin que les appelants fassent un appel
  direct plutôt qu'une répartition par interface. La règle ne se déclenche que
  sur des membres non publics. Dans le code signalé, l'interface exprime un
  contrat : une aide construisant un message d'erreur retourne
  `IReadOnlyList<string>` pour que ses appelants ne puissent pas muter le
  résultat, et les aides de test prennent `IAny<T>` précisément parce que c'est
  l'abstraction publique qui est sous test.
* **`CA1861` — 22 constats, tous dans un projet de test.** Demande qu'un tableau
  constant passé en argument soit hissé dans un champ `static readonly`, pour
  n'être alloué qu'une fois au lieu d'une fois par appel. Les arguments signalés
  sont les valeurs attendues d'assertions et les listes de cas de générateurs de
  propriétés, écrites en ligne à côté de la vérification qui les lit.
* **`S7682` — 12 constats**, dans l'outillage shell du dépôt et les *hooks*
  Claude. Demande un `return` explicite à la fin d'une fonction shell. Chaque
  fonction signalée se termine par la commande dont le code de sortie est le
  résultat voulu de la fonction — un `cat` avec document en ligne, un appel à
  `awk`, un `printf` — et l'une d'elles se termine par `exit`, après quoi un
  `return` est inatteignable.
* **`S7679` — 9 constats**, dans les mêmes scripts. Demande qu'un paramètre
  positionnel soit affecté à une variable locale. Tous les scripts du dépôt
  déclarent `#!/bin/sh`, et `local` ne fait pas partie de POSIX ;
  `tools/trains.sh` montre déjà ce que coûte l'obéissance sans lui, puisque la
  seule aide qui y avait besoin de paramètres nommés porte des variables
  globales préfixées `_tf_`. Les autres fonctions signalées sont des aides d'une
  ou deux lignes dont le `$1` se trouve une ligne sous le nom de la fonction.

Le code visé par ces règles se trouve sur des chemins de construction d'erreurs,
dans l'outillage de documentation, dans les suites de tests et dans les scripts
du dépôt. Rien de tout cela n'est un chemin chaud mesuré, et aucune exigence de
performance n'est consignée à son encontre.

Le dépôt porte déjà les deux précédents entre lesquels cette décision s'inscrit.
L'ADR-0055 a établi qu'une règle de style que le compilateur sait exprimer est
redite dans `.editorconfig` et appliquée à la compilation, le fichier DotSettings
restant la référence pour tout ce que Roslyn ne sait pas exprimer. L'ADR-0058 a
décliné `CA1510` et choisi une suppression par projet plutôt qu'à l'échelle du
dépôt, au motif exprès que les projets capables d'honorer une règle doivent la
conserver. Par ailleurs, les règles de codage du dépôt tranchent déjà un
arbitrage performance-contre-invariant en faveur de l'invariant : les objets
valeur et les résultats restent des classes validantes plutôt que des structures,
parce que la correction prime l'allocation sur les chemins d'erreur.

## Décision

Le conseil générique d'un analyseur est décliné — par écrit, à côté de sa raison,
et à la portée la plus étroite qui couvre le constat — partout où le code
signalé exprime délibérément une intention, la lisibilité et les contrats énoncés
primant la micro-performance tant qu'aucun besoin mesuré n'est consigné.

## Justification

* **Les règles sont génériques ; le code est spécifique.** Chacune des trois est
  juste là où elle a été écrite, et fausse ici pour une raison que l'analyseur ne
  peut pas voir. `CA1859` ne sait pas distinguer une abstraction fortuite d'un
  contrat : elle lit `IReadOnlyList<string>` comme un oubli alors que c'est tout
  le propos, et l'honorer offrirait `.Add()` à chaque appelant contre quelques
  nanosecondes sur un chemin parcouru une fois par conflit de validation.
  `CA1861` ne sait pas distinguer une boucle chaude d'une assertion : elle
  éloignerait les valeurs attendues d'un test de la vérification qui les lit,
  pour économiser une allocation survenant quelques centaines de fois dans une
  suite. Les désactiver n'est pas esquiver le conseil, c'est y répondre.
* **Décliner dans la configuration vaut mieux que décliner dans le rapport.**
  Partout où un constat naît de la compilation, une sévérité `none` l'empêche
  d'être produit ; là où ce n'est pas le cas — les deux règles shell — c'est la
  configuration du scanner qui porte le refus. Dans les deux cas la décision
  atterrit dans un fichier qui vit dans le dépôt et porte sa raison en ligne, là
  où un « ne sera pas corrigé » sur le serveur SonarQube mettrait le
  raisonnement à un endroit que le code ne montre jamais.
* **L'endroit où le refus est écrit suit l'endroit où le constat est produit.**
  Les règles Roslyn sont déclinées dans `.editorconfig`, que lit le compilateur :
  la compilation cesse de les émettre et chaque contributeur rencontre la raison
  là même où la règle se serait déclenchée. Les règles shell ne sont pas
  joignables ainsi et sont déclinées dans l'invocation du scanner. Les séparer
  n'est pas une incohérence, mais le seul agencement où chaque refus siège là où
  vit sa règle.
* **La portée suit la raison, non la commodité.** La justification de `CA1861`
  porte sur les tests, et tous ses constats sont dans des projets de test : elle
  est donc déclinée pour les projets de test et laissée active pour le code
  livré, où un chemin chaud peut réellement la vouloir. `CA1859` et les deux
  règles shell sont déclinées sur tout le code qu'elles atteignent, parce que
  leurs justifications valent partout où elles se déclenchent. Le principe de l'ADR-0058 — un projet capable
  d'honorer une règle la conserve — est ainsi préservé, tout en reconnaissant
  qu'ici la raison de décliner est uniforme plutôt qu'un accident de plateforme.
* **Le volet performance est un arbitrage que ce dépôt a déjà rendu.** Les deux
  règles de performance réclament la même monnaie : de la lisibilité dépensée
  pour une vitesse que personne n'a demandée. La règle des objets valeur en
  classes a tranché le même arbitrage dans le même sens. Le décider une fois, de
  façon générale, évite de le rejouer à chaque constat.
* **On décline le conseil que le code contredit, pas celui qui coûte cher.** Le
  même rapport portait un cinquième candidat, `IDE0028`, avec 147 constats — de
  loin le plus gros groupe et le moins cher à faire disparaître. Il est appliqué
  au contraire, parce que ses constats signalaient une dérive réelle (le code
  écrivait les initialiseurs de collection des deux façons, 85 sites contre 147)
  et non un choix délibéré. Le volume n'est pas un argument pour décliner une
  règle, et cette ADR ne veut pas être lue comme s'il l'était.

## Alternatives envisagées

### Appliquer les quatre règles

Élimine 65 constats en s'y conformant, et ne laisse aucune suppression à
expliquer.

Rejetée parce qu'elle inverse le propos de l'exercice. Chacune des quatre
dégraderait le code qu'elle touche : élargir un contrat de lecture seule en
contrat mutable, séparer les données d'un test de l'assertion qui les lit,
ajouter un `return` qui soit masque un échec soit redit le comportement par
défaut, et introduire un `local` non POSIX dans des scripts qui déclarent
`#!/bin/sh`.

### Supprimer site par site avec `[SuppressMessage]` et une justification

La portée la plus fine possible, chaque suppression portant sa raison à la ligne
exacte qui l'a levée.

Rejetée sur le volume, sur le message et sur la portée. Soixante-cinq attributs
ajouteraient plus de lignes que les corrections qu'ils remplacent, répéter un
argument une fois par site l'énonce de nombreuses fois sans jamais l'énoncer une
seule, et les règles shell ne disposent d'aucun mécanisme de ce genre. La raison
est ici une politique, non une exception locale, et une politique tient en un
seul endroit.

### Marquer les constats « ne sera pas corrigé » dans SonarQube Cloud

Ne coûte rien dans le dépôt et vide le rapport immédiatement.

Rejetée parce qu'elle place la décision hors du code. La compilation continuerait
d'émettre les diagnostics, chaque nouvelle occurrence devrait être écartée à la
main, et un contributeur lisant les sources ne trouverait aucune trace du
raisonnement — exactement l'échec que l'ADR-0056 a consigné lorsqu'une règle ne
vivait que là où les lecteurs du code ne pouvaient pas la voir.

### Décliner `CA1861` à l'échelle du dépôt également

Plus simple, et symétrique des deux autres.

Rejetée parce que la justification ne porte pas si loin. L'argument est qu'un
littéral à côté de son assertion est plus clair qu'un champ hissé ; dans du code
livré à l'intérieur d'une boucle, c'est l'argument de la règle qui l'emporte.
La décliner là où elle n'est pas justifiée échangerait une décision précise
contre une décision ordonnée, et retirerait le rappel au seul endroit où il
pourrait compter.

## Conséquences

### Positives

* 65 constats sur 255 disparaissent, et toute occurrence future disparaît avec
  eux au lieu de s'accumuler.
* Le raisonnement vit à côté de son effet — dans `.editorconfig` pour les règles
  que la compilation produit, dans l'invocation du scanner pour les deux qu'elle
  ne produit pas — lisible par quiconque, humain ou agent, édite le dépôt.
* Les deux volets de la politique sont énoncés une fois et peuvent être cités,
  si bien que le même argument n'est pas rejoué à chaque nouveau constat.
* `CA1861` reste active là où elle pourrait réellement payer : la décision
  conserve donc sa propre porte de sortie.

### Négatives

* Plus aucun analyseur n'orientera un chemin livré réellement chaud vers un type
  de retour concret, puisque `CA1859` est éteinte partout. Ce jugement repose
  désormais entièrement sur l'auteur et le relecteur.
* Quatre règles déclinées, c'est une liste qui peut croître, et elle vit dans
  deux fichiers. Chaque ajout exige la même justification, et rien d'autre que la
  relecture ne l'impose.

### Risques

* S'en tenir au décompte surestime le changement : aucun code n'a été amélioré.
  La valeur tient ici à une politique consignée et à un rapport qui ne montre
  plus que ce sur quoi il vaut la peine d'agir.
* Un contributeur pourrait lire les sections de règles déclinées comme une
  licence d'éteindre tout analyseur gênant. Elles sont bornées à quatre
  identifiants de règle nommés, chacun portant sa raison, précisément pour
  rendre cette lecture difficile à tenir.
* Si une exigence de performance venait à être consignée sur un chemin couvert
  par ces règles, la décision devrait y être réexaminée plutôt que supposée
  toujours valide.

## Actions de suivi

* Réexaminer la décision sur `CA1859` pour tout chemin de code qui acquerrait une
  exigence de performance mesurée.

## Références

* ADR-0055 — la redite dans `.editorconfig` des règles de style exprimables par
  le compilateur, et leur application à la compilation.
* ADR-0056 — énoncer les règles de codage là où un agent peut s'en saisir, et
  pourquoi une décision consignée hors de portée du code ne tient pas.
* ADR-0058 — le refus de `CA1510`, et le principe de portée que cette ADR suit.
* `.editorconfig` — où vivent les trois règles Roslyn déclinées, chacune avec sa
  raison.
* `.github/workflows/sonar.yml` — où vivent les deux règles shell déclinées, pour
  la même raison et au seul endroit qui puisse la porter.
* `CONTRIBUTING.md`, `CLAUDE.md` — les règles de codage, dont l'arbitrage
  « objets valeur en classes » cité dans la Justification.
