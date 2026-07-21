# ADR-0032 | Tirer des valeurs arbitraires depuis un pool de choix explicite et de premier niveau

🌍 🇬🇧 [English](0032-draw-arbitrary-values-from-an-explicit-top-level-pool.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-21
**Décideurs :** Reefact

## Contexte

`Dummies` fournit des valeurs arbitraires mais valides depuis une source unique et seedable, de sorte que tout run est
reproductible à partir d'un seed reporté (ADR-0006). Un besoin récurrent est une valeur dont le domaine est un
**ensemble fermé que l'appelant détient déjà** — l'une des devises pour lesquelles un contexte est configuré, l'une des
commandes déjà présentes dans une fixture, l'un d'une poignée d'états métier sur lesquels le test ne porte aucune
assertion. La bibliothèque génère des formes structurelles (une longueur, un intervalle, un motif) ; elle ne peut pas
synthétiser un tel ensemble du monde réel, et c'est l'appelant qui possède les valeurs.

Plusieurs faits existants cadrent le choix :

* La façon la plus courante de tirer aujourd'hui dans un ensemble détenu par l'appelant est écrite à la main —
  `pool[new Random().Next(pool.Count)]` — ce qui tire d'un `Random` neuf, ignore la source seedée ambiante et ne peut
  donc pas être rejoué sous `Any.Reproducibly(...)` : précisément le piège que la bibliothèque existe pour supprimer.
* Les builders scalaires et d'enum exposent `OneOf(params T[])`, mais uniquement **au sein de leur propre domaine** —
  il restreint l'intervalle ou le pool d'un scalaire et se contrôle vis-à-vis des autres contraintes. Il n'existe aucun
  combinateur de premier niveau pour tirer dans un pool d'objets métier arbitraires.
* L'ADR-0030 a ajouté `Any.String().OneOf(...)` comme générateur d'ensemble de valeurs **terminal** chaîné sur le point
  d'entrée des chaînes : il implémente `ICardinalityHint<string>`, déduplique sous une comparaison ordinale, tire
  uniformément et de façon reproductible, et **rejette un élément `null`**, en orientant l'appelant vers `OrNull()`. Il
  est spécifique aux chaînes ; un pool d'objets métier agnostique au type n'a aucun builder typé sur lequel se chaîner.
* Les collections distinctes se gardent, au moment de la déclaration, sur la cardinalité annoncée par le générateur
  d'éléments (ADR-0013), à travers l'interface interne `ICardinalityHint<T>` ; un générateur qui annonce une
  cardinalité répond aussi à l'appartenance.
* `OrNull()` est le décorateur orthogonal de nullabilité de la bibliothèque, pour les types valeur comme référence.
* Avec une méthode `params T[]` seule, passer une unique collection détenue lie le paramètre de type au **type de la
  collection**, non à ses éléments ; et lorsque le type d'élément est lui-même énumérable, une surcharge acceptant
  aussi `IEnumerable<T>` rend `OneOf(collection)` ambigu entre « un pool contenant la collection » et « un pool de ses
  éléments ».
* Une factory qui prend des valeurs brutes plutôt qu'un opérande `IAny<>` n'hérite d'aucun contexte aléatoire depuis un
  opérande, donc — contrairement à `Combine`/`ListOf`/`SetOf` — elle doit exister à la fois sur `Any` (ambiant) et
  `AnyContext` (seedé) ; la garde de parité de surface traite une telle factory comme une factory scalaire et exige le
  miroir.
* L'audit d'architecture et de conception de Dummies du 2026-07-20 (§10) classe cet ajout comme le Must-Have à plus
  fort levier : chaque consommateur, presque chaque semaine.

## Décision

`Any.OneOf<T>(params T[])` et `Any.ElementOf<T>(IReadOnlyList<T>)`/`Any.ElementOf<T>(IEnumerable<T>)` — reflétées sur
`AnyContext` — tirent une valeur uniformément dans un pool explicite fourni par l'appelant, en tant que générateur
terminal, en rejetant un pool vide et tout élément `null`, et en dédupliquant, dimensionnant et testant l'appartenance
du pool sous `EqualityComparer<T>.Default`.

## Justification

* **Cela ferme le piège de reproductibilité qui est la raison d'être de la bibliothèque.** Un tirage de pool conscient
  du seed remplace le `Random` écrit à la main, si bien que le choix se rejoue sous `Any.Reproducibly(...)` et
  `Any.WithSeed(...)` comme tout autre tirage (ADR-0006). L'audit désigne cet ajout comme celui au plus fort levier.
* **Rejeter `null` garde la nullabilité orthogonale et la surface symétrique.** `OrNull()` est l'unique manière
  d'exprimer une valeur optionnelle, aussi un membre de pool `null` réintroduirait-il l'ambiguïté « `null` est-il une
  valeur ou une absence » que ce décorateur existe pour supprimer. Cela s'aligne aussi sur le générateur de chaînes
  livré (ADR-0030) : les deux combinateurs d'ensemble de valeurs restent symétriques sur leur contrat `null` au lieu de
  diverger — le type d'asymétrie contre lequel l'audit met en garde. Un appelant qui veut un `null` occasionnel écrit
  toujours `OneOf(...).OrNull()`.
* **`EqualityComparer<T>.Default` est l'analogue agnostique au type de la déduplication ordinale du générateur de
  chaînes, et c'est le choix *sound* pour le contrat de cardinalité.** Une collection distincte en aval portant un
  comparateur personnalisé plus grossier ne peut que *fusionner* des valeurs du pool, jamais en créer de nouvelles ; le
  nombre distinct annoncé reste donc une borne supérieure conservatrice et l'appartenance ne revendique jamais une
  valeur absente du pool — la collection continue de se garder correctement (ADR-0013).
* **Un générateur terminal qui annonce sa cardinalité compose gratuitement.** Le pool est la spécification tout entière
  — il n'y a aucun domaine scalaire à restreindre — de sorte que le générateur n'expose aucune autre contrainte, tout
  en circulant à travers `As(...)`, `OrNull()`, `Combine(...)` et les générateurs de collections comme tout `IAny<T>`,
  et une collection distincte au-dessus de lui se garde tôt comme les autres générateurs à domaine dénombrable.
* **Deux noms valent mieux qu'un seul nom surchargé.** `OneOf` prend des littéraux en ligne ; `ElementOf` prend une
  collection détenue. La séparation supprime le piège d'inférence générique : les valeurs en ligne ne lient jamais le
  paramètre de type à un conteneur, et une collection détenue n'est jamais confondue avec ses propres éléments. La
  surcharge séquence d'`ElementOf` matérialise une fois, de sorte qu'une requête paresseuse n'est pas ré-énumérée à
  chaque tirage.
* **Le miroir `AnyContext` est requis, pas optionnel.** Le pool ne porte aucun opérande d'où hériter d'un contexte
  seedé, donc sans miroir la surface seedée présenterait un trou silencieux ; la garde de parité fait de l'omission un
  test qui échoue.

## Alternatives considérées

### Autoriser `null` comme membre de pool

Considérée parce qu'un objet métier `null` est sans doute un choix arbitraire valide, et parce qu'un membre `null`
(poids `1/n`) diffère, sur le plan de la distribution, du tirage au sort indépendant d'`OrNull()` — la direction que
l'issue déposée proposait d'abord.

Rejetée parce qu'elle contredirait le `Any.String().OneOf(...)` livré (ADR-0030), réintroduirait l'ambiguïté
valeur-contre-absence qu'`OrNull()` supprime, et scinderait les deux combinateurs d'ensemble de valeurs sur leur
contrat `null` — exactement l'asymétrie de surface que l'audit signale. Le cas du `null` occasionnel reste servi par
`OneOf(...).OrNull()`.

### Un unique `OneOf(params T[])` surchargé plus `OneOf(IEnumerable<T>)`, sans `ElementOf`

Considérée pour l'économie de surface, en miroir des deux surcharges du builder de chaînes.

Rejetée parce que l'inférence générique en fait un piège : passer une collection détenue à la forme `params` met le
conteneur lui-même en pool, et lorsque le type d'élément est énumérable les deux surcharges rendent `OneOf(collection)`
ambigu entre le conteneur et ses éléments. Un nom `ElementOf` distinct rend l'intention non ambiguë au site d'appel.

### Ajouter une surcharge `IEqualityComparer<T>`, comme `SetOf`

Considérée parce qu'un appelant pourrait vouloir que l'identité du pool soit décidée par un comparateur personnalisé.

Rejetée comme inutile pour la v1 : le comparateur par défaut fournit déjà une borne de cardinalité *sound* sous
n'importe quel comparateur aval, et un comparateur spécifique au pool pourra être ajouté plus tard sur preuve de besoin
sans changer le contrat par défaut.

### La chaîner sur un point d'entrée typé, comme le `OneOf` des chaînes

Considérée pour la cohérence avec `Any.String().OneOf(...)`.

Rejetée parce qu'un objet métier arbitraire n'a aucun builder `Any.X()` sur lequel se chaîner — tout l'intérêt est une
factory de premier niveau, agnostique au type — de sorte qu'une factory statique sur `Any`/`AnyContext` est la seule
forme qui convienne.

## Conséquences

### Positives

* La lacune classée première par l'audit est comblée : tirer dans un ensemble détenu par l'appelant devient un dummy
  d'une ligne, reproductible par seed, qui compose vers des objets valeur (`As`), des optionnels (`OrNull`) et des
  collections comme tout autre générateur.
* Les combinateurs d'ensemble de valeurs des chaînes et génériques partagent désormais un seul contrat `null` (rejeté,
  via `OrNull()`), supprimant une asymétrie au lieu d'en ajouter une.
