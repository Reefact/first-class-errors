# ADR-0020 | Matérialiser les dummies uniquement via Generate()

🌍 🇬🇧 [English](0020-materialize-dummies-only-through-generate.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

`Dummies` est une DSL fluide de générateurs typés porteurs de contraintes. Chaque
générateur implémente `IAny<T>`, dont l'unique membre `Generate()` tire une valeur
satisfaisant les contraintes déclarées ; les points de composition `As` et
`Combine` construisent des générateurs plus larges et matérialisent leurs parties
en appelant `Generate()`. Le modèle affiché de la bibliothèque est qu'un générateur
est une **recette immuable, pas une valeur** : l'aléatoire n'est tiré que lorsque
`Generate()` s'exécute, et une même recette peut être générée plusieurs fois, en
produisant une valeur fraîche à chaque fois.

Jusqu'ici, chaque générateur concret définissait aussi une **conversion
implicite** vers son type généré — 28 opérateurs au total, un par type simple et
un par type de collection (`List<T>`, `T[]`, `HashSet<T>`,
`Dictionary<TKey,TValue>`). Cette conversion rendait une affectation à type
explicite concise, par exemple une variable locale `string` affectée directement
depuis un générateur de chaînes.

Plusieurs faits sur cette conversion ont émergé lors d'une revue ciblée de la
bibliothèque (issue #190) :

* La conversion a des **effets de bord** : elle tire de l'aléatoire, ce n'est donc
  pas un élargissement ; elle peut **lever une exception**
  (`AnyGenerationException`, `ConflictingAnyConstraintException`) sur un site qui
  se lit comme une simple affectation ; et elle n'est **pas idempotente** — chaque
  conversion tire une valeur fraîche, si bien que lire deux fois la « même »
  variable donne deux valeurs.
* La conversion ne se déclenche que dans **une** forme syntaxique — une variable
  locale ou un paramètre à type explicite. Dans les formes voisines, elle fait
  silencieusement autre chose : `var` lie le générateur, `object` et
  `params object[]` boxent le générateur, l'inférence générique passe le
  générateur, et des surcharges concurrentes peuvent se résoudre vers le
  générateur plutôt que la valeur. La suite de tests devait déjà utiliser des
  locales à type explicite autour d'une API `params object[]` pour cette raison.
* `Generate()` fonctionne déjà de manière uniforme dans chacun de ces contextes,
  est le membre par lequel passe l'inférence générique, et est l'opération
  qu'utilisent les points de composition. C'est l'idiome dominant dans la suite de
  tests.

Deux contraintes bornent le calendrier. `Dummies` est un package autonome
pré-1.0 dont l'API évoluera le plus dans ses premières itérations, et il n'est
référencé que par son propre projet de test (ADR-0011) ; retirer une surface
d'opérateurs publique est donc peu coûteux maintenant et deviendrait un
changement cassant une fois un `1.0` stable publié. L'issue #190 exige aussi que
le contrat de ces conversions soit décidé et consigné avant ce `1.0`.

## Décision

Les générateurs concrets de `Dummies` n'exposent aucune conversion implicite vers
leur type généré : une valeur n'est matérialisée que par `Generate()`, appelé
directement ou par les points de composition `As` et `Combine` qui l'appellent en
interne.

## Justification

* **Une conversion implicite devrait être bon marché, totale et
  référentiellement transparente ; celle-ci n'est aucune des trois.** Parce
  qu'elle tire de l'aléatoire, peut lever une exception et renvoie une valeur
  différente à chaque exécution, c'est un appel de méthode à effet de bord
  déguisé derrière une affectation. Cela contredit directement le modèle que la
  bibliothèque enseigne — un générateur est une recette, et la valeur n'est tirée
  qu'à `Generate()` — en fournissant l'unique chemin qui laisse l'appelant oublier
  que le tirage a lieu.
* **La commodité est une abstraction partielle et surprenante.** Elle se comporte
  comme annoncé dans une seule forme syntaxique et se comporte silencieusement mal
  dans les formes voisines. La garder en documentant le piège décrirait une
  complexité accidentelle au lieu de la retirer ; la complexité est accidentelle
  précisément parce que `Generate()` couvre déjà tous les contextes de manière
  uniforme.
* **Le retrait ne coûte aucune capacité.** `Generate()` est déjà le chemin
  canonique — au niveau de l'interface, cible de l'inférence générique, opération
  qu'appellent les points de composition, et idiome dominant dans la suite. Ce qui
  est perdu est un raccourci qui économisait un appel dans un seul contexte, pas
  une quelconque expressivité.
* **C'est le moment le moins coûteux pour décider.** Le package est pré-1.0,
  autonome et auto-consommé (ADR-0011), donc le changement ne touche aujourd'hui
  que ses propres tests ; le même retrait après un `1.0` stable casserait chaque
  consommateur qui affectait un générateur à une locale typée. L'issue #190 exige
  que la décision soit consignée avant cette publication.

## Alternatives considérées

### Garder les conversions et documenter le contrat

La direction que privilégie l'issue #190. Envisagée parce qu'elle préserve le site
d'appel vedette concis et indique, en documentation, où la conversion s'exécute ou
non. Rejetée parce qu'elle documente un piège au lieu d'en retirer un, et conserve
une conversion à effet de bord, non idempotente et pouvant lever une exception qui
contredit le modèle recette-contre-valeur au centre de la bibliothèque.

### Garder les conversions et ajouter un analyzer pour les contextes trompeurs

Envisagée parce qu'un analyzer signalant les usages `var`, `object` et par
inférence générique pourrait préserver l'ergonomie tout en attrapant les pièges.
Rejetée parce que c'est une surface large et permanente — 28 opérateurs plus un
analyzer et ses tests — pour préserver un raccourci d'un appel, et parce qu'un
contrat « convertit, sauf là où l'analyzer dit que non » est lui-même
schizophrène. Retirer les opérateurs rend l'analyzer sans objet, ce pourquoi
l'issue #190 le liste comme optionnel.

### Retirer les conversions de certains types seulement

Envisagée comme compromis — par exemple les garder sur les types simples immuables
et ne les retirer que des collections. Rejetée parce qu'une règle par type est
plus difficile à expliquer que l'un ou l'autre choix uniforme, et laisse quand
même la surprise de l'affectation à effet de bord sur les types qui les gardent.

## Conséquences

### Positives

* Il existe une seule façon évidente et uniforme de matérialiser une valeur, et la
  distinction recette-contre-valeur que la bibliothèque enseigne n'est plus
  contredite par une fonctionnalité qui masque le tirage.
* Un générateur ne se substitue jamais silencieusement à sa valeur sous `var`,
  `object`, `params object[]`, inférence générique ou résolution de surcharge ;
  ces sites échouent désormais à la compilation au lieu de mal se comporter.

### Négatives

* Le site d'appel vedette est plus verbeux : une affectation à type explicite gagne
  un `.Generate()`.
* Vingt-huit opérateurs, ainsi que leurs tests et exemples de documentation, sont
  retirés ; la surface publique pré-1.0 change — acceptable maintenant, et la
  raison pour laquelle la décision est prise avant le `1.0`.

### Risques

* Un utilisateur portant un modèle mental de conversion implicite pourrait au
  début omettre `.Generate()`. Le risque est borné : l'omission est une erreur de
  compilation au message actionnable (affecter via `IAny<T>` ou appeler
  `Generate()`), jamais une valeur fausse silencieuse.

## Actions de suivi

* Mettre à jour la documentation pour que `.Generate()` soit présenté comme
  l'unique matérialisation — le README du package et les docs XML — fait dans le
  même changement que cette décision.
* Ne pas poursuivre l'analyzer optionnel suggéré par l'issue #190 ; le retrait le
  rend inutile.
* À revisiter seulement si un futur consommateur hors tests démontre un besoin
  ergonomique que la forme `Generate()` ne peut satisfaire.

## Références

* Issue #190 — Définir et documenter le contrat des conversions implicites de
  générateurs.
* ADR-0011 — Héberger Dummies comme package autonome (churn pré-1.0,
  auto-consommé).
* ADR-0006 — Fournir des valeurs de test arbitraires depuis une source unique à
  graine.
* `Dummies/IAny.cs` — le contrat `Generate()` par lequel passent ces générateurs.
