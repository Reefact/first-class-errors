# ADR-0047 | Mesurer la mutation de JustDummies contre la seule suite unitaire déterministe

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0047-measure-justdummies-mutation-against-the-unit-suite-only.md)

**Statut :** Proposé
**Proposé :** 2026-07-27
**Décideurs :** Reefact

## Contexte

L'ADR-0043 a configuré le run de mutation de JustDummies pour utiliser **les deux** suites de tests
comme oracle censé tuer chaque mutant : `JustDummies.UnitTests` **et** `JustDummies.PropertyTests`
(`build/stryker/justdummies.json`, `test-projects`).

La suite property est basée sur FsCheck. Chaque propriété tire ~100 cas aléatoires par run, depuis une
graine aléatoire. Cela en fait deux choses à la fois :

* **La moitié coûteuse du coût par mutant.** Chaque mutant rejoue tout l'oracle
  (`"coverage-analysis": "off"`, ADR-0043), et cent cas par propriété dominent ce temps — ce qui, sur un
  gros fichier changé comme `Any.cs`, fait la différence entre minutes et dizaines de minutes.
* **Un oracle non-déterministe.** Un verdict de mutation répond à « un test de l'oracle échoue-t-il sur ce
  mutant ? ». Avec un oracle randomisé, cette réponse dépend de la graine FsCheck : un mutant peut être
  **tué sur les tirages d'un run et survivant sur ceux d'un autre**. Le score de mutation reflète alors la
  graine autant que le code et les assertions — l'inverse du chiffre reproductible et vrai que
  `"coverage-analysis": "off"` existe pour protéger (ADR-0043).

Le non-déterminisme n'est pas hypothétique. Le 2026-07-27 (issue #335), la suite property elle-même a
flanché en CI : un bug regex `IgnoreCase` latent n'a surgi qu'après ~89 cas FsCheck et a fait échouer le
leg `Build & test` d'une pull request **sans rapport**. Le hasard qui rend un vrai test intermittent rend
intermittent un verdict de mutation bâti dessus.

## Décision

L'oracle de mutation de JustDummies est **la seule suite unitaire déterministe** : `test-projects` dans
`build/stryker/justdummies.json` ne liste plus que `JustDummies.UnitTests`. La suite property FsCheck est
retirée de l'oracle. Elle continue de tourner dans `Build & test` comme vraie assurance — elle ne juge
simplement plus les mutants.

## Justification

* **Un score reproductible.** La mutation mesure désormais si les tests **d'exemple** (unitaires) épinglent
  le comportement — une propriété du code et de ces tests seuls. Le même commit donne le même score, run
  après run, ce qui est tout l'intérêt de le mesurer.
* **Plus rapide, là où ça fait mal.** Les cent cas par propriété de la suite property sont le goulot par
  mutant ; les retirer raccourcit chaque run de mutation — le leg par-PR comme le balayage hebdomadaire.
* **Les property tests protègent toujours la bibliothèque.** Ils tournent dans `Build & test` et attrapent
  les régressions ; ils sont seulement retirés du *juge* de mutation. La mutation demande « tes assertions
  épinglent-elles ce comportement ? », et un test d'exemple est l'oracle naturel et déterministe de cette
  question. Une propriété qui re-randomise à chaque run ne l'est pas — elle répond à une autre question
  (l'invariant tient-il sur de nombreuses entrées ?), que la suite pose toujours là où c'est sa place
  (ADR-0040).

## Alternatives considérées

### Garder la suite property dans l'oracle

Rejeté : elle rend le score de mutation non-reproductible (dépendant de la graine) et est la moitié la plus
lente du run. Les deux sont exactement les coûts que cette décision supprime.

### Semer la suite property à une graine fixe pour le run de mutation

Considéré parce que cela rendrait l'oracle déterministe sans le retirer. Rejeté : c'est toujours la moitié
lente (cent cas par propriété), et cela épingle le score à une graine arbitraire au lieu de supprimer la
dépendance à une graine — un levier caché qui déplace tous les scores dès qu'on y touche. Semer ou non la
suite property dans **`Build & test`** pour tuer le landmine du rouge intermittent (#335) est une question
séparée, tranchée pour elle-même.

## Conséquences

### Positives

* Le score de mutation de JustDummies est reproductible : il dépend du code et des tests unitaires, pas
  d'une graine aléatoire.
* Chaque run de mutation de JustDummies est plus rapide — le leg par-PR comme le balayage complet
  hebdomadaire.

### Négatives

* Un comportement épinglé **uniquement** par un property test, sans aucun test unitaire l'affirmant,
  apparaît désormais comme un **survivant** de mutation. C'est un vrai signal, pas un faux : il dit
  « aucun test d'exemple n'épingle ceci ». Là où la couverture compte vraiment, le correctif est
  d'ajouter un test unitaire — l'ADR-0040 régit déjà quelle suite possède quel cas.
* La base du score se déplace. Avec `break: 0`, cela ne fait rien échouer ; le prochain balayage
  hebdomadaire publie le nouveau chiffre.

## Références

* ADR-0043 — Contrôler les pull requests sur le score de mutation du diff : le run dont ceci restreint
  l'oracle.
* ADR-0040 — Séparer le banc de tests de JustDummies entre une suite d'exemples et une suite de
  propriétés : pourquoi les deux suites répondent à des questions différentes, d'où l'une est l'oracle de
  mutation et l'autre non.
* ADR-0046 — Rendre la porte de mutation par pull request consultative : la décision sœur de
  vitesse/blocage.
* Issue #335 — le flake de la propriété `IgnoreCase` qui a rendu le non-déterminisme concret.