* Une collection distincte au-dessus du pool est gardée tôt par sa cardinalité, en cohérence avec les autres
  générateurs à domaine dénombrable.

### Négatives

* Un nouveau type public (`AnyOneOf<T>`) et deux noms de point d'entrée (`OneOf`/`ElementOf`) à maintenir, documenter
  et garder reflétés sur `AnyContext`.
* La bibliothèque ne vérifie pas que les valeurs du pool respectent un format externe : ce sont le contenu de
  l'appelant, et un objet valeur a toujours besoin d'`As(...)` pour faire respecter son invariant.

### Risques

* Un appelant peut s'attendre à ce que `null` soit un membre de pool légal et être surpris qu'il soit refusé. Atténué
  par le message d'exception qui pointe vers `OrNull()` — le même conseil que donne le générateur de chaînes.
* Un appelant peut passer une collection détenue à `OneOf` et obtenir un pool d'un seul élément. Atténué par `ElementOf`
  qui est le chemin documenté pour une collection détenue, et par le résumé d'`OneOf` qui y oriente.

## Actions de suivi

* Documenter `OneOf`/`ElementOf` dans le guide utilisateur de Dummies (`ArbitraryTestValues.en.md`) et sa traduction
  française, ainsi que dans le README du package (`README.nuget.md`), avec un exemple.
