# ADR-0030 | Tirer des chaînes arbitraires depuis un ensemble de valeurs explicite et terminal

🌍 🇬🇧 [English](0030-draw-arbitrary-strings-from-an-explicit-terminal-set.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-21
**Décideurs :** Reefact

## Contexte

`Dummies` fournit des valeurs arbitraires mais valides, avec des contraintes qui expriment ce que le code environnant
exige d'une valeur. Un besoin récurrent est une valeur dont le domaine est une **liste fixe et fermée** que le test
n'assère pas — un code devise tiré d'une petite table, un libellé de statut, le nom d'une entreprise connue. La
bibliothèque génère des formes **structurelles** (une longueur, une famille de caractères, un motif régulier) ; elle
ne sait pas synthétiser un tel ensemble du monde réel, et c'est l'appelant qui détient les valeurs.

Plusieurs faits établis cadrent le choix :

* Les générateurs de scalaires et d'enums exposent déjà `OneOf(params T[])` — tirer uniformément dans une liste
  explicite — mais il s'agit là d'une contrainte **composable** : elle restreint *au sein* de l'intervalle ou du pool
  du type et se cross-valide avec les autres contraintes. `AnyString` n'a aucun `OneOf`.
* `Any.StringMatching` (ADR-0025) est un générateur **terminal** : le motif est toute la spécification, il n'expose
  donc aucune contrainte de forme ou de longueur supplémentaire, tout en se composant via `As`, `OrNull`, `Combine`
  et les générateurs de collections comme n'importe quel `IAny<string>`.
* La surface de mise en forme d'une chaîne est bien plus large que l'intervalle d'un scalaire : préfixe, suffixe,
  fragments contenus, famille de caractères, casse et longueur, chacun déjà cross-validé avec les autres.
* Les collections distinctes bornent à la déclaration selon la cardinalité annoncée par le générateur d'éléments
  (ADR-0013), via l'interface interne `ICardinalityHint<T>` ; un générateur qui n'en annonce pas retombe sur un
  tirage dédupliquant borné.
* La bibliothèque puise dans une source unique seedable pour que tout run soit reproductible (ADR-0006), construit
  les valeurs pour satisfaire les contraintes plutôt que de générer-puis-filtrer, et est livrée **sans aucune
  dépendance runtime ni jeu de données** — son README liste « pas de fausses données réalistes (noms, e-mails,
  adresses) » comme non-objectif explicite (ADR-0011).
* La forme d'appel demandée est `Any.String().OneOf(...)` — chaînée depuis le point d'entrée des chaînes.

## Décision

`Any.String().OneOf(...)` tire la chaîne dans un ensemble de valeurs explicite fourni par l'appelant, sous la forme
d'un générateur **terminal** — l'ensemble est toute la spécification et ne se combine pas avec les autres contraintes
de chaîne — plutôt que comme une contrainte composable à la manière du `OneOf` des générateurs de scalaires.

## Justification

* **Un ensemble terminal garde la surface petite et sans contradiction.** Réconcilier un ensemble de valeurs
  explicite avec le préfixe, le suffixe, les fragments, la famille de caractères, la casse et la longueur d'une chaîne
  multiplierait les combinaisons contradictoires et leurs messages de conflit, pour une combinaison dont personne n'a
  besoin — un appelant qui fournit des valeurs littérales en fixe déjà la forme. Faire de l'ensemble toute la
  spécification supprime cette classe entière d'un coup. `Any.StringMatching` a tranché de même, pour la même raison
  (ADR-0025) ; s'aligner sur ce précédent garde les deux terminaux de chaîne cohérents.
* **Il reste sur `Any.String()` pour la découvrabilité, et reste honnête par l'échec précoce.** Un appelant part de
  `Any.String()` et trouve `OneOf` à côté des autres façons d'obtenir une chaîne. La nature terminale est garantie de
  deux façons : le générateur renvoyé ne porte aucune méthode de mise en forme, et déclarer `OneOf` après une autre
  contrainte lève une `ConflictingAnyConstraintException` à la déclaration — la même règle « un Arrange impossible est
  un défaut du test » que la bibliothèque applique à tout autre conflit.
* **Des valeurs fournies par l'appelant préservent l'identité de la bibliothèque.** Le contenu réaliste vit dans le
  test du consommateur, pas dans le paquet : le non-objectif « pas de fausses données réalistes » tient, et aucun jeu
  de données, dépendance ou appel réseau n'est introduit. `OneOf` est la réponse sans dépendance et déterministe à
  « donne-moi une valeur plausible tirée d'un ensemble connu ».
* **Annoncer la cardinalité garde les collections distinctes précoces.** Un ensemble explicite est un petit domaine
  dénombrable, donc le générateur implémente `ICardinalityHint<string>` ; une collection distincte le borne à la
  déclaration (ADR-0013), exactement comme sur `AnyChar` ou `AnyEnum`, au lieu de compter silencieusement sur le repli
  par tirage dédupliquant borné.
* **La reproductibilité est préservée.** La valeur est un tirage uniforme dans l'ensemble dédupliqué, via la même
  source seedable que tout autre générateur : un run se rejoue sous une graine (ADR-0006) ; dédupliquer empêche
  qu'une valeur listée soit implicitement surpondérée.

## Alternatives considérées

### Un `OneOf` composable sur `AnyString`, comme les générateurs de scalaires

Considérée par symétrie de surface avec `AnyInt32.OneOf` et ses pairs. Rejetée parce que les contraintes de mise en
forme d'une chaîne croisent un ensemble de valeurs explicite de multiples façons, chacune nécessitant sa propre
analyse de conflit précoce et son message, pour une combinaison dont un appelant fournissant des littéraux n'a jamais
besoin — la forme terminale supprime la classe entière, cohérente avec ADR-0025.

### Une factory statique `Any.StringOneOf(...)` (ou `Any.OneOf(...)`), parallèle à `Any.StringMatching`

Considérée parce qu'une factory statique est terminale dès le premier appel et esquive tout cas « une contrainte est
déjà déclarée ». Rejetée parce que la surface demandée et plus découvrable est `Any.String().OneOf(...)`, qui garde
les points d'entrée des chaînes ensemble ; le cas de la contrainte préalable est couvert par un conflit clair à la
déclaration, le mécanisme que la bibliothèque emploie déjà pour toute combinaison impossible.

### Livrer des jeux de données réalistes curés (`Any.CompanyName()`, `Any.FirstName()`, ...)

Considérée parce qu'elle répond directement à « donne-moi une valeur plausible ». Rejetée parce qu'elle contredit le
non-objectif affiché de ne livrer aucune fausse donnée réaliste, et ferait porter à la bibliothèque un jeu de données
ouvert qu'elle devrait maintenir, faire grossir et localiser ; le consommateur fournit l'ensemble et `OneOf` y tire à
la place.

### Générer l'ensemble au premier run via un service externe et le mettre en cache

Considérée comme un moyen de composer l'ensemble sans l'écrire à la main. Rejetée parce qu'elle ajouterait une
dépendance runtime et un premier run non déterministe et non hermétique à une bibliothèque dont l'identité est une
génération déterministe sans dépendance (ADR-0006, ADR-0011) ; composer l'ensemble est une préoccupation de temps de
conception, qui a sa place hors de la bibliothèque.

## Conséquences

### Positives

* Une valeur dont le domaine est une liste courte et fermée devient un dummy d'une ligne, sans dépendance et
  reproductible, qui se compose en objets-valeurs (`As`), en optionnels (`OrNull`) et en collections comme tout autre
  générateur.
* La forme terminale garde la surface des chaînes petite et exempte d'une nouvelle classe de combinaisons de
  contraintes contradictoires.
* Une collection distincte sur l'ensemble est bornée précocement par sa cardinalité, cohérente avec les autres
  générateurs à domaine dénombrable.

### Négatives

* Un nouveau type public (`AnyStringOneOf`) et une méthode à maintenir et documenter, et une seconde forme de `OneOf`
  dans la bibliothèque — terminale pour les chaînes, composable pour les scalaires — que la documentation doit
  expliquer.
* La bibliothèque ne vérifie pas que les valeurs fournies respectent un format externe : c'est le contenu de
  l'appelant, et un objet-valeur a toujours besoin de `As(...)` pour imposer son invariant.

### Risques

* Un appelant peut attendre la composabilité du `OneOf` scalaire et être surpris que celui des chaînes soit terminal.
  Atténué par le type renvoyé qui ne porte aucune méthode de mise en forme et par le conflit à la déclaration lors
  qu'une contrainte le précède — les deux rendent la nature terminale explicite au point d'appel.

## Actions de suivi

* Documenter le générateur dans le README du paquet `Dummies` (fait) et dans la documentation utilisateur lors de la
  prochaine révision de la surface des chaînes.
* Garder exact le non-objectif « pas de fausses données réalistes » du README : `OneOf` tire dans des valeurs
  fournies par l'appelant et ne livre aucun jeu de données.

## Références

* ADR-0025 — Générer les chaînes qui matchent depuis un sous-ensemble régulier maison (le précédent du générateur
  terminal).
* ADR-0013 — Borner les collections distinctes par la cardinalité, sinon par un tirage borné (le contrat
  `ICardinalityHint`).
* ADR-0006 — Fournir des valeurs de test arbitraires depuis une source unique seedable (la reproductibilité).
* ADR-0011 — Héberger Dummies comme un paquet autonome dans ce dépôt (la frontière zéro-dépendance, sans jeu de
  données).
* Le type `AnyStringOneOf`, la méthode `AnyString.OneOf` et leurs tests dans le projet `Dummies` et
  `Dummies.UnitTests`.