* Garder le miroir `Any`↔`AnyContext` au vert (imposé par `SurfaceParityTests`).
* Envisager d'aligner les messages d'élément `null` du générateur de chaînes et du générateur générique lors de la
  prochaine révision de la surface des chaînes.

## Références

* ADR-0030 — Tirer des chaînes arbitraires depuis un ensemble de valeurs explicite et terminal (le frère « chaînes » ;
  le précédent générateur terminal et rejet du `null`).
* ADR-0013 — Garder les collections distinctes par cardinalité, sinon par un tirage borné (le contrat
  `ICardinalityHint`).
* ADR-0006 — Fournir les valeurs de test arbitraires depuis une source unique et seedable (reproductibilité).
* ADR-0031 — Nommer les factories scalaires d'Any d'après leur type CLR (pourquoi `OneOf`/`ElementOf`, en tant que
  combinateurs, sont exemptés).
* ADR-0020 — Ne matérialiser les dummies qu'à travers `Generate()`.
* Issue [#223](https://github.com/Reefact/first-class-errors/issues/223) et l'audit d'architecture et de conception de
  Dummies du 2026-07-20 (§10 Must-Have).
* Le type `AnyOneOf<T>`, les factories `Any.OneOf`/`Any.ElementOf` et `AnyContext.OneOf`/`AnyContext.ElementOf`, et
  leurs tests dans le projet `Dummies` et `Dummies.UnitTests`.
